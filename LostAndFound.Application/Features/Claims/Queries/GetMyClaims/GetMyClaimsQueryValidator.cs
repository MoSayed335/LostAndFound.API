using FluentValidation;

namespace LostAndFound.Application.Features.Claims.Queries.GetMyClaims;

public class GetMyClaimsQueryValidator : AbstractValidator<GetMyClaimsQuery>
{
    public GetMyClaimsQueryValidator()
    {
        RuleFor(x => x.ClaimantId)
            .GreaterThan(0).WithMessage("Invalid claimant ID.");
    }
}
