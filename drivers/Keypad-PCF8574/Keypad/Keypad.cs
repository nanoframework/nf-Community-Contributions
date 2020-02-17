//
// Copyright (c) 2020 The nanoFramework project contributors
// See LICENSE file in the project root for full license information.
//

using System;
using System.Threading;
using Windows.Devices.Gpio;
using Windows.Devices.I2c;

namespace nanoFramework.Hardware.Drivers
{
    /// <summary>
    /// Keypad driver.
    /// </summary>
    /// <remarks>
    /// This driver is based on the PCF8574 IO port expander.
    /// </remarks>
    public class Keypad
    {
        private readonly int _address;
        private readonly I2cDevice _expanderController;
        private readonly GpioPin _interruptPin;
        private readonly byte _columnCount;
        private readonly byte _rowCount;
        private bool _interruptEnabled;
        private Thread _keypadThread;
        private KeyPressedEventArgs _lastKey = null;
        private DateTime _lastKeyPressTime;
        private long _keyDelay;

        private readonly static AutoResetEvent s_KeyActivity = new AutoResetEvent(false);

        /// <summary>
        /// I2C address of the STMPE811 device.
        /// </summary>
        public int Address => _address;

        /// <summary>
        /// Delay time between keypad press event raising.
        /// </summary>
        /// <remarks>Default is 500 ms</remarks>
        public TimeSpan KeyDelayMiliseconds
        {
            get => TimeSpan.FromMilliseconds(_keyDelay);
            set => _keyDelay = value.Ticks;
        }

        /// <summary>
        /// Optional key map to provide easy key mapping.
        /// </summary>
        public char[][] KeyMap { get; set; }

        /// <summary>
        /// Creates a driver for the PCF8574  Remote 8-Bit I/O Expander for I2C Bus.
        /// </summary>
        /// <param name="address">The I2C address of the device.</param>
        /// <param name="i2cBus">The I2C bus where the device is connected to.</param>
        /// <param name="columnCount">How many columns the driver is to scan.</param>
        /// <param name="rowCount">THow many rows the driver is to scan.</param>

        public Keypad(
            int address,
            string i2cBus,
            GpioPin interruptPin,
            byte columnCount,
            byte rowCount)
        {
            _lastKeyPressTime = DateTime.UtcNow;
            // default to 500ms
            KeyDelayMiliseconds = TimeSpan.FromMilliseconds(500);

            // store I2C address
            _address = address;

            // instantiate I2C controller
            _expanderController = I2cDevice.FromId(i2cBus, new I2cConnectionSettings(address));

            // store INT pin
            _interruptPin = interruptPin;

            _columnCount = columnCount;
            _rowCount = rowCount;
        }

        /// <summary>
        /// Enable processing of key press.
        /// </summary>
        public void EnableKeyPress()
        {
            if (!_interruptEnabled)
            {
                _interruptPin.SetDriveMode(GpioPinDriveMode.InputPullUp);

                _interruptPin.ValueChanged += KeyPressed_ValueChanged;

                _keypadThread = new Thread(new ThreadStart(KeyPressedThread));

                _keypadThread.Start();

                _interruptEnabled = true;

                // write '1s' to the lower 4 bits (P0-P4) that are feeding the keypad to make them outputs
                if (_expanderController.WritePartial(new byte[] { 0b00001111 }).Status == I2cTransferStatus.FullTransfer)
                {
                    // all good
                }

                // dummy read to clear interrupt
                s_KeyActivity.Set();
            }
        }

        /// <summary>
        /// Disable processing of key press.
        /// </summary>
        public void DisableKeyPress()
        {
            if (_interruptEnabled)
            {
                _interruptPin.ValueChanged -= KeyPressed_ValueChanged;

                _keypadThread.Suspend();

                _interruptEnabled = false;
            }
        }

        #region key pressed event 

        /// <summary>
        /// Represents the delegate used for the <see cref="KeyPressed"/> event.
        /// <para name="e">Details of the pressed key</para>
        /// </summary>
        public delegate void KeyPressedEventHandler(KeyPressedEventArgs e);

        /// <summary>
        /// Event raised when a key is pressed.
        /// </summary>
        public event KeyPressedEventHandler KeyPressed;

        private KeyPressedEventHandler _onKeyPressed;

