using LostAndFound.Application.Common.Models;
using LostAndFound.Application.Features.Claims.DTOs;
using MediatR;

namespace LostAndFound.Application.Features.Claims.Commands.RejectClaim;

public record RejectClaimCommand(
    int ClaimId,
    int CurrentUserId,
    bool IsAdmin
) : IRequest<Result<ClaimResponseDto>>;
