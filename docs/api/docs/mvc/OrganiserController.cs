using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Shared;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[Authorize(Roles = Roles.Organiser)]
public class OrganiserController : AppController
{
    public OrganiserController(ApiClient api) : base(api) { }

    // ---------- Dashboard ----------
    public async Task<IActionResult> Dashboard()
    {
        var model = new OrganiserDashboardViewModel();
        var events = await Api.GetAsync<List<EventDto>>("api/events/mine");
        var categories = await Api.GetAsync<List<CategoryDto>>("api/categories");
        if (!events.Success) Error(events.Error);
        else model.Events = events.Data ?? new();
        if (categories.Success) model.CategoryCount = categories.Data?.Count ?? 0;
        return View(model);
    }

    // ---------- Events ----------
    public async Task<IActionResult> Events()
    {
        var result = await Api.GetAsync<List<EventDto>>("api/events/mine");
        if (!result.Success) Error(result.Error);
        return View(result.Success ? result.Data ?? new() : new List<EventDto>());
    }

    [HttpGet]
    public async Task<IActionResult> CreateEvent()
    {
        var model = new EventFormViewModel { Date = DateTime.Today.AddDays(30).AddHours(8), EventType = EventType.Run, DistanceKm = 5 };
        await LoadCategoriesAsync(model);
        return View("EventForm", model);
    }

    [HttpPost]
    public async Task<IActionResult> CreateEvent(EventFormViewModel model)
    {
        await LoadCategoriesAsync(model);
        if (!ModelState.IsValid) return View("EventForm", model);
        if (!await ApplyImageAsync(model)) return View("EventForm", model);

        var result = await Api.PostAsync<EventDto>("api/events", ToUpsert(model));
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View("EventForm", model);
        }
        Success("Event created.");
        return RedirectToAction(nameof(Events));
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var result = await Api.GetAsync<EventDto>($"api/events/{id}");
        if (!result.Success)
        {
            Error(result.Error);
            return RedirectToAction(nameof(Events));
        }
        var e = result.Data;
        if (e.OrganiserId != CurrentUserId)
        {
            Error("You can only edit events that you manage.");
            return RedirectToAction(nameof(Events));
        }

