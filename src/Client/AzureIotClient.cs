using System.Text.Json;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Shared;
using Serilog;

namespace IotDeviceMigrator.Client;

public class AzureIotClient : ISourceIotClient, ITargetIotClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public required ServiceClient ServiceClient { private get; init;}

    public required RegistryManager Registry { private get; init;}

    public required string Name { get; init;}

    static ITargetIotClient ITargetIotClient.Create(string name, string connectionString) =>
        new AzureIotClient
        {
            Name = name,
            ServiceClient = ServiceClient.CreateFromConnectionString(connectionString),
            Registry = RegistryManager.CreateFromConnectionString(connectionString)
        };

    static ISourceIotClient ISourceIotClient.Create(string name, string connectionString) =>
        new AzureIotClient
        {
            Name = name,
            ServiceClient = ServiceClient.CreateFromConnectionString(connectionString),
            Registry = RegistryManager.CreateFromConnectionString(connectionString)
        };

    public async Task<bool> IsDeviceRegisteredAsync(string deviceId)
    {
        try
        {
            var device = await Registry.GetDeviceAsync(deviceId);
            return device is not null;
        }
        catch (DeviceNotFoundException)
        {
            return false;
        }
    }

    public async Task<TwinProperties?> GetPropertiesAsync(string deviceId)
    {
        var twin = await Registry.GetTwinAsync(deviceId);
        return twin?.Properties;
    }


    public async Task<CloudToDeviceMethodResult> InvokeMethodAsync(string deviceId, string methodName, object? payload)
    {
        var payloadJson = JsonSerializer.Serialize(payload, SerializerOptions);
        Log.Information("Calling {Method} on device {DeviceId} with payload {Payload}", methodName, deviceId, payloadJson);

        var methodInvocation = new CloudToDeviceMethod(methodName)
        {
            ResponseTimeout   = TimeSpan.FromSeconds(5),
            ConnectionTimeout = TimeSpan.FromSeconds(5)
        };
        methodInvocation.SetPayloadJson(payloadJson);
        return await ServiceClient.InvokeDeviceMethodAsync(deviceId, methodInvocation);
    }
}