using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceTwinSetup
{
    class Program
    {
        static RegistryManager registryManager;
        static string DeviceID = "AndroidThingsDevice1";
        static string connectionString = "HostName=BMCHub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=tIHhvSCH6kSRn/JHY3+/7yzdYSaCA5rbzP66VengzjA=";
        static void Main(string[] args)
        {
            registryManager = RegistryManager.CreateFromConnectionString(connectionString);
            AddTagsAndQuery().Wait();
            SetDesiredConfigurationAndQuery();
            Console.WriteLine("Press Enter to exit.");
            Console.ReadLine();
        }
        public static async Task AddTagsAndQuery()
        {
            var twin = await registryManager.GetTwinAsync("AndroidThingsDevice1");
            var patch =
                @"{
             tags: {
                 location: {
                     region: 'Jakarta',
                     position: 'Kalibata'
                 }
             }
         }";
            await registryManager.UpdateTwinAsync(twin.DeviceId, patch, twin.ETag);
            var query = registryManager.CreateQuery("SELECT * FROM devices WHERE tags.location.position = 'Kalibata'", 100);
            var twinsInJakarta = await query.GetNextAsTwinAsync();
            Console.WriteLine("Devices in Jakarta: {0}", string.Join(", ", twinsInJakarta.Select(t => t.DeviceId)));

            query = registryManager.CreateQuery("SELECT * FROM devices WHERE tags.location.position = 'Kalibata' AND properties.reported.connectivity.type = 'wifi'", 100);
            var twinsUsingWifi = await query.GetNextAsTwinAsync();
            Console.WriteLine("Devices in Jakarta using wifi network: {0}", string.Join(", ", twinsUsingWifi.Select(t => t.DeviceId)));
        }
        static private async Task SetDesiredConfigurationAndQuery()
        {
            var twin = await registryManager.GetTwinAsync(DeviceID);
            var patch = new
            {
                properties = new
                {
                    desired = new
                    {
                        telemetryConfig = new
                        {
                            configId = Guid.NewGuid().ToString(),
                            sendFrequency = "0:0:10"
                        }
                    }
                }
            };

            await registryManager.UpdateTwinAsync(twin.DeviceId, JsonConvert.SerializeObject(patch), twin.ETag);
            Console.WriteLine("Updated desired configuration");

            while (true)
            {
                var query = registryManager.CreateQuery("SELECT * FROM devices WHERE deviceId = '"+DeviceID+"'");
                var results = await query.GetNextAsTwinAsync();
                foreach (var result in results)
                {
                    Console.WriteLine("Config report for: {0}", result.DeviceId);
                    Console.WriteLine("Desired telemetryConfig: {0}", JsonConvert.SerializeObject(result.Properties.Desired["telemetryConfig"], Formatting.Indented));
                    Console.WriteLine("Reported telemetryConfig: {0}", JsonConvert.SerializeObject(result.Properties.Reported["telemetryConfig"], Formatting.Indented));
                    Console.WriteLine();
                }
                Thread.Sleep(10000);
            }
        }

    }
}
