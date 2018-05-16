using Android.App;
using Android.Widget;
using Android.OS;
using System;
using Android.Util;
using System.Threading.Tasks;
using GHIElectronics.UWP.Shields;
using System.Threading;

namespace FezHatApp
{
    [Activity(Label = "FezHatApp", MainLauncher = true)]
    public class MainActivity : Activity
    {
        private const string TAG = "MainActivity";

        private FezHat hat;

        private bool next;
        private int i;
        private void Setup()
        {
            SetContentView(Resource.Layout.Main);
            this.hat = FezHat.Create();

            this.hat.S1.SetLimits(500, 2400, 0, 180);
            this.hat.S2.SetLimits(500, 2400, 0, 180);


        }
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            Log.Info(TAG, "Starting BlinkActivity");


            try
            {

                Setup();
                int count = 0;

                var LightTextBox = this.FindViewById<TextView>(Resource.Id.LightTextBox);
                var TempTextBox = this.FindViewById<TextView>(Resource.Id.TempTextBox);
                var AccelTextBox = this.FindViewById<TextView>(Resource.Id.AccelTextBox);
                var Button18TextBox = this.FindViewById<TextView>(Resource.Id.Button18TextBox);
                var Button22TextBox = this.FindViewById<TextView>(Resource.Id.Button22TextBox);
                var AnalogTextBox = this.FindViewById<TextView>(Resource.Id.AnalogTextBox);
                var LedsTextBox = this.FindViewById<TextView>(Resource.Id.LedsTextBox);
                Task.Run(() =>
                {
                    while (true)
                    {
                        double x, y, z;

                        this.hat.GetAcceleration(out x, out y, out z);
                        RunOnUiThread(() =>
                        {
                            LightTextBox.Text = this.hat.GetLightLevel().ToString("P2");
                            TempTextBox.Text = this.hat.GetTemperature().ToString("N2");
                            AccelTextBox.Text = $"({x:N2}, {y:N2}, {z:N2})";
                            Button18TextBox.Text = this.hat.IsDIO18Pressed().ToString();
                            Button22TextBox.Text = this.hat.IsDIO22Pressed().ToString();
                            AnalogTextBox.Text = this.hat.ReadAnalog(FezHat.AnalogPin.Ain1).ToString("N2");
                        });
                        if ((this.i++ % 5) == 0)
                        {
                            RunOnUiThread(() =>
                            {
                                LedsTextBox.Text = this.next.ToString();
                            });

                            this.hat.DIO24On = this.next;
                            this.hat.D2.Color = this.next ? FezHat.Color.Green : FezHat.Color.Blue;
                            this.hat.D3.Color = this.next ? FezHat.Color.Green : FezHat.Color.Blue;

                            this.hat.WriteDigital(FezHat.DigitalPin.DIO16, this.next);
                            this.hat.WriteDigital(FezHat.DigitalPin.DIO26, this.next);

                            this.hat.SetPwmDutyCycle(FezHat.PwmPin.Pwm5, this.next ? 1.0 : 0.0);
                            this.hat.SetPwmDutyCycle(FezHat.PwmPin.Pwm6, this.next ? 1.0 : 0.0);
                            this.hat.SetPwmDutyCycle(FezHat.PwmPin.Pwm7, this.next ? 1.0 : 0.0);
                            this.hat.SetPwmDutyCycle(FezHat.PwmPin.Pwm11, this.next ? 1.0 : 0.0);
                            this.hat.SetPwmDutyCycle(FezHat.PwmPin.Pwm12, this.next ? 1.0 : 0.0);

                            this.next = !this.next;
                        }

                        if (this.hat.IsDIO18Pressed())
                        {
                            this.hat.S1.Position += 5.0;
                            this.hat.S2.Position += 5.0;

                            if (this.hat.S1.Position >= 180.0)
                            {
                                this.hat.S1.Position = 0.0;
                                this.hat.S2.Position = 0.0;
                            }
                        }

                        if (this.hat.IsDIO22Pressed())
                        {
                            if (this.hat.MotorA.Speed == 0.0)
                            {
                                this.hat.MotorA.Speed = 0.5;
                                this.hat.MotorB.Speed = -0.7;
                            }
                        }
                        else
                        {
                            if (this.hat.MotorA.Speed != 0.0)
                            {
                                this.hat.MotorA.Speed = 0.0;
                                this.hat.MotorB.Speed = 0.0;
                            }
                        }

                        count++;
                        Log.Info(TAG, "iterasi");

                        Thread.Sleep(100);
                    }
                });
            }
            catch (Exception e)
            {
                Log.Error(TAG, "Error on PeripheralIO API", e);
            }
        }
    }
    }

