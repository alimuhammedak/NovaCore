namespace NovaCore.MultiTenancy;

/// <summary>Where a tenant is in its life cycle. Only an active tenant is resolved for a request.</summary>
public enum TenantStatus
{
    /// <summary>Its members work in it.</summary>
    Active = 0,

    /// <summary>Temporarily out of reach: no member works in it until it is reactivated.</summary>
    Suspended = 1
}
