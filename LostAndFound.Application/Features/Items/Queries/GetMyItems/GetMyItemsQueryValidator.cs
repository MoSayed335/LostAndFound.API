using FluentValidation;

namespace LostAndFound.Application.Features.Items.Queries.GetMyItems;

public class GetMyItemsQueryValidator : AbstractValidator<GetMyItemsQuery>
{
    public GetMyItemsQueryValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("Invalid user ID.");

        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("PageNumber must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50).WithMessage("PageSize must be between 1 and 50.");
    }
}
