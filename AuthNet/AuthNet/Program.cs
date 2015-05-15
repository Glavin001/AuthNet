using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using System.Text;
using System.IO.Ports;
using StefanCo.NETMF.Hardware;

namespace AuthNet
{
    public class Program
    {
        // IMPORTANT: CHANGE _HOST
        static string _Host = "192.168.1.103";
        static int _Port = 3000;

        static int roomId = 123;

        static IPAddress _Address = IPAddress.Parse("10.95.5.215");

        static SerialPort Serial1 = new SerialPort(SerialPorts.COM1, 9600, Parity.None, 8, StopBits.One);
        static OutputPort _LED = new OutputPort(Pins.ONBOARD_LED, false);
        static PWM Buzzer = new PWM(Cpu.PWMChannel.PWM_2, 0, 0, PWM.ScaleFactor.Microseconds, false);
        static char[] KeyPadIndexToChar = new char[12] { '1', '2', '3', '4', '5', '6', '7', '8', '9', '*', '0', '#' };


        public static void Main()
        {
            // write your code here
            var Interface = Microsoft.SPOT.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()[0];
            Interface.EnableDynamicDns();
            Interface.EnableDhcp();
            Debug.Print("IPAddress: " + Interface.IPAddress);
            foreach (var d in Interface.DnsAddresses)
                Debug.Print("DNS: " + d);

            Serial1.Open();
            Thread.Sleep(2000); // Need this pause

            ClearDisplay(Serial1);

            Debug.Print("Started");
            DoFlash(200, 4);

            Cpu.Pin[] RowPins = { Pins.GPIO_PIN_D3, Pins.GPIO_PIN_D8, Pins.GPIO_PIN_D7, Pins.GPIO_PIN_D5 };
            Cpu.Pin[] ColPins = { Pins.GPIO_PIN_D4, Pins.GPIO_PIN_D2, Pins.GPIO_PIN_D6 };
            MatrixKeyPad kb = new MatrixKeyPad(RowPins, ColPins);
            kb.OnKeyDown += kb_OnKeyDown;

            Debug.Print("Listening for input");

            StartBeeping();

            while (true)
            {
                Thread.Sleep(500);
                //Debug.Print("Get Value");
                ////LED.Write(false);
                //var Result = GetValue();
                ////LED.Write(true);
                //Debug.Print(Result);

                //Debug.Print("Process Result");
                //ProcessResult(Result, SetValue);
                ////LED.Write(false);
            }

        }


        static bool _NextClears = true;
        static int _CharCount = 0;
        static string _Code = null;
        static string _Entered = "";
        static string _SubmitChar = "#";

        static void kb_OnKeyDown(uint KeyCode, uint data2, DateTime time)
        {
            //if (_Beeping == false)
            //    return;
            Debug.Print("OnKeyDown: " + KeyCode);
            var KeyText = KeyPadIndexToChar[(int)KeyCode].ToString();
            Debug.Print("KeyText: " + KeyText);
            if (_NextClears == true)
            {
                _NextClears = false;
                _Entered = "";
                ClearDisplay(Serial1);
            }

            // Check for Submit char
            if (KeyText == _SubmitChar)
            {
                if (_CharCount == 0)
                {
                    return;
                }
                string body = "code=" + _Entered + "&room=" + roomId;
                string endpoint = "code";
                SendToNode("POST", endpoint, body);
                // Reset entered
                _Entered = "";
                _CharCount = 0;

                return;
            }

            AddTextToDisplay(Serial1, KeyText);
            _Entered += KeyText;
            Debug.Print("Entered: " + _Entered);

            _CharCount++;


            //if (_CharCount == 2)
            //{
            //    _NextClears = true;
            //    _CharCount = 0;

            //    // Check answer
            //    if(_Code != null && _Entered == _Code)
            //    {
            //        StopBeeping();
            //        WriteTextToDisplay(Serial1, "Code Found!");
            //        SendResult(_GadgetID, "Correct");
            //    }
            //    else
            //    {
            //        AddTextToDisplay(Serial1, " Wrong!");
            //        SendResult(_GadgetID, "Wrong");
            //    }
            //}

        }

