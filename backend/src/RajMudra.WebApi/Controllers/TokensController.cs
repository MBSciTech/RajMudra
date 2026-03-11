using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RajMudra.Application.Abstractions.Services;
using RajMudra.Application.DTOs;

namespace RajMudra.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class TokensController : ControllerBase
{
    private readonly ITokenService _tokenService;

    public TokensController(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                  User.FindFirstValue("sub");
        return Guid.Parse(sub!);
    }

    public sealed record TransferRequest(Guid TokenId, Guid RecipientId, decimal Amount);

    public sealed record RedeemRequest(Guid TokenId);

    [HttpPost("transfer")]
    public async Task<ActionResult<TokenTransferResultDto>> Transfer(
        [FromBody] TransferRequest request,
        CancellationToken cancellationToken)
    {
        var senderId = GetUserId();
        var result = await _tokenService.TransferAsync(
            senderId,
            request.TokenId,
            request.RecipientId,
            request.Amount,
            cancellationToken);
        return Ok(result);
    }

    [HttpPost("redeem")]
    [Authorize(Roles = "Merchant")]
    public async Task<ActionResult<TokenTransferResultDto>> Redeem(
        [FromBody] RedeemRequest request,
        CancellationToken cancellationToken)
    {
        var merchantId = GetUserId();
        var result = await _tokenService.RedeemAsync(merchantId, request.TokenId, cancellationToken);
        return Ok(result);
    }
}

