using FluentValidation;

namespace LostAndFound.Application.Features.Claims.Queries.GetItemClaims;

public class GetItemClaimsQueryValidator : AbstractValidator<GetItemClaimsQuery>
{
    public GetItemClaimsQueryValidator()
    {
        RuleFor(x => x.ItemId)
            .GreaterThan(0).WithMessage("Invalid item ID.");
    }
}
