using System.Text.Json.Nodes;

namespace IotDeviceMigrator.Config;

internal static class JsonExtensions
{
    public static T ExpectValue<T>(this JsonNode node, string key, string configFileName)
    {
        var valueNode = node[key];
        if (valueNode is null)
        {
            throw new ConfigParseException($"'{key}' not found in config file '{configFileName}'");
        }

        var value = valueNode.GetValue<T>();
        if (value is null)
        {
            throw new ConfigParseException($"The config '{key}' is expected to be of type '{typeof(T).Name}', but it is of type '{valueNode.GetType().Name}'");
        }
        return value;
    }
}