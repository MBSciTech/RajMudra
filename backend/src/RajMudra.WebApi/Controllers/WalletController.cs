using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RajMudra.Application.Abstractions.Services;
using RajMudra.Application.DTOs;

namespace RajMudra.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class WalletController : ControllerBase
{
    private readonly ITokenService _tokenService;

    public WalletController(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                  User.FindFirstValue("sub");
        return Guid.Parse(sub!);
    }

    [HttpGet("balance")]
    public async Task<ActionResult<object>> GetBalance(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var balance = await _tokenService.GetWalletBalanceAsync(userId, cancellationToken);
        return Ok(new { balance });
    }

    [HttpGet("tokens")]
    public async Task<ActionResult<IReadOnlyList<TokenDto>>> GetTokens(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var tokens = await _tokenService.GetActiveTokensAsync(userId, cancellationToken);
        return Ok(tokens);
    }
}

