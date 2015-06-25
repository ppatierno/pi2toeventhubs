using ppatierno.IoT;
using ppatierno.IoT.Hardware;
using ppatierno.IoTCoreSensors.Hardware;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Pi2ToEventHubs
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private string connectionString = "[EVENT_HUBS_CONNECTIONSTRING]";
        private string eventHubEntity = "[EVENT_HUBS_NAME]";

        private IIoTClient iotClient;
        private TMP102 tmp102;

        public MainPage()
        {
            this.InitializeComponent();

            this.tmp102 = new TMP102();

            // set and open the IoT client
            if (this.iotClient == null)
            {
                //this.iotClient = new IoTClient("raspberrypi2", Guid.NewGuid().ToString(), this.connectionString, this.eventHubEntity);
                this.iotClient = new IoTClientConnectTheDots("raspberrypi2", Guid.NewGuid().ToString(), this.connectionString, this.eventHubEntity);
            }

            if (!this.iotClient.IsOpen)
                this.iotClient.Open();

            // just to start without UI :-)
            this.btnStart_Click(null, null);
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(async () =>
            {
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
            });

        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            this.iotClient.Close();
        }
    }
}
