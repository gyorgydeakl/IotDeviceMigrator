using System.Text.Json.Nodes;

namespace IotDeviceMigrator.Config;

public record ConnectionConfig
{
    public required string SourceConnectionString {get; init;}
    public required string TargetConnectionString {get; init;}
    public required string SourceHubName {get; init;}
    public required string TargetHubName {get; init;}

    public static ConnectionConfig FromJson(JsonNode? json, string configFileName)
    {
        if (json is null)
        {
            throw new ConfigParseException($"'Connection' not found in config file '{configFileName}'");
        }

        return new ConnectionConfig()
        {
            SourceConnectionString = json.ExpectValue<string>("SourceConnectionString", configFileName),
            TargetConnectionString = json.ExpectValue<string>("TargetConnectionString", configFileName),
            SourceHubName = json.ExpectValue<string>("SourceHubName", configFileName),
            TargetHubName = json.ExpectValue<string>("TargetHubName", configFileName)
        };
    }
}