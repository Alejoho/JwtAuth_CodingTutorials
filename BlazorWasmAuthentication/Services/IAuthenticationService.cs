using BlazorWasmAuthentication.DTOs;

namespace BlazorWasmAuthentication.Services;

public interface IAuthenticationService
{
    event Action<string?>? LoginChanged;

    ValueTask<string> GetJwtAsync();
    Task<DateTime> LoginAsync(LoginDto dto);
    Task LogOutAsync();
    string GetUsername(string jwt);
    /// <summary>
    /// Refresh the tokens to access the backend.
    /// </summary>
    /// <returns>Returns true if the refresh was successfull, otherwise false.</returns>
    /// <exception cref="InvalidDataException">Throws this is the content of the response is null</exception>
    Task<bool> RefreshAsync();
}