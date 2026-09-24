using LostAndFound.Application.Common.Models;
using LostAndFound.Application.Features.Claims.DTOs;
using MediatR;

namespace LostAndFound.Application.Features.Claims.Commands.ApproveClaim;

public record ApproveClaimCommand(
    int ClaimId,
    int CurrentUserId,
    bool IsAdmin
) : IRequest<Result<ClaimResponseDto>>;
