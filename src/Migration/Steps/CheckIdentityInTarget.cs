using IotDeviceMigrator.Client;

namespace IotDeviceMigrator.Migration.Steps;

public class CheckIdentityInTarget(ITargetIotClient target) : IMigrationStep
{
    public string Name { get; } = "Checking if device identity is registered";
    public IIotClient HubClient => target;
    public async Task<MigrationResult?> StepAsync(string deviceId)
    {
        var registered = await target.IsDeviceRegisteredAsync(deviceId);
        if (!registered)
        {
            throw new MigrationException(deviceId, "Device is not registered in QAS");
        }

        return null;
    }
}