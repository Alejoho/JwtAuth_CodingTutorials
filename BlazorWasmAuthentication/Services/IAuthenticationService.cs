using BlazorWasmAuthentication.DTOs;
using System.Net;

namespace BlazorWasmAuthentication.Services;

public interface IAuthenticationService
{
    event Action<string?>? LoginChanged;

    ValueTask<string> GetJwtAsync();
    Task<DateTime> LoginAsync(LoginDto dto);
    /// <summary>
    /// Log out the user as an async operation
    /// </summary>
    /// <param name="callApi">A flag to specify if the log out endpoint of the api should be called.</param>
    /// <returns>The Task object representing the async operation</returns>
    Task LogOutAsync(bool callApi);
    string GetUsername(string jwt);
    /// <summary>
    /// Refresh the access and refresh tokens.
    /// </summary>
    /// <returns>Returns the <c>StatusCode</c> of the call to the backend</returns>
    /// <exception cref="InvalidDataException">Throws this is the content of the response is null</exception>
    Task<HttpStatusCode> RefreshAsync();
}