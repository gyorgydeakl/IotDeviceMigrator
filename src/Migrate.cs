using IotDeviceMigrator.Config;
using IotDeviceMigrator.Migration;
using Serilog;
using SourceClient = IotDeviceMigrator.Client.AzureIotClient;
using TargetClient = IotDeviceMigrator.Client.AzureIotClient;

namespace IotDeviceMigrator;

internal class Migrate
{
    public static async Task Main(string[] args)
    {
        try
        {
            Logging.Init("logs/migrate-.log");
            var config = Config.Config.FromFile(GetConfigFileName(args));

            Log.Information("Parsed Config: {ConfigJson}", config.ToJsonString());

            var deviceIds = Parse.ParseDeviceIds(config.DeviceIdImport);
            Log.Information("Parsed device ids: {DeviceIds}", string.Join(", ", deviceIds));

            var process = DeviceMigrationProcess.Create<SourceClient, TargetClient>(config.Migration, config.Connection);
            Log.Information(
                "This script is going to migrate these devices from '{SourceHubName}' to '{TargetHubName}'",
                config.Connection.SourceHubName,
                config.Connection.TargetHubName);

            var errors = await process.MigrateAllAsync(deviceIds);
            Log.Information("Migrations finished for all {DeviceCount} devices {Devices}", deviceIds.Length, string.Join(", ", deviceIds));

            var unSuccessfulDevices = errors.Keys.ToList();
            Log.Information(
                "Migrations were unsuccessful for {Count} devices in total: {UnsuccessfulDevices}",
                unSuccessfulDevices.Count,
                string.Join(", ", unSuccessfulDevices));

            var successfulDevices = deviceIds.Except(errors.Keys).ToList();
            Log.Information(
                "Migrations were successful for {Count} devices in total: {SuccessfulDevices}",
                successfulDevices.Count,
                string.Join(", ", successfulDevices));
        }
        catch (ConfigParseException e)
        {
            Log.Fatal(e, "{ConfigFileName} must match the shape of the Config class", GetConfigFileName(args));
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Error: {Err}", e);
        }
    }

    private static string GetConfigFileName(string[] args)
    {
        const string defaultConfigFile = "config.json";
        if (args.Length > 1)
        {
            throw new ArgumentException("Too many arguments. Only 1 (or 0) argument is allowed: config file name.");
        }
        return args.ElementAtOrDefault(1) ?? defaultConfigFile;
    }
}