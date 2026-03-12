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
    private string _jwt = string.Empty;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await TryAddAuthorizationHeader(request);

        var response = await base.SendAsync(request, cancellationToken);

        var needsRefresh = _refreshing is false
            && string.IsNullOrEmpty(_jwt) is false
            && response.StatusCode is HttpStatusCode.Unauthorized;

        if (needsRefresh is true)
        {
            try
            {
                _refreshing = true;

                var refreshResponse = await _authService.RefreshAsync();

                if (refreshResponse is HttpStatusCode.OK)
                {
                    await TryAddAuthorizationHeader(request);

                    response = await base.SendAsync(request, cancellationToken);
                }
                else if (refreshResponse is HttpStatusCode.Forbidden)
                {
                    await _authService.LogOutAsync(callApi: false);
                }
                else if (refreshResponse is HttpStatusCode.Unauthorized)
                {
                    // just to check
                    throw new Exception();
                }
            }
            finally
            {
                _refreshing = false;
            }
        }

        return response;
    }

    private async Task TryAddAuthorizationHeader(HttpRequestMessage request)
    {
        _jwt = await _authService.GetJwtAsync();

        var serverUrl = _config["ServerUrl"]
            ?? throw new InvalidOperationException("ServerUrl not configured");

        var isToAuthServer = request.RequestUri?.AbsoluteUri.StartsWith(serverUrl) ?? false;

        if (string.IsNullOrWhiteSpace(_jwt) == false && isToAuthServer)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _jwt);
        }
    }
}
