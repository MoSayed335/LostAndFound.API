using FluentValidation;

namespace LostAndFound.Application.Features.Items.Commands.ReturnItem;

public class ReturnItemCommandValidator : AbstractValidator<ReturnItemCommand>
{
    public ReturnItemCommandValidator()
    {
        RuleFor(x => x.ItemId)
            .GreaterThan(0).WithMessage("Invalid item ID.");
    }
}
