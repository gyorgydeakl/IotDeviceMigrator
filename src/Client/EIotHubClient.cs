using Microsoft.Azure.Devices;

namespace IotDeviceMigrator.Client;

public class EIotHubClient : ITargetIotClient
{
    public string Name => throw new NotImplementedException();

    public Task<CloudToDeviceMethodResult> InvokeMethodAsync(string deviceId, string methodName, object? payload)
    {
        throw new NotImplementedException();
    }

    public Task<bool> IsDeviceRegisteredAsync(string deviceId)
    {
        throw new NotImplementedException();
    }

    public static ITargetIotClient Create(string name, string connectionString)
    {
        throw new NotImplementedException();
    }
}