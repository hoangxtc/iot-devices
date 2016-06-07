using System;
using Windows.Devices.Pwm;

namespace Microsoft.IoT.Shields.MotorHat.Devices
{
    public abstract class PwmMotorBase : IMotor, IDisposable
    {
        public PwmController PwmController { get; set; }
        protected readonly PwmPin PwmPin;        
        private double _speed;
        protected bool Disposed { get; set; }

        protected PwmMotorBase(PwmController pwmController, int pwmPin)
        {
            if (pwmController == null) throw new ArgumentNullException(nameof(pwmController));
            PwmController = pwmController;
            PwmPin = PwmController.OpenPin(pwmPin);
            PwmPin.Start();
        }

        /// <summary>
        ///     The speed of the motor. The sign controls the direction while the magnitude controls the speed (0 is off, 1 is full
        ///     speed).
        /// </summary>
        public double Speed
        {
            get { return _speed; }
            set
            {
                PwmPin.SetActiveDutyCyclePercentage(0);

                UpdateDirection(value);

                PwmPin.SetActiveDutyCyclePercentage(Math.Abs(value));

                _speed = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        protected abstract void UpdateDirection(double value);

        /// <summary>
        ///     Disposes of the object releasing control the pins.
        /// </summary>
        public abstract void Dispose();
    }
}