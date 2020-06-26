using System;
using System.Threading;
//-------------------------------------------
//WARNING :
//-------------------------------------------
//instead to call System.Diagnostics;  
//you need to call System.Diagnostics.Uart;  only
//otherwise you should have conflict between System.Diagnostics and System.Diagnostics.Uart
//-------------------------------------------
using System.Diagnostics.Uart;

namespace DebugToUart
{
    public class Program
    {
        public static void Main()
        {
            DebugWritelnToUart Debug = new DebugWritelnToUart("COM6", 57600);
            int cnt = 0;

            while(true)
            {
                //-------------------------------------------
                // Debug.xxx
                // works in same way ...
                //-------------------------------------------
                Debug.WriteLine("Hello nanoFramework World >>" + cnt);
                cnt++;

                Thread.Sleep(500);
            }
        }
    }
}
