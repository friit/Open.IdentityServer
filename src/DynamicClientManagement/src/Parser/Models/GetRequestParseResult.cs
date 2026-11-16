namespace Open.IdentityServer.DynamicClientManagement.Parser.Models;

public sealed class GetRequestParseResult
{
    /// <summary>
    /// Gets the client ID extracted from the request.
    /// </summary>
    public required string ClientId { get; init; }

    /// <summary>
    /// Gets the access token extracted from the request.
    /// </summary>
    public string Token { get; init; }
}