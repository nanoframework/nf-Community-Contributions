### MAC Address


### How to use Helpers Program.cs
```csharp
public class Program
{
    private const string c_SSID = "xxxxxxxxx";
    private const string c_AP_PASSWORD = "!xxxxxxxx";
    public static void Main()
    {
        // Wait for network to connect
        SetupAndConnectNetwork();

        //We should have a valid betwork interface now
        var mac = Utilities.GetMacId();
        
        Thread.Sleep(Timeout.Infinite);
    }

    public static void SetupAndConnectNetwork()
    {
        NetworkInterface[] nis = NetworkInterface.GetAllNetworkInterfaces();
        if (nis.Length > 0)
        {
            // get the first interface
            NetworkInterface ni = nis[0];

            if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
            {
                // network interface is Wi-Fi
                Debug.WriteLine("Network connection is: Wi-Fi");

                Wireless80211Configuration wc = Wireless80211Configuration.GetAllWireless80211Configurations()[ni.SpecificConfigId];
                if (wc.Ssid != c_SSID && wc.Password != c_AP_PASSWORD)
                {
                    // have to update Wi-Fi configuration
                    wc.Ssid = c_SSID;
                    wc.Password = c_AP_PASSWORD;
                    wc.SaveConfiguration();
                }
                else
                {   // Wi-Fi configuration matches
                }
            }
            else
            {
                // network interface is Ethernet
                Debug.WriteLine("Network connection is: Ethernet");

                ni.EnableDhcp();
            }

            // wait for DHCP to complete
            WaitIP();
        }
        else
        {
            throw new NotSupportedException("ERROR: there is no network interface configured.\r\nOpen the 'Edit Network Configuration' in Device Explorer and configure one.");
        }
    }

    static void WaitIP()
    {
        Debug.WriteLine("Waiting for IP...");

        while (true)
        {
            NetworkInterface ni = NetworkInterface.GetAllNetworkInterfaces()[0];
            if (ni.IPv4Address != null && ni.IPv4Address.Length > 0)
            {
                if (ni.IPv4Address[0] != '0')
                {
                    Debug.WriteLine($"We have an IP: {ni.IPv4Address}");
                    break;
                }
            }

            Thread.Sleep(500);
        }
    }
}

```

### Helpers.cs
```csharp
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
        /// <remarks>Usage: string b = Utilities.ByteArrayToHex(new byte[] { 0x20, 0x20, 0x20, 0x20 });.</remarks>

        public static string ByteArrayToHex(byte[] barray)
        {
            char[] c = new char[barray.Length * 2];
            byte b;
            for (int i = 0; i < barray.Length; ++i)
            {
                b = ((byte)(barray[i] >> 4));
                c[i * 2] = (char)(b > 9 ? b + 0x37 : b + 0x30);
                b = ((byte)(barray[i] & 0xF));
                c[i * 2 + 1] = (char)(b > 9 ? b + 0x37 : b + 0x30);
            }
            return new string(c).ToLower();
        }
    }
}
```
