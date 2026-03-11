using RajMudra.Application.DTOs;

namespace RajMudra.Application.Abstractions.Services;

public interface IAdminUserService
{
    Task<IReadOnlyList<AdminUserDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<AdminUserDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AdminUserDto> UpdateAsync(Guid id, AdminUpdateUserRequestDto request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(Guid id, AdminResetPasswordRequestDto request, CancellationToken cancellationToken = default);
}

