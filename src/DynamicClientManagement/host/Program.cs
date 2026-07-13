
using Open.IdentityServer.DynamicClientManagement.Configuration.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDynamicClientManagement();

WebApplication app = builder.Build();

app.MapDynamicClientManagement();

app.Run();