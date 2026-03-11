namespace RajMudra.Application.DTOs;

public sealed record AdminUserDto(
    Guid Id,
    string Email,
    string Role,
    string? MerchantCategory);

public sealed record AdminUpdateUserRequestDto(
    string Email,
    string Role,
    string? MerchantCategory);

public sealed record AdminResetPasswordRequestDto(
    string NewPassword);

