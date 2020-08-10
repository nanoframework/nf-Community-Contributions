using Driver.MCP23017;
using System.Threading;
using Windows.Devices.Gpio;

namespace IO_Expander_MCP23017
{
    public class Program
    {
        static MCP23017Pin led;

        public static void Main()
        {
            MCP23017 mcp23017 = new MCP23017();
            mcp23017.init("I2C1", Windows.Devices.I2c.I2cBusSpeed.FastMode, 1, 23, 22);

            led = mcp23017.OpenPin("A", 7);
            led.SetDriveMode(MCP23017.MCP23017PinDriveMode.Output);

            MCP23017Pin led1 = mcp23017.OpenPin("A", 6);
            led1.SetDriveMode(MCP23017.MCP23017PinDriveMode.Output);
            led1.Write(GpioPinValue.High);

            MCP23017Pin led2 = mcp23017.OpenPin("B", 3);
            led2.SetDriveMode(MCP23017.MCP23017PinDriveMode.Output);
            led2.Write(GpioPinValue.Low);

            MCP23017Pin button = mcp23017.OpenPin("B", 0);
            button.SetDriveMode(MCP23017.MCP23017PinDriveMode.InputPullUp);
            button.ValueChanged += Button_ValueChanged;

            while (true)
            {
                Thread.Sleep(250);
                led1.Toggle();
                led2.Toggle();
            }
        }

        private static void Button_ValueChanged(object sender, GpioPinValueChangedEventArgs e)
        {
            if(e.Edge == GpioPinEdge.FallingEdge)
            {
                led.Write(GpioPinValue.High);
            }
            else
            {
                led.Write(GpioPinValue.Low);
            }
        }
    }
}
