using IotDeviceMigrator.Client;
using IotDeviceMigrator.Config;
using IotDeviceMigrator.Migration.Steps;
using Microsoft.Azure.Devices.Common.Exceptions;
using Serilog;

namespace IotDeviceMigrator.Migration;

public class DeviceMigrationProcess
{


    public required string DeviceId { get; init;}
    public required MigrationConfig Config { private get; init;}
    public required ISourceIotClient SourceIotClient { private get; init;}
    public required ITargetIotClient TargetIotClient { private get; init;}

    public static DeviceMigrationProcess Create<TSource, TTarget>(string deviceId, Config.Config config)
        where TSource : ISourceIotClient
        where TTarget : ITargetIotClient
    {
        return new DeviceMigrationProcess
        {
            DeviceId = deviceId,
            Config = config.Migration,
            SourceIotClient = TSource.CreateFromConnectionString(config.Connection.SourceConnectionString),
            TargetIotClient = TTarget.CreateFromConnectionString(config.Connection.TargetConnectionString),
        };
    }
    public async Task<MigrationResult> MigrateAsync()
    {
        var checkStep = new CheckActivity(TargetIotClient);
        List<IMigrationStep> steps =
        [
            checkStep,
            new CheckIdentityInTarget(TargetIotClient),
            new CheckFirmwareVersion(SourceIotClient, Config),
            new SetupSecondaryEnv(SourceIotClient),
            new ChangeSecondaryEnvToPrimary(SourceIotClient),
        ];

        foreach (var retryIdx in Enumerable.Range(0, Config.NumberOfRetries))
        {
            Log.Information(
                "({Idx}/{MaxIdx}) Starting migration for device '{DeviceId}'. This migration consists of {StepCout} steps",
                retryIdx + 1,
                Config.NumberOfRetries,
                DeviceId,
                steps.Count);

            foreach (var (step, stepIdx) in steps.Select((s, idx) => (s, idx)))
            {
                Log.Information("Executing step {StepIdx} out of {MaxStepIdx} on hub {HubName}: {Name}",
                    stepIdx + 1,
                    steps.Count,
                    step.HubClient.Name,
                    step.Name);

                var result = await step.StepAsync(DeviceId);
                if (result is not null)
                {
                    Log.Information("Migration finished, retrying in {RetryDelayInSeconds} seconds", Config.RetryDelayInSeconds);
                    return result;
                }
                Log.Information("Encountered no issues during step {StepIdx} out of {MaxStepIdx}: {Name}",
                    stepIdx + 1,
                    steps.Count,
                    step.Name);
            }


            Log.Information("Migration process finished, retrying in {RetryDelayInSeconds} seconds", Config.RetryDelayInSeconds);
            Thread.Sleep(TimeSpan.FromSeconds(Config.RetryDelayInSeconds));
        }

        // Should run the first check one last time after
        Log.Information("Checking last time: {Name}", checkStep.Name);
        var finalCheckResult = await checkStep.StepAsync(DeviceId);
        if (finalCheckResult is null)
        {
            throw new MigrationException(DeviceId, $"Tried to migrate {Config.NumberOfRetries} times, but did not succeed.");
        }

        Log.Information("Migration finished successfully");
        return finalCheckResult;

    }
}

public record MigrationResult(string DeviceId);

public class MigrationException(string deviceId, string message) : Exception($"Error migrating device {deviceId}: {message}");

public class DeviceMethodInvocationException(string deviceId, string methodName, string message) : MigrationException(deviceId, $"Error while invoking method '{methodName}': {message}");