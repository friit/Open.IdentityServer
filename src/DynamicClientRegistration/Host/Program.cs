
using Open.IdentityServer.DynamicClientRegistration.Configuration.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDynamicClientRegistration();

WebApplication app = builder.Build();

app.MapDynamicClientRegistration();

app.Run();