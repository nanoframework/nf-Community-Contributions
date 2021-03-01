# DS3231 driver

Get/Set time<br/>
Get/Set alarms<br/>
Read temperature<br/>
The SQW pin toggles from high to low when the alarm triggers<br/>
Example code:<br/>
```
            // Set I2C pins for ESP32 (choose your own)
            Configuration.SetPinFunction(21, DeviceFunction.I2C1_DATA);
            Configuration.SetPinFunction(22, DeviceFunction.I2C1_CLOCK);

            // Setup I2C
            var settings = new I2cConnectionSettings(1, DS3231.DefaultI2cAddress, I2cBusSpeed.StandardMode);
            var i2cDevice = I2cDevice.Create(settings);
             

            using (var rtc = new DS3231(i2cDevice))
            {
                // Set DS3231 time
                rtc.DateTime = new DateTime(2021, 2, 27, 16, 01, 0);

                // Read time
                DateTime dt = rtc.DateTime;

                // Read temperature
                double temp = rtc.ReadTemperature();

                // Set alarm
                var alarmOne = new DS3231AlarmOne(0, new TimeSpan(0, 0, 59), DS3231AlarmOneMatchMode.Seconds);  
                rtc.SetAlarmOne(alarmOne);
                rtc.EnabledAlarm = DS3231Alarm.AlarmOne;
                while (true)
                {
                    dt = rtc.DateTime;
                    Debug.WriteLine(dt.ToString());
                    if (rtc.CheckIfAlarmTriggered(DS3231Alarm.AlarmOne))
                    {
                        Debug.WriteLine("Alarm triggerred");
                        rtc.ResetAlarmTriggeredStates();
                    }
                    Thread.Sleep(1000);
                }
            }
```
