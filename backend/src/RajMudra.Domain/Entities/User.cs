namespace RajMudra.Domain.Entities;

public sealed class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = default!;

    public string PasswordHash { get; set; } = default!;

    /// <summary>
    /// Role name, e.g. "Admin", "Merchant", "User".
    /// </summary>
    public string Role { get; set; } = "User";

    /// <summary>
    /// Optional merchant category for purpose-restricted vouchers, e.g. "Food".
    /// </summary>
    public string? MerchantCategory { get; set; }
}

