using Blazored.SessionStorage;
using BlazorWasmAuthentication;
using BlazorWasmAuthentication.Handlers;
using BlazorWasmAuthentication.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var server = builder.Configuration["ServerUrl"]
        ?? throw new InvalidOperationException("ServerUrl not configured");

builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
builder.Services.AddBlazoredSessionStorageAsSingleton();

builder.Services.AddTransient<AuthenticationHandler>();

builder.Services.AddHttpClient(ConstantNames.ServerApiHttpClient, opts =>
{
    opts.BaseAddress = new Uri(server);
}).AddHttpMessageHandler<AuthenticationHandler>();

builder.Services.AddScoped<IReviewRepository, ReviewRepository>();

await builder.Build().RunAsync();
