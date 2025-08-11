using System.Text.Json;
using Microsoft.Azure.Devices;

namespace IotDeviceMigrator;

public class DeviceMigrationProcess
{
    private const string GetConfigMethod = "getIotConfig";
    private const string SetIotConfigMethod = "setIotConfig";

    private const string CorrectFirmwareVersion = "v3.1-2507031257";

    public required string DeviceId { private get; init;}
    public required MigrationConfig Config { private get; init;}
    public required ISourceIotClient SourceIotClient { private get; init;}
    public required ITargetIotClient TargetIotClient { private get; init;}

    public static DeviceMigrationProcess Create<TSource, TTarget>(string deviceId, Config config)
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
    public async Task MigrateAsync()
    {
        foreach (var _ in Enumerable.Range(0, Config.NumberOfRetries))
        {
            // Does device send data to server?
            var getConfigResult = await TargetIotClient.InvokeMethodAsync(DeviceId, GetConfigMethod, new {}); // target
            if (getConfigResult.Status == 200)
            {
                return; // We are done
            }

            // Is the identity of the device even registered in QAS?
            var registered = await TargetIotClient.IsDeviceRegisteredAsync(DeviceId); // target
            if (!registered)
            {
                throw new MigrationException(DeviceId, "Device is not registered in QAS");
            }

            // is the firmware correct?
            var properties = await SourceIotClient.GetPropertiesAsync(DeviceId);
            string firmwareVersion = properties.Reported["devInfo"]["swVersion"]; // TODO: how do I get firmware version?
            var isFirmwareCorrect = firmwareVersion.Equals(Config.CorrectFirmwareVersion, StringComparison.InvariantCultureIgnoreCase);
            if (!isFirmwareCorrect)
            {
                throw new MigrationException(DeviceId, $"Firmware version '{firmwareVersion}' is not correct. Should be '{CorrectFirmwareVersion}'");
            }

            // setup secondary env: QAS IoTHub
            var changeSecondaryEnvResult = await SourceIotClient.InvokeMethodAsync(DeviceId, SetIotConfigMethod, SetupSecondaryEnvPayload);
            if (changeSecondaryEnvResult.Status != 200)
            {
                throw new DeviceMethodInvocationException(DeviceId, SetIotConfigMethod, $"Failed to set up secondary environment: {changeSecondaryEnvResult.GetPayloadAsJson()}");
            }

            // change active environment to secondary
            var changeActiveEnvResult = await SourceIotClient.InvokeMethodAsync(DeviceId, SetIotConfigMethod, ChangeActiveEnvPayload);
            if (changeActiveEnvResult.Status != 200) // TODO: What status do I check?
            {
                throw new DeviceMethodInvocationException(DeviceId, SetIotConfigMethod, $"Failed to change active environment: {changeActiveEnvResult.GetPayloadAsJson()}");
            }

            Thread.Sleep(TimeSpan.FromSeconds(Config.RetryDelayInSeconds)); // from config
        }
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

public class MigrationException(string deviceId, string message) : Exception($"Error migrating device {deviceId}: {message}");

public class DeviceMethodInvocationException( string deviceId, string methodName, string message) : MigrationException(deviceId, $"Error while invoking method '{methodName}': {message}");