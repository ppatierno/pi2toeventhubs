using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.I2c;

namespace ppatierno.IoTCoreSensors.Hardware
{
    /// <summary>
    /// Driver for Texas Instruments TMP102 Temperature Sensor
    /// </summary>
    public class TMP102
    {
        public delegate void AlertEventHandler(object sender, EventArgs e);

        #region Constants...

        // registers addresses (based on Pointer Register)
        internal const byte TEMP_REG_ADDR = 0x00;                                   // (00000000) Temperature Register (Read Only)
        internal const byte CONFIG_REG_ADDR = 0x01;                                 // (00000001) Configuration Register (Read/Write)
        internal const byte TEMP_LOW_REG_ADDR = 0x02;                               // (00000010) TLOW Register (Read/Write)
        internal const byte TEMP_HIGH_REG_ADDR = 0x03;                              // (00000011) THIGH Register (Read/Write)

        // mask for settings inside Configuration Register
        internal const ushort EXTENDED_MODE = (1 << 4);                             // Extended Mode
        internal const ushort ALERT = (1 << 5);                                     // Alert
        internal const ushort CONV_RATE_BIT0 = (1 << 6);                            // Convertion Rate (bit 0)
        internal const ushort CONV_RATE_BIT1 = (1 << 7);                            // Convertion Rate (bit 1);
        internal const ushort SHUTDOWN_MODE = (1 << 8);                             // Shutdown Mode
        internal const ushort THERMOSTAT_MODE = (1 << 9);                           // Thermostat Mode
        internal const ushort POLARITY = (1 << 10);                                 // Polarity
        internal const ushort FAULT_QUEUE_BIT0 = (1 << 11);                         // Fault Queue (bit 0)
        internal const ushort FAULT_QUEUE_BIT1 = (1 << 12);                         // Fault Queue (bit 1)
        internal const ushort CONV_RES_BIT0 = (1 << 13);                            // Conversion Resolution (bit 0)
        internal const ushort CONV_RES_BIT1 = (1 << 14);                            // Conversion Resolution (bit 1)
        internal const ushort ONE_SHOT = (1 << 15);                                 // One Shot / Conversion Ready

        // conversion rate
        internal const ushort CONV_RATE_025Hz = 0;                                  // 0.25 Hz convertion rate
        internal const ushort CONV_RATE_1Hz = CONV_RATE_BIT0;                       // 1 Hz convertion rate
        internal const ushort CONV_RATE_4Hz = CONV_RATE_BIT1;                       // 4 Hz convertion rate
        internal const ushort CONV_RATE_8Hz = CONV_RATE_BIT1 | CONV_RATE_BIT0;      // 8 Hz convertion rate
        internal const ushort CONV_RATE_MASK = CONV_RATE_BIT1 | CONV_RATE_BIT0;

        // consecutive faults
        internal const ushort FAULTS_1 = 0;                                         // 1 consecutive faults
        internal const ushort FAULTS_2 = FAULT_QUEUE_BIT0;                          // 2 consecutive faults
        internal const ushort FAULTS_4 = FAULT_QUEUE_BIT1;                          // 4 consecutive faults
        internal const ushort FAULTS_6 = FAULT_QUEUE_BIT1 | FAULT_QUEUE_BIT0;       // 6 consecutive faults
        internal const ushort FAULT_QUEUE_MASK = FAULT_QUEUE_BIT1 | FAULT_QUEUE_BIT0;

        // size of buffers I2C communications 
        private const int REG_ADDRESS_SIZE = 1;
        private const int REG_DATA_SIZE = 2;
        // I2C transaction timeout (ms)
        private const int TIMEOUT_TRANS = 1000;

        // base TMP102 address (based on A0 pin connection)
        // GND -> 0x48, VDD -> 0x49, SDA -> 0x4A, SCL -> 0x4B
        private const int TMP102_ADDRESS_BASE = 0x48;