        public class MessageSend
        {
            private string _ID;
            private string _Result;

            public MessageSend(string ID, string Result)
            {
                _ID = ID;
                _Result = Result;
            }

            public void SendThreadProc()
            {
                string text = "\"" + _ID.Substring(0, 2) + " " + _Result + "\""; // +Result;
                //SendSMS(text);
            }
        }

        static void SendResult(string ID, string Result)
        {
            var MS = new MessageSend(ID, Result);

            var Thread = new Thread(new ThreadStart(MS.SendThreadProc));
            Thread.Start();
        }


        private static void SetValue(string Name, string Value)
        {

            switch (Name.ToLower())
            {
                case "led":
                    if (Value != null)
                    {
                        if (Value.ToLower().Equals("on"))
                        {
                            DoFlash(100, 3);
                            _LED.Write(true);
                        }
                        else
                            _LED.Write(false);
                    }
                    break;

                case "countdown":
                    if (Value != null)
                    {
                        _Entered = "";
                        ClearDisplay(Serial1);
                        _NextClears = false;
                        _CharCount = 0;

                        if (Value.ToLower().Equals("on"))
                        {
                            var RNG = new Random(DateTime.Now.Millisecond);
                            _Code = (RNG.Next(10) % 10).ToString() + (RNG.Next(10) % 10).ToString();
                            Debug.Print("Code " + _Code);
                            //_Code = "55";
                            StartBeeping();
                        }
                        else
                        {
                            _Code = null;
                            StopBeeping();
                        }
                    }
                    break;

            }
        }

        private static bool _Beeping = false;
        private static bool _StopBeeping = false;
        //private static int _TimeLeft = 60;

        private static void StartBeeping()
        {
            if (_Beeping)
                return;

            _Beeping = true;
            //_TimeLeft = 60;
            _StopBeeping = false;
            _BeepingStopped.Reset();
            var Thread = new Thread(new ThreadStart(BeepThreadProc));
            Thread.Start();
        }
        private static void StopBeeping()
        {
            if (_Beeping == false)
                return;

            _StopBeeping = true;
            _BeepingStopped.WaitOne();
            _Beeping = false;
        }

        static void BeepThreadProc()
        {
            while (_StopBeeping == false)
            {
                PlayNote("c", 6, 200);
                Thread.Sleep(800);
            }
            _BeepingStopped.Set();
        }

        static ManualResetEvent _BeepingStopped = new ManualResetEvent(false);


        private static void DoFlash(int Pause, int count)
        {
            for (int i = 0; i < count; i++)
            {
                _LED.Write(true);
                Thread.Sleep(Pause);
                _LED.Write(false);
                if (i != count - 1)
                    Thread.Sleep(Pause);
            }
        }

        private static void PlayNote(string Note, int Octave, int DurationMS)
        {
            float freq;
            uint period;
            freq = CalculateFrequency(Octave, Note);
            period = (uint)(1000000 / freq);
            Buzzer.Start();
            Buzzer.Duration = period / 2;
            Buzzer.Period = period;
            Thread.Sleep(DurationMS);
            Buzzer.Stop();
        }

        public static float CalculateFrequency(int octave, string note)
        {
            string noteLC = note.ToLower();
            string[] notes = "c,c#,d,d#,e,f,f#,g,g#,a,a#,b".Split(',');


            // loop through each note until we find the index of the one we want         
            for (int n = 0; n < notes.Length; n++)
            {
                if (notes[n] == noteLC // frequency found for major and sharp notes 
                    || (note.Length > 1 && noteLC[1] == 'b' && notes[n + 1][0] == noteLC[0])) // or flat of next note 
                {
                    // Multiply initial note by 2 to the power (n / 12) to get correct frequency,  
                    //  (where n is the number of notes above the first note).  
                    //  Then mutiply that value by 2 to go up each octave 
                    return (16.35f * (float)System.Math.Pow(2, ((double)n / 12.0)))
                        * (float)System.Math.Pow(2, octave);
                }
            }
            throw new ArgumentException("No frequency found for note : " + note, note);
        }




