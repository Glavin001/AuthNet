using System;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT;
using System.Threading;

namespace StefanCo.NETMF.Hardware
{
    /// Generic KeyPad driver
    /// See also: http://forums.netduino.com/index.php?/topic/1516-keypad-driver-and-scheme/
    /// Made by: Stefan Thoolen with loads of help from the NetDuino community members Klotz and Vernarim.
    public class MatrixKeyPad
    {
        /// <summary>When a button is pushed, this event will be triggered</summary>
        /// <param name="data1">The pushed key index</param>
        /// <param name="data2">Unused</param>
        /// <param name="time">Time when the event occures</param>
        public event NativeEventHandler OnKeyDown;

        /// <summary>When a button is released, this event will be triggered</summary>
        /// <param name="data1">The released key index</param>
        /// <param name="data2">Unused</param>
        /// <param name="time">Time when the event occures</param>
        public event NativeEventHandler OnKeyUp;

        // Rows and columns will be stored in these arrays
        private InterruptPort[] _ColPins;
        private TristatePort[] _RowPins;
        private uint[] _ColPinIds;

        // We have a few states in which we do different checks
        private enum CheckStates
        {
            WaitingForSignal = 0,
            RowCheck = 1,
            WaitingForRelease = 2,
            WaitingForMultipleRelease = 3,
        }
        private CheckStates _CheckState;

        // Stores the last key press
        private uint _LastKeyPress;

        /// <summary>Generic KeyPad driver</summary>
        /// <param name="RowPins">The pins bound to rows on the keypad matrix</param>
        /// <param name="ColPins">The pins bound to columns on the keypad matrix</param>
        public MatrixKeyPad(Cpu.Pin[] RowPins, Cpu.Pin[] ColPins)
        {
            _Events = new System.Collections.Queue();
            var Thread = new Thread(new ThreadStart(ThreadProc));
            Thread.Start();

            // Defines all RowPins
            this._RowPins = new TristatePort[RowPins.Length];
            for (var RowPinCount = 0; RowPinCount < RowPins.Length; ++RowPinCount)
            {
                this._RowPins[RowPinCount] = new TristatePort(RowPins[RowPinCount], false, false, Port.ResistorMode.PullUp);
                this._RowPins[RowPinCount].Active = true;
            }
            // Defines all ColPins
            this._ColPinIds = new uint[ColPins.Length];
            this._ColPins = new InterruptPort[ColPins.Length];
            for (var ColPinCount = 0; ColPinCount < ColPins.Length; ++ColPinCount)
            {
                this._ColPinIds[ColPinCount] = (uint)ColPins[ColPinCount];
                this._ColPins[ColPinCount] = new InterruptPort(ColPins[ColPinCount], true, Port.ResistorMode.PullUp, Port.InterruptMode.InterruptEdgeBoth);
                this._ColPins[ColPinCount].OnInterrupt += new NativeEventHandler(MatrixKeyPad_OnInterrupt);
            }
            this.ActivateRowPorts(true);
            this.ActivateColInterrupts(true);

            // Defines the check state
            this._CheckState = CheckStates.WaitingForSignal;
        }

        bool _LastDown;
        uint _LastKey;


        void ThreadProc()
        {
            do
            {
                _NewEvent.WaitOne();
                KeyEventArgs NewEvent = null;
                do
                {
                    NewEvent = null;

                    lock (_Events)
                    {
                        if (_Events.Count > 0)
                            NewEvent = (KeyEventArgs)_Events.Dequeue();
                    }
                    if (NewEvent != null)
                    {
                        if (NewEvent.Down)
                        {
                            _LastDown = true;
                            _LastKey = NewEvent.KeyId;
                            if (this.OnKeyDown != null)
                            {
                                this.OnKeyDown((uint)NewEvent.KeyId, 0, NewEvent.DT);
                            }
                        }
                        else
                        {
                            if (this.OnKeyUp != null && _LastDown == true)
                            {
                                this.OnKeyUp(_LastKey, 0, NewEvent.DT);
                            }
                            _LastDown = false;
                        }
                    }
                }
                while (NewEvent != null);
            }
            while (true);
        }

        static AutoResetEvent _NewEvent = new AutoResetEvent(false);
        static System.Collections.Queue _Events;

        public class KeyEventArgs : EventArgs
        {
            public bool Down { get; set; }
            public uint KeyId { get; set; }
            public DateTime DT { get; set; }
        }

