namespace IotDeviceMigrator;

public static class Parse
{
    public static string[] ParseDeviceIds(string importFile) =>
        File.ReadAllLines(importFile)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Split(';')[0])
            .ToArray();
}