using System;
using System.Threading;
using System.Text;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;

namespace nf.RDM6300
{
    public class Program
    {
        static Rdm630 rfid;
        static int cnt = 0;
        public static void Main()
        {

            rfid = new Rdm630("COM6");
            rfid.DataReceived += Rfid_DataReceived;

         
            Thread.Sleep(Timeout.Infinite);
        }

        private static void Rfid_DataReceived(uint data1, uint data2, DateTime time)
        {
           
            Console.WriteLine(rfid.Tag);

        }
    }
}
