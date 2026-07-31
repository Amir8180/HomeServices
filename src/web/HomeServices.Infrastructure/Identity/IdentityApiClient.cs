using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HomeServices.Application.Contracts;
using HomeServices.Application.Interfaces;
using HomeServices.Shared.Common;
using HomeServices.Shared.Dtos;
using HomeServices.Shared.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HomeServices.Infrastructure.Identity;

public class IdentityApiClient : IIdentityApiClient
{
    private readonly HttpClient _http;
    private readonly ICacheService _cache;
    private readonly ILogger<IdentityApiClient> _logger;
    private static readonly TimeSpan UserCacheTtl = TimeSpan.FromMinutes(5);

    // Camel-case JSON options match the default ASP.NET Core serialization contract,
    // so deserialization of the API response works regardless of property casing.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public IdentityApiClient(
        HttpClient http,
        ICacheService cache,
        ILogger<IdentityApiClient> logger)
    {
        _http = http;
        _cache = cache;
        _logger = logger;
    }

    private static string NormalizeRoute(string route) => route.StartsWith("/") ? route : "/" + route;

    public async Task<Result<AuthResultDto>> RegisterAsync(
        string fullName, string email, string phoneNumber, string password, UserType userType,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            fullName, email, phoneNumber, password, confirmPassword = password, userType
        };

        var response = await PostJsonAsync(NormalizeRoute("api/auth/register"), payload, cancellationToken);
        var auth = await ReadAuthResponseAsync(response, cancellationToken);
        return auth.Succeeded
            ? Result.Success(auth.Data!)
            : Result.Failure<AuthResultDto>(auth.Message ?? "Registration failed.");
    }

    public async Task<Result<AuthResultDto>> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var payload = new { email, password, rememberMe = false };
        var response = await PostJsonAsync(NormalizeRoute("api/auth/login"), payload, cancellationToken);
        var auth = await ReadAuthResponseAsync(response, cancellationToken);
        return auth.Succeeded
            ? Result.Success(auth.Data!)
            : Result.Failure<AuthResultDto>(auth.Message ?? "Login failed.");
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"identity:user:{id}";
        return await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var response = await _http.GetAsync(NormalizeRoute($"api/users/{id}"), cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GetUserByIdAsync({Id}) returned {Status}.", id, response.StatusCode);
                return null;
            }
            return await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken: cancellationToken);
        }, UserCacheTtl, cancellationToken);
    }

    public async Task<IReadOnlyList<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync(NormalizeRoute("api/users"), cancellationToken);
        if (!response.IsSuccessStatusCode) return Array.Empty<UserDto>();
        var users = await response.Content.ReadFromJsonAsync<List<UserDto>>(cancellationToken: cancellationToken);
        return users ?? new List<UserDto>();
    }

    public async Task<bool> UpdateProfileAsync(Guid id, string fullName, string? avatarUrl, string? phoneNumber, CancellationToken cancellationToken = default)
    {
        var payload = new { fullName, avatarUrl, phoneNumber };
        var response = await _http.PutAsJsonAsync(NormalizeRoute($"api/users/{id}/profile"), payload, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            await _cache.RemoveAsync($"identity:user:{id}", cancellationToken);
            return true;
        }
        _logger.LogWarning("UpdateProfileAsync({Id}) returned {Status}.", id, response.StatusCode);
        return false;
    }

    public async Task<Result> ChangePasswordAsync(Guid id, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var payload = new { currentPassword, newPassword, confirmNewPassword = newPassword };
        var response = await _http.PostAsJsonAsync(NormalizeRoute($"api/users/{id}/change-password"), payload, cancellationToken);
        return response.IsSuccessStatusCode
            ? Result.Success("Password changed.")
            : Result.Failure("Password change failed. Please check your current password.");
    }

    public async Task<bool> ToggleUserStatusAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsync(NormalizeRoute($"api/users/{id}/toggle-status"), null, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            await _cache.RemoveAsync($"identity:user:{id}", cancellationToken);
            return true;
        }
        _logger.LogWarning("ToggleUserStatusAsync({Id}) returned {Status}.", id, response.StatusCode);
        return false;
    }

    // ---------------- helpers ----------------
    private async Task<HttpResponseMessage> PostJsonAsync(string route, object payload, CancellationToken ct)
    {
        var requestUri = _http.BaseAddress == null
            ? route
            : new Uri(_http.BaseAddress, route).ToString();
        try
        {
            return await _http.PostAsJsonAsync(route, payload, ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "PostJsonAsync failed. BaseAddress={BaseAddress} Route={Route} RequestUri={RequestUri} Message={Message}",
                _http.BaseAddress, route, requestUri, ex.Message);
            throw;
        }
    }

    private async Task<(bool Succeeded, AuthResultDto? Data, string? Message)> ReadAuthResponseAsync(HttpResponseMessage response, CancellationToken ct)
    {
        // Always read the raw body first so we never throw on empty/non-JSON responses.
        var rawBody = await response.Content.ReadAsStringAsync(ct);
        var statusCode = (int)response.StatusCode;

        if (string.IsNullOrWhiteSpace(rawBody))
        {
            _logger.LogError(
                "Identity API returned an EMPTY body. Status={Status} BaseAddress={BaseAddress} Reason={Reason}",
                statusCode, _http.BaseAddress, response.ReasonPhrase);
            return (false, null,
                $"Identity service returned an empty response (HTTP {statusCode}). " +
                "Verify the Identity.Api service is running and reachable at the configured BaseAddress.");
        }

        IdentityAuthResponse? apiResponse;
        try
        {
            apiResponse = JsonSerializer.Deserialize<IdentityAuthResponse>(rawBody, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex,
                "Identity API returned non-JSON body. Status={Status} Body={Body}",
                statusCode, rawBody.Length > 500 ? rawBody[..500] + "..." : rawBody);
            return (false, null,
                $"Identity service returned a non-JSON response (HTTP {statusCode}). " +
                "This usually means an HTTP->HTTPS redirect or a reverse proxy error page.");
        }

        if (apiResponse == null)
            return (false, null, "No response from Identity service.");

        if (!apiResponse.Succeeded)
            return (false, null, apiResponse.Message ??
                (apiResponse.Errors.Count > 0 ? string.Join(" | ", apiResponse.Errors) : "Request failed."));

        var data = new AuthResultDto
        {
            AccessToken = apiResponse.AccessToken ?? string.Empty,
            RefreshToken = apiResponse.RefreshToken,
            ExpiresAt = apiResponse.ExpiresAt,
            User = apiResponse.User,
        };
        return (true, data, null);
    }

    private sealed class IdentityAuthResponse
    {
        public bool Succeeded { get; set; }
        public string? Message { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public UserDto? User { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
