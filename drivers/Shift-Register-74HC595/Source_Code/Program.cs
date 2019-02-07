using System;
using System.Threading;

namespace ShiftRegister.Driver
{
    
    public class Program
    {

        public static void Main()
        {
            // Set digital pins 
            // Any digital pins can be used
            int Clock = 18;
            int Data = 23;
            int Latch = 5;

            // Initiate the 74HC595 Shift Register
            HC595 ShiftRegister = new HC595(Clock, Data, Latch);

            // Loop forever
            while (true)
            {

            // Turn all 8 pins on
            for (int i = 0; i < 8; i++)
            {
                ShiftRegister.SetPin(i,true);

                Thread.Sleep(500);
                
            }

            // Turn pins off
            for (int i = 0; i < 8; i++)
            {
                ShiftRegister.SetPin(i, false);

                    Thread.Sleep(500);

            }
                             
            }
  
        }

     }

 }


   

