using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";

    public static bool IsValid(string? role) => role == Organiser || role == Participant;
}

public enum EventType
{
    Run,
    Walk,
    Cycle
}

public class User
{
    public int Id { get; set; }
    [Required, MaxLength(100)] public string FirstName { get; set; } = "";
    [Required, MaxLength(100)] public string LastName { get; set; } = "";
    [Required, MaxLength(200)] public string Email { get; set; } = "";
    [Required] public string PasswordHash { get; set; } = "";
    [Required, MaxLength(20)] public string Role { get; set; } = Roles.Participant;
    [MaxLength(500)] public string? ProfileImageBlobName { get; set; }
}

public class Event
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string Name { get; set; } = "";
    [MaxLength(2000)] public string Description { get; set; } = "";
    public DateTime Date { get; set; }
    [Required, MaxLength(200)] public string Location { get; set; } = "";
    public double DistanceKm { get; set; }
    public EventType EventType { get; set; }
    public int OrganiserId { get; set; }
    public User? Organiser { get; set; }
    public List<Category> Categories { get; set; } = new();
    [MaxLength(500)] public string? BannerBlobName { get; set; }
}

public class Category
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public Event? Event { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = "";
}

public class Enrolment
{
    public int Id { get; set; }
    public int ParticipantId { get; set; }
    public User? Participant { get; set; }
    public int EventId { get; set; }
    public Event? Event { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public RaceResult? Result { get; set; }
}

public class RaceResult
{
    public int Id { get; set; }
    public int EnrolmentId { get; set; }
    public Enrolment? Enrolment { get; set; }
    public TimeSpan FinishTime { get; set; }
    public int Position { get; set; }
}
