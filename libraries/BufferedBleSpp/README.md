# A nanoFramefork BLE SPP server with Xamarin Forms client sample

The BufferedBleSpp library is an BLE SPP implementation that allows for the transfer of large byte arrays between a Xamarin Forms client and nanoFramework device.

The NordicSpp example in the nanoFramework Bluetooth BLE samples repository only allows transfers of up to 256 bytes at a time. BufferedBleSpp improves on this. It can come in handy when transferring e.g. Json, which can become quite lengthy, and one doesn't need to care too much about the size of the data. 

BufferedBleSpp transfers unfortunately are slow, so should only be used under non-critical timing situations.

Its been tested on a ESP32 that was able to load the ESP32_BLE_REV3 firmware.

This sample also demonstrates BufferBleSpp's use in a Xamarin Forms mobile client environment.

The nanoFramework project is provided as a zipped file [BufferedBleSppNanoF.zip].

The Xamarin Forms project shared project source files are also provided. See the instructions below for the setup activities.

There are two objects, both called BufferedBleSpp, one for each platform:

Sample Xamarin Forms code demonstrating its usage:
```	
messageLabel.Text = "";
var message = "";
for (int i = 0; i < 10000; i++) message += " " + i.ToString();
var messageArray = Encoding.UTF8.GetBytes(message);
progressBar.Progress = 0.0f;
var returnedData = await spp.SendMessage(messageArray, new CancellationTokenSource(30000).Token, (progress) => { 
  progressBar.Progress = progress;
});
messageLabel.Text = "Receive success";
Debug.WriteLine(Encoding.UTF8.GetString(returnedData));
```

Sample nanoFramework code demonstrating its usage:
```
private static byte[] Spp_ReceivedData(BufferedBleSpp sender, byte[] readDataEventArgs)
{
    string message = System.Text.UTF8Encoding.UTF8.GetString(readDataEventArgs, 0, readDataEventArgs.Length);

    // Do something with incoming message
    Thread.Sleep(1000); // Dummy delay

    // For this example lets respond by echoing received message
    return System.Text.UTF8Encoding.UTF8.GetBytes(message);
}
```

On the nanoFramework side the following needs to be done in preparation:

    Plugins to be loaded:

        nanoFramework.Device.Bluetooth
        nanoFramework.System.IO.Streams
        nanoFramework.System.Math

    The target needs to be updated to contain the latest BLE libraries:

        nanoff --serialport COMx  --target ESP32_BLE_REV3 --preview --update
	
On the Xamarin Forms the following needs to be done in preparation:

    Plugins	to be loaded:

        Plugin.BLE (xabre/xamarin-bluetooth-le) https://github.com/xabre/xamarin-bluetooth-le

    Android manifest need addition: 
 
    <uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION" />
    <uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
    <uses-permission android:name="android.permission.BLUETOOTH" />
    <uses-permission android:name="android.permission.BLUETOOTH_ADMIN" />
   
    On iOS you must add the following keys to your Info.plist: 
 
    <key>UIBackgroundModes</key>
    <array>
      <!--for connecting to devices (client)-->
      <string>bluetooth-central</string>

      <!--for server configurations if needed-->
      <string>bluetooth-peripheral</string>
    </array>

    <!--Description of the Bluetooth request message (required on iOS 10, deprecated)-->
    <key>NSBluetoothPeripheralUsageDescription</key>
    <string>YOUR CUSTOM MESSAGE</string>

    <!--Description of the Bluetooth request message (required on iOS 13)-->
    <key>NSBluetoothAlwaysUsageDescription</key>
    <string>YOUR CUSTOM MESSAGE</string>
