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

namespace GHIElectronics.UWP.LowLevelDrivers
{
    public class ADS7830
    {
        private II2cDevice device;
        private bool disposed;
        private byte[] read;
        private byte[] write;
        public static int I2C_ADDRESS = GetAddress(false, false);
        public static byte GetAddress(bool a0, bool a1) => (byte)(0x48 | (a0 ? 1 : 0) | (a1 ? 2 : 0));

        public void Dispose() => this.Dispose(true);

        public ADS7830(String I2CBus)
        {
            PeripheralManager pioService = PeripheralManager.Instance;
            II2cDevice _device = pioService.OpenI2cDevice(I2CBus, I2C_ADDRESS);
            this.device = _device;
            this.disposed = false;
            this.read = new byte[1];
            this.write = new byte[1];
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.device.Dispose();
                }

                this.disposed = true;
            }
        }

        public int ReadRaw(int channel)
        {
            if (this.disposed) throw new ObjectDisposedException(nameof(ADS7830));
            if (channel > 8 || channel < 0) throw new ArgumentOutOfRangeException(nameof(channel));

            int addr = (0x84 | ((channel % 2 == 0 ? channel / 2 : (channel - 1) / 2 + 4) << 4));

            this.device.ReadRegBuffer(addr, this.read, this.read.Length);

            return this.read[0];
        }

        public double Read(int channel) => this.ReadRaw(channel) / 255.0;
    }
}