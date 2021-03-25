using System;
using System.Text;

namespace MBN
{
    /// <summary>
    /// Interface used for drivers using temperature sensors
    /// </summary>
    public interface ITemperature
    {
        /// <summary>
        /// Reads the temperature.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns>A single representing the temperature read from the source, degrees Celsius</returns>
        Single ReadTemperature(TemperatureSources source = TemperatureSources.Ambient);

        /// <summary>
        /// Gets the raw data of the temperature value.
        /// </summary>
        /// <value>
        /// Raw data in the range depending on sensor's precision (8/10/12 bits, for example)
        /// </value>
        Int32 RawData { get; }
    }
}
