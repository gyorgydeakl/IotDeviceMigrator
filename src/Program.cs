using IotDeviceMigrator.Client;
using IotDeviceMigrator.Config;
using IotDeviceMigrator.Migration;
using Serilog;

using SourceClient = IotDeviceMigrator.Client.AzureIotClient;
using TargetClient = IotDeviceMigrator.Client.AzureIotClient;

internal class Program
{
    private const string DefaultConfigFile = "config.json";

    public static async Task Main(string[] args)
    {
        InitLogger();
        try
        {
            var config = Config.FromFile(GetConfigFileName(args));
            Log.Information("Parsed Config: {ConfigJson}", config.ToJsonString());

            var deviceIds = ParseDeviceIds(config.DeviceIdImport);
            Log.Information("Parsed device ids: {DeviceIds}", string.Join(", ", deviceIds));

            var process = DeviceMigrationProcess.Create<SourceClient, TargetClient>(config);
            Log.Information(
                "This script is going to migrate these devices from '{SourceHubName}' to '{TargetHubName}'",
                config.Connection.SourceHubName,
                config.Connection.TargetHubName);

            var errors = await process.MigrateAllAsync(deviceIds);
            Log.Information("Migrations finished for all {DeviceCount} devices {Devices}", deviceIds.Length, string.Join(", ", deviceIds));

            var unSuccessfulDevices = errors.Keys.ToList();
            Log.Information(
                "Migrations were unsuccessful for {Count} devices in total: {UnsuccessfulDevices}",
                unSuccessfulDevices.Count,
                string.Join(", ", unSuccessfulDevices));

            var successfulDevices = deviceIds.Except(errors.Keys).ToList();
            Log.Information(
                "Migrations were successful for {Count} devices in total: {SuccessfulDevices}",
                successfulDevices.Count,
                string.Join(", ", successfulDevices));
        }
        catch (ConfigParseException e)
        {
            Log.Fatal(e, "Error while parsing config: {Message}", e.Message);
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Error: {Err}", e);
        }
    }


    private static void InitLogger()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate:
                "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File(
                path: "logs/migrate-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                shared: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .CreateLogger();
    }

    private static string GetConfigFileName(string[] args)
    {
        if (args.Length > 1)
        {
            throw new ArgumentException("Too many arguments. Only 1 (or 0) argument is allowed: config file name.");
        }
        return args.ElementAtOrDefault(1) ?? DefaultConfigFile;
    }

    private static string[] ParseDeviceIds(string importFile) =>
        File.ReadAllLines(importFile)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Split(';')[0])
            .ToArray();
}