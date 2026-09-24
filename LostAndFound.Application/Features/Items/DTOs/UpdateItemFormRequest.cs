using LostAndFound.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace LostAndFound.Application.Features.Items.DTOs;

public class UpdateItemFormRequest
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public string Location { get; set; } = string.Empty;

    public DateTime DateLostOrFound { get; set; }

    public ItemStatus? Status { get; set; }

    public IFormFile? Image { get; set; }
}
