using nanoFramework.Hardware.Esp32.RMT.Tx;
using System;

namespace WS2812
{
    public class PixelController
    {
        #region Fields
        // 80MHz / 4 => min pulse 0.00us
        protected const byte CLOCK_DEVIDER = 2;
        // one pulse duration in us
        protected const float min_pulse = 1000000.0f / (80000000 / CLOCK_DEVIDER);

        // default datasheet values
        protected readonly PulseCommand onePulse =
            new PulseCommand((ushort)(0.8 / min_pulse), true, (ushort)(0.45 / min_pulse), false);

        protected readonly PulseCommand zeroPulse =
            new PulseCommand((ushort)(0.4 / min_pulse), true, (ushort)(0.85 / min_pulse), false);

        protected readonly PulseCommand RETCommand =
            new PulseCommand((ushort)(25 / min_pulse), false, (ushort)(26 / min_pulse), false);

        protected Color[] pixels;
        //pixels as binary command ready to be send
        private byte[] binaryCommandData;

        protected Transmitter transmitter;

        public bool Is4BytesPrePixel { get; set; }

        public int PixelsCount { get => pixels.Length; }

        public float T0H
        {
            get => zeroPulse.Duration1 * min_pulse;
            set => zeroPulse.Duration1 = (ushort)(value / min_pulse);
        }

        public float T0L
        {
            get => zeroPulse.Duration2 * min_pulse;
            set => zeroPulse.Duration2 = (ushort)(value / min_pulse);
        }

        public float T1H
        {
            get => onePulse.Duration1 * min_pulse;
            set => onePulse.Duration1 = (ushort)(value / min_pulse);
        }

        public float T1L
        {
            get => onePulse.Duration2 * min_pulse;
            set => onePulse.Duration2 = (ushort)(value / min_pulse);
        }
        #endregion Fields

        public PixelController(int gpioPin, uint pixelCount, bool is4BytesPrePixel = false)
        {
            transmitter = Transmitter.Register(gpioPin);
            ConfigureTransmitter();
            Is4BytesPrePixel = is4BytesPrePixel;
            pixels = new Color[pixelCount];

            for (uint i = 0; i < pixelCount; ++i)
            {
                pixels[i] = new Color();
            }

            //Populate all bytes of the binaryCommand array that will not change during update operations. Just for faster performance on updating pixels
            binaryCommandData = new byte[pixelCount * 3 * 8 * 4 + 4];
            for (int i = 1; i <= binaryCommandData.Length - 4 - 3; i += 4)
            {
                binaryCommandData[i] = 128;
                binaryCommandData[i + 2] = 0;
            }
        }

        #region Public

        /// <summary>
        /// Set specific color (HSV format) on pixel at position
        /// </summary>
        /// <param name="index"></param>
        /// <param name="hue"></param>
        /// <param name="saturation"></param>
        /// <param name="value"></param>
        public void SetHSVColor(short index, short hue, float saturation, float value)
        {
            pixels[index].SetHSV(hue, saturation, value);
            UpdatePixelBinary(index);
        }

        /// <summary>
        /// Set specific color (RGB Format) on pixel at position
        /// </summary>
        /// <param name="index"></param>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        public void SetColor(short index, byte r, byte g, byte b)
        {
            pixels[index].R = r;
            pixels[index].G = g;
            pixels[index].B = b;
            UpdatePixelBinary(index);
        }

        /// <summary>
        /// Update pixels that are changed befor with (SetColor, SetHSVColor) methods. Works fast.
        /// </summary>
        public void UpdatePixels()
        {
            var start = DateTime.UtcNow;
            transmitter.SendData(binaryCommandData);
        }

        /// <summary>
        /// Update all pixels. Working slower
        /// </summary>
        public void Update()
        {
            var commandlist = new PulseCommandList();
            for (uint pixel = 0; pixel < pixels.Length; ++pixel)
            {
                SerialiseColor(pixels[pixel].G, commandlist);
                SerialiseColor(pixels[pixel].R, commandlist);
                SerialiseColor(pixels[pixel].B, commandlist);
                if (Is4BytesPrePixel)
                    SerialiseColor(pixels[pixel].W, commandlist);
            }
            commandlist.AddCommand(RETCommand);
            transmitter.Send(commandlist);
        }

        private void SerialiseColor(byte b, PulseCommandList commandlist)
        {
            for (int i = 0; i < 8; ++i)
            {
                commandlist.AddCommand(((b & (1u << 7)) != 0) ? onePulse : zeroPulse);
                b <<= 1;
            }
        }

        public void MovePixelsByStep(short step)
        {
            byte[] bytesToMove = new byte[96 * step];
            Array.Copy(binaryCommandData, 0, bytesToMove, 0, (96 * step));

            Array.Copy(binaryCommandData, 96 * step, binaryCommandData, 0, binaryCommandData.Length - (96 * step) - 4);

            Array.Copy(bytesToMove, 0, binaryCommandData, binaryCommandData.Length - (96 * step) - 4, (96 * step));
        }

        public void TurnOff()
        {
            for (uint pixel = 0; pixel < pixels.Length; pixel++)
            {
                pixels[pixel].R = pixels[pixel].G = pixels[pixel].B = 0;
            }
            Update();
        }

        #endregion Public

        #region Private

        private void UpdatePixelBinary(short index)
        {
            byte convertValueG = pixels[index].G;
            byte convertValueR = pixels[index].R;
            byte convertValueB = pixels[index].B;
            byte[] bits = new byte[24];
            //Get all 24 bits of the changed pixel
            for (int i = 0; i < 8; i++)
            {
                bits[i] = (byte)(((convertValueG & (1u << 7)) != 0) ? 1 : 0);
                convertValueG <<= 1;

                bits[i + 8] = (byte)(((convertValueR & (1u << 7)) != 0) ? 1 : 0);
                convertValueR <<= 1;

                bits[i + 16] = (byte)(((convertValueB & (1u << 7)) != 0) ? 1 : 0);
                convertValueB <<= 1;
            }

            AddBitsToCommand(bits, 32 * 3 * index);
        }

        private void AddBitsToCommand(byte[] bits, int index)
        {
            //Every bit is represented as command. Every command contains 4 bytes the first 2 bytes are for the high signal and the second 2 for the low.
            //In every paire the first is the duration of the signal and the second is the level of the signal high/low.
            //Here we construct the binary command by setting durations for every bit of the pixel. So we have total of 86 bytes for every bit that we update.
            //The levels are previously filled for all pixels so here we set only the durations.
            for (int i = 0; i < bits.Length; i++)
            {
                if (bits[i] == 1)
                {
                    binaryCommandData[i * 4 + index] = (byte)onePulse.Duration1;
                    binaryCommandData[(i * 4) + 2 + index] = (byte)onePulse.Duration2;
                }
                else
                {
                    binaryCommandData[i * 4 + index] = (byte)zeroPulse.Duration1;
                    binaryCommandData[(i * 4) + 2 + index] = (byte)zeroPulse.Duration2;
                }
            }
        }

        private void ConfigureTransmitter()
        {
            transmitter.CarierEnabled = false;
            transmitter.ClockDivider = CLOCK_DEVIDER;
            transmitter.isSource80MHz = true;
            transmitter.TransmitIdleLevel = false;
            transmitter.IsTransmitIdleEnabled = true;
        }

        private void Dispose(bool disposing)
        {
            transmitter.Dispose();
        }

        #endregion Private
    }
}
