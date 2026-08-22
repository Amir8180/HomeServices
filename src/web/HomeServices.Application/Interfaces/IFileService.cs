using HomeServices.Application.Dtos;
using HomeServices.Domain.Enums;

namespace HomeServices.Application.Interfaces;

/// <summary>
/// Abstraction over the file/media system: saves uploaded images to disk under
/// wwwroot/uploads, generates a thumbnail with ImageSharp, and records the asset
/// in the central Media table. Implemented in Infrastructure.
/// </summary>
public interface IFileService
{
    /// <summary>Saves an uploaded image and generates a thumbnail. Returns the media record.</summary>
    Task<MediaDto> SaveImageAsync(
        Stream stream,
        string fileName,
        string contentType,
        long fileSize,
        MediaEntityType? entityType,
        Guid? uploadedBy,
        CancellationToken cancellationToken = default);

    /// <summary>Saves an uploaded image or video (no thumbnail for videos). Returns the media record.</summary>
    Task<MediaDto> SaveMediaAsync(
        Stream stream,
        string fileName,
        string contentType,
        long fileSize,
        MediaType mediaType,
        MediaEntityType? entityType,
        Guid? uploadedBy,
        CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes a media record (file kept on disk for reference).</summary>
    Task<bool> DeleteAsync(int mediaId, CancellationToken cancellationToken = default);

    Task<MediaDto?> GetByIdAsync(int mediaId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MediaDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}
