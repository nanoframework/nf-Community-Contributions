using System;
using System.Text;

namespace MBN
{
    /// <summary>
    /// Units used by the ITemperature interface
    /// </summary>
    public enum TemperatureUnits
    {
        /// <summary>
        /// Celsius unit
        /// </summary>
        Celsius,
        /// <summary>
        /// Fahrenheit unit
        /// </summary>
        Fahrenheit,
        /// <summary>
        /// Kelvin unit
        /// </summary>
        Kelvin
    }

    /// <summary>
    /// Temperature sources used by the ITemperature interface.
    /// </summary>
    public enum TemperatureSources
    {
        /// <summary>
        /// Measures the ambient (room) temperature.
        /// </summary>
        Ambient,
        /// <summary>
        /// Measures an object temperature, either via external sensor or IR sensor, for example.
        /// </summary>
        Object
    }

    /// <summary>
    /// Measurement modes used by the IHumidity interface.
    /// </summary>
    public enum HumidityMeasurementModes
    {
        /// <summary>
        /// Relative humidity measurement mode
        /// </summary>
        Relative,
        /// <summary>
        /// Absolute humidity measurement mode
        /// </summary>
        Absolute
    }

    /// <summary>
    /// Compensation modes for pressure sensors
    /// </summary>
    public enum PressureCompensationModes
    {
        /// <summary>
        /// Sea level compensated
        /// </summary>
        SeaLevelCompensated,
        /// <summary>
        /// Raw uncompensated
        /// </summary>
        Uncompensated
    }

    /// <summary>
    /// Power modes that may be applicable to a module
    /// </summary>
    public enum PowerModes : Byte
    {
        /// <summary>
        /// Module is turned off, meaning it generally can't perform measures or operate
        /// </summary>
        Off,
        /// <summary>
        /// Module is either in hibernate mode or low power mode (depending on the module)
        /// </summary>
        Low,
        /// <summary>
        /// Module is turned on, at full power, meaning it is fully functionnal
        /// </summary>
        On
    }

    /// <summary>
    /// Reset modes that may be applicable to a module
    /// </summary>
    public enum ResetModes : Byte
    {
        /// <summary>
        /// Software reset, which usually consists in a command sent to the device.
        /// </summary>
        Soft,
        /// <summary>
        /// Hardware reset, which usually consists in toggling a IO pin connected to the device.
        /// </summary>
        Hard
    }
}