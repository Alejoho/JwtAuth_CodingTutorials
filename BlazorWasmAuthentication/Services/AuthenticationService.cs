using Blazored.SessionStorage;
using BlazorWasmAuthentication.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;

namespace BlazorWasmAuthentication.Services;

public class AuthenticationService(IHttpClientFactory factory, ISessionStorageService sessionStorage) : IAuthenticationService
{
    private readonly IHttpClientFactory _factory = factory;
    private readonly ISessionStorageService _sessionStorage = sessionStorage;
    private const string JwtKey = nameof(JwtKey);
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
        await _sessionStorage.RemoveItemAsync(JwtKey);
        _jwtCache = null;
        LoginChanged?.Invoke(null);
    }

    private static string GetUsername(string jwt)
    {
        var token = new JwtSecurityToken(jwt);
        var username = token.Claims.First(c => c.Type == ClaimTypes.Name).Value;
        return username;
    }
}
