using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Tests;

public class EnrolmentTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _f;
    public EnrolmentTests(ApiFactory f) => _f = f;

    [Fact]
    public async Task Participant_CanEnrol_AndEnrolmentIsRecordedCorrectly()
    {
        var org = await _f.RegisterAndLogin("Organiser");
        var part = await _f.RegisterAndLogin("Participant");
        var eventId = await _f.CreateEvent(org);
        var catId = await _f.CreateCategory(org, eventId, "21km");

        var res = await part.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId = catId });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var enrolment = await ApiFactory.Read<ApiFactory.EnrolmentDto>(res);

        // Link between participant, event and category is stored
        Assert.Equal(eventId, enrolment.EventId);
        Assert.Equal(catId, enrolment.CategoryId);

        var mine = await ApiFactory.Read<List<ApiFactory.EnrolmentDto>>(await part.GetAsync("/api/enrolments/mine"));
        var single = Assert.Single(mine);
        Assert.Equal(enrolment.Id, single.Id);
        Assert.Equal(enrolment.ParticipantId, single.ParticipantId);
    }

    [Fact]
    public async Task Organiser_CanViewAllEnrolmentsForOwnEvent()
    {
        var org = await _f.RegisterAndLogin("Organiser");
        var p1 = await _f.RegisterAndLogin("Participant");
        var p2 = await _f.RegisterAndLogin("Participant");
        var eventId = await _f.CreateEvent(org);
        var catId = await _f.CreateCategory(org, eventId);

        await p1.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId = catId });
        await p2.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId = catId });

        var res = await org.GetAsync($"/api/events/{eventId}/enrolments");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal(2, (await ApiFactory.Read<List<ApiFactory.EnrolmentDto>>(res)).Count);
    }

    [Fact]
    public async Task Organiser_CannotViewEnrolmentsOfAnotherOrganisersEvent()
    {
        var owner = await _f.RegisterAndLogin("Organiser");
        var other = await _f.RegisterAndLogin("Organiser");
        var eventId = await _f.CreateEvent(owner);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"/api/events/{eventId}/enrolments")).StatusCode);
    }

    [Fact]
    public async Task Enrol_TwiceInSameEvent_ReturnsConflict()
    {
        var org = await _f.RegisterAndLogin("Organiser");
        var part = await _f.RegisterAndLogin("Participant");
        var eventId = await _f.CreateEvent(org);
        var catId = await _f.CreateCategory(org, eventId);

        await part.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId = catId });
        var again = await part.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId = catId });
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Enrol_WithCategoryFromAnotherEvent_ReturnsBadRequest()
    {
        var org = await _f.RegisterAndLogin("Organiser");
        var part = await _f.RegisterAndLogin("Participant");
        var eventA = await _f.CreateEvent(org, "A");
        var eventB = await _f.CreateEvent(org, "B");
        var catB = await _f.CreateCategory(org, eventB);

        var res = await part.PostAsJsonAsync($"/api/events/{eventA}/enrolments", new { categoryId = catB });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Enrol_UnknownEvent_ReturnsNotFound()
    {
        var part = await _f.RegisterAndLogin("Participant");
        var res = await part.PostAsJsonAsync("/api/events/999999/enrolments", new { categoryId = 1 });
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Enrol_Unauthenticated_ReturnsUnauthorized()
    {
        var res = await _f.NewClient().PostAsJsonAsync("/api/events/1/enrolments", new { categoryId = 1 });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ---- Results ----

    [Fact]
    public async Task Organiser_CapturesResult_ParticipantSeesOnlyOwnResult()
    {
        var org = await _f.RegisterAndLogin("Organiser");
        var p1 = await _f.RegisterAndLogin("Participant");
        var p2 = await _f.RegisterAndLogin("Participant");
        var eventId = await _f.CreateEvent(org);
        var catId = await _f.CreateCategory(org, eventId);

        var e1 = await ApiFactory.Read<ApiFactory.EnrolmentDto>(
            await p1.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId = catId }));
        var e2 = await ApiFactory.Read<ApiFactory.EnrolmentDto>(
            await p2.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId = catId }));

        var r1 = await org.PostAsJsonAsync($"/api/events/{eventId}/results",
            new { enrolmentId = e1.Id, finishTime = "00:45:10", position = 1 });
        Assert.Equal(HttpStatusCode.Created, r1.StatusCode);
        await org.PostAsJsonAsync($"/api/events/{eventId}/results",
            new { enrolmentId = e2.Id, finishTime = "00:52:30", position = 2 });

        var mine = await ApiFactory.Read<List<ApiFactory.ResultDto>>(await p1.GetAsync("/api/results/mine"));
        var single = Assert.Single(mine);
        Assert.Equal(1, single.Position);
        Assert.Equal(e1.Id, single.EnrolmentId);

        var all = await ApiFactory.Read<List<ApiFactory.ResultDto>>(await org.GetAsync($"/api/events/{eventId}/results"));
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task Participant_CannotCaptureResults()
    {
        var org = await _f.RegisterAndLogin("Organiser");
        var part = await _f.RegisterAndLogin("Participant");
        var eventId = await _f.CreateEvent(org);
        var catId = await _f.CreateCategory(org, eventId);
        var e = await ApiFactory.Read<ApiFactory.EnrolmentDto>(
            await part.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId = catId }));

        var res = await part.PostAsJsonAsync($"/api/events/{eventId}/results",
            new { enrolmentId = e.Id, finishTime = "00:30:00", position = 1 });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_Twice_ReturnsConflict()
    {
        var org = await _f.RegisterAndLogin("Organiser");
        var part = await _f.RegisterAndLogin("Participant");
        var eventId = await _f.CreateEvent(org);
        var catId = await _f.CreateCategory(org, eventId);
        var e = await ApiFactory.Read<ApiFactory.EnrolmentDto>(
            await part.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId = catId }));

        var body = new { enrolmentId = e.Id, finishTime = "00:30:00", position = 1 };
        await org.PostAsJsonAsync($"/api/events/{eventId}/results", body);
        var again = await org.PostAsJsonAsync($"/api/events/{eventId}/results", body);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }
}
