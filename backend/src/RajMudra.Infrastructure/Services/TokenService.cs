using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RajMudra.Application.Abstractions.Services;
using RajMudra.Application.Common.Exceptions;
using RajMudra.Application.DTOs;
using RajMudra.Domain.Entities;
using RajMudra.Infrastructure.Persistence;

namespace RajMudra.Infrastructure.Services;

public sealed class TokenService : ITokenService
{
    private readonly RajMudraDbContext _db;
    private readonly TimeProvider _timeProvider;

    public TokenService(RajMudraDbContext db, TimeProvider? timeProvider = null)
    {
        _db = db;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<decimal> GetWalletBalanceAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await _db.Tokens
            .Where(t => t.OwnerId == ownerId && !t.IsSpent)
            .SumAsync(t => t.Denomination, cancellationToken);
    }

    public async Task<IReadOnlyList<TokenDto>> GetActiveTokensAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await _db.Tokens
            .AsNoTracking()
            .Where(t => t.OwnerId == ownerId && !t.IsSpent)
            .OrderBy(t => t.CreatedAt)
            .Select(t => new TokenDto(t.Id, t.OwnerId, t.Denomination, t.CreatedAt, t.Purpose))
            .ToListAsync(cancellationToken);
    }

    public async Task<TokenDto> MintAsync(
        Guid ownerId,
        decimal denomination,
        string? purpose,
        CancellationToken cancellationToken = default)
    {
        if (ownerId == Guid.Empty) throw new ValidationException("OwnerId is required.");
        if (denomination <= 0) throw new ValidationException("Denomination must be > 0.");

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var token = new Token
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Denomination = denomination,
            CreatedAt = now,
            IsSpent = false,
            Purpose = string.IsNullOrWhiteSpace(purpose) ? null : purpose.Trim()
        };

        _db.Tokens.Add(token);
        await _db.SaveChangesAsync(cancellationToken);

        return new TokenDto(token.Id, token.OwnerId, token.Denomination, token.CreatedAt, token.Purpose);
    }

    public async Task<TokenTransferResultDto> TransferAsync(
        Guid senderId,
        Guid tokenId,
        Guid recipientId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        if (senderId == Guid.Empty) throw new ValidationException("SenderId is required.");
        if (recipientId == Guid.Empty) throw new ValidationException("RecipientId is required.");
        if (tokenId == Guid.Empty) throw new ValidationException("TokenId is required.");
        if (senderId == recipientId) throw new ValidationException("Sender and recipient cannot be the same.");
        if (amount <= 0) throw new ValidationException("Amount must be > 0.");

        await using IDbContextTransaction tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        try
        {
            var token = await _db.Tokens
                .SingleOrDefaultAsync(t => t.Id == tokenId, cancellationToken);

            if (token is null)
            {
                throw new NotFoundException("Token not found.");
            }

            if (token.OwnerId != senderId)
            {
                throw new ForbiddenException("Token does not belong to sender.");
            }

            if (token.IsSpent)
            {
                throw new ValidationException("Token is already spent.");
            }

            if (amount > token.Denomination)
            {
                throw new ValidationException("Amount exceeds token value.");
            }

            // Purpose-based validation: if token has Purpose, ensure recipient merchant category matches.
            if (!string.IsNullOrWhiteSpace(token.Purpose))
            {
                var recipient = await _db.Users
                    .AsNoTracking()
                    .SingleOrDefaultAsync(u => u.Id == recipientId, cancellationToken);

                if (recipient is null)
                {
                    throw new NotFoundException("Recipient user not found.");
                }

                if (!string.Equals(recipient.MerchantCategory, token.Purpose, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ValidationException("Recipient merchant category does not match token purpose.");
                }
            }

            token.IsSpent = true;

            var now = _timeProvider.GetUtcNow().UtcDateTime;

            var recipientToken = new Token
            {
                Id = Guid.NewGuid(),
                OwnerId = recipientId,
                Denomination = amount,
                CreatedAt = now,
                IsSpent = false,
                Purpose = token.Purpose
            };
            _db.Tokens.Add(recipientToken);

            Token? changeToken = null;
            var changeAmount = token.Denomination - amount;
            if (changeAmount > 0)
            {
                changeToken = new Token
                {
                    Id = Guid.NewGuid(),
                    OwnerId = senderId,
                    Denomination = changeAmount,
                    CreatedAt = now,
                    IsSpent = false,
                    Purpose = token.Purpose
                };
                _db.Tokens.Add(changeToken);
            }

            _db.TransactionHistory.Add(new TransactionHistory
            {
                Id = Guid.NewGuid(),
                Type = TransactionType.Transfer,
                FromUserId = senderId,
                ToUserId = recipientId,
                TokenId = token.Id,
                Amount = amount,
                Purpose = token.Purpose,
                CreatedAt = now,
                Description = changeAmount > 0 ? "Transfer with change" : "Full-value transfer"
            });

            await _db.SaveChangesAsync(cancellationToken);

            await tx.CommitAsync(cancellationToken);

            return new TokenTransferResultDto(
                SpentTokenId: token.Id,
                RecipientTokenId: recipientToken.Id,
                SenderId: senderId,
                RecipientId: recipientId,
                Denomination: amount,
                Purpose: token.Purpose,
                OccurredAtUtc: now);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<TokenTransferResultDto> RedeemAsync(
        Guid merchantId,
        Guid tokenId,
        CancellationToken cancellationToken = default)
    {
        if (merchantId == Guid.Empty) throw new ValidationException("MerchantId is required.");
        if (tokenId == Guid.Empty) throw new ValidationException("TokenId is required.");

        await using IDbContextTransaction tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        try
        {
            var merchant = await _db.Users
                .SingleOrDefaultAsync(u => u.Id == merchantId, cancellationToken);

            if (merchant is null)
            {
                throw new NotFoundException("Merchant not found.");
            }

            var token = await _db.Tokens
                .SingleOrDefaultAsync(t => t.Id == tokenId, cancellationToken);

            if (token is null)
            {
                throw new NotFoundException("Token not found.");
            }

            if (token.IsSpent)
            {
                throw new ValidationException("Token is already spent.");
            }

            // Purpose-based validation for merchant redemption.
            if (!string.IsNullOrWhiteSpace(token.Purpose))
            {
                if (!string.Equals(merchant.MerchantCategory, token.Purpose, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ValidationException("Merchant category does not match token purpose.");
                }
            }

            token.IsSpent = true;

            var now = _timeProvider.GetUtcNow().UtcDateTime;

            _db.TransactionHistory.Add(new TransactionHistory
            {
                Id = Guid.NewGuid(),
                Type = TransactionType.Redemption,
                FromUserId = token.OwnerId,
                ToUserId = merchantId,
                TokenId = token.Id,
                Amount = token.Denomination,
                Purpose = token.Purpose,
                CreatedAt = now,
                Description = "Merchant redemption"
            });

            await _db.SaveChangesAsync(cancellationToken);

            await tx.CommitAsync(cancellationToken);

            return new TokenTransferResultDto(
                SpentTokenId: token.Id,
                RecipientTokenId: token.Id,
                SenderId: token.OwnerId,
                RecipientId: merchantId,
                Denomination: token.Denomination,
                Purpose: token.Purpose,
                OccurredAtUtc: now);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

