using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RaceDay.Web.Services;

public class ApiResult
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string Error { get; set; }
}

public class ApiResult<T> : ApiResult
{
    public T Data { get; set; }
}

/// <summary>Thin wrapper around HttpClient that talks to the RaceDay API and forwards the user's JWT.</summary>
public class ApiClient
{
    public const string TokenClaim = "access_token";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _context;

    public ApiClient(HttpClient http, IHttpContextAccessor context)
    {
        _http = http;
        _context = context;
    }

    public Task<ApiResult<T>> GetAsync<T>(string url) => SendAsync<T>(HttpMethod.Get, url, null);
    public Task<ApiResult<T>> PostAsync<T>(string url, object body) => SendAsync<T>(HttpMethod.Post, url, body);
    public Task<ApiResult<T>> PutAsync<T>(string url, object body) => SendAsync<T>(HttpMethod.Put, url, body);

    public async Task<ApiResult> DeleteAsync(string url)
    {
        var r = await SendAsync<JsonElement?>(HttpMethod.Delete, url, null);
        return new ApiResult { Success = r.Success, StatusCode = r.StatusCode, Error = r.Error };
    }

    public async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, object body)
    {
        using var request = new HttpRequestMessage(method, url);
        if (body != null) request.Content = JsonContent.Create(body, body.GetType(), options: Json);
        return await ExecuteAsync<T>(request);
    }

    /// <summary>Uploads an image to the API (which stores it in Azure Blob Storage or locally) and returns its URL.</summary>
    public async Task<ApiResult<string>> UploadImageAsync(IFormFile file)
    {
        using var content = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
        content.Add(fileContent, "file", file.FileName);

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/files/upload") { Content = content };
        var result = await ExecuteAsync<Shared.FileUploadResultDto>(request);
        return new ApiResult<string>
        {
            Success = result.Success,
            StatusCode = result.StatusCode,
            Error = result.Error,
            Data = result.Data?.Url
        };
    }

    private async Task<ApiResult<T>> ExecuteAsync<T>(HttpRequestMessage request)
    {
        var token = _context.HttpContext?.User.FindFirst(TokenClaim)?.Value;
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            using var response = await _http.SendAsync(request);
            var text = await response.Content.ReadAsStringAsync();
            var status = (int)response.StatusCode;

            if (!response.IsSuccessStatusCode)
                return new ApiResult<T> { Success = false, StatusCode = status, Error = ParseError(text, response.StatusCode) };

            T data = default;
            if (!string.IsNullOrWhiteSpace(text))
                data = JsonSerializer.Deserialize<T>(text, Json);
            return new ApiResult<T> { Success = true, StatusCode = status, Data = data };
        }
        catch (HttpRequestException)
        {
            return new ApiResult<T>
            {
                Success = false,
                StatusCode = 503,
                Error = $"Cannot reach the RaceDay API at {_http.BaseAddress}. Make sure the API is running."
            };
        }
        catch (TaskCanceledException)
        {
            return new ApiResult<T> { Success = false, StatusCode = 504, Error = "The API took too long to respond." };
        }
    }

    private static string ParseError(string text, HttpStatusCode status)
    {
        try
        {
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    var messages = new List<string>();
                    foreach (var prop in errors.EnumerateObject())
                        if (prop.Value.ValueKind == JsonValueKind.Array)
                            foreach (var m in prop.Value.EnumerateArray())
                                if (m.ValueKind == JsonValueKind.String) messages.Add(m.GetString());
                    if (messages.Count > 0) return string.Join(" ", messages);
                }
                if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
                    return detail.GetString();
                if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                    return title.GetString();
            }
        }
        catch (JsonException) { }

        return status switch
        {
            HttpStatusCode.Unauthorized => "Your session has expired. Please log in again.",
            HttpStatusCode.Forbidden => "You do not have permission to do that.",
            HttpStatusCode.NotFound => "The requested item was not found.",
            _ => $"The API returned an error ({(int)status})."
        };
    }
}
