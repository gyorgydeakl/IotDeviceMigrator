using System.Text.Json;
using System.Text.Json.Nodes;

namespace IotDeviceMigrator;

public record Config
{
    public required string SourceHubConnectionString {get; init;}
    public required string TargetHubConnectionString {get; init;}
    public required string LogFile {get; init;}
    public required string DeviceIdImport {get; init;}
    public required MigrationConfig MigrationConfig {get; init;}

    public static async Task<Config> FromFileAsync(string configFileName)
    {
        var config = JsonNode.Parse(await File.ReadAllTextAsync(configFileName));
        if (config is null)
        {
            throw new ConfigParseException($"{configFileName} not found");
        }

        return new Config
        {
            SourceHubConnectionString = config.ExpectValue<string>("SourceIotHubConnectionString", configFileName),
            TargetHubConnectionString = config.ExpectValue<string>("SourceIotHubConnectionString", configFileName),
            LogFile = config.ExpectValue<string>("LogFile", configFileName),
            DeviceIdImport = config.ExpectValue<string>("DeviceIdImport", configFileName),
            MigrationConfig = MigrationConfig.FromJson(config["Migration"], configFileName)
        };
    }

    public string ToJsonString()
    {
        return JsonSerializer.Serialize(this, options: new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}

public record MigrationConfig
{
    public required int NumberOfRetries {get; init;}
    public required int RetryDelayInSeconds {get; init;}
    public required string CorrectFirmwareVersion {get; init;}

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
            CorrectFirmwareVersion = json.ExpectValue<string>("CorrectFirmwareVersion", configFileName)
        };
    }
}

public static class JsonExtensions
{
    public static T ExpectValue<T>(this JsonNode node, string key, string configFileName)
    {
        var valueNode = node[key];
        if (valueNode is null)
        {
            throw new ConfigParseException($"'{key}' not found in config file '{configFileName}'");
        }

        var value = valueNode.GetValue<T>();
        if (value is null)
        {
            throw new ConfigParseException($"The config '{key}' is expected to be of type '{typeof(T).Name}', but it is of type '{valueNode.GetType().Name}'");
        }
        return value;
    }
}

public class ConfigParseException(string message) : Exception(message);