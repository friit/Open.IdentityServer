// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Open.IdentityServer.DynamicClientManagement.IntegrationTests;

public class RegisterEndpointGetTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public RegisterEndpointGetTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RegisterEndpoint_WhenGETManagementRouteIsCalledWithoutBearerToken_ShouldReturnMissingBearerTokenError()
    {
        var response = await _client.GetAsync(
            "/connect/register/client-id",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var responseDocument = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var responseJson = responseDocument.RootElement;

        responseJson.GetProperty("error").GetString().Should().Be("missing_bearer_token");
    }
}
