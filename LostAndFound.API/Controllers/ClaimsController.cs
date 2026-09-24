using LostAndFound.Application.Features.Claims.Commands.ApproveClaim;
using LostAndFound.Application.Features.Claims.Commands.CreateClaim;
using LostAndFound.Application.Features.Claims.Commands.RejectClaim;
using LostAndFound.Application.Features.Claims.DTOs;
using LostAndFound.Application.Features.Claims.Queries.GetItemClaims;
using LostAndFound.Application.Features.Claims.Queries.GetMyClaims;
using LostAndFound.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.API.Controllers;

public class ClaimsController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public ClaimsController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Submits a new claim for an item. Claimant is automatically set to the authenticated caller.
    /// </summary>
    [Authorize]
    [HttpPost("api/items/{itemId:int}/claims")]
    [ProducesResponseType(typeof(ClaimResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateClaim(int itemId, [FromBody] CreateClaimRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(Envelope("User is not authenticated."));
        }

        var command = new CreateClaimCommand(itemId, request.Message, _currentUserService.UserId.Value);
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result, StatusCodes.Status201Created);
    }

    /// <summary>
    /// Retrieves claims submitted by the currently authenticated user.
    /// </summary>
    [Authorize]
    [HttpGet("api/claims/my")]
    [ProducesResponseType(typeof(IReadOnlyList<MyClaimResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyClaims(CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(Envelope("User is not authenticated."));
        }

        var query = new GetMyClaimsQuery(_currentUserService.UserId.Value);
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Retrieves all claims for an item. Only the item owner or an Admin can access this.
    /// </summary>
    [Authorize]
    [HttpGet("api/items/{itemId:int}/claims")]
    [ProducesResponseType(typeof(IReadOnlyList<ItemClaimResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetItemClaims(int itemId, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(Envelope("User is not authenticated."));
        }

        var query = new GetItemClaimsQuery(itemId, _currentUserService.UserId.Value, _currentUserService.IsAdmin);
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Approves a pending claim. Atomically marks the item as Claimed and rejects other pending claims.
    /// </summary>
    [Authorize]
    [HttpPost("api/claims/{claimId:int}/approve")]
    [ProducesResponseType(typeof(ClaimResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveClaim(int claimId, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(Envelope("User is not authenticated."));
        }

        var command = new ApproveClaimCommand(claimId, _currentUserService.UserId.Value, _currentUserService.IsAdmin);
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Rejects a pending claim. The item remains Active.
    /// </summary>
    [Authorize]
    [HttpPost("api/claims/{claimId:int}/reject")]
    [ProducesResponseType(typeof(ClaimResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectClaim(int claimId, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(Envelope("User is not authenticated."));
        }

        var command = new RejectClaimCommand(claimId, _currentUserService.UserId.Value, _currentUserService.IsAdmin);
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}
