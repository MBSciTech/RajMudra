using RajMudra.Application.DTOs;

namespace RajMudra.Application.Abstractions.Services;

public interface ITokenService
{
    Task<decimal> GetWalletBalanceAsync(Guid ownerId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TokenDto>> GetActiveTokensAsync(Guid ownerId, CancellationToken cancellationToken = default);

    Task<TokenDto> MintAsync(
        Guid ownerId,
        decimal denomination,
        string? purpose,
        CancellationToken cancellationToken = default);

    Task<TokenTransferResultDto> TransferAsync(
        Guid senderId,
        Guid tokenId,
        Guid recipientId,
        decimal amount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Merchant redemption of a voucher/token by scanning its id.
    /// </summary>
    Task<TokenTransferResultDto> RedeemAsync(
        Guid merchantId,
        Guid tokenId,
        CancellationToken cancellationToken = default);
}

