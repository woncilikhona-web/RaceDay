using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Shared;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : AppController
{
    public AccountController(ApiClient api) : base(api) { }

    [HttpGet]
    public IActionResult Login(string returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToDashboard();
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await Api.PostAsync<AuthResponseDto>("api/auth/login", new LoginDto { Email = model.Email, Password = model.Password });
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(model);
        }

        await SignInAsync(result.Data);
        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);
        return RedirectToDashboard(result.Data.Role);
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToDashboard();
        return View(new RegisterViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var dto = new RegisterDto { FullName = model.FullName, Email = model.Email, Password = model.Password, Role = model.Role };
        var result = await Api.PostAsync<AuthResponseDto>("api/auth/register", dto);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(model);
        }

        await SignInAsync(result.Data);
        Success("Welcome to RaceDay! Your account has been created.");
        return RedirectToDashboard(result.Data.Role);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    [Authorize, HttpGet]
    public async Task<IActionResult> Profile()
    {
        var result = await Api.GetAsync<ProfileDto>("api/profile");
        if (!result.Success)
        {
            Error(result.Error);
            return RedirectToDashboard();
        }
        return View(ToViewModel(result.Data));
    }

    [Authorize, HttpPost]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var imageUrl = model.ProfileImageUrl;
        if (model.PhotoFile != null && model.PhotoFile.Length > 0)
        {
            var upload = await Api.UploadImageAsync(model.PhotoFile);
            if (!upload.Success)
            {
                ModelState.AddModelError(nameof(model.PhotoFile), upload.Error);
                return View(model);
            }
            imageUrl = upload.Data;
        }

        var dto = new UpdateProfileDto
        {
            FullName = model.FullName,
            PhoneNumber = model.PhoneNumber,
            DateOfBirth = model.DateOfBirth,
            Bio = model.Bio,
            ProfileImageUrl = imageUrl
        };
        var result = await Api.PutAsync<ProfileDto>("api/profile", dto);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(model);
        }

        await RefreshDisplayNameAsync(result.Data.FullName);
        Success("Your profile has been updated.");
        return RedirectToAction(nameof(Profile));
    }

    private static ProfileViewModel ToViewModel(ProfileDto p) => new()
    {
        FullName = p.FullName,
        PhoneNumber = p.PhoneNumber,
        DateOfBirth = p.DateOfBirth,
        Bio = p.Bio,
        ProfileImageUrl = p.ProfileImageUrl,
        Email = p.Email,
        Role = p.Role
    };

    private IActionResult RedirectToDashboard(string role) =>
        role == Roles.Organiser
            ? RedirectToAction("Dashboard", "Organiser")
            : RedirectToAction("Dashboard", "Participant");

    private async Task SignInAsync(AuthResponseDto auth)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, auth.UserId.ToString()),
            new Claim(ClaimTypes.Name, auth.FullName),
            new Claim(ClaimTypes.Email, auth.Email),
            new Claim(ClaimTypes.Role, auth.Role),
            new Claim(ApiClient.TokenClaim, auth.Token)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var props = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = new DateTimeOffset(DateTime.SpecifyKind(auth.ExpiresAt, DateTimeKind.Utc))
        };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), props);
    }

    private async Task RefreshDisplayNameAsync(string fullName)
    {
        var auth = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (auth?.Principal == null) return;
        var claims = auth.Principal.Claims.Where(c => c.Type != ClaimTypes.Name).ToList();
        claims.Add(new Claim(ClaimTypes.Name, fullName));
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), auth.Properties);
    }
}
