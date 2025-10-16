using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;

namespace IotDeviceMigrator.Client;

/// <summary>
/// Common abstraction for an IoT Hub client used by the migrator.
/// Implementations encapsulate communication with a specific IoT backend (e.g., Azure IoT Hub).
/// </summary>
public interface IIotClient
{
    /// <summary>
    /// A human-friendly identifier of the client instance (typically the hub name or environment label).
    /// Used only for logging/telemetry.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Invokes a direct method on a device.
    /// </summary>
    /// <param name="deviceId">Target device identifier.</param>
    /// <param name="methodName">Name of the device method to invoke.</param>
    /// <param name="payload">Optional payload object that will be serialized by the concrete implementation.</param>
    /// <returns>The result returned by the device for the invoked method.</returns>
    Task<CloudToDeviceMethodResult> InvokeMethodAsync(string deviceId, string methodName, object? payload);
}

/// <summary>
/// Abstraction for a target IoT client (the destination environment/hub).
/// Extends <see cref="IIotClient"/> with capabilities needed when writing to the target.
/// </summary>
public interface ITargetIotClient : IIotClient
{
    /// <summary>
    /// Checks whether a device is registered in the target hub.
    /// </summary>
    /// <param name="deviceId">Device identifier to check.</param>
    /// <returns>True if the device exists; otherwise, false.</returns>
    Task<bool> IsDeviceRegisteredAsync(string deviceId);
    
    /// <summary>
    /// Factory method to create an instance of the target client.
    /// Implemented as a static abstract member to allow strongly-typed construction
    /// without exposing concrete types to the caller.
    /// </summary>
    /// <param name="name">Logical name for the client instance (used for logging).</param>
    /// <param name="connectionString">Connection string to the target IoT backend.</param>
    /// <returns>A concrete implementation of <see cref="ITargetIotClient"/>.</returns>
    static abstract ITargetIotClient Create(string name, string connectionString);
}

/// <summary>
/// Abstraction for a source IoT client (the origin environment/hub).
/// Extends <see cref="IIotClient"/> with read capabilities needed during migration.
/// </summary>
public interface ISourceIotClient : IIotClient
{
    /// <summary>
    /// Gets the device twin properties from the source hub.
    /// </summary>
    /// <param name="deviceId">Device identifier.</param>
    /// <returns>
    /// The <see cref="TwinProperties"/> if the device exists; otherwise, null.
    /// </returns>
    Task<TwinProperties?> GetPropertiesAsync(string deviceId);
    
    /// <summary>
    /// Factory method to create an instance of the source client.
    /// Implemented as a static abstract member to allow strongly-typed construction
    /// without exposing concrete types to the caller.
    /// </summary>
    /// <param name="name">Logical name for the client instance (used for logging).</param>
    /// <param name="connectionString">Connection string to the source IoT backend.</param>
    /// <returns>A concrete implementation of <see cref="ISourceIotClient"/>.</returns>
    static abstract ISourceIotClient Create(string name, string connectionString);
}