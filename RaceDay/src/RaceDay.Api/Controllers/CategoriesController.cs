using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/events/{eventId:int}/categories")]
public class CategoriesController : ApiControllerBase
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db) => _db = db;

    /// <summary>List the categories of an event (both roles).</summary>
    /// <response code="200">Categories for the event.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAll(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.Id == eventId)) return NotFound();
        var cats = await _db.Categories.AsNoTracking().Where(c => c.EventId == eventId).ToListAsync();
        return Ok(cats.Select(c => new CategoryResponse(c.Id, c.EventId, c.Name)));
    }

    /// <summary>Add an age or distance category to one of your events (Organiser only).</summary>
    /// <remarks>Examples: "Under 20", "Senior", "10km", "21km".</remarks>
    /// <response code="201">Category created.</response>
    /// <response code="403">Caller does not own the event.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="409">Category name already exists for this event.</response>
    [HttpPost]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(int eventId, CategoryRequest req)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return Forbid();

        var name = req.Name.Trim();
        if (await _db.Categories.AnyAsync(c => c.EventId == eventId && c.Name == name))
            return Conflict(new { error = "Category already exists for this event." });

        var cat = new Category { EventId = eventId, Name = name };
        _db.Categories.Add(cat);
        await _db.SaveChangesAsync();
        return StatusCode(StatusCodes.Status201Created, new CategoryResponse(cat.Id, cat.EventId, cat.Name));
    }

    /// <summary>Remove a category from one of your events (Organiser only).</summary>
    /// <response code="204">Category deleted.</response>
    /// <response code="403">Caller does not own the event.</response>
    /// <response code="404">Event or category not found.</response>
    /// <response code="409">Category already has enrolments.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int eventId, int id)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return Forbid();

        var cat = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.EventId == eventId);
        if (cat == null) return NotFound();
        if (await _db.Enrolments.AnyAsync(e => e.CategoryId == id))
            return Conflict(new { error = "Category has enrolments and cannot be deleted." });

        _db.Categories.Remove(cat);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
