// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Open.IdentityServer.DynamicClientManagement.Endpoints;
using Open.IdentityServer.DynamicClientManagement.Exceptions;
using Open.IdentityServer.DynamicClientManagement.Generators;
using Open.IdentityServer.DynamicClientManagement.Models;
using Open.IdentityServer.DynamicClientManagement.Models.Results;
using Open.IdentityServer.DynamicClientManagement.Parser;
using Open.IdentityServer.DynamicClientManagement.Parser.Models;
using Open.IdentityServer.DynamicClientManagement.Services;
using Open.IdentityServer.DynamicClientManagement.Validation;
using Open.IdentityServer.DynamicClientManagement.Validation.Models;
using Open.IdentityServer.Models;
using Xunit;

namespace Open.IdentityServer.DynamicClientManagement.UnitTests.Endpoints.Register;

public class RegisterEndpointTests
{
    private const string Category = "Register Endpoint";

    private HttpContext _context;

    private ILogger<RegisterEndpoint> _fakeLogger = new NullLogger<RegisterEndpoint>();

    private RegisterEndpoint _subject;

    private IDynamicClientManagementRequestParser _parser;
    private IDynamicClientManagementValidator _validator;
    private IDynamicClientManagementService _service;
    private IDynamicClientManagementResponseGenerator _responseGenerator;

