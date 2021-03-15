/*
MD_YX5300 - Library for YX5300 Serial MP3 module
Converted from C++ to C# with original code: MD_YX5300 by MajicDesigns for Arduino. MajicDesigns allowed the conversion.
See example module specifications: https://wiki.keyestudio.com/KS0387_keyestudio_YX5200-24SS_MP3_Module
*/

// #define DISPLAY_LOGS
#undef DISPLAY_LOGS

using System;
using Windows.Devices.SerialCommunication;
using nanoFramework.Hardware.Esp32;
using Windows.Storage.Streams;
using System.Threading;
using System.Diagnostics;

namespace Device.YX5300_NF
{
    public class YX5300_NF
    {
        private const int SERIAL_BPS = 9600;
        private const int TIMEOUT_IN_SEC = 1;
        public const int MAX_VOLUME = 30;
        

        private enum CommandSet
        {
            CMD_NUL = 0x00,             ///< No command
            CMD_NEXT_SONG = 0x01,       ///< Play next song
            CMD_PREV_SONG = 0x02,       ///< Play previous song
            CMD_PLAY_WITH_INDEX = 0x03, ///< Play song with index number
            CMD_VOLUME_UP = 0x04,       ///< Volume increase by one
            CMD_VOLUME_DOWN = 0x05,     ///< Volume decrease by one
            CMD_SET_VOLUME = 0x06,      ///< Set the volume to level specified
            CMD_SET_EQUALIZER = 0x07,   ///< Set the equalizer to specified level
            CMD_SNG_CYCL_PLAY = 0x08,   ///< Loop play (repeat) specified track
            CMD_SEL_DEV = 0x09,         ///< Select storage device to TF card
            CMD_SLEEP_MODE = 0x0a,      ///< Chip enters sleep mode
            CMD_WAKE_UP = 0x0b,         ///< Chip wakes up from sleep mode
            CMD_RESET = 0x0c,           ///< Chip reset
            CMD_PLAY = 0x0d,            ///< Playback restart
            CMD_PAUSE = 0x0e,           ///< Playback is paused
            CMD_PLAY_FOLDER_FILE = 0x0f,///< Play the song with the specified folder and index number
            CMD_STOP_PLAY = 0x16,       ///< Playback is stopped
            CMD_FOLDER_CYCLE = 0x17,    ///< Loop playback from specified folder
            CMD_SHUFFLE_PLAY = 0x18,    ///< Playback shuffle mode
            CMD_SET_SNGL_CYCL = 0x19,   ///< Set loop play (repeat) on/off for current file
            CMD_SET_DAC = 0x1a,         ///< DAC on/off control
            CMD_PLAY_W_VOL = 0x22,      ///< Play track at the specified volume
            CMD_SHUFFLE_FOLDER = 0x28,  ///< Playback shuffle mode for folder specified
            CMD_QUERY_STATUS = 0x42,    ///< Query Device Status
            CMD_QUERY_VOLUME = 0x43,    ///< Query Volume level
            CMD_QUERY_EQUALIZER = 0x44, ///< Query current equalizer (disabled in hardware)
            CMD_QUERY_TOT_FILES = 0x48, ///< Query total files in all folders
            CMD_QUERY_PLAYING = 0x4c,   ///< Query which track playing
            CMD_QUERY_FLDR_FILES = 0x4e,///< Query total files in folder
            CMD_QUERY_TOT_FLDR = 0x4f,  ///< Query number of folders
        }

        public enum StatusCode
        {
            STS_OK = 0x00,         ///< No error (library generated status)
            STS_TIMEOUT = 0x01,    ///< Timeout on response message (library generated status)
            STS_VERSION = 0x02,    ///< Wrong version number in return message (library generated status)
            STS_CHECKSUM = 0x03,   ///< Device checksum invalid (library generated status)
            STS_TF_INSERT = 0x3a,  ///< TF Card was inserted (unsolicited)
            STS_TF_REMOVE = 0x3b,  ///< TF card was removed (unsolicited)
            STS_FILE_END = 0x3d,   ///< Track/file has ended (unsolicited)
            STS_INIT = 0x3f,       ///< Initialization complete (unsolicited)
            STS_ERR_FILE = 0x40,   ///< Error file not found
            STS_ACK_OK = 0x41,     ///< Message acknowledged ok
            STS_STATUS = 0x42,     ///< Current status
            STS_VOLUME = 0x43,     ///< Current volume level
            STS_EQUALIZER = 0x44,  ///< Equalizer status
            STS_TOT_FILES = 0x48,  ///< TF Total file count
            STS_PLAYING = 0x4c,    ///< Current file playing
            STS_FLDR_FILES = 0x4e, ///< Total number of files in the folder
            STS_TOT_FLDR = 0x4f,   ///< Total number of folders
        };

