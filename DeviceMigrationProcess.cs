using System.Text.Json;
using Microsoft.Azure.Devices;

namespace IotDeviceMigrator;

public class DeviceMigrationProcess(string deviceId, ServiceClient serviceClient, RegistryManager registryManager)
{
    // TODO: method names?
    private const string GetConfigMethod = "getIotConfig";
    private const string SetIotConfigMethod = "setIotConfig";

    private const string CorrectFirmwareVersion = "v3.1-2507031257";
    public async Task MigrateAsync()
    {
        while (true)
        {
            // Does device send data to server?
            var getConfigResult = await InvokeMethodAsync(GetConfigMethod, "{}");
            if (getConfigResult.Status == 200)
            {
                return; // We are done
            }

            // Is the identity of the device even registered in QAS?
            var twinInQas = await registryManager.GetTwinAsync(deviceId);
            if (twinInQas is null)
            {
                throw new MigrationException(deviceId, "Device is not registered in QAS");
            }

            // is the firmware correct?
            string firmwareVersion = twinInQas.Properties.Reported["devInfo"]["swVersion"]; // TODO: how do I get firmware version?
            if (!firmwareVersion.Equals(CorrectFirmwareVersion, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new MigrationException(deviceId, $"Firmware version '{firmwareVersion}' is not correct. Should be '{CorrectFirmwareVersion}'");
            }

            // setup secondary env: QAS IoTHub
            var changeSecondaryEnvResult = await InvokeMethodAsync(SetIotConfigMethod, ChangeSecondaryEnvPayload);
            if (changeSecondaryEnvResult.Status != 200) // TODO: What status do I check?
            {
                throw new DeviceMethodInvocationException(deviceId, SetIotConfigMethod, $"Failed to set up secondary environment: {changeSecondaryEnvResult.GetPayloadAsJson()}");
            }

            // change active environment to secondary
            var changeActiveEnvResult = await InvokeMethodAsync(SetIotConfigMethod, ChangeActiveEnvPayload);
            if (changeActiveEnvResult.Status != 200) // TODO: What status do I check?
            {
                throw new DeviceMethodInvocationException(deviceId, SetIotConfigMethod, $"Failed to change active environment: {changeActiveEnvResult.GetPayloadAsJson()}");
            }
        }
    }

    private async Task<CloudToDeviceMethodResult> InvokeMethodAsync(string methodName, object? payload)
    {
        var methodInvocation = new CloudToDeviceMethod(methodName)
        {
            ResponseTimeout   = TimeSpan.FromSeconds(30),
            ConnectionTimeout = TimeSpan.FromSeconds(10)
        };
        methodInvocation.SetPayloadJson(JsonSerializer.Serialize(payload));
        return await serviceClient.InvokeDeviceMethodAsync(deviceId, methodInvocation);
    }

    private static readonly object ChangeSecondaryEnvPayload =
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