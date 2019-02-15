### ST7735 Driver with Nucleo STM32F411RET6 Pins Class 

|No| ST7735 Pin|Nucleo Pin|
|------|-------|------|
|1|LED   |D7     |
|2|SCK   |SPI2SCK |
|3|SDA   |SPI2MOSI|
|4|A0 (DC)|D9     |
|5|RESET  |D8     |
|6|GND    |GND     |
|7|VCC    |5V     |

### Wiring
![alt text](https://github.com/valoni/netmf-interpreter4x/blob/master/nanoFrameworks%20Drivers%20and%20Examples/ST7735/Nucleo_ST7735_160x128.png "Nucleo F411 wiring with ST7735")

### How to use driver / Program.cs
```csharp
using System;
using System.Threading;
using STM32F4.Pins;
using nanoFramework.Driver;


namespace NFApp5
{
    public class Program
    {
         public static void Main()
         {
            

            ST7735 display = new ST7735(
               NUCLEOF411.Gpio.D8,                 //Reset
               NUCLEOF411.Gpio.D7,                 //BackLight 
               NUCLEOF411.Gpio.D9,                 //A0 (DC) Control Pin / Data Command
               NUCLEOF411.SpiDevice.Sp2.Name,      //SPI SCK/MOSI 
               NUCLEOF411.Gpio.D10                 //chipSelect
              );                          

            display.TurnOn();

            short i = 0;

            display.DrawCircle(20, 20, 20, Color.Red);
            display.DrawRectangle(40, 40, 40, 40, Color.Cyan);
            display.DrawFilledRectangle(80, 80, 40, 40, Color.Blue);
            display.DrawText(10, 30, "Hello nanoFramework", Color.Green);
            display.DrawText(30, 60, "from ST7735 SPI", Color.Green);

            while (true)
            {
                i++;
                display.DrawText(10, 10, i.ToString(),Color.Green);

                Thread.Sleep(500);
            }
        }
        
    }
}
```

### Nucleo F411 Class / STM32F411.cs
```csharp
//
// Copyright (c) 2019 The nanoFramework project contributors
// See LICENSE file in the project root for full license information.
//
using System;

namespace STM32F4.Pins
{
    public static class Pinouts
    { 
    static int PinNumber(char port, byte pin)
    {
            if (port < 'A' || port > 'J')
                throw new ArgumentException();

            return ((port - 'A') * 16) + pin;
        }
    }

    public static class NUCLEOF411
    {
        /// <summary>
        /// Enumeration of ADC channels available on ST_NUCLEO64_F411RE_NF
        /// </summary>
        public static class AdcChannels
        {
            /// <summary>
            /// Channel 0, exposed as A0, connected to pin 1 on CN8 = PA0 (ADC1 - IN0)
            /// </summary>
            public const int Channel_0 = 0;

            /// <summary>
            /// Channel 1, exposed as A1, connected to pin 2 on CN8 = PA1 (ADC1 - IN1)
            /// </summary>
            public const int Channel_1 = 1;

            /// <summary>
            /// Channel 2, exposed as A2, connected to pin 3 on CN8 = PA4 (ADC1 - IN4)
            /// </summary>
            public const int Channel_2 = 2;

            /// <summary>
            /// Channel 3, exposed as A3, connected to pin 4 on CN8 = PB0 (ADC1 - IN8)
            /// </summary>
            public const int Channel_3 = 3;

            /// <summary>
            /// Channel 4, exposed as A4, connected to pin 5 on CN8 = PC1 (ADC1 - IN11)
            /// </summary>
            public const int Channel_4 = 4;

            /// <summary>
            /// Channel 5, exposed on A5, connected to pin 6 on CN8 = PC0 (ADC1 - IN10)
            /// </summary>
            public const int Channel_5 = 5;

            /// <summary>
            /// Channel 6, internal temperature sensor, connected to ADC1
            /// </summary>
            public const int Channel_TemperatureSensor = 6;

            /// <summary>
            /// Channel 7, internal voltage reference, connected to ADC1
            /// </summary>
            public const int Channel_VrefIn = 7;

            /// <summary>
            /// Channel 8, connected to VBatt pin, ADC1
            /// </summary>
            public const int Channel_Vbatt = 8;
        }

        /// <summary>
        ///  GPIO class represent mapped info and values on 
        ///  Arduino Headers as A0 ... A5 and D0 ... D15 and
        ///  Morpho Headers as PA0 .... PC15 ...
        /// </summary>

        public static class Gpio 
        {
            public const int PA0 = 0;   public const int A0 = 0;
            public const int PA1 = 1;   public const int A1 = 1;  
            public const int PA2 = 2;   public const int D1 = 2;
            public const int PA3 = 3;   public const int D0 = 3;
            public const int PA4 = 4;   public const int A2 = 4;
            public const int PA5 = 5;   public const int D13 = 5;  public const int SCK = 5;     public const int Led1 = 5;
            public const int PA6 = 6;   public const int D12 = 6;  public const int MISO = 6;
            public const int PA7 = 7;   public const int D11 = 7;  public const int MOSI = 7;
            public const int PA8 = 8;   public const int D7 = 8;
            public const int PA9 = 9;   public const int D8 = 9;
            public const int PA10 = 10; public const int D2 = 10;
            public const int PA11 = 11;
            public const int PA12 = 12;
            public const int PA13 = 13;
            public const int PA14 = 14;
            public const int PA15 = 15;

            public const int PB0 = 16;  public const int A3 = 16;
            public const int PB1 = 17; 
            public const int PB2 = 18; 
            public const int PB3 = 19;  public const int D3 = 19;
            public const int PB4 = 20;  public const int D5 = 20;
            public const int PB5 = 21;  public const int D4 = 21;
            public const int PB6 = 22;  public const int D10 = 22;
            public const int PB7 = 23; 
            public const int PB8 = 24;  public const int D15 = 24;  public const int SCL = 24;
            public const int PB9 = 25;  public const int D14 = 25;  public const int SDA = 25;
            public const int PB10 = 26; public const int D6 = 26;
            public const int PB11 = 27;
            public const int PB12 = 28;
            public const int PB13 = 28;
            public const int PB14 = 30;
            public const int PB15 = 31;

            public const int PC0 = 32;  public const int A5 = 32;
            public const int PC1 = 33;  public const int A4 = 22;
            public const int PC2 = 34;
            public const int PC3 = 35;
            public const int PC4 = 36;
            public const int PC5 = 37;
            public const int PC6 = 38;
            public const int PC7 = 39;  public const int D9 = 39;
            public const int PC8 = 40;
            public const int PC9 = 41;
            public const int PC10 = 42;
            public const int PC11 = 43;
            public const int PC12 = 44;
            public const int PC13 = 45; public const int Buttonn1 = 45;
            public const int PC14 = 46;
            public const int PC15 = 47;

        }

        /// <summary>
        ///  this class is represented SPI Device on Nucleo F411 
        /// </summary>
        public static class SpiDevice
        {
            public static class Sp2
            {
                public const string Name = "SPI2";
                public const int MOSI = Gpio.PB15;
                public const int MISO = Gpio.PB14;
                public const int SCK = Gpio.PB13;
            }
        }

        /// <summary>
        ///  this clcass is represented I2C Device on Nucleo F411
        /// </summary>
        public class I2CDevice
        {
            struct I2C1
            {
                public const string Name = "I2C1";
                public const int SCL = 24;
                public const int SDA = 25;
            }
        }

    }
}
```

### ST7735 Driver / ST7735.cs
```csharp
using System;
using System.Threading;
using Windows.Devices.Gpio;
using Windows.Devices.Spi;

namespace nanoFramework.Driver
{
    /* source code taken from there 
     * https://github.com/bauland/TinyClrLib/blob/master/Modules/Gadgeteer/DisplayN18/
     * and modified to work with nanoFramework 
     */
    public class ST7735
    {
        private readonly SpiDevice _spi;
        private readonly GpioPin _controlPin;
        private readonly GpioPin _resetPin;
        private readonly GpioPin _backlightPin;
        private readonly GpioPin _chipSelectLine;

        private readonly byte[] _buffer1;
        private readonly byte[] _buffer2;
        private readonly byte[] _buffer4;
        private readonly byte[] _clearBuffer;
        private readonly byte[] _characterBuffer1;
        private readonly byte[] _characterBuffer2;
        private readonly byte[] _characterBuffer4;

        private const byte St7735Madctl = 0x36;
        private const byte MadctlMy = 0x80;
        private const byte MadctlMx = 0x40;
        private const byte MadctlMv = 0x20;
        private const byte MadctlBgr = 0x08;

        /// <summary>
        /// The width of the display in pixels.
        /// </summary>
        public const int Width = 160;

        /// <summary>
        /// The height of the display in pixels.
        /// </summary>
        public const int Height = 128;

        /// <summary>
        /// Constructor of Display ST7735
        /// </summary>
        /// <param name="resetPin">Reset Pin ST7735</param>
        /// <param name="backlightPin">BackLight Pin ST7735</param>
        /// <param name="controlPin">A0/DC Pin ST7735</param>
        /// <param name="spiBusLine">SCK=SPI SCK and SDA=SPI MOSI</param>
        /// <param name="chipSelectPin">CS Pin ST7735</param>
        public ST7735(int resetPin, int backlightPin, int controlPin, string spiLine, int chipSelectPin)
        {
            _buffer1 = new byte[1];
            _buffer2 = new byte[2];
            _buffer4 = new byte[4];
            _clearBuffer = new byte[160 * 2 * 16];
            _characterBuffer1 = new byte[80];
            _characterBuffer2 = new byte[320];
            _characterBuffer4 = new byte[1280];

            _controlPin = GpioController.GetDefault().OpenPin(controlPin);
            _controlPin.SetDriveMode(GpioPinDriveMode.Output);

            _resetPin = GpioController.GetDefault().OpenPin(resetPin);
            _resetPin.SetDriveMode(GpioPinDriveMode.Output);

            _backlightPin = GpioController.GetDefault().OpenPin(backlightPin);
            _backlightPin.SetDriveMode(GpioPinDriveMode.Output);
            _backlightPin.Write(GpioPinValue.Low);

            _chipSelectLine = GpioController.GetDefault().OpenPin(chipSelectPin);
            _chipSelectLine.SetDriveMode(GpioPinDriveMode.Output);


            var spiSettings = new SpiConnectionSettings(_chipSelectLine.PinNumber);
            spiSettings.ClockFrequency = 12000000;
            spiSettings.DataBitLength = 8;
            spiSettings.Mode = SpiMode.Mode0;

            _spi = SpiDevice.FromId(spiLine, spiSettings);

            Reset();

            WriteCommand(0x11); //Sleep exit 
            Thread.Sleep(120);

            // ST7735R Frame Rate
            WriteCommand(0xB1);
            WriteData(0x01); WriteData(0x2C); WriteData(0x2D);
            WriteCommand(0xB2);
            WriteData(0x01); WriteData(0x2C); WriteData(0x2D);
            WriteCommand(0xB3);
            WriteData(0x01); WriteData(0x2C); WriteData(0x2D);
            WriteData(0x01); WriteData(0x2C); WriteData(0x2D);

            WriteCommand(0xB4); // Column inversion 
            WriteData(0x07);

            // ST7735R Power Sequence
            WriteCommand(0xC0);
            WriteData(0xA2); WriteData(0x02); WriteData(0x84);
            WriteCommand(0xC1); WriteData(0xC5);
            WriteCommand(0xC2);
            WriteData(0x0A); WriteData(0x00);
            WriteCommand(0xC3);
            WriteData(0x8A); WriteData(0x2A);
            WriteCommand(0xC4);
            WriteData(0x8A); WriteData(0xEE);

            WriteCommand(0xC5); // VCOM 
            WriteData(0x0E);

            WriteCommand(0x36); // MX, MY, RGB mode
            WriteData(MadctlMx | MadctlMy | MadctlBgr);

            // ST7735R Gamma Sequence
            WriteCommand(0xe0);
            WriteData(0x0f); WriteData(0x1a);
            WriteData(0x0f); WriteData(0x18);
            WriteData(0x2f); WriteData(0x28);
            WriteData(0x20); WriteData(0x22);
            WriteData(0x1f); WriteData(0x1b);
            WriteData(0x23); WriteData(0x37); WriteData(0x00);

            WriteData(0x07);
            WriteData(0x02); WriteData(0x10);
            WriteCommand(0xe1);
            WriteData(0x0f); WriteData(0x1b);
            WriteData(0x0f); WriteData(0x17);
            WriteData(0x33); WriteData(0x2c);
            WriteData(0x29); WriteData(0x2e);
            WriteData(0x30); WriteData(0x30);
            WriteData(0x39); WriteData(0x3f);
            WriteData(0x00); WriteData(0x07);
            WriteData(0x03); WriteData(0x10);

            WriteCommand(0x2a);
            WriteData(0x00); WriteData(0x00);
            WriteData(0x00); WriteData(0x7f);
            WriteCommand(0x2b);
            WriteData(0x00); WriteData(0x00);
            WriteData(0x00); WriteData(0x9f);

            WriteCommand(0xF0); //Enable test command  
            WriteData(0x01);
            WriteCommand(0xF6); //Disable ram power save mode 
            WriteData(0x00);

            WriteCommand(0x3A); //65k mode 
            WriteData(0x05);

            // Rotate
            WriteCommand(St7735Madctl);
            WriteData(MadctlMv | MadctlMy);

            WriteCommand(0x29); //Display on
            Thread.Sleep(50);

            Clear();
        }

        private void WriteData(byte[] data)
        {
            _controlPin.Write(GpioPinValue.High);
            _spi.Write(data);
        }

        private void WriteCommand(byte command)
        {
            _buffer1[0] = command;
            _controlPin.Write(GpioPinValue.Low);
            _spi.Write(_buffer1);
        }

        private void WriteData(byte data)
        {
            _buffer1[0] = data;
            _controlPin.Write(GpioPinValue.High);
            _spi.Write(_buffer1);
        }

        private void Reset()
        {
            _resetPin.Write(GpioPinValue.Low);
            Thread.Sleep(300);
            _resetPin.Write(GpioPinValue.High);
            Thread.Sleep(1000);
        }

        private void SetClip(int x, int y, int width, int height)
        {
            WriteCommand(0x2A);

            _controlPin.Write(GpioPinValue.High);
            _buffer4[1] = (byte)x;
            _buffer4[3] = (byte)(x + width - 1);
            _spi.Write(_buffer4);

            WriteCommand(0x2B);
            _controlPin.Write(GpioPinValue.High);
            _buffer4[1] = (byte)y;
            _buffer4[3] = (byte)(y + height - 1);
            _spi.Write(_buffer4);
        }

        /// <summary>
        /// Clears the Display.
        /// </summary>
        public void Clear()
        {
            SetClip(0, 0, 160, 128);
            WriteCommand(0x2C);

            for (var i = 0; i < 128 / 16; i++)
                WriteData(_clearBuffer);
        }

        /// <summary>
        /// Draws an image.
        /// </summary>
        /// <param name="data">The image as a byte array.</param>
        public void DrawImage(byte[] data)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (data.Length == 0) throw new ArgumentException("data.Length must not be zero.", nameof(data));

            WriteCommand(0x2C);
            WriteData(data);
        }

        /// <summary>
        /// Draws an image at the given location.
        /// </summary>
        /// <param name="x">The x coordinate to draw at.</param>
        /// <param name="y">The y coordinate to draw at.</param>
        /// <param name="image">The image to draw.</param>
        public void DrawImage(int x, int y, Image image)
        {
            if (image == null) throw new ArgumentNullException("image");
            if (x < 0) throw new ArgumentOutOfRangeException(nameof(x), "x must not be negative.");
            if (y < 0) throw new ArgumentOutOfRangeException(nameof(y), "y must not be negative.");

            SetClip(x, y, image.Width, image.Height);
            DrawImage(image.Pixels);
        }

        /// <summary>
        /// Draws a filled rectangle.
        /// </summary>
        /// <param name="x">The x coordinate to draw at.</param>
        /// <param name="y">The y coordinate to draw at.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        /// <param name="color">The color to draw.</param>
        public void DrawFilledRectangle(int x, int y, int width, int height, Color color)
        {
            if (x < 0) throw new ArgumentOutOfRangeException(nameof(x), "x must not be negative.");
            if (y < 0) throw new ArgumentOutOfRangeException(nameof(y), "y must not be negative.");
            if (width < 0) throw new ArgumentOutOfRangeException(nameof(width), "width must not be negative.");
            if (height < 0) throw new ArgumentOutOfRangeException(nameof(height), "height must not be negative.");

            SetClip(x, y, width, height);

            var data = new byte[width * height * 2];
            for (var i = 0; i < data.Length; i += 2)
            {
                data[i] = (byte)((color.As565 >> 8) & 0xFF);
                data[i + 1] = (byte)((color.As565 >> 0) & 0xFF);
            }

            DrawImage(data);
        }

        /// <summary>
        /// Turns the backlight on.
        /// </summary>
        public void TurnOn()
        {
            _backlightPin.Write(GpioPinValue.High);
        }

        /// <summary>
        /// Turns the backlight off.
        /// </summary>
        public void TurnOff()
        {
            _backlightPin.Write(GpioPinValue.Low);
        }

        /// <summary>
        /// Draws a pixel.
        /// </summary>
        /// <param name="x">The x coordinate to draw at.</param>
        /// <param name="y">The y coordinate to draw at.</param>
        /// <param name="color">The color to draw.</param>
        public void SetPixel(int x, int y, Color color)
        {
            if (x < 0) throw new ArgumentOutOfRangeException(nameof(x), "x must not be negative.");
            if (y < 0) throw new ArgumentOutOfRangeException(nameof(y), "y must not be negative.");

            SetClip(x, y, 1, 1);

            _buffer2[0] = (byte)(color.As565 >> 8);
            _buffer2[1] = (byte)(color.As565 >> 0);

            DrawImage(_buffer2);
        }

        /// <summary>
        /// Draws a line.
        /// </summary>
        /// <param name="x0">The x coordinate to start drawing at.</param>
        /// <param name="y0">The y coordinate to start drawing at.</param>
        /// <param name="x1">The ending x coordinate.</param>
        /// <param name="y1">The ending y coordinate.</param>
        /// <param name="color">The color to draw.</param>
        public void DrawLine(int x0, int y0, int x1, int y1, Color color)
        {
            if (x0 < 0) throw new ArgumentOutOfRangeException(nameof(x0), "x0 must not be negative.");
            if (y0 < 0) throw new ArgumentOutOfRangeException(nameof(y0), "y0 must not be negative.");
            if (x1 < 0) throw new ArgumentOutOfRangeException(nameof(x1), "x1 must not be negative.");
            if (y1 < 0) throw new ArgumentOutOfRangeException(nameof(y1), "y1 must not be negative.");

            var steep = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);
            int t, dX, dY, yStep, error;

            if (steep)
            {
                t = x0;
                x0 = y0;
                y0 = t;
                t = x1;
                x1 = y1;
                y1 = t;
            }

            if (x0 > x1)
            {
                t = x0;
                x0 = x1;
                x1 = t;

                t = y0;
                y0 = y1;
                y1 = t;
            }

            dX = x1 - x0;
            dY = Math.Abs(y1 - y0);

            error = (dX / 2);

            if (y0 < y1)
            {
                yStep = 1;
            }
            else
            {
                yStep = -1;
            }

            for (; x0 < x1; x0++)
            {
                if (steep)
                {
                    SetPixel(y0, x0, color);
                }
                else
                {
                    SetPixel(x0, y0, color);
                }

                error -= dY;

                if (error < 0)
                {
                    y0 += (byte)yStep;
                    error += dX;
                }
            }
        }


        //public int Abs(int Values)
        //{
        //    int retValue = 0;
        //    if (Values <= 0) retValue = Values * -1;
        //    if (Values > 0) retValue = Values;

        //    return retValue;
        //}

        /// <summary>
        /// Draws a circle.
        /// </summary>
        /// <param name="x">The x coordinate to draw at.</param>
        /// <param name="y">The y coordinate to draw at.</param>
        /// <param name="r">The radius of the circle.</param>
        /// <param name="color">The color to draw.</param>
        public void DrawCircle(int x, int y, int r, Color color)
        {
            if (x < 0) throw new ArgumentOutOfRangeException(nameof(x), "x must not be negative.");
            if (y < 0) throw new ArgumentOutOfRangeException(nameof(y), "y must not be negative.");
            if (r <= 0) throw new ArgumentOutOfRangeException(nameof(r), "radius must be positive.");

            int f = 1 - r;
            int ddFx = 1;
            int ddFy = -2 * r;
            int dX = 0;
            int dY = r;

            SetPixel(x, y + r, color);
            SetPixel(x, y - r, color);
            SetPixel(x + r, y, color);
            SetPixel(x - r, y, color);

            while (dX < dY)
            {
                if (f >= 0)
                {
                    dY--;
                    ddFy += 2;
                    f += ddFy;
                }

                dX++;
                ddFx += 2;
                f += ddFx;

                SetPixel(x + dX, y + dY, color);
                SetPixel(x - dX, y + dY, color);
                SetPixel(x + dX, y - dY, color);
                SetPixel(x - dX, y - dY, color);

                SetPixel(x + dY, y + dX, color);
                SetPixel(x - dY, y + dX, color);
                SetPixel(x + dY, y - dX, color);
                SetPixel(x - dY, y - dX, color);
            }
        }

        /// <summary>
        /// Draws a rectangle.
        /// </summary>
        /// <param name="x">The x coordinate to draw at.</param>
        /// <param name="y">The y coordinate to draw at.</param>
        /// <param name="width">The width of the rectangle.</param>
        /// <param name="height">The height of the rectangle.</param>
        /// <param name="color">The color to use.</param>
        public void DrawRectangle(int x, int y, int width, int height, Color color)
        {
            if (x < 0) throw new ArgumentOutOfRangeException(nameof(x), "x must not be negative.");
            if (y < 0) throw new ArgumentOutOfRangeException(nameof(y), "y must not be negative.");
            if (width < 0) throw new ArgumentOutOfRangeException(nameof(width), "width must not be negative.");
            if (height < 0) throw new ArgumentOutOfRangeException(nameof(height), "height must not be negative.");

            for (var i = x; i < x + width; i++)
            {
                SetPixel(i, y, color);
                SetPixel(i, y + height - 1, color);
            }

            for (var i = y; i < y + height; i++)
            {
                SetPixel(x, i, color);
                SetPixel(x + width - 1, i, color);
            }
        }

        static readonly byte[] Font = new byte[ /* 95 * 5*/ ] {
            0x00, 0x00, 0x00, 0x00, 0x00, /* Space	0x20 */
            0x00, 0x00, 0x4f, 0x00, 0x00, /* ! */
            0x00, 0x07, 0x00, 0x07, 0x00, /* " */
            0x14, 0x7f, 0x14, 0x7f, 0x14, /* # */
            0x24, 0x2a, 0x7f, 0x2a, 0x12, /* $ */
            0x23, 0x13, 0x08, 0x64, 0x62, /* % */
            0x36, 0x49, 0x55, 0x22, 0x20, /* & */
            0x00, 0x05, 0x03, 0x00, 0x00, /* ' */
            0x00, 0x1c, 0x22, 0x41, 0x00, /* ( */
            0x00, 0x41, 0x22, 0x1c, 0x00, /* ) */
            0x14, 0x08, 0x3e, 0x08, 0x14, /* // */
            0x08, 0x08, 0x3e, 0x08, 0x08, /* + */
            0x50, 0x30, 0x00, 0x00, 0x00, /* , */
            0x08, 0x08, 0x08, 0x08, 0x08, /* - */
            0x00, 0x60, 0x60, 0x00, 0x00, /* . */
            0x20, 0x10, 0x08, 0x04, 0x02, /* / */
            0x3e, 0x51, 0x49, 0x45, 0x3e, /* 0		0x30 */
            0x00, 0x42, 0x7f, 0x40, 0x00, /* 1 */
            0x42, 0x61, 0x51, 0x49, 0x46, /* 2 */
            0x21, 0x41, 0x45, 0x4b, 0x31, /* 3 */
            0x18, 0x14, 0x12, 0x7f, 0x10, /* 4 */
            0x27, 0x45, 0x45, 0x45, 0x39, /* 5 */
            0x3c, 0x4a, 0x49, 0x49, 0x30, /* 6 */
            0x01, 0x71, 0x09, 0x05, 0x03, /* 7 */
            0x36, 0x49, 0x49, 0x49, 0x36, /* 8 */
            0x06, 0x49, 0x49, 0x29, 0x1e, /* 9 */
            0x00, 0x36, 0x36, 0x00, 0x00, /* : */
            0x00, 0x56, 0x36, 0x00, 0x00, /* ; */
            0x08, 0x14, 0x22, 0x41, 0x00, /* < */
            0x14, 0x14, 0x14, 0x14, 0x14, /* = */
            0x00, 0x41, 0x22, 0x14, 0x08, /* > */
            0x02, 0x01, 0x51, 0x09, 0x06, /* ? */
            0x3e, 0x41, 0x5d, 0x55, 0x1e, /* @		0x40 */
            0x7e, 0x11, 0x11, 0x11, 0x7e, /* A */
            0x7f, 0x49, 0x49, 0x49, 0x36, /* B */
            0x3e, 0x41, 0x41, 0x41, 0x22, /* C */
            0x7f, 0x41, 0x41, 0x22, 0x1c, /* D */
            0x7f, 0x49, 0x49, 0x49, 0x41, /* E */
            0x7f, 0x09, 0x09, 0x09, 0x01, /* F */
            0x3e, 0x41, 0x49, 0x49, 0x7a, /* G */
            0x7f, 0x08, 0x08, 0x08, 0x7f, /* H */
            0x00, 0x41, 0x7f, 0x41, 0x00, /* I */
            0x20, 0x40, 0x41, 0x3f, 0x01, /* J */
            0x7f, 0x08, 0x14, 0x22, 0x41, /* K */
            0x7f, 0x40, 0x40, 0x40, 0x40, /* L */
            0x7f, 0x02, 0x0c, 0x02, 0x7f, /* M */
            0x7f, 0x04, 0x08, 0x10, 0x7f, /* N */
            0x3e, 0x41, 0x41, 0x41, 0x3e, /* O */
            0x7f, 0x09, 0x09, 0x09, 0x06, /* P		0x50 */
            0x3e, 0x41, 0x51, 0x21, 0x5e, /* Q */
            0x7f, 0x09, 0x19, 0x29, 0x46, /* R */
            0x26, 0x49, 0x49, 0x49, 0x32, /* S */
            0x01, 0x01, 0x7f, 0x01, 0x01, /* T */
            0x3f, 0x40, 0x40, 0x40, 0x3f, /* U */
            0x1f, 0x20, 0x40, 0x20, 0x1f, /* V */
            0x3f, 0x40, 0x38, 0x40, 0x3f, /* W */
            0x63, 0x14, 0x08, 0x14, 0x63, /* X */
            0x07, 0x08, 0x70, 0x08, 0x07, /* Y */
            0x61, 0x51, 0x49, 0x45, 0x43, /* Z */
            0x00, 0x7f, 0x41, 0x41, 0x00, /* [ */
            0x02, 0x04, 0x08, 0x10, 0x20, /* \ */
            0x00, 0x41, 0x41, 0x7f, 0x00, /* ] */
            0x04, 0x02, 0x01, 0x02, 0x04, /* ^ */
            0x40, 0x40, 0x40, 0x40, 0x40, /* _ */
            0x00, 0x00, 0x03, 0x05, 0x00, /* `		0x60 */
            0x20, 0x54, 0x54, 0x54, 0x78, /* a */
            0x7F, 0x44, 0x44, 0x44, 0x38, /* b */
            0x38, 0x44, 0x44, 0x44, 0x44, /* c */
            0x38, 0x44, 0x44, 0x44, 0x7f, /* d */
            0x38, 0x54, 0x54, 0x54, 0x18, /* e */
            0x04, 0x04, 0x7e, 0x05, 0x05, /* f */
            0x08, 0x54, 0x54, 0x54, 0x3c, /* g */
            0x7f, 0x08, 0x04, 0x04, 0x78, /* h */
            0x00, 0x44, 0x7d, 0x40, 0x00, /* i */
            0x20, 0x40, 0x44, 0x3d, 0x00, /* j */
            0x7f, 0x10, 0x28, 0x44, 0x00, /* k */
            0x00, 0x41, 0x7f, 0x40, 0x00, /* l */
            0x7c, 0x04, 0x7c, 0x04, 0x78, /* m */
            0x7c, 0x08, 0x04, 0x04, 0x78, /* n */
            0x38, 0x44, 0x44, 0x44, 0x38, /* o */
            0x7c, 0x14, 0x14, 0x14, 0x08, /* p		0x70 */
            0x08, 0x14, 0x14, 0x14, 0x7c, /* q */
            0x7c, 0x08, 0x04, 0x04, 0x00, /* r */
            0x48, 0x54, 0x54, 0x54, 0x24, /* s */
            0x04, 0x04, 0x3f, 0x44, 0x44, /* t */
            0x3c, 0x40, 0x40, 0x20, 0x7c, /* u */
            0x1c, 0x20, 0x40, 0x20, 0x1c, /* v */
            0x3c, 0x40, 0x30, 0x40, 0x3c, /* w */
            0x44, 0x28, 0x10, 0x28, 0x44, /* x */
            0x0c, 0x50, 0x50, 0x50, 0x3c, /* y */
            0x44, 0x64, 0x54, 0x4c, 0x44, /* z */
            0x08, 0x36, 0x41, 0x41, 0x00, /* { */
            0x00, 0x00, 0x77, 0x00, 0x00, /* | */
            0x00, 0x41, 0x41, 0x36, 0x08, /* } */
            0x08, 0x08, 0x2a, 0x1c, 0x08  /* ~ */
        };

        private void DrawLetter(int x, int y, char letter, Color color, int scaleFactor)
        {
            var index = 5 * (letter - 32);
            var upper = (byte)(color.As565 >> 8);
            var lower = (byte)(color.As565 >> 0);
            var characterBuffer = scaleFactor == 1 ? _characterBuffer1 : (scaleFactor == 2 ? _characterBuffer2 : _characterBuffer4);

            var i = 0;

            for (var j = 1; j <= 64; j *= 2)
            {
                for (var k = 0; k < scaleFactor; k++)
                {
                    for (var l = 0; l < 5; l++)
                    {
                        for (var m = 0; m < scaleFactor; m++)
                        {
                            var show = (Font[index + l] & j) != 0;

                            characterBuffer[i++] = show ? upper : (byte)0x00;
                            characterBuffer[i++] = show ? lower : (byte)0x00;
                        }
                    }
                }
            }

            SetClip(x, y, 5 * scaleFactor, 8 * scaleFactor);
            DrawImage(characterBuffer);
        }

        /// <summary>
        /// Draws a letter at the given location.
        /// </summary>
        /// <param name="x">The x coordinate to draw at.</param>
        /// <param name="y">The y coordinate to draw at.</param>
        /// <param name="letter">The letter to draw.</param>
        /// <param name="color">The color to use.</param>
        public void DrawLetter(int x, int y, char letter, Color color)
        {
            if (letter > 126 || letter < 32) throw new ArgumentOutOfRangeException(nameof(letter), "This letter cannot be drawn.");
            if (x < 0) throw new ArgumentOutOfRangeException(nameof(x), "x must not be negative.");
            if (y < 0) throw new ArgumentOutOfRangeException(nameof(y), "y must not be negative.");

            DrawLetter(x, y, letter, color, 1);
        }

        /// <summary>
        /// Draws a large letter at the given location.
        /// </summary>
        /// <param name="x">The x coordinate to draw at.</param>
        /// <param name="y">The y coordinate to draw at.</param>
        /// <param name="letter">The letter to draw.</param>
        /// <param name="color">The color to use.</param>
        public void DrawLargeLetter(int x, int y, char letter, Color color)
        {
            if (letter > 126 || letter < 32) throw new ArgumentOutOfRangeException(nameof(letter), "This letter cannot be drawn.");
            if (x < 0) throw new ArgumentOutOfRangeException(nameof(x), "x must not be negative.");
            if (y < 0) throw new ArgumentOutOfRangeException(nameof(y), "y must not be negative.");

            DrawLetter(x, y, letter, color, 2);
        }

        /// <summary>
        /// Draws an extra large letter at the given location.
        /// </summary>
        /// <param name="x">The x coordinate to draw at.</param>
        /// <param name="y">The y coordinate to draw at.</param>
        /// <param name="letter">The letter to draw.</param>
        /// <param name="color">The color to use.</param>
        public void DrawExtraLargeLetter(int x, int y, char letter, Color color)
        {
            if (letter > 126 || letter < 32) throw new ArgumentOutOfRangeException(nameof(letter), "This letter cannot be drawn.");
            if (x < 0) throw new ArgumentOutOfRangeException(nameof(x), "x must not be negative.");
            if (y < 0) throw new ArgumentOutOfRangeException(nameof(y), "y must not be negative.");

            DrawLetter(x, y, letter, color, 4);
        }

        /// <summary>
        /// Draws text at the given location.
        /// </summary>
        /// <param name="x">The x coordinate to draw at.</param>
        /// <param name="y">The y coordinate to draw at.</param>
        /// <param name="text">The string to draw.</param>
        /// <param name="color">The color to use.</param>
        public void DrawText(int x, int y, string text, Color color)
        {
            if (x < 0) throw new ArgumentOutOfRangeException(nameof(x), "x must not be negative.");
            if (y < 0) throw new ArgumentOutOfRangeException(nameof(y), "y must not be negative.");
            if (text == null) throw new ArgumentNullException("data");

            for (var i = 0; i < text.Length; i++)
                DrawLetter(x + i * 6, y, text[i], color, 1);
        }

        /// <summary>
        /// Draws large text at the given location.
        /// </summary>
        /// <param name="x">The x coordinate to draw at.</param>
        /// <param name="y">The y coordinate to draw at.</param>
        /// <param name="text">The string to draw.</param>
        /// <param name="color">The color to use.</param>
        public void DrawLargeText(int x, int y, string text, Color color)
        {
            if (x < 0) throw new ArgumentOutOfRangeException(nameof(x), "x must not be negative.");
            if (y < 0) throw new ArgumentOutOfRangeException(nameof(y), "y must not be negative.");
            if (text == null) throw new ArgumentNullException("data");

            for (var i = 0; i < text.Length; i++)
                DrawLetter(x + i * 6 * 2, y, text[i], color, 2);
        }

        /// <summary>
        /// Draws extra large text at the given location.
        /// </summary>
        /// <param name="x">The x coordinate to draw at.</param>
        /// <param name="y">The y coordinate to draw at.</param>
        /// <param name="text">The string to draw.</param>
        /// <param name="color">The color to use.</param>
        public void DrawExtraLargeText(int x, int y, string text, Color color)
        {
            if (x < 0) throw new ArgumentOutOfRangeException(nameof(x), "x must not be negative.");
            if (y < 0) throw new ArgumentOutOfRangeException(nameof(y), "y must not be negative.");
            if (text == null) throw new ArgumentNullException("data");

            for (var i = 0; i < text.Length; i++)
                DrawLetter(x + i * 6 * 4, y, text[i], color, 4);
        }

        /// <summary>
        /// Draws a number at the given location.
        /// </summary>
        /// <param name="x">The x coordinate to draw at.</param>
        /// <param name="y">The y coordinate to draw at.</param>
        /// <param name="number">The number to draw.</param>
        /// <param name="color">The color to use.</param>
        public void DrawNumber(int x, int y, double number, Color color)
        {
            if (x < 0) throw new ArgumentOutOfRangeException(nameof(x), "x must not be negative.");
            if (y < 0) throw new ArgumentOutOfRangeException(nameof(y), "y must not be negative.");

            DrawText(x, y, number.ToString("N2"), color);
        }

        /// <summary>
        /// Draws a large number at the given location.
        /// </summary>
        /// <param name="x">The x coordinate to draw at.</param>
        /// <param name="y">The y coordinate to draw at.</param>
        /// <param name="number">The number to draw.</param>
        /// <param name="color">The color to use.</param>
        public void DrawLargeNumber(int x, int y, double number, Color color)
        {
            if (x < 0) throw new ArgumentOutOfRangeException(nameof(x), "x must not be negative.");
            if (y < 0) throw new ArgumentOutOfRangeException(nameof(y), "y must not be negative.");

            DrawLargeText(x, y, number.ToString("N2"), color);
        }

        /// <summary>
        /// Draws an extra large number at the given location.
        /// </summary>
        /// <param name="x">The x coordinate to draw at.</param>
        /// <param name="y">The y coordinate to draw at.</param>
        /// <param name="number">The number to draw.</param>
        /// <param name="color">The color to use.</param>
        public void DrawExtraLargeNumber(int x, int y, double number, Color color)
        {
            if (x < 0) throw new ArgumentOutOfRangeException(nameof(x), "x must not be negative.");
            if (y < 0) throw new ArgumentOutOfRangeException(nameof(y), "y must not be negative.");

            DrawExtraLargeText(x, y, number.ToString("N2"), color);
        }

        /// <summary>
        /// Draws a number at the given location.
        /// </summary>
        /// <param name="x">The x coordinate to draw at.</param>
        /// <param name="y">The y coordinate to draw at.</param>
        /// <param name="number">The number to draw.</param>
        /// <param name="color">The color to use.</param>
        public void DrawNumber(int x, int y, long number, Color color)
        {
            if (x < 0) throw new ArgumentOutOfRangeException(nameof(x), "x must not be negative.");
            if (y < 0) throw new ArgumentOutOfRangeException(nameof(y), "y must not be negative.");

            DrawText(x, y, number.ToString("N0"), color);
        }

        /// <summary>
        /// Draws a large number at the given location.
        /// </summary>
        /// <param name="x">The x coordinate to draw at.</param>
        /// <param name="y">The y coordinate to draw at.</param>
        /// <param name="number">The number to draw.</param>
        /// <param name="color">The color to use.</param>
        public void DrawLargeNumber(int x, int y, long number, Color color)
        {
            if (x < 0) throw new ArgumentOutOfRangeException(nameof(x), "x must not be negative.");
            if (y < 0) throw new ArgumentOutOfRangeException(nameof(y), "y must not be negative.");

            DrawLargeText(x, y, number.ToString("N0"), color);
        }

        /// <summary>
        /// Draws an extra large number at the given location.
        /// </summary>
        /// <param name="x">The x coordinate to draw at.</param>
        /// <param name="y">The y coordinate to draw at.</param>
        /// <param name="number">The number to draw.</param>
        /// <param name="color">The color to use.</param>
        public void DrawExtraLargeNumber(int x, int y, long number, Color color)
        {
            if (x < 0) throw new ArgumentOutOfRangeException(nameof(x), "x must not be negative.");
            if (y < 0) throw new ArgumentOutOfRangeException(nameof(y), "y must not be negative.");

            DrawExtraLargeText(x, y, number.ToString("N0"), color);
        }
    }

    public class Color
    {
        /// <summary>
        /// The amount of red.
        /// </summary>
        public byte R { get; set; }

        /// <summary>
        /// The amount of green.
        /// </summary>
        public byte G { get; set; }

        /// <summary>
        /// The amount of blue.
        /// </summary>
        public byte B { get; set; }

        /// <summary>
        /// The color in 565 format.
        /// </summary>
        public ushort As565
        {
            get
            {
                return (ushort)(((R & 0x1F) << 11) | ((G & 0x3F) << 5) | (B & 0x1F));
            }
        }

        /// <summary>
        /// Constructs a new instance with the given levels.
        /// </summary>
        public Color()
            : this(0, 0, 0)
        {

        }

        /// <summary>
        /// Constructs a new instance with the given levels.
        /// </summary>
        /// <param name="red">The amount of red.</param>
        /// <param name="green">The amount of green.</param>
        /// <param name="blue">The amount of blue.</param>
        public Color(byte red, byte green, byte blue)
        {
            R = red;
            G = green;
            B = blue;
        }

        /// <summary>
        /// A predefined color for black.
        /// </summary>
        public static Color Black = new Color(0, 0, 0);
        /// <summary>
        /// A predefined color for white.
        /// </summary>
        public static Color White = new Color(255, 255, 255);
        /// <summary>
        /// A predefined color for red.
        /// </summary>
        public static Color Red = new Color(255, 0, 0);
        /// <summary>
        /// A predefined color for green.
        /// </summary>
        public static Color Green = new Color(0, 255, 0);
        /// <summary>
        /// A predefined color for blue.
        /// </summary>
        public static Color Blue = new Color(0, 0, 255);
        /// <summary>
        /// A predefined color for yellow.
        /// </summary>
        public static Color Yellow = new Color(255, 255, 0);
        /// <summary>
        /// A predefined color for cyan.
        /// </summary>
        public static Color Cyan = new Color(0, 255, 255);
        /// <summary>
        /// A predefined color for magenta.
        /// </summary>
        public static Color Magenta = new Color(255, 0, 255);
    }

    /// <summary>
    /// Represents an image that can be reused.
    /// </summary>
    public class Image
    {
        /// <summary>
        /// Width of image
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// Height of image
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// Data of each pixel of image
        /// </summary>
        public byte[] Pixels { get; private set; }

        /// <summary>
        /// Constructs a new image with the given dimensions.
        /// </summary>
        /// <param name="width">The image width.</param>
        /// <param name="height">The image height.</param>
        public Image(int width, int height)
        {
            if (width < 0) throw new ArgumentOutOfRangeException(nameof(width), "width must not be negative.");
            if (height < 0) throw new ArgumentOutOfRangeException(nameof(height), "height must not be negative.");

            Width = width;
            Height = height;
            Pixels = new byte[width * height * 2];
        }

        /// <summary>
        /// Sets the given pixel to the given color.
        /// </summary>
        /// <param name="x">The x coordinate of the pixel to set.</param>
        /// <param name="y">The y coordinate of the pixel to set.</param>
        /// <param name="color">The color to set the pixel to.</param>
        public void SetPixel(int x, int y, Color color)
        {
            if (x < 0) throw new ArgumentOutOfRangeException(nameof(x), "x must not be negative.");
            if (y < 0) throw new ArgumentOutOfRangeException(nameof(y), "y must not be negative.");
            if (x > Width) throw new ArgumentOutOfRangeException(nameof(x), "x must not exceed Width.");
            if (y > Height) throw new ArgumentOutOfRangeException(nameof(y), "y must not exceed Height.");

            Pixels[(x * Width + y) * 2 + 0] = (byte)(color.As565 >> 8);
            Pixels[(x * Width + y) * 2 + 1] = (byte)(color.As565 >> 0);
        }
    }
}
```
