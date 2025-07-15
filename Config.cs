using System.Text.Json.Nodes;

namespace IotDeviceMigrator;

public record Config(string HubConnectionString, string LogFile, List<string> DeviceIds)
{
    private const string DefaultLogFile = "log.txt";

    public static async Task<Config> FromFileAsync(string fileName)
    {
        var config = JsonNode.Parse(await File.ReadAllTextAsync("config.json"));
        if (config is null)
        {
            throw new ConfigParseException($"{fileName} not found");
        }

        var connectionString = config["IotHubConnectionString"]?.GetValue<string>() ?? throw new ConfigParseException($"IotHubConnectionString not found in {fileName}");
        var deviceIds = config["DeviceIds"]
            ?.AsArray()
            .Select(e => e?.GetValue<string>())
            .OfType<string>()
            .ToList() ?? throw new ConfigParseException($"DeviceIds not found in {fileName}");
        var logFile = config["LogFile"]?.GetValue<string>() ?? DefaultLogFile;
        return new Config(connectionString, logFile, deviceIds);
    }
}

public class ConfigParseException(string message) : Exception(message);