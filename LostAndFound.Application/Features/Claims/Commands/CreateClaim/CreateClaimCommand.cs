using LostAndFound.Application.Common.Models;
using LostAndFound.Application.Features.Claims.DTOs;
using MediatR;

namespace LostAndFound.Application.Features.Claims.Commands.CreateClaim;

public record CreateClaimCommand(
    int ItemId,
    string Message,
    int ClaimantId
) : IRequest<Result<ClaimResponseDto>>;
