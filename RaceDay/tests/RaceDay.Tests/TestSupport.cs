using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;

namespace RaceDay.Tests;

/// <summary>Boots the real API in memory, swapping SQL Server for an isolated in-memory database.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "RaceDayTests-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureTestServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(_dbName));
        });
    }

    /// <summary>A new client with its own cookie jar (i.e. its own session).</summary>
    public HttpClient NewClient() =>
        CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<HttpClient> RegisterAndLogin(string role, string? email = null, string password = "Passw0rd!123")
    {
        email ??= $"{role.ToLower()}-{Guid.NewGuid():N}@raceday.test";
        var client = NewClient();
        var reg = await client.PostAsJsonAsync("/api/auth/register",
            new { firstName = "Test", lastName = role, email, password, role });
        Assert.Equal(HttpStatusCode.Created, reg.StatusCode);
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return client;
    }

    public static async Task<T> Read<T>(HttpResponseMessage res) =>
        (await res.Content.ReadFromJsonAsync<T>(Json))!;

    public static object NewEvent(string name = "City 10km") => new
    {
        name,
        description = "Fun run through the city",
        date = DateTime.UtcNow.AddDays(30),
        location = "Johannesburg",
        distanceKm = 10,
        eventType = "Run"
    };

    public async Task<int> CreateEvent(HttpClient organiser, string name = "City 10km")
    {
        var res = await organiser.PostAsJsonAsync("/api/events", NewEvent(name));
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await Read<EventDto>(res)).Id;
    }

    public async Task<int> CreateCategory(HttpClient organiser, int eventId, string name = "Senior")
    {
        var res = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new { name });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await Read<CategoryDto>(res)).Id;
    }

    public record EventDto(int Id, string Name, string Location, double DistanceKm, string EventType, int OrganiserId);
    public record CategoryDto(int Id, int EventId, string Name);
    public record EnrolmentDto(int Id, int EventId, int CategoryId, int ParticipantId, string ParticipantName);
    public record ResultDto(int Id, int EnrolmentId, int Position, string FinishTime);
    public record ProfileDto(int Id, string FirstName, string LastName, string Email, string Role);
}
