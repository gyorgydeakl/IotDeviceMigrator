using IotDeviceMigrator.Client;
using IotDeviceMigrator.Common;
using Serilog;

namespace IotDeviceMigrator;
using IotClient = AzureIotClient;
public class BatchExecute
{
    public static async Task Run(Config config, string[] deviceIds)
    {
        var client = CreateIotClient<IotClient>(config.Connection);
        var indexedDevices = deviceIds.Select((d, idx) => (d, idx));
        foreach (var (deviceId, idx) in indexedDevices)
        {
            try
            {
                await client.InvokeMethodAsync(deviceId,
                    methodName: "setSpecConfig",
                    payload: new { wsFW = "dsuMender" });
            }
            catch (Exception e)
            {
                Log.Fatal(e, "Error: {Err}", e);
            }
        }
    }

    private static IIotClient CreateIotClient<TIotClient>(ConnectionConfig config)
    where TIotClient : ITargetIotClient
    {
        return TIotClient.Create(config.TargetHubName, config.TargetConnectionString);
    }
}