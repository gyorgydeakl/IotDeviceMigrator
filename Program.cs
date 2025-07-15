using IotDeviceMigrator;
using Microsoft.Azure.Devices;

internal class Program
{
    private const string DefaultConfigFile = "config.json";
    private static string _logFile = "log.txt";

    public static async Task Main(string[] args)
    {
        try
        {
            var (hubConnectionString, logFile, deviceIds) = await Config.FromFileAsync(GetConfigFileName(args));
            _logFile = logFile;

            using var serviceClient = ServiceClient.CreateFromConnectionString(hubConnectionString);
            using var registry = RegistryManager.CreateFromConnectionString(hubConnectionString);

            var processes = deviceIds
                .Select(id => new DeviceMigrationProcess(id, serviceClient, registry))
                .ToList();

            foreach (var p in processes)
            {
                await p.MigrateAsync();
            }

            var finalMessage = $"Migration completed successfully for devices {string.Join(", ", deviceIds)}";
            Console.WriteLine(finalMessage);
            await File.AppendAllLinesAsync(_logFile, [finalMessage]);
        }
        catch (ConfigParseException e)
        {
            await Console.Error.WriteLineAsync($"Error while parsing config: {e.Message}");
        }
        catch (MigrationException e)
        {
            var message = $"Migration error: {e.Message}";
            await Console.Error.WriteLineAsync(message);
            await File.AppendAllLinesAsync(_logFile, [message]);
        }
        catch (Exception e)
        {
            await Console.Error.WriteLineAsync($"Error: {e.Message}");
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