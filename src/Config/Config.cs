using System.Text.Json;
using System.Text.Json.Nodes;

namespace IotDeviceMigrator.Config;

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