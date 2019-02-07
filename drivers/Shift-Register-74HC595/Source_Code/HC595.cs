

using System;
using System.Threading;
using Windows.Devices.Gpio;

namespace ShiftRegister.Driver
{

    /// <summary>
    /// Driver for the 74HC595 Shift Register
    /// </summary>
    public class HC595
    {
     
        /// <summary>
        /// Bit state is saved each time a pin is set
        /// </summary>
        readonly int[] Bits = { 0, 0, 0, 0, 0, 0, 0, 0 };

        /// <summary>
        /// Pins can be any digital pin
        /// </summary>
        private readonly GpioPin ClockPin;
        private readonly GpioPin DataPin;
        private readonly GpioPin LatchPin;

        /// <summary>
        /// Constructor
        /// Example:  HC595 ShiftRegister = new HC595(Clock, Data, Latch);
        /// </summary>
        /// <param name="ClockPinNumber"></param>
        /// <param name="DataPinNumber"></param>
        /// <param name="LatchPinNumber"></param>
        public HC595(int ClockPinNumber, int DataPinNumber, int LatchPinNumber)
        {

            //Set pins 
            ClockPin = GpioController.GetDefault().OpenPin(ClockPinNumber);
            ClockPin.SetDriveMode(GpioPinDriveMode.Output);
            ClockPin.Write(GpioPinValue.Low);

            DataPin = GpioController.GetDefault().OpenPin(DataPinNumber);
            DataPin.SetDriveMode(GpioPinDriveMode.Output);
            DataPin.Write(GpioPinValue.Low);

            LatchPin = GpioController.GetDefault().OpenPin(LatchPinNumber);
            LatchPin.SetDriveMode(GpioPinDriveMode.Output);
            LatchPin.Write(GpioPinValue.Low);

            // Set all pins low
           for (int i = 0; i < 8; i++)
            {
                SetPin(i, false);
                Thread.Sleep(10);

            }

        }

        /// <summary>
        /// Change the state of the pin number
        /// </summary>
        /// <param name="Pin"></param>
        /// Pin to change 0 to 7 
        /// <param name="State"></param>
        /// Pin On of Off  ... High or Low
        public void SetPin(int Pin, bool State)
        {

            // Pull latch low 
            LatchPin.Write(GpioPinValue.Low);


            //If Pin is low it's bit position will be changed to 0
            int PinValue = 0;

            //Pin is high change to 1
            if (State)
                PinValue = 1;
           
            // Update the Bits array with pin state
            Bits[Pin] = PinValue;
           
            //Repeat for each bit
            for (int i = 0; i < 8; i++)  
            {

                // Read each bit in the array and set the data pin with current state 
                // Set DataPin high for 1 and low for 0
                if (Bits[i] == 1)
                {

                    DataPin.Write(GpioPinValue.High);    
                   
                }
                else
                {

                    DataPin.Write(GpioPinValue.Low);   
                   
                }

                
               // Pulse clock high and then low to set send the bit
                ClockPin.Write(GpioPinValue.High); 
                
                Thread.Sleep(1);

                ClockPin.Write(GpioPinValue.Low);
            }

            // Pull latch pin high to "Latch" 
            // activate the Shift Register pins to the current bits state 
            LatchPin.Write(GpioPinValue.High);
           
        }
 
    }

}

