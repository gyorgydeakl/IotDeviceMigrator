using IotDeviceMigrator.Client;
using IotDeviceMigrator.Config;
using IotDeviceMigrator.Migration;
using Serilog;

internal class Program
{
    private const string DefaultConfigFile = "config.json";

    public static async Task Main(string[] args)
    {
        InitLogger();
        try
        {
            var config = await Config.FromFileAsync(GetConfigFileName(args));
            Log.Information("Parsed Config: {ConfigJson}", config.ToJsonString());

            var lines = await File.ReadAllLinesAsync(config.DeviceIdImport);

            var processes = lines
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Split(';')[0])
                .Select(id => DeviceMigrationProcess.Create<AzureIotClient, AzureIotClient>(id, config))
                .ToList();

            foreach (var p in processes)
            {
                try
                {
                    var result = await p.MigrateAsync();
                    Log.Information("Migration result: {Result}", result);
                }
                catch (MigrationException e)
                {
                    var message = $"Migration error: {e.Message}";
                    Log.Error(message);
                    await File.AppendAllLinesAsync(config.LogFile, [message]);
                }
            }

            var finalMessage = $"Migration completed successfully for devices {string.Join(", ", lines)}";
            Log.Information(finalMessage);
            await File.AppendAllLinesAsync(config.LogFile, [finalMessage]);
        }
        catch (ConfigParseException e)
        {
            Log.Fatal("Error while parsing config: {Message}", e.Message);
        }
        catch (Exception e)
        {
            Log.Fatal("Error: {Message}", e);
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
}