    public RegisterEndpointTests()
    {
        Init();
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [Trait("Category", Category)]
    public async Task process_when_not_POST_should_return_405(string method)
    {
        _context.Request.Method = method;
        await _subject.ProcessRegistrationAsync(_context);
        
        _context.Response.StatusCode.Should().Be(405);
        _context.Response.Headers.Allow.ToString().Should().Be(HttpMethods.Post);
        Mock.Get(_parser)
            .Verify(
                x => x.ParseAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
    }

    [Fact]
    public async Task process_when_parse_throws_exception_should_return_500()
    {
        Mock.Get(_parser)
            .Setup(x => x.ParseAsync(
                _context.Request.Body,
                "application/json",
                _context.RequestAborted))
            .ThrowsAsync(new Exception());
        
        await _subject.ProcessRegistrationAsync(_context);
        
        await ExpectServerErrorResponse(_context);
    }

    [Fact]
    public async Task process_when_parse_throws_dcr_exception_should_return_400_with_error()
    {
        var exception = new DynamicClientTestException();

        Mock.Get(_parser)
            .Setup(x => x.ParseAsync(
                _context.Request.Body,
                "application/json",
                _context.RequestAborted))
            .ThrowsAsync(exception);

        await _subject.ProcessRegistrationAsync(_context);

        await ExpectErrorResponse(_context, exception);
    }

    [Fact]
    public async Task process_when_validation_throws_dcr_exception_should_return_400_with_error()
    {
        var registerRequest = new RegisterRequest();
        var parseResult = CreateParseResult(registerRequest);
        Mock.Get(_parser)
            .Setup(x => x.ParseAsync(
                _context.Request.Body, 
                "application/json", 
                _context.RequestAborted)
            ).ReturnsAsync(parseResult);
        
        var exception = new DynamicClientTestException();
        Mock.Get(_validator)
            .Setup(x => x.ValidateAsync(parseResult, _context.RequestAborted))
            .ThrowsAsync(exception);
        
        await _subject.ProcessRegistrationAsync(_context);
        
        await ExpectErrorResponse(_context, exception);
    }

    [Fact]
    public async Task process_when_validation_fails_should_return_400_with_validation_errors()
    {
        var registerRequest = new RegisterRequest();
        var parseResult = CreateParseResult(registerRequest);
        Mock.Get(_parser)
            .Setup(x => x.ParseAsync(
                _context.Request.Body, 
                "application/json", 
                _context.RequestAborted)
            ).ReturnsAsync(parseResult);
        
        var validationResult = new RegistrationRequestValidationResult(registerRequest, "test_error", "test_description");
        Mock.Get(_validator)
            .Setup(x => x.ValidateAsync(parseResult, _context.RequestAborted))
            .ReturnsAsync(validationResult);

        await _subject.ProcessRegistrationAsync(_context);

        _context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        ExpectNoCacheJsonResponse(_context);

        var response = await ReadResponseJsonAsync(_context);
        response.GetProperty("error").GetString().Should().Be(string.Join(",\n ", validationResult.Errors));        
        Mock.Get(_service)
            .Verify(
                x => x.RegisterAsync(
                    It.IsAny<RegisterRequest>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        Mock.Get(_responseGenerator)
            .Verify(
                x => x.GenerateAsync(
                    It.IsAny<DynamicClientManagementResult>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
    }

    [Fact]
    public async Task process_when_register_throws_exception_should_return_500()
    {
        var registerRequest = new RegisterRequest();
        var parseResult = CreateParseResult(registerRequest);
        Mock.Get(_parser)
            .Setup(x => x.ParseAsync(
                _context.Request.Body, 
                "application/json", 
                _context.RequestAborted)
            ).ReturnsAsync(parseResult);
        
        Mock.Get(_validator)
            .Setup(x => x.ValidateAsync(parseResult, _context.RequestAborted))
            .ReturnsAsync(new RegistrationRequestValidationResult(registerRequest));
        
        Mock.Get(_service)
            .Setup(x => x.RegisterAsync(registerRequest, _context.RequestAborted))
            .ThrowsAsync(new Exception());
        
        await _subject.ProcessRegistrationAsync(_context);
        
        await ExpectServerErrorResponse(_context);
    }

    [Fact]
    public async Task process_when_register_throws_dcr_exception_should_return_400_with_error()
    {
        var registerRequest = new RegisterRequest();
        var parseResult = CreateParseResult(registerRequest);
        Mock.Get(_parser)
            .Setup(x => x.ParseAsync(
                _context.Request.Body, 
                "application/json", 
                _context.RequestAborted)
            ).ReturnsAsync(parseResult);
        
        Mock.Get(_validator)
            .Setup(x => x.ValidateAsync(parseResult, _context.RequestAborted))
            .ReturnsAsync(new RegistrationRequestValidationResult(registerRequest));
        
        var exception = new DynamicClientTestException();
        Mock.Get(_service)
            .Setup(x => x.RegisterAsync(registerRequest, _context.RequestAborted))
            .ThrowsAsync(exception);

        await _subject.ProcessRegistrationAsync(_context);

        await ExpectErrorResponse(_context, exception);
    }
    
    [Fact]
    public async Task process_when_response_generator_throws_exception_should_return_500()
    {
        var registerRequest = new RegisterRequest();
        var parseResult = CreateParseResult(registerRequest);
        Mock.Get(_parser)
            .Setup(x => x.ParseAsync(
                _context.Request.Body, 
                "application/json", 
                _context.RequestAborted)
            ).ReturnsAsync(parseResult);
        
        Mock.Get(_validator)
            .Setup(x => x.ValidateAsync(parseResult, _context.RequestAborted))
            .ReturnsAsync(new RegistrationRequestValidationResult(registerRequest));

        var result = new DynamicClientManagementResult
        {
            Client = new Client(),
            ClientSecret = "secret",
            ClientIdIssuedAt = DateTimeOffset.UtcNow,
            EffectiveRequest = registerRequest,
            RegistrationAccessToken = "token",
            RegistrationClientUri = "uri"
        };
        Mock.Get(_service)
            .Setup(x => x.RegisterAsync(registerRequest, _context.RequestAborted))
            .ReturnsAsync(result);

        Mock.Get(_responseGenerator)
            .Setup(x => x.GenerateAsync(result, _context.RequestAborted))
            .ThrowsAsync(new Exception());
        
        await _subject.ProcessRegistrationAsync(_context);
        
        await ExpectServerErrorResponse(_context);
    }
    
    [Fact]
    public async Task process_when_response_generator_throws_dcr_exception_should_return_400_with_error()
    {
        var registerRequest = new RegisterRequest();
        var parseResult = CreateParseResult(registerRequest);
        Mock.Get(_parser)
            .Setup(x => x.ParseAsync(
                _context.Request.Body, 
                "application/json", 
                _context.RequestAborted)
            ).ReturnsAsync(parseResult);
        
        Mock.Get(_validator)
            .Setup(x => x.ValidateAsync(parseResult, _context.RequestAborted))
            .ReturnsAsync(new RegistrationRequestValidationResult(registerRequest));

        var result = new DynamicClientManagementResult
        {
            Client = new Client(),
            ClientSecret = "secret",
            ClientIdIssuedAt = DateTimeOffset.UtcNow,
            EffectiveRequest = registerRequest,
            RegistrationAccessToken = "token",
            RegistrationClientUri = "uri"
        };
        Mock.Get(_service)
            .Setup(x => x.RegisterAsync(registerRequest, _context.RequestAborted))
            .ReturnsAsync(result);

        var exception = new DynamicClientTestException();
        Mock.Get(_responseGenerator)
            .Setup(x => x.GenerateAsync(result, _context.RequestAborted))
            .ThrowsAsync(exception);
        
        await _subject.ProcessRegistrationAsync(_context);
        
        await ExpectErrorResponse(_context, exception);
    }

    [Fact]
    public async Task process_when_register_succeeds_should_return_201_with_client_metadata()
    {
        var registerRequest = new RegisterRequest();
        var parseResult = CreateParseResult(registerRequest);
        Mock.Get(_parser)
            .Setup(x => x.ParseAsync(
                _context.Request.Body, 
                "application/json", 
                _context.RequestAborted)
            ).ReturnsAsync(parseResult);
        
        Mock.Get(_validator)
            .Setup(x => x.ValidateAsync(parseResult, _context.RequestAborted))
            .ReturnsAsync(new RegistrationRequestValidationResult(registerRequest));

        var result = new DynamicClientManagementResult()
        {
            Client = new Client(),
            ClientSecret = "secret",
            ClientIdIssuedAt = DateTime.UtcNow,
            EffectiveRequest = registerRequest,
            RegistrationAccessToken = "token",
            RegistrationClientUri = "uri"   
        };
        Mock.Get(_service)
            .Setup(x => x.RegisterAsync(registerRequest, _context.RequestAborted))
            .ReturnsAsync(result);

        var registerResponse = new RegisterResponse()
        {
            ClientId = "client_id",
            ClientSecret = "client_secret",
            ClientIdIssuedAt = long.MaxValue,
            ClientSecretExpiresAt = long.MaxValue,
            RedirectUris = [ "https://redirect_uri.test", "https://redirect_uri2.test"],
            GrantTypes = [ "grant_type", "grant_type2"],
            ResponseTypes = [ "response_type", "response_type2"],
            TokenEndpointAuthMethod = "token_endpoint_auth_method",
            ClientName = "client_name",
            Scope = "scope scope2", 
            RegistrationAccessToken = "token",
            RegistrationClientUri = "uri",
            AdditionalData = new Dictionary<string, JsonElement>
            {
                ["custom_metadata"] = JsonDocument.Parse("""{"custom_metadata_value":"custom_metadata_value"}""").RootElement.Clone()
            }
        };
        
        Mock.Get(_responseGenerator)
            .Setup(x => x.GenerateAsync(result, _context.RequestAborted))
            .ReturnsAsync(registerResponse);

        await _subject.ProcessRegistrationAsync(_context);

        _context.Response.StatusCode.Should().Be((int)HttpStatusCode.Created);
        ExpectNoCacheJsonResponse(_context);
        
        var response = await ReadResponseJsonAsync<RegisterResponse>(_context);
        response.Should().BeEquivalentTo(
            registerResponse,
            options => options.Excluding(x => x.AdditionalData));
        response.AdditionalData.Should().ContainKey("custom_metadata");
        response.AdditionalData["custom_metadata"]
            .GetProperty("custom_metadata_value")
            .GetString()
            .Should()
            .Be("custom_metadata_value");
    }
    
    internal void Init()
    {
        _context = new DefaultHttpContext();
        _context.Request.Method = "POST";
        _context.Request.ContentType = "application/json";
        _context.Response.Body = new MemoryStream();
        
        _parser = Mock.Of<IDynamicClientManagementRequestParser>();
        _validator = Mock.Of<IDynamicClientManagementValidator>();
        _service = Mock.Of<IDynamicClientManagementService>();
        _responseGenerator = Mock.Of<IDynamicClientManagementResponseGenerator>();

        _subject = new RegisterEndpoint(_parser, _validator, _service, _responseGenerator, _fakeLogger);
    }

    private async Task ExpectServerErrorResponse(HttpContext context)
    {
        context.Response.StatusCode.Should().Be(500);
        ExpectNoCacheJsonResponse(context);
        
        var response = await ReadResponseJsonAsync(context);
        
        response.GetProperty("error").GetString().Should().Be("server_error");
        response.GetProperty("error_description").GetString().Should().Be("An error occurred processing the request.");
    }
    
    private async Task ExpectErrorResponse(HttpContext context, DynamicClientManagementException exception)
    {
        context.Response.StatusCode.Should().Be((int)exception.StatusCode);
        ExpectNoCacheJsonResponse(context);

        var response = await ReadResponseJsonAsync(context);

        response.GetProperty("error").GetString().Should().Be(exception.Error);
        response.GetProperty("error_description").GetString().Should().Be(exception.ErrorDescription);
    }
    
    private static void ExpectNoCacheJsonResponse(HttpContext context)
    {
        context.Response.ContentType.Should().StartWith("application/json");
        context.Response.Headers.CacheControl.ToString().Should().Be("no-store");
        context.Response.Headers.Pragma.ToString().Should().Be("no-cache");
    }
    
    private static async Task<T> ReadResponseJsonAsync<T>(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        return await JsonSerializer.DeserializeAsync<T>(
            context.Response.Body,
            cancellationToken: TestContext.Current.CancellationToken);
    }
    
    private static async Task<JsonElement> ReadResponseJsonAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        using var document = await JsonDocument.ParseAsync(
            context.Response.Body,
            cancellationToken: TestContext.Current.CancellationToken);

        return document.RootElement.Clone();
    }

    private static RegistrationRequestParseResult CreateParseResult(RegisterRequest request)
    {
        return new RegistrationRequestParseResult
        {
            Request = request
        };
    }

    private class DynamicClientTestException : DynamicClientManagementException
    {
        public DynamicClientTestException() : base("test_error", "test_description")
        {
        }
    }
}
