using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;

namespace IotDeviceMigrator.Client;

public interface IIotClient
{
    Task<CloudToDeviceMethodResult> InvokeMethodAsync(string deviceId, string methodName, object? payload);
}

public interface ITargetIotClient : IIotClient
{
    Task<bool> IsDeviceRegisteredAsync(string deviceId);
    static abstract ITargetIotClient CreateFromConnectionString(string connectionString);
}

public interface ISourceIotClient : IIotClient
{
    Task<TwinProperties> GetPropertiesAsync(string deviceId);
    static abstract ISourceIotClient CreateFromConnectionString(string connectionString);
}