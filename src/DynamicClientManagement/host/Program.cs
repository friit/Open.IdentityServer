// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Open.IdentityServer.DynamicClientManagement.Configuration.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDynamicClientManagement();

WebApplication app = builder.Build();

app.MapDynamicClientManagement();

app.Run();