using System;
using System.Text;

namespace MBN
{
    /// <summary>
    /// Interface used for drivers using pressure sensors
    /// </summary>
    public interface IPressure
    {
        /// <summary>
        /// Reads the pressure from the sensor.
        /// </summary>
        /// <param name="compensationMode">Indicates if the pressure reading returned by the sensor is see-level compensated or not.</param>
        /// <returns>A single representing the pressure read from the source, in hPa (hectoPascal)</returns>
        Single ReadPressure(PressureCompensationModes compensationMode = PressureCompensationModes.SeaLevelCompensated);

        /// <summary>
        /// Gets the raw data of the pressure value.
        /// </summary>
        /// <value>
        /// Raw data in the range depending on sensor's precision (8/10/12 bits, for example)
        /// </value>
        Int32 RawData { get; }
    }
}
