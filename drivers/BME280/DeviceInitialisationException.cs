using System;
using System.Text;

namespace MBN
{
    /// <summary>
    /// Exception thrown when a new instance of a driver can't be created. It may be because of too short delays or bad commands sent to the device.
    /// </summary>
    [Serializable]
    public class DeviceInitialisationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceInitialisationException"/> class.
        /// </summary>
        public DeviceInitialisationException() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceInitialisationException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public DeviceInitialisationException(String message) : base(message) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceInitialisationException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public DeviceInitialisationException(String message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Gets the <see cref="T:System.Exception" /> instance that caused the current exception.
        /// </summary>
        /// <returns>An instance of Exception that describes the error that caused the current exception. The InnerException property returns the same value as was passed into the constructor, or a null reference (Nothing in Visual Basic) if the inner exception value was not supplied to the constructor. This property is read-only.</returns>
        public new Exception InnerException { get { return base.InnerException; } }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        /// <PermissionSet><IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" PathDiscovery="*AllFiles*" /></PermissionSet>
        public override String ToString() => "DeviceInitialisationException : " + base.Message;
    }
}
