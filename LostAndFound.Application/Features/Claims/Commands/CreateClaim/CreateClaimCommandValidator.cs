using FluentValidation;

namespace LostAndFound.Application.Features.Claims.Commands.CreateClaim;

public class CreateClaimCommandValidator : AbstractValidator<CreateClaimCommand>
{
    public CreateClaimCommandValidator()
    {
        RuleFor(x => x.ItemId)
            .GreaterThan(0).WithMessage("Invalid item ID.");

        RuleFor(x => x.ClaimantId)
            .GreaterThan(0).WithMessage("Invalid claimant ID.");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required.")
            .MinimumLength(5).WithMessage("Message must be at least 5 characters.")
            .MaximumLength(1000).WithMessage("Message cannot exceed 1000 characters.");
    }
}
