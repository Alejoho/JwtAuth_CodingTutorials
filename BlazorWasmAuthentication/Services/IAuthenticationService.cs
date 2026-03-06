using BlazorWasmAuthentication.DTOs;

namespace BlazorWasmAuthentication.Services;

public interface IAuthenticationService
{
    event Action<string?>? LoginChanged;

    ValueTask<string> GetJwtAsync();
    Task<DateTime> LoginAsync(LoginDto dto);
    Task LogOutAsync();
}