using Device.YX5300_NF;
using nanoFramework.Hardware.Esp32;
using System.Threading;
using Windows.Devices.SerialCommunication;

namespace YX5300_NF_Demo
{
    // Note MCU TX => YX5300 RX and visa versa
    public class Program
    {
        const int FOLDER_NUM = 1;
        const int FILE_NUM = 1;
        static SerialDevice serialDevice;
        static YX5300_NF mp3Player;
        public static void Main()
        {
            // Set GPIO functions for COM2 (this is UART2 on ESP32)
            Configuration.SetPinFunction(Gpio.IO17, DeviceFunction.COM2_TX);
            Configuration.SetPinFunction(Gpio.IO16, DeviceFunction.COM2_RX);

            // Open COM2 and instantiate player
            serialDevice = SerialDevice.FromId("COM2");
            mp3Player = new YX5300_NF(serialDevice);

            // Start player and play some files
            mp3Player.Begin();
            Thread.Sleep(1000);
            mp3Player.Volume(YX5300_NF.MAX_VOLUME / 2);

#if false
            // Repeat a folder
            mp3Player.PlayFolderRepeat(FOLDER_NUM);
            mp3Player.PlayStart();

#else
            // Repeat a file
            mp3Player.PlayTrackRepeat(FILE_NUM);
            mp3Player.PlayStart();
#endif

            Thread.Sleep(Timeout.Infinite);
        }
    }
}
