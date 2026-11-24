using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Shared;

namespace RaceDay.Api.Controllers;

[Route("api/enrolments")]
[Authorize(Roles = Roles.Participant)]
public class EnrolmentsController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;
    public EnrolmentsController(RaceDayDbContext db) => _db = db;

    private static IQueryable<EnrolmentDto> Project(IQueryable<Enrolment> q) =>
        q.Select(x => new EnrolmentDto
        {
            Id = x.Id,
            EventId = x.EventId,
            EventName = x.Event.Name,
            EventDate = x.Event.Date,
            Location = x.Event.Location,
            EventType = x.Event.EventType,
            DistanceKm = x.Event.DistanceKm,
            EnrolledAt = x.EnrolledAt,
            HasResult = x.Result != null
        });

    /// <summary>Enrol the logged-in participant in an event.</summary>
    [HttpPost]
    public async Task<ActionResult<EnrolmentDto>> Enrol(EnrolRequestDto request)
    {
        var uid = CurrentUserId;
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == request.EventId);
        if (ev == null) return Fail(404, "Event not found.");
        if (ev.Date < DateTime.Now) return Fail(400, "Enrolment is closed because this event has already taken place.");
        if (await _db.Enrolments.AnyAsync(e => e.EventId == request.EventId && e.UserId == uid))
            return Fail(409, "You are already enrolled in this event.");

        var entity = new Enrolment { EventId = request.EventId, UserId = uid, EnrolledAt = DateTime.UtcNow };
        _db.Enrolments.Add(entity);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Fail(409, "You are already enrolled in this event.");
        }

        var dto = await Project(_db.Enrolments.AsNoTracking().Where(x => x.Id == entity.Id)).FirstAsync();
        return StatusCode(201, dto);
    }

    /// <summary>The logged-in participant's enrolments.</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<List<EnrolmentDto>>> Mine()
    {
        var uid = CurrentUserId;
        var list = await Project(_db.Enrolments.AsNoTracking().Where(x => x.UserId == uid).OrderBy(x => x.Event.Date)).ToListAsync();
        return Ok(list);
    }

    /// <summary>Withdraw from an event (only before a result has been recorded).</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Withdraw(int id)
    {
        var uid = CurrentUserId;
        var entity = await _db.Enrolments.FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);
        if (entity == null) return Fail(404, "Enrolment not found.");
        if (await _db.Results.AnyAsync(r => r.EnrolmentId == id))
            return Fail(400, "You cannot withdraw because a result has already been recorded.");

        _db.Enrolments.Remove(entity);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
