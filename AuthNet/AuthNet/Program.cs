using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using System.IO.Ports;
using StefanCo.NETMF.Hardware;

namespace AuthNet
{
    public class Program
    {
        static char[] KeyPadIndexToChar = new char[12] { '1', '2', '3', '4', '5', '6', '7', '8', '9', '*', '0', '#' };
        static SerialPort Serial1 = new SerialPort(SerialPorts.COM1, 9600, Parity.None, 8, StopBits.One);
        static PWM Buzzer = new PWM(Cpu.PWMChannel.PWM_2, 0, 0, PWM.ScaleFactor.Microseconds, false);

        public static void Main()
        {
            // write your code here

            // Setup and clear display
            Serial1.Open();
            Thread.Sleep(600); // Need this pause
            ClearDisplay(Serial1);

            // Setup keyboard buffer
            Cpu.Pin[] RowPins = { Pins.GPIO_PIN_D3, Pins.GPIO_PIN_D8, Pins.GPIO_PIN_D7, Pins.GPIO_PIN_D5 };
            Cpu.Pin[] ColPins = { Pins.GPIO_PIN_D4, Pins.GPIO_PIN_D2, Pins.GPIO_PIN_D6 };
            MatrixKeyPad kb = new MatrixKeyPad(RowPins, ColPins);
            kb.OnKeyDown += kb_OnKeyDown;

            WriteTextToDisplay(Serial1, "Play...         ");
        }

        static string[] CScale = { "c", "d", "e", "f", "g", "a", "b" };

        static void kb_OnKeyDown(uint KeyCode, uint data2, DateTime time)
        {
            if (KeyCode >= 0 && KeyCode <= 6)
                PlayNote(CScale[KeyCode], 4, 150);
            else if (KeyCode == 7)
                PlayNote(CScale[0], 5, 150);
            AddTextToDisplay(Serial1, KeyPadIndexToChar[(int)KeyCode].ToString());
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

        private static void PlayNote(string Note, int Octave, int DurationMS)
        {
            float freq;
            uint period;
            freq = CalculateFrequency(Octave, Note);
            period = (uint)(1000000 / freq);

            Buzzer.Duration = period / 2;
            Buzzer.Period = period;
            Buzzer.Start();
            Thread.Sleep(DurationMS);
            Buzzer.Stop();
        }
    }
}
