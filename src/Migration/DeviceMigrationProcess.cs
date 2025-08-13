using IotDeviceMigrator.Client;
using IotDeviceMigrator.Config;
using IotDeviceMigrator.Migration.Steps;
using Serilog;

namespace IotDeviceMigrator.Migration;

public class DeviceMigrationProcess
{
    public required MigrationConfig Config { private get; init;}
    public required List<IMigrationStep> Steps { private get; init; }

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


public class MigrationException(string deviceId, string message) : Exception($"Error migrating device {deviceId}: {message}");

public class DeviceMethodInvocationException(string deviceId, string methodName, string message) : MigrationException(deviceId, $"Error while invoking method '{methodName}': {message}");