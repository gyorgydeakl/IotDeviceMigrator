using System.Text.Json;

namespace IotDeviceMigrator.Common;

public record Config
{
    public required string LogFile {get; init;}
    public required string DeviceIdImport {get; init;}
    public MigrationConfig? Migration {get; init;}
    public required ConnectionConfig Connection {get; init;}

    public static Config FromFile(string configFileName)
    {
        return JsonSerializer.Deserialize<Config>(File.ReadAllText(configFileName), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        })!;
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
}

public record MigrationConfig
{
    public required int NumberOfRetries {get; init;}
    public required int RetryDelayInSeconds {get; init;}
    public required string CorrectFirmwareVersion {get; init;}
    public required string[] FirmwarePath {get; init;}
}
