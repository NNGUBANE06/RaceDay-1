using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Tests;

public class EventTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _f;
    public EventTests(ApiFactory f) => _f = f;

    // ---- Organiser can manage events ----

    [Fact]
    public async Task Organiser_CanCreateEvent()
    {
        var org = await _f.RegisterAndLogin("Organiser");
        var res = await org.PostAsJsonAsync("/api/events", ApiFactory.NewEvent("Soweto Marathon"));
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var ev = await ApiFactory.Read<ApiFactory.EventDto>(res);
        Assert.Equal("Soweto Marathon", ev.Name);
        Assert.Equal("Run", ev.EventType);
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnEvent()
    {
        var org = await _f.RegisterAndLogin("Organiser");
        var id = await _f.CreateEvent(org);
        var res = await org.PutAsJsonAsync($"/api/events/{id}", new
        {
            name = "Renamed", description = "d", date = DateTime.UtcNow.AddDays(10),
            location = "Pretoria", distanceKm = 21, eventType = "Cycle"
        });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("Renamed", (await ApiFactory.Read<ApiFactory.EventDto>(res)).Name);
    }

    [Fact]
    public async Task Organiser_CanDeleteOwnEvent()
    {
        var org = await _f.RegisterAndLogin("Organiser");
        var id = await _f.CreateEvent(org);
        Assert.Equal(HttpStatusCode.NoContent, (await org.DeleteAsync($"/api/events/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await org.GetAsync($"/api/events/{id}")).StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotUpdateOrDeleteAnotherOrganisersEvent()
    {
        var owner = await _f.RegisterAndLogin("Organiser");
        var other = await _f.RegisterAndLogin("Organiser");
        var id = await _f.CreateEvent(owner);

        var put = await other.PutAsJsonAsync($"/api/events/{id}", ApiFactory.NewEvent("Hijacked"));
        Assert.Equal(HttpStatusCode.Forbidden, put.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.DeleteAsync($"/api/events/{id}")).StatusCode);
    }

    [Fact]
    public async Task CreateEvent_InvalidData_ReturnsBadRequest()
    {
        var org = await _f.RegisterAndLogin("Organiser");
        var res = await org.PostAsJsonAsync("/api/events", new
        {
            name = "", description = "", date = DateTime.UtcNow, location = "", distanceKm = 0, eventType = "Run"
        });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task GetEvent_UnknownId_ReturnsNotFound()
    {
        var org = await _f.RegisterAndLogin("Organiser");
        Assert.Equal(HttpStatusCode.NotFound, (await org.GetAsync("/api/events/999999")).StatusCode);
    }

    // ---- Both roles can view ----

    [Fact]
    public async Task BothRoles_CanViewEvents()
    {
        var org = await _f.RegisterAndLogin("Organiser");
        var part = await _f.RegisterAndLogin("Participant");
        await _f.CreateEvent(org, "Viewable Event");

        foreach (var client in new[] { org, part })
        {
            var res = await client.GetAsync("/api/events");
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var list = await ApiFactory.Read<List<ApiFactory.EventDto>>(res);
            Assert.Contains(list, e => e.Name == "Viewable Event");
        }
    }

    // ---- Different roles are rejected ----

    [Fact]
    public async Task Participant_CannotCreateUpdateOrDeleteEvents()
    {
        var org = await _f.RegisterAndLogin("Organiser");
        var part = await _f.RegisterAndLogin("Participant");
        var id = await _f.CreateEvent(org);

        Assert.Equal(HttpStatusCode.Forbidden, (await part.PostAsJsonAsync("/api/events", ApiFactory.NewEvent())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await part.PutAsJsonAsync($"/api/events/{id}", ApiFactory.NewEvent())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await part.DeleteAsync($"/api/events/{id}")).StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_CannotCreateEvent()
    {
        var res = await _f.NewClient().PostAsJsonAsync("/api/events", ApiFactory.NewEvent());
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotDefineCategories()
    {
        var org = await _f.RegisterAndLogin("Organiser");
        var part = await _f.RegisterAndLogin("Participant");
        var id = await _f.CreateEvent(org);
        var res = await part.PostAsJsonAsync($"/api/events/{id}/categories", new { name = "Sneaky" });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEnrolOrViewParticipantOnlyEndpoints()
    {
        var org = await _f.RegisterAndLogin("Organiser");
        var id = await _f.CreateEvent(org);
        var cat = await _f.CreateCategory(org, id);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await org.PostAsJsonAsync($"/api/events/{id}/enrolments", new { categoryId = cat })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await org.GetAsync("/api/enrolments/mine")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await org.GetAsync("/api/results/mine")).StatusCode);
    }

    [Fact]
    public async Task Participant_CannotViewOrganiserOnlyEndpoints()
    {
        var org = await _f.RegisterAndLogin("Organiser");
        var part = await _f.RegisterAndLogin("Participant");
        var id = await _f.CreateEvent(org);

        Assert.Equal(HttpStatusCode.Forbidden, (await part.GetAsync($"/api/events/{id}/enrolments")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await part.GetAsync($"/api/events/{id}/results")).StatusCode);
    }

    // ---- Categories ----

    [Fact]
    public async Task Categories_OrganiserDefines_BothRolesCanView()
    {
        var org = await _f.RegisterAndLogin("Organiser");
        var part = await _f.RegisterAndLogin("Participant");
        var id = await _f.CreateEvent(org);
        await _f.CreateCategory(org, id, "Under 20");
        await _f.CreateCategory(org, id, "10km");

        foreach (var client in new[] { org, part })
        {
            var cats = await ApiFactory.Read<List<ApiFactory.CategoryDto>>(await client.GetAsync($"/api/events/{id}/categories"));
            Assert.Equal(2, cats.Count);
        }
    }

    [Fact]
    public async Task Categories_DuplicateName_ReturnsConflict()
    {
        var org = await _f.RegisterAndLogin("Organiser");
        var id = await _f.CreateEvent(org);
        await _f.CreateCategory(org, id, "Senior");
        var dup = await org.PostAsJsonAsync($"/api/events/{id}/categories", new { name = "Senior" });
        Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);
    }
}
