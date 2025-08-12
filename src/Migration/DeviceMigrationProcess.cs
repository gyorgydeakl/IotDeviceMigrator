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
                    "(Device {DeviceIdx}/{TotalDeviceCount}) Starting migration for device '{DeviceId}'",
                    idx + 1,
                    deviceIds.Length,
                    deviceId);
                var result = await MigrateAsync(deviceId);
                Log.Information(
                    "(Device {DeviceIdx}/{TotalDeviceCount}) Migration result for device '{DeviceId}': {Result}",
                    idx + 1,
                    deviceIds.Length,
                    deviceId,
                    result);
            }
            catch (MigrationException e)
            {
                errors[deviceId] = e;
                Log.Error(e, "Migration error for device {DeviceId}", deviceId);
            }
        }
        return errors;
    }

    private async Task<MigrationResult> MigrateAsync(string deviceId)
    {
        Log.Information(
            "Starting migration for device '{DeviceId}'. This will be retried {RetryCount} times, unless the process is cancelled by a known error, or the migration is successful",
            deviceId,
            Config.NumberOfRetries);
        foreach (var retryIdx in Enumerable.Range(0, Config.NumberOfRetries))
        {
            Log.Information(
                "(Try {Idx}/{MaxIdx}) Migrating device '{DeviceId}'. This migration consists of {StepCout} steps",
                retryIdx + 1,
                Config.NumberOfRetries,
                deviceId,
                Steps.Count);
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
                    Log.Information("Migration finished, retrying in {RetryDelayInSeconds} seconds", Config.RetryDelayInSeconds);
                    return result;
                }
                Log.Information("Encountered no issues during step {StepIdx} out of {MaxStepIdx}: {Name}",
                    stepIdx + 1,
                    Steps.Count,
                    step.Name);
            }


            Log.Information("Migration process finished, retrying in {RetryDelayInSeconds} seconds", Config.RetryDelayInSeconds);
            Thread.Sleep(TimeSpan.FromSeconds(Config.RetryDelayInSeconds));
        }

        // Should run the first check one last time after the last retry
        var checkStep = Steps[0];
        Log.Information("Checking last time: {Name}", checkStep.Name);
        var finalCheckResult = await checkStep.StepAsync(deviceId);
        if (finalCheckResult is null)
        {
            throw new MigrationException(deviceId, $"Tried to migrate {Config.NumberOfRetries} times, but did not succeed.");
        }

        Log.Information("Migration finished successfully");
        return finalCheckResult;
    }
}

public record MigrationResult(bool Continue);

public class MigrationException(string deviceId, string message) : Exception($"Error migrating device {deviceId}: {message}");

public class DeviceMethodInvocationException(string deviceId, string methodName, string message) : MigrationException(deviceId, $"Error while invoking method '{methodName}': {message}");