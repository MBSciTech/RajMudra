using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using RajMudra.Application.Abstractions.Services;
using RajMudra.Application.Common.Exceptions;
using RajMudra.Application.DTOs;
using RajMudra.Domain.Entities;
using RajMudra.Infrastructure.Persistence;

namespace RajMudra.Infrastructure.Services;

public sealed class AdminUserService : IAdminUserService
{
    private readonly RajMudraDbContext _db;

    public AdminUserService(RajMudraDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AdminUserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _db.Users
            .AsNoTracking()
            .OrderBy(u => u.Email)
            .Select(u => new AdminUserDto(u.Id, u.Email, u.Role, u.MerchantCategory))
            .ToListAsync(cancellationToken);

        return users;
    }

    public async Task<AdminUserDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null) throw new NotFoundException("User not found.");
        return new AdminUserDto(user.Id, user.Email, user.Role, user.MerchantCategory);
    }

    public async Task<AdminUserDto> UpdateAsync(Guid id, AdminUpdateUserRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email)) throw new ValidationException("Email is required.");
        if (string.IsNullOrWhiteSpace(request.Role)) throw new ValidationException("Role is required.");

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null) throw new NotFoundException("User not found.");

        var emailInUse = await _db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id != id && u.Email == normalizedEmail, cancellationToken);
        if (emailInUse) throw new ValidationException("Email already in use.");

        user.Email = normalizedEmail;
        user.Role = request.Role.Trim();
        user.MerchantCategory = string.IsNullOrWhiteSpace(request.MerchantCategory)
            ? null
            : request.MerchantCategory.Trim();

        await _db.SaveChangesAsync(cancellationToken);

        return new AdminUserDto(user.Id, user.Email, user.Role, user.MerchantCategory);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null) return;

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetPasswordAsync(Guid id, AdminResetPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword))
            throw new ValidationException("NewPassword is required.");
        if (request.NewPassword.Trim().Length < 6)
            throw new ValidationException("Password must be at least 6 characters.");

        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user is null) throw new NotFoundException("User not found.");

        user.PasswordHash = HashPasswordForStorage(request.NewPassword.Trim());
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string HashPasswordForStorage(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        using var deriveBytes = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        var hash = deriveBytes.GetBytes(32);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }
}

