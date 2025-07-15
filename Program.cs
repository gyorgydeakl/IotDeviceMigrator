using IotDeviceMigrator;
using Microsoft.Azure.Devices;
const string configFile = "config.json";
const string logFile = "log.txt";
try
{
    var (hubConnectionString, deviceIds) = await Config.FromFileAsync(configFile);
    using var serviceClient = ServiceClient.CreateFromConnectionString(hubConnectionString);
    using var registry = RegistryManager.CreateFromConnectionString(hubConnectionString);

    var processes = deviceIds
        .Select(id => new DeviceMigrationProcess(id, serviceClient, registry))
        .ToList();

    foreach (var p in processes)
    {
        await p.MigrateAsync();
    }
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

