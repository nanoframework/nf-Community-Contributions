**BME280 Driver**

.Net nanoFramework driver for BME280 sensor measuring relative humidity, barometric pressure and ambient temperature. 

Source code taken from 
https://github.com/networkfusion/MBN-TinyCLR/tree/develop-nanoframework/Drivers/BME280

From the driver removed portions of code related to the MikroBus and fixed errors to Math.Pow for ESP32 firmware without support for DP floating point

Example code:

```
//Setup I2C pins for ESP32 board
Configuration.SetPinFunction(21, DeviceFunction.I2C1_DATA);
Configuration.SetPinFunction(22, DeviceFunction.I2C1_CLOCK);

BME280 bme280Sensor = new BME280(1, BME280.I2CAddresses.Address0);

while (true)
{             
    Debug.WriteLine(bme280Sensor.ReadHumidity());
    Debug.WriteLine(bme280Sensor.ReadPressure());
    Debug.WriteLine(bme280Sensor.ReadTemperature());

    Thread.Sleep(10000);            
}   
```
