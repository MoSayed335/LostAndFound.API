using LostAndFound.Application.Common.Models;
using LostAndFound.Application.Features.Claims.DTOs;
using MediatR;

namespace LostAndFound.Application.Features.Claims.Queries.GetItemClaims;

public record GetItemClaimsQuery(
    int ItemId,
    int CurrentUserId,
    bool IsAdmin
) : IRequest<Result<IReadOnlyList<ItemClaimResponseDto>>>;
