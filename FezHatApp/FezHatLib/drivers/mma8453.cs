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
        public class MMA8453
        {
            private II2cDevice device;
            private byte[] write;
            private byte[] read;
            private bool disposed;
            public static int I2C_ADDRESS = GetAddress(false);
            public static byte GetAddress(bool a0) => (byte)(0x1C | (a0 ? 1 : 0));

            public void Dispose() => this.Dispose(true);

            public MMA8453(String I2CBus)
            {
                PeripheralManager pioService = PeripheralManager.Instance;
                II2cDevice _device = pioService.OpenI2cDevice(I2CBus, I2C_ADDRESS);

               
                this.device = _device;
                this.write = new byte[1];
                this.write[0] = 0x01;
                this.read = new byte[6];
                this.disposed = false;
                byte[] initdata = new byte[] { 0x2A, 0x01 };
                this.device.Write(initdata, initdata.Length);
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

            public void GetAcceleration(out double x, out double y, out double z)
            {
                if (this.disposed) throw new ObjectDisposedException(nameof(MMA8453));
                this.device.ReadRegBuffer(this.write[0], this.read, this.read.Length);

                x = this.Normalize(0);
                y = this.Normalize(2);
                z = this.Normalize(4);
            }

            private double Normalize(int offset)
            {
                double value = (this.read[offset] << 2) | (this.read[offset + 1] >> 6);

                if (value > 511.0)
                    value = value - 1024.0;

                value /= 256.0;

                return value;
            }
        }
    }
