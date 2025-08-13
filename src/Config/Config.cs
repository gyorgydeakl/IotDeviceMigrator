using System.Text.Json;
using System.Text.Json.Nodes;

namespace IotDeviceMigrator.Config;

public record Config
{
    public required string LogFile {get; init;}
    public required string DeviceIdImport {get; init;}
    public required MigrationConfig Migration {get; init;}
    public required ConnectionConfig Connection {get; init;}

    public static Config FromFile(string configFileName)
    {
        var config = JsonNode.Parse(File.ReadAllText(configFileName));
        if (config is null)
        {
            throw new ConfigParseException($"{configFileName} not found");
        }

        return new Config
        {
            Connection = ConnectionConfig.FromJson(config["Connection"], configFileName),
            LogFile = config.ExpectValue<string>("LogFile", configFileName),
            DeviceIdImport = config.ExpectValue<string>("DeviceIdImport", configFileName),
            Migration = MigrationConfig.FromJson(config["Migration"], configFileName)
        };
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public string ToJsonString() => JsonSerializer.Serialize(this, SerializerOptions);
}
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

public record MigrationConfig
{
    public required int NumberOfRetries {get; init;}
    public required int RetryDelayInSeconds {get; init;}
    public required string CorrectFirmwareVersion {get; init;}
    public required string[] FirmwarePath {get; init;}

    public static MigrationConfig FromJson(JsonNode? json, string configFileName)
    {
        if (json is null)
        {
            throw new ConfigParseException($"'Migration' not found in config file '{configFileName}'");
        }
        return new MigrationConfig
        {
            NumberOfRetries = json.ExpectValue<int>("NumberOfRetries", configFileName),
            RetryDelayInSeconds = json.ExpectValue<int>("RetryDelayInSeconds", configFileName),
            CorrectFirmwareVersion = json.ExpectValue<string>("CorrectFirmwareVersion", configFileName),
            FirmwarePath = json.ExpectArray<string>("FirmwarePath", configFileName, 1)
        };
    }
}
public class ConfigParseException(string message) : Exception(message);