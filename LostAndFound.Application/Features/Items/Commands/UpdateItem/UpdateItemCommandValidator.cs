using FluentValidation;
using LostAndFound.Application.Interfaces;

namespace LostAndFound.Application.Features.Items.Commands.UpdateItem;

public class UpdateItemCommandValidator : AbstractValidator<UpdateItemCommand>
{
    public UpdateItemCommandValidator(ICategoryRepository categoryRepository)
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Invalid item ID.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MinimumLength(2).WithMessage("Title must be at least 2 characters.")
            .MaximumLength(150).WithMessage("Title cannot exceed 150 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters.");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("CategoryId must be greater than 0.")
            .MustAsync(async (catId, ct) => await categoryRepository.ExistsAsync(catId, ct))
            .WithMessage("The specified category does not exist.");

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("Location is required.")
            .MinimumLength(2).WithMessage("Location must be at least 2 characters.")
            .MaximumLength(200).WithMessage("Location cannot exceed 200 characters.");

        RuleFor(x => x.DateLostOrFound)
            .NotEmpty().WithMessage("Date is required.")
            .Must(d => (d.Kind == DateTimeKind.Utc ? d : d.ToUniversalTime()) <= DateTime.UtcNow.AddMinutes(5))
            .WithMessage("Date cannot be in the future.")
            .Must(d => (d.Kind == DateTimeKind.Utc ? d : d.ToUniversalTime()) >= DateTime.UtcNow.AddYears(-10))
            .WithMessage("Date cannot be older than 10 years.");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(500).WithMessage("ImageUrl cannot exceed 500 characters.")
            .Must(url => string.IsNullOrEmpty(url) || Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out _))
            .WithMessage("ImageUrl must be a valid URL or path if provided.");

        RuleFor(x => x.Status)
            .IsInEnum().When(x => x.Status.HasValue)
            .WithMessage("Invalid status value.");
    }
}
