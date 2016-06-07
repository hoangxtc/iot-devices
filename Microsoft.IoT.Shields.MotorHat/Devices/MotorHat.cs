using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Pwm;
using Microsoft.IoT.DeviceCore.Pwm;
using Microsoft.IoT.Devices.Pwm;

namespace Microsoft.IoT.Shields.MotorHat.Devices
{
    /// <summary>
    ///     Borrowed from Adafruit.IoT.Devices.MotorHat2348 with the customization
    /// </summary>
    public sealed class MotorHat : IDisposable
    {
        private const int MaxMotorChanel = 4;
        private readonly byte _i2Caddr;
        private double _frequency;
        private PwmController _pwmController;
        private readonly List<IMotor> _motors;
        private readonly List<PwmPin> _pins = new List<PwmPin>();
        private readonly bool[] _motorChannelsUsed = new bool[MaxMotorChanel]; // There are a total of 4 motor channels
        private readonly bool[] _pinChannelsUsed = new bool[MaxMotorChanel]; // There are a total of 4 additional PWM pins
        private bool _isInitialized;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MotorHat" /> class with the specified I2C address and PWM frequency.
        /// </summary>
        /// <param name="i2CAddrress">The I2C address of the MotorHat's PWM controller.</param>
        /// <param name="frequency">The frequency in Hz to set the PWM controller.</param>
        public MotorHat(byte i2CAddrress, double frequency)
        {
            _motors = new List<IMotor>();
            _i2Caddr = i2CAddrress; // default I2C address of the HAT
            _frequency = frequency; // default @1600Hz PWM freq
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MotorHat" /> class with the specified I2C address and default PWM
        ///     frequency.
        /// </summary>
        /// <param name="i2CAddrress">The I2C address of the MotorHat's PWM controller.</param>
        /// <remarks>
        ///     The <see cref="MotorHat" /> will be created with the default frequency of 1600 Hz.
        /// </remarks>
        public MotorHat(byte i2CAddrress) : this(i2CAddrress, 1600)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MotorHat" /> class with the default I2C address and PWM frequency.
        /// </summary>
        /// <remarks>
        ///     The <see cref="MotorHat" /> will be created with the default I2C address of 0x60 and PWM frequency of 1600 Hz.
        /// </remarks>
        public MotorHat() : this(0x60, 1600)
        {
        }

        private void EnsureInitialized()
        {
            DeviceHelpers.TaskExtensions.UISafeWait(EnsureInitializedAsync);
        }

        private async Task EnsureInitializedAsync()
        {
            if (_isInitialized) return;

            // Create PWM manager
            var pwmManager = new PwmProviderManager();

            // Add providers ~ pwmControllers
            //var provider = new PCA9685Provider(_i2Caddr);
            pwmManager.Providers.Add(new PCA9685());

            // Get the well-known controller collection back
            var pwmControllers = await pwmManager.GetControllersAsync();
            _pwmController = pwmControllers.Last();
            if (_frequency > _pwmController.MaxFrequency) _frequency = _pwmController.MaxFrequency;
            _pwmController.SetDesiredFrequency(_frequency);

            _isInitialized = true;
        }

        /// <summary>
        ///     Creates a <see cref="PwmDCMotor" />  object for the specified channel and adds it to the list of Motors.
        /// </summary>
        /// <param name="driverChannel">A motor driver channel from 1 to 4.</param>
        /// <returns>The created DCMotor object.</returns>
        /// <remarks>
        ///     The driver parameter refers to the motor driver channels M1, M2, M3 or M4.
        /// </remarks>
        public PwmMotor CreateDCMotor(byte driverChannel)
        {
            if ((driverChannel < 1) || (driverChannel > MaxMotorChanel))
                throw new InvalidOperationException("CreateDCMotor parameter 'driver' must be between 1 and 4.");
            if (_motorChannelsUsed[driverChannel - 1])
                throw new MotorHatException(string.Format("Channel {0} aleady in assigned.", driverChannel));
            EnsureInitialized();

            var pwmMotor = GetAPwmMotor(driverChannel);
            _motorChannelsUsed[driverChannel - 1] = true;
            _motors.Add(pwmMotor);

            return pwmMotor;
        }

        private PwmMotor GetAPwmMotor(byte driver)
        {
            byte pwm, in1, in2;

            switch (driver)
            {
                case 0:
                    pwm = 8;
                    in2 = 9;
                    in1 = 10;
                    break;
                case 1:
                    pwm = 13;
                    in2 = 12;
                    in1 = 11;
                    break;
                case 2:
                    pwm = 2;
                    in2 = 3;
                    in1 = 4;
                    break;
                case 3:
                    pwm = 7;
                    in2 = 6;
                    in1 = 5;
                    break;
                default:
                    throw new MotorHatException("MotorHat Motor must be between 1 and 4 inclusive");
            }

            return new PwmMotor(_pwmController, pwm, in1, in2);
        }

        /// <summary>
        ///     Creates a <see cref="Windows.Devices.Pwm.PwmPin" /> for the specified channel.
        /// </summary>
        /// <param name="channel">The PWM channel number.</param>
        /// <returns>The created <see cref="Windows.Devices.Pwm.PwmPin" /> for the specified channel.</returns>
        /// <remarks>Channel numbers 1 through 4 correspond to the auxiliary PCA9685 channels 0, 1, 14 and 15.</remarks>
        public PwmPin CreatePwm(byte channel)
        {
            int pwapin;

            if ((channel < 1) || (channel > MaxMotorChanel))
                throw new ArgumentOutOfRangeException("channel");
            EnsureInitialized();

            switch (channel)
            {
                case 1:
                    pwapin = 0;
                    break;
                case 2:
                    pwapin = 1;
                    break;
                case 3:
                    pwapin = 14;
                    break;
                case 4:
                    pwapin = 15;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("channel");
            }
            _pinChannelsUsed[channel - 1] = true;
            var pwmPin = _pwmController.OpenPin(pwapin);
            _pins.Add(pwmPin);
            return pwmPin;
        }

        /// <summary>
        ///     Gets all motors created to this <see cref="MotorHat" />.
        /// </summary>
        /// <value>
        ///     A list of <see cref="IMotor" /> objects.
        /// </value>
        /// <remarks>
        ///     The method returns a list of values that represent the <see cref="IMotor" /> objects created on this
        ///     <see cref="MotorHat" />.
        /// </remarks>
        public IReadOnlyList<IMotor> Motors
        {
            get
            {
                EnsureInitialized();
                return _motors;
            }
        }

        /// <summary>
        ///     Gets all auxiliary PWM pins created to this <see cref="MotorHat" />.
        /// </summary>
        /// <value>
        ///     A list of <see cref="PwmPins" /> objects.
        /// </value>
        /// <remarks>
        ///     The method returns a list of values that represent the <see cref="PwmPins" /> objects created on this
        ///     <see cref="MotorHat" />.
        /// </remarks>
        public IReadOnlyList<PwmPin> PwmPins
        {
            get
            {
                EnsureInitialized();
                return _pins;
            }
        }

        #region IDisposable Support

        private bool _disposedValue; // To detect redundant calls

        /// <inheritdoc />
        private void Dispose(bool disposing)
        {
            if (_disposedValue) return;

            if (disposing)
            {
                for (var i = _motors.Count - 1; i >= 0; i--)
                {
                    var motor = _motors[i] as IDisposable;
                    motor?.Dispose();
                    _motors.RemoveAt(i);
                }
                for (var i = _pins.Count - 1; i >= 0; i--)
                {
                    var pin = _pins[i] as IDisposable;
                    pin?.Dispose();
                    _pins.RemoveAt(i);
                }
            }

            _disposedValue = true;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
