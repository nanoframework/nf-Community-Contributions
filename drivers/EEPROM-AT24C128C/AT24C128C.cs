using System;
using Windows.Devices.I2c;

namespace Driver
{
    public class AT24C128C
    {
        private int _address;
        private I2cDevice _memoryController;

        /// <summary>
        /// Creates a driver for the AT24C128C.
        /// </summary>
        /// <param name="address">The I2C address of the device.</param>
        /// <param name="i2cBus">The I2C bus where the device is connected to.</param>
        public AT24C128C(int address, string i2cBus)
        {
            // Store I2C address
            _address = address;

            var settings = new I2cConnectionSettings(address);

            // Instantiate I2C controller
            _memoryController = I2cDevice.FromId(i2cBus, settings);
        }

        public void write(UInt16 memoryAddress, byte[] messageToSent)
        {
            byte[] txBuffer = new byte[2 + messageToSent.Length];
            txBuffer[0] = (byte)((memoryAddress >> 8) & 0xFF);
            txBuffer[1] = (byte)(memoryAddress & 0xFF);
            messageToSent.CopyTo(txBuffer, 2);
            _memoryController.Write(txBuffer);
        }

        public byte[] read(UInt16 memoryAddress, int numOfBytes)
        {
            byte[] rxBuffer = new byte[numOfBytes];
            // Device address is followed by the memory address (two words)
            // and must be sent over the I2C bus before data reception
            byte[] txBuffer = new byte[2];
            txBuffer[0] = (byte)((memoryAddress >> 8) & 0xFF);
            txBuffer[1] = (byte)(memoryAddress & 0xFF);
            _memoryController.WriteRead(txBuffer, rxBuffer);

            return rxBuffer;
        }
        
            
    }
}