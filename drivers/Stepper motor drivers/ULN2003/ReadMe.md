A stepper motor driver for nanoFramework using the ULN2003 hardware module.<br>
Original source is dotnet/iot https://github.com/dotnet/iot/tree/main/src/devices/Uln2003<br>
Its been slightly adapted to:<br>
<ul>
  <li>Use nanoFramework GPIO pins</li>
  <li>Allow for non blocking operation</li>
  <li>Allow the use of MCU specific low latency clock routines to increase motor speeds</li>
</ul>
See the sample usage code below:<br>

```C#
public static void Main()
{
    Debug.WriteLine("ULN2003 demonstration");

    var stepper = new Uln2003(19, 21, 22, 23);
    // var stepper = new Uln2003(19, 21, 22, 23, new Esp32HiresStopwatch());

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
```
How to import:<br>
<ul>
  <li>Copy the driver source files to your project folder</li>
  <li>Add the latest Nugets as required (see the "using"s at the top of the driver source file)</li>
  <li>See the demo example in Program.cs</li>
</ul>

