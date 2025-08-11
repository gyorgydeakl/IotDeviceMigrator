using IotDeviceMigrator;
using Microsoft.Azure.Devices;

internal class Program
{
    private const string DefaultConfigFile = "config.json";

    public static async Task Main(string[] args)
    {
        try
        {
            var config = await Config.FromFileAsync(GetConfigFileName(args)); // device ids from csv
            Console.WriteLine("Parsed Config: " + config.ToJsonString());

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
                    await p.MigrateAsync();
                }
                catch (MigrationException e)
                {
                    var message = $"Migration error: {e.Message}";
                    await Console.Error.WriteLineAsync(message);
                    await File.AppendAllLinesAsync(config.LogFile, [message]);
                }
            }

            var finalMessage = $"Migration completed successfully for devices {string.Join(", ", lines)}";
            Console.WriteLine(finalMessage);
            await File.AppendAllLinesAsync(config.LogFile, [finalMessage]);
        }
        catch (ConfigParseException e)
        {
            await Console.Error.WriteLineAsync($"Error while parsing config: {e.Message}");
        }
        catch (Exception e)
        {
            await Console.Error.WriteLineAsync($"Error: {e.Message}");
            await Console.Error.WriteLineAsync(e.StackTrace);
        }
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