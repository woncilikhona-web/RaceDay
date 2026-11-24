using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using RaceDay.Shared;

namespace RaceDay.Api.Controllers;

[Route("api/events")]
public class EventsController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;
    public EventsController(RaceDayDbContext db) => _db = db;

    private static IQueryable<EventDto> Project(IQueryable<Event> query, int userId) =>
        query.Select(e => new EventDto
        {
            Id = e.Id,
            Name = e.Name,
            Description = e.Description,
            Date = e.Date,
            Location = e.Location,
            DistanceKm = e.DistanceKm,
            EventType = e.EventType,
            CategoryId = e.CategoryId,
            CategoryName = e.Category.Name,
            OrganiserId = e.OrganiserId,
            OrganiserName = e.Organiser.FullName,
            ImageUrl = e.ImageUrl,
            EnrolmentCount = e.Enrolments.Count(),
            IsEnrolled = e.Enrolments.Any(en => en.UserId == userId)
        });

    /// <summary>Browse and search events.</summary>
    [HttpGet, AllowAnonymous]
    public async Task<ActionResult<List<EventDto>>> Search(
        [FromQuery] string search, [FromQuery] EventType? eventType, [FromQuery] int? categoryId, [FromQuery] bool upcomingOnly = false)
    {
        var query = _db.Events.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(e => e.Name.Contains(s) || e.Location.Contains(s) || e.Description.Contains(s));
        }
        if (eventType.HasValue) query = query.Where(e => e.EventType == eventType.Value);
        if (categoryId.HasValue) query = query.Where(e => e.CategoryId == categoryId.Value);
        if (upcomingOnly)
        {
            var now = DateTime.Now;
            query = query.Where(e => e.Date >= now);
        }

        var list = await Project(query.OrderBy(e => e.Date), CurrentUserId).ToListAsync();
        return Ok(list);
    }

    /// <summary>Events managed by the logged-in organiser.</summary>
    [HttpGet("mine"), Authorize(Roles = Roles.Organiser)]
    public async Task<ActionResult<List<EventDto>>> Mine()
    {
        var uid = CurrentUserId;
        var list = await Project(_db.Events.AsNoTracking().Where(e => e.OrganiserId == uid).OrderBy(e => e.Date), uid).ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id:int}"), AllowAnonymous]
    public async Task<ActionResult<EventDto>> Get(int id)
    {
        var dto = await Project(_db.Events.AsNoTracking().Where(e => e.Id == id), CurrentUserId).FirstOrDefaultAsync();
        return dto == null ? Fail(404, "Event not found.") : Ok(dto);
    }

    [HttpPost, Authorize(Roles = Roles.Organiser)]
    public async Task<ActionResult<EventDto>> Create(EventUpsertDto dto)
    {
        if (!await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId))
            return Fail(400, "The selected category does not exist.");

        var entity = new Event { OrganiserId = CurrentUserId, CreatedAt = DateTime.UtcNow };
        Apply(entity, dto);
        _db.Events.Add(entity);
        await _db.SaveChangesAsync();

        var created = await Project(_db.Events.AsNoTracking().Where(e => e.Id == entity.Id), CurrentUserId).FirstAsync();
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, created);
    }

    [HttpPut("{id:int}"), Authorize(Roles = Roles.Organiser)]
    public async Task<ActionResult<EventDto>> Update(int id, EventUpsertDto dto)
    {
        var entity = await _db.Events.FirstOrDefaultAsync(e => e.Id == id);
        if (entity == null) return Fail(404, "Event not found.");
        if (entity.OrganiserId != CurrentUserId) return Fail(403, "You can only edit events that you manage.");
        if (!await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId))
            return Fail(400, "The selected category does not exist.");

        Apply(entity, dto);
        await _db.SaveChangesAsync();

        var updated = await Project(_db.Events.AsNoTracking().Where(e => e.Id == id), CurrentUserId).FirstAsync();
        return Ok(updated);
    }

    [HttpDelete("{id:int}"), Authorize(Roles = Roles.Organiser)]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.Events.FirstOrDefaultAsync(e => e.Id == id);
        if (entity == null) return Fail(404, "Event not found.");
        if (entity.OrganiserId != CurrentUserId) return Fail(403, "You can only delete events that you manage.");

        _db.Events.Remove(entity);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Participants enrolled in one of the organiser's events (with any recorded result).</summary>
    [HttpGet("{id:int}/participants"), Authorize(Roles = Roles.Organiser)]
    public async Task<ActionResult<List<ParticipantDto>>> Participants(int id)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        if (ev == null) return Fail(404, "Event not found.");
        if (ev.OrganiserId != CurrentUserId) return Fail(403, "You can only view participants of events that you manage.");

        var rows = await _db.Enrolments.AsNoTracking()
            .Where(x => x.EventId == id)
            .OrderBy(x => x.User.FullName)
            .Select(x => new
            {
                x.Id,
                x.UserId,
                x.User.FullName,
                x.User.Email,
                x.EnrolledAt,
                ResultId = x.Result != null ? (int?)x.Result.Id : null,
                Finish = x.Result != null ? (TimeSpan?)x.Result.FinishTime : null,
                Position = x.Result != null ? x.Result.Position : null,
                Notes = x.Result != null ? x.Result.Notes : null
            })
            .ToListAsync();

        var list = rows.Select(r => new ParticipantDto
        {
            EnrolmentId = r.Id,
            UserId = r.UserId,
            FullName = r.FullName,
            Email = r.Email,
            EnrolledAt = r.EnrolledAt,
            ResultId = r.ResultId,
            FinishTime = Mapping.FormatTime(r.Finish),
            Position = r.Position,
            Notes = r.Notes
        }).ToList();
        return Ok(list);
    }

    private static void Apply(Event entity, EventUpsertDto dto)
    {
        entity.Name = dto.Name.Trim();
        entity.Description = dto.Description.Trim();
        entity.Date = dto.Date;
        entity.Location = dto.Location.Trim();
        entity.DistanceKm = dto.DistanceKm;
        entity.EventType = dto.EventType;
        entity.CategoryId = dto.CategoryId;
        entity.ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim();
    }
}
