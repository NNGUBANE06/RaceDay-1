using System.ComponentModel.DataAnnotations;
namespace RaceDay.Web.Models;
public record CategoryViewModel(int Id,int EventId,string Name);
public record EnrolmentViewModel(int Id,int EventId,string EventName,int CategoryId,string CategoryName,int ParticipantId,string ParticipantName,DateTime EnrolledAt);
public record ResultViewModel(int Id,int EnrolmentId,int EventId,string EventName,int ParticipantId,string ParticipantName,TimeSpan FinishTime,int Position);
public class EventFormModel { public int Id {get;set;} public IFormFile? BannerImage {get;set;} [Required,MaxLength(200)] public string Name {get;set;}=""; [MaxLength(2000)] public string Description {get;set;}=""; [Required] public DateTime Date {get;set;}=DateTime.Today.AddDays(14); [Required] public string Location {get;set;}=""; [Range(.1,1000)] public double DistanceKm {get;set;}=5; [Required] public string EventType {get;set;}="Run"; }
public class CategoryFormModel { [Required] public string Name {get;set;}=""; }
public class EnrolFormModel { [Range(1,int.MaxValue)] public int CategoryId {get;set;} }
public class ResultFormModel { [Range(1,int.MaxValue)] public int EnrolmentId {get;set;} [Required] public string FinishTime {get;set;}="00:30:00"; [Range(1,int.MaxValue)] public int Position {get;set;}=1; }
public class ProfileFormModel { public IFormFile? ProfileImage {get;set;} [Required] public string FirstName {get;set;}=""; [Required] public string LastName {get;set;}=""; public string? NewPassword {get;set;} }
