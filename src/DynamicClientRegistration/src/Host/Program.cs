using Open.IdentityServer.DynamicClientRegistration.Configuration.DependencyInjection;
using Open.IdentityServer.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddIdentityServer()
    .AddInMemoryClients([])
    .AddInMemoryApiScopes([])
    .AddInMemoryApiResources([])
    .AddInMemoryIdentityResources([])
    .AddTestUsers([])
    .AddDynamicClientRegistration();

var app = builder.Build();

app.UseIdentityServer();

app.Run();