using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using RaceDay.Api.Data;

namespace RaceDay.Tests;

public class AuthTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _f;
    public AuthTests(ApiFactory f) => _f = f;

    private static object Reg(string email, string role = "Participant", string password = "Passw0rd!123") =>
        new { firstName = "Jane", lastName = "Doe", email, password, role };

    // ---- Registration ----

    [Theory]
    [InlineData("Organiser")]
    [InlineData("Participant")]
    public async Task Register_ValidUser_ReturnsCreated(string role)
    {
        var res = await _f.NewClient().PostAsJsonAsync("/api/auth/register", Reg($"{Guid.NewGuid():N}@x.test", role));
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var profile = await ApiFactory.Read<ApiFactory.ProfileDto>(res);
        Assert.Equal(role, profile.Role);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var email = $"{Guid.NewGuid():N}@x.test";
        var c = _f.NewClient();
        await c.PostAsJsonAsync("/api/auth/register", Reg(email));
        var second = await c.PostAsJsonAsync("/api/auth/register", Reg(email));
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_ReturnsBadRequest()
    {
        var res = await _f.NewClient().PostAsJsonAsync("/api/auth/register", Reg($"{Guid.NewGuid():N}@x.test", "Admin"));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_ReturnsBadRequest()
    {
        var res = await _f.NewClient().PostAsJsonAsync("/api/auth/register",
            Reg($"{Guid.NewGuid():N}@x.test", "Participant", "short"));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NotPlainText()
    {
        var email = $"{Guid.NewGuid():N}@x.test";
        await _f.NewClient().PostAsJsonAsync("/api/auth/register", Reg(email, password: "Passw0rd!123"));

        using var scope = _f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = db.Users.Single(u => u.Email == email);
        Assert.NotEqual("Passw0rd!123", user.PasswordHash);
        Assert.DoesNotContain("Passw0rd!123", user.PasswordHash);
    }

    // ---- Login ----

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkAndSessionCookie()
    {
        var email = $"{Guid.NewGuid():N}@x.test";
        var c = _f.NewClient();
        await c.PostAsJsonAsync("/api/auth/register", Reg(email));
        var res = await c.PostAsJsonAsync("/api/auth/login", new { email, password = "Passw0rd!123" });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Contains(res.Headers, h => h.Key == "Set-Cookie");
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var email = $"{Guid.NewGuid():N}@x.test";
        var c = _f.NewClient();
        await c.PostAsJsonAsync("/api/auth/register", Reg(email));
        var res = await c.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword1" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownUser_ReturnsUnauthorized()
    {
        var res = await _f.NewClient().PostAsJsonAsync("/api/auth/login",
            new { email = "nobody@x.test", password = "Passw0rd!123" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ---- Session / authenticated vs unauthenticated ----

    [Theory]
    [InlineData("/api/events")]
    [InlineData("/api/profile")]
    [InlineData("/api/enrolments/mine")]
    [InlineData("/api/results/mine")]
    public async Task ProtectedEndpoints_Unauthenticated_Return401(string url)
    {
        var res = await _f.NewClient().GetAsync(url);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Session_PersistsAcrossRequests()
    {
        var c = await _f.RegisterAndLogin("Participant");
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/api/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/api/events")).StatusCode);
    }

    [Fact]
    public async Task Logout_EndsSession()
    {
        var c = await _f.RegisterAndLogin("Participant");
        Assert.Equal(HttpStatusCode.NoContent, (await c.PostAsync("/api/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/profile")).StatusCode);
    }

    // ---- Profile ----

    [Fact]
    public async Task Profile_UserCanViewAndUpdateOwnProfile()
    {
        var c = await _f.RegisterAndLogin("Organiser");
        var put = await c.PutAsJsonAsync("/api/profile", new { firstName = "New", lastName = "Name" });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var profile = await ApiFactory.Read<ApiFactory.ProfileDto>(await c.GetAsync("/api/profile"));
        Assert.Equal("New", profile.FirstName);
        Assert.Equal("Name", profile.LastName);
    }
}
