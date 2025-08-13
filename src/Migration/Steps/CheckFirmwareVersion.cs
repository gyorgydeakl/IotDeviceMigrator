using IotDeviceMigrator.Client;
using IotDeviceMigrator.Config;
using Serilog;

namespace IotDeviceMigrator.Migration.Steps;

public class CheckFirmwareVersion(ISourceIotClient source, MigrationConfig config) : IMigrationStep
{
    public string Name { get; } = "Checking if firmware version is correct";
    public IIotClient HubClient => source;
    public async Task<StepSuccessResult> StepAsync(string deviceId)
    {
        var properties = await source.GetPropertiesAsync(deviceId);
        if (properties is null)
        {
            throw new MigrationException(deviceId, $"Device properties are null; Unable check firmware version. This might be because it does not exist in '{source.Name}'.");
        }

        var reported = properties.Reported;
        foreach (var pathSegment in config.FirmwarePath.SkipLast(1))
        {
            if (!reported.Contains(pathSegment))
            {
                throw new MigrationException(deviceId, $"Firmware path '{string.Join('/',config.FirmwarePath)}' not found in reported properties of the device.");
            }
            reported = reported[pathSegment];
        }
        var firmwareVersion = reported[config.FirmwarePath[^1]].ToString();
        var isFirmwareCorrect = firmwareVersion.Equals(config.CorrectFirmwareVersion, StringComparison.InvariantCultureIgnoreCase);
        if (!isFirmwareCorrect)
        {
            throw new MigrationException(deviceId, $"Firmware version '{firmwareVersion}' is not correct. Should be '{config.CorrectFirmwareVersion}'");
        }

        Log.Information("Firmware version of device '{DeviceId}' is '{Version}', which is correct", deviceId, firmwareVersion);
        return new StepSuccessResult(true);
    }
}