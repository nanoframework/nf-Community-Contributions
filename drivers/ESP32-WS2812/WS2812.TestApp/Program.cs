using System;
using System.Diagnostics;
using System.Threading;

namespace WS2812.TestApp
{
    public class Program
    {
        public static void Main()
        {
            uint ledCount = 25;
            PixelController controller = new PixelController(27, ledCount, false);

            int step = (int)(360 / ledCount);
            var hue = 0;

            for (uint i = 0; i < ledCount; i++)
            {
                controller.SetHSVColor((short)i, (short)hue, 1, 0.05f);
                hue = hue + step;
                controller.UpdatePixels();
            }

            for (; ; )
            {
                controller.MovePixelsByStep(1);
                controller.UpdatePixels();
                Thread.Sleep(10);
            }
        }
    }
}
