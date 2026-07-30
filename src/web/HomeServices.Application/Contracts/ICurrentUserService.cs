using HomeServices.Shared.Enums;

namespace HomeServices.Application.Contracts;

/// <summary>
/// Abstraction over the current HTTP user (claims principal). Implemented in the
/// web layer and injected into services that need to know who the current user is
/// without depending on ASP.NET Core directly.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? FullName { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
    UserType? UserType { get; }
}
