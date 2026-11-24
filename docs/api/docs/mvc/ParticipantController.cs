using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Shared;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[Authorize(Roles = Roles.Participant)]
public class ParticipantController : AppController
{
    public ParticipantController(ApiClient api) : base(api) { }

    public async Task<IActionResult> Dashboard()
    {
        var model = new ParticipantDashboardViewModel();
        var enrolments = await Api.GetAsync<List<EnrolmentDto>>("api/enrolments/mine");
        var results = await Api.GetAsync<List<ResultDto>>("api/results/mine");
        if (!enrolments.Success) Error(enrolments.Error);
        else model.Enrolments = enrolments.Data ?? new();
        if (results.Success) model.Results = results.Data ?? new();
        return View(model);
    }

    public async Task<IActionResult> Events(EventSearchViewModel model)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(model.Search)) query.Add("search=" + Uri.EscapeDataString(model.Search));
        if (model.EventType.HasValue) query.Add("eventType=" + model.EventType.Value);
        if (model.CategoryId.HasValue) query.Add("categoryId=" + model.CategoryId.Value);
        query.Add("upcomingOnly=" + (model.UpcomingOnly ? "true" : "false"));

        var events = await Api.GetAsync<List<EventDto>>("api/events?" + string.Join("&", query));
        var categories = await Api.GetAsync<List<CategoryDto>>("api/categories");
        if (!events.Success) Error(events.Error);
        model.Events = events.Success ? events.Data ?? new() : new();
        model.Categories = categories.Success ? categories.Data ?? new() : new();
        return View(model);
    }

    public async Task<IActionResult> EventDetails(int id)
    {
        var result = await Api.GetAsync<EventDto>($"api/events/{id}");
        if (!result.Success)
        {
            Error(result.Error);
            return RedirectToAction(nameof(Events));
        }
        return View(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> Enrol(int id)
    {
        var result = await Api.PostAsync<EnrolmentDto>("api/enrolments", new EnrolRequestDto { EventId = id });
        if (result.Success)
        {
            Success("You are now enrolled in this event.");
            return RedirectToAction(nameof(Enrolments));
        }
        Error(result.Error);
        return RedirectToAction(nameof(EventDetails), new { id });
    }

    public async Task<IActionResult> Enrolments()
    {
        var result = await Api.GetAsync<List<EnrolmentDto>>("api/enrolments/mine");
        if (!result.Success) Error(result.Error);
        return View(result.Success ? result.Data ?? new() : new List<EnrolmentDto>());
    }

    [HttpPost]
    public async Task<IActionResult> Withdraw(int id)
    {
        var result = await Api.DeleteAsync($"api/enrolments/{id}");
        if (result.Success) Success("You have withdrawn from the event.");
        else Error(result.Error);
        return RedirectToAction(nameof(Enrolments));
    }

    public async Task<IActionResult> Results()
    {
        var result = await Api.GetAsync<List<ResultDto>>("api/results/mine");
        if (!result.Success) Error(result.Error);
        return View(result.Success ? result.Data ?? new() : new List<ResultDto>());
    }
}
