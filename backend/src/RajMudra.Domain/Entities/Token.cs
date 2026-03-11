namespace RajMudra.Domain.Entities;

public sealed class Token
{
    public Guid Id { get; set; }

    public Guid OwnerId { get; set; }

    public decimal Denomination { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsSpent { get; set; }

    /// <summary>
    /// Optional e-RUPI-style restriction/purpose (e.g., "Education").
    /// </summary>
    public string? Purpose { get; set; }
}

