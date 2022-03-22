//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

using System;
using nanoFramework.Device.Bluetooth.GenericAttributeProfile;
using System.Diagnostics;
using System.Text;
using nanoFramework.Device.Bluetooth.Spp;
using System.IO;
using System.Collections;
using nanoFramework.Device.Bluetooth;

namespace nanoFramework.Device.BufferedBleSpp
{
    /// <summary>
    /// Implementation of a SPP profile which can handle large buffer sizes.
    /// </summary>
    public class BufferedBleSpp
    {
        private const int BUFFER_SIZE = 250;

        private string deviceName;

        // UUID for UART service
        private Guid ServiceUUID = new Guid("12345678-1234-5678-1234-56789abcdef0");
        private Guid RxAmountCharacteristicUUID = new Guid("12345678-1234-5678-1234-56789abcdef1");
        private Guid RxBufferCharacteristicUUID = new Guid("12345678-1234-5678-1234-56789abcdef2");

        private Guid TxAmountCharacteristicUUID = new Guid("12345678-1234-5678-1234-56789abcdef3");
        private Guid TxBufferCharacteristicUUID = new Guid("12345678-1234-5678-1234-56789abcdef4");


        private readonly GattServiceProvider serviceProvider;
        private readonly GattLocalCharacteristic txAmountCharacteristic;
        private readonly GattLocalCharacteristic txBufferCharacteristic;
        private bool isConnected = false;

        Int32 rxByteAmount;
        Int32 rxByteCount;
        byte[] rxByteArray;

        int txAmount;
        ArrayList txByteArray = new ArrayList();

        /// <summary>
        /// Return true id client connected
        /// </summary>
        public bool IsConnected { get => this.isConnected; }

        public delegate byte[] RxDataEventHandler(BufferedBleSpp sender, byte[] ReadDataEventArgs);
        public delegate void ConnectedEventHandler(BufferedBleSpp sender, EventArgs e);

        /// <summary>
        /// Event handler for receiving data
        /// </summary>
        public event RxDataEventHandler ReceivedData;


        /// <summary>
        /// Constructor  SPP profile
        /// </summary>
        public BufferedBleSpp(string deviceName)
        {
            this.deviceName = deviceName;

            GattServiceProviderResult gspr = GattServiceProvider.Create(ServiceUUID);
            if (gspr.Error != nanoFramework.Device.Bluetooth.BluetoothError.Success)
            {
                throw new ArgumentException("Unable to create service");
            }

            serviceProvider = gspr.ServiceProvider;

            // Define RX characteristic
            GattLocalCharacteristicParameters rxAmountParam = new GattLocalCharacteristicParameters()
            {
                UserDescription = "RX Amount Characteristic",
                CharacteristicProperties = GattCharacteristicProperties.WriteWithoutResponse 
            };

            GattLocalCharacteristicResult rxAmountCharRes = serviceProvider.Service.CreateCharacteristic(RxAmountCharacteristicUUID, rxAmountParam);
            if (rxAmountCharRes.Error != nanoFramework.Device.Bluetooth.BluetoothError.Success)
            {
                throw new ArgumentException("Unable to create RX Amount Characteristic");
            }

            GattLocalCharacteristicParameters rxBufferParam = new GattLocalCharacteristicParameters()
            {
                UserDescription = "RX Buffer Characteristic",
                CharacteristicProperties = GattCharacteristicProperties.WriteWithoutResponse 
            };

            GattLocalCharacteristicResult rxBufferCharRes = serviceProvider.Service.CreateCharacteristic(RxBufferCharacteristicUUID, rxBufferParam);
            if (rxBufferCharRes.Error != nanoFramework.Device.Bluetooth.BluetoothError.Success)
            {
                throw new ArgumentException("Unable to create RX Buffer Characteristic");
            }

            GattLocalCharacteristic rxAmountCharacteristic = rxAmountCharRes.Characteristic;
            rxAmountCharacteristic.WriteRequested += RxAmountCharacteristic_WriteRequested;

            GattLocalCharacteristic rxBufferCharacteristic = rxBufferCharRes.Characteristic;
            rxBufferCharacteristic.WriteRequested += RxBufferCharacteristic_WriteRequested;


            // Define TX characteristic
            GattLocalCharacteristicParameters txAmountParam = new GattLocalCharacteristicParameters()
            {
                UserDescription = "TX Amount Characteristic",
                CharacteristicProperties =  GattCharacteristicProperties.Read
            };

            GattLocalCharacteristicResult txAmountCharRes = serviceProvider.Service.CreateCharacteristic(TxAmountCharacteristicUUID, txAmountParam);
            if (txAmountCharRes.Error != nanoFramework.Device.Bluetooth.BluetoothError.Success)
            {
                throw new ArgumentException("Unable to create TX Amount Characteristic");
            }

            GattLocalCharacteristicParameters txBufferParam = new GattLocalCharacteristicParameters()
            {
                UserDescription = "TX Characteristic",
                CharacteristicProperties = GattCharacteristicProperties.Read
            };

            GattLocalCharacteristicResult txBufferCharRes = serviceProvider.Service.CreateCharacteristic(TxBufferCharacteristicUUID, txBufferParam);
            if (txBufferCharRes.Error != nanoFramework.Device.Bluetooth.BluetoothError.Success)
            {
                throw new ArgumentException("Unable to create TX Amount Characteristic");
            }

            txAmountCharacteristic = txAmountCharRes.Characteristic;
            txAmountCharacteristic.ReadRequested += _txCharacteristic_AmountReadRequested;

            txBufferCharacteristic = txBufferCharRes.Characteristic;
            txBufferCharacteristic.ReadRequested += _txCharacteristic_BufferReadRequested;

        }