        // default values for temperature high and low and I2C clock rate
        private const float TEMP_HIGH_DEFAULT = 80;
        private const float TEMP_LOW_DEFAULT = 75;
        private const int CLOCK_RATE_KHZ_DEFAULT = 100;

        // I2C selector
        private const string PI2_I2C_SELECTOR = "I2C1";

        #endregion

        #region Fields...

        // buffers for one and two bytes for I2C communications
        private byte[] regAddress;
        private byte[] regData;

        private ushort configuration;

        // reference to I2C device and configuration
        private I2cDevice i2c;
        private I2cConnectionSettings i2cConfig;
        // reference to the GPIO alert pin
        private GpioPin alertPin;

        public event AlertEventHandler Alert;

        #endregion

        #region Properties...

        /// <summary>
        /// Extended Mode
        /// </summary>
        public bool ExtendedMode
        {
            get { return (this.configuration & EXTENDED_MODE) == EXTENDED_MODE; }
            set
            {
                // extended mode (13 bit) not supported yet
                if (value)
                    throw new Exception("Extendend Mode not supported yet!");

                if ((this.configuration & EXTENDED_MODE) != (value ? EXTENDED_MODE : 0))
                {
                    this.configuration = (value) ? (ushort)(this.configuration | EXTENDED_MODE) : (ushort)(this.configuration & ~EXTENDED_MODE);
                    this.ChangeConfiguration();
                }
            }
        }

        /// <summary>
        /// Conversion Rate
        /// </summary>
        public ConversionRate ConversionRate
        {
            get
            {
                return (ConversionRate)(this.configuration & CONV_RATE_MASK);
            }
            set
            {
                if ((this.configuration & CONV_RATE_MASK) != (ushort)value)
                {
                    // clear and set of conversion rate
                    this.configuration = (ushort)(this.configuration & ~CONV_RATE_MASK);
                    this.configuration |= (ushort)value;
                    this.ChangeConfiguration();
                }
            }
        }

        /// <summary>
        /// Shutdown Mode
        /// </summary>
        public bool ShutdowndMode
        {
            get { return (this.configuration & SHUTDOWN_MODE) == SHUTDOWN_MODE; }
            set
            {
                if ((this.configuration & SHUTDOWN_MODE) != (value ? SHUTDOWN_MODE : 0))
                {
                    this.configuration = (value) ? (ushort)(this.configuration | SHUTDOWN_MODE) : (ushort)(this.configuration & ~SHUTDOWN_MODE);
                    this.ChangeConfiguration();
                }
            }
        }

        /// <summary>
        /// Thermostat Mode
        /// </summary>
        public ThermostatMode ThermostatMode
        {
            get
            {
                return (ThermostatMode)(this.configuration & THERMOSTAT_MODE);
            }
            set
            {
                if ((this.configuration & THERMOSTAT_MODE) != (ushort)value)
                {
                    this.configuration = (value == ThermostatMode.Interrupt) ? (ushort)(this.configuration | THERMOSTAT_MODE) : (ushort)(this.configuration & ~THERMOSTAT_MODE);
                    this.ChangeConfiguration();
                }
            }
        }

        /// <summary>
        /// Polarity (High/Low) of Alert pin
        /// </summary>
        public AlertPolarity AlertPolarity
        {
            get
            {
                return (AlertPolarity)(this.configuration & POLARITY);
            }
            set
            {
                if ((this.configuration & POLARITY) != (ushort)value)
                {
                    this.configuration = (value == AlertPolarity.ActiveHigh) ? (ushort)(this.configuration | POLARITY) : (ushort)(this.configuration & ~POLARITY);
                    this.ChangeConfiguration();
                }
            }
        }

        /// <summary>
        /// Consecutive Faults
        /// </summary>
        public ConsecutiveFaults ConsecutiveFaults
        {
            get
            {
                return (ConsecutiveFaults)(this.configuration & FAULT_QUEUE_MASK);
            }
            set
            {
                if ((this.configuration & FAULT_QUEUE_MASK) != (ushort)value)
                {
                    // clear and set of fault queue
                    this.configuration = (ushort)(this.configuration & ~FAULT_QUEUE_MASK);
                    this.configuration |= (ushort)value;
                    this.ChangeConfiguration();
                }
            }
        }

