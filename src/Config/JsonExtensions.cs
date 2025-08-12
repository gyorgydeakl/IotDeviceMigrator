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

    public static T[] ExpectArray<T>(this JsonNode node, string key, string configFileName)
    {
        var arrayNode = node[key];
        if (arrayNode is null)
        {
            throw new ConfigParseException($"'{key}' not found in config file '{configFileName}'");
        }

        if (arrayNode is not JsonArray jsonArray)
        {
            throw new ConfigParseException($"The config '{key}' is expected to be an array, but it is of type '{arrayNode.GetType().Name}'");
        }

        try
        {
            return jsonArray.Select(item =>
            {
                if (item is null)
                {
                    throw new ConfigParseException($"Null element found in array '{key}' in config file '{configFileName}'");
                }
                return item.GetValue<T>();
            }).ToArray();
        }
        catch (InvalidOperationException)
        {
            throw new ConfigParseException($"One or more elements in '{key}' are not of type '{typeof(T).Name}' in config file '{configFileName}'");
        }
    }
}