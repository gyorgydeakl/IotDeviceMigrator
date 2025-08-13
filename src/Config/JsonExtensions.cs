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

        try
        {
            return valueNode.GetValue<T>();
        }
        catch (InvalidOperationException)
        {
            throw new ConfigParseException(
                $"The config '{key}' is expected to be of type '{typeof(T).Name}', but it is of type '{valueNode.GetValueKind()}'");
        }
        catch (FormatException)
        {
            throw new ConfigParseException($"The config '{key}' is expected to be of type '{typeof(T).Name}', but it is not a valid value");
        }
    }

    public static T[] ExpectArray<T>(this JsonNode node, string key, string configFileName, int minLength)
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
            var result = jsonArray.Select(item =>
            {
                if (item is null)
                {
                    throw new ConfigParseException($"Null element found in array '{key}' in config file '{configFileName}'");
                }
                return item.GetValue<T>();
            }).ToArray();

            if (result.Length < minLength)
            {
                throw new ConfigParseException($"Array '{key}' in config file '{configFileName}' must have at least {minLength} elements");
            }
            return result;
        }
        catch (InvalidOperationException)
        {
            throw new ConfigParseException($"One or more elements in '{key}' are not of type '{typeof(T).Name}' in config file '{configFileName}'");
        }
    }
}