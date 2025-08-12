using IotDeviceMigrator.Client;

namespace IotDeviceMigrator.Migration;

public interface IMigrationStep
{
    string Name { get; }
    IIotClient HubClient { get; }
    Task<MigrationResult> StepAsync(string deviceId);
}