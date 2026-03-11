namespace RajMudra.Application.DTOs;

public sealed record TokenTransferResultDto(
    Guid SpentTokenId,
    Guid RecipientTokenId,
    Guid SenderId,
    Guid RecipientId,
    decimal Denomination,
    string? Purpose,
    DateTime OccurredAtUtc);