        /// <summary>
        /// Event triggered when a button is pressed or released
        /// </summary>
        /// <param name="ColPinId">The Column Pin in which a key is pressed</param>
        /// <param name="State">The state of the button (0 = pressed, 1 = released)</param>
        /// <param name="time">Time of the event</param>
        void MatrixKeyPad_OnInterrupt(uint ColPinId, uint State, DateTime time)
        {
            // Translates the ColPinId to the actual column
            uint ColPin;
            for (ColPin = 0; ColPin < this._ColPinIds.Length; ++ColPin)
            {
                if (this._ColPinIds[ColPin] == ColPinId)
                {
                    break;
                }
            }

            if (this._CheckState == CheckStates.WaitingForSignal && State == 0)
            {
                // To avoid interrupts interfear with each other we disable them temporarily
                this.ActivateColInterrupts(false);
                // Button pressed. We need to find out in which row!
                this._CheckState = CheckStates.RowCheck;
                // We set each pin high one by one
                uint KeyPress = uint.MaxValue;
                for (uint RowPinCount = 0; RowPinCount < this._RowPins.Length; ++RowPinCount)
                {
                    this.ActivateRowPorts(false);
                    this._RowPins[RowPinCount].Active = true;
                    if (!this._ColPins[ColPin].Read())
                    {
                        // Keypress found, we calculate the key number
                        //this._LastKeyPress = unchecked((uint)(RowPinCount * this._ColPins.Length + ColPin));
                        this._LastKeyPress = KeyPress = unchecked((uint)(RowPinCount * this._ColPins.Length + ColPin));
                    }
                }

                if (KeyPress != uint.MaxValue)
                {
                    lock (_Events)
                    {
                        _Events.Enqueue(new KeyEventArgs()
                        {
                            KeyId = KeyPress,
                            Down = true,
                            DT = DateTime.Now,
                        });
                        _NewEvent.Set();
                    }
                    this._CheckState = CheckStates.WaitingForSignal;
                }
                else
                    this._CheckState = CheckStates.WaitingForRelease;

                // Now lets wait for the key to be released
                this._CheckState = CheckStates.WaitingForRelease;
                // Activates all row pins again
                this.ActivateRowPorts(true);
                // Re-activates the interrupts
                this.ActivateColInterrupts(true);

                //// Sends back the keynumber through the event (if it exists)
                //var DT = DateTime.Now;
                ////if (this.OnKeyDown != null && (DT - LastDown).Milliseconds > 200 && (DT - LastUp).Milliseconds > 200)
                //if (this.OnKeyDown != null)
                //{
                //    LastDown = DT;
                //    LastUp = DateTime.MinValue;
                //    this.OnKeyDown(this._LastKeyPress, 0, new DateTime());
                //}
            }
            else if (this._CheckState == CheckStates.WaitingForRelease && State == 1)
            {
                lock (_Events)
                {
                    _Events.Enqueue(new KeyEventArgs()
                    {
                        Down = false,
                        DT = DateTime.Now,
                    });
                    _NewEvent.Set();
                }

                //// Button released, send back the event (if it exists)
                //var DT = DateTime.Now;
                ////if (this.OnKeyUp != null && (DT - LastUp).Milliseconds > 200)
                //if (this.OnKeyUp != null)
                //{
                //    LastUp = DT;
                //    this.OnKeyUp(this._LastKeyPress, 0, DT);
                //}
                this._CheckState = CheckStates.WaitingForSignal;
            }
        }

        DateTime LastDown = DateTime.MinValue;
        DateTime LastUp = DateTime.MaxValue;

        /// <summary>Switches all Row ports activity</summary>
        /// <param name="Active">True when they must be active, false otherwise</param>
        private void ActivateRowPorts(bool Active)
        {
            for (var RowPinCount = 0; RowPinCount < this._RowPins.Length; ++RowPinCount)
            {
                if (this._RowPins[RowPinCount].Active != Active)
                    this._RowPins[RowPinCount].Active = Active;
            }
        }

        /// <summary>Disables or enables all interrupt events</summary>
        /// <param name="Active">When true, all events will be enabled, oftherwise disabled</param>
        private void ActivateColInterrupts(bool Active)
        {
            // Switching the interrupts
            for (uint ColPinCount = 0; ColPinCount < this._ColPins.Length; ++ColPinCount)
            {
                if (Active)
                    this._ColPins[ColPinCount].EnableInterrupt();
                else
                    this._ColPins[ColPinCount].DisableInterrupt();
            }
        }

        /// <summary>Reads the KeyPad and returns the currently pressed scan code</summary>
        /// <returns>The key code or -1 when nothing is pressed</returns>
        public int Read()
        {
            if (this._CheckState == CheckStates.WaitingForSignal)
                return -1;
            else
                return unchecked((int)this._LastKeyPress);
        }

    }
}
