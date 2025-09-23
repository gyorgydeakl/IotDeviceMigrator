using System.Text.Json;
using IotDeviceMigrator.Common;
using Serilog;

namespace IotDeviceMigrator;

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            Logging.Init("logs/migrate-.log");
            var config = Config.FromFile(Parse.GetConfigFileName(args));
            Log.Information("Parsed Config: {ConfigJson}", config.ToJsonString());

            var deviceIds = Parse.ParseDeviceIds(config.DeviceIdImport);
            Log.Information("Parsed device ids: {DeviceIds}", string.Join(", ", deviceIds));

            await BatchExecute.Run(config, deviceIds);
        }
        catch (JsonException e)
        {
            Log.Fatal(e, "{ConfigFileName} must match the shape of the Config class", Parse.GetConfigFileName(args));
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Error: {Err}", e);
        }
    }
}