using IotDeviceMigrator.Client;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Exceptions;

namespace IotDeviceMigrator.Migration.Steps;

public class SetupSecondaryEnv(ISourceIotClient source) : IMigrationStep
{
    private const string SetIotConfigMethod = "setIotConfig";

    public string Name { get; } = "Setting up secondary environment";
    public IIotClient HubClient => source;
    public async Task<StepSuccessResult> StepAsync(string deviceId)
    {
        try
        {
            var setupSecondaryEnvResult =
                await source.InvokeMethodAsync(deviceId, SetIotConfigMethod, SetupSecondaryEnvPayload);
            if (setupSecondaryEnvResult.Status != 200)
            {
                throw new DeviceMethodInvocationException(deviceId, SetIotConfigMethod,
                    $"Failed to set up secondary environment: {setupSecondaryEnvResult.GetPayloadAsJson()}");
            }
        }
        catch (DeviceNotFoundException e)
        {
            throw new DeviceMethodInvocationException(deviceId, SetIotConfigMethod, e.Message);
        }


        return new StepSuccessResult(true);
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
}