        // Protocol Message Characters
        private const byte PKT_SOM = 0x7e;       ///< Start of message delimiter character
        private const byte PKT_VER = 0xff;       ///< Version information
        private const byte PKT_LEN = 0x06;       ///< Data packet length in bytes (excluding SOM, EOM)
        private const byte PKT_CMD_DUMMY = 0x00;    ///< Command placeholder
        private const byte PKT_FB_OFF = 0x00;    ///< Command feedback OFF
        private const byte PKT_FB_ON = 0x01;     ///< Command feedback ON
        private const byte PKT_DATA_NUL = 0x00;  ///< Packet data place marker 
        private const byte PKT_EOM = 0xef;       ///< End of message delimiter character

        // Command options
        private const byte CMD_OPT_ON = 0x00;    ///< On indicator
        private const byte CMD_OPT_OFF = 0x01;   ///< Off indicator
        private const byte CMD_OPT_DEV_UDISK = 0X01; ///< Device option UDisk (not used)
        private const byte CMD_OPT_DEV_TF = 0X02;    ///< Device option TF
        private const byte CMD_OPT_DEV_FLASH = 0X04; ///< Device option Flash (not used)

        private byte[] msg = new byte[] {
                PKT_SOM,      // 0: Start
                PKT_VER,      // 1: Version
                PKT_LEN,      // 2: Length
                PKT_CMD_DUMMY,         // 3: Command placeholder
                PKT_FB_ON,    // 4: Feedback
                PKT_DATA_NUL, // 5: Data Hi
                PKT_DATA_NUL, // 6: Data Lo
                PKT_DATA_NUL, // [7]: Checksum Hi (optional)
                PKT_DATA_NUL, // [8]: Checksum Lo (optional)
                PKT_EOM       // 7, [9]: End
        };

        public class Status
        {
            public StatusCode Code { get; set; }
            public UInt16 Data { get; set; }
        }
        private Status status = new Status();

        private SerialDevice serialDevice;
        private DataWriter outputDataWriter;
        private DataReader inputDataReader;

        private byte[] bufRx = new byte[30]; // receive buffer for serial comms
        private byte bufIdx;    // index for next char into _bufIdx
        private DateTime timeSent; // time last serial message was sent
        private bool waitResponse; // true when we are waiting response to a query
        private int timeoutDurationInMs = TIMEOUT_IN_SEC * 1000;

        public YX5300_NF(SerialDevice serialDevice)
        {
            this.serialDevice = serialDevice;
            this.serialDevice.BaudRate = SERIAL_BPS;
            this.serialDevice.Parity = SerialParity.None;
            this.serialDevice.StopBits = SerialStopBitCount.One;
            this.serialDevice.Handshake = SerialHandshake.None;
            this.serialDevice.DataBits = 8;
            this.serialDevice.WriteTimeout = new TimeSpan(0, 0, TIMEOUT_IN_SEC);
            this.serialDevice.ReadTimeout = new TimeSpan(0, 0, TIMEOUT_IN_SEC);
            outputDataWriter = new DataWriter(serialDevice.OutputStream);
            inputDataReader = new DataReader(serialDevice.InputStream)
            {
                InputStreamOptions = InputStreamOptions.Partial
            };           
        }

        // Methods for object management.

        public void Begin()
        {
            int cachedTimeout = timeoutDurationInMs;

            timeoutDurationInMs = 2000;  // initialization timeout needs to be a long one
            Reset();          // long timeout on this message
            timeoutDurationInMs = cachedTimeout;  // put back saved value

            // set the TF card system.
            // The synchronous call will return when the command is accepted
            // then it will be followed by an initialization message saying TF card is inserted.
            // Doc says this should be 200ms, so we set a timeout for 1000ms.
            Device(CMD_OPT_DEV_TF); // set the TF card file system
            timeSent = DateTime.UtcNow;
            while (!Check())
            {
                if ((DateTime.UtcNow - timeSent).TotalMilliseconds >= 1000) break;
            }
        }

