using BlazorWasmAuthentication.Services;
using System.Net.Http.Headers;

namespace BlazorWasmAuthentication.Handlers;

public class AuthenticationHandler(
    IAuthenticationService authService,
    IConfiguration config) : DelegatingHandler
{

    private readonly IAuthenticationService _authService = authService;
    private readonly IConfiguration _config = config;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var jwt = await _authService.GetJwtAsync();

        var serverUrl = _config["ServerUrl"]
            ?? throw new InvalidOperationException("ServerUrl not configured");

        var isToAuthServer = request.RequestUri?.AbsoluteUri.StartsWith(serverUrl) ?? false;

        if (string.IsNullOrWhiteSpace(jwt) == false && isToAuthServer)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
