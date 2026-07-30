namespace HomeServices.Shared.Enums;

/// <summary>
/// The role a user plays in the home-services marketplace.
/// Stored on the Identity service and surfaced via JWT claims.
/// </summary>
public enum UserType
{
    /// <summary>Customer who requests home services.</summary>
    Customer = 1,

    /// <summary>Service professional/expert who submits proposals.</summary>
    Expert = 2,

    /// <summary>Platform administrator.</summary>
    Admin = 3
}
