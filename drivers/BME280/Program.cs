using nanoFramework.Hardware.Esp32;

namespace BME280
{
    public class Program
    {
        public static void Main()
        {
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
        }
    }
}