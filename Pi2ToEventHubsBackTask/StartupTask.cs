using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using ppatierno.IoT;
using ppatierno.IoTCoreSensors.Hardware;
using System.Threading.Tasks;
using System.Collections;
using ppatierno.IoT.Hardware;
using Windows.System.Threading;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace Pi2ToEventHubsBackTask
{
    public sealed class StartupTask : IBackgroundTask
    {
        private string connectionString = "[EVENT_HUBS_CONNECTIONSTRING]";
        private string eventHubEntity = "[EVENT_HUBS_NAME]";

        private IIoTClient iotClient;
        private TMP102 tmp102;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            // 
            // TODO: Insert code to start one or more asynchronous methods 
            //

            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();

            this.tmp102 = new TMP102();

            // set and open the IoT client
            if (this.iotClient == null)
            {
                //this.iotClient = new IoTClient("raspberrypi2", Guid.NewGuid().ToString(), this.connectionString, this.eventHubEntity);
                this.iotClient = new IoTClientConnectTheDots("raspberrypi2", Guid.NewGuid().ToString(), this.connectionString, this.eventHubEntity);
            }

            if (!this.iotClient.IsOpen)
                this.iotClient.Open();


            bool isOpened = await this.tmp102.OpenAsync();
            
            IDictionary bag = new Dictionary<SensorType, float>();

            while (true)
            {
                float temperature = tmp102.Temperature();
                SensorType sensorType = SensorType.Temperature;

                if (!bag.Contains(sensorType))
                    bag.Add(sensorType, temperature);
                else
                    bag[sensorType] = temperature;

                if ((this.iotClient != null) && (this.iotClient.IsOpen))
                {
                    this.iotClient.SendAsync(bag);
                }

                await Task.Delay(5000);
            }

            deferral.Complete();
        }
    }
}
