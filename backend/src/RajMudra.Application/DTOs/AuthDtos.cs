namespace RajMudra.Application.DTOs;

public sealed record RegisterRequestDto(
    string Email,
    string Password,
    string Role,
    string? MerchantCategory);

public sealed record LoginRequestDto(
    string Email,
    string Password);

public sealed record AuthResultDto(
    Guid UserId,
    string Email,
    string Role,
    string? MerchantCategory,
    string Token);

