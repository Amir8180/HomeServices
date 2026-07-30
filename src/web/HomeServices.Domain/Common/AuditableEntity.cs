namespace HomeServices.Domain.Common;

/// <summary>
/// Base entity that also tracks which user created and last updated the record.
/// Used by entities that require full audit traceability (orders, payments, reviews).
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    /// <summary>Identifier (from the Identity service) of the user who created this record.</summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>Identifier (from the Identity service) of the user who last updated this record.</summary>
    public Guid? UpdatedBy { get; set; }
}
