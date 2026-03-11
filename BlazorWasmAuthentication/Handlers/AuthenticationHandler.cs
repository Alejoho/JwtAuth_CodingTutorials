using BlazorWasmAuthentication.Services;
using System.Net;
using System.Net.Http.Headers;

namespace BlazorWasmAuthentication.Handlers;

public class AuthenticationHandler(
    IAuthenticationService authService,
    IConfiguration config) : DelegatingHandler
{

    private readonly IAuthenticationService _authService = authService;
    private readonly IConfiguration _config = config;
    private bool _refreshing = false;

    // TODO: Refactor this method to separate it into 2.
    // the new method should return a Task<HttpResponseMessage> and not be async
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

        var response = await base.SendAsync(request, cancellationToken);

        // TODO: Create a method for this condition
        if (_refreshing is false
            && string.IsNullOrEmpty(jwt) is false
            && response.StatusCode is HttpStatusCode.Unauthorized)
        {
            try
            {
                _refreshing = true;

                if (await _authService.RefreshAsync())
                {
                    jwt = await _authService.GetJwtAsync();

                    if (string.IsNullOrWhiteSpace(jwt) == false && isToAuthServer)
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
                    }

                    response = await base.SendAsync(request, cancellationToken);
                }
            }
            finally
            {
                _refreshing = false;
            }
        }

        return response;
    }
}
