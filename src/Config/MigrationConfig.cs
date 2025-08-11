using System.Text.Json.Nodes;

namespace IotDeviceMigrator.Config;

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