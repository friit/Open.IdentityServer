using System.Text.Json.Serialization;
using Open.IdentityServer.Models;

namespace Open.IdentityServer.DynamicClientManagement.Models;

public class GetResponseDTO
{
    [JsonPropertyName("client")]
    public Client Client { get; set; }
}