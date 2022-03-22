using Plugin.BLE.Abstractions.Contracts;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace BufferedBleSppXamarin
{
    internal class BufferedBleSpp
    {
        // RX... and TX... naming is from server viewpoint, not the client (this code)

        const int BUFFER_SIZE = 250;

        private Guid ServiceUUID = new Guid("12345678-1234-5678-1234-56789abcdef0");
        private Guid RxAmountCharacteristicUUID = new Guid("12345678-1234-5678-1234-56789abcdef1");
        private Guid RxBufferCharacteristicUUID = new Guid("12345678-1234-5678-1234-56789abcdef2");

        private Guid TxAmountCharacteristicUUID = new Guid("12345678-1234-5678-1234-56789abcdef3");
        private Guid TxBufferCharacteristicUUID = new Guid("12345678-1234-5678-1234-56789abcdef4");

        IAdapter adapter;
        IDevice device;
        ICharacteristic rxAmountCharacteristic;
        ICharacteristic rxBufferCharacteristic;
        ICharacteristic txAmountCharacteristic;
        ICharacteristic txBufferCharacteristic;
        byte[] txByteArray;


        public BufferedBleSpp(IAdapter adapter, IDevice device)
        {
            this.adapter = adapter;
            this.device = device;
        }

        public async Task Connect()
        {
            await adapter.ConnectToDeviceAsync(device);
            var service = await device.GetServiceAsync(ServiceUUID);
            await device.RequestMtuAsync(512);
            rxAmountCharacteristic = await service.GetCharacteristicAsync(RxAmountCharacteristicUUID);
            rxBufferCharacteristic = await service.GetCharacteristicAsync(RxBufferCharacteristicUUID);
            txAmountCharacteristic = await service.GetCharacteristicAsync(TxAmountCharacteristicUUID);
            txBufferCharacteristic = await service.GetCharacteristicAsync(TxBufferCharacteristicUUID);
        }

        /// <summary>
        /// Used to send a large (can be larger than 256 bytes) byte array to a nanoFramework device
        /// </summary>
        /// <param name="message">The large byte array</param>
        /// <param name="ctsToken">Used to cancel the action if needed. Typically used to provide a timeout</param>
        /// <param name="onProgress">Delegate that returns progress as a (0.0 < value < 1.0) value. 0.0 -> 0.5 is Tx progress, 0.5 -> 1.0 is Rx progress</param>
        /// <returns>A large byte array from the nanoFramework device</returns>
        public Task<byte[]> SendMessage(byte[] message, CancellationToken ctsToken, Action<float> onProgress = null)
        {
            bool isCancelled = false;
            var tcs = new TaskCompletionSource<byte[]>();
            Device.BeginInvokeOnMainThread(async () => {

            try
            {
                    ctsToken.Register(() => { isCancelled = true; });

                    #region *// Send data
                    txByteArray = new byte[0];

                    #region *// Indicate server how much data to expect
                    Int32 toTransmitCount = 0;
                    Int32 toTransmitAmount = message.Length;
                    await rxAmountCharacteristic.WriteAsync(BitConverter.GetBytes(toTransmitAmount), ctsToken);
                    #endregion

                    #region *// Send chunks to server from the source array
                    byte[] inBuffer = new byte[BUFFER_SIZE];
                    byte[] outBuffer;
                    using (Stream input = new MemoryStream(message))
                    {
                        while (input.Position < input.Length)
                        {
                            int remaining = BUFFER_SIZE, bytesRead;
                            while (remaining > 0 && (bytesRead = input.Read(inBuffer, 0,
                                    Math.Min(remaining, BUFFER_SIZE))) > 0)
                            {
                                outBuffer = new byte[bytesRead];
                                Array.Copy(inBuffer, 0, outBuffer, 0, bytesRead);
                                await rxBufferCharacteristic.WriteAsync(outBuffer, ctsToken);
                                remaining -= bytesRead;



                                #region *// Update progress
                                toTransmitCount += bytesRead;
                                var progress = 0.5f * toTransmitCount / toTransmitAmount;
                                onProgress?.Invoke(progress); 
                                #endregion
                            }
                        }
                    }
                    #endregion
                    #endregion

                    #region *// Receive data
                    #region *// Wait for server to respond with the data amount to receive
                    // await DoEvents();
                    var result = await txAmountCharacteristic.ReadAsync(ctsToken);
                    var dataAmount = BitConverter.ToInt32(result, 0);
                    var dataCount = 0;
                    while ((dataAmount == -1) && (isCancelled == false))
                    {
                        result = await txAmountCharacteristic.ReadAsync(ctsToken);
                        dataAmount = BitConverter.ToInt32(result, 0);
                    }
                    if (isCancelled) throw new Exception("The SendMessage command has been cancelled. Probably due to timeout");
                    #endregion

                    #region *// Read data chunks from server until all consumed
                    txByteArray = new byte[0];
                    while ((dataCount != dataAmount) && (isCancelled == false))
                    {
                        var data = await txBufferCharacteristic.ReadAsync(ctsToken); // Slow, but reliable
                        txByteArray = Combine(txByteArray, data);
                        dataCount += data.Length;

                        #region *// Update progress
                        var progress = (0.5f * dataCount / dataAmount) + 0.5f;
                        onProgress?.Invoke(progress); 
                        #endregion
                    }
                    if (isCancelled) throw new Exception("The SendMessage command has been cancelled. Probably due to timeout");
                    #endregion
                    tcs.SetResult(txByteArray);
                    #endregion
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            return tcs.Task;

            // Helper
            byte[] Combine(byte[] first, byte[] second)
            {
                byte[] bytes = new byte[first.Length + second.Length];
                Array.Copy(first, 0, bytes, 0, first.Length);
                Array.Copy(second, 0, bytes, first.Length, second.Length);
                return bytes;
            }
        }
    }
}
