using IotDeviceMigrator.Client;
using Serilog;

namespace IotDeviceMigrator.Migration.Steps;

public class CheckIdentity(ITargetIotClient target) : IMigrationStep
{
    public string Name { get; } = "Checking if device identity is registered";
    public IIotClient HubClient => target;
    public async Task<MigrationResult> StepAsync(string deviceId)
    {
        var registered = await target.IsDeviceRegisteredAsync(deviceId);
        if (registered)
        {
            Log.Information("Device '{DeviceId}' is registered in '{TargetHubName}'", deviceId, target.Name);
            return new MigrationResult(true);
        }

        Log.Information("Device '{DeviceId}' is not registered in '{TargetHubName}'", deviceId, target.Name);
        throw new MigrationException(deviceId, $"Device is not registered in '{target.Name}'");

    }
}