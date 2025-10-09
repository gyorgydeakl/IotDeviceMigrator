using CommandLine;
using System.Text.Json;
using IotDeviceMigrator.Common;
using IotDeviceMigrator.Migration;
using Serilog;

namespace IotDeviceMigrator;

public class Program
{
    public static async Task Main(string[] args)
    {
        var opts = new Parser(cfg =>
        {
            cfg.CaseInsensitiveEnumValues = true;
            cfg.HelpWriter = Console.Error;
        }).ParseArguments<Options>(args);
        if (opts.Tag != ParserResultType.Parsed)
        {
            await Console.Error.WriteLineAsync("Error parsing arguments. CLI parameter usage: [migrate|batch] [-c|--config <FILE>]");
            return;
        }

        try
        {
            Logging.Init("logs/migrate-.log");

            var config = Config.FromFile(opts.Value.Config);
            Log.Information("Parsed Config: {ConfigJson}", config.ToJsonString());

            var deviceIds = Parse.ParseDeviceIds(config.DeviceIdImport);
            Log.Information("Parsed device ids: {DeviceIds}", string.Join(", ", deviceIds));

            switch (opts.Value.Mode)
            {
                case Mode.Migrate:
                    await Migrate.Run(config, deviceIds);
                    break;
                case Mode.Batch:
                    await BatchExecute.Run(config, deviceIds);
                    break;
            }
        }
        catch (JsonException e)
        {
            Log.Fatal(e, "{ConfigFileName} must match the shape of the Config class", opts.Value.Config);
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Error: {Err}", e);
        }
    }
}