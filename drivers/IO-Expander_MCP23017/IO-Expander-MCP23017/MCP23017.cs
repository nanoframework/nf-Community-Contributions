using System;
using Windows.Devices.Gpio;
using Windows.Devices.I2c;

namespace Driver.MCP23017
{
    /// <summary>
    /// Driver for the MCP23017 I/O Expander
    /// </summary>
    class MCP23017
    {
        internal const byte REG_IODIRA = 0x00;    // I/O direction register A
        internal const byte REG_IPOLA = 0x02;     // Input polarity port register A
        internal const byte REG_GPINTENA = 0x04;  // Interrupt-on-change pins A
        internal const byte REG_DEFVALA = 0x06;   // Default value register A
        internal const byte REG_INTCONA = 0x08;   // Interrupt-on-change control register A
        internal const byte REG_IOCONA = 0x0a;    // I/O expander configuration register A
        internal const byte REG_GPPUA = 0x0c;     // GPIO pull-up resistor register A
        internal const byte REG_INTFA = 0x0e;     // Interrupt flag register A
        internal const byte REG_INTCAPA = 0x10;   // Interrupt captured value for port register A
        internal const byte REG_GPIOA = 0x12;     // General purpose I/O port register A
        internal const byte REG_OLATA = 0x14;     // Output latch register 0 A

        internal const byte REG_IODIRB = 0x01;    // I/O direction register B
        internal const byte REG_IPOLB = 0x03;     // Input polarity port register B
        internal const byte REG_GPINTENB = 0x05;  // Interrupt-on-change pins B
        internal const byte REG_DEFVALB = 0x07;   // Default value register B
        internal const byte REG_INTCONB = 0x09;   // Interrupt-on-change control register B
        internal const byte REG_IOCONB = 0x0b;    // I/O expander configuration register B
        internal const byte REG_GPPUB = 0x0d;     // GPIO pull-up resistor register B
        internal const byte REG_INTFB = 0x0f;     // Interrupt flag register B
        internal const byte REG_INTCAPB = 0x11;   // Interrupt captured value for port register B
        internal const byte REG_GPIOB = 0x13;     // General purpose I/O port register B
        internal const byte REG_OLATB = 0x15;     // Output latch register 0 B

        public enum MCP23017PinDriveMode
        {
            Input,
            InputPullUp,
            Output
        }

        /// <summary>
        /// I2C Device instance
        /// </summary>
        private I2cDevice _i2c;

        /// <summary>
        /// MCP23017 Interrupt Pin A
        /// </summary>
        private GpioPin _irqPinA = null;

        /// <summary>
        /// MCP23017 Interrupt Pin B
        /// </summary>
        private GpioPin _irqPinB = null;
        
        /// <summary>
        /// Array containing pin references used to invoke value changed events
        /// </summary>
        private MCP23017Pin[] _pins = new MCP23017Pin[16];

        /// <summary>
        /// Internal method for wrinting MCP23017 registers
        /// </summary>
        /// <param name="registerAddress">Register address</param>
        /// <param name="value">Value to write</param>
        /// <returns>I2C transfer result</returns>
        internal I2cTransferResult WriteRegister(byte registerAddress, byte value)
        {
            byte[] data = new byte[] { registerAddress, value };
            I2cTransferResult result = _i2c.WritePartial(data);
            return result;
        }

        /// <summary>
        /// Internal method for reading MCP23017 registers
        /// </summary>
        /// <param name="registerAddress">Register address</param>
        /// <returns>Register contents</returns>
        internal byte ReadRegister(byte registerAddress)
        {
            byte[] data = new byte[] { registerAddress };
            byte[] result = new byte[1];
            _i2c.WriteRead(data, result);
            return result[0];
        }

