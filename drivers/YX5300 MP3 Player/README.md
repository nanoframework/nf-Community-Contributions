# XY5300 driver

Use this driver to control the Keyestudio YX5200-24SS MP3/Jaycar XC3748 Music Player Module

This MP3 module is a MP3/WAV/WMA music player. It uses YX5200/YX5300 at its core and it plays files from an integrated SD card reader.

See https://wiki.keyestudio.com/KS0387_keyestudio_YX5200-24SS_MP3_Module for a good oversight.

Example code:<br/>
```
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
```
