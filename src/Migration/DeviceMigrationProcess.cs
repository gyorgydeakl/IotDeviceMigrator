using IotDeviceMigrator.Client;
using IotDeviceMigrator.Config;
using Microsoft.Azure.Devices.Common.Exceptions;
using Serilog;

namespace IotDeviceMigrator.Migration;

public class DeviceMigrationProcess
{
    private const string GetConfigMethod = "getIotConfig";
    private const string SetIotConfigMethod = "setIotConfig";

    private const string CorrectFirmwareVersion = "v3.1-2507031257";

    public required string DeviceId { private get; init;}
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
            Config = config.MigrationConfig,
            SourceIotClient = TSource.CreateFromConnectionString(config.SourceHubConnectionString),
            TargetIotClient = TTarget.CreateFromConnectionString(config.TargetHubConnectionString)
        };
    }
    public async Task<MigrationResult> MigrateAsync()
    {
        List<MigrationStep> steps =
        [
            new()
            {
                Name = $"Checking if device responds by calling '{GetConfigMethod}'",
                StepAsync = CheckActivity,
            },
            new()
            {
                Name = $"Checking if device identity is registered in target hub",
                StepAsync = CheckIdentityInTarget
            },
            new()
            {
                Name = $"Checking if firmware version is correct",
                StepAsync = CheckFirmwareVersion
            },
            new()
            {
                Name = $"Setting up secondary environment in source",
                StepAsync = SetupSecondaryEnv
            },
            new()
            {
                Name = $"Setting secondary environment as primary in source",
                StepAsync = ChangeSecondaryEnv
            }
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
                Log.Information("Executing step {StepIdx} out of {MaxStepIdx}: {Name}",
                    stepIdx + 1,
                    steps.Count,
                    step.Name);

                var result = await step.StepAsync();
                if (result is not null)
                {
                    Log.Information("Migration finished, retrying in {RetryDelayInSeconds} seconds", Config.RetryDelayInSeconds);
                    return result;
                }
                Log.Information("Successfully executed step {StepIdx} out of {MaxStepIdx}: {Name}",
                    stepIdx + 1,
                    steps.Count,
                    step.Name);
            }


            Log.Information("Migration process finished, retrying in {RetryDelayInSeconds} seconds", Config.RetryDelayInSeconds);
            Thread.Sleep(TimeSpan.FromSeconds(Config.RetryDelayInSeconds));
        }

        Log.Information("Checking last time: {Name}", steps[0].Name);
        var resultFinal = await steps[0].StepAsync();
        if (resultFinal is not null)
        {
            return resultFinal;
        }

        throw new MigrationException(DeviceId, $"Tried to migrate {Config.NumberOfRetries} times, but did not succeed.");
    }

    private async Task<MigrationResult?> CheckActivity()
    {
        try
        {
            await TargetIotClient.InvokeMethodAsync(DeviceId, GetConfigMethod, new { });
            return new MigrationResult(DeviceId);
        }
        catch (DeviceNotFoundException)
        {
            return null;
        }
    }

    private async Task<MigrationResult?> CheckIdentityInTarget()
    {
        var registered = await TargetIotClient.IsDeviceRegisteredAsync(DeviceId);
        if (!registered)
        {
            throw new MigrationException(DeviceId, "Device is not registered in QAS");
        }

        return null;
    }

    private async Task<MigrationResult?> CheckFirmwareVersion()
    {
        var properties = await SourceIotClient.GetPropertiesAsync(DeviceId);
        string firmwareVersion = properties.Reported["devInfo"]["swVersion"]; // TODO: how do I get firmware version?
        var isFirmwareCorrect = firmwareVersion.Equals(Config.CorrectFirmwareVersion, StringComparison.InvariantCultureIgnoreCase);
        if (!isFirmwareCorrect)
        {
            throw new MigrationException(DeviceId, $"Firmware version '{firmwareVersion}' is not correct. Should be '{CorrectFirmwareVersion}'");
        }

        return null;
    }

    private async Task<MigrationResult?> ChangeSecondaryEnv()
    {
        var changeActiveEnvResult = await SourceIotClient.InvokeMethodAsync(DeviceId, SetIotConfigMethod, ChangeActiveEnvPayload);
        if (changeActiveEnvResult.Status != 200) // TODO: What status do I check?
        {
            throw new DeviceMethodInvocationException(DeviceId, SetIotConfigMethod, $"Failed to change active environment: {changeActiveEnvResult.GetPayloadAsJson()}");
        }

        return null;
    }

    private async Task<MigrationResult?> SetupSecondaryEnv()
    {
        var setupSecondaryEnvResult = await SourceIotClient.InvokeMethodAsync(DeviceId, SetIotConfigMethod, SetupSecondaryEnvPayload);
        if (setupSecondaryEnvResult.Status != 200)
        {
            throw new DeviceMethodInvocationException(DeviceId, SetIotConfigMethod, $"Failed to set up secondary environment: {setupSecondaryEnvResult.GetPayloadAsJson()}");
        }

        return null;
    }


    private static readonly object SetupSecondaryEnvPayload =
        new
        {
            SecondaryConfig = new
            {
                AuthMode = "x509",
                DpsEnable = false,
                DpsIdScope = string.Empty,
                IotGateway = string.Empty,
                IotHostName = "iot-IngridSmart-DEV.azure-devices.net",
                IotPort = 8883,
                SharedKeyPrimary = string.Empty,
                SharedKeySecondary = string.Empty
            }
        };

    private static readonly object ChangeActiveEnvPayload =
        new
        {
            TestInactiveConfig = new
            {
                Timeout = 10,
                SetPreferredIfReady = true,
            }
        };
}

public record MigrationResult(string DeviceId);

public class MigrationException(string deviceId, string message) : Exception($"Error migrating device {deviceId}: {message}");

public class DeviceMethodInvocationException( string deviceId, string methodName, string message) : MigrationException(deviceId, $"Error while invoking method '{methodName}': {message}");