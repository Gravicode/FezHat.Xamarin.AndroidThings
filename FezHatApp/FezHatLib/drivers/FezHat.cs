using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using Android.Things.Pio;
using GHIElectronics.UWP.LowLevelDrivers;
using System.Threading.Tasks;

namespace GHIElectronics.UWP.Shields
{
    /// <summary>
    /// A helper class for the FEZ HAT.
    /// </summary>
    public class FezHat : IDisposable
    {
        private bool disposed;
        private PCA9685 pwm;
        private ADS7830 analog;
        private MMA8453 accelerometer;
        private IGpio motorEnable;
        private IGpio dio16;
        private IGpio dio26;
        private IGpio dio24;
        private IGpio dio18;
        private IGpio dio22;

        public static int SpiChipSelectLine => 0;

        /// <summary>
        /// The SPI device name exposed on the header.
        /// </summary>
        public static String SpiDeviceName => "SPI0.0";//SPI0

        /// <summary>
        /// The I2C device name exposed on the header.
        /// </summary>
        public static  String I2cDeviceName => "I2C1";

        /// <summary>
        /// The frequency that the onboard PWM controller outputs. All PWM pins use the same frequency, only the duty cycle is controllable.
        /// </summary>
        /// <remarks>
        /// Care needs to be taken when using the exposed PWM pins, motors, or servos. Motors generally require a high frequency while servos require a specific low frequency, usually 50Hz.
        /// If you set the frequency to a certain value, you may impair the ability of another part of the board to function.
        /// </remarks>
        public int PwmFrequency
        {
            get
            {
                return this.pwm.Frequency;
            }
            set
            {
                this.pwm.Frequency = value;
            }
        }

        /// <summary>
        /// The object used to control the motor terminal labeled A.
        /// </summary>
        public Motor MotorA { get; private set; }

        /// <summary>
        /// The object used to control the motor terminal labeled A.
        /// </summary>
        public Motor MotorB { get; private set; }

        /// <summary>
        /// The object used to control the RGB led labeled D2.
        /// </summary>
        public RgbLed D2 { get; private set; }

        /// <summary>
        /// The object used to control the RGB led labeled D3.
        /// </summary>
        public RgbLed D3 { get; private set; }

        /// <summary>
        /// The object used to control the servo header labeled S1.
        /// </summary>
        public Servo S1 { get; private set; }

        /// <summary>
        /// The object used to control the servo header labeled S2.
        /// </summary>
        public Servo S2 { get; private set; }

        /// <summary>
        /// Whether or not the DIO24 led is on or off.
        /// </summary>
        public bool DIO24On
        {
            get
            {
                return this.dio24.Value;
            }
            set
            {
                this.dio24.Value = value;
            }
        }

        /// <summary>
        /// Whether or not the button labeled DIO18 is pressed.
        /// </summary>
        /// <returns>The pressed state.</returns>
        public bool IsDIO18Pressed() => this.dio18.Value == false;

        /// <summary>
        /// Whether or not the button labeled DIO18 is pressed.
        /// </summary>
        /// <returns>The pressed state.</returns>
        public bool IsDIO22Pressed() => this.dio22.Value == false;

        /// <summary>
        /// Gets the light level from the onboard sensor.
        /// </summary>
        /// <returns>The light level between 0 (low) and 1 (high).</returns>
        public double GetLightLevel() => this.analog.Read(5);

        /// <summary>
        /// Gets the temperature in celsius from the onboard sensor.
        /// </summary>
        /// <returns>The temperature.</returns>
        public double GetTemperature() => (this.analog.Read(4) * 3300.0 - 450.0) / 19.5;

        /// <summary>
        /// Gets the acceleration in G's for each axis from the onboard sensor.
        /// </summary>
        /// <param name="x">The current X-axis acceleration.</param>
        /// <param name="y">The current Y-axis acceleration.</param>
        /// <param name="z">The current Z-axis acceleration.</param>
        public void GetAcceleration(out double x, out double y, out double z) => this.accelerometer.GetAcceleration(out x, out y, out z);

        /// <summary>
        /// Disposes of the object releasing control the pins.
        /// </summary>
        public void Dispose() => this.Dispose(true);

        private FezHat()
        {
            this.disposed = false;
        }

