using Device.ULN2003_NF;
using nanoFramework.Hardware.Esp32;
using System.Device.Gpio;
using System.Diagnostics;
using static Device.ULN2003_NF.Uln2003;

namespace ULN2003
{
    public class Program
    {
        // Assign GPIO pin numbers
        const int IN1 = 19;
        const int IN2 = 21;
        const int IN3 = 22;
        const int IN4 = 23;


        public static void Main()
        {
            Debug.WriteLine("ULN2003 demonstration");

            var stepper = new Uln2003(IN1, IN2, IN3, IN4);
            // var stepper = new Uln2003(IN1, IN2, IN3, IN4, new Esp32HiresStopwatch());

            stepper.Mode = StepperMode.FullStepSinglePhase;

            stepper.SpeedAsDegPerSec = 80;

            // Blocking operation
            Debug.WriteLine("Rotate and block");
            stepper.StepAndWait(-4069);

            // Non blocking operation
            #region // Cycles between forward and reverse
            int totalSteps = 4069;
            Debug.WriteLine("Rotate in positive direction");
            stepper.StepNoWait(totalSteps);

            while (true)
            {
                if (stepper.IsTargetPositionReached())
                {
                    totalSteps = -totalSteps;
                    Debug.WriteLine("Rotate in negative direction");
                    stepper.StepNoWait(totalSteps);
                }
                stepper.Run();

            } 
            #endregion

        }

        public class Esp32HiresStopwatch : IUln2003StopWatch
        {

            ulong startTicks = HighResTimer.GetCurrent();
            bool isRunning = false;
            ulong stopTicks;

            public void Stop()
            {
                isRunning = false;
                stopTicks = HighResTimer.GetCurrent();
            }

            public void Restart()
            {
                startTicks = HighResTimer.GetCurrent();
                isRunning = true;
            }

            public ulong TotalMicroSeconds => isRunning ? HighResTimer.GetCurrent() - startTicks : stopTicks - startTicks;
        }

    }
}
