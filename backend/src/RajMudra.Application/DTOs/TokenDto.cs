namespace RajMudra.Application.DTOs;

public sealed record TokenDto(
    Guid Id,
    Guid OwnerId,
    decimal Denomination,
    DateTime CreatedAt,
    string? Purpose);