        /// <summary>
        /// Start device advertising
        /// </summary>
        /// <param name="deviceName">Device name for Advertising</param>
        /// <returns></returns>
        public bool Start()
        {
            GattServiceProviderAdvertisingParameters advParameters = new GattServiceProviderAdvertisingParameters
            {
                DeviceName = deviceName,
                IsDiscoverable = true,
                IsConnectable = true
            };

            serviceProvider.StartAdvertising(advParameters);

            return true;
        }

        /// <summary>
        /// Stop advertising.
        /// </summary>
        public void Stop()
        {
            serviceProvider?.StopAdvertising();
        }

        /// <summary>
        /// Event handler for Received data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="WriteRequestEventArgs"></param>
        private void RxAmountCharacteristic_WriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs WriteRequestEventArgs)
        {
            GattWriteRequest request = WriteRequestEventArgs.GetRequest();

            byte[] data = new byte[request.Value.Length];

            DataReader rdr = DataReader.FromBuffer(request.Value);
            rdr.ReadBytes(data);

            #region *// Init the process
            rxByteAmount = BitConverter.ToInt32(data, 0);
            rxByteCount = 0;
            rxByteArray = new byte[0];
            txAmount = -1;
            txByteArrayReadCount = 0;
            txByteArray.Clear(); 
            #endregion

        }

        private void RxBufferCharacteristic_WriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs WriteRequestEventArgs)
        {
            GattWriteRequest request = WriteRequestEventArgs.GetRequest();

            byte[] data = new byte[request.Value.Length];

            DataReader rdr = DataReader.FromBuffer(request.Value);
            rdr.ReadBytes(data);

            rxByteArray = Combine(rxByteArray, data);
            rxByteCount += data.Length;

            if (rxByteCount == rxByteAmount)
            {
                // Request received, pass data to application
                var response = ReceivedData?.Invoke(this, rxByteArray);

                // Return data from application
                txAmount = response.Length;


                #region *// Build list with chunks of bytes buffers from the source array
                byte[] inBuffer = new byte[BUFFER_SIZE];
                byte[] outBuffer;
                using (Stream input = new MemoryStream(response))
                {
                    while (input.Position < input.Length)
                    {
                        int remaining = BUFFER_SIZE, bytesRead;
                        while (remaining > 0 && (bytesRead = input.Read(inBuffer, 0,
                                Math.Min(remaining, BUFFER_SIZE))) > 0)
                        {
                            outBuffer = new byte[bytesRead];
                            Array.Copy(inBuffer, 0, outBuffer, 0, bytesRead);
                            txByteArray.Add(new Buffer(outBuffer));
                            remaining -= bytesRead;
                        }
                    }
                }
                #endregion
            }

            if (request.Option == GattWriteOption.WriteWithResponse)
            {
                request.Respond();
            }

            // Helper
            byte[] Combine(byte[] first, byte[] second)
            {
                byte[] bytes = new byte[first.Length + second.Length];
                Array.Copy(first, 0, bytes, 0, first.Length);
                Array.Copy(second, 0, bytes, first.Length, second.Length);
                return bytes;
            }

        }

        
        private void _txCharacteristic_AmountReadRequested(GattLocalCharacteristic sender, GattReadRequestedEventArgs ReadRequestEventArgs)
        {
            var request = ReadRequestEventArgs.GetRequest();
            var amountArray = BitConverter.GetBytes((Int32)txAmount);
            request.RespondWithValue(new Buffer(amountArray));
        }

        int txByteArrayReadCount;
        private void _txCharacteristic_BufferReadRequested(GattLocalCharacteristic sender, GattReadRequestedEventArgs ReadRequestEventArgs)
        {
            var request = ReadRequestEventArgs.GetRequest();
            if (txByteArrayReadCount < txByteArray.Count)
            {
                request.RespondWithValue((Buffer) txByteArray[txByteArrayReadCount]);
                txByteArrayReadCount++;
            }
            else
            {
                request.RespondWithValue(new Buffer(new byte[1]{ 0x00 }));
            }
        }
    }
}