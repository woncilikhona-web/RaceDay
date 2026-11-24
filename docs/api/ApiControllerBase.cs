using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Id of the logged-in user (0 when the request is anonymous).</summary>
    protected int CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    protected ObjectResult Fail(int statusCode, string message) => Problem(detail: message, statusCode: statusCode);
}
