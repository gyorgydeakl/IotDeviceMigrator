using IotDeviceMigrator.Client;

namespace IotDeviceMigrator;
using IotClient = AzureIotClient;

public class BatchExecute
{
    public static void Main(string[] args)
    {
        Logging.Init("log.txt");
        var ids = Parse.ParseDeviceIds("devices.csv");

    }
}