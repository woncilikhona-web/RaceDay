using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Shared;

namespace RaceDay.Api.Controllers;

[Route("api/categories")]
public class CategoriesController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;
    public CategoriesController(RaceDayDbContext db) => _db = db;

    [HttpGet, AllowAnonymous]
    public async Task<ActionResult<List<CategoryDto>>> GetAll()
    {
        var list = await _db.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto { Id = c.Id, Name = c.Name, Description = c.Description, EventCount = c.Events.Count() })
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id:int}"), AllowAnonymous]
    public async Task<ActionResult<CategoryDto>> Get(int id)
    {
        var c = await _db.Categories.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new CategoryDto { Id = x.Id, Name = x.Name, Description = x.Description, EventCount = x.Events.Count() })
            .FirstOrDefaultAsync();
        return c == null ? Fail(404, "Category not found.") : Ok(c);
    }

    [HttpPost, Authorize(Roles = Roles.Organiser)]
    public async Task<ActionResult<CategoryDto>> Create(CategoryDto dto)
    {
        var name = dto.Name.Trim();
        if (await _db.Categories.AnyAsync(c => c.Name == name))
            return Fail(409, "A category with this name already exists.");

        var entity = new Category { Name = name, Description = dto.Description?.Trim() };
        _db.Categories.Add(entity);
        await _db.SaveChangesAsync();

        dto.Id = entity.Id;
        dto.Name = entity.Name;
        dto.EventCount = 0;
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, dto);
    }

    [HttpPut("{id:int}"), Authorize(Roles = Roles.Organiser)]
    public async Task<ActionResult<CategoryDto>> Update(int id, CategoryDto dto)
    {
        var entity = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (entity == null) return Fail(404, "Category not found.");

        var name = dto.Name.Trim();
        if (await _db.Categories.AnyAsync(c => c.Name == name && c.Id != id))
            return Fail(409, "A category with this name already exists.");

        entity.Name = name;
        entity.Description = dto.Description?.Trim();
        await _db.SaveChangesAsync();

        dto.Id = entity.Id;
        dto.Name = entity.Name;
        dto.EventCount = await _db.Events.CountAsync(e => e.CategoryId == id);
        return Ok(dto);
    }

    [HttpDelete("{id:int}"), Authorize(Roles = Roles.Organiser)]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (entity == null) return Fail(404, "Category not found.");

        if (await _db.Events.AnyAsync(e => e.CategoryId == id))
            return Fail(409, "This category is used by one or more events and cannot be deleted.");

        _db.Categories.Remove(entity);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
