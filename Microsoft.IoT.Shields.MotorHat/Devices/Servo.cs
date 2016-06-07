using System;
using System.Diagnostics;
using Windows.Devices.Pwm;

namespace Microsoft.IoT.Shields.MotorHat.Devices
{
    public class Servo
    {
        private readonly PwmPin _pwmPin;
        private bool _limitsSet;
        private double _maxAngle;
        private double _minAngle;
        private double _offset;
        private double _position;
        private double _scale;

        internal Servo(PwmController pwmController, int pwmPin)
        {
            _position = 0.0;
            _limitsSet = false;
            _pwmPin = pwmController.OpenPin(pwmPin);
            _pwmPin.Start();
        }

        /// <summary>
        ///     The current position of the servo between the minimumAngle and maximumAngle passed to SetLimits.
        /// </summary>
        public double Position
        {
            get { return _position; }
            set
            {
                if (!_limitsSet) throw new InvalidOperationException($"You must call {nameof(SetLimits)} first.");
                if (value < _minAngle || value > _maxAngle) throw new ArgumentOutOfRangeException(nameof(value));

                _position = value;
                var dutyCyclePercentage = Math.Round(_scale * value + _offset, 2);
                _pwmPin.SetActiveDutyCyclePercentage(dutyCyclePercentage);
                Debug.WriteLine($"DutyCycle:{dutyCyclePercentage}");
            }
        }

        /// <summary>
        ///     Sets the limits of the servo.
        /// </summary>
        public void SetLimits(double minimumDutyCycle, double maximumDutyCycle, double minimumAngle, double maximumAngle)
        {
            if (minimumDutyCycle < 0) throw new ArgumentOutOfRangeException(nameof(minimumDutyCycle));
            if (maximumDutyCycle < 0) throw new ArgumentOutOfRangeException(nameof(maximumDutyCycle));
            if (minimumDutyCycle >= maximumDutyCycle) throw new ArgumentException(nameof(minimumDutyCycle));
            if (minimumAngle < 0) throw new ArgumentOutOfRangeException(nameof(minimumAngle));
            if (maximumAngle < 0) throw new ArgumentOutOfRangeException(nameof(maximumAngle));
            if (minimumAngle >= maximumAngle) throw new ArgumentException(nameof(minimumAngle));

            var pwmController = _pwmPin.Controller;
            if (!pwmController.ActualFrequency.Equals(50))
                pwmController.SetDesiredFrequency(50);

            _minAngle = minimumAngle;
            _maxAngle = maximumAngle;            

            _scale = (maximumDutyCycle - minimumDutyCycle) /(maximumAngle - minimumAngle);
            _offset = minimumDutyCycle;

            _limitsSet = true;
        }
    }
}