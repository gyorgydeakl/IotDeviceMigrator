using CommandLine;

namespace IotDeviceMigrator.Common;

public enum Mode { Migrate, Batch }

/// <summary>
/// Command line options.
/// </summary>
class Options
{
    [Value(0, MetaName = "mode",
        HelpText = "Run mode: migrate | batch",
        Required = true)]
    public Mode Mode { get; set; }

    [Option('c', "config", Required = false, Default = "config.json",
        HelpText = "Path to config file (default: config.json).")]
    public string Config { get; set; } = "config.json";
}

public static class Parse
{
    public static string[] ParseDeviceIds(string importFile) =>
        File.ReadAllLines(importFile)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Split(';')[0])
            .ToArray();
}