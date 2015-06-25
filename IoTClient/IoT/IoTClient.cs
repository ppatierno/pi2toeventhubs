using System;
using System.Collections;
using ppatierno.AzureSBLite.Messaging;
using System.Diagnostics;
using ppatierno.IoT.Hardware;

namespace ppatierno.IoT
{
    /// <summary>
    /// IoT client implementation
    /// </summary>
    public class IoTClient : IoTClientBase
    {
        public IoTClient(string deviceName, string deviceId, string connectionString, string eventhubentity)
            : base(deviceName, deviceId, connectionString, eventhubentity)
        {
        }

        internal override EventData PrepareEventData(IDictionary bag)
        {
            EventData data = new EventData();

            foreach (SensorType type in bag.Keys)
            {
                if (type == SensorType.Temperature)
                {
                    float temperature = (float)bag[type];
                    data.Properties["time"] = DateTime.UtcNow;
                    data.Properties["temp"] = temperature;
                    Debug.WriteLine("temp: " + temperature);
                }
                else if (type == SensorType.Humidity)
                {
                    // nothing
                }
                else if (type == SensorType.Accelerometer)
                {
                    // nothing
                }
            }

            data.PartitionKey = this.DeviceId;

            return data;
        }
    }
}
