using nanoFramework.Device.BufferedBleSpp;
using System;
using System.Diagnostics;
using System.Threading;

namespace BufferedBleSppNanoF
{
    public class Program
    {
        public static void Main()
        {
            try
            {
                Debug.WriteLine("Hello from nanoFramework!");

#if true
                if (!Debugger.IsAttached)
                {
                    Debug.WriteLine("App stopped because not connected to debugger. Remove this code when running standalone");
                    Thread.Sleep(Timeout.Infinite);
                } 
#endif

                var spp = new BufferedBleSpp("BufferedBleSppServer");
                spp.ReceivedData += Spp_ReceivedData;
                spp.Start();

                Thread.Sleep(Timeout.Infinite);

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

        }



        private static byte[] Spp_ReceivedData(BufferedBleSpp sender, byte[] readDataEventArgs)
        {
            string message = System.Text.UTF8Encoding.UTF8.GetString(readDataEventArgs, 0, readDataEventArgs.Length);

            // Do something with incoming message
            Thread.Sleep(1000); // Dummy delay

            // For this example lets respond by echoing received message
            return System.Text.UTF8Encoding.UTF8.GetBytes(message);
        }

    }
}
