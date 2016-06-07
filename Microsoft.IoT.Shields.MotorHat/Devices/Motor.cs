using Windows.Devices.Gpio;
using Windows.Devices.Pwm;

namespace Microsoft.IoT.Shields.MotorHat.Devices
{
    public sealed class Motor : PwmMotorBase
    {
        private readonly GpioPin _directionPin1;
        private readonly GpioPin _directionPin2;        

        internal Motor(PwmController pwmController, int pwmPin, int direction1Pin, int direction2Pin) : base(pwmController, pwmPin)
        {
            var gpioController = GpioController.GetDefault();
            _directionPin1 = gpioController.OpenPin(direction1Pin);
            _directionPin2 = gpioController.OpenPin(direction2Pin);
            _directionPin1.SetDriveMode(GpioPinDriveMode.Output);
            _directionPin2.SetDriveMode(GpioPinDriveMode.Output);
        }

        protected override void UpdateDirection(double value)
        {
            _directionPin1.Write(value > 0 ? GpioPinValue.High : GpioPinValue.Low);
            _directionPin2.Write(value < 0 ? GpioPinValue.High : GpioPinValue.Low);
        }

        /// <summary>
        ///     Disposes of the object releasing control the pins.
        /// </summary>
        public override void Dispose() => Dispose(true);

        /// <summary>
        ///     Disposes of the object releasing control the pins.
        /// </summary>
        /// <param name="disposing">Whether or not this method is called from Dispose().</param>
        private void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                PwmPin.Dispose();
                _directionPin1.Dispose();
                _directionPin2.Dispose();
            }

            Disposed = true;
        }
    }
}