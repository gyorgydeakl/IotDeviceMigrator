using System.Text.Json;
using IotDeviceMigrator.Common;
using IotDeviceMigrator.Migration;
using Serilog;
using SourceClient = IotDeviceMigrator.Client.AzureIotClient;
using TargetClient = IotDeviceMigrator.Client.AzureIotClient;

namespace IotDeviceMigrator;

public class Migrate
{
    public static async Task Run(Config config, string[] deviceIds)
    {
        var migrationCfg = config.Migration ?? throw new ArgumentException("Migration must be specified in the config file.");
        var process = DeviceMigrationProcess.Create<SourceClient, TargetClient>(migrationCfg, config.Connection);
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
}