using LostAndFound.Application.Common.Models;
using LostAndFound.Application.Features.Claims.DTOs;
using MediatR;

namespace LostAndFound.Application.Features.Claims.Queries.GetMyClaims;

public record GetMyClaimsQuery(int ClaimantId) : IRequest<Result<IReadOnlyList<MyClaimResponseDto>>>;
