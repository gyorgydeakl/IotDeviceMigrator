using IotDeviceMigrator.Common;
using Serilog;
using SourceClient = IotDeviceMigrator.Client.AzureIotClient;
using TargetClient = IotDeviceMigrator.Client.AzureIotClient;

namespace IotDeviceMigrator.Migration;

/// <summary>
/// Entrypoint for running the device migration workflow end-to-end.
/// Orchestrates creation of the migration process and aggregates results across devices.
/// </summary>
public class Migrate
{
    /// <summary>
    /// Runs the migration for the provided set of device IDs using settings from the given configuration.
    /// </summary>
    /// <param name="config">Global configuration containing connection and migration settings.</param>
    /// <param name="deviceIds">List of device identifiers to migrate.</param>
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