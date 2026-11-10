using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;
public record LoginViewModel([Required, EmailAddress] string Email, [Required] string Password);
public record RegisterViewModel([Required] string FirstName, [Required] string LastName, [Required, EmailAddress] string Email, [Required, MinLength(8)] string Password, [Required] string Role);
public record ProfileViewModel(int Id, string FirstName, string LastName, string Email, string Role);
public record EventViewModel(int Id, string Name, string Description, DateTime Date, string Location, double DistanceKm, string EventType, int OrganiserId);
