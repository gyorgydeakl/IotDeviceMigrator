using System.Text.Json;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Shared;

namespace IotDeviceMigrator.Client;

public class AzureIotClient : ISourceIotClient, ITargetIotClient
{
    public required ServiceClient ServiceClient { private get; init;}
    public required RegistryManager Registry { private get; init;}

    static ITargetIotClient ITargetIotClient.CreateFromConnectionString(string connectionString) =>
        new AzureIotClient
        {
            ServiceClient = ServiceClient.CreateFromConnectionString(connectionString),
            Registry = RegistryManager.CreateFromConnectionString(connectionString)
        };

    static ISourceIotClient ISourceIotClient.CreateFromConnectionString(string connectionString) =>
        new AzureIotClient
        {
            ServiceClient = ServiceClient.CreateFromConnectionString(connectionString),
            Registry = RegistryManager.CreateFromConnectionString(connectionString)
        };

    public async Task<bool> IsDeviceRegisteredAsync(string deviceId)
    {
        try
        {
            await Registry.GetDeviceAsync(deviceId);
        }
        catch (DeviceNotFoundException)
        {
            return false;
        }
        return true;
    }

    public async Task<TwinProperties> GetPropertiesAsync(string deviceId)
    {
        var twin = await Registry.GetTwinAsync(deviceId);
        return twin.Properties;
    }

    public async Task<CloudToDeviceMethodResult> InvokeMethodAsync(string deviceId, string methodName, object? payload)
    {
        var methodInvocation = new CloudToDeviceMethod(methodName)
        {
            ResponseTimeout   = TimeSpan.FromSeconds(30),
            ConnectionTimeout = TimeSpan.FromSeconds(10)
        };
        methodInvocation.SetPayloadJson(JsonSerializer.Serialize(payload, JsonSerializerOptions.Web));
        return await ServiceClient.InvokeDeviceMethodAsync(deviceId, methodInvocation);
    }
}