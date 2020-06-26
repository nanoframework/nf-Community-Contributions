/*
// ----------------------------------------------------------------
// Copyright (c) 2018 The nanoFramework project contributors
// See LICENSE file in the project root for full license information.
// ----------------------------------------------------------------
// Valon Hoti @ Prishtine // Jul 23 - 2020 
// this class made to work only on debug mode ...
// ----------------------------------------------------------------
// usage :
// ----------------------------------------------------------------

     DebugWritelnToUart debug = new DebugWritelnToUart("COM2", 57600);
                        debug.WriteLine("Hello to uart ! \n"); 


     if you do not want to use more this class

     you can also dispose like 

                       debug.Dispose();

// ----------------------------------------------------------------
// Warning :
// ----------------------------------------------------------------

    you need to choose what to use 

    System.Diagnostics or
    System.Dianogstics.Uart 

    you can not use both, just only ...

 */

using System;

namespace System.Diagnostics.Uart
{
    //-------------------------------------------
    // include references 
    //-------------------------------------------
    //  nanoFramework.Runtime.Events.dll
    //  nanoFramework.System.Text.dll
    //  Windows.Devices.SerialCommunication.dll
    //  Windows.Storage.Stream.dll
    //-------------------------------------------
    using System.Diagnostics;
    using Windows.Devices.SerialCommunication;
    using Windows.Storage.Streams;
    public class DebugWritelnToUart:IDisposable
    {
        private SerialDevice uart;
        private DataWriter uartoutput;

        /// <summary>
        ///   Set which COM port you want to use and what baudrate
        /// </summary>
        /// <param name="Comuart"></param>
        /// <param name="BaudRate"></param>
        public DebugWritelnToUart(string Comuart,uint BaudRate)
        {
#if DEBUG
            uart = SerialDevice.FromId(Comuart);
            uart.WatchChar = '\n';

            uart.BaudRate = BaudRate;
            uart.Parity = SerialParity.None;
            uart.StopBits = SerialStopBitCount.One;
            uart.Handshake = SerialHandshake.None;
            uart.DataBits = 8;

            uartoutput = new DataWriter(uart.OutputStream);
            uart.WriteTimeout = new TimeSpan(0, 0, 5);
#endif
        }

        /// <summary>
        ///  use in same way as Debug.Write
        /// </summary>
        /// <param name="message"></param>
        public void Write(string message)
        {
            Debug.Write(message);
#if DEBUG
            uartoutput.WriteString(message);
            uartoutput.Store();
#endif
        }

        /// <summary>
        ///  use in same way as Debug.WriteLine
        /// </summary>
        /// <param name="message"></param>
        public void WriteLine(string message)
        {
            Debug.WriteLine(message);
#if DEBUG
            uartoutput.WriteString(message+"\r\n");
            uartoutput.Store();
#endif
        }

        /// <summary>
        ///  use in same way as Debug.Assert
        /// </summary>
        /// <param name="condition"></param>
        public void Assert(bool condition)
        {
            Debug.Assert(condition);
#if DEBUG
            uartoutput.WriteString("Assert[" + condition.ToString() + "]"+"\r\n");
            uartoutput.Store();
#endif
        }
        /// <summary>
        ///  use in same way as Debug.Assert
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="message"></param>
        public void Assert(bool condition, string message)
        {
            Debug.Assert(condition, message);
#if DEBUG
            uartoutput.WriteString("Assert["+condition.ToString()+"], Message["+message+"]"+"\r\n");
            uartoutput.Store();
#endif
        }

        /// <summary>
        ///  use in same way as Debug.Assert
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="message"></param>
        /// <param name="detailedMessage"></param>
        public void Assert(bool condition, string message, string detailedMessage)
        {
            Debug.Assert(condition, message , detailedMessage);
#if DEBUG
            uartoutput.WriteString("Assert[" + condition.ToString() + "], Message[" + message + "], DetailedMessage["+detailedMessage+"]"+"\r\n");
            uartoutput.Store();
#endif
        }

        public void Dispose()
        {
            uart.Dispose();
        }
    }
}
