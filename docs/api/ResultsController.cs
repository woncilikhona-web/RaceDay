using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using RaceDay.Shared;

namespace RaceDay.Api.Controllers;

[Route("api/results")]
public class ResultsController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;
    public ResultsController(RaceDayDbContext db) => _db = db;

    /// <summary>Record or update a participant's result (organiser of the event only).</summary>
    [HttpPut, Authorize(Roles = Roles.Organiser)]
    public async Task<ActionResult<ResultDto>> Upsert(ResultUpsertDto dto)
    {
        if (!Mapping.TryParseTime(dto.FinishTime, out var finish))
            return Fail(400, ValidationPatterns.FinishTimeMessage);

        var enrolment = await _db.Enrolments
            .Include(e => e.Event)
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.Id == dto.EnrolmentId);
        if (enrolment == null) return Fail(404, "Enrolment not found.");
        if (enrolment.Event.OrganiserId != CurrentUserId)
            return Fail(403, "You can only record results for events that you manage.");

        var result = enrolment.Result;
        if (result == null)
        {
            result = new RaceResult { EnrolmentId = enrolment.Id };
            _db.Results.Add(result);
        }
        result.FinishTime = finish;
        result.Position = dto.Position;
        result.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();
        result.RecordedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new ResultDto
        {
            Id = result.Id,
            EnrolmentId = enrolment.Id,
            EventId = enrolment.EventId,
            EventName = enrolment.Event.Name,
            EventDate = enrolment.Event.Date,
            Location = enrolment.Event.Location,
            EventType = enrolment.Event.EventType,
            DistanceKm = enrolment.Event.DistanceKm,
            FinishTime = Mapping.FormatTime(result.FinishTime),
            Position = result.Position,
            Notes = result.Notes
        });
    }

    [HttpDelete("{id:int}"), Authorize(Roles = Roles.Organiser)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _db.Results.Include(r => r.Enrolment).ThenInclude(e => e.Event).FirstOrDefaultAsync(r => r.Id == id);
        if (result == null) return Fail(404, "Result not found.");
        if (result.Enrolment.Event.OrganiserId != CurrentUserId)
            return Fail(403, "You can only delete results for events that you manage.");

        _db.Results.Remove(result);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>The logged-in participant's own results.</summary>
    [HttpGet("mine"), Authorize(Roles = Roles.Participant)]
    public async Task<ActionResult<List<ResultDto>>> Mine()
    {
        var uid = CurrentUserId;
        var rows = await _db.Results.AsNoTracking()
            .Where(r => r.Enrolment.UserId == uid)
            .OrderByDescending(r => r.Enrolment.Event.Date)
            .Select(r => new
            {
                r.Id,
                r.EnrolmentId,
                EventId = r.Enrolment.EventId,
                EventName = r.Enrolment.Event.Name,
                EventDate = r.Enrolment.Event.Date,
                Location = r.Enrolment.Event.Location,
                EventType = r.Enrolment.Event.EventType,
                DistanceKm = r.Enrolment.Event.DistanceKm,
                r.FinishTime,
                r.Position,
                r.Notes
            })
            .ToListAsync();

        var list = rows.Select(r => new ResultDto
        {
            Id = r.Id,
            EnrolmentId = r.EnrolmentId,
            EventId = r.EventId,
            EventName = r.EventName,
            EventDate = r.EventDate,
            Location = r.Location,
            EventType = r.EventType,
            DistanceKm = r.DistanceKm,
            FinishTime = Mapping.FormatTime(r.FinishTime),
            Position = r.Position,
            Notes = r.Notes
        }).ToList();
        return Ok(list);
    }
}
