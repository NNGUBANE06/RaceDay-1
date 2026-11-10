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

    private HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        var cookie = _accessor.HttpContext?.Session.GetString("ApiCookie");
        if (!string.IsNullOrWhiteSpace(cookie)) request.Headers.TryAddWithoutValidation("Cookie", cookie);
        return request;
    }

    public async Task<T?> Get<T>(string path)
    {
        using var request = CreateRequest(HttpMethod.Get, path);
        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return default;
        return await response.Content.ReadFromJsonAsync<T>(Json);
    }

    public async Task<T?> Post<T>(string path, object body)
    {
        using var request = CreateRequest(HttpMethod.Post, path);
        request.Content = new StringContent(JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json");
        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return default;
        return await response.Content.ReadFromJsonAsync<T>(Json);
    }

    public async Task<(bool Ok, string? Error)> Send(string path, HttpMethod method, object? body = null, bool saveCookie = false)
    {
        using var request = CreateRequest(method, path);
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

    public async Task<(bool Ok, string? Error)> Upload(string path, IFormFile file)
    {
        using var request = CreateRequest(HttpMethod.Post, path);
        using var content = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", Path.GetFileName(file.FileName));
        request.Content = content;
        using var response = await _http.SendAsync(request);
        return response.IsSuccessStatusCode ? (true, null) : (false, await response.Content.ReadAsStringAsync());
    }

    public void ClearSession() => _accessor.HttpContext?.Session.Remove("ApiCookie");
}