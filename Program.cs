using IotDeviceMigrator;
using Microsoft.Azure.Devices;

const string defaultConfigFile = "config.json";
var logFile = "log.txt";

try
{
    if (args.Length > 1)
    {
        throw new ArgumentException("Too many arguments. Only one argument is allowed: config file name.");
    }
    var configFile = args.ElementAtOrDefault(1) ?? defaultConfigFile;

    var config = await Config.FromFileAsync(configFile);
    logFile = config.LogFile;

    using var serviceClient = ServiceClient.CreateFromConnectionString(config.HubConnectionString);
    using var registry = RegistryManager.CreateFromConnectionString(config.HubConnectionString);

    var processes = config.DeviceIds
        .Select(id => new DeviceMigrationProcess(id, serviceClient, registry))
        .ToList();

    foreach (var p in processes)
    {
        await p.MigrateAsync();
    }

    var finalMessage = $"Migration completed successfully for devices {string.Join(", ", config.DeviceIds)}";
    Console.WriteLine(finalMessage);
    await File.AppendAllLinesAsync(logFile, [finalMessage]);
}
catch (ConfigParseException e)
{
    await Console.Error.WriteLineAsync($"Error while parsing config: {e.Message}");
}
catch (MigrationException e)
{
    var message = $"Migration error: {e.Message}";
    await Console.Error.WriteLineAsync(message);
    await File.AppendAllLinesAsync(logFile, [message]);
}
catch (Exception e)
{
    await Console.Error.WriteLineAsync($"Error: {e.Message}");
}
