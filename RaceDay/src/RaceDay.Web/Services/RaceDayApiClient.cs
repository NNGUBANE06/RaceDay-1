using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RaceDay.Web.Services;

public sealed class RaceDayApiClient
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public RaceDayApiClient(HttpClient http, IHttpContextAccessor accessor) { _http = http; _accessor = accessor; }

    private void AttachSession()
    {
        var cookie = _accessor.HttpContext?.Session.GetString("ApiCookie");
        _http.DefaultRequestHeaders.Remove("Cookie");
        if (!string.IsNullOrWhiteSpace(cookie)) _http.DefaultRequestHeaders.Add("Cookie", cookie);
    }
    public async Task<T?> Get<T>(string path)
    {
        AttachSession(); using var response = await _http.GetAsync(path);
        if (!response.IsSuccessStatusCode) return default;
        return await response.Content.ReadFromJsonAsync<T>(Json);
    }
    public async Task<(bool Ok, string? Error)> Send(string path, HttpMethod method, object? body = null, bool saveCookie = false)
    {
        AttachSession();
        using var request = new HttpRequestMessage(method, path);
        if (body is not null) request.Content = new StringContent(JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json");
        using var response = await _http.SendAsync(request);
        if (saveCookie && response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            var sessionCookie = values.FirstOrDefault(x => x.StartsWith("RaceDay.Session=", StringComparison.Ordinal));
            if (sessionCookie is not null) _accessor.HttpContext?.Session.SetString("ApiCookie", sessionCookie.Split(';')[0]);
        }
        if (response.IsSuccessStatusCode) return (true, null);
        return (false, await response.Content.ReadAsStringAsync());
    }
    public void ClearSession() => _accessor.HttpContext?.Session.Remove("ApiCookie");
}
