using System;
using System.Text;

namespace MBN
{
    /// <summary>
    /// Interface used for drivers using humidity sensors
    /// </summary>
    public interface IHumidity
    {
        /// <summary>
        /// Reads the relative or absolute humidity value from the sensor.
        /// </summary>
        /// <returns>A single representing the relative/absolute humidity as read from the sensor, in percentage (%) for relative reading or value in case of absolute reading.</returns>
        Single ReadHumidity(HumidityMeasurementModes measurementMode = HumidityMeasurementModes.Relative);

        /// <summary>
        /// Gets the raw data of the humidity value.
        /// </summary>
        /// <value>
        /// Raw data in the range depending on sensor's precision (8/10/12 bits, for example)
        /// </value>
        Int32 RawData { get; }
    }
}
