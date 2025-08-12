using System.Text.Json;
using System.Text.Json.Nodes;

namespace IotDeviceMigrator.Config;

public record Config
{
    public required string LogFile {get; init;}
    public required string DeviceIdImport {get; init;}
    public required MigrationConfig Migration {get; init;}
    public required ConnectionConfig Connection {get; init;}

    public static async Task<Config> FromFileAsync(string configFileName)
    {
        var config = JsonNode.Parse(await File.ReadAllTextAsync(configFileName));
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

    public string ToJsonString()
    {
        return JsonSerializer.Serialize(this, options: new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}

public class ConfigParseException(string message) : Exception(message);