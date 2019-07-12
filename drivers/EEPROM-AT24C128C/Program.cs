using System;
using System.Text;
using System.Threading;
using Driver;


namespace I2C_EEPROM_TEST
{
    public class Program
    {
        public static void Main()
        {
            AT24C128C eeprom = new AT24C128C(0x50, "I2C1");

            String message = "Hello from MIMXRT1060!";
            byte[] messageToSent = Encoding.UTF8.GetBytes(message);
            UInt16 memoryAddress = 0x0;
            eeprom.write(memoryAddress, messageToSent);

            Thread.Sleep(100);

            byte[] receivedData = eeprom.read(memoryAddress, message.Length);
            String dataConvertedToString = System.Text.Encoding.UTF8.GetString(receivedData, 0, receivedData.Length);
            Console.WriteLine("Message read from EEPROM: " + dataConvertedToString);

            
        }
    }
}
