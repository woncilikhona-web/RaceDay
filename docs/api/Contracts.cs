using System.ComponentModel.DataAnnotations;

namespace RaceDay.Shared;

public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

public enum EventType
{
    Run = 1,
    Walk = 2,
    Cycle = 3
}

public static class ValidationPatterns
{
    public const string Password = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,100}$";
    public const string PasswordMessage = "Password must be 8-100 characters and include an upper-case letter, a lower-case letter and a digit.";
    public const string FinishTime = @"^\d{1,2}:[0-5]\d:[0-5]\d$";
    public const string FinishTimeMessage = "Finish time must be in the format hh:mm:ss (for example 01:45:30).";
}

// ---------- Auth ----------
public class RegisterDto
{
    [Required, StringLength(100, MinimumLength = 2)]
    [Display(Name = "Full name")]
    public string FullName { get; set; }

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; }

    [Required, RegularExpression(ValidationPatterns.Password, ErrorMessage = ValidationPatterns.PasswordMessage)]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [Required, RegularExpression("^(Organiser|Participant)$", ErrorMessage = "Role must be Organiser or Participant.")]
    public string Role { get; set; } = Roles.Participant;
}

public class LoginDto
{
    [Required, EmailAddress]
    public string Email { get; set; }

    [Required, DataType(DataType.Password)]
    public string Password { get; set; }
}

public class AuthResponseDto
{
    public string Token { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
}

// ---------- Profile ----------
public class UpdateProfileDto
{
    [Required, StringLength(100, MinimumLength = 2)]
    [Display(Name = "Full name")]
    public string FullName { get; set; }

    [StringLength(30), Phone]
    [Display(Name = "Phone number")]
    public string PhoneNumber { get; set; }

    [Display(Name = "Date of birth")]
    public DateTime? DateOfBirth { get; set; }

    [StringLength(500)]
    public string Bio { get; set; }

    [StringLength(500)]
    public string ProfileImageUrl { get; set; }
}

public class ProfileDto : UpdateProfileDto
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
}

// ---------- Categories ----------
public class CategoryDto
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; }

    [StringLength(300)]
    public string Description { get; set; }

    public int EventCount { get; set; }
}

// ---------- Events ----------
public class EventUpsertDto
{
    [Required, StringLength(150)]
    [Display(Name = "Event name")]
    public string Name { get; set; }

    [Required, StringLength(2000)]
    public string Description { get; set; }

    [Required]
    [Display(Name = "Date and time")]
    public DateTime Date { get; set; }

    [Required, StringLength(200)]
    public string Location { get; set; }

    [Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal DistanceKm { get; set; }

    [Required]
    [Display(Name = "Event type")]
    public EventType EventType { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Please choose a category.")]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    [StringLength(500)]
    public string ImageUrl { get; set; }
}

public class EventDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime Date { get; set; }
    public string Location { get; set; }
    public decimal DistanceKm { get; set; }
    public EventType EventType { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; }
    public int OrganiserId { get; set; }
    public string OrganiserName { get; set; }
    public string ImageUrl { get; set; }
    public int EnrolmentCount { get; set; }
    public bool IsEnrolled { get; set; }
}

// ---------- Enrolments ----------
public class EnrolRequestDto
{
    [Range(1, int.MaxValue)]
    public int EventId { get; set; }
}

public class EnrolmentDto
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; }
    public EventType EventType { get; set; }
    public decimal DistanceKm { get; set; }
    public DateTime EnrolledAt { get; set; }
    public bool HasResult { get; set; }
}

public class ParticipantDto
{
    public int EnrolmentId { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public DateTime EnrolledAt { get; set; }
    public int? ResultId { get; set; }
    public string FinishTime { get; set; }
    public int? Position { get; set; }
    public string Notes { get; set; }
}

// ---------- Results ----------
public class ResultUpsertDto
{
    [Range(1, int.MaxValue)]
    public int EnrolmentId { get; set; }

    [Required, RegularExpression(ValidationPatterns.FinishTime, ErrorMessage = ValidationPatterns.FinishTimeMessage)]
    [Display(Name = "Finish time (hh:mm:ss)")]
    public string FinishTime { get; set; }

    [Range(1, 1000000)]
    public int? Position { get; set; }

    [StringLength(500)]
    public string Notes { get; set; }
}

public class ResultDto
{
    public int Id { get; set; }
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; }
    public EventType EventType { get; set; }
    public decimal DistanceKm { get; set; }
    public string FinishTime { get; set; }
    public int? Position { get; set; }
    public string Notes { get; set; }
}

public class FileUploadResultDto
{
    public string Url { get; set; }
}
