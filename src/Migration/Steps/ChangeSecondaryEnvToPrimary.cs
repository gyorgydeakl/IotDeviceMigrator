using IotDeviceMigrator.Client;

namespace IotDeviceMigrator.Migration.Steps;

public class ChangeSecondaryEnvToPrimary(ISourceIotClient source) : IMigrationStep
{
    private const string SetIotConfigMethod = "setIotConfig";

    public string Name { get; } = "Change secondary environment as primary";
    public IIotClient HubClient => source;
    public async Task<MigrationResult> StepAsync(string deviceId)
    {
        var changeActiveEnvResult = await source.InvokeMethodAsync(deviceId, SetIotConfigMethod, ChangeActiveEnvPayload);
        if (changeActiveEnvResult.Status != 200) // TODO: What status do I check?
        {
            throw new DeviceMethodInvocationException(deviceId, SetIotConfigMethod, $"Failed to change active environment: {changeActiveEnvResult.GetPayloadAsJson()}");
        }

        return new MigrationResult(true);
    }

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