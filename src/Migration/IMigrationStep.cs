using IotDeviceMigrator.Client;

namespace IotDeviceMigrator.Migration;

public interface IMigrationStep
{
    string Name { get; }
    IIotClient HubClient { get; }
    Task<StepSuccessResult> StepAsync(string deviceId);
}

public record StepSuccessResult(bool Continue);