        /// <summary>
        /// Temperature High limit
        /// </summary>
        public float TemperatureHigh
        {
            get
            {
                this.regAddress[0] = TEMP_HIGH_REG_ADDR;
                this.ReadRegister(this.regAddress, this.regData);

                return this.BytesToTemperature(this.regData);
            }
            set
            {
                this.regAddress[0] = TEMP_HIGH_REG_ADDR;
                this.regData = this.TemperatureToBytes(value);
                this.WriteRegister(this.regAddress, this.regData);
            }
        }

        /// <summary>
        /// Temperature Low limit
        /// </summary>
        public float TemperatureLow
        {
            get
            {
                this.regAddress[0] = TEMP_LOW_REG_ADDR;
                this.ReadRegister(this.regAddress, this.regData);

                return this.BytesToTemperature(this.regData);
            }
            set
            {
                this.regAddress[0] = TEMP_LOW_REG_ADDR;
                this.regData = this.TemperatureToBytes(value);

                this.WriteRegister(this.regAddress, this.regData);
            }
        }


        #endregion

        #region Ctor...

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="a0addrSelect">A0 pin connection for address selection</param>
        /// <param name="clockRateKhz">I2C clock rate in KHz</param>
        public TMP102(A0AddressSelect a0addrSelect = A0AddressSelect.GND,
            int clockRateKhz = CLOCK_RATE_KHZ_DEFAULT)
        {
            // create buffers for one and two bytes for I2C communications
            this.regAddress = new byte[REG_ADDRESS_SIZE];
            this.regData = new byte[REG_DATA_SIZE];

            // configure and create I2C reference device
            this.i2cConfig = new I2cConnectionSettings((int)(TMP102_ADDRESS_BASE + a0addrSelect));
            I2cBusSpeed i2cBusSpeed = (clockRateKhz == CLOCK_RATE_KHZ_DEFAULT) ? I2cBusSpeed.StandardMode : I2cBusSpeed.FastMode;
            this.i2cConfig.BusSpeed = i2cBusSpeed;
            this.i2cConfig.SharingMode = I2cSharingMode.Exclusive;
        }

        #endregion

