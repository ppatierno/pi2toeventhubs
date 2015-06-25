using System;
using Windows.Devices.Gpio;

namespace ppatierno.IoTCoreSensors.Hardware
{
    /// <summary>
    /// Args for Pir motion event
    /// </summary>
    public class PirEventArgs : EventArgs
    {
        /// <summary>
        /// Motion detected state
        /// </summary>
        public bool Motion { get; internal set; }

        /// <summary>
        /// Timestamp when motion detected or not occured
        /// </summary>
        public DateTime Time { get; internal set; }
    }

    /// <summary>
    /// Driver for Passive Infrared Sensor
    /// </summary>
    public class Pir : IDisposable
    {
        /// <summary>
        /// Delegate that define motion event handler
        /// </summary>
        public delegate void MotionEventHandler(object sender, PirEventArgs e);

        #region Fields...

        // reference to GPIO pin
        private GpioPin gpioPin;

        // motion event
        public event MotionEventHandler Motion;

        // Pir enable state
        private bool enabled;

        #endregion

        #region Properties...

        /// <summary>
        /// Pir enable state
        /// </summary>
        public bool Enabled
        {
            get
            {
                return this.enabled;
            }
            set
            {
                if (value != this.enabled)
                {
                    this.enabled = value;
                    if (this.enabled)
                        this.gpioPin.ValueChanged += GpioPin_ValueChanged;
                    else
                        this.gpioPin.ValueChanged -= GpioPin_ValueChanged;
                }
            }
        }

        #endregion


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="pin">Pin connected to the PIR</param>
        /// <param name="enabled">Intial PIR enable state</param>
        public Pir(int pin, bool enabled = true)
        {
            GpioController gpioController = GpioController.GetDefault();

            this.gpioPin = gpioController.OpenPin(pin);
            this.gpioPin.SetDriveMode(GpioPinDriveMode.Input);

            this.Enabled = enabled;
        }

        private void GpioPin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            this.OnMotion(args.Edge == GpioPinEdge.RisingEdge, DateTime.Now);
        }


        /// <summary>
        /// Wrapper method for raising motion event
        /// </summary>
        /// <param name="motion">Motion detected state</param>
        /// <param name="time">Timestamp when motion detected or not occured</param>
        private void OnMotion(bool motion, DateTime time)
        {
            if (this.Motion != null)
                this.Motion(this, new PirEventArgs() { Motion = motion, Time = time });
        }

        #region IDisposable and Dispose Pattern...

        /// <summary>
        /// Disponse() method from IDisposable interface
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Internal dispose method
        /// </summary>
        /// <param name="disposing">Disposing managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.Enabled = false;
                this.gpioPin.Dispose();
            }
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~Pir()
        {
            this.Dispose(false);
        }

        #endregion
    }
}