        private bool Check()
        {
            // returns true when received full message or timeout

            byte c = 0x00;

            // check for timeout if waiting response
            var currentWaitDuration = (DateTime.UtcNow - timeSent).TotalMilliseconds;
            if (waitResponse && (currentWaitDuration >= timeoutDurationInMs))
            {
                ProcessResponse(true);
                return (true);
            }

            // check if any characters available
            var bytesRead = inputDataReader.Load(10);
            if (bytesRead == 0) return false;

            // process all the characters waiting
            do
            {
                c = inputDataReader.ReadByte();
#if DISPLAY_LOGS
                Debug.WriteLine($"{c:x}");
#endif

                if (c == PKT_SOM)
                {
                    bufIdx = 0;      // start of message - reset the index
                }                 

                bufRx[bufIdx++] = c;

                if (bufIdx >= bufRx.Length)  // keep index within array memory bounds
                    bufIdx = (byte)(bufRx.Length - 1);

                bytesRead--;

            } while (bytesRead > 0 && c != PKT_EOM);

            // check if we have a whole message to 
            // process and do something with it here!
            if (c == PKT_EOM)
            {
                ProcessResponse();
            }

            return (c == PKT_EOM);   // we have just processed a response

        }

        public void SetTimeout(int timeoutInSec)
        {
            timeoutDurationInMs = timeoutInSec * 1000;
        }

        public Status GetStatus()
        {
            return status;
        }

        public StatusCode GetStatusCode()
        {
            return status.Code;
        }

        public UInt16 GetStatusData()
        {
            return status.Data;
        }

        private bool Device(byte devId)
        {
            return SendRequest(CommandSet.CMD_SEL_DEV, PKT_DATA_NUL, devId);
        }

        internal bool Equalizer(int eqId)
        {
            return SendRequest(CommandSet.CMD_SET_EQUALIZER, PKT_DATA_NUL, (byte)(eqId > 5 ? 0 : eqId));
        }

        internal bool Sleep(int eqId)
        {
            return SendRequest(CommandSet.CMD_SLEEP_MODE, PKT_DATA_NUL, PKT_DATA_NUL);
        }

        internal bool WakeUp(int eqId)
        {
            return SendRequest(CommandSet.CMD_WAKE_UP, PKT_DATA_NUL, PKT_DATA_NUL);
        }

        internal bool Shuffle(bool isShuffled)
        {
            return SendRequest(CommandSet.CMD_SHUFFLE_PLAY, PKT_DATA_NUL, isShuffled ? CMD_OPT_ON : CMD_OPT_OFF);
        }
        
        private bool Reset()
        {
            return SendRequest(CommandSet.CMD_RESET, PKT_DATA_NUL, PKT_DATA_NUL);
        }

        // Methods for controlling playing MP3 files
        internal bool PlayNext()
        {
            return SendRequest(CommandSet.CMD_NEXT_SONG, PKT_DATA_NUL, PKT_DATA_NUL);
        }

        internal bool PlayPrev()
        {
            return SendRequest(CommandSet.CMD_PREV_SONG, PKT_DATA_NUL, PKT_DATA_NUL);
        }

        internal bool PlayStop()
        {
            return SendRequest(CommandSet.CMD_STOP_PLAY, PKT_DATA_NUL, PKT_DATA_NUL);
        }

        internal bool PauseStop()
        {
            return SendRequest(CommandSet.CMD_PAUSE, PKT_DATA_NUL, PKT_DATA_NUL);
        }

        internal bool PlayStart()
        {
            return SendRequest(CommandSet.CMD_PLAY, PKT_DATA_NUL, PKT_DATA_NUL);
        }

        internal bool PlayTrack(int trackNum)
        {
            return SendRequest(CommandSet.CMD_PLAY_WITH_INDEX, PKT_DATA_NUL, (byte)trackNum);
        }

        internal bool PlayTrackRepeat(int fileNum)
        {
            return SendRequest(CommandSet.CMD_SNG_CYCL_PLAY, PKT_DATA_NUL, (byte)  fileNum);
        }
        internal bool PlaySpecific(int folderNum, int fileNum)
        {
            return SendRequest(CommandSet.CMD_PLAY_FOLDER_FILE, (byte) folderNum, (byte) fileNum);
        }
        internal bool PlayFolderRepeat(int folderNum)
        {
            return SendRequest(CommandSet.CMD_FOLDER_CYCLE,  PKT_DATA_NUL, (byte)folderNum);
        }

        internal bool PlayFolderShuffle(int folderNum)
        {
            return SendRequest(CommandSet.CMD_SHUFFLE_FOLDER, PKT_DATA_NUL, (byte)folderNum);
        }

        // Methods for controlling MP3 output volume
        public  bool Volume(int volume)
        {
            return SendRequest(CommandSet.CMD_SET_VOLUME, PKT_DATA_NUL, (byte)(volume > MAX_VOLUME ? MAX_VOLUME : volume));
        }
        public int GetMaxVolume() { return MAX_VOLUME; }

