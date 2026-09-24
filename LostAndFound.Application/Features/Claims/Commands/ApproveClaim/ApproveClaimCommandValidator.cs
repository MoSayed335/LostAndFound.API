using FluentValidation;

namespace LostAndFound.Application.Features.Claims.Commands.ApproveClaim;

public class ApproveClaimCommandValidator : AbstractValidator<ApproveClaimCommand>
{
    public ApproveClaimCommandValidator()
    {
        RuleFor(x => x.ClaimId)
            .GreaterThan(0).WithMessage("Invalid claim ID.");
    }
}
