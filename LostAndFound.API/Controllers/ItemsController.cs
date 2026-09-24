using LostAndFound.Application.Common.Models;
using LostAndFound.Application.Features.Items.Commands.CreateFoundItem;
using LostAndFound.Application.Features.Items.Commands.CreateLostItem;
using LostAndFound.Application.Features.Items.Commands.DeleteItem;
using LostAndFound.Application.Features.Items.Commands.ReturnItem;
using LostAndFound.Application.Features.Items.Commands.UpdateItem;
using LostAndFound.Application.Features.Items.DTOs;
using LostAndFound.Application.Features.Items.Queries.GetItemById;
using LostAndFound.Application.Features.Items.Queries.GetItems;
using LostAndFound.Application.Features.Items.Queries.GetMyItems;
using LostAndFound.Application.Features.Matching.DTOs;
using LostAndFound.Application.Features.Matching.Queries.GetItemMatches;
using LostAndFound.Application.Interfaces;
using LostAndFound.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.API.Controllers;


[ApiController]
[Route("api/items")]
public class ItemsController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public ItemsController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Reports a lost item with optional image file upload via multipart/form-data.
    /// </summary>
    [Authorize]
    [HttpPost("lost")]
    [Consumes("multipart/form-data")]
    [ActionName("CreateLostItemWithImage")]
    [ProducesResponseType(typeof(ItemResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateLostItemForm([FromForm] CreateItemFormRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(Envelope("User is not authenticated."));
        }

        var command = new CreateLostItemCommand(
            request.Title,
            request.Description,
            request.CategoryId,
            request.Location,
            request.DateLostOrFound,
            null,
            _currentUserService.UserId.Value,
            request.Image);

        var result = await _mediator.Send(command, cancellationToken);

        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : MapError(result);
    }

    /// <summary>
    /// Reports a lost item via application/json.
    /// </summary>
    [Authorize]
    [HttpPost("lost")]
    [Consumes("application/json")]
    [ActionName("CreateLostItem")]
    [ProducesResponseType(typeof(ItemResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateLostItem([FromBody] CreateItemRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(Envelope("User is not authenticated."));
        }

        var command = new CreateLostItemCommand(
            request.Title,
            request.Description,
            request.CategoryId,
            request.Location,
            request.DateLostOrFound,
            request.ImageUrl,
            _currentUserService.UserId.Value);

        var result = await _mediator.Send(command, cancellationToken);

        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : MapError(result);
    }

    /// <summary>
    /// Reports a found item with optional image file upload via multipart/form-data.
    /// </summary>
    [Authorize]
    [HttpPost("found")]
    [Consumes("multipart/form-data")]
    [ActionName("CreateFoundItemWithImage")]
    [ProducesResponseType(typeof(ItemResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateFoundItemForm([FromForm] CreateItemFormRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(Envelope("User is not authenticated."));
        }

        var command = new CreateFoundItemCommand(
            request.Title,
            request.Description,
            request.CategoryId,
            request.Location,
            request.DateLostOrFound,
            null,
            _currentUserService.UserId.Value,
            request.Image);

        var result = await _mediator.Send(command, cancellationToken);

        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : MapError(result);
    }

    /// <summary>
    /// Reports a found item via application/json.
    /// </summary>
    [Authorize]
    [HttpPost("found")]
    [Consumes("application/json")]
    [ActionName("CreateFoundItem")]
    [ProducesResponseType(typeof(ItemResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateFoundItem([FromBody] CreateItemRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(Envelope("User is not authenticated."));
        }

        var command = new CreateFoundItemCommand(
            request.Title,
            request.Description,
            request.CategoryId,
            request.Location,
            request.DateLostOrFound,
            request.ImageUrl,
            _currentUserService.UserId.Value);

        var result = await _mediator.Send(command, cancellationToken);

        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : MapError(result);
    }

    /// <summary>
    /// Browses and filters items with SQL-level IQueryable pagination.
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<ItemResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        [FromQuery] ItemType? type,
        [FromQuery] int? categoryId,
        [FromQuery] ItemStatus? status,
        [FromQuery] string? location,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? search,
        [FromQuery(Name = "page")] int? page,
        [FromQuery(Name = "pageNumber")] int? pageNumber,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var resolvedPage = pageNumber ?? page ?? 1;
        var query = new GetItemsQuery(type, categoryId, status, location, fromDate, toDate, search, resolvedPage, pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Retrieves a specific item by its identifier.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ItemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var query = new GetItemByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Retrieves items reported by the currently authenticated user.
    /// </summary>
    [Authorize]
    [HttpGet("my")]
    [ProducesResponseType(typeof(PaginatedList<ItemResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyItems(
        [FromQuery(Name = "page")] int? page,
        [FromQuery(Name = "pageNumber")] int? pageNumber,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(Envelope("User is not authenticated."));
        }

        var resolvedPage = pageNumber ?? page ?? 1;
        var query = new GetMyItemsQuery(_currentUserService.UserId.Value, resolvedPage, pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Updates an item with optional new image file via multipart/form-data. Only the item owner or an Admin can perform this action.
    /// </summary>
    [Authorize]
    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    [ActionName("UpdateWithImage")]
    [ProducesResponseType(typeof(ItemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateForm(int id, [FromForm] UpdateItemFormRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(Envelope("User is not authenticated."));
        }

        var command = new UpdateItemCommand(
            id,
            request.Title,
            request.Description,
            request.CategoryId,
            request.Location,
            request.DateLostOrFound,
            null,
            request.Status,
            _currentUserService.UserId.Value,
            _currentUserService.IsAdmin,
            request.Image);

        var result = await _mediator.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(result.Value)
            : MapError(result);
    }

    /// <summary>
    /// Updates an item via application/json. Only the item owner or an Admin can perform this action.
    /// </summary>
    [Authorize]
    [HttpPut("{id:int}")]
    [Consumes("application/json")]
    [ActionName("Update")]
    [ProducesResponseType(typeof(ItemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateItemRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(Envelope("User is not authenticated."));
        }

        var command = new UpdateItemCommand(
            id,
            request.Title,
            request.Description,
            request.CategoryId,
            request.Location,
            request.DateLostOrFound,
            request.ImageUrl,
            request.Status,
            _currentUserService.UserId.Value,
            _currentUserService.IsAdmin);

        var result = await _mediator.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(result.Value)
            : MapError(result);
    }

    /// <summary>
    /// Deletes an item. Only the item owner or an Admin can perform this action.
    /// </summary>
    [Authorize]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(Envelope("User is not authenticated."));
        }

        var command = new DeleteItemCommand(id, _currentUserService.UserId.Value, _currentUserService.IsAdmin);
        var result = await _mediator.Send(command, cancellationToken);

        return result.Succeeded
            ? NoContent()
            : MapError(result);
    }

    /// <summary>
    /// Marks a claimed item as returned. Only the item owner or an Admin can perform this.
    /// </summary>
    [Authorize]
    [HttpPost("{itemId:int}/return")]
    [ProducesResponseType(typeof(ItemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReturnItem(int itemId, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(Envelope("User is not authenticated."));
        }

        var command = new ReturnItemCommand(itemId, _currentUserService.UserId.Value, _currentUserService.IsAdmin);
        var result = await _mediator.Send(command, cancellationToken);

        return result.Succeeded
            ? Ok(result.Value)
            : MapError(result);
    }

    /// <summary>
    /// Finds potentially matching opposite-type active items based on deterministic scoring.
    /// Only accessible by the item owner or an Admin.
    /// </summary>
    [Authorize]
    [HttpGet("{itemId:int}/matches")]
    [ProducesResponseType(typeof(IReadOnlyList<MatchResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMatches(int itemId, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(Envelope("User is not authenticated."));
        }

        var query = new GetItemMatchesQuery(itemId, _currentUserService.UserId.Value, _currentUserService.IsAdmin);
        var result = await _mediator.Send(query, cancellationToken);

        return HandleResult(result);
    }
}
