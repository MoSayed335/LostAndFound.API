using FluentValidation;

namespace LostAndFound.Application.Features.Matching.Queries.GetItemMatches;

public class GetItemMatchesQueryValidator : AbstractValidator<GetItemMatchesQuery>
{
    public GetItemMatchesQueryValidator()
    {
        RuleFor(x => x.ItemId)
            .GreaterThan(0).WithMessage("ItemId must be greater than 0.");

        RuleFor(x => x.CurrentUserId)
            .GreaterThan(0).WithMessage("CurrentUserId must be greater than 0.");
    }
}
