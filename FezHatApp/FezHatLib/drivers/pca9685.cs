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
using Android.Util;

namespace GHIElectronics.UWP.LowLevelDrivers
{
    public class PCA9685
    {
        private static String TAG = nameof(PCA9685);

        /* Start Driver */
        private II2cDevice device;
        private IGpio outputEnable;
        private byte[] write5;
        private byte[] write2;
        private byte[] write1;
        private byte[] read1;
        private bool disposed;

        public static int I2C_ADDRESS = GetAddress(true, true, true, true, true, true);

        private enum Register
        {
            Mode1 = 0x00,
            Mode2 = 0x01,
            Led0OnLow = 0x06,
            Prescale = 0xFE
        }




        public static int GetAddress(bool a0, bool a1, bool a2, bool a3, bool a4, bool a5)
        {
            return (int)(0x40 | (a0 ? 1 : 0) | (a1 ? 2 : 0) | (a2 ? 4 : 0) | (a3 ? 8 : 0) | (a4 ? 16 : 0) | (a5 ? 32 : 0));
        }

        public PCA9685(String I2CBus) : this(I2CBus, null)
        {

        }

        public PCA9685(String I2CBus, String GPIO_NAME)
        {

            PeripheralManager pioService = PeripheralManager.Instance;
            II2cDevice _device = pioService.OpenI2cDevice(I2CBus, I2C_ADDRESS);

            this.write5 = new byte[5];
            this.write2 = new byte[2];
            this.write1 = new byte[1];
            this.read1 = new byte[1];
            this.disposed = false;

            this.device = _device;
            try
            {
                PeripheralManager manager = PeripheralManager.Instance;
                this.outputEnable = manager.OpenGpio(GPIO_NAME);
            }
            catch (Exception e)
            {
                Log.WriteLine(LogPriority.Info, TAG, "Unable to access GPIO", e);
            }


            if (this.outputEnable != null)
            {
                // Initialize the pin as a high output
                this.outputEnable.SetDirection(Gpio.DirectionOutInitiallyHigh);
                // Low voltage is considered active
                this.outputEnable.SetActiveType(Gpio.ActiveLow);
                // Toggle the value to be LOW
                this.outputEnable.Value = (false);
                //this.outputEnable.SetDriveMode(GpioPinDriveMode.Output);
                //this.outputEnable.Write(GpioPinValue.Low);
            }

            this.WriteRegister(Register.Mode1, (byte)0x20);
            this.WriteRegister(Register.Mode2, (byte)0x06);
        }
        public void Dispose() => this.Dispose(true);
        /**
         * Close the device and the underlying device.
         */
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.device.Dispose();
                    this.outputEnable?.Dispose();
                }

                this.disposed = true;
            }
        }

        public int Frequency
        {
            get
            {
                if (this.disposed) throw new ObjectDisposedException(nameof(PCA9685));

                int _Val = (int)(25000000 / (4096 * (this.ReadRegister(Register.Prescale) + 1)) / 0.9);
                return _Val;
            }
            set
            {
                if (this.disposed) throw new ObjectDisposedException(nameof(PCA9685));
                if (value < 40 || value > 1500) throw new ArgumentOutOfRangeException(nameof(value), "Valid range is 40 to 1500.");

                value *= 10;
                value /= 9;

                var mode = this.ReadRegister(Register.Mode1);

                this.WriteRegister(Register.Mode1, (byte)(mode | 0x10));

                this.WriteRegister(Register.Prescale, (byte)(25000000 / (4096 * value) - 1));

                this.WriteRegister(Register.Mode1, (byte)mode);

                this.WriteRegister(Register.Mode1, (byte)(mode | 0x80));
            }
        }

        public bool OutputEnabled
        {
            get
            {
                return this.outputEnable.Value;
            }
            set
            {
                if (this.disposed) throw new ObjectDisposedException(nameof(PCA9685));
                if (this.outputEnable == null) throw new NotSupportedException();

                this.outputEnable.Value = value;
            }
        }


        public void TurnOn(int channel)
        {
            this.SetChannel(channel, (short)0x1000, (short)0x0000);
        }

        public void TurnOff(int channel)
        {
            this.SetChannel(channel, (short)0x0000, (short)0x1000);
        }

        public void TurnAllOn()
        {
            for (int i = 0; i < 16; i++)
                this.TurnOn(i);
        }

        public void TurnAllOff()
        {
            for (int i = 0; i < 16; i++)
                this.TurnOff(i);
        }

        public void SetDutyCycle(int channel, double dutyCycle)
        {
            if (dutyCycle < 0.0 || dutyCycle > 1.0) throw new Exception("Out of Range dutyCycle");

            if (dutyCycle == 1.0)
            {
                this.TurnOn(channel);
            }
            else if (dutyCycle == 0.0)
            {
                this.TurnOff(channel);
            }
            else
            {
                this.SetChannel(channel, (short)0x0000, (short)(4096 * dutyCycle));
            }
        }

        public void SetChannel(int channel, short on, short off)
        {
            if (this.disposed) throw new Exception("Sudah di dispose PCA9685");
            if (channel < 0 || channel > 15) throw new Exception("Out of range channel");
            if (on > 4096) throw new Exception("Out of Range on");
            if (off > 4096) throw new Exception("Out of Range off");

            this.write5[0] = (byte)((byte)Register.Led0OnLow + (byte)channel * 4);
            this.write5[1] = (byte)on;
            this.write5[2] = (byte)(on >> 8);
            this.write5[3] = (byte)off;
            this.write5[4] = (byte)(off >> 8);

            this.device.Write(this.write5, this.write5.Length);
        }

        private void WriteRegister(Register register, byte value)
        {
            //this.write2[0] = register.getId();
            //this.write2[1] = value;

            this.device.WriteRegByte((int)register, (sbyte)value);
        }

        private byte ReadRegister(Register register)
        {
            //this.write1[0] = register.getId();

            this.device.ReadRegBuffer((int)register, this.read1, this.read1.Length);

            return this.read1[0];
        }
    }
}