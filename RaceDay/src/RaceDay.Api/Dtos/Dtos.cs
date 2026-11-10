using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public record RegisterRequest(
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required] string Role, string? ProfileImageUrl = null);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public record ProfileResponse(int Id, string FirstName, string LastName, string Email, string Role);

public record UpdateProfileRequest(
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    string? NewPassword);

public record EventRequest(
    [Required, MaxLength(200)] string Name,
    [MaxLength(2000)] string? Description,
    DateTime Date,
    [Required, MaxLength(200)] string Location,
    [Range(0.1, 1000)] double DistanceKm,
    EventType EventType);

public record EventResponse(
    int Id, string Name, string Description, DateTime Date, string Location,
    double DistanceKm, EventType EventType, int OrganiserId, string? BannerImageUrl = null);

public record CategoryRequest([Required, MaxLength(100)] string Name);

public record CategoryResponse(int Id, int EventId, string Name);

public record EnrolRequest([Range(1, int.MaxValue)] int CategoryId);

public record EnrolmentResponse(
    int Id, int EventId, string EventName, int CategoryId, string CategoryName,
    int ParticipantId, string ParticipantName, DateTime EnrolledAt);

public record ResultRequest(
    [Range(1, int.MaxValue)] int EnrolmentId,
    TimeSpan FinishTime,
    [Range(1, int.MaxValue)] int Position);

public record ResultResponse(
    int Id, int EnrolmentId, int EventId, string EventName, int ParticipantId,
    string ParticipantName, TimeSpan FinishTime, int Position);