        /// <summary>
        /// Raises the <see cref="KeyPressed"/> event.
        /// <para name="e">Details of the pressed key</para>
        /// </summary>
        protected virtual void OnKeyPressed(KeyPressedEventArgs e)
        {
            if (_onKeyPressed == null) _onKeyPressed = new KeyPressedEventHandler(KeyPressed);

            //Update last keystroke time stamp
            _lastKeyPressTime = DateTime.UtcNow;

            // invoke handlers
            KeyPressed?.Invoke(e);
        }

        #endregion

        #region key released event 

        /// <summary>
        /// Represents the delegate used for the <see cref="KeyReleased"/> event.
        /// </summary>
        public delegate void KeyReleasedEventHandler();

        /// <summary>
        /// Event raised when a key is released.
        /// </summary>
        public event KeyReleasedEventHandler KeyReleased;

        private KeyReleasedEventHandler _onKeyReleased;

        /// <summary>
        /// Raises the <see cref="KeyReleased"/> event.
        /// </summary>
        protected virtual void OnKeyReleased()
        {
            if (_onKeyReleased == null)
                _onKeyReleased = new KeyReleasedEventHandler(KeyReleased);

            //Update last keystroke time stamp
            _lastKeyPressTime = DateTime.UtcNow;

            // invoke handlers
            KeyReleased?.Invoke();
        }

        #endregion

        private void KeyPressed_ValueChanged(object sender, GpioPinValueChangedEventArgs e)
        {
            //check last key press time and see if allowed to raise only if is past the KeyDelay time 
            if (DateTime.UtcNow.Ticks - _lastKeyPressTime.Ticks > _keyDelay)
            {
                s_KeyActivity.Set();
            }
        }

        private void KeyPressedThread()
        {
            // I2C buffers
            var writeBuffer = new byte[1];
            var readBuffer = new byte[1];

            // working vars
            sbyte column = -1;
            sbyte row = -1;
            int index = 1;

            while (true)
            {
                // wait for event
                s_KeyActivity.WaitOne();

                // load the write buffer with the initial value to scan the keypad
                writeBuffer[0] = 0b00000001;

                // reset working vars
                column = -1;
                row = -1;
                index = 1;

                // scan columns
                for (; index <= _columnCount; index++)
                {
                    if (_expanderController.WriteReadPartial(writeBuffer, readBuffer).Status == I2cTransferStatus.FullTransfer)
                    {
                        if (readBuffer[0] == 0)
                        {
                            // we have a hit, store the column index
                            column = (sbyte)index;
                        }
                    }

                    writeBuffer[0] = (byte)(writeBuffer[0] << 1);
                }

                // scan rows
                for (; index <= (_columnCount + _rowCount); index++)
                {
                    if (_expanderController.WriteReadPartial(writeBuffer, readBuffer).Status == I2cTransferStatus.FullTransfer)
                    {
                        if (readBuffer[0] == 0)
                        {
                            // we have a hit, store the row index
                            // subtract the number of columns, because they're the 1st ones
                            row = (sbyte)(index - _columnCount);
                        }
                    }

                    writeBuffer[0] = (byte)(writeBuffer[0] << 1);
                }

                //Console.WriteLine($"[{column.ToString()} , {row.ToString()}]");

                // is this the same key?
                if (_lastKey != null)
                {
                    // check for key released
                    if (column == -1 && row == -1)
                    {
                        // clear last key
                        _lastKey = null;

                        // fire event
                        OnKeyReleased();
                    }
                    else if (_lastKey.Column != column ||
                             _lastKey.Row != row)
                    {
                        // different!!
                        // store it and set key if mapping provided
                        if (KeyMap != null)
                            _lastKey = new KeyPressedEventArgs(column, row, KeyMap[row - 1][column - 1]);
                        else
                            _lastKey = new KeyPressedEventArgs(column, row, ' ');

                        // fire event
                        OnKeyPressed(_lastKey);
                    }
                }
                else
                {
                    if (column != -1 &&
                        row != -1)
                    {
                        // store it and set key if mapping provided
                        if (KeyMap != null)
                            _lastKey = new KeyPressedEventArgs(column, row, KeyMap[row - 1][column - 1]);
                        else
                            _lastKey = new KeyPressedEventArgs(column, row, ' ');

                        // fire event
                        OnKeyPressed(_lastKey);
                    }
                }

                // set all LSB back to output for the next interrupt
                if (_expanderController.WritePartial(new byte[] { 0b00001111 }).Status != I2cTransferStatus.FullTransfer)
                {
                    // ooopps
                }
            }
        }
    }
}
