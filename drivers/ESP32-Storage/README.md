# SD card and internal storage support for nanoFramework

Thanks to Adrian Soundly and Jose Somoes and everyone helped make "Storage" possible for nanoFramework.
Storage is still under construction check the samples here https://github.com/nanoframework/Samples/tree/master/samples/Storage
for updates and changes. This library is for SD card readers connected using SPI. Other SD card options below were not tested.

Mount a MMC sdcard using 4 bit data ( e.g Wrover boards )
Example: SDCard.MountMMC(false);

 Mount a MMC sdcard using 1 bit data ( e.g Olimex EVB boards )
Example: SDCard.MountMMC(true);

You may need a separate 3.3v power source for the SD card reader 

The card reader that I used for the test is the SparkFun microSD breakout board https://www.sparkfun.com/products/544

This library also supports internal storage for the ESP32 boards. Serial Peripheral Interface Flash File System (SPIFFS).  
SPIFFS is a lightweight file system connected by SPI bus. The configuration data space reserved for network, wireless, 
certificates,  and user data is 256K. The area reserved for SPIFFS is 0x2D0000, 0x40000.

Source Code: https://github.com/Dweaver309/nanoFrameworkStorage

![ScreenShot](https://github.com/Dweaver309/nanoFrameworkStorage/blob/master/ESP32SDCard.png)


