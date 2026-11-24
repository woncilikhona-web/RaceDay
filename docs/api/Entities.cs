using RaceDay.Shared;

namespace RaceDay.Api.Models;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string Role { get; set; }
    public string PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string Bio { get; set; }
    public string ProfileImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<Event> OrganisedEvents { get; set; } = new List<Event>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public ICollection<Event> Events { get; set; } = new List<Event>();
}

public class Event
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime Date { get; set; }
    public string Location { get; set; }
    public decimal DistanceKm { get; set; }
    public EventType EventType { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; }
    public int OrganiserId { get; set; }
    public User Organiser { get; set; }
    public string ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}

public class Enrolment
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public Event Event { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public DateTime EnrolledAt { get; set; }
    public RaceResult Result { get; set; }
}

public class RaceResult
{
    public int Id { get; set; }
    public int EnrolmentId { get; set; }
    public Enrolment Enrolment { get; set; }
    public TimeSpan FinishTime { get; set; }
    public int? Position { get; set; }
    public string Notes { get; set; }
    public DateTime RecordedAt { get; set; }
}
