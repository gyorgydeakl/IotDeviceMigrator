namespace IotDeviceMigrator.Common;

public static class Parse
{
    public static string[] ParseDeviceIds(string importFile) =>
        File.ReadAllLines(importFile)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Split(';')[0])
            .ToArray();
    public static string GetConfigFileName(string[] args)
    {
        const string defaultConfigFile = "config.json";
        if (args.Length > 1)
        {
            throw new ArgumentException("Too many arguments. Only 1 (or 0) argument is allowed: config file name.");
        }
        return args.ElementAtOrDefault(1) ?? defaultConfigFile;
    }
}