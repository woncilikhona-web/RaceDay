using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Shared;

namespace RaceDay.Api.Controllers;

[Route("api/profile")]
[Authorize]
public class ProfileController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;
    public ProfileController(RaceDayDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<ProfileDto>> Get()
    {
        var uid = CurrentUserId;
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == uid);
        if (user == null) return Fail(404, "User not found.");
        return Ok(new ProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            Role = user.Role,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            DateOfBirth = user.DateOfBirth,
            Bio = user.Bio,
            ProfileImageUrl = user.ProfileImageUrl
        });
    }

    [HttpPut]
    public async Task<ActionResult<ProfileDto>> Update(UpdateProfileDto dto)
    {
        var uid = CurrentUserId;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == uid);
        if (user == null) return Fail(404, "User not found.");

        user.FullName = dto.FullName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();
        user.DateOfBirth = dto.DateOfBirth;
        user.Bio = string.IsNullOrWhiteSpace(dto.Bio) ? null : dto.Bio.Trim();
        user.ProfileImageUrl = string.IsNullOrWhiteSpace(dto.ProfileImageUrl) ? null : dto.ProfileImageUrl.Trim();
        await _db.SaveChangesAsync();

        return Ok(new ProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            Role = user.Role,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            DateOfBirth = user.DateOfBirth,
            Bio = user.Bio,
            ProfileImageUrl = user.ProfileImageUrl
        });
    }
}