        /// <summary>
        /// Open driver
        /// </summary>
        /// <param name="conversionRate">Conversion rate</param>
        /// <param name="shutdownMode">Shutdown mode</param>
        /// <param name="thermostatMode">Thermostat mode</param>
        /// <param name="alertPolarity">Polarity of the alert pin</param>
        /// <param name="consecutiveFaults">Consecutive faults before activate alert pin</param>
        /// <param name="temperatureHigh">Temperature High for alert</param>
        /// <param name="temperatureLow">Temperature Low for alert</param>
        /// <param name="alertPin">Alert pin</param>
        /// <param name="i2cSelector">I2C selector string</param>
        /// <returns>Driver opened</returns>
        public async Task<bool> OpenAsync(
            ConversionRate conversionRate = ConversionRate._4Hz,
            bool shutdownMode = false,
            ThermostatMode thermostatMode = ThermostatMode.Comparator,
            AlertPolarity alertPolarity = AlertPolarity.ActiveLow,
            ConsecutiveFaults consecutiveFaults = ConsecutiveFaults._1,
            float temperatureHigh = TEMP_HIGH_DEFAULT,
            float temperatureLow = TEMP_LOW_DEFAULT,
            int alertPin = 0,
            string i2cSelector = PI2_I2C_SELECTOR)
        {
            try
            {
                string advancedQuerySyntax = I2cDevice.GetDeviceSelector(i2cSelector);
                DeviceInformationCollection device_information_collection = await DeviceInformation.FindAllAsync(advancedQuerySyntax);
                string deviceId = device_information_collection[0].Id;

                this.i2c = await I2cDevice.FromIdAsync(deviceId, this.i2cConfig);

                // load configuration register
                this.LoadConfiguration();

                // set conversion rate
                this.configuration = (ushort)(this.configuration & ~CONV_RATE_MASK);
                this.configuration |= (ushort)conversionRate;
                // set shutdown mode
                this.configuration = (shutdownMode) ? (ushort)(this.configuration | SHUTDOWN_MODE) : (ushort)(this.configuration & ~SHUTDOWN_MODE);
                // set thermostat mode
                this.configuration = (thermostatMode == ThermostatMode.Interrupt) ? (ushort)(this.configuration | THERMOSTAT_MODE) : (ushort)(this.configuration & ~THERMOSTAT_MODE);
                // set alert pin polarity
                this.configuration = (alertPolarity == AlertPolarity.ActiveHigh) ? (ushort)(this.configuration | POLARITY) : (ushort)(this.configuration & ~POLARITY);
                // set consecutive faults for alert
                this.configuration = (ushort)(this.configuration & ~FAULT_QUEUE_MASK);
                this.configuration |= (ushort)consecutiveFaults;

                // save configuration register
                this.ChangeConfiguration();

                // set temperature high for alert
                this.regAddress[0] = TEMP_HIGH_REG_ADDR;
                this.regData = this.TemperatureToBytes(temperatureHigh);
                this.WriteRegister(this.regAddress, this.regData);
                // set temperature low for alert
                this.regAddress[0] = TEMP_LOW_REG_ADDR;
                this.regData = this.TemperatureToBytes(temperatureLow);
                this.WriteRegister(this.regAddress, this.regData);

                if (alertPin != 0)
                {
                    GpioController gpioController = GpioController.GetDefault();
                    this.alertPin = gpioController.OpenPin(alertPin);
                    this.alertPin.ValueChanged += AlertPin_ValueChanged;
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private void AlertPin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            this.OnAlert();
        }

        /// <summary>
        /// Raise the Alter event
        /// </summary>
        private void OnAlert()
        {
            if (this.Alert != null)
            {
                this.Alert(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Retrieve temperature value from the register
        /// </summary>
        /// <returns>Temperature value (in �C)</returns>
        public float Temperature()
        {
            // if sensor is set in shutdown mode
            if (this.ShutdowndMode)
            {
                // execute a one shot conversion
                this.configuration |= ONE_SHOT;
                this.ChangeConfiguration();

                // during conversion, the sensor set ONE_SHOT bit to 1
                // and set it to 0 when the conversion end
                do
                {
                    this.LoadConfiguration();
                }
                while ((this.configuration & ONE_SHOT) != ONE_SHOT);
            }

            this.regAddress[0] = TEMP_REG_ADDR;
            this.ReadRegister(this.regAddress, this.regData);

            return this.BytesToTemperature(this.regData);
        }

        /// <summary>
        /// Convert data bytes in temperature value
        /// </summary>
        /// <param name="data">Bytes represent temperature value</param>
        /// <returns>Temperature value (in �C)</returns>
        private float BytesToTemperature(byte[] data)
        {
            // we are interested in only 12 bit from temperature register
            short rawTemp = (short)((data[0] << 4) | (data[1] >> 4));

            float temperature = 0;
            // if the 12 bit MSB is 1 then the temperature is negative
            if ((rawTemp & 0x0800) == 0x0800)
            {
                rawTemp -= 1;
                rawTemp = (short)(~rawTemp & 0x0FFF);
                temperature = rawTemp * -0.0625f;
            }
            else
                temperature = rawTemp * 0.0625f;

            return temperature;
        }

        /// <summary>
        /// Convert temperature value in data bytes
        /// </summary>
        /// <param name="temperature">Temperature value (in �C)</param>
        /// <returns>Bytes represent temperature value</returns>
        private byte[] TemperatureToBytes(float temperature)
        {
            byte[] data = new byte[REG_DATA_SIZE];
            short rawTemp = 0;
            if (temperature >= 0)
            {
                rawTemp = (short)(temperature / 0.0625);
            }
            else
            {
                rawTemp = (short)(-temperature / 0.0625);
                rawTemp = (short)(~rawTemp & 0x0FFF);
                rawTemp += 1;
            }

            data[0] = (byte)((rawTemp >> 4) & 0x00FF);
            data[1] = (byte)((rawTemp << 4) & 0x00F0);

            return data;
        }

        /// <summary>
        /// Load configuration from Configuration Register
        /// </summary>
        private void LoadConfiguration()
        {
            this.regAddress[0] = CONFIG_REG_ADDR;
            this.ReadRegister(this.regAddress, this.regData);
            this.configuration = (ushort)((this.regData[0] << 8) | this.regData[1]);
        }

        /// <summary>
        /// Change configuration into Configuration Register
        /// </summary>
        private void ChangeConfiguration()
        {
            this.regAddress[0] = CONFIG_REG_ADDR;
            this.regData[0] = (byte)((this.configuration >> 8) & 0x00FF);
            this.regData[1] = (byte)(this.configuration & 0x00FF);
            this.WriteRegister(this.regAddress, this.regData);
        }

        /// <summary>
        /// Execute a read I2C transaction from a register
        /// </summary>
        /// <param name="register">Address register for reading</param>
        /// <param name="data">Data buffer read from register</param>
        private void ReadRegister(byte[] register, byte[] data)
        {
            try
            {
                this.i2c.WriteRead(register, data);
            }
            catch
            {
                throw new Exception("Error executing I2C reading from register");
            }
        }

        /// <summary>
        /// Execute a write I2C transaction to a register
        /// </summary>
        /// <param name="register">Address register for writing</param>
        /// <param name="data">Data buffer write to register</param>
        private void WriteRegister(byte[] register, byte[] data)
        {
            try
            {
                this.i2c.Write(new byte[] { register[0], data[0], data[1] });
            }
            catch
            {
                throw new Exception("Error executing I2C writing to register");
            }
        }
    }

    /// <summary>
    /// Conversion rate
    /// </summary>
    public enum ConversionRate
    {
        /// <summary>
        /// 0.25 Hz
        /// </summary>
        _025Hz = TMP102.CONV_RATE_025Hz,

        /// <summary>
        /// 1 Hz
        /// </summary>
        _1Hz = TMP102.CONV_RATE_1Hz,

        /// <summary>
        /// 4 Hz
        /// </summary>
        _4Hz = TMP102.CONV_RATE_4Hz,

        /// <summary>
        /// 8 Hz
        /// </summary>
        _8Hz = TMP102.CONV_RATE_8Hz
    }

    /// <summary>
    /// Polarity of the Alert pin output
    /// </summary>
    public enum AlertPolarity
    {
        /// <summary>
        /// Alert pin will be active low
        /// </summary>
        ActiveLow = 0,

        /// <summary>
        /// Alert pin will be active high
        /// </summary>
        ActiveHigh = TMP102.POLARITY
    }

    /// <summary>
    /// Consecutive faults
    /// </summary>
    public enum ConsecutiveFaults
    {
        _1 = TMP102.FAULTS_1,
        _2 = TMP102.FAULTS_2,
        _4 = TMP102.FAULTS_4,
        _6 = TMP102.FAULTS_6
    }

    /// <summary>
    /// Thermostat mode
    /// </summary>
    public enum ThermostatMode
    {
        /// <summary>
        /// Comparator Mode
        /// </summary>
        Comparator = 0,

        /// <summary>
        /// Interrupt Mode
        /// </summary>
        Interrupt = TMP102.THERMOSTAT_MODE
    }

    /// <summary>
    /// A0 Pin connection for address selection
    /// </summary>
    public enum A0AddressSelect
    {
        GND = 0,
        VDD = 1,
        SDA = 2,
        SCL = 3
    }
}
