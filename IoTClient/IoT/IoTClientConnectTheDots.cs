using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ppatierno.AzureSBLite.Messaging;
using ppatierno.IoT.Hardware;
using System.Diagnostics;
using System.Runtime.Serialization.Json;
using System.IO;

namespace ppatierno.IoT
{
    /// <summary>
    /// IoT client implementation for ConnectTheDots
    /// </summary>
    public class IoTClientConnectTheDots : IoTClientBase
    {
        private ConnectTheDotsSensor sensor;

        public IoTClientConnectTheDots(string deviceName, string deviceId, string connectionString, string eventhubentity)
            : base(deviceName, deviceId, connectionString, eventhubentity)
        {
            this.sensor = new ConnectTheDotsSensor();
            this.sensor.displayname = deviceName;
            this.sensor.guid = deviceId;
            this.sensor.location = "my location";
            this.sensor.organization = "my organization";
        }

        internal override EventData PrepareEventData(IDictionary bag)
        {
            foreach (SensorType type in bag.Keys)
            {
                if (type == SensorType.Temperature)
                {
                    float temperature = (float)bag[type];

                    this.sensor.measurename = Enum.GetName(typeof(SensorType), SensorType.Temperature);
                    this.sensor.timecreated = DateTime.UtcNow.ToString("o");
                    this.sensor.unitofmeasure = "C";
                    this.sensor.value = temperature;

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

            EventData data = new EventData(Encoding.UTF8.GetBytes(this.sensor.ToJson()));

            return data;
        }
    }

    /// <summary>
    /// Class to manage sensor data and attributes 
    /// </summary>
    public class ConnectTheDotsSensor
    {
        public string guid { get; set; }
        public string displayname { get; set; }
        public string organization { get; set; }
        public string location { get; set; }
        public string measurename { get; set; }
        public string unitofmeasure { get; set; }
        public string timecreated { get; set; }
        public double value { get; set; }

        /// <summary>
        /// Default parameterless constructor needed for serialization of the objects of this class
        /// </summary>
        public ConnectTheDotsSensor()
        {
        }

        /// <summary>
        /// Construtor taking parameters guid, measurename and unitofmeasure
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="measurename"></param>
        /// <param name="unitofmeasure"></param>
        public ConnectTheDotsSensor(string guid, string measurename, string unitofmeasure)
        {
            this.guid = guid;
            this.measurename = measurename;
            this.unitofmeasure = unitofmeasure;
        }

        /// <summary>
        /// ToJson function is used to convert sensor data into a JSON string to be sent to Azure Event Hub
        /// </summary>
        /// <returns>JSon String containing all info for sensor data</returns>
        public string ToJson()
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(ConnectTheDotsSensor));
            MemoryStream ms = new MemoryStream();
            ser.WriteObject(ms, this);
            string json = Encoding.UTF8.GetString(ms.ToArray(), 0, (int)ms.Length);

            return json;
        }
    }

}
