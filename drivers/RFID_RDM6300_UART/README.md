### Wiring
![alt text](https://github.com/valoni/netmf-interpreter4x/blob/master/nanoFrameworks%20Drivers%20and%20Examples/RDM6300/NUCLEO_RDM6300.png "Nucleo F411 wiring with RDM6300")

### How to use driver / Program.cs
```csharp
using System;
using System.Threading;
using System.Text;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;

namespace nf.RDM6300
{
    public class Program
    {
        static Rdm630 rfid;
        static int cnt = 0;
        public static void Main()
        {

            rfid = new Rdm630("COM6");
            rfid.DataReceived += Rfid_DataReceived;

         
            Thread.Sleep(Timeout.Infinite);
        }

        private static void Rfid_DataReceived(uint data1, uint data2, DateTime time)
        {
           
            Console.WriteLine(rfid.Tag);

        }
    }
}

```

### Driver / RDM6300.cs
```csharp
using System;
using System.Text;
using nanoFramework.Runtime.Events;

using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;


namespace nf.RDM6300
{

    /// <summary>
    /// Rdm630 RFID Reader
    /// </summary>
    /// <remarks><![CDATA[
    /// RDM630 pin layout:
    /// 
    ///   10 9  8           7  6
    ///   │  │  │           │  │
    /// █████████████████████████
    /// █████████████████████████
    /// █████████████████████████
    /// █████████████████████████
    ///   │  │  │  │  │         
    ///   1  2  3  4  5         
    ///
    ///  1 TX (Data out) -> Netduino pin 0 or 2 (COM1 or COM2)
    ///  2 RX (Data in) -> Netduino pin 1 or 3 (COM1 or COM2), but since it's read-only, may be left empty
    ///  3 Unused
    ///  4 GND -> Netduino Gnd
    ///  5 +5V(DC) -> Netduino +5V
    ///  6 ANT1 -> Antenna (polarity doesn't matter)
    ///  7 ANT2 -> Antenna (polarity doesn't matter)
    ///  8 GND -> Netduino Gnd (but if pin 4 is already connected, this may be left empty)
    ///  9 +5V(DC) -> Netduino +5V (but if pin 5 is already connected, this may be left empty)
    /// 10 LED -> A led if you want to have a led signalling when there's a transfer
    /// ]]></remarks>
    public class Rdm630
    {
        /// <summary>
        /// Contains a reference to the serial port the Rdm630 is connected to
        /// </summary>
        private SerialDevice _Serial;

        /// <summary>
        /// A read buffer of 14 bytes. Since every block of data has 14 bytes, this should be enough.
        /// </summary>
        private byte[] _ReadBuffer = new byte[14];

        /// <summary>
        /// The current position on the _ReadBuffer
        /// </summary>
        private byte _ReadPosition;

        /// <summary>
        /// Table to convert integers from the serial bus to a hex digit quickly
        /// </summary>
        private string _SerialConversionTable = "------------------------------------------------0123456789-------ABCDEF";

        /// <summary>
        /// Contains the last successfull RFID tag
        /// </summary>
        private string _LastSuccessfullRead;

        /// <summary>
        /// Triggered when data has been received
        /// </summary>
        public event NativeEventHandler DataReceived;

        /// <summary>
        /// The most recent scanned tag
        /// </summary>
        public string Tag
        {
            get { return this._LastSuccessfullRead; }
        }

        /// <summary>
        /// Rdm630 RFID Reader
        /// </summary>
        /// <param name="Port">The serial port the Rdm630 is connected to</param>
        public Rdm630(string Port)
        {
            //this._Serial = new SerialPort(Port, 9600, Parity.None, 8, StopBits.One);
            //this._Serial.ReadTimeout = 1000;
            //this._Serial.DataReceived += new SerialDataReceivedEventHandler(_Serial_DataReceived);
            //this._Serial.Open();

            //

            // set parameters
            _Serial = SerialDevice.FromId(Port);
            _Serial.BaudRate = 9600;
            _Serial.Parity = SerialParity.None;
            _Serial.StopBits = SerialStopBitCount.One;
            _Serial.Handshake = SerialHandshake.None;
            _Serial.DataBits = 8;
            this._Serial.DataReceived += new SerialDataReceivedEventHandler(_Serial_DataReceived);


        }

        /// <summary>
        /// Triggers when there is new data on the serial port
        /// </summary>
        /// <param name="Sender">The sender of the event, which is the SerialPort object</param>
        /// <param name="EventData">A SerialDataReceivedEventArgs object that contains the event data</param>
        private void _Serial_DataReceived(object Sender, SerialDataReceivedEventArgs EventData)
        {
            // Reads the whole buffer from the serial port
            byte[] ReadBuffer = new byte[this._Serial.BytesToRead];
           // this._Serial.Read(ReadBuffer, 0, ReadBuffer.Length);

            DataReader inputDataReader = new DataReader(_Serial.InputStream);
            inputDataReader.InputStreamOptions = InputStreamOptions.Partial;

            var bytesRead = inputDataReader.Load(_Serial.BytesToRead);

            inputDataReader.ReadBytes(ReadBuffer);

            // Loops through all bytes
            for (uint Index = 0; Index < ReadBuffer.Length; ++Index)
            {
                // Start byte
                if (ReadBuffer[Index] == 2)
                    this._ReadPosition = 0;
                // Adds the digit to the global read buffer
                this._ReadBuffer[this._ReadPosition] = ReadBuffer[Index];
                // Increases the position of the global read buffer
                ++this._ReadPosition;
                // global read buffer is full, lets validate
                if (this._ReadPosition == this._ReadBuffer.Length)
                {
                    // Resets the read position
                    this._ReadPosition = 0;
                    // Announces we got a full set of bytes
                    this._Rdm630_DataReceived();
                }
            }
        }

        /// <summary>
        /// Triggers when a full RFID tag is scanned
        /// </summary>
        private void _Rdm630_DataReceived()
        {
            // Validates the start and stop byte (should be 2 & 3)
            if (this._ReadBuffer[0] != 2 || this._ReadBuffer[13] != 3) return;

            // Fetches the 10 digits
            string Digits = "";
            for (int Index = 0; Index < 10; ++Index)
            {
                // Index + 1 since the first byte is the start byte
                Digits += this._SerialConversionTable[this._ReadBuffer[Index + 1]];
            }

            // Fetches the checksum from the buffer
            string BufferCheckSum = "";
            BufferCheckSum += this._SerialConversionTable[this._ReadBuffer[11]];
            BufferCheckSum += this._SerialConversionTable[this._ReadBuffer[12]];

            // Calculates the checksum from the digits
            uint CalcCheckSum = 0;
            for (int Index = 0; Index < 10; Index = Index + 2)
            {
                CalcCheckSum = CalcCheckSum ^ Tools.Hex2Dec(Digits.Substring(Index, 2));
            }

            // Do both checksums match?
            if (Tools.Hex2Dec(BufferCheckSum) == CalcCheckSum)
            {
                this._LastSuccessfullRead = Digits;
                if (this.DataReceived != null)
                    this.DataReceived(0, 0, new DateTime());
            }
        }
    }

    /// <summary>
    /// Generic, useful tools
    /// </summary>
    public static class Tools
    {
     
        /// <summary>Contains the name of the hardware provider</summary>
        private static string _HardwareProvider = "";

        /// <summary>Escapes all non-visible characters</summary>
        /// <param name="Input">Input text</param>
        /// <returns>Output text</returns>
        public static string Escape(string Input)
        {
            if (Input == null) return "";

            char[] Buffer = Input.ToCharArray();
            string RetValue = "";
            for (int i = 0; i < Buffer.Length; ++i)
            {
                if (Buffer[i] == 13)
                    RetValue += "\\r";
                else if (Buffer[i] == 10)
                    RetValue += "\\n";
                else if (Buffer[i] == 92)
                    RetValue += "\\\\";
                else if (Buffer[i] < 32 || Buffer[i] > 126)
                    RetValue += "\\" + Tools.Dec2Hex((int)Buffer[i], 2);
                else
                    RetValue += Buffer[i];
            }

            return RetValue;
        }

        /// <summary>
        /// Converts a Hex string to a number
        /// </summary>
        /// <param name="HexNumber">The Hex string (ex.: "0F")</param>
        /// <returns>The decimal value</returns>
        public static uint Hex2Dec(string HexNumber)
        {
            // Always in upper case
            HexNumber = HexNumber.ToUpper();
            // Contains all Hex posibilities
            string ConversionTable = "0123456789ABCDEF";
            // Will contain the return value
            uint RetVal = 0;
            // Will increase
            uint Multiplier = 1;

            for (int Index = HexNumber.Length - 1; Index >= 0; --Index)
            {
                RetVal += (uint)(Multiplier * (ConversionTable.IndexOf(HexNumber[Index])));
                Multiplier = (uint)(Multiplier * ConversionTable.Length);
            }

            return RetVal;
        }

        /// <summary>
        /// Converts a byte array to a char array
        /// </summary>
        /// <param name="Input">The byte array</param>
        /// <returns>The char array</returns>
        public static char[] Bytes2Chars(byte[] Input)
        {
            char[] Output = new char[Input.Length];
            for (int Counter = 0; Counter < Input.Length; ++Counter)
                Output[Counter] = (char)Input[Counter];
            return Output;
        }

        /// <summary>
        /// Converts a char array to a byte array
        /// </summary>
        /// <param name="Input">The char array</param>
        /// <returns>The byte array</returns>
        public static byte[] Chars2Bytes(char[] Input)
        {
            byte[] Output = new byte[Input.Length];
            for (int Counter = 0; Counter < Input.Length; ++Counter)
                Output[Counter] = (byte)Input[Counter];
            return Output;
        }

        /// <summary>
        /// Changes a number into a string and add zeros in front of it, if required
        /// </summary>
        /// <param name="Number">The input number</param>
        /// <param name="Digits">The amount of digits it should be</param>
        /// <param name="Character">The character to repeat in front (default: 0)</param>
        /// <returns>A string with the right amount of digits</returns>
        public static string ZeroFill(string Number, int Digits, char Character = '0')
        {
            bool Negative = false;
            if (Number.Substring(0, 1) == "-")
            {
                Negative = true;
                Number = Number.Substring(1);
            }

            for (int Counter = Number.Length; Counter < Digits; ++Counter)
            {
                Number = Character + Number;
            }
            if (Negative) Number = "-" + Number;
            return Number;
        }

        /// <summary>
        /// Changes a number into a string and add zeros in front of it, if required
        /// </summary>
        /// <param name="Number">The input number</param>
        /// <param name="MinLength">The amount of digits it should be</param>
        /// <param name="Character">The character to repeat in front (default: 0)</param>
        /// <returns>A string with the right amount of digits</returns>
        public static string ZeroFill(int Number, int MinLength, char Character = '0')
        {
            return ZeroFill(Number.ToString(), MinLength, Character);
            // In 4.2 it should be possible to replace this with the following line,
            // but due to a bug (http://netmf.codeplex.com/workitem/1322) it isn't.
            // return Number.toString("d" + MinLength.toString());
        }

        /// <summary>
        /// URL-encode according to RFC 3986
        /// </summary>
        /// <param name="Input">The URL to be encoded.</param>
        /// <returns>Returns a string in which all non-alphanumeric characters except -_.~ have been replaced with a percent (%) sign followed by two hex digits.</returns>
        public static string RawUrlEncode(string Input)
        {
            string RetValue = "";
            for (int Counter = 0; Counter < Input.Length; ++Counter)
            {
                byte CharCode = (byte)(Input.ToCharArray()[Counter]);
                if (
                   CharCode == 0x2d                        // -
                   || CharCode == 0x5f                     // _
                   || CharCode == 0x2e                     // .
                   || CharCode == 0x7e                     // ~
                   || (CharCode > 0x2f && CharCode < 0x3a) // 0-9
                   || (CharCode > 0x40 && CharCode < 0x5b) // A-Z
                   || (CharCode > 0x60 && CharCode < 0x7b) // a-z
                   )
                {
                    RetValue += Input.Substring(Counter, 1);
                }
                else
                {
                    // Calculates the hex value in some way
                    RetValue += "%" + Dec2Hex(CharCode, 2);
                }
            }

            return RetValue;
        }

        /// <summary>
        /// URL-decode according to RFC 3986
        /// </summary>
        /// <param name="Input">The URL to be decoded.</param>
        /// <returns>Returns a string in which original characters</returns>
        public static string RawUrlDecode(string Input)
        {
            string RetValue = "";
            for (int Counter = 0; Counter < Input.Length; ++Counter)
            {
                string Char = Input.Substring(Counter, 1);
                if (Char == "%")
                {
                    // Encoded character
                    string HexValue = Input.Substring(++Counter, 2);
                    ++Counter;
                    RetValue += (char)Hex2Dec(HexValue);
                }
                else
                {
                    // Normal character
                    RetValue += Char;
                }
            }

            return RetValue;
        }

        /// <summary>
        /// Encodes a string according to the BASE64 standard
        /// </summary>
        /// <param name="Input">The input string</param>
        /// <returns>The output string</returns>
        public static string Base64Encode(string Input)
        {
            // Pairs of 3 8-bit bytes will become pairs of 4 6-bit bytes
            // That's the whole trick of base64 encoding :-)

            int Blocks = Input.Length / 3;           // The amount of original pairs
            if (Blocks * 3 < Input.Length) ++Blocks; // Fixes rounding issues; always round up
            int Bytes = Blocks * 4;                  // The length of the base64 output

            // These characters will be used to represent the 6-bit bytes in ASCII
            char[] Base64_Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=".ToCharArray();

            // Converts the input string to characters and creates the output array
            char[] InputChars = Input.ToCharArray();
            char[] OutputChars = new char[Bytes];

            // Converts the blocks of bytes
            for (int Block = 0; Block < Blocks; ++Block)
            {
                // Fetches the input pairs
                byte Input0 = (byte)(InputChars.Length > Block * 3 ? InputChars[Block * 3] : 0);
                byte Input1 = (byte)(InputChars.Length > Block * 3 + 1 ? InputChars[Block * 3 + 1] : 0);
                byte Input2 = (byte)(InputChars.Length > Block * 3 + 2 ? InputChars[Block * 3 + 2] : 0);

                // Generates the output pairs
                byte Output0 = (byte)(Input0 >> 2);                           // The first 6 bits of the 1st byte
                byte Output1 = (byte)(((Input0 & 0x3) << 4) + (Input1 >> 4)); // The last 2 bits of the 1st byte followed by the first 4 bits of the 2nd byte
                byte Output2 = (byte)(((Input1 & 0xf) << 2) + (Input2 >> 6)); // The last 4 bits of the 2nd byte followed by the first 2 bits of the 3rd byte
                byte Output3 = (byte)(Input2 & 0x3f);                         // The last 6 bits of the 3rd byte

                // This prevents 0-bytes at the end
                if (InputChars.Length < Block * 3 + 2) Output2 = 64;
                if (InputChars.Length < Block * 3 + 3) Output3 = 64;

                // Converts the output pairs to base64 characters
                OutputChars[Block * 4] = Base64_Characters[Output0];
                OutputChars[Block * 4 + 1] = Base64_Characters[Output1];
                OutputChars[Block * 4 + 2] = Base64_Characters[Output2];
                OutputChars[Block * 4 + 3] = Base64_Characters[Output3];
            }

            return new string(OutputChars);
        }

        /// <summary>
        /// Converts a number to a Hex string
        /// </summary>
        /// <param name="Input">The number</param>
        /// <param name="MinLength">The minimum length of the return string (filled with 0s)</param>
        /// <returns>The Hex string</returns>
        public static string Dec2Hex(int Input, int MinLength = 0)
        {
#if MF_FRAMEWORK_VERSION_V4_2 || MF_FRAMEWORK_VERSION_V4_3
                // Since NETMF 4.2 int.toString() exists, so we can do this:
                return Input.ToString("x" + MinLength.ToString());
#else
            // Contains all Hex posibilities
            string ConversionTable = "0123456789ABCDEF";
            // Starts the conversion
            string RetValue = "";
            int Current = 0;
            int Next = Input;
            do
            {
                if (Next >= ConversionTable.Length)
                {
                    // The current digit
                    Current = (Next / ConversionTable.Length);
                    if (Current * ConversionTable.Length > Next) --Current;
                    // What's left
                    Next = Next - (Current * ConversionTable.Length);
                }
                else
                {
                    // The last digit
                    Current = Next;
                    // Nothing left
                    Next = -1;
                }
                RetValue += ConversionTable[Current];
            } while (Next != -1);

            return Tools.ZeroFill(RetValue, MinLength);
#endif
        }

        /// <summary>
        /// Converts a 16-bit array to an 8 bit array
        /// </summary>
        /// <param name="Data">The 16-bit array</param>
        /// <returns>The 8-bit array</returns>
        public static byte[] UShortsToBytes(ushort[] Data)
        {
            byte[] RetVal = new byte[Data.Length * 2];

            int BytePos = 0;
            for (int ShortPos = 0; ShortPos < Data.Length; ++ShortPos)
            {
                RetVal[BytePos++] = (byte)(Data[ShortPos] >> 8);
                RetVal[BytePos++] = (byte)(Data[ShortPos] & 0x00ff);
            }
            return RetVal;
        }

        /// <summary>
        /// Converts an 8-bit array to a 16 bit array
        /// </summary>
        /// <param name="Data">The 8-bit array</param>
        /// <returns>The 16-bit array</returns>
        public static ushort[] BytesToUShorts(byte[] Data)
        {
            ushort[] RetVal = new ushort[Data.Length / 2];

            int BytePos = 0;
            for (int ShortPos = 0; ShortPos < RetVal.Length; ++ShortPos)
            {
                RetVal[ShortPos] = (ushort)((Data[BytePos++] << 8) + Data[BytePos++]);
            }
            return RetVal;
        }

        /// <summary>Calculates an XOR Checksum</summary>
        /// <param name="Data">Input data</param>
        /// <returns>XOR Checksum</returns>
        public static byte XorChecksum(string Data)
        {
            return Tools.XorChecksum(Tools.Chars2Bytes(Data.ToCharArray()));
        }

        /// <summary>Calculates an XOR Checksum</summary>
        /// <param name="Data">Input data</param>
        /// <returns>XOR Checksum</returns>
        public static byte XorChecksum(byte[] Data)
        {
            byte Checksum = 0;
            for (int Pos = 0; Pos < Data.Length; ++Pos)
                Checksum ^= Data[Pos];

            return Checksum;
        }

     
        /// <summary>
        /// Rounds a value to a certain amount of digits
        /// </summary>
        /// <param name="Input">The input number</param>
        /// <param name="Digits">Amount of digits after the .</param>
        /// <returns>The rounded value (as float or double gave precision errors, hence the String type)</returns>
        public static string Round(float Input, int Digits = 2)
        {
            int Multiplier = 1;
            for (int i = 0; i < Digits; ++i) Multiplier *= 10;
            string Rounded = ((int)(Input * Multiplier)).ToString();

            return (Rounded.Substring(0, Rounded.Length - 2) + "." + Rounded.Substring(Rounded.Length - 2)).TrimEnd(new char[] { '0', '.' });
        }

        /// <summary>A generic event handler when receiving a string</summary>
        /// <param name="text">The actual string</param>
        /// <param name="time">Timestamp of the event</param>
        public delegate void StringEventHandler(string text, DateTime time);
    }
}
```