        private delegate void AssignValue(string name, string value);

        //static void ProcessResult(string Result)
        static void ProcessResult(string Result, AssignValue Assign)
        {
            try
            {
                var Lines = Result.Split(new char[] { '\n' });
                string Content = null;
                bool FoundBlank = false;
                foreach (var l in Lines)
                {
                    var TrimmedLine = l.Trim(new char[] { '\r', '\n' });
                    if (FoundBlank == true)
                    {
                        Content = TrimmedLine;
                        break;
                    }
                    if (TrimmedLine == "")
                        FoundBlank = true;
                }

                string Name = "", Value = null;

                if (Content != null && Content != "")
                {
                    var EqIdx = Content.IndexOf('-');
                    if (EqIdx == -1)
                        Name = Content;
                    else
                    {
                        Name = Content.Substring(0, EqIdx);
                        Value = Content.Substring(EqIdx + 1);
                    }
                }

                Assign(Name, Value);

            }
            catch (Exception)
            {
            }
        }



        static void ClearDisplay(SerialPort Port)
        {
            ResetDisplayCursor(Port);
            AddTextToDisplay(Port, "                ");
            AddTextToDisplay(Port, "                ");
            ResetDisplayCursor(Port);
        }

        static void ResetDisplayCursor(SerialPort Port)
        {
            Port.WriteByte(254);
            Port.WriteByte(128);
        }

        static void WriteTextToDisplay(SerialPort Port, string Text)
        {
            ClearDisplay(Port);
            AddTextToDisplay(Port, Text);
        }

        static void AddTextToDisplay(SerialPort Port, string Text)
        {
            var Bytes = System.Text.Encoding.UTF8.GetBytes(Text);
            var Length = System.Math.Min(Bytes.Length, 32);
            Port.Write(Bytes, 0, Length);
        }

        static void SendToNode(String method, String endpoint, String body)
        {

            var Entry = Dns.GetHostEntry(_Host);
            var _Address = new IPAddress(Entry.AddressList[0].GetAddressBytes());

            var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPHostEntry ipHostInfo = Dns.GetHostEntry(_Host);
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint endPoint = new IPEndPoint(ipAddress, _Port);
            //var EndPoint = new IPEndPoint(_Address, 80);
            s.Connect(endPoint);

            var url = "http://" + _Host + "/" + endpoint;

            var databuilder = new StringBuilder();
            databuilder.AppendLine(method + " " + url + " HTTP/1.1");
            databuilder.AppendLine("Accept: application/json");
            databuilder.AppendLine("Content-Type: application/x-www-form-urlencoded; charset=utf-8");
            //databuilder.AppendLine("Content-Type: text/plain; charset=utf-8");
            databuilder.AppendLine("Host: " + _Host);
            databuilder.AppendLine("Content-Length: " + body.Length);
            databuilder.AppendLine("Connection: Keep-Alive");
            databuilder.AppendLine("");
            databuilder.AppendLine(body);

            var databytes = Encoding.UTF8.GetBytes(databuilder.ToString());

            Debug.Print("Sending...");
            var numbytessend = s.Send(databytes);
            Debug.Print("Sent");

            var buff = new byte[10000];
            var numbytesread = s.Receive(buff, 0, 10000, SocketFlags.None);
            if (numbytesread > 0)
            {
                var chars = Encoding.UTF8.GetChars(buff, 0, numbytesread);
                Debug.Print("Result: " + new String(chars));
            }

        }

    }
}
