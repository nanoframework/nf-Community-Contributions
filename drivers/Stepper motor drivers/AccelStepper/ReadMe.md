C# nanoFramework version of the popular Arduino C++ stepper motor library.<br>
See original C++ source by http://www.airspayce.com<br>
Copyright (C) Mike McCauley<br>
See http://www.airspayce.com/mikem/arduino/AccelStepper/index.html for licencing information and documentation<br>
In short, this library provides:
<ul>
<li>Non blocking operation</li>
<li>Acceleration and Deceleration motion profiles</li>
<li>Drivers for: </li>
<ul>
<li>Stepper Driver, 2 driver pins required</li>
<li>2 wire stepper, 2 motor pins required</li>
<li>3 wire stepper, such as HDD spindle, 3 motor pins required</li>
<li>4 wire full stepper, 4 motor pins required (e.g. UML2003 module and NEMA motors)</li>
<li>3 wire half stepper, such as HDD spindle, 3 motor pins required</li>
<li>4 wire half stepper, 4 motor pins required</li>
</ul>
</ul>
This library is slightly modified to allow the use of hardware specific low latency timing routines. This is to improve maximum motor speed if the stock nanoFramework DateTime.UTC.Ticks becomes the bottleneck.<br><br>
Example usage:<br>

```C#
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
```
How to import:
<ul>
  <li>Copy the driver source files to your project folder</li>
  <li>Add the latest Nugets as required (see the "using"s at the top of the driver source file)</li>
    <li>See the demo example in Program.cs</li>
</ul>

