using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RajMudra.Application.Abstractions.Services;
using RajMudra.Application.Common.Exceptions;
using RajMudra.Application.DTOs;
using RajMudra.Domain.Entities;
using RajMudra.Infrastructure.Persistence;

namespace RajMudra.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private readonly RajMudraDbContext _db;
    private readonly IConfiguration _configuration;

    public AuthService(RajMudraDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task<AuthResultDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ValidationException("Email is required.");
        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ValidationException("Password is required.");
        if (string.IsNullOrWhiteSpace(request.Role))
            throw new ValidationException("Role is required.");

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var existing = await _db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
        if (existing is not null)
        {
            throw new ValidationException("Email already registered.");
        }

        var (hash, salt) = HashPassword(request.Password);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordHash = $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}",
            Role = request.Role.Trim(),
            MerchantCategory = string.IsNullOrWhiteSpace(request.MerchantCategory)
                ? null
                : request.MerchantCategory.Trim()
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        var token = GenerateJwt(user);

        return new AuthResultDto(user.Id, user.Email, user.Role, user.MerchantCategory, token);
    }

    public async Task<AuthResultDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Users
            .SingleOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("Invalid credentials.");
        }

        if (!VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new ValidationException("Invalid credentials.");
        }

        var token = GenerateJwt(user);

        return new AuthResultDto(user.Id, user.Email, user.Role, user.MerchantCategory, token);
    }

    private (byte[] hash, byte[] salt) HashPassword(string password)
    {
        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[16];
        rng.GetBytes(salt);

        using var deriveBytes = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        var hash = deriveBytes.GetBytes(32);
        return (hash, salt);
    }

    private bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split('.', 2);
        if (parts.Length != 2) return false;

        var salt = Convert.FromBase64String(parts[0]);
        var hash = Convert.FromBase64String(parts[1]);

        using var deriveBytes = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        var computed = deriveBytes.GetBytes(32);

        return CryptographicOperations.FixedTimeEquals(hash, computed);
    }

    private string GenerateJwt(User user)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var issuer = jwtSection["Issuer"] ?? "RajMudra";
        var audience = jwtSection["Audience"] ?? "RajMudraClient";
        var secret = jwtSection["Secret"] ?? throw new InvalidOperationException("Missing JWT Secret.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };

        if (!string.IsNullOrWhiteSpace(user.MerchantCategory))
        {
            claims.Add(new Claim("merchant_category", user.MerchantCategory));
        }

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddHours(4),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

