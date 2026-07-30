namespace HomeServices.Domain.Common;

/// <summary>
/// Base class for all domain entities. Provides identity, audit timestamps and a
/// soft-delete flag so records are never physically removed from the database.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
