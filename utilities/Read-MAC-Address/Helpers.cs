using System;
using System.Net.NetworkInformation;
using System.Text;

namespace Helpers
{
    public static class Helpers
    {
        /// <summary>
        /// Return MAC Address from network interface.
        /// </summary>
        /// <returns>String from "First" Converted Physical Address</returns>
        /// <remarks>Usage: string mac = Utilities.GetMacId();</remarks>
        public static string GetMacId()
        {
            NetworkInterface[] nis = NetworkInterface.GetAllNetworkInterfaces();
            if (nis.Length > 0)
            {
                // get the first interface
                NetworkInterface ni = nis[0];
                return ByteArrayToHex(ni.PhysicalAddress);
            }
            else
            {
                return "000000000000";
            }
        }

        /// <summary>
        /// Return String from byte[].
        /// </summary>
        /// <param name="barray">The value to convert. Supported types are: byte[].</param>
        /// <returns>String from byte array</returns>
        /// <remarks>Usage: string b = Utilities.ByteArrayToHex(new byte[] { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 });.</remarks>

        public static string ByteArrayToHex(byte[] barray)
        {
            string bs = "";
            for (int i = 0; i < barray.Length; ++i)
            {
               bs += barray[i].ToString("X2");
            }
            return bs;
        }
    }
}