        /// <summary>
        /// Disposes of the object releasing control the pins.
        /// </summary>
        /// <param name="disposing">Whether or not this method is called from Dispose().</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.pwm.Dispose();
                    this.analog.Dispose();
                    this.accelerometer.Dispose();
                    this.motorEnable.Dispose();
                    this.dio16.Dispose();
                    this.dio26.Dispose();
                    this.dio24.Dispose();
                    this.dio18.Dispose();
                    this.dio22.Dispose();

                    this.MotorA.Dispose();
                    this.MotorB.Dispose();
                }

                this.disposed = true;
            }
        }

        /// <summary>
        /// Creates a new instance of the FEZ HAT.
        /// </summary>
        /// <returns>The new instance.</returns>
        public static FezHat Create()
        {
            PeripheralManager manager = PeripheralManager.Instance;

            //var gpioController = GpioController.GetDefault();
            //var i2cController = (await DeviceInformation.FindAllAsync(I2cDevice.GetDeviceSelector(FezHat.I2cDeviceName)))[0];
            FezHat hat = new FezHat();

            hat.accelerometer = new MMA8453(I2cDeviceName);
            hat.analog = new ADS7830(I2cDeviceName);

            hat.pwm = new PCA9685(I2cDeviceName, "BCM13");//gpioController.OpenPin(13)
            hat.pwm.OutputEnabled = true;
            hat.pwm.Frequency = 1500;

            hat.dio16 = manager.OpenGpio("BCM16");//gpioController.OpenPin(16);
            hat.dio26 = manager.OpenGpio("BCM26");//gpioController.OpenPin(26);
            hat.dio24 = manager.OpenGpio("BCM24");//gpioController.OpenPin(24);
            hat.dio18 = manager.OpenGpio("BCM18");//gpioController.OpenPin(18);xx
            hat.dio22 = manager.OpenGpio("BCM22");//gpioController.OpenPin(22);

            //hat.dio16.SetDriveMode(GpioPinDriveMode.Input);
            // Initialize the pin as an input
            hat.dio16.SetDirection(Gpio.DirectionIn);
            // High voltage is considered active
            hat.dio16.SetActiveType(Gpio.ActiveHigh);

            //hat.dio26.SetDriveMode(GpioPinDriveMode.Input);
            // Initialize the pin as an input
            hat.dio26.SetDirection(Gpio.DirectionIn);
            // High voltage is considered active
            hat.dio26.SetActiveType(Gpio.ActiveHigh);

            //hat.dio24.SetDriveMode(GpioPinDriveMode.Output);
            // Initialize the pin as a high output
            hat.dio24.SetDirection(Gpio.DirectionOutInitiallyHigh);
            // Low voltage is considered active
            hat.dio24.SetActiveType(Gpio.ActiveHigh);

            //hat.dio18.SetDriveMode(GpioPinDriveMode.Input);
            // Initialize the pin as an input
            hat.dio18.SetDirection(Gpio.DirectionIn);
            // High voltage is considered active
            hat.dio18.SetActiveType(Gpio.ActiveHigh);

            //hat.dio22.SetDriveMode(GpioPinDriveMode.Input);
            // Initialize the pin as an input
            hat.dio22.SetDirection(Gpio.DirectionIn);
            // High voltage is considered active
            hat.dio22.SetActiveType(Gpio.ActiveHigh);

            hat.motorEnable = manager.OpenGpio("BCM12");//gpioController.OpenPin(12);
            //hat.motorEnable.SetDriveMode(GpioPinDriveMode.Output);
            // Initialize the pin as a high output
            hat.motorEnable.SetDirection(Gpio.DirectionOutInitiallyHigh);
            // Low voltage is considered active
            hat.motorEnable.SetActiveType(Gpio.ActiveHigh);
            hat.motorEnable.Value = true;

            //hat.MotorA = new Motor(hat.pwm, 14, 27, 23);
            hat.MotorA = new Motor(hat.pwm, 14, "BCM27", "BCM23");
            //hat.MotorB = new Motor(hat.pwm, 13, 6, 5);
            hat.MotorB = new Motor(hat.pwm, 13, "BCM6", "BCM5");

            hat.D2 = new RgbLed(hat.pwm, 1, 0, 2);
            hat.D3 = new RgbLed(hat.pwm, 4, 3, 15);

            hat.S1 = new Servo(hat.pwm, 9);
            hat.S2 = new Servo(hat.pwm, 10);

            return hat;
        }

        /// <summary>
        /// Sets the duty cycle of the given pwm pin.
        /// </summary>
        /// <param name="pin">The pin to set the duty cycle for.</param>
        /// <param name="value">The new duty cycle between 0 (off) and 1 (on).</param>
        public void SetPwmDutyCycle(PwmPin pin, double value)
        {
            if (value < 0.0 || value > 1.0) throw new ArgumentOutOfRangeException(nameof(value));
            if (!Enum.IsDefined(typeof(PwmPin), pin)) throw new ArgumentException(nameof(pin));

            this.pwm.SetDutyCycle((int)pin, value);
        }

        /// <summary>
        /// Write the given value to the given pin.
        /// </summary>
        /// <param name="pin">The pin to set.</param>
        /// <param name="state">The new state of the pin.</param>
        public void WriteDigital(DigitalPin pin, bool state)
        {
            IGpio gpioPin = pin == DigitalPin.DIO16 ? this.dio16 : this.dio26;

            //if (gpioPin. != GpioPinDriveMode.Output)
            //gpioPin.SetDriveMode(GpioPinDriveMode.Output);
            // Initialize the pin as a high output
            gpioPin.SetDirection(Gpio.DirectionOutInitiallyHigh);
            // Low voltage is considered active
            gpioPin.SetActiveType(Gpio.ActiveHigh);

            gpioPin.Value = (state ? true : false);
        }

        /// <summary>
        /// Reads the current state of the given pin.
        /// </summary>
        /// <param name="pin">The pin to read.</param>
        /// <returns>True if high, false is low.</returns>
        public bool ReadDigital(DigitalPin pin)
        {
            //if (!Enum.IsDefined(typeof(DigitalPin), pin)) throw new ArgumentException(nameof(pin));

            IGpio gpioPin = pin == DigitalPin.DIO16 ? this.dio16 : this.dio26;

            //if (gpioPin.GetDriveMode() != GpioPinDriveMode.Input)
            //  gpioPin.SetDriveMode(GpioPinDriveMode.Input);

            // Initialize the pin as an input
            gpioPin.SetDirection(Gpio.DirectionIn);
            // High voltage is considered active
            gpioPin.SetActiveType(Gpio.ActiveHigh);

            return gpioPin.Value == true;
        }

        /// <summary>
        /// Reads the current voltage on the given pin.
        /// </summary>
        /// <param name="pin">The pin to read.</param>
        /// <returns>The voltage between 0 (0V) and 1 (3.3V).</returns>
        public double ReadAnalog(AnalogPin pin)
        {
            if (!Enum.IsDefined(typeof(AnalogPin), pin)) throw new ArgumentException(nameof(pin));

            return this.analog.Read((byte)pin);
        }

        /// <summary>
        /// The possible analog pins.
        /// </summary>
        public enum AnalogPin
        {
            /// <summary>An analog pin.</summary>
            Ain1 = 1,
            /// <summary>An analog pin.</summary>
            Ain2 = 2,
            /// <summary>An analog pin.</summary>
            Ain3 = 3,
            /// <summary>An analog pin.</summary>
            Ain6 = 6,
            /// <summary>An analog pin.</summary>
            Ain7 = 7,
        }

        /// <summary>
        /// The possible pwm pins.
        /// </summary>
        public enum PwmPin
        {
            /// <summary>A pwm pin.</summary>
            Pwm5 = 5,
            /// <summary>A pwm pin.</summary>
            Pwm6 = 6,
            /// <summary>A pwm pin.</summary>
            Pwm7 = 7,
            /// <summary>A pwm pin.</summary>
            Pwm11 = 11,
            /// <summary>A pwm pin.</summary>
            Pwm12 = 12,
        }

        /// <summary>
        /// The possible digital pins.
        /// </summary>
        public enum DigitalPin
        {
            /// <summary>A digital pin.</summary>
            DIO16,
            /// <summary>A digital pin.</summary>
            DIO26
        }

        /// <summary>
        /// Represents a color of the onboard LEDs.
        /// </summary>
        public class Color
        {
            /// <summary>
            /// The red channel intensity.
            /// </summary>
            public byte R { get; }
            /// <summary>
            /// The green channel intensity.
            /// </summary>
            public byte G { get; }
            /// <summary>
            /// The blue channel intensity.
            /// </summary>
            public byte B { get; }

            /// <summary>
            /// Constructs a new color.
            /// </summary>
            /// <param name="red">The red channel intensity.</param>
            /// <param name="green">The green channel intensity.</param>
            /// <param name="blue">The blue channel intensity.</param>
            public Color(byte red, byte green, byte blue)
            {
                this.R = red;
                this.G = green;
                this.B = blue;
            }

            /// <summary>
            /// A predefined red color.
            /// </summary>
            public static Color Red => new Color(255, 0, 0);

            /// <summary>
            /// A predefined green color.
            /// </summary>
            public static Color Green => new Color(0, 255, 0);

            /// <summary>
            /// A predefined blue color.
            /// </summary>
            public static Color Blue => new Color(0, 0, 255);

            /// <summary>
            /// A predefined cyan color.
            /// </summary>
            public static Color Cyan => new Color(0, 255, 255);

            /// <summary>
            /// A predefined magneta color.
            /// </summary>
            public static Color Magneta => new Color(255, 0, 255);

            /// <summary>
            /// A predefined yellow color.
            /// </summary>
            public static Color Yellow => new Color(255, 255, 0);

            /// <summary>
            /// A predefined white color.
            /// </summary>
            public static Color White => new Color(255, 255, 255);

            /// <summary>
            /// A predefined black color.
            /// </summary>
            public static Color Black => new Color(0, 0, 0);
        }

        /// <summary>
        /// Represents an onboard RGB led.
        /// </summary>
        public class RgbLed
        {
            private PCA9685 pwm;
            private Color color;
            private int redChannel;
            private int greenChannel;
            private int blueChannel;

            /// <summary>
            /// The current color of the LED.
            /// </summary>
            public Color Color
            {
                get
                {
                    return this.color;
                }
                set
                {
                    this.color = value;

                    this.pwm.SetDutyCycle(this.redChannel, (double)value.R / 255.0);
                    this.pwm.SetDutyCycle(this.greenChannel, (double)value.G / 255.0);
                    this.pwm.SetDutyCycle(this.blueChannel, (double)value.B / 255.0);
                }
            }

            internal RgbLed(PCA9685 pwm, int redChannel, int greenChannel, int blueChannel)
            {
                this.color = Color.Black;
                this.pwm = pwm;
                this.redChannel = redChannel;
                this.greenChannel = greenChannel;
                this.blueChannel = blueChannel;
            }

            /// <summary>
            /// Turns the LED off.
            /// </summary>
            public void TurnOff()
            {
                this.pwm.SetDutyCycle(this.redChannel, 0.0);
                this.pwm.SetDutyCycle(this.greenChannel, 0.0);
                this.pwm.SetDutyCycle(this.blueChannel, 0.0);
            }
        }

        /// <summary>
        /// Represents an onboard servo.
        /// </summary>
        public class Servo
        {
            private PCA9685 pwm;
            private int channel;
            private double position;
            private double minAngle;
            private double maxAngle;
            private double scale;
            private double offset;
            private bool limitsSet;

            /// <summary>
            /// The current position of the servo between the minimumAngle and maximumAngle passed to SetLimits.
            /// </summary>
            public double Position
            {
                get
                {
                    return this.position;
                }
                set
                {
                    if (!this.limitsSet) throw new InvalidOperationException($"You must call {nameof(this.SetLimits)} first.");
                    if (value < this.minAngle || value > this.maxAngle) throw new ArgumentOutOfRangeException(nameof(value));

                    this.position = value;

                    this.pwm.SetChannel(this.channel, 0x0000, (short)(this.scale * value + this.offset));
                }
            }

            internal Servo(PCA9685 pwm, int channel)
            {
                this.pwm = pwm;
                this.channel = channel;
                this.position = 0.0;
                this.limitsSet = false;
            }

            /// <summary>
            /// Sets the limits of the servo.
            /// </summary>
            /// <param name="minimumPulseWidth">The minimum pulse width in milliseconds.</param>
            /// <param name="maximumPulseWidth">The maximum pulse width in milliseconds.</param>
            /// <param name="minimumAngle">The minimum angle of input passed to Position.</param>
            /// <param name="maximumAngle">The maximum angle of input passed to Position.</param>
            public void SetLimits(int minimumPulseWidth, int maximumPulseWidth, double minimumAngle, double maximumAngle)
            {
                if (minimumPulseWidth < 0) throw new ArgumentOutOfRangeException(nameof(minimumPulseWidth));
                if (maximumPulseWidth < 0) throw new ArgumentOutOfRangeException(nameof(maximumPulseWidth));
                if (minimumAngle < 0) throw new ArgumentOutOfRangeException(nameof(minimumAngle));
                if (maximumAngle < 0) throw new ArgumentOutOfRangeException(nameof(maximumAngle));
                if (minimumPulseWidth >= maximumPulseWidth) throw new ArgumentException(nameof(minimumPulseWidth));
                if (minimumAngle >= maximumAngle) throw new ArgumentException(nameof(minimumAngle));

                if (this.pwm.Frequency != 50)
                    this.pwm.Frequency = 50;

                this.minAngle = minimumAngle;
                this.maxAngle = maximumAngle;

                var period = 1000000.0 / this.pwm.Frequency;

                minimumPulseWidth = (int)(minimumPulseWidth / period * 4096.0);
                maximumPulseWidth = (int)(maximumPulseWidth / period * 4096.0);

                this.scale = ((maximumPulseWidth - minimumPulseWidth) / (maximumAngle - minimumAngle));
                this.offset = minimumPulseWidth;

                this.limitsSet = true;
            }
        }

        /// <summary>
        /// Represents an onboard motor.
        /// </summary>
        public class Motor : IDisposable
        {
            private double speed;
            private bool disposed;
            private PCA9685 pwm;
            private IGpio direction1;
            private IGpio direction2;
            private int pwmChannel;

            /// <summary>
            /// The speed of the motor. The sign controls the direction while the magnitude controls the speed (0 is off, 1 is full speed).
            /// </summary>
            public double Speed
            {
                get
                {
                    return this.speed;
                }
                set
                {
                    this.pwm.SetDutyCycle(this.pwmChannel, 0);

                    this.direction1.Value = (value > 0 ? true : false);
                    this.direction2.Value = (value > 0 ? false : true);

                    this.pwm.SetDutyCycle(this.pwmChannel, Math.Abs(value));

                    this.speed = value;
                }
            }

            /// <summary>
            /// Disposes of the object releasing control the pins.
            /// </summary>
            public void Dispose() => this.Dispose(true);

            public Motor(PCA9685 pwm, int pwmChannel, String direction1Pin, String direction2Pin)
            {
                PeripheralManager manager = PeripheralManager.Instance;
                this.direction1 = manager.OpenGpio(direction1Pin);
                this.direction2 = manager.OpenGpio(direction2Pin);
                this.speed = 0.0;
                this.pwm = pwm;
                this.disposed = false;

                this.pwmChannel = pwmChannel;
                // Initialize the pin as a high output
                this.direction1.SetDirection(Gpio.DirectionOutInitiallyHigh);
                // Low voltage is considered active
                this.direction1.SetActiveType(Gpio.ActiveHigh);

                // Initialize the pin as a high output
                this.direction2.SetDirection(Gpio.DirectionOutInitiallyHigh);
                // Low voltage is considered active
                this.direction2.SetActiveType(Gpio.ActiveHigh);
            }

            /// <summary>
            /// Stops the motor.
            /// </summary>
            public void Stop()
            {
                this.pwm.SetDutyCycle(this.pwmChannel, 0.0);
            }

            /// <summary>
            /// Disposes of the object releasing control the pins.
            /// </summary>
            /// <param name="disposing">Whether or not this method is called from Dispose().</param>
            protected virtual void Dispose(bool disposing)
            {
                if (!this.disposed)
                {
                    if (disposing)
                    {
                        this.direction1.Dispose();
                        this.direction2.Dispose();
                    }

                    this.disposed = true;
                }
            }
        }
    }
}