using IotDeviceMigrator.Client;
using Microsoft.Azure.Devices.Common.Exceptions;
using Serilog;

namespace IotDeviceMigrator.Migration.Steps;

public class CheckActivity(ITargetIotClient target) : IMigrationStep
{
    private const string GetConfigMethod = "getIotConfig";
    public string Name { get; } = "Checking if device responds by calling 'getIotConfig'";
    public IIotClient HubClient => target;
    public async Task<StepSuccessResult> StepAsync(string deviceId)
    {
        try
        {
            await target.InvokeMethodAsync(deviceId, GetConfigMethod, new { });
            return new StepSuccessResult(false);
        }
        catch (DeviceNotFoundException)
        {
            Log.Information("Device '{DeviceId}' did not respond in '{TargetHubName}'", deviceId, target.Name);
            return new StepSuccessResult(true);
        }
    }
}