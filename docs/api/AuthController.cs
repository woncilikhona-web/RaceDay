using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using RaceDay.Shared;

namespace RaceDay.Api.Controllers;

[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IPasswordService _passwords;
    private readonly ITokenService _tokens;

    public AuthController(RaceDayDbContext db, IPasswordService passwords, ITokenService tokens)
    {
        _db = db;
        _passwords = passwords;
        _tokens = tokens;
    }

    /// <summary>Register a new Organiser or Participant account.</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Fail(409, "An account with this email address already exists.");

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = email,
            PasswordHash = _passwords.Hash(dto.Password),
            Role = dto.Role,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Fail(409, "An account with this email address already exists.");
        }

        return Ok(BuildResponse(user));
    }

    /// <summary>Log in and receive a JWT bearer token.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null || !_passwords.Verify(dto.Password, user.PasswordHash))
            return Fail(401, "Invalid email or password.");

        return Ok(BuildResponse(user));
    }

    private AuthResponseDto BuildResponse(User user)
    {
        var (token, expires) = _tokens.Create(user);
        return new AuthResponseDto
        {
            Token = token,
            ExpiresAt = expires,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role
        };
    }
}
