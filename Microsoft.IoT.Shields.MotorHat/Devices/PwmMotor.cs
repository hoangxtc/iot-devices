using Windows.Devices.Pwm;

namespace Microsoft.IoT.Shields.MotorHat.Devices
{
    public sealed class PwmMotor : PwmMotorBase
    {
        private readonly PwmPin _directionPin1;
        private readonly PwmPin _directionPin2;

        public PwmMotor(PwmController pwmController, int pwmPin, int direction1Pin, int direction2Pin) : base(pwmController, pwmPin)
        {
            _directionPin1 = PwmController.OpenPin(direction1Pin);
            _directionPin2 = PwmController.OpenPin(direction2Pin);
            _directionPin1.Start();
            _directionPin2.Start();
        }

        protected override void UpdateDirection(double value)
        {
            if (value > 0)
            {
                _directionPin1.SetActiveDutyCyclePercentage(1);                
                _directionPin2.SetActiveDutyCyclePercentage(0);
            }
            else
            {
                _directionPin1.SetActiveDutyCyclePercentage(0);
                _directionPin2.SetActiveDutyCyclePercentage(1);
            }
        }

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