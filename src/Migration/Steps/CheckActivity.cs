using IotDeviceMigrator.Client;
using Microsoft.Azure.Devices.Common.Exceptions;
using Serilog;

namespace IotDeviceMigrator.Migration.Steps;

public class CheckActivity(ITargetIotClient target) : IMigrationStep
{
    private const string GetConfigMethod = "getIotConfig";
    public string Name { get; } = "Checking if device responds by calling 'getIotConfig'";
    public IIotClient HubClient => target;
    public async Task<MigrationResult?> StepAsync(string deviceId)
    {
        try
        {
            await target.InvokeMethodAsync(deviceId, GetConfigMethod, new { });
            return new MigrationResult(deviceId);
        }
        catch (DeviceNotFoundException)
        {
            Log.Information("Device '{deviceId}' did not respond in '{TargetHubName}'}", deviceId, target.Name);;
            return null;
        }
    }
}