        var model = new EventFormViewModel
        {
            Id = e.Id,
            Name = e.Name,
            Description = e.Description,
            Date = e.Date,
            Location = e.Location,
            DistanceKm = e.DistanceKm,
            EventType = e.EventType,
            CategoryId = e.CategoryId,
            ImageUrl = e.ImageUrl
        };
        await LoadCategoriesAsync(model);
        return View("EventForm", model);
    }

    [HttpPost]
    public async Task<IActionResult> EditEvent(int id, EventFormViewModel model)
    {
        model.Id = id;
        await LoadCategoriesAsync(model);
        if (!ModelState.IsValid) return View("EventForm", model);
        if (!await ApplyImageAsync(model)) return View("EventForm", model);

        var result = await Api.PutAsync<EventDto>($"api/events/{id}", ToUpsert(model));
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View("EventForm", model);
        }
        Success("Event updated.");
        return RedirectToAction(nameof(Events));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var result = await Api.DeleteAsync($"api/events/{id}");
        if (result.Success) Success("Event deleted.");
        else Error(result.Error);
        return RedirectToAction(nameof(Events));
    }

    // ---------- Participants & results ----------
    public async Task<IActionResult> Participants(int id)
    {
        var ev = await Api.GetAsync<EventDto>($"api/events/{id}");
        if (!ev.Success)
        {
            Error(ev.Error);
            return RedirectToAction(nameof(Events));
        }
        var participants = await Api.GetAsync<List<ParticipantDto>>($"api/events/{id}/participants");
        if (!participants.Success)
        {
            Error(participants.Error);
            return RedirectToAction(nameof(Events));
        }
        return View(new ParticipantsViewModel { Event = ev.Data, Participants = participants.Data ?? new() });
    }

    [HttpGet]
    public async Task<IActionResult> RecordResult(int eventId, int enrolmentId)
    {
        var model = await BuildResultFormAsync(eventId, enrolmentId);
        if (model == null) return RedirectToAction(nameof(Participants), new { id = eventId });
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> RecordResult(ResultFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var dto = new ResultUpsertDto
        {
            EnrolmentId = model.EnrolmentId,
            FinishTime = model.FinishTime,
            Position = model.Position,
            Notes = model.Notes
        };
        var result = await Api.PutAsync<ResultDto>("api/results", dto);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(model);
        }
        Success("Result saved.");
        return RedirectToAction(nameof(Participants), new { id = model.EventId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteResult(int id, int eventId)
    {
        var result = await Api.DeleteAsync($"api/results/{id}");
        if (result.Success) Success("Result removed.");
        else Error(result.Error);
        return RedirectToAction(nameof(Participants), new { id = eventId });
    }

    // ---------- Categories ----------
    public async Task<IActionResult> Categories()
    {
        var result = await Api.GetAsync<List<CategoryDto>>("api/categories");
        if (!result.Success) Error(result.Error);
        return View(result.Success ? result.Data ?? new() : new List<CategoryDto>());
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategory(CategoryDto model)
    {
        if (!ModelState.IsValid)
        {
            Error("Please enter a category name (max 100 characters).");
            return RedirectToAction(nameof(Categories));
        }
        var result = await Api.PostAsync<CategoryDto>("api/categories", new CategoryDto { Name = model.Name, Description = model.Description });
        if (result.Success) Success("Category added.");
        else Error(result.Error);
        return RedirectToAction(nameof(Categories));
    }

    [HttpGet]
    public async Task<IActionResult> EditCategory(int id)
    {
        var result = await Api.GetAsync<CategoryDto>($"api/categories/{id}");
        if (!result.Success)
        {
            Error(result.Error);
            return RedirectToAction(nameof(Categories));
        }
        return View(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> EditCategory(int id, CategoryDto model)
    {
        model.Id = id;
        if (!ModelState.IsValid) return View(model);
        var result = await Api.PutAsync<CategoryDto>($"api/categories/{id}", new CategoryDto { Name = model.Name, Description = model.Description });
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(model);
        }
        Success("Category updated.");
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var result = await Api.DeleteAsync($"api/categories/{id}");
        if (result.Success) Success("Category deleted.");
        else Error(result.Error);
        return RedirectToAction(nameof(Categories));
    }

    // ---------- Helpers ----------
    private async Task LoadCategoriesAsync(EventFormViewModel model)
    {
        var result = await Api.GetAsync<List<CategoryDto>>("api/categories");
        model.Categories = result.Success ? result.Data ?? new() : new List<CategoryDto>();
    }

    private async Task<bool> ApplyImageAsync(EventFormViewModel model)
    {
        if (model.ImageFile == null || model.ImageFile.Length == 0) return true;
        var upload = await Api.UploadImageAsync(model.ImageFile);
        if (!upload.Success)
        {
            ModelState.AddModelError(nameof(model.ImageFile), upload.Error);
            return false;
        }
        model.ImageUrl = upload.Data;
        return true;
    }

    private static EventUpsertDto ToUpsert(EventFormViewModel m) => new()
    {
        Name = m.Name,
        Description = m.Description,
        Date = m.Date,
        Location = m.Location,
        DistanceKm = m.DistanceKm,
        EventType = m.EventType,
        CategoryId = m.CategoryId,
        ImageUrl = m.ImageUrl
    };

    private async Task<ResultFormViewModel> BuildResultFormAsync(int eventId, int enrolmentId)
    {
        var ev = await Api.GetAsync<EventDto>($"api/events/{eventId}");
        var participants = await Api.GetAsync<List<ParticipantDto>>($"api/events/{eventId}/participants");
        if (!ev.Success || !participants.Success)
        {
            Error(ev.Success ? participants.Error : ev.Error);
            return null;
        }
        var p = participants.Data?.FirstOrDefault(x => x.EnrolmentId == enrolmentId);
        if (p == null)
        {
            Error("That participant is not enrolled in this event.");
            return null;
        }
        return new ResultFormViewModel
        {
            EnrolmentId = enrolmentId,
            EventId = eventId,
            EventName = ev.Data.Name,
            ParticipantName = p.FullName,
            FinishTime = p.FinishTime,
            Position = p.Position,
            Notes = p.Notes
        };
    }
}
