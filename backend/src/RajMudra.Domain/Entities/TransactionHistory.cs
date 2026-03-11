namespace RajMudra.Domain.Entities;

public enum TransactionType
{
    Mint = 1,
    Transfer = 2,
    Redemption = 3
}

public sealed class TransactionHistory
{
    public Guid Id { get; set; }

    public TransactionType Type { get; set; }

    public Guid? FromUserId { get; set; }

    public Guid? ToUserId { get; set; }

    public Guid? TokenId { get; set; }

    public decimal Amount { get; set; }

    public string? Purpose { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Arbitrary JSON or string describing the intent (e.g. "wallet transfer", "merchant redemption").
    /// </summary>
    public string? Description { get; set; }
}

