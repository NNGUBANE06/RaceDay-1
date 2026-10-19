using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Authorize]
public class EnrolmentsController : ApiControllerBase
{
    private readonly AppDbContext _db;

    public EnrolmentsController(AppDbContext db) => _db = db;

    private static EnrolmentResponse ToDto(Enrolment e) =>
        new(e.Id, e.EventId, e.Event!.Name, e.CategoryId, e.Category!.Name,
            e.ParticipantId, $"{e.Participant!.FirstName} {e.Participant.LastName}", e.EnrolledAt);

    private IQueryable<Enrolment> Query() =>
        _db.Enrolments.AsNoTracking().Include(e => e.Event).Include(e => e.Category).Include(e => e.Participant);

    /// <summary>Enrol in an event by selecting one of its categories (Participant only).</summary>
    /// <remarks>Records the link between the participant, the event and the chosen category.</remarks>
    /// <response code="201">Enrolment recorded.</response>
    /// <response code="400">Category does not belong to this event.</response>
    /// <response code="403">Caller is not a Participant.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="409">Already enrolled in this event.</response>
    [HttpPost("api/events/{eventId:int}/enrolments")]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(int eventId, EnrolRequest req)
    {
        if (!await _db.Events.AnyAsync(e => e.Id == eventId)) return NotFound();
        if (!await _db.Categories.AnyAsync(c => c.Id == req.CategoryId && c.EventId == eventId))
            return BadRequest(new { error = "Category does not belong to this event." });

        var userId = CurrentUserId;
        if (await _db.Enrolments.AnyAsync(e => e.EventId == eventId && e.ParticipantId == userId))
            return Conflict(new { error = "You are already enrolled in this event." });

        var enrolment = new Enrolment
        {
            EventId = eventId,
            CategoryId = req.CategoryId,
            ParticipantId = userId,
            EnrolledAt = DateTime.UtcNow
        };
        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        var saved = await Query().FirstAsync(e => e.Id == enrolment.Id);
        return StatusCode(StatusCodes.Status201Created, ToDto(saved));
    }

    /// <summary>View all enrolments for one of your events (Organiser only).</summary>
    /// <response code="200">Enrolments for the event.</response>
    /// <response code="403">Caller does not own the event.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("api/events/{eventId:int}/enrolments")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ForEvent(int eventId)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId);
        if (ev == null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return Forbid();

        var list = await Query().Where(e => e.EventId == eventId).ToListAsync();
        return Ok(list.Select(ToDto));
    }

    /// <summary>View your own enrolments (Participant only).</summary>
    /// <response code="200">The participant's enrolments.</response>
    /// <response code="403">Caller is not a Participant.</response>
    [HttpGet("api/enrolments/mine")]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Mine()
    {
        var userId = CurrentUserId;
        var list = await Query().Where(e => e.ParticipantId == userId).ToListAsync();
        return Ok(list.Select(ToDto));
    }
}
