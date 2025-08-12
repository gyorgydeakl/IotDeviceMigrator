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

public class ConfigParseException(string message) : Exception(message);