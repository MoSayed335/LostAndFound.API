using FluentValidation;

namespace LostAndFound.Application.Features.Claims.Commands.RejectClaim;

public class RejectClaimCommandValidator : AbstractValidator<RejectClaimCommand>
{
    public RejectClaimCommandValidator()
    {
        RuleFor(x => x.ClaimId)
            .GreaterThan(0).WithMessage("Invalid claim ID.");
    }
}
