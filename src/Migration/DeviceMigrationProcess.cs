using IotDeviceMigrator.Client;
using IotDeviceMigrator.Common;
using IotDeviceMigrator.Migration.Steps;
using Serilog;

namespace IotDeviceMigrator.Migration;

/// <summary>
/// Coordinates the device migration workflow by executing a predefined sequence of steps
/// with retries and logging. Each step decides whether the process should continue.
/// </summary>
public class DeviceMigrationProcess
{
    /// <summary>
    /// Migration tuning/configuration (retries, delays, expected firmware, etc.).
    /// </summary>
    public required MigrationConfig Config { private get; init;}
    
    /// <summary>
    /// Ordered list of migration steps to execute for each device.
    /// </summary>
    public required List<IMigrationStep> Steps { private get; init; }

    /// <summary>
    /// Factory method that creates a process instance with a concrete source and target client type,
    /// pre-populating the standard sequence of steps.
    /// </summary>
    /// <typeparam name="TSource">Concrete source IoT client type.</typeparam>
    /// <typeparam name="TTarget">Concrete target IoT client type.</typeparam>
    /// <param name="migration">Migration configuration.</param>
    /// <param name="connection">Connection details for both source and target hubs.</param>
    /// <returns>Configured <see cref="DeviceMigrationProcess"/>.</returns>
    public static DeviceMigrationProcess Create<TSource, TTarget>(MigrationConfig migration, ConnectionConfig connection)
        where TSource : ISourceIotClient
        where TTarget : ITargetIotClient
    {
        var source = TSource.Create(connection.SourceHubName, connection.SourceConnectionString);
        var target = TTarget.Create(connection.TargetHubName, connection.TargetConnectionString);
        return new DeviceMigrationProcess
        {
            Config = migration,
            Steps = [
                new CheckActivity(target),
                new CheckIdentity(target),
                new CheckFirmwareVersion(source, migration),
                new SetupSecondaryEnv(source),
                new ChangeSecondaryEnvToPrimary(source),
            ]
        };
    }
    
    /// <summary>
    /// Executes migration for all provided devices and aggregates per-device errors.
    /// </summary>
    /// <param name="deviceIds">Device identifiers to migrate.</param>
    /// <returns>
    /// A dictionary mapping deviceId to the last <see cref="MigrationException"/> thrown for that device.
    /// Devices not present in the dictionary are considered successfully migrated.
    /// </returns>
    public async Task<Dictionary<string, MigrationException>> MigrateAllAsync(string[] deviceIds)
    {
        Dictionary<string, MigrationException> errors = new();
        var indexedDevices = deviceIds.Select((d, idx) => (d, idx));
        foreach (var (deviceId, idx) in indexedDevices)
        {
            try
            {
                Log.Information(
                    "(Device {DeviceIdx}/{TotalDeviceCount}: '{DeviceId}') Starting migration for device. This will be " +
                    "retried {RetryCount} times, unless the process is cancelled by a known error, or the migration is successful",
                    idx + 1,
                    deviceIds.Length,
                    deviceId,
                    Config.NumberOfRetries);
                await MigrateWithRetriesAsync(deviceId);
                Log.Information(
                    "(Device {DeviceIdx}/{TotalDeviceCount}: '{DeviceId}') Migration successful",
                    idx + 1,
                    deviceIds.Length,
                    deviceId);
            }
            catch (MigrationException e)
            {
                errors[deviceId] = e;
                Log.Error(e, "Migration error for device {DeviceId}", deviceId);
            }
        }
        return errors;
    }
    
    /// <summary>
    /// Executes the step pipeline for a single device with retry semantics.
    /// Stops early if any step indicates completion (Continue == false).
    /// After exhausting retries, performs a final check to confirm completion.
    /// </summary>
    /// <param name="deviceId">The device to migrate.</param>
    private async Task MigrateWithRetriesAsync(string deviceId)
    {
        foreach (var retryIdx in Enumerable.Range(0, Config.NumberOfRetries))
        {
            var stepsIndexed = Steps.Select((s, idx) => (s, idx));
            foreach (var (step, stepIdx) in stepsIndexed)
            {
                Log.Information("(Try {Idx}/{MaxIdx}, Step {StepIdx}/{MaxStepIdx}) on hub '{HubName}': {Name}",
                    retryIdx + 1,
                    Config.NumberOfRetries,
                    stepIdx + 1,
                    Steps.Count,
                    step.HubClient.Name,
                    step.Name);

                var result = await step.StepAsync(deviceId);
                if (!result.Continue)
                {
                    Log.Information("Migration finished successfully");
                    return;
                }
                Log.Information("(Try {Idx}/{MaxIdx}, Step {StepIdx}/{MaxStepIdx}) executed step successfully on hub '{HubName}': {Name}",
                    retryIdx + 1,
                    Config.NumberOfRetries,
                    stepIdx + 1,
                    Steps.Count,
                    step.HubClient.Name,
                    step.Name);
            }

            Log.Information("Migration process finished, retrying in {RetryDelayInSeconds} seconds", Config.RetryDelayInSeconds);
            Thread.Sleep(TimeSpan.FromSeconds(Config.RetryDelayInSeconds));
        }

        // Should run the first check one last time after the last retry
        var checkStep = Steps[0];
        Log.Information("Checking last time: {Name}", checkStep.Name);
        var finalCheckResult = await checkStep.StepAsync(deviceId);
        if (finalCheckResult.Continue)
        {
            throw new MigrationException(deviceId, $"Tried to migrate {Config.NumberOfRetries} times, but did not succeed.");
        }

        Log.Information("Migration finished successfully");
    }
}

/// <summary>
/// Base exception for migration failures, enriched with device context.
/// </summary>
/// <param name="deviceId">Device identifier involved in the failure.</param>
/// <param name="message">Human-readable error description.</param>
public class MigrationException(string deviceId, string message) : Exception($"Error migrating device {deviceId}: {message}");

/// <summary>
/// Specialized exception for failures that occur during device method invocation.
/// </summary>
/// <param name="deviceId">Device identifier.</param>
/// <param name="methodName">The device method that failed.</param>
/// <param name="message">Error description.</param>
public class DeviceMethodInvocationException(string deviceId, string methodName, string message) : MigrationException(deviceId, $"Error while invoking method '{methodName}': {message}");