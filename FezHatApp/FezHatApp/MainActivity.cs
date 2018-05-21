using Android.App;
using Android.Widget;
using Android.OS;
using System;
using Android.Util;
using System.Threading.Tasks;
using GHIElectronics.UWP.Shields;
using System.Threading;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Text;
using System.Collections.Generic;

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
            var DeviceIoT = new AzureIoT("HostName=BMCHub.azure-devices.net;DeviceId=AndroidThingsDevice1;SharedAccessKey=jNVYlHP+x/O7NdMT2gY32zrC1RBEl0UCUoxtraXJ9pE=");

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
                var StatusTextBox = this.FindViewById<TextView>(Resource.Id.StatusTextBox);

                Task.Run(async() =>
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
                        Log.Info(TAG, $"iterasi-{count}");
                        var ItemJson = new { Temp = hat.GetTemperature(), Light = hat.GetLightLevel(), X = x, Y=y, Z=z };
                        var res = await DeviceIoT.SendDeviceToCloudMessagesAsync(JsonConvert.SerializeObject(ItemJson));
                        RunOnUiThread(() =>
                        {
                            if(res)
                                StatusTextBox.Text = $"send data to azure iot - {DateTime.Now.ToString()}";
                        });
                            Thread.Sleep(2000);
                    }
                });
            }
            catch (Exception e)
            {
                Log.Error(TAG, "Error on PeripheralIO API", e);
            }
        }
    }

    public class AzureIoT
    {
        public AzureIoT(string ConnectionString)
        {
            Console.WriteLine("Starting Send Telemetry to Azure Iot Hub. Ctrl-C to exit.\n");
            s_connectionString = ConnectionString;
            // Connect to the IoT hub using the MQTT protocol
            s_deviceClient = DeviceClient.CreateFromConnectionString(s_connectionString, TransportType.Amqp);


        }
        private static DeviceClient s_deviceClient;

        // The device connection string to authenticate the device with your IoT hub.
        // Using the Azure CLI:
        // az iot hub device-identity show-connection-string --hub-name {YourIoTHubName} --device-id MyDotnetDevice --output table
        private static string s_connectionString { set; get; }

        // Async method to send simulated telemetry
        public  async Task<bool> SendDeviceToCloudMessagesAsync(string Message, Dictionary<string, string> Properties = null)
        {
            try
            {
                var message = new Microsoft.Azure.Devices.Client.Message(Encoding.ASCII.GetBytes(Message));

                // Add a custom application property to the message.
                // An IoT hub can filter on these properties without access to the message body.
                if (Properties != null)
                {
                    foreach (var item in Properties)
                    {
                        message.Properties.Add(item.Key, item.Value);
                    }
                }

                // Send the tlemetry message
                await s_deviceClient.SendEventAsync(message);
                Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, Message);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

}


