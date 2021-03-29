using Device.AccelStepper_NF;
using nanoFramework.Hardware.Esp32;
using System;
using System.Diagnostics;
using System.Threading;
using static Device.AccelStepper_NF.AccelStepper;

namespace AccelStepperDemo
{
    public class Program
    {
        const int IN1 = 19;
        const int IN2 = 21;
        const int IN3 = 22;
        const int IN4 = 23;

        public static void Main()
        {
            Debug.WriteLine("Hello from nanoFramework!");
            // var accelStepper = new AccelStepper(AccelStepper.MotorInterfaceType.FULL4WIRE, IN1, IN3, IN2, IN4, false);
            var accelStepper = new AccelStepper(AccelStepper.MotorInterfaceType.FULL4WIRE, IN1, IN3, IN2, IN4, false, new GetMicroSecondsHandler(GetMicroSeconds));
            accelStepper.SetMaxSpeed(400.0f);
            accelStepper.SetAcceleration(100.0f);
            accelStepper.MoveTo(-4069);
            accelStepper.EnableOutputs();

            while (true)
            {
                // Change direction at the limits
                if (accelStepper.DistanceToGo() == 0)
                    accelStepper.MoveTo(-accelStepper.CurrentPosition());
                accelStepper.Run();
            }

        }

        private static ulong GetMicroSeconds()
        {
            return HighResTimer.GetCurrent();
        }
    }
}
