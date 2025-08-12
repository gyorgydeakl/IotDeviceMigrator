using IotDeviceMigrator.Client;
using IotDeviceMigrator.Config;

namespace IotDeviceMigrator.Migration.Steps;

public class CheckFirmwareVersion(ISourceIotClient source, MigrationConfig config) : IMigrationStep
{
    public string Name { get; } = "Checking if firmware version is correct";
    public IIotClient HubClient => source;
    public async Task<MigrationResult?> StepAsync(string deviceId)
    {
        var properties = await source.GetPropertiesAsync(deviceId);
        if (properties is null)
        {
            throw new MigrationException(deviceId, "Device properties are null. It might not exist in the source hub.");
        }

        var reported = properties.Reported;
        foreach (var pathSegment in config.FirmwarePath)
        {
            if (!reported.Contains(pathSegment))
            {
                throw new MigrationException(deviceId, $"Firmware path '{string.Join('/',config.FirmwarePath)}' not found in reported properties of the device.");
            }
            reported = reported[pathSegment];
        }
        var firmwareVersion = reported.ToString();
        var isFirmwareCorrect = firmwareVersion.Equals(config.CorrectFirmwareVersion, StringComparison.InvariantCultureIgnoreCase);
        if (!isFirmwareCorrect)
        {
            throw new MigrationException(deviceId, $"Firmware version '{firmwareVersion}' is not correct. Should be '{config.CorrectFirmwareVersion}'");
        }

        return null;
    }
}