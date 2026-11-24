using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using RaceDay.Shared;

namespace RaceDay.Web.Models;

public class LoginViewModel : LoginDto
{
    public string ReturnUrl { get; set; }
}

public class RegisterViewModel : RegisterDto
{
    [Required, DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; }
}

public class ProfileViewModel : UpdateProfileDto
{
    public string Email { get; set; }
    public string Role { get; set; }

    [ValidateNever]
    [Display(Name = "New profile photo")]
    public IFormFile PhotoFile { get; set; }
}

public class EventFormViewModel : EventUpsertDto
{
    public int Id { get; set; }
    public bool IsEdit => Id > 0;

    [ValidateNever]
    public List<CategoryDto> Categories { get; set; } = new();

    [ValidateNever]
    [Display(Name = "Event image")]
    public IFormFile ImageFile { get; set; }
}

public class ResultFormViewModel
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; }
    public string ParticipantName { get; set; }

    [Required, RegularExpression(ValidationPatterns.FinishTime, ErrorMessage = ValidationPatterns.FinishTimeMessage)]
    [Display(Name = "Finish time (hh:mm:ss)")]
    public string FinishTime { get; set; }

    [Range(1, 1000000)]
    public int? Position { get; set; }

    [StringLength(500)]
    public string Notes { get; set; }
}

public class EventSearchViewModel
{
    public string Search { get; set; }
    public EventType? EventType { get; set; }
    public int? CategoryId { get; set; }
    public bool UpcomingOnly { get; set; } = true;
    public List<EventDto> Events { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
}

public class ParticipantDashboardViewModel
{
    public List<EnrolmentDto> Enrolments { get; set; } = new();
    public List<ResultDto> Results { get; set; } = new();
}

public class OrganiserDashboardViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public int CategoryCount { get; set; }
}

public class ParticipantsViewModel
{
    public EventDto Event { get; set; }
    public List<ParticipantDto> Participants { get; set; } = new();
}
