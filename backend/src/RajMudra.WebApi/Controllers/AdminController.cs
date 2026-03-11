using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RajMudra.Application.Abstractions.Services;
using RajMudra.Application.DTOs;

namespace RajMudra.WebApi.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly ITokenService _tokenService;
    private readonly IAdminUserService _adminUserService;

    public AdminController(ITokenService tokenService, IAdminUserService adminUserService)
    {
        _tokenService = tokenService;
        _adminUserService = adminUserService;
    }

    public sealed record MintRequest(Guid UserId, decimal Denomination, string? Purpose);

    [HttpPost("mint")]
    public async Task<ActionResult<TokenDto>> Mint([FromBody] MintRequest request, CancellationToken cancellationToken)
    {
        var token = await _tokenService.MintAsync(
            request.UserId,
            request.Denomination,
            request.Purpose,
            cancellationToken);
        return Ok(token);
    }

    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<AdminUserDto>>> GetUsers(CancellationToken cancellationToken)
    {
        var users = await _adminUserService.GetAllAsync(cancellationToken);
        return Ok(users);
    }

    [HttpGet("users/{id:guid}")]
    public async Task<ActionResult<AdminUserDto>> GetUser(Guid id, CancellationToken cancellationToken)
    {
        var user = await _adminUserService.GetByIdAsync(id, cancellationToken);
        return Ok(user);
    }

    [HttpPut("users/{id:guid}")]
    public async Task<ActionResult<AdminUserDto>> UpdateUser(
        Guid id,
        [FromBody] AdminUpdateUserRequestDto request,
        CancellationToken cancellationToken)
    {
        var updated = await _adminUserService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("users/{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        await _adminUserService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("users/{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(
        Guid id,
        [FromBody] AdminResetPasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        await _adminUserService.ResetPasswordAsync(id, request, cancellationToken);
        return NoContent();
    }
}

