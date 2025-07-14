// See https://aka.ms/new-console-template for more information

using Microsoft.Azure.Devices;

const string IotHubConnectionString =
    "HostName=cmgtestIoTHub2.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=+6Og9v6MwNPazLiM7yqchGQOvl5Zs61tFAIoTCGrceU=";
const string DeviceId   = "device01";
const string MethodName = "asd";

using var serviceClient =
    ServiceClient.CreateFromConnectionString(IotHubConnectionString);

var methodInvocation = new CloudToDeviceMethod(MethodName)
{
    // How long you’ll wait for the device to respond
    ResponseTimeout   = TimeSpan.FromSeconds(30),
    // (Optional) how long to wait for IoT Hub to connect to the device
    ConnectionTimeout = TimeSpan.FromSeconds(10)
};

// (Optional) JSON payload for the device method
methodInvocation.SetPayloadJson("{\"example\":\"data\"}");

var result = await serviceClient.InvokeDeviceMethodAsync(DeviceId, methodInvocation);

Console.WriteLine($"Status  : {result.Status}");
Console.WriteLine($"Payload : {result.GetPayloadAsJson()}");