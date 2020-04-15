/**
 * Original source code:https://github.com/sharmavishnu/nf-companion/tree/master/nf-companion-lib-drivers-sensors
 * Modified by
 * Ricardo Santos -  @ Discord nanoframework UpStream#2439 https://github.com/up-streamer
 * 
 * Added ability to work with more than one device on 1-Wire bus.
 * Modify conversion Calculation
 * Added resolution modification
 * Added ability to read/write alarm set point.
 * Added ability to work on network (multidrop mode)
 * Added searching solution when triggering alarm setpoint.
 * Now possible to check 18B20 power mode.
 * Simplified driver structre to a single file.
 * 
 */

using System;
using System.Collections;
using System.Threading;
using nanoFramework.Devices.OneWire;

namespace Driver.DS18B20
{
    /// <summary>
    /// Driver for DS18B20 temperature sensor. At the time of writing this code, more details about
    /// this sensor can be found at https://datasheets.maximintegrated.com/en/ds/DS18B20.pdf
    /// </summary>
    public class DS18B20
    {
        #region Implementation
        /// <summary>
        /// The underlying One Wire device
        /// </summary>
        private OneWireController _oneWire = null;
        /// <summary>
        /// Is this sensor tracking changes
        /// </summary>
        protected bool _isTrackingChanges = false;
        /// <summary>
        /// The thread that keeps a track of sensor value change
        /// </summary>
        protected Thread _changeTracker = null;
        #endregion

        #region Events/Delegates
        /// <summary>
        /// Delegate that defines method signature that will be called
        /// when sensor value change event happens
        /// </summary>
        public delegate void OnSensorChanged();
        /// <summary>
        /// Event that is called when the sensor value changes
        /// </summary>
        public event OnSensorChanged SensorValueChanged;
        #endregion

        #region Constants
        /// <summary>
        /// Command to soft reset the HTU21D sensor
        /// </summary>
        public static readonly byte FAMILY_CODE = 0x28;
        /// <summary>
        /// Command to address specific device on network
        /// </summary>
        public static readonly byte MATCH_ROM = 0x55;
        /// <summary>
        /// Command to address all devices on the bus simultaneously
        /// </summary>
        public static readonly byte SKIP_ROM = 0xCC;
        /// <summary>
        /// Set search mode to normal
        /// </summary>
        public const bool NORMAL = false;
        /// <summary>
        /// Set search mode to search alarm
        /// </summary>
        public const bool SEARCH_ALARM = true;
        /// <summary>
        /// Command to trigger a temperature conversion
        /// </summary>
        private readonly byte CONVERT_TEMPERATURE = 0x44;
        /// <summary>
        /// Command copy scratchpad registers to EEPROM
        /// </summary>
        private readonly byte COPY_SCRATCHPAD = 0x48;
        /// <summary>
        /// Recalls the alarm trigger values and configuration
        /// from EEPROM to scratchpad registers
        /// </summary>
        private readonly byte RECALL_E2 = 0xB8;
        /// <summary>
        /// Command to write to scratchpad registers
        /// </summary>
        private readonly byte WRITE_SCRATCHPAD = 0x4E;
        /// <summary>
        /// Command to read scratchpad registers
        /// </summary>
        private readonly byte READ_SCRATCHPAD = 0xBE;
        /// <summary>
        /// Check if any DS18B20s on the bus are using parasite power
        /// Return false for parasite power, true for external power
        /// </summary>
        private readonly byte READ_POWER_SUPPLY = 0xB8;
        /// <summary>
        /// Error value of temperature
        /// </summary>
        private const float ERROR_TEMPERATURE = -999.99F;
        #endregion

