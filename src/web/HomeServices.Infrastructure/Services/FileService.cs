using AutoMapper;
using HomeServices.Application.Dtos;
using HomeServices.Application.Interfaces;
using HomeServices.Domain.Entities;
using HomeServices.Domain.Enums;
using HomeServices.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace HomeServices.Infrastructure.Services;

/// <summary>
/// File/media service. Saves uploaded images to wwwroot/uploads (organised by entity
/// type and year/month), generates a compressed JPEG thumbnail via ImageSharp, and
/// records the asset in the central Media table so the admin file manager can list,
/// search and soft-delete it.
/// </summary>
public class FileService : IFileService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FileService> _logger;

    private const string UploadRoot = "uploads";
    private const int ThumbnailWidth = 400;
    private const long MaxFileBytes = 10 * 1024 * 1024; // 10 MB

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/jpg", "image/png", "image/webp", "image/gif"
    };

    public FileService(
        AppDbContext db,
        IMapper mapper,
        IWebHostEnvironment env,
        ILogger<FileService> logger)
    {
        _db = db;
        _mapper = mapper;
        _env = env;
        _logger = logger;
    }

    public async Task<MediaDto> SaveImageAsync(
        Stream stream,
        string fileName,
        string contentType,
        long fileSize,
        MediaEntityType? entityType,
        Guid? uploadedBy,
        CancellationToken cancellationToken = default)
    {
        if (!AllowedContentTypes.Contains(contentType))
            throw new InvalidOperationException($"Unsupported content type '{contentType}'. Allowed: jpeg, png, webp, gif.");
        if (fileSize > MaxFileBytes)
            throw new InvalidOperationException($"File exceeds the {MaxFileBytes / (1024 * 1024)} MB limit.");

        var now = DateTime.UtcNow;
        var relativeDir = Path.Combine(UploadRoot, entityType?.ToString() ?? "misc", now.ToString("yyyy"), now.ToString("MM"));
        var absoluteDir = Path.Combine(_env.WebRootPath, relativeDir);
        Directory.CreateDirectory(absoluteDir);

        var safeName = Path.GetFileName(fileName);
        var uniqueName = $"{now:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{Path.GetExtension(safeName)}";
        var absolutePath = Path.Combine(absoluteDir, uniqueName);

        // Save the original image.
        await using (var fs = new FileStream(absolutePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await stream.CopyToAsync(fs, cancellationToken);
        }

        // Generate a thumbnail next to the original.
        string? thumbnailRelativeUrl = null;
        try
        {
            using var image = await SixLabors.ImageSharp.Image.LoadAsync(absolutePath, cancellationToken);
            image.Mutate(ctx => ctx.Resize(new ResizeOptions
            {
                Size = new Size(ThumbnailWidth, 0),
                Mode = ResizeMode.Max,
            }));

            var thumbName = $"thumb_{Path.GetFileNameWithoutExtension(uniqueName)}.jpg";
            var thumbPath = Path.Combine(absoluteDir, thumbName);
            await image.SaveAsJpegAsync(thumbPath, cancellationToken);
            thumbnailRelativeUrl = $"/{relativeDir.Replace('\\', '/')}/{thumbName}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Thumbnail generation failed for {File}; original will be used.", uniqueName);
        }

        var originalRelativeUrl = $"/{relativeDir.Replace('\\', '/')}/{uniqueName}";

        var media = new Media
        {
            FileName = safeName,
            OriginalUrl = originalRelativeUrl,
            ThumbnailUrl = thumbnailRelativeUrl ?? originalRelativeUrl,
            ContentType = contentType,
            FileSizeBytes = fileSize,
            MediaType = MediaType.Image,
            EntityType = entityType,
            UploadedBy = uploadedBy,
        };

        _db.Media.Add(media);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Saved media {Id} ({File}) for {EntityType}.", media.Id, safeName, entityType);

        return _mapper.Map<MediaDto>(media);
    }

    public async Task<bool> DeleteAsync(int mediaId, CancellationToken cancellationToken = default)
    {
        var media = await _db.Media.FirstOrDefaultAsync(m => m.Id == mediaId, cancellationToken);
        if (media == null) return false;

        media.IsDeleted = true;
        media.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<MediaDto?> GetByIdAsync(int mediaId, CancellationToken cancellationToken = default)
    {
        var media = await _db.Media.AsNoTracking().FirstOrDefaultAsync(m => m.Id == mediaId, cancellationToken);
        return media == null ? null : _mapper.Map<MediaDto>(media);
    }

    public async Task<IReadOnlyList<MediaDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var items = await _db.Media.AsNoTracking()
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return _mapper.Map<List<MediaDto>>(items);
    }
}
