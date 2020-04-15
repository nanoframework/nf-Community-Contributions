using System;
using System.Threading;
using Driver.DS18B20;
using nanoFramework.Devices.OneWire;


namespace OneWire_v3
{
    public class Program
    {
        public static void Main()
        {
            Console.WriteLine("");
            Console.WriteLine("** nanoFramework OneWire ESP32 DEV KITV1 Sample! **");

            OneWireController oneWire = new OneWireController();

            DS18B20 ds18b20 = new DS18B20(oneWire,  /* The 1-wire bus*/
                                    null,           /*Let this driver find out a DS18B20 on the bus*/
                                    true,           /* Multidrop, network*/
                                    3       /* 3 decimal places is enough for us while reading temperature changes*/
                                    );

            string devAddrStr = "";//store the device address as string...

            ds18b20.SetSearchMode = DS18B20.NORMAL;
            if (ds18b20.Initialize())    //Initialize sensors | search for 18B20 devices
            {
                Console.WriteLine("");
                Console.WriteLine("Devices found = " + ds18b20.Found);
                Console.WriteLine("");

                for (int i = 0; i < ds18b20.Found; i++)
                {
                    ds18b20.Address = ds18b20.AddressNet[i];

                    foreach (var addrByte in ds18b20.AddressNet[i]) devAddrStr += addrByte.ToString("X2");
                    Console.WriteLine("18b20-" + i.ToString("X2") + " " + devAddrStr);
                    devAddrStr = "";
                    CheckPowerMode();
                    setAlarmSetPoints(); // Set same setpoint for selected device
                }
                Console.WriteLine("");
                loopReadAll();
                alarmSearch();
            }
            else
            {
                notFound();
            }

            void loopReadAll()
            {
                int loopRead = 10;

                while (loopRead > 0)
                {
                    Console.WriteLine("LoopRead " + loopRead);
                    ds18b20.PrepareToRead(); // Update temp. value in all devices

                    for (int index = 0; index < ds18b20.Found; index++)
                    {
                        //Select the device
                        ds18b20.Address = ds18b20.AddressNet[index];
                        devAddrStr = "";
                        foreach (var addrByte in ds18b20.AddressNet[index]) devAddrStr += addrByte.ToString("X2");

                        //Read Temperature on selected device
                        ds18b20.Read();
                        Console.WriteLine("DS18B20[" + devAddrStr + "] Sensor reading in One-Shot-mode; T = " + ds18b20.TemperatureInCelcius.ToString("f2") + " C"); //"f2" two decimal point format.
                    }

                    Console.WriteLine("");
                    loopRead--;
                }
            }

            // Set alarm setpoint for selected device
            void setAlarmSetPoints()
            {
                //ds18b20.ConfigurationRead(false);
                //Console.WriteLine("Alarm Setpoints before:");
                //Console.WriteLine("Hi alarm = " + ds18b20.TempHiAlarm + " C");
                //Console.WriteLine("Lo alarm = " + ds18b20.TempLoAlarm + " C");
                //Console.WriteLine("");

                ds18b20.TempHiAlarm = 30;
                ds18b20.TempLoAlarm = 25;
                ds18b20.ConfigurationWrite(false); //Write configuration on ScratchPad,
                                                   //If true, save it on EEPROM too.
                ds18b20.ConfigurationRead(true);
                Console.WriteLine("Alarm Setpoints-RecallE2:");
                Console.WriteLine("Hi alarm = " + ds18b20.TempHiAlarm + " C");
                Console.WriteLine("Lo alarm = " + ds18b20.TempLoAlarm + " C");
                Console.WriteLine("");
            }

            void alarmSearch()
            {
                int loopRead = 40;
                ds18b20.SetSearchMode = DS18B20.SEARCH_ALARM;

                while (loopRead > 0)
                {
                    Console.WriteLine("LoopRead " + loopRead);

                    if (ds18b20.SearchForAlarmCondition())
                    {
                        for (int index = 0; index < ds18b20.Found; index++)
                        {
                            //Select the device
                            ds18b20.Address = ds18b20.AddressNet[index];
                            //Read Temperature on selected device
                            ds18b20.Read();

                            devAddrStr = "";
                            foreach (var addrByte in ds18b20.AddressNet[index]) devAddrStr += addrByte.ToString("X2");
                            Console.WriteLine("DS18B20[" + devAddrStr + "] Sensor reading in One-Shot-mode; T = " + ds18b20.TemperatureInCelcius.ToString("f2") + " C");

                            ds18b20.ConfigurationRead(false); //Read alarm setpoint.
                            Console.WriteLine("Alarm Setpoints:");
                            Console.WriteLine("Hi alarm = " + ds18b20.TempHiAlarm + " C");
                            Console.WriteLine("Lo alarm = " + ds18b20.TempLoAlarm + " C");
                            Console.WriteLine("");
                        }
                    }
                    else
                    {
                        Console.WriteLine("***** No devices in alarm ****");
                    }

                    loopRead--;
                }
                Console.WriteLine("");
            }

            void notFound()
            {

                Console.WriteLine("*****************");
                Console.WriteLine("No devices found.");
                Console.WriteLine("*****************");
            };

            void CheckPowerMode()
            {
                if (ds18b20.IsParasitePowered())
                {
                    Console.WriteLine("Parasite powered");
                }
                else
                {
                    Console.WriteLine("External powered");
                }
            }

            #region Other Examples
            //OneWireController oneWire = new OneWireController();

            //DS18B20 ds18b20 = new DS18B20(oneWire,/* The 1-wire bus*/
            //                        null, /*Let this driver find out a DS18B20 on the bus*/
            //                        false, /* single drop, no network*/
            //                        3 /*3 decimal places is enough for us while reading temperature changes*/
            //                        );

            ///*
            // * NOTE: Limiting to the decimal places will not work when you do a "ToString" on floats.
            // * The limit to decimal places is only for comparison. For exmaple, if last measured temperature value
            // * was 25.3343567 and the next value is 25.3343667, then the difference between the two is about 0.00001.
            // * If we limit to 3 decimal places, then the values are read as 25.334 and 25.334, resulting in a difference 
            // * of zero. This is used to compute if sensors changed or not...more the number of decimal places, higher
            // * is the change event possibility (because even a very small change will be registered)
            // */

            //int loopCount = 3; //used later to limit test duration
            //string devAddrStr = "";//store the device address as string...

            ///*********************************************************************************************************
            // * This driver supports, one-shot , poll mode (meaning,you check the sensor for changes 
            // * in temperature values) and event mode (meaning, the driver will alert you when 
            // * temperature changes)
            // *********************************************************************************************************/
            ////One-Shot-mode example...
            //ds18b20.Initialize(); //Initialize sensor
            ///*After device gets initialized and if initialization is successful, the class DS18B20 should have an address*/
            //if (ds18b20.Address != null && ds18b20.Address.Length == 8 && ds18b20.Address[0] == DS18B20.FAMILY_CODE)
            //{
            //    //Initialization successful...let's try to read the address
            //    /*
            //     * Since this class was initialized without an address, the Initialize() method will search for valid 
            //     * devices on the bus, and select the first device of type DS18B20 on the bus. If you have multiple devices,
            //     * You can use the OneWireController class's "Find" methods to first search for devices, and then initialize
            //     * the class with an address.
            //     */
            //    foreach (var addrByte in ds18b20.Address) devAddrStr += addrByte.ToString("X2");

            //    ds18b20.PrepareToRead();
            //    ds18b20.ConfigurationRead();
            //    Console.WriteLine("Resolution = " + ds18b20.Resolution);
            //    Console.WriteLine("Temperute Hi alarm =" + ds18b20.TempHiAlarm + " C");
            //    Console.WriteLine("Temperute Lo alarm =" + ds18b20.TempLoAlarm + " C");

            //    ds18b20.Resolution = -4;
            //    ds18b20.TempHiAlarm = -10;
            //    ds18b20.TempLoAlarm = 10;
            //    ds18b20.ConfigurationWrite();
            //    ds18b20.PrepareToRead();
            //    ds18b20.ConfigurationRead();
            //    Console.WriteLine("New Resolution = " + ds18b20.Resolution);
            //    Console.WriteLine("New Temperute Hi alarm = " + ds18b20.TempHiAlarm + " C");
            //    Console.WriteLine("New Temperute Lo alarm = " + ds18b20.TempLoAlarm + " C");

            //    int loopRead = 20;

            //    while (loopRead > 0)
            //    {
            //        ds18b20.PrepareToRead();
            //        ds18b20.Read();
            //        Console.WriteLine("DS18B20[" + devAddrStr + "] Sensor reading in One-Shot-mode; T=" + ds18b20.TemperatureInCelcius.ToString() + " C"); //"f2" two decimal point format.
            //        Thread.Sleep(4000);
            //        loopRead--;
            //    }
            //}

            ///*Polled example*/
            //loopCount = 3;
            //ds18b20.Reset();
            //ds18b20.Initialize();//after this device should have valid address...see above on how to check

            //while (loopCount > 0)
            //{
            //    if (ds18b20.HasSensorValueChanged())
            //    {
            //        //no need to read again (like HTU21D)
            //        Console.WriteLine("DS18B20[" + devAddrStr + "] in Poll-mode;T=" + ds18b20.TemperatureInCelcius.ToString());
            //    }
            //    loopCount--;
            //}

            ///*Event mode...*/
            //loopCount = 3;
            //ds18b20.Reset();
            //ds18b20.Initialize(); //again, if initialization is successful, object will have valid address (see above)
            //if (ds18b20.CanTrackChanges())
            //{
            //    ds18b20.SensorValueChanged += () =>
            //    {
            //        Console.WriteLine("DS18B20 (" + devAddrStr + ") in Event-mode;T=" + ds18b20.TemperatureInCelcius.ToString());
            //    };
            //    ds18b20.BeginTrackChanges(2000/*track changes every 2 seconds*/);
            //    while (loopCount > 0)
            //    {
            //        Thread.Sleep(3000);//Wait for a change...
            //        loopCount--;
            //    }
            //    ds18b20.EndTrackChanges();
            //}
            //ds18b20.Dispose();
            #endregion
        }

    }
}
