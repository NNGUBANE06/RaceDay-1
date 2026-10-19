using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/events")]
public class EventsController : ApiControllerBase
{
    private readonly AppDbContext _db;

    public EventsController(AppDbContext db) => _db = db;

    private static EventResponse ToDto(Event e) =>
        new(e.Id, e.Name, e.Description, e.Date, e.Location, e.DistanceKm, e.EventType, e.OrganiserId);

    /// <summary>List all events (both roles).</summary>
    /// <response code="200">All events ordered by date.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll()
    {
        var events = await _db.Events.AsNoTracking().OrderBy(e => e.Date).ToListAsync();
        return Ok(events.Select(ToDto));
    }

    /// <summary>Get a single event by id (both roles).</summary>
    /// <response code="200">The event.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id)
    {
        var e = await _db.Events.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return e == null ? NotFound() : Ok(ToDto(e));
    }

    /// <summary>Create an event (Organiser only).</summary>
    /// <remarks>Captures name, description, date, location, distance (km) and event type (Run, Walk or Cycle).</remarks>
    /// <response code="201">Event created.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="403">Caller is not an Organiser.</response>
    [HttpPost]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(EventRequest req)
    {
        var e = new Event
        {
            Name = req.Name.Trim(),
            Description = req.Description ?? "",
            Date = req.Date,
            Location = req.Location.Trim(),
            DistanceKm = req.DistanceKm,
            EventType = req.EventType,
            OrganiserId = CurrentUserId
        };
        _db.Events.Add(e);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = e.Id }, ToDto(e));
    }

    /// <summary>Update one of your events (Organiser only).</summary>
    /// <response code="200">Event updated.</response>
    /// <response code="403">Caller is not the Organiser who owns the event.</response>
    /// <response code="404">Event not found.</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, EventRequest req)
    {
        var e = await _db.Events.FindAsync(id);
        if (e == null) return NotFound();
        if (e.OrganiserId != CurrentUserId) return Forbid();

        e.Name = req.Name.Trim();
        e.Description = req.Description ?? "";
        e.Date = req.Date;
        e.Location = req.Location.Trim();
        e.DistanceKm = req.DistanceKm;
        e.EventType = req.EventType;
        await _db.SaveChangesAsync();
        return Ok(ToDto(e));
    }

    /// <summary>Delete one of your events (Organiser only).</summary>
    /// <response code="204">Event deleted.</response>
    /// <response code="403">Caller is not the Organiser who owns the event.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="409">Event already has enrolments and cannot be deleted.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var e = await _db.Events.FindAsync(id);
        if (e == null) return NotFound();
        if (e.OrganiserId != CurrentUserId) return Forbid();
        if (await _db.Enrolments.AnyAsync(x => x.EventId == id))
            return Conflict(new { error = "Event has enrolments and cannot be deleted." });

        _db.Events.Remove(e);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