        /// <summary>
        /// Internal method for writing a single bit in a byte
        /// </summary>
        /// <param name="src">Source byte</param>
        /// <param name="bitNumber">Bit number to be written</param>
        /// <param name="value">Value to be written</param>
        /// <returns>Source byte with modified bit</returns>
        internal static byte WriteBit(byte src, int bitNumber, int value)
        {
            byte result = 0;

            if(value == 0)
            {
                result = (byte)(src & ~(1 << bitNumber));
            }
            else if(value == 1)
            {
                result = (byte)(src | (1 << bitNumber));
            }

            return result;
        }

        /// <summary>
        /// Internal method for checking a single bit in a byte
        /// </summary>
        /// <param name="src">Source byte</param>
        /// <param name="pos">Position to be checked</param>
        /// <returns>Bit value</returns>
        internal static bool CheckBit(byte src, int pos)
        {
            return (src & (1 << pos)) != 0;
        }

        /// <summary>
        /// Initializes the MCP23017. Has to be called when creating a new instance.
        /// </summary>
        /// <param name="bus">I2C bus controller identifier</param>
        /// <param name="speed">I2C bus speed</param>
        /// <param name="address">MCP23017 hardware address (0 - 7)</param>
        public void init(string bus, I2cBusSpeed speed, byte address)
        {
            if (address > 7)
            {
                throw new ArgumentOutOfRangeException("Address has to be between 0 and 7");
            }

            // Create I2C device instance
            _i2c = I2cDevice.FromId(bus, new I2cConnectionSettings(address | 0b0100000) { BusSpeed = speed });

            // Set all pins as inputs
            WriteRegister(REG_IODIRA, 0xff);
            WriteRegister(REG_IODIRB, 0xff);
        }

        /// <summary>
        /// Initializes the MCP23017 with one interrupt pin. Has to be called when creating a new instance.
        /// </summary>
        /// <param name="bus">I2C bus controller identifier</param>
        /// <param name="speed">I2C bus speed</param>
        /// <param name="address">MCP23017 hardware address (0 - 7)</param>
        /// <param name="irqPin">MCP23017 interrupt pin</param>
        public void init(string bus, I2cBusSpeed speed, byte address, int irqPin)
        {
            init(bus, speed, address);

            // Set interrupt pins to mirrored and active high
            WriteRegister(REG_IOCONA, 0b01000010);

            // Set up interrupt pin A
            _irqPinA = GpioController.GetDefault().OpenPin(irqPin);
            _irqPinA.SetDriveMode(GpioPinDriveMode.Input);
            _irqPinA.ValueChanged += IrqACallback;
        }

        /// <summary>
        /// Initializes the MCP23017 with two interrupt pin. Has to be called when creating a new instance.
        /// </summary>
        /// <param name="bus">I2C bus controller identifier</param>
        /// <param name="speed">I2C bus speed</param>
        /// <param name="address">MCP23017 hardware address (0 - 7)</param>
        /// <param name="irqPinA">MCP23017 interrupt pin A</param>
        /// <param name="irqPinB">MCP23017 interrupt pin B</param>
        public void init(string bus, I2cBusSpeed speed, byte address, int irqPinA, int irqPinB)
        {
            init(bus, speed, address, irqPinA);

            // Set interrupt pins to separate and active high
            WriteRegister(REG_IOCONA, 0b00000010);

            // Set up interrupt pin B
            _irqPinB = GpioController.GetDefault().OpenPin(irqPinB);
            _irqPinB.SetDriveMode(GpioPinDriveMode.Input);
            _irqPinB.ValueChanged += IrqBCallback;
        }

