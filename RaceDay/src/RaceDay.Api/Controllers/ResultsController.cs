using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Authorize]
public class ResultsController : ApiControllerBase
{
    private readonly AppDbContext _db;

    public ResultsController(AppDbContext db) => _db = db;

    private static ResultResponse ToDto(RaceResult r) =>
        new(r.Id, r.EnrolmentId, r.Enrolment!.EventId, r.Enrolment.Event!.Name, r.Enrolment.ParticipantId,
            $"{r.Enrolment.Participant!.FirstName} {r.Enrolment.Participant.LastName}", r.FinishTime, r.Position);

    private IQueryable<RaceResult> Query() =>
        _db.Results.AsNoTracking()
            .Include(r => r.Enrolment).ThenInclude(e => e!.Event)
            .Include(r => r.Enrolment).ThenInclude(e => e!.Participant);

    /// <summary>Capture a participant's finish time and position (Organiser only).</summary>
    /// <response code="201">Result captured.</response>
    /// <response code="403">Caller does not own the event.</response>
    /// <response code="404">Event or enrolment not found.</response>
    /// <response code="409">A result already exists for this enrolment.</response>
    [HttpPost("api/events/{eventId:int}/results")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Capture(int eventId, ResultRequest req)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId);
        if (ev == null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return Forbid();

        var enrolment = await _db.Enrolments.FirstOrDefaultAsync(e => e.Id == req.EnrolmentId && e.EventId == eventId);
        if (enrolment == null) return NotFound(new { error = "Enrolment not found for this event." });
        if (await _db.Results.AnyAsync(r => r.EnrolmentId == enrolment.Id))
            return Conflict(new { error = "A result has already been captured for this enrolment." });

        var result = new RaceResult { EnrolmentId = enrolment.Id, FinishTime = req.FinishTime, Position = req.Position };
        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        var saved = await Query().FirstAsync(r => r.Id == result.Id);
        return StatusCode(StatusCodes.Status201Created, ToDto(saved));
    }

    /// <summary>View all results for one of your events (Organiser only).</summary>
    /// <response code="200">Results ordered by position.</response>
    /// <response code="403">Caller does not own the event.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("api/events/{eventId:int}/results")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ForEvent(int eventId)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId);
        if (ev == null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return Forbid();

        var list = await Query().Where(r => r.Enrolment!.EventId == eventId).OrderBy(r => r.Position).ToListAsync();
        return Ok(list.Select(ToDto));
    }

    /// <summary>View your own results (Participant only).</summary>
    /// <response code="200">The participant's own results.</response>
    /// <response code="403">Caller is not a Participant.</response>
    [HttpGet("api/results/mine")]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Mine()
    {
        var userId = CurrentUserId;
        var list = await Query().Where(r => r.Enrolment!.ParticipantId == userId).ToListAsync();
        return Ok(list.Select(ToDto));
    }
}