        #region Properties
        /// <summary>
        /// The 8-byte address of selected device 
        /// (since there could be more than one such devices on the bus)
        /// </summary>
        public byte[] Address { get; set; }
        /// <summary>
        /// Contains an array of address of all 18B20 devices on network or only
        /// devices in alarm if mode is set to SEARCH_ALARM 
        /// </summary>
        public byte[][] AddressNet { get; private set; }
        /// <summary>
        /// Set to true if more than one device connected ie network.
        /// </summary>
        public bool Multidrop { get; set; }
        /// <summary>
        /// Total number of 18B20 devices on network.
        /// </summary>
        public int Found;
        /// <summary>
        /// Accessor/Mutator for temperature in celcius
        /// </summary>
        public float TemperatureInCelcius { get; private set; }
        /// <summary>
        /// Accessor/Mutator for Sensor resolution
        /// R1=0,R0=0=>0 -> 9bit 
        /// R1=0,R0=1=>1 -> 10bit 
        /// R1=1,R0=0=>2 -> 11bit 
        /// R1=1,R0=1=>3 -> 12bit (default on power up) 
        /// </summary>
        private int resolution;
        public int Resolution
        {
            get { return resolution; }
            set
            {
                resolution = value < 0 ? 0 : value > 3 ? 3 : value;
            }

        }
        /// <summary>
        /// Accessor/Mutator for Alarm Hi register in celcius
        /// Min -55, Max 125
        /// </summary>
        private sbyte tempHiAlarm;
        public sbyte TempHiAlarm
        {
            get { return tempHiAlarm; }
            set
            {
                tempHiAlarm = value;
                if (value < -55) { tempHiAlarm = -55; }
                if (value > 125) { tempHiAlarm = 125; }
            }
        }
        /// <summary>
        /// Accessor/Mutator for Alarm Lo register in celcius
        /// Min -55, Max 125
        /// </summary>
        private sbyte tempLoAlarm;
        public sbyte TempLoAlarm
        {
            get { return tempLoAlarm; }
            set
            {
                tempLoAlarm = value;
                if (value < -55) { tempLoAlarm = -55; }
                if (value > 125) { tempLoAlarm = 125; }
            }
        }
        /// <summary>
        /// Set search mode to normal
        /// or only devices in alarm
        /// </summary>
        private bool searchMode;
        public bool SetSearchMode
        {
            set { searchMode = value; }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="owBus">Which one wire controller (logical bus) to use</param>
        /// <param name="deviceAddr">The device address (if null, then this device will search for one on the bus and latch on to the first one found)</param>
        /// <param name="Multidrop"> True for more than one sensor</param>
        /// <param name="Resolution">Sensor resolution</param>
        public DS18B20(OneWireController owBus, byte[] deviceAddr = null, bool multidrop = false, int resolution = 3)
        {
            _oneWire = owBus;
            Multidrop = multidrop;
            Resolution = resolution;

            if (deviceAddr != null)
            {
                if (deviceAddr.Length != 8) throw new ArgumentException();//must be 8 bytes
                if (deviceAddr[0] != FAMILY_CODE) throw new ArgumentException();//invalid family code
                Address = deviceAddr;
            }
            TemperatureInCelcius = ERROR_TEMPERATURE;
            TempHiAlarm = 30; // Set default alarm values
            TempLoAlarm = 20;
        }
        #endregion

        #region IDisposable Support

        /// <summary>
        /// Dispose this object
        /// </summary>
        void DisposeSensor()
        {
            Address = null;
        }
        #endregion

        #region Core Methods
        /// <summary>
        /// Initialize the sensor. This step will perform a reset of the 1-wire bus.
        /// It will check for existence of a 1-wire device. If no address was provided, then the
        /// 1-wire bus will be searched and the first device that matches the family code will be latched on to.
        /// Developer should check for successful initialization by checking the value returned. 
        /// It must be bigger than 0.
        /// If in Multidrop mode will keep seaching until find last device, saving all in AddressNet array.
        /// </summary>
        public bool Initialize()
        {
            Found = 0;
            //ArrayList allDevices;
            ArrayList allDevices = new ArrayList();

            _oneWire.TouchReset();

            if (Address == null) //search for a device with the required family code
            {
                //found the device
                if (Multidrop)
                {
                    if (_oneWire.FindFirstDevice(false, searchMode))
                    {
                        do
                        {
                            if (_oneWire.SerialNumber[0] == FAMILY_CODE)
                            {
                                _oneWire.TouchReset();
                                Address = new byte[_oneWire.SerialNumber.Length];
                                Array.Copy(_oneWire.SerialNumber, Address, _oneWire.SerialNumber.Length);
                                Found++;
                                allDevices.Add(Address);
                                //if (Found == 6) { break; } //Temp fix during test endless loop
                            }
                        } while (_oneWire.FindNextDevice(false, searchMode));//keep searching until we get one
                    }

                    if (Found > 0)
                    {
                        AddressNet = new byte[Found][];
                        int i = 0;
                        foreach (byte[] device in allDevices)
                        {
                            AddressNet[i] = new byte[device.Length];
                            Array.Copy(device, AddressNet[i], device.Length);
                            i++;
                        }
                        allDevices = null;
                    }
                }
                else
                {
                    if (_oneWire.FindFirstDevice(true, searchMode))
                    {
                        do
                        {
                            if (_oneWire.SerialNumber[0] == FAMILY_CODE)
                            {
                                Address = new byte[_oneWire.SerialNumber.Length];
                                Array.Copy(_oneWire.SerialNumber, Address, _oneWire.SerialNumber.Length);
                                Found = 1;
                                break;
                            }
                        } while (_oneWire.FindNextDevice(true, searchMode));//keep searching until we get one
                    }
                }
            }
            if (Found > 0) { return true; };
            return false;
        }

        private void SelectDevice()
        {
            if (Address != null && Address.Length == 8 && Address[0] == FAMILY_CODE)
            {
                //now write command and ROM at once
                byte[] cmdAndData = new byte[9] {
                   MATCH_ROM, //Address specific device command
                   Address[0],Address[1],Address[2],Address[3],Address[4],Address[5],Address[6],Address[7] //do not convert to a for..loop
               };

                _oneWire.TouchReset();
                foreach (var b in cmdAndData) _oneWire.WriteByte(b);
            }
        }

        private void Convert_T()
        {
            _oneWire.TouchReset();
            //first address all devices
            _oneWire.WriteByte(SKIP_ROM);//Skip ROM command
            _oneWire.WriteByte(CONVERT_TEMPERATURE);//convert temperature
                                                    // According datasheet. Less resolution needs less time to complete.
            int waitConversion = 1000;
            switch (Resolution)
            {
                case 0:
                    waitConversion = 125;
                    break;
                case 1:
                    waitConversion = 250;
                    break;
                case 2:
                    waitConversion = 500;
                    break;
            }
            Thread.Sleep(waitConversion); //Wait for conversion (in default 12-bit resolution mode, 1000ms) 
        }

        /// <summary>
        /// Prepare sensor to read the data
        /// </summary>
        public void PrepareToRead()
        {
            if ((Address != null || Found != 0) && Address.Length == 8 && Address[0] == FAMILY_CODE)
            {
                Convert_T();
            }
        }

        /// <summary>
        /// Read sensor data
        /// </summary>
        /// <returns>true on success, else false</returns>
        public bool Read()
        {
            SelectDevice();

            //now read the scratchpad
            var verify = _oneWire.WriteByte(READ_SCRATCHPAD);

            //Now read the temperature
            var tempLo = _oneWire.ReadByte();
            var tempHi = _oneWire.ReadByte();

            if (_oneWire.TouchReset())
            {
                var temp = ((tempHi << 8) | tempLo);

                // Bits manipulation to represent negative values correctly.
                if ((tempHi >> 7) == 1)
                {
                    temp = (temp | unchecked((int)0xffff0000));
                }

                TemperatureInCelcius = ((float)temp) / 16;
                return true;
            }
            else
            {
                TemperatureInCelcius = ERROR_TEMPERATURE;
                return false;
            }
        }

        /// <summary>
        /// Reset the sensor...this performs a soft reset. To perform a hard reset, the system must be 
        /// power cycled
        /// </summary>
        public void Reset()
        {
            _oneWire.TouchReset();
            TemperatureInCelcius = ERROR_TEMPERATURE;
        }
        #endregion

        /// <summary>
        /// Search for alarm condition.
        /// Save in AddressNet the list of devices
        /// under alarm condition.
        /// </summary>
        /// <returns>bool</returns>
        public bool SearchForAlarmCondition()
        {
            Address = null;

            Convert_T();
            if (Initialize()) { return true; }
            return false;
        }

        #region Configuration Methods
        /// <summary>
        /// Read sensor Configuration and
        /// Write on Resolution, TempHiAlarm and TempLoAlarm properties.
        /// Returns false if error during reading sensor.
        /// Write 0xEE (238) to a property if
        /// error during property handle.
        /// </summary>
        public bool ConfigurationRead(bool recall = false)
        {
            var verify = 0;

            // Restore Register from EEPROM
            if (recall == true)
            {
                SelectDevice();
                verify = _oneWire.WriteByte(RECALL_E2);
                while (_oneWire.ReadByte() == 0) { Thread.Sleep(10); }
            }

            // Now read the scratchpad
            SelectDevice();
            verify = _oneWire.WriteByte(READ_SCRATCHPAD);

            // Discard temperature bytes
            _oneWire.ReadByte();
            _oneWire.ReadByte();

            TempHiAlarm = (sbyte)_oneWire.ReadByte();
            TempLoAlarm = (sbyte)_oneWire.ReadByte();
            int configReg = _oneWire.ReadByte();

            if (_oneWire.TouchReset())
            {
                Resolution = (configReg >> 5);
            }
            else
            {
                Resolution = 0xEE;
                return false;
            };

            return true;
        }

        /// <summary>
        /// Write sensor Configuration
        /// from tempHiAlarm, tempLoAlarm and
        /// resolution.
        /// The unchanged registers will be overwritten.
        /// </summary>
        public bool ConfigurationWrite(bool save = false)
        {

            SelectDevice();

            //now write the scratchpad
            var verify = _oneWire.WriteByte(WRITE_SCRATCHPAD);

            _oneWire.WriteByte((byte)tempHiAlarm);
            _oneWire.WriteByte((byte)tempLoAlarm);
            _oneWire.WriteByte((byte)(resolution << 5));

            // Save confuguration on device's EEPROM
            if (save)
            {
                SelectDevice();
                verify = _oneWire.WriteByte(COPY_SCRATCHPAD);
                Thread.Sleep(10);
            };
            return true;
        }

        public bool IsParasitePowered()
        {
            SelectDevice();

            // Now read power supply external | parasite
            var verify = _oneWire.WriteByte(READ_POWER_SUPPLY);

            if (_oneWire.ReadByte() == 0x00) { return true; } else { return false; }
        }
        #endregion

        #region Change tracking
        /// <summary>
        /// This sensor suports change tracking
        /// </summary>
        /// <returns>bool</returns>
        public bool CanTrackChanges()
        {
            return true;
        }

        /// <summary>
        /// Let the world know whether the sensor value has changed or not
        /// </summary>
        /// <returns>bool</returns>
        bool HasSensorValueChanged()
        {
            float previousTemperature = TemperatureInCelcius;

            PrepareToRead();
            Read();

            float currentTemperature = TemperatureInCelcius;

            bool valuesChanged = (previousTemperature != currentTemperature);

            return valuesChanged;
        }

        /// <summary>
        /// Start to track the changes
        /// </summary>
        /// <param name="ms">Interval in milliseconds to track the changes to sensor values</param>
        public virtual void BeginTrackChanges(int ms)
        {
            if (_isTrackingChanges) throw new InvalidOperationException("Already tracking changes");
            if (ms < 50) throw new ArgumentOutOfRangeException("ms", "Minimum interval to track sensor changes is 50 milliseconds");
            if (SensorValueChanged == null) throw new NotSupportedException("Tracking not supported if SensorValueChanged event is not defined");

            _changeTracker = new Thread(() => {
                int divs = (int)(ms / 1000);

                while (_isTrackingChanges)
                {
                    if (ms > 1000)
                    {
                        divs = (int)(ms / 1000);
                        while (_isTrackingChanges && divs > 0)
                        {
                            Thread.Sleep(1000);
                            divs--;
                        }
                    }
                    else
                        Thread.Sleep(ms);
                    //now check for change
                    if (HasSensorValueChanged() && SensorValueChanged != null)
                    {
                        try { SensorValueChanged(); } catch {; ; /*do nothing..upto event handler to decide what to do*/ }
                    }
                }

            });
            _isTrackingChanges = true;
            _changeTracker.Start();
        }

        /// <summary>
        /// Stop tracking changes
        /// </summary>
        public virtual void EndTrackChanges()
        {
            _isTrackingChanges = false;
            Thread.Sleep(3000);//see BeginChangeTracker to know why 3000 is chosen...3x of lowest wait time
            if (_changeTracker.IsAlive)
            {
                //force kill
                try { _changeTracker.Abort(); } finally { _changeTracker = null; }
            }
        }

        #endregion
    }
}
