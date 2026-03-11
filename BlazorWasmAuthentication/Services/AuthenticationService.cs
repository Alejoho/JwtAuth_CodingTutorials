using Blazored.SessionStorage;
using BlazorWasmAuthentication.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;

namespace BlazorWasmAuthentication.Services;

public class AuthenticationService(
    IHttpClientFactory factory,
    ISessionStorageService sessionStorage) : IAuthenticationService
{
    private readonly IHttpClientFactory _factory = factory;
    private readonly ISessionStorageService _sessionStorage = sessionStorage;
    private const string JwtKey = nameof(JwtKey);
    private const string RefreshKey = nameof(RefreshKey);
    private string? _jwtCache;
    public event Action<string?>? LoginChanged;

    public async Task<DateTime> LoginAsync(LoginDto dto)
    {
        var client = _factory.CreateClient(ConstantNames.ServerApiHttpClient);

        var response = await client.PostAsync(
            "api/authentication/login",
            JsonContent.Create(dto));

        if (response.IsSuccessStatusCode is false)
        {
            throw new UnauthorizedAccessException("Login failed");
        }

        var content = await response.Content.ReadFromJsonAsync<LoginResponseDto>();

        if (content is null)
        {
            throw new InvalidDataException();
        }

        await _sessionStorage.SetItemAsync(JwtKey, content.JwtToken);
        await _sessionStorage.SetItemAsync(RefreshKey, content.RefreshToken);

        LoginChanged?.Invoke(GetUsername(content.JwtToken));

        return content.Expiration;
    }

    public async ValueTask<string> GetJwtAsync()
    {
        if (string.IsNullOrWhiteSpace(_jwtCache))
        {
            _jwtCache = await _sessionStorage.GetItemAsync<string>(JwtKey);
        }

        return _jwtCache;
    }

    public async Task LogOutAsync()
    {
        if (string.IsNullOrEmpty(_jwtCache))
        {
            return;
        }

        var client = _factory.CreateClient(ConstantNames.ServerApiHttpClient);
        var response = await client.DeleteAsync("api/authentication/revoke");

        await Console.Out.WriteLineAsync($"Response status code from the logout: {response.StatusCode}");

        if (response.IsSuccessStatusCode is not true)
        {
            return;
        }

        await _sessionStorage.RemoveItemAsync(JwtKey);
        await _sessionStorage.RemoveItemAsync(RefreshKey);

        _jwtCache = null;

        LoginChanged?.Invoke(null);
    }

    public string GetUsername(string jwt)
    {
        var token = new JwtSecurityToken(jwt);
        var username = token.Claims.First(c => c.Type == ClaimTypes.Name).Value;
        return username;
    }

    public async Task<bool> RefreshAsync()
    {
        // get the access token
        var accessToken = await _sessionStorage.GetItemAsync<string>(JwtKey);
        // get the refresh token
        var refreshToken = await _sessionStorage.GetItemAsync<string>(RefreshKey);
        // create the dto
        var dto = new RefreshDto(accessToken, refreshToken);
        // post to the backend
        var client = _factory.CreateClient(ConstantNames.ServerApiHttpClient);
        var response = await client.PostAsync(
            "api/authentication/refresh",
            JsonContent.Create(dto));
        // ensure success
        if (response.IsSuccessStatusCode is not true)
        {
            // TODO: Maybe move this line to the authentication handler
            // if not logout the user
            await LogOutAsync();
            return false;
        }
        // get content
        var content = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        // save both token to session storage
        if (content is null)
        {
            throw new InvalidDataException();
        }

        await _sessionStorage.SetItemAsync(JwtKey, content.JwtToken);
        await _sessionStorage.SetItemAsync(RefreshKey, content.RefreshToken);
        // populate the jwtcache
        _jwtCache = content.JwtToken;
        // and return true
        return true;
    }
}