        public bool VolumeInc()
        {
            return SendRequest(CommandSet.CMD_VOLUME_UP, PKT_DATA_NUL, PKT_DATA_NUL);
        }

        public bool VolumeDec()
        {
            return SendRequest(CommandSet.CMD_VOLUME_DOWN, PKT_DATA_NUL, PKT_DATA_NUL);
        }

        public bool VolumeMute(bool isMute)
        {
            return SendRequest(CommandSet.CMD_SET_DAC, PKT_DATA_NUL, isMute ? CMD_OPT_OFF : CMD_OPT_ON);
        }

        // Low level code
        private UInt16 CalcCheckSum(SpanByte data, int len)
        {
            UInt16 sum = 0;

            for (int i = 0; i < len; i++)
                sum += data[i];

            return (UInt16)(-sum);
        }

        private bool SendRequest(CommandSet cmd, byte dataHi, byte dataLo)
        {
            msg[3] = (byte) cmd;
            msg[5] = dataHi;
            msg[6] = dataLo;

            var data = new SpanByte(msg, 1, msg.Length - 1);
            UInt16 chk = CalcCheckSum(data, msg[2]);

            msg[7] = (byte)(chk >> 8);
            msg[8] = (byte)(chk & 0x00ff);

            outputDataWriter.WriteBytes(msg);
            outputDataWriter.Store();
            bufIdx = 0;

            Thread.Sleep(20);

            timeSent = DateTime.UtcNow;
            status.Code = StatusCode.STS_OK;
            waitResponse = true;

            do {
                Thread.Sleep(10);
            } while (!Check());

            return true;
        }

        private void ProcessResponse(bool isTimeout = false)
        {
            waitResponse = false;    // definitely no longer waiting

            SpanByte data = new SpanByte(bufRx, 1, bufRx.Length - 1);
            UInt16 chk = CalcCheckSum(data, bufRx[2]);
            UInt16 chkRcv = (UInt16)(((UInt16)bufRx[7] << 8) + bufRx[8]);


            // initialize to most probable message outcome
            status.Code = (StatusCode)bufRx[3];
            status.Data = (UInt16)(((UInt16)bufRx[5] << 8) | bufRx[6]);

            // now override with message packet errors, if any
            if (isTimeout)
                status.Code = StatusCode.STS_TIMEOUT;
            else if (bufRx[1] != PKT_VER)
                status.Code = StatusCode.STS_VERSION;
              else if (chk != chkRcv)
                status.Code = StatusCode.STS_CHECKSUM;

#if DISPLAY_LOGS

            Debug.Write($"Response status code {status.Code:x} => ");

            // allocate the return code & print debug message
            switch (status.Code)
            {
                case StatusCode.STS_OK: Debug.WriteLine("OK"); break;
                case StatusCode.STS_TIMEOUT: Debug.WriteLine("Timeout"); break;
                case StatusCode.STS_VERSION: Debug.WriteLine("Ver error"); break;
                case StatusCode.STS_CHECKSUM: Debug.Write($"Chk error calc={chk}"); Debug.WriteLine($" rcv={chkRcv}"); break;
                case StatusCode.STS_TF_INSERT: Debug.WriteLine("TF inserted"); break;
                case StatusCode.STS_TF_REMOVE: Debug.WriteLine("TF removed"); break;
                case StatusCode.STS_ACK_OK: Debug.WriteLine("Ack OK"); break;
                case StatusCode.STS_ERR_FILE: Debug.WriteLine($"File Error {status.Data}"); break;
                case StatusCode.STS_INIT: Debug.WriteLine($"Init 0x{status.Data:x}"); break;
                case StatusCode.STS_FILE_END: Debug.WriteLine($"Ended track {status.Data:x}"); break;
                case StatusCode.STS_STATUS: Debug.WriteLine($"Status 0x{status.Data:x}"); break;
                case StatusCode.STS_EQUALIZER: Debug.WriteLine($"Equalizer {status.Data}"); break;
                case StatusCode.STS_VOLUME: Debug.WriteLine($"Vol {status.Data:x}"); break;
                case StatusCode.STS_TOT_FILES: Debug.WriteLine($"Tot files {status.Data}"); break;
                case StatusCode.STS_PLAYING: Debug.WriteLine($"Playing File {status.Data}"); break;
                case StatusCode.STS_FLDR_FILES: Debug.WriteLine($"Folder files {status.Data}"); break;
                case StatusCode.STS_TOT_FLDR: Debug.WriteLine($"Tot folder: {status.Data}"); break;
                default: Debug.WriteLine("Unknown Status Code"); break;
            } 
#endif

        }

    }
}
