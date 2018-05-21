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
        TextView LightTextBox ;
        TextView TempTextBox ;
        TextView AccelTextBox;
        TextView Button18TextBox ;
        TextView Button22TextBox ;
        TextView AnalogTextBox ;
        TextView LedsTextBox ;
        TextView StatusTextBox ;


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
            DeviceIoT.DeviceMethodInvoke += DeviceIoT_DeviceMethodInvoke;
            DeviceIoT.IncomingMessage += DeviceIoT_IncomingMessage;
            try
            {

                Setup();
                int count = 0;
                LightTextBox = this.FindViewById<TextView>(Resource.Id.LightTextBox);
                TempTextBox = this.FindViewById<TextView>(Resource.Id.TempTextBox);
                AccelTextBox = this.FindViewById<TextView>(Resource.Id.AccelTextBox);
                Button18TextBox = this.FindViewById<TextView>(Resource.Id.Button18TextBox);
                Button22TextBox = this.FindViewById<TextView>(Resource.Id.Button22TextBox);
                AnalogTextBox = this.FindViewById<TextView>(Resource.Id.AnalogTextBox);
                LedsTextBox = this.FindViewById<TextView>(Resource.Id.LedsTextBox);
                StatusTextBox = this.FindViewById<TextView>(Resource.Id.StatusTextBox);


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

        private void DeviceIoT_IncomingMessage(string Message)
        {
            RunOnUiThread(() =>
            {
                StatusTextBox.Text = $"Incoming message : {Message}";
            });
        }
        private void DeviceIoT_DeviceMethodInvoke(string Method, string Message, EventArgs e)
        {
            RunOnUiThread(() =>
            {
                switch (Method)
                {
                    case "ChangeColor":
                        {
                            var SelColor = FezHat.Color.Black;
                            var data = JsonConvert.DeserializeObject<ColorData>(Message);
                            switch (data.ColorName.ToLower())
                            {
                                case "red":
                                    SelColor = FezHat.Color.Red;
                                    break;
                                case "green":
                                    SelColor = FezHat.Color.Green;
                                    break;
                                case "blue":
                                    SelColor = FezHat.Color.Blue;
                                    break;
                                case "yellow":
                                    SelColor = FezHat.Color.Yellow;
                                    break;
                                case "white":
                                    SelColor = FezHat.Color.White;
                                    break;

                                default:

                                    break;
                            }
                            hat.D2.Color = SelColor;
                            hat.D3.Color = SelColor;
                        }
                        break;
                    case "RotateServo":
                        {
                            var data = JsonConvert.DeserializeObject<ServoData>(Message);
                            hat.S1.Position = data.Position;
                            hat.S2.Position = data.Position;

                        }
                        break;
                    default:
                        break;
                }   
                    StatusTextBox.Text = $"method {Method} called -> {Message}";
            });

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
            // Create a handler for the direct method call
            s_deviceClient.SetMethodHandlerAsync("ChangeColor", InvokeDeviceMethod, null).Wait();
            s_deviceClient.SetMethodHandlerAsync("RotateServo", InvokeDeviceMethod, null).Wait();
            Task ReceivingThread = new Task(new Action(ReceiveC2dAsync));
            ReceivingThread.Start();
        }

        public event DeviceMethodHandler DeviceMethodInvoke;
        public EventArgs e = null;
        public delegate void DeviceMethodHandler(string Method, string Message, EventArgs e);

        public event IncomingMessageHandler IncomingMessage;
        public delegate void IncomingMessageHandler(string Message);

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

        // Handle the direct method call
        private Task<MethodResponse> InvokeDeviceMethod(MethodRequest methodRequest, object userContext)
        {
            try
            {
                var data = Encoding.UTF8.GetString(methodRequest.Data);

                // Check the payload is a single integer value

                // Acknowlege the direct method call with a 200 success message
                string result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"}";
                if (DeviceMethodInvoke != null)
                {
                    DeviceMethodInvoke(methodRequest.Name, data, null);
                }
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
            }
            catch(Exception ex)
            {
                string result = "{\"result\":\""+ex.Message+"\"}";
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 400));

            }
        }
        private async void ReceiveC2dAsync()
        {
            while (true)
            {
                var receivedMessage = await s_deviceClient.ReceiveAsync();
                if (receivedMessage == null) continue;

                Console.ForegroundColor = ConsoleColor.Yellow;
                var msg = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                Console.WriteLine("Received message: {0}", msg);
                Console.ResetColor();

                await s_deviceClient.CompleteAsync(receivedMessage);
                if (IncomingMessage != null)
                {
                    IncomingMessage(msg);
                }
                Thread.Sleep(500);
            }
        }
    }
    public class ColorData
    {
        public string ColorName { get; set; }
    }
    public class ServoData
    {
        public int Position { get; set; }
    }
}