        /// <summary>
        /// Interrupt event callback for pin A
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IrqACallback(object sender, GpioPinValueChangedEventArgs e)
        {
            // Check port A and port B if only interrupt A is set up
            if(e.Edge == GpioPinEdge.RisingEdge)
            {
                if(_irqPinB == null)
                {
                    // Read interrupt flag register A
                    byte intfA = ReadRegister(REG_INTFA);
                    // Read interrupt capture register A
                    byte intCapA = ReadRegister(REG_INTCAPA);

                    // Read interrupt flag register B
                    byte intfB = ReadRegister(REG_INTFB);
                    // Read interrupt capture register B
                    byte intCapB = ReadRegister(REG_INTCAPB);

                    // Check which pins have the interrupt flag set
                    for (int i = 0; i < 8; i++)
                    {
                        if (CheckBit(intfA, i))
                        {
                            MCP23017Pin pin = _pins[i];
                            if (pin != null)
                            {
                                GpioPinEdge edge;
                                if(CheckBit(intCapA, i))
                                {
                                    edge = GpioPinEdge.RisingEdge;
                                }
                                else
                                {
                                    edge = GpioPinEdge.FallingEdge;
                                }
                                pin.OnValueChanged(edge);
                            }
                        }
                    }

                    for (int i = 8; i < 16; i++)
                    {
                        if (CheckBit(intfB, i - 8))
                        {
                            MCP23017Pin pin = _pins[i];
                            if (pin != null)
                            {
                                GpioPinEdge edge;
                                if (CheckBit(intCapB, i - 8))
                                {
                                    edge = GpioPinEdge.RisingEdge;
                                }
                                else
                                {
                                    edge = GpioPinEdge.FallingEdge;
                                }
                                pin.OnValueChanged(edge);
                            }
                        }
                    }
                }
                else // Only check port A if both interrupt pins are set up
                {
                    // Read interrupt flag register A
                    byte intfA = ReadRegister(REG_INTFA);
                    // Read interrupt capture register A
                    byte intCapA = ReadRegister(REG_INTCAPA);

                    // Check which pins have the interrupt flag set
                    for (int i = 0; i < 8; i++)
                    {
                        if (CheckBit(intfA, i))
                        {
                            MCP23017Pin pin = _pins[i];
                            if (pin != null)
                            {
                                GpioPinEdge edge;
                                if (CheckBit(intCapA, i))
                                {
                                    edge = GpioPinEdge.RisingEdge;
                                }
                                else
                                {
                                    edge = GpioPinEdge.FallingEdge;
                                }
                                pin.OnValueChanged(edge);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Interrupt event callback for pin B
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IrqBCallback(object sender, GpioPinValueChangedEventArgs e)
        {
            // Read interrupt flag register B
            byte intfB = ReadRegister(REG_INTFB);
            // Read interrupt capture register B
            byte intCapB = ReadRegister(REG_INTCAPB);

            // Check which pins have the interrupt flag set
            for (int i = 8; i < 16; i++)
            {
                if (CheckBit(intfB, i - 8))
                {
                    MCP23017Pin pin = _pins[i];
                    if (pin != null)
                    {
                        GpioPinEdge edge;
                        if (CheckBit(intCapB, i - 8))
                        {
                            edge = GpioPinEdge.RisingEdge;
                        }
                        else
                        {
                            edge = GpioPinEdge.FallingEdge;
                        }
                        pin.OnValueChanged(edge);
                    }
                }
            }
        }

        /// <summary>
        /// Opens a pin of the MCP23017
        /// </summary>
        /// <param name="port">Port name: "A" or "B"</param>
        /// <param name="pin">Pin number: 0 - 7</param>
        /// <returns></returns>
        public MCP23017Pin OpenPin(string port, int pin)
        {
            if (pin > 15)
            {
                throw new ArgumentOutOfRangeException("Pin has to be between 0 and 17");
            }
            if(port != "A" && port != "B")
            {
                throw new ArgumentException("Port has to be A or B");
            }

            MCP23017Pin mcpPin = new MCP23017Pin(port, pin, this);

            // Add pin to pin array
            if(port == "A")
            {
                _pins[pin] = mcpPin;
            }
            else
            {
                _pins[pin + 8] = mcpPin;
            }

            return mcpPin;
        }

        /// <summary>
        /// Opens a port of the MCP23017
        /// </summary>
        /// <param name="port">Port name: "A" or "B"</param>
        /// <returns></returns>
        public MCP23017Port OpenPort(string port)
        {
            if (port != "A" && port != "B")
            {
                throw new ArgumentException("Port has to be A or B");
            }
            
            MCP23017Port mcpPort = new MCP23017Port(port, this);

            return mcpPort;
        }
    }

    class MCP23017Pin
    {
        private MCP23017 _mcp23017;
        private string _Port;
        private int _pinNumber;

        public delegate void GpioPinValueChangedEventHandler(
        Object sender,
        GpioPinValueChangedEventArgs e);

        /// <summary>
        /// Internal constructor
        /// </summary>
        /// <param name="port"></param>
        /// <param name="pin"></param>
        /// <param name="parent">Reference of the parent MCP23017 instance</param>
        internal MCP23017Pin(string port, int pin, MCP23017 parent)
        {
            _mcp23017 = parent;
            _Port = port;
            _pinNumber = pin;
        }

        /// <summary>
        /// Sets the pin drive mode
        /// </summary>
        /// <param name="value"></param>
        public void SetDriveMode(MCP23017.MCP23017PinDriveMode value)
        {
            int dir = (value == MCP23017.MCP23017PinDriveMode.Output) ? 0 : 1;

            // Set I/O direction register
            switch (_Port)
            {
                case "A":
                    byte ioDir = _mcp23017.ReadRegister(MCP23017.REG_IODIRA);
                    _mcp23017.WriteRegister(MCP23017.REG_IODIRA, MCP23017.WriteBit(ioDir, _pinNumber, dir));
                    byte gpIntEn = _mcp23017.ReadRegister(MCP23017.REG_GPINTENA);
                    _mcp23017.WriteRegister(MCP23017.REG_GPINTENA, MCP23017.WriteBit(gpIntEn, _pinNumber, 0));
                    break;
                case "B":
                    ioDir = _mcp23017.ReadRegister(MCP23017.REG_IODIRB);
                    _mcp23017.WriteRegister(MCP23017.REG_IODIRB, MCP23017.WriteBit(ioDir, _pinNumber, dir));
                    gpIntEn = _mcp23017.ReadRegister(MCP23017.REG_GPINTENB);
                    _mcp23017.WriteRegister(MCP23017.REG_GPINTENB, MCP23017.WriteBit(gpIntEn, _pinNumber, 0));
                    break;
            }

            // Set pullup and interrupt enable register
            if (value == MCP23017.MCP23017PinDriveMode.Input || value == MCP23017.MCP23017PinDriveMode.InputPullUp)
            {
                int pullup = (value == MCP23017.MCP23017PinDriveMode.InputPullUp) ? 1 : 0;

                switch (_Port)
                {
                    case "A":
                        byte ioPullup = _mcp23017.ReadRegister(MCP23017.REG_GPPUA);
                        _mcp23017.WriteRegister(MCP23017.REG_GPPUA, MCP23017.WriteBit(ioPullup, _pinNumber, pullup));
                        byte gpIntEn = _mcp23017.ReadRegister(MCP23017.REG_GPINTENA);
                        _mcp23017.WriteRegister(MCP23017.REG_GPINTENA, MCP23017.WriteBit(gpIntEn, _pinNumber, 1));
                        break;
                    case "B":
                        ioPullup = _mcp23017.ReadRegister(MCP23017.REG_GPPUB);
                        _mcp23017.WriteRegister(MCP23017.REG_GPPUB, MCP23017.WriteBit(ioPullup, _pinNumber, pullup));
                        gpIntEn = _mcp23017.ReadRegister(MCP23017.REG_GPINTENB);
                        _mcp23017.WriteRegister(MCP23017.REG_GPINTENB, MCP23017.WriteBit(gpIntEn, _pinNumber, 1));
                        break;
                }
            }
        }

        public void Write(GpioPinValue value)
        {
            int val = (value == GpioPinValue.High) ? 1 : 0;

            switch (_Port)
            {
                case "A":
                    byte latch = _mcp23017.ReadRegister(MCP23017.REG_OLATA);
                    _mcp23017.WriteRegister(MCP23017.REG_GPIOA, MCP23017.WriteBit(latch, _pinNumber, val));
                    break;
                case "B":
                    latch = _mcp23017.ReadRegister(MCP23017.REG_OLATB);
                    _mcp23017.WriteRegister(MCP23017.REG_GPIOB, MCP23017.WriteBit(latch, _pinNumber, val));
                    break;
            }
        }

        public void Toggle()
        {
            switch (_Port)
            {
                case "A":
                    byte latch = _mcp23017.ReadRegister(MCP23017.REG_OLATA);
                    _mcp23017.WriteRegister(MCP23017.REG_GPIOA, (byte)(latch ^ (1 << _pinNumber)));
                    break;
                case "B":
                    latch = _mcp23017.ReadRegister(MCP23017.REG_OLATB);
                    _mcp23017.WriteRegister(MCP23017.REG_GPIOB, (byte)(latch ^ (1 << _pinNumber)));
                    break;
            }
        }

        public GpioPinValue Read()
        {
            byte gpio = 0;

            switch (_Port)
            {
                case "A":
                    gpio = _mcp23017.ReadRegister(MCP23017.REG_GPIOA);
                    break;
                case "B":
                    gpio = _mcp23017.ReadRegister(MCP23017.REG_GPIOB);
                    break;
            }

            GpioPinValue result = ((gpio & (byte)(1 << _pinNumber)) == 1) ? GpioPinValue.High : GpioPinValue.Low;

            return result;
        }

        public event GpioPinValueChangedEventHandler ValueChanged;

        internal void OnValueChanged(GpioPinEdge edge)
        {
            ValueChanged?.Invoke(this, new GpioPinValueChangedEventArgs(edge));
        }
    }

    class MCP23017Port
    {
        private MCP23017 _mcp23017;
        private string _Port;

        /// <summary>
        /// Internal constructor
        /// </summary>
        /// <param name="port"></param>
        /// <param name="parent">Reference of the parent MCP23017 instance</param>
        internal MCP23017Port(string port, MCP23017 parent)
        {
            _mcp23017 = parent;
            _Port = port;
        }

        /// <summary>
        /// Sets the port drive mode
        /// </summary>
        /// <param name="value">1 = input, 0 = output</param>
        public void SetDriveMode(byte value)
        {
            switch (_Port)
            {
                case "A":
                    _mcp23017.WriteRegister(MCP23017.REG_IODIRA, value);
                    break;
                case "B":
                    _mcp23017.WriteRegister(MCP23017.REG_IODIRB, value);
                    break;
            }
        }

        public void SetPullups(byte value)
        {
            switch (_Port)
            {
                case "A":
                    _mcp23017.WriteRegister(MCP23017.REG_GPPUA, value);
                    break;
                case "B":
                    _mcp23017.WriteRegister(MCP23017.REG_GPPUB, value);
                    break;
            }
        }

        public void Write(byte value)
        {
            switch (_Port)
            {
                case "A":
                    _mcp23017.WriteRegister(MCP23017.REG_GPIOA, value);
                    break;
                case "B":
                    _mcp23017.WriteRegister(MCP23017.REG_GPIOB, value);
                    break;
            }
        }

        public void Toggle()
        {
            switch (_Port)
            {
                case "A":
                    byte latch = _mcp23017.ReadRegister(MCP23017.REG_OLATA);
                    _mcp23017.WriteRegister(MCP23017.REG_GPIOA, (byte)(latch ^ 0xff));
                    break;
                case "B":
                    latch = _mcp23017.ReadRegister(MCP23017.REG_OLATB);
                    _mcp23017.WriteRegister(MCP23017.REG_GPIOB, (byte)(latch ^ 0xff));
                    break;
            }
        }

        public byte Read()
        {
            byte result = 0;

            switch (_Port)
            {
                case "A":
                    result = _mcp23017.ReadRegister(MCP23017.REG_GPIOA);
                    break;
                case "B":
                    result = _mcp23017.ReadRegister(MCP23017.REG_GPIOB);
                    break;
            }

            return result;
        }
    }
}
