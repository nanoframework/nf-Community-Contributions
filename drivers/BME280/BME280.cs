/*
 * BME280 driver for TinyCLR 2.0 
 * 
 * Copyright 2020 Christophe Gerbier, Stephen Cardinale and MikroBus.Net
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
 * http://www.apache.org/licenses/LICENSE-2.0
 * Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, 
 * either express or implied. See the License for the specific language governing permissions and limitations under the License.
 * 
 */

#region Usings

#if (NANOFRAMEWORK_1_0)
using System.Device.I2c;
#else
using GHIElectronics.TinyCLR.Devices.I2c;
#endif

using System;
using System.Threading;

#endregion

namespace MBN.Modules
{
    /// <inheritdoc cref="IPressure" />
    /// <inheritdoc cref="ITemperature" />
    /// <inheritdoc cref="IHumidity" />
    /// <summary>
    ///  Main class for the BME280 driver
    ///  <para><b>Pins used :</b> Scl, Sda</para>
    /// </summary>
    /// <example>
    /// Example usage:
    /// <code language="C#">
    /// using MBN;
    /// using MBN.Modules;
    ///
    /// using System;
    /// using System.Diagnostics;
    /// using System.Threading;
    ///
    /// namespace Examples
    /// {
    ///     internal class Program
    ///     {
    ///         private static BME280 _sensor;
    ///
    ///         private static void Main()
    ///         {
    ///             _sensor = new BME280(1, BME280.I2CAddresses.Address0)
    ///             {
    ///                 TemperatureUnit = TemperatureUnits.Fahrenheit,
    ///                 PressureCompensation = PressureCompensationModes.SeaLevelCompensated
    ///             };
    ///
    ///             _sensor.SetRecommendedMode(BME280.RecommendedModes.WeatherMonitoring);
    ///
    ///             while (true)
    ///             {
    ///                 Debug.WriteLine("------Reading individual values-------");
    ///
    ///                 Debug.WriteLine($"Pressure.......: {_sensor.ReadPressure():F1} hPa");
    ///                 Debug.WriteLine($"Temperature....: {_sensor.ReadTemperature():F2} °F");
    ///                 Debug.WriteLine($"Humidity.......: {_sensor.ReadHumidity():F2} %RH");
    ///                 Debug.WriteLine($"Altitude.......: {_sensor.ReadAltitude():F0} meters\n");
    ///
    ///                 _sensor.ReadSensor(out Single pressure, out Single temperature, out Single humidity, out Single altitude);
    ///
    ///                 Debug.WriteLine("------Using the ReadSensor Method-------");
    ///                 Debug.WriteLine($"Pressure.......: {pressure:F1} hPa");
    ///                 Debug.WriteLine($"Temperature....: {temperature:F2} °F");
    ///                 Debug.WriteLine($"Humidity.......: {humidity:F2} %RH");
    ///                 Debug.WriteLine($"Altitude.......: {altitude:F0} meters\n");
    ///
    ///                 Thread.Sleep(5000);
    ///             }
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    public sealed class BME280 : IPressure, ITemperature, IHumidity
    {
        #region CTOR

        /// <summary>
        ///     Initializes a new instance of the <see cref="BME280" /> class.
        /// </summary>
        /// <param name="socket">The socket on which the WeatherClick module is plugged on MikroBus.Net board</param>
        /// <param name="slaveAddress">The address of the module.</param>
        public BME280(int i2cBus, I2CAddresses slaveAddress)
        {
            //_socket = socket;
#if (NANOFRAMEWORK_1_0)
            _sensor = I2cDevice.Create(new I2cConnectionSettings(i2cBus, (int)slaveAddress, I2cBusSpeed.StandardMode));
#else
            _sensor = I2cController.FromName(socket.I2cBus).GetDevice(new I2cConnectionSettings((Int32) slaveAddress, 100000));
#endif

            Reset(ResetModes.Soft);

            if (DeviceId != 0x60) throw new DeviceInitialisationException();

            SetRecommendedMode(RecommendedModes .WeatherMonitoring); // Minimal consumption at startup (other than PowerModes.Off, of course)

            ReadCalibrationData();
        }

        #endregion

        #region Constants

        private const Byte REG_ID = 0xD0;
        private const Byte REG_RESET = 0xE0;
        private const Byte REG_CTRL_HUM = 0xF2;
        private const Byte REG_STATUS = 0xF3;
        private const Byte REG_CTRL_MEAS = 0xF4;
        private const Byte REG_CONFIG = 0xF5;
        private const Byte REG_READ_PRESSURE = 0xF7;
        private const Byte READ_TEMPERATURE = 0xFA;
        private const Byte READ_HUMIDITY = 0xFD;

        private const Byte DIG_T1_LSB_REG = 0x88;
        private const Byte DIG_H1_REG = 0xA1;
        private const Byte DIG_H2_LSB_REG = 0xE1;

        #endregion

        #region ENUMS

        /// <summary>
        ///     Oversampling rates
        /// </summary>
        public enum OversamplingRates : byte
        {
            /// <summary>
            ///     No oversampling, output is set to 0x8000
            /// </summary>
            Skipped = 0,

            /// <summary>
            ///     Oversampling x 1
            /// </summary>
            Osr1 = 1,

            /// <summary>
            ///     Oversampling x 2
            /// </summary>
            Osr2 = 2,

            /// <summary>
            ///     Oversampling x 4
            /// </summary>
            Osr4 = 3,

            /// <summary>
            ///     Oversampling x 8
            /// </summary>
            Osr8 = 4,

            /// <summary>
            ///     Oversampling x 16
            /// </summary>
            Osr16 = 5
        }

        /// <summary>
        ///     Infinite Impulse Response Filter Coefficient.
        /// </summary>
        public enum FilterCoefficient : byte
        {
            /// <summary>
            ///     IIR Filter is off.
            /// </summary>
            IIROff = 0x00,

            /// <summary>
            ///     IIR Filter is 2.
            /// </summary>
            IIR2 = 0x01,

            /// <summary>
            ///     IIR Filter is 4.
            /// </summary>
            IIR4 = 0x02,

            /// <summary>
            ///     IIR Filter is 8.
            /// </summary>
            IIR8 = 0x03,

            /// <summary>
            ///     IIR Filter is 16.
            /// </summary>
            IIR16 = 0x04
        }

        /// <summary>
        ///     Inactive duration in normal mode
        /// </summary>
        public enum InactiveDuration : byte
        {
            /// <summary>
            ///     Standby time of 0.5 milliseconds between readings.
            /// </summary>
            MS_0_5 = 0x00,

            /// <summary>
            ///     Standby time of 62.5 milliseconds between readings.
            /// </summary>
            MS_62_5 = 0x01,

            /// <summary>
            ///     Standby time of 125 milliseconds between readings.
            /// </summary>
            MS_125 = 0x02,

            /// <summary>
            ///     Standby time of 250 milliseconds between readings.
            /// </summary>
            MS_250 = 0x03,

            /// <summary>
            ///     Standby time of 500 milliseconds between readings.
            /// </summary>
            MS_500 = 0x04,

            /// <summary>
            ///     Standby time of 1000 milliseconds between readings.
            /// </summary>
            MS_1000 = 0x05,

            /// <summary>
            ///     Standby time of 10 milliseconds between readings.
            /// </summary>
            MS_10 = 0x06,

            /// <summary>
            ///     Standby time of 20 milliseconds between readings.
            /// </summary>
            MS_20 = 0x07
        }

        /// <summary>
        ///     Preconfigured operating modes, recommended by the Bosch.
        /// </summary>
        public enum RecommendedModes
        {
            /// <summary>
            ///     Only a very low data rate is needed. Power consumption is minimal. Noise of pressure values is of no concern.
            ///     Humidity, pressure and temperature are monitored.
            ///     <para>
            ///         Current consumption 0.16 µA
            ///         RMS Noise 3.3 Pa / 30 cm, 0.07 %RH
            ///         Data output rate 1/60 Hz
            ///     </para>
            /// </summary>
            WeatherMonitoring,

            /// <summary>
            ///     A low data rate is needed. Power consumption is minimal. Forced mode is used to minimize power consumption and to
            ///     synchronize readout, but using normal mode would also be possible.
            ///     <para>
            ///         Current consumption 2.9 µA
            ///         RMS Noise 0.07 %RH
            ///         Data output rate 1/60 Hz
            ///     </para>
            /// </summary>
            HumiditySensing,

            /// <summary>
            ///     Lowest possible altitude noise is needed. A very low bandwidth is preferred. Increased power consumption  is
            ///     tolerated.  Humidity  is  measured  to  help  detect  room  changes.
            ///     <para>
            ///         Current consumption  633 µA
            ///         RMS Noise  0.2 Pa / 1.7 cm
            ///         Data output rate  25Hz
            ///         Filter bandwidth  0.53 Hz
            ///         Response time (75%)  0.9 s
            ///     </para>
            /// </summary>
            IndoorNavigation,

            /// <summary>
            ///     Low altitude noise is needed. The required bandwidth is ~2 Hz in order to respond quickly to altitude  changes
            ///     (e.g.  be  able  to  dodge  a  flying  monster  in  a  game).
            ///     Increased  power consumption is tolerated. Humidity sensor is disabled.
            ///     <para>
            ///         Current consumption  581 µA
            ///         RMS Noise  0.3 Pa / 2.5 cm
            ///         Data output rate  83 Hz
            ///         Filter bandwidth  1.75 Hz
            ///         Response time (75%)  0.3 s
            ///     </para>
            /// </summary>
            Gaming,

            /// <summary>
            ///     Maximal power consumption
            /// </summary>
            FullPower
        }

        /// <summary>
        ///     Various I2C address that the Weather click supports.
        /// </summary>
        public enum I2CAddresses
        {
            /// <summary>
            ///     I2C Address id 0x76 with I2C Address jumper soldered to position Zero (0).
            /// </summary>
            Address0 = 0x76,

            /// <summary>
            ///     I2C Address id 0x77 with I2C Address jumper soldered to position One (1).
            /// </summary>
            Address1 = 0x77
        }

        #endregion

        #region Fields

        private readonly I2cDevice _sensor;
     //   private readonly Hardware.Socket _socket;
        private OversamplingRates _humiditySamplingRate = OversamplingRates.Osr1;
        private OversamplingRates _temperatureSamplingRate = OversamplingRates.Osr1;
        private OversamplingRates _pressureSamplingRate = OversamplingRates.Osr1;
        private PowerModes _powerMode = PowerModes.On;
        private InactiveDuration _standbyDuration;
        private FilterCoefficient _filter;
        private Int32 _pwr;
        private Object LockI2c = new Object();

        #region Calibration Fields

        private UInt16 _digT1;
        private Int16 _digT2, _digT3;
        private UInt16 _digP1;
        private Int16 _digP2, _digP3, _digP4, _digP5, _digP6, _digP7, _digP8, _digP9;
        private Byte _digH1, _digH3, _digH6;
        private Int16 _digH2, _digH4, _digH5;

        #endregion

        #endregion

        #region Public Properties

        /// <summary>
        ///     Controls the time constant of the IIR filter.
        ///     <p>
        ///         Although this property is exposed, it is recommended against setting this property directly. Use the
        ///         <see cref="SetRecommendedMode" /> method to avoid improper settings.
        ///     </p>
        /// </summary>
        /// <value>
        ///     The Filter Coefficient. See data sheet for the values associated to this coefficient.
        /// </value>
        /// <example>
        ///     Example usage:
        ///     <code language="C#">
        /// _weather.Filter = WeatherClick.FilterCoefficient.IIR16;
        /// </code>
        /// </example>
        public FilterCoefficient Filter
        {
            get => _filter;
            set
            {
                if ((Byte) value > 4) value = FilterCoefficient.IIR16;
                WriteByte(REG_CONFIG, (Byte) (((Byte) _standbyDuration << 5) | ((Byte) value << 2)));
                _filter = value;
            }
        }

        /// <summary>
        ///     Gets or sets the inactive duration in normal mode.
        ///     <p>
        ///         Although this property is exposed, it is recommended against setting this property directly. Use the
        ///         <see cref="SetRecommendedMode" /> method to avoid improper settings.
        ///     </p>
        /// </summary>
        /// <value>
        ///     The duration. See data sheet for the values (in ms) associated to this parameter
        /// </value>
        /// <example>
        ///     Example usage:
        ///     <code language="C#">
        /// _weather.StandbyDuration = WeatherClick.InactiveDuration.MS_0_5;
        /// </code>
        /// </example>
        public InactiveDuration StandbyDuration
        {
            get => _standbyDuration;
            set
            {
                if ((Byte) value > 7) value = InactiveDuration.MS_20;
                WriteByte(REG_CONFIG, (Byte) (((Byte) value << 5) + ((Byte) _filter << 2)));
                _standbyDuration = value;
            }
        }

        /// <summary>
        ///     Gets or sets the humidity sampling rate.
        ///     <p>
        ///         Although this property is exposed, it is recommended against setting this property directly. Use the
        ///         <see cref="SetRecommendedMode" /> method to avoid improper settings.
        ///     </p>
        /// </summary>
        /// <value>
        ///     The humidity sampling rate. See the <seealso cref="OversamplingRates" /> for oversampling rates.
        /// </value>
        /// <example>
        ///     Example usage:
        ///     <code language="C#">
        /// _weather.HumiditySamplingRate = WeatherClick.OversamplingRates.Osr1;
        /// </code>
        /// </example>
        public OversamplingRates HumiditySamplingRate
        {
            get => _humiditySamplingRate;
            set
            {
                _humiditySamplingRate = value;
                WriteByte(REG_CTRL_HUM, (Byte) value);
                SetCTRL_MEAS();
            }
        }

        /// <summary>
        ///     Gets or sets the temperature sampling rate.
        ///     <p>
        ///         Although this property is exposed, it is recommended against setting this property directly. Use the
        ///         <see cref="SetRecommendedMode" /> method to avoid improper settings.
        ///     </p>
        /// </summary>
        /// <value>
        ///     The temperature sampling rate. See the <seealso cref="OversamplingRates" /> for oversampling rates.
        /// </value>
        /// <example>
        ///     Example usage:
        ///     <code language="C#">
        /// _weather.TemperatureSamplingRate = WeatherClick.OversamplingRates.Osr1;
        /// </code>
        /// </example>
        public OversamplingRates TemperatureSamplingRate
        {
            get => _temperatureSamplingRate;
            set
            {
                _temperatureSamplingRate = value;
                SetCTRL_MEAS();
            }
        }

        /// <summary>
        ///     Gets or sets the pressure sampling rate.
        ///     <p>
        ///         Although this property is exposed, it is recommended against setting this property directly. Use the
        ///         <see cref="SetRecommendedMode" /> method to avoid improper settings.
        ///     </p>
        /// </summary>
        /// <value>
        ///     The pressure sampling rate. See the <seealso cref="OversamplingRates" /> for oversampling rates.
        /// </value>
        /// <example>
        ///     Example usage:
        ///     <code language="C#">
        /// _weather.PressureSamplingRate = WeatherClick.OversamplingRates.Osr1;
        /// </code>
        /// </example>
        public OversamplingRates PressureSamplingRate
        {
            get => _pressureSamplingRate;
            set
            {
                _pressureSamplingRate = value;
                SetCTRL_MEAS();
            }
        }

        /// <summary>
        ///     Gets the identifier of the chip.
        /// </summary>
        /// <value>
        ///     Should be 0x60. Other value means error.
        /// </value>
        /// <example>
        ///     Example usage:
        ///     <code language="C#">
        /// Debug.Print("Device ID is " + _weather.DeviceId);
        /// </code>
        /// </example>
        public Byte DeviceId => ReadRegister(REG_ID)[0];

        /// <summary>
        ///     Gets or sets the <see cref="TemperatureUnits" /> used for temperature measurements.
        /// </summary>
        /// <example>
        ///     Example usage:
        ///     <code language="C#">
        /// _sensor.TemperatureUnits = TemperatureUnits.Kelvin;
        /// </code>
        /// </example>
        public TemperatureUnits TemperatureUnit { get; set; } = TemperatureUnits.Celsius;

        /// <summary>
        ///     Gets or sets the pressure compensation mode for one-shot pressure measurements.
        /// </summary>
        /// <example>
        ///     Example usage:
        ///     <code language="C#">
        /// _sensor.PressureCompensation = PressureCompensationModes.SeaLevelCompensated;
        /// </code>
        /// </example>
        public PressureCompensationModes PressureCompensation { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Sets operating mode according to Bosch's recommended modes of operation.
        /// </summary>
        /// <param name="mode">The desired mode.</param>
        /// <example>
        ///     Example usage:
        ///     <code language="C#">
        /// _weather.SetRecommendedMode(WeatherClick.RecommendedModes.WeatherMonitoring);
        /// </code>
        /// </example>
        public void SetRecommendedMode(RecommendedModes mode)
        {
            switch (mode)
            {
                case RecommendedModes.Gaming:
                {
                    Filter = FilterCoefficient.IIR16;
                    StandbyDuration = InactiveDuration.MS_0_5;
                    PressureSamplingRate = OversamplingRates.Osr4;
                    TemperatureSamplingRate = OversamplingRates.Osr1;
                    HumiditySamplingRate = OversamplingRates.Skipped;
                    PowerMode = PowerModes.On;
                    break;
                }

                case RecommendedModes.HumiditySensing:
                {
                    Filter = FilterCoefficient.IIROff;
                    StandbyDuration = InactiveDuration.MS_1000;
                    PressureSamplingRate = OversamplingRates.Skipped;
                    TemperatureSamplingRate = OversamplingRates.Osr1;
                    HumiditySamplingRate = OversamplingRates.Osr1;
                    PowerMode = PowerModes.Low;
                    break;
                }

                case RecommendedModes.IndoorNavigation:
                {
                    Filter = FilterCoefficient.IIR16;
                    StandbyDuration = InactiveDuration.MS_0_5;
                    PressureSamplingRate = OversamplingRates.Osr16;
                    TemperatureSamplingRate = OversamplingRates.Osr2;
                    HumiditySamplingRate = OversamplingRates.Osr1;
                    PowerMode = PowerModes.On;
                    break;
                }

                case RecommendedModes.WeatherMonitoring:
                {
                    Filter = FilterCoefficient.IIROff;
                    StandbyDuration = InactiveDuration.MS_1000;
                    PressureSamplingRate = OversamplingRates.Osr1;
                    TemperatureSamplingRate = OversamplingRates.Osr1;
                    HumiditySamplingRate = OversamplingRates.Osr1;
                    PowerMode = PowerModes.Low;
                    break;
                }

                case RecommendedModes.FullPower:
                {
                    Filter = FilterCoefficient.IIR16;
                    StandbyDuration = InactiveDuration.MS_0_5;
                    PressureSamplingRate = OversamplingRates.Osr16;
                    TemperatureSamplingRate = OversamplingRates.Osr16;
                    HumiditySamplingRate = OversamplingRates.Osr16;
                    PowerMode = PowerModes.On;
                    break;
                }

                default:
                {
                    throw new ArgumentOutOfRangeException(nameof(mode));
                }
            }
        }

        /// <summary>
        ///     The altitude as read from the Weather Click.
        /// </summary>
        /// <returns>The altitude in meters.</returns>
        /// <remarks>The altitude reading is a calculated value based on well established mathematical formulas.</remarks>
        /// <example>
        ///     Example usage to read the altitude:
        ///     <code language="C#">
        /// Debug.Print("Altitude - " + _weather.ReadAltitude());
        /// </code>
        /// </example>
        public Int32 ReadAltitude()
        {
            return (Int32) Math.Round((Single)(44330 * (1.0 - Math.Pow((Single)(ReadPressure(PressureCompensationModes.Uncompensated) / ReadPressure()), (Single)0.1903))));
        }

        /// <summary>
        ///     Reads the Temperature, Pressure and Humidity from the Weather Click. Additionally, provides calculated Altitude.
        /// </summary>
        /// <remarks>
        ///     As pressure measurement is dependent on temperature, this method reads both temperature and pressure data on
        ///     the same conversion to ensure data integrity.
        /// </remarks>
        /// <param name="pressure">The referenced <c>out</c> parameter that will hold the pressure value.</param>
        /// <param name="temperature">The referenced <c>out</c> parameter that will hold the temperature value.</param>
        /// <param name="humidity">he referenced <c>out</c> parameter that will hold the humidity value.</param>
        /// <param name="altitude">The referenced <c>out</c> parameter that will hold the altitude value.</param>
        /// <example>
        ///     Example usage:
        ///     <code language="C#">
        /// _sensor.SetRecommendedMode(Pressure4Click.RecommendedModes.HandheldDeviceLowPower);
        /// 
        /// while (true)
        /// {
        ///     Single pressure, temperature, altitude;
        /// 
        ///    _sensor.ReadSensor(out pressure, out temperature, out altitude);
        /// 
        ///     Debug.Print("Pressure.......: " + pressure.ToString("F1") + " hPa");
        ///     Debug.Print("Temperature....: " + temperature.ToString("F2") + " °K");
        ///     Debug.Print("Altitude.......: " + altitude.ToString("F0") + " meters");
        /// 
        ///     Thread.Sleep(5000);
        /// }
        /// </code>
        /// </example>
        public void ReadSensor(out Single pressure, out Single temperature, out Single humidity, out Single altitude)
        {
            if (_temperatureSamplingRate == OversamplingRates.Skipped ||
                _pressureSamplingRate == OversamplingRates.Skipped ||
                _humiditySamplingRate == OversamplingRates.Skipped)
            {
                pressure = Single.MinValue;
                temperature = Single.MinValue;
                humidity = Single.MinValue;
                altitude = Single.MinValue;
                return;
            }

            if (_powerMode == PowerModes.Off) EnableForcedMeasurement();

            Int32 adc = (this as ITemperature).RawData;
            Int32 var1 = (((adc >> 3) - (_digT1 << 1)) * _digT2) >> 11;
            Int32 var2 = (((((adc >> 4) - _digT1) * ((adc >> 4) - _digT1)) >> 12) * _digT3) >> 14;
            Int32 tFine = var1 + var2;

            Single temp = ((tFine * 5 + 128) >> 8) / 100.0F;

            switch (TemperatureUnit)
            {
                case TemperatureUnits.Celsius:
                {
                    temperature = temp;
                    break;
                }
                case TemperatureUnits.Fahrenheit:
                {
                    temperature = temp * 1.8F + 32;
                    break;
                }
                case TemperatureUnits.Kelvin:
                {
                    temperature = temp + 273.15F;
                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }

            adc = (this as IPressure).RawData;
            Int64 var3 = (Int64) tFine - 128000;
            Int64 var4 = var3 * var3 * _digP6;
            var4 += ((var3 * _digP5) << 17);
            var4 += ((Int64) _digP4 << 35);
            var3 = ((var3 * var3 * _digP3) >> 8) + ((var3 * _digP2) << 12);
            var3 = ((((Int64) 1 << 47) + var3) * _digP1) >> 33;

            if (var3 == 0)
            {
                pressure = Single.MinValue; // avoid exception caused by division by zero
                altitude = Single.MinValue;
                temperature = Single.MinValue;
                humidity = Single.MinValue;
                return;
            }

            Int64 uncompensatedPressure = 1048576 - adc;
            uncompensatedPressure = ((uncompensatedPressure << 31) - var4) * 3125 / var3;
            var3 = (_digP9 * (uncompensatedPressure >> 13) * (uncompensatedPressure >> 13)) >> 25;
            var4 = (_digP8 * uncompensatedPressure) >> 19;
            uncompensatedPressure = ((uncompensatedPressure + var3 + var4) >> 8) + ((Int64) _digP7 << 4);
            uncompensatedPressure /= 256;

            Single compensatedPressure = CalculatePressureAsl(uncompensatedPressure);

            pressure = PressureCompensation == PressureCompensationModes.Uncompensated
                ? uncompensatedPressure / 100.0F
                : compensatedPressure / 100.0F;

            altitude = (Int32) Math.Round(44330 * (1.0 - Math.Pow(uncompensatedPressure / compensatedPressure,
                                                       0.1903)));

            adc = (this as IHumidity).RawData;
            Int32 u32R = tFine - 76800;
            u32R = (((adc << 14) - (_digH4 << 20) - _digH5 * u32R + 16384) >> 15) *
                   (((((((u32R * _digH6) >> 10) * (((u32R * _digH3) >> 11) + 32768)) >> 10) + 2097152) * _digH2 +
                     8192) >> 14);
            u32R -= (((((u32R >> 15) * (u32R >> 15)) >> 7) * _digH1) >> 4);
            u32R = u32R < 0 ? 0 : u32R;
            u32R = u32R > 419430400 ? 419430400 : u32R;

            humidity = (u32R >> 12) / 1024.0F;
        }

        /// <summary>
        ///     Resets the module
        /// </summary>
        /// <param name="resetMode">
        ///     The reset mode :
        ///     <para>SOFT reset : generally by sending a software command to the chip</para>
        ///     <para>HARD reset : generally by activating a special chip's pin</para>
        /// </param>
        /// <returns></returns>
        /// <exception cref="T:System.NotImplementedException">Thrown because this module has no reset feature.</exception>
        /// <example>
        ///     Example usage:
        ///     <code language="C#">
        /// _weather.Reset(ResetModes.Soft);
        /// </code>
        ///     <code language="VB">
        /// _weather.Reset(ResetModes.Soft)
        /// </code>
        /// </example>
        /// <exception cref="T:System.NotSupportedException">
        ///     A System.NotSupportedException will be thrown if attempting to reset
        ///     this module using a <see cref="F:MBN.Enums.ResetModes.Hard" /> as this module does not support hard resets. Use
        ///     <see cref="F:MBN.Enums.ResetModes.Soft" /> instead.
        /// </exception>
        public Boolean Reset(ResetModes resetMode)
        {
            if (resetMode == ResetModes.Hard) throw new NotSupportedException("This module does not support hard resets. To reset this module, call this method with ResetModes.Soft.");

            WriteByte(REG_RESET, 0xB6);

            do
            {
                Thread.Sleep(10);
            } while (ReadRegister(REG_CONFIG)[0] != 0);

            return ReadRegister(REG_CONFIG)[0] == 0;
        }

        /// <summary>
        ///     Gets or sets the power mode.
        /// </summary>
        /// <example>
        ///     This sample shows how to use the PowerMode property.
        ///     <code language="C#">
        /// _weatherClick.PowerMode = PowerModes.Off;
        /// </code>
        ///     <code language="VB">
        /// _weatherClick.PowerMode = PowerModes.Off
        /// </code>
        /// </example>
        /// <value>
        ///     The current power mode of the module.
        /// </value>
        public PowerModes PowerMode
        {
            get => _powerMode;
            set
            {
                switch (value)
                {
                    case PowerModes.Off:
                    {
                        _pwr = 0x00;
                        break;
                    }

                    case PowerModes.Low:
                    {
                        _pwr = 0x01;
                        break;
                    }

                    case PowerModes.On:
                    {
                        _pwr = 0x03;
                        break;
                    }

                    default:
                    {
                        throw new ArgumentOutOfRangeException(nameof(value));
                    }
                }

                _powerMode = value;
                SetCTRL_MEAS();
            }
        }

        #endregion

        #region Private Methods

        private Boolean Measuring => (ReadRegister(REG_STATUS)[0] & 0x08) == 1;

        private Boolean UpdatingNvm => (ReadRegister(REG_STATUS)[0] & 0x01) == 1;

        private static Single CalculatePressureAsl(Single uncompensatedPressure)
        {
            //      Single seaLevelCompensation = (Single) (101325 * Math.Pow((288 - 0.0065 * 143) / 288f, 5.256f));
            Single seaLevelCompensation = (Single)(101325 * Math.Pow((Single)((288 - 0.0065 * 143) / 288), (Single)5.256));
            return 101325 + uncompensatedPressure - seaLevelCompensation;
        }

        private void SetCTRL_MEAS()
        {
            WriteByte(REG_CTRL_MEAS, (Byte) (((Byte) _temperatureSamplingRate << 5) + ((Byte) _pressureSamplingRate << 2) + (_pwr & 0x03)));
        }

        private Byte[] ReadRegister(Byte registerAddress, Byte bytesToRead = 1)
        {
            Byte[] writeBuffer = {registerAddress};
            Byte[] readBuffer = new Byte[bytesToRead];
            lock (LockI2c)
            {
#if (NANOFRAMEWORK_1_0)
                _sensor.WriteRead(writeBuffer, readBuffer);
#else
                _sensor?.WriteRead(writeBuffer, readBuffer);
#endif
            }
            return readBuffer;
        }

        private void WriteByte(Byte registerAddress, Byte data)
        {
            lock (LockI2c)
            {
                _sensor.Write(new[] { registerAddress, data });
            }
        }

        private void ReadCalibrationData()
        {
            Byte[] rawData = ReadRegister(DIG_T1_LSB_REG, 24); // Temperature & Pressure

            // Temperature
            _digT1 = (UInt16)((rawData[1] << 8) + rawData[0]);
            _digT2 = (Int16)((rawData[3] << 8) + rawData[2]);
            _digT3 = (Int16)((rawData[5] << 8) + rawData[4]);

            // Pressure
            _digP1 = (UInt16)((rawData[7] << 8) + rawData[6]);
            _digP2 = (Int16)((rawData[9] << 8) + rawData[8]);
            _digP3 = (Int16)((rawData[11] << 8) + rawData[10]);
            _digP4 = (Int16)((rawData[13] << 8) + rawData[12]);
            _digP5 = (Int16)((rawData[15] << 8) + rawData[14]);
            _digP6 = (Int16)((rawData[17] << 8) + rawData[16]);
            _digP7 = (Int16)((rawData[19] << 8) + rawData[18]);
            _digP8 = (Int16)((rawData[21] << 8) + rawData[20]);
            _digP9 = (Int16)((rawData[23] << 8) + rawData[22]);

            // Humidity
            _digH1 = ReadRegister(DIG_H1_REG)[0];
            rawData = ReadRegister(DIG_H2_LSB_REG, 7);
            _digH2 = (Int16)((rawData[1] << 8) + rawData[0]);
            _digH3 = rawData[2];
            _digH4 = (Int16)((rawData[4] & 0x0F) + (rawData[3] << 4));
            _digH5 = (Int16)((rawData[4] & 0xF0) + (rawData[5] << 4));
            _digH6 = rawData[6];
        }

        private Byte[] ReadTempData()
        {
            return ReadRegister(READ_TEMPERATURE, 3);
        }

        private Byte[] ReadPressureData()
        {
            return ReadRegister(REG_READ_PRESSURE, 3);
        }

        private Byte[] ReadHumidityData()
        {
            return ReadRegister(READ_HUMIDITY, 2);
        }

        private void EnableForcedMeasurement()
        {
            Byte registerData = ReadRegister(REG_CTRL_MEAS)[0];
            registerData &= 0xFC;
            registerData |= 0x02;
            WriteByte(REG_CTRL_MEAS, registerData);
        }

#endregion

#region Interface Implementations

        /// <inheritdoc cref="IPressure" />
        /// <summary>
        /// Reads the pressure from the sensor.
        /// </summary>
        /// <param name="compensationMode">
        /// Indicates if the pressure reading returned by the sensor is see-level compensated or not.
        /// </param>
        /// <returns>
        /// A <see cref="T:System.Single" /> representing the pressure read from the source, in hPa (hectoPascal)
        /// </returns>
        /// <example>
        /// Example usage:
        /// <code language="C#">
        /// Debug.Print("Pressure is " + _weather.ReadPressure(PressureCompensationModes.Uncompensated).ToString("F1") + " mBar");
        /// </code>
        /// </example>
        public Single ReadPressure(PressureCompensationModes compensationMode = PressureCompensationModes.SeaLevelCompensated)
        {
            if (TemperatureSamplingRate == OversamplingRates.Skipped ||
                PressureSamplingRate == OversamplingRates.Skipped ||
                PowerMode == PowerModes.Off) return Single.MaxValue;

            Int32 adc = (this as ITemperature).RawData;
            Int32 var1 = (((adc >> 3) - (_digT1 << 1)) * _digT2) >> 11;
            Int32 var2 = (((((adc >> 4) - _digT1) * ((adc >> 4) - _digT1)) >> 12) * _digT3) >> 14;
            Int32 tFine = var1 + var2;

            adc = (this as IPressure).RawData;
            Int64 var3 = (Int64) tFine - 128000;
            Int64 var4 = var3 * var3 * _digP6;
            var4 += ((var3 * _digP5) << 17);
            var4 += ((Int64) _digP4 << 35);
            var3 = ((var3 * var3 * _digP3) >> 8) + ((var3 * _digP2) << 12);
            var3 = ((((Int64) 1 << 47) + var3) * _digP1) >> 33;

            if (var3 == 0) return 0; // avoid exception caused by division by zero

            Int64 p = 1048576 - adc;
            p = ((p << 31) - var4) * 3125 / var3;
            var3 = (_digP9 * (p >> 13) * (p >> 13)) >> 25;
            var4 = (_digP8 * p) >> 19;
            p = ((p + var3 + var4) >> 8) + ((Int64) _digP7 << 4);
            p /= 256;

            return compensationMode == PressureCompensationModes.Uncompensated
                ? p / 100.0F
                : CalculatePressureAsl(p) / 100.0F;
        }

        /// <inheritdoc cref="ITemperature" />
        /// <summary>
        /// Reads the temperature.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns>
        /// A single representing the temperature read from the source, degrees Celsius
        /// </returns>
        /// <example>
        /// Example usage:
        /// <code language="C#">
        /// Debug.Print("Temperature is " + _weather.ReadTemperature(TemperatureSources.Ambient).ToString("F2") + " °C");
        /// </code>
        /// </example>
        public Single ReadTemperature(TemperatureSources source = TemperatureSources.Ambient)
        {
            if (TemperatureSamplingRate == OversamplingRates.Skipped || PowerMode == PowerModes.Off) return Single.MaxValue;

            Int32 adc = (this as ITemperature).RawData;
            Int32 var1 = (((adc >> 3) - (_digT1 << 1)) * _digT2) >> 11;
            Int32 var2 = (((((adc >> 4) - _digT1) * ((adc >> 4) - _digT1)) >> 12) * _digT3) >> 14;
            Int32 tFine = var1 + var2;
            Int32 T = (tFine * 5 + 128) >> 8;

            switch (TemperatureUnit)
            {
                case TemperatureUnits.Celsius:
                {
                    return T / 100.0F;
                }
                case TemperatureUnits.Fahrenheit:
                {
                    return T / 100.0F * 1.8F + 32;
                }
                case TemperatureUnits.Kelvin:
                {
                    return T / 100.0F + 273.15F;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <inheritdoc cref="IHumidity" />
        /// <summary>
        ///     Reads the relative or absolute humidity value from the sensor.
        /// </summary>
        /// <returns>
        ///     A single representing the relative/absolute humidity as read from the sensor, in percentage (%) for relative
        ///     reading or value in case of absolute reading.
        /// </returns>
        /// <example>
        ///     Example usage:
        ///     <code language="C#">
        /// Debug.Print("Humidity is " + _weather.ReadHumidity(HumidityMeasurementModes.Relative).ToString("F2") + " %RH");
        /// </code>
        ///     <code language="VB">
        /// Debug.Print("Humidity is " <![CDATA[&]]> _weather.ReadHumidity(HumidityMeasurementModes.Relative).ToString("F2") <![CDATA[&]]> " %RH");
        /// </code>
        /// </example>
        public Single ReadHumidity(HumidityMeasurementModes measurementMode = HumidityMeasurementModes.Relative)
        {
            if (measurementMode == HumidityMeasurementModes.Absolute)
                throw new NotSupportedException(
                    "This module does not support reading \"Absolute Humidity\". Use Relative Humidity instead.");

            if (TemperatureSamplingRate == OversamplingRates.Skipped ||
                HumiditySamplingRate == OversamplingRates.Skipped ||
                PowerMode == PowerModes.Off) return Single.MaxValue;

            Int32 adc = (this as ITemperature).RawData;
            Int32 var1 = (((adc >> 3) - (_digT1 << 1)) * _digT2) >> 11;
            Int32 var2 = (((((adc >> 4) - _digT1) * ((adc >> 4) - _digT1)) >> 12) * _digT3) >> 14;
            Int32 tFine = var1 + var2;

            adc = (this as IHumidity).RawData;
            Int32 u32R = tFine - 76800;
            u32R = (((adc << 14) - (_digH4 << 20) - _digH5 * u32R + 16384) >> 15) *
                   (((((((u32R * _digH6) >> 10) * (((u32R * _digH3) >> 11) + 32768)) >> 10) + 2097152) * _digH2 +
                     8192) >> 14);
            u32R -= (((((u32R >> 15) * (u32R >> 15)) >> 7) * _digH1) >> 4);
            u32R = u32R < 0 ? 0 : u32R;
            u32R = u32R > 419430400 ? 419430400 : u32R;

            return (Single) ((u32R >> 12) / 1024.0);
        }

        /// <inheritdoc cref="ITemperature" />
        /// <summary>Gets the raw data of the temperature value.</summary>
        /// <value>
        ///     Raw data in the range depending on sensor's precision (8/10/12 bits, for example)
        /// </value>
        Int32 ITemperature.RawData
        {
            get
            {
                while (Measuring || UpdatingNvm)
                {
                    Thread.Sleep(10);
                }

                if (PowerMode == PowerModes.Low)
                {
                    SetCTRL_MEAS();
                }

                Byte[] tempTmp = ReadTempData();
                return ((tempTmp[0] << 16) + (tempTmp[1] << 8) + tempTmp[2]) >> 4;
            }
        }

        /// <inheritdoc cref="IPressure" />
        /// <summary>Gets the raw data of the pressure value.</summary>
        /// <value>
        ///     Raw data in the range depending on sensor's precision (8/10/12 bits, for example)
        /// </value>
        Int32 IPressure.RawData
        {
            get
            {
                while (Measuring || UpdatingNvm)
                {
                    Thread.Sleep(10);
                }

                if (PowerMode == PowerModes.Low)
                {
                    SetCTRL_MEAS();
                }

                Byte[] pressTmp = ReadPressureData();
                return ((pressTmp[0] << 16) + (pressTmp[1] << 8) + pressTmp[2]) >> 4;
            }
        }

        /// <inheritdoc cref="IHumidity" />
        /// <summary>Gets the raw data of the humidity value.</summary>
        /// <value>
        ///     Raw data in the range depending on sensor's precision (8/10/12 bits, for example)
        /// </value>
        Int32 IHumidity.RawData
        {
            get
            {
                while (Measuring || UpdatingNvm)
                {
                    Thread.Sleep(10);
                }

                if (PowerMode == PowerModes.Low)
                {
                    SetCTRL_MEAS();
                }

                Byte[] humTmp = ReadHumidityData();
                return (humTmp[0] << 8) + humTmp[1];
            }
        }

#endregion
    }
}