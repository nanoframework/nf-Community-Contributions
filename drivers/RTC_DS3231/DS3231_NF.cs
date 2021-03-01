/**
 * Original source code:https://github.com/dotnet/iot/tree/main/src/devices/Rtc/Devices/Ds3231
 * Modified by
 * Heinrich Braasch
 * 
 * Updated to use nanoFramework I2C
 * Added a alarm triggered check
 */

using System;
using System.Device.I2c;

namespace Device.DS3231_NF
{
    /// <summary>
    /// Realtime Clock DS3231
    /// </summary>

    public class DS3231 : IDisposable
    {
        /// <summary>
        /// DS3231 Default I2C Address
        /// </summary>
        public const byte DefaultI2cAddress = 0x68;

        private I2cDevice i2cDevice;

        internal enum DS3231Register : byte
        {
            RTC_SEC_REG_ADDR = 0x00,
            RTC_MIN_REG_ADDR = 0x01,
            RTC_HOUR_REG_ADDR = 0x02,
            RTC_DAY_REG_ADDR = 0x03,
            RTC_DATE_REG_ADDR = 0x04,
            RTC_MONTH_REG_ADDR = 0x05,
            RTC_YEAR_REG_ADDR = 0x06,

            RTC_ALM1_SEC_REG_ADDR = 0x07,
            RTC_ALM1_MIN_REG_ADDR = 0x08,
            RTC_ALM1_HOUR_REG_ADDR = 0x09,
            RTC_ALM1_DATE_REG_ADDR = 0x0A,

            RTC_ALM2_MIN_REG_ADDR = 0x0B,
            RTC_ALM2_HOUR_REG_ADDR = 0x0C,
            RTC_ALM2_DATE_REG_ADDR = 0x0D,

            RTC_CTRL_REG_ADDR = 0x0E,
            RTC_STAT_REG_ADDR = 0x0F,
            RTC_TEMP_MSB_REG_ADDR = 0x11,
            RTC_TEMP_LSB_REG_ADDR = 0x12,
        }

        /// <summary>
        /// Creates a new instance of the DS3231
        /// </summary>
        /// <param name="i2cDevice">The I2C device used for communication.</param>
        public DS3231(I2cDevice i2cDevice)
        {
            this.i2cDevice = i2cDevice ?? throw new ArgumentNullException(nameof(i2cDevice));
        }

        public DateTime DateTime
        {
            get => ReadTime();
            set => SetTime(value);
        }

        /// <summary>
        /// Gets or sets which of the two alarms is enabled
        /// </summary>
        public DS3231Alarm EnabledAlarm { get => ReadEnabledAlarm(); set => SetEnabledAlarm(value); }


        /// <summary>
        /// Read Time from DS3231
        /// </summary>
        /// <returns>DS3231 Time</returns>
        protected DateTime ReadTime()
        {
            // Sec, Min, Hour, Day, Date, Month & Century, Year
            var byteBuff = new byte[7];
            SpanByte rawData = new SpanByte(byteBuff);

            i2cDevice.WriteByte((byte)DS3231Register.RTC_SEC_REG_ADDR);
            i2cDevice.Read(rawData);

            return new DateTime(1900 + (rawData[5] >> 7) * 100 + NumberHelper.Bcd2Dec(rawData[6]),
                                NumberHelper.Bcd2Dec((byte)(rawData[5] & 0b_0001_1111)),
                                NumberHelper.Bcd2Dec(rawData[4]),
                                NumberHelper.Bcd2Dec(rawData[2]),
                                NumberHelper.Bcd2Dec(rawData[1]),
                                NumberHelper.Bcd2Dec(rawData[0]));
        }

        /// <summary>
        /// Set DS3231 Time
        /// </summary>
        /// <param name="time">Time</param>
        protected void SetTime(DateTime time)
        {
            SpanByte setData = new SpanByte(new byte[8]);

            setData[0] = (byte)DS3231Register.RTC_SEC_REG_ADDR;

            setData[1] = NumberHelper.Dec2Bcd(time.Second);
            setData[2] = NumberHelper.Dec2Bcd(time.Minute);
            setData[3] = NumberHelper.Dec2Bcd(time.Hour);
            setData[4] = NumberHelper.Dec2Bcd((int)time.DayOfWeek + 1);
            setData[5] = NumberHelper.Dec2Bcd(time.Day);
            if (time.Year >= 2000)
            {
                setData[6] = (byte)(NumberHelper.Dec2Bcd(time.Month) | 0b_1000_0000);
                setData[7] = NumberHelper.Dec2Bcd(time.Year - 2000);
            }
            else
            {
                setData[6] = NumberHelper.Dec2Bcd(time.Month);
                setData[7] = NumberHelper.Dec2Bcd(time.Year - 1900);
            }

            i2cDevice.Write(setData);
        }

        /// <summary>
        /// Read DS3231 Temperature in Celcius
        /// </summary>
        /// <returns>Temperature</returns>
        public double ReadTemperature()
        {
            SpanByte data = new SpanByte(new byte[2]);
            i2cDevice.WriteByte((byte)DS3231Register.RTC_TEMP_MSB_REG_ADDR);
            i2cDevice.Read(data);
            // datasheet Temperature part
            return data[0] + (data[1] >> 6) * 0.25;
        }

        /// <summary>
        /// Reads the currently set alarm 1
        /// </summary>
        /// <returns>Alarm 1</returns>
        public DS3231AlarmOne ReadAlarmOne()
        {
            SpanByte rawData = new SpanByte(new byte[4]);
            i2cDevice.WriteByte((byte)DS3231Register.RTC_ALM1_SEC_REG_ADDR);
            i2cDevice.Read(rawData);

            byte matchMode = 0;
            matchMode |= (byte)((rawData[0] >> 7) & 1); // Get A1M1 bit
            matchMode |= (byte)((rawData[1] >> 6) & (1 << 1)); // Get A1M2 bit
            matchMode |= (byte)((rawData[2] >> 5) & (1 << 2)); // Get A1M3 bit
            matchMode |= (byte)((rawData[3] >> 4) & (1 << 3)); // Get A1M4 bit
            matchMode |= (byte)((rawData[3] >> 2) & (1 << 4)); // Get DY/DT bit

            return new DS3231AlarmOne(
                NumberHelper.Bcd2Dec((byte)(rawData[3] & 0b_0011_1111)),
                new TimeSpan(NumberHelper.Bcd2Dec((byte)(rawData[2] & 0b_0111_1111)),
                NumberHelper.Bcd2Dec((byte)(rawData[1] & 0b_0111_1111)),
                NumberHelper.Bcd2Dec((byte)(rawData[0] & 0b_0111_1111))),
                (DS3231AlarmOneMatchMode)matchMode);
        }

        /// <summary>
        /// Sets alarm 1
        /// </summary>
        /// <param name="alarm">New alarm 1</param>
        public void SetAlarmOne(DS3231AlarmOne alarm)
        {
            if (alarm == null)
            {
                throw new ArgumentNullException(nameof(alarm));
            }

            if (alarm.MatchMode == DS3231AlarmOneMatchMode.DayOfWeekHoursMinutesSeconds)
            {
                if (alarm.DayOfMonthOrWeek < 1 || alarm.DayOfMonthOrWeek > 7)
                {
                    throw new ArgumentOutOfRangeException(nameof(alarm), "Day of week must be between 1 and 7.");
                }
            }
            else if (alarm.MatchMode == DS3231AlarmOneMatchMode.DayOfMonthHoursMinutesSeconds)
            {
                if (alarm.DayOfMonthOrWeek < 1 || alarm.DayOfMonthOrWeek > 31)
                {
                    throw new ArgumentOutOfRangeException(nameof(alarm), "Day of month must be between 1 and 31.");
                }
            }

            SpanByte setData = new SpanByte(new byte[5]);
            setData[0] = (byte)DS3231Register.RTC_ALM1_SEC_REG_ADDR;

            setData[1] = NumberHelper.Dec2Bcd(alarm.AlarmTime.Seconds);
            setData[2] = NumberHelper.Dec2Bcd(alarm.AlarmTime.Minutes);
            setData[3] = NumberHelper.Dec2Bcd(alarm.AlarmTime.Hours);
            setData[4] = NumberHelper.Dec2Bcd(alarm.DayOfMonthOrWeek);

            setData[1] |= (byte)((byte)(((byte)alarm.MatchMode) & 1) << 7); // Set A1M1 bit
            setData[2] |= (byte)((byte)(((byte)alarm.MatchMode) & (1 << 1)) << 6); // Set A1M2 bit
            setData[3] |= (byte)((byte)(((byte)alarm.MatchMode) & (1 << 2)) << 5); // Set A1M3 bit
            setData[4] |= (byte)((byte)(((byte)alarm.MatchMode) & (1 << 3)) << 4); // Set A1M4 bit
            setData[4] |= (byte)((byte)(((byte)alarm.MatchMode) & (1 << 4)) << 2); // Set DY/DT bit

            i2cDevice.Write(setData);
        }



        /// <summary>
        /// Reads the currently set alarm 2
        /// </summary>
        /// <returns>Alarm 1</returns>
        public DS3231AlarmTwo ReadAlarmTwo()
        {
            SpanByte rawData = new SpanByte(new byte[3]);
            i2cDevice.WriteByte((byte)DS3231Register.RTC_ALM2_MIN_REG_ADDR);
            i2cDevice.Read(rawData);

            byte matchMode = 0;
            matchMode |= (byte)((rawData[0] >> 7) & 1); // Get A2M2 bit
            matchMode |= (byte)((rawData[1] >> 6) & (1 << 1)); // Get A2M3 bit
            matchMode |= (byte)((rawData[2] >> 5) & (1 << 2)); // Get A2M4 bit
            matchMode |= (byte)((rawData[2] >> 3) & (1 << 3)); // Get DY/DT bit

            return new DS3231AlarmTwo(
                NumberHelper.Bcd2Dec((byte)(rawData[2] & 0b_0011_1111)),
                new TimeSpan(NumberHelper.Bcd2Dec((byte)(rawData[1] & 0b_0111_1111)),
                NumberHelper.Bcd2Dec((byte)(rawData[0] & 0b_0111_1111)),
                0),
                (DS3231AlarmTwoMatchMode)matchMode);
        }

        /// <summary>
        /// Sets alarm 2
        /// </summary>
        /// <param name="alarm">New alarm 2</param>
        public void SetAlarmTwo(DS3231AlarmTwo alarm)
        {
            if (alarm == null)
            {
                throw new ArgumentNullException(nameof(alarm));
            }

            if (alarm.MatchMode == DS3231AlarmTwoMatchMode.DayOfWeekHoursMinutes)
            {
                if (alarm.DayOfMonthOrWeek < 1 || alarm.DayOfMonthOrWeek > 7)
                {
                    throw new ArgumentOutOfRangeException(nameof(alarm), "Day of week must be between 1 and 7.");
                }
            }
            else if (alarm.MatchMode == DS3231AlarmTwoMatchMode.DayOfMonthHoursMinutes)
            {
                if (alarm.DayOfMonthOrWeek < 1 || alarm.DayOfMonthOrWeek > 31)
                {
                    throw new ArgumentOutOfRangeException(nameof(alarm), "Day of month must be between 1 and 31.");
                }
            }

            SpanByte setData = new SpanByte(new byte[4]);
            setData[0] = (byte)DS3231Register.RTC_ALM2_MIN_REG_ADDR;

            setData[1] = NumberHelper.Dec2Bcd(alarm.AlarmTime.Minutes);
            setData[2] = NumberHelper.Dec2Bcd(alarm.AlarmTime.Hours);
            setData[3] = NumberHelper.Dec2Bcd(alarm.DayOfMonthOrWeek);

            setData[1] |= (byte)((byte)(((byte)alarm.MatchMode) & 1) << 7); // Set A2M2 bit
            setData[2] |= (byte)((byte)(((byte)alarm.MatchMode) & (1 << 1)) << 6); // Set A2M3 bit
            setData[3] |= (byte)((byte)(((byte)alarm.MatchMode) & (1 << 2)) << 5); // Set A2M4 bit
            setData[3] |= (byte)((byte)(((byte)alarm.MatchMode) & (1 << 3)) << 3); // Set DY/DT bit

            i2cDevice.Write(setData);
        }

        /// <summary>
        /// Reads which alarm is enabled
        /// </summary>
        /// <returns>The enabled alarm</returns>
        protected DS3231Alarm ReadEnabledAlarm()
        {
            SpanByte getData = new SpanByte(new byte[1]);
            i2cDevice.WriteByte((byte)DS3231Register.RTC_CTRL_REG_ADDR);
            i2cDevice.Read(getData);

            bool a1ie = (getData[0] & 1) != 0; // Get A1IE bit
            bool a2ie = (getData[0] & (1 << 1)) != 0; // Get A2IE bit

            if (a1ie)
            {
                return DS3231Alarm.AlarmOne;
            }
            else if (a2ie)
            {
                return DS3231Alarm.AlarmTwo;
            }
            else
            {
                return DS3231Alarm.None;
            }
        }

        /// <summary>
        /// Sets which alarm is enabled
        /// </summary>
        /// <param name="alarmMode">Alarm to enable</param>
        protected void SetEnabledAlarm(DS3231Alarm alarmMode)
        {
            SpanByte getData = new SpanByte(new byte[1]);
            i2cDevice.WriteByte((byte)DS3231Register.RTC_CTRL_REG_ADDR);
            i2cDevice.Read(getData);

            SpanByte setData = new SpanByte(new byte[2]);
            setData[0] = (byte)DS3231Register.RTC_CTRL_REG_ADDR;

            setData[1] = getData[0];
            setData[1] &= unchecked((byte)~1); // Clear A1IE bit
            setData[1] &= unchecked((byte)~(1 << 1)); // Clear A2IE bit

            if (alarmMode == DS3231Alarm.AlarmOne)
            {
                setData[1] |= 1; // Set A1IE bit
            }
            else if (alarmMode == DS3231Alarm.AlarmTwo)
            {
                setData[1] |= 1 << 1; // Set A2IE bit
            }

            i2cDevice.Write(setData);
        }

        /// <summary>
        /// Allows user to poll which alarm is triggered
        /// </summary>
        /// <param name="alarm">Alarm to check</param>
        public bool CheckIfAlarmTriggered(DS3231Alarm alarm)
        {
            SpanByte rawData = new SpanByte(new byte[1]);
            i2cDevice.WriteByte((byte)DS3231Register.RTC_STAT_REG_ADDR);
            i2cDevice.Read(rawData);
            switch (alarm)
            {
                case DS3231Alarm.AlarmOne:
                    return (rawData[0] & 0b00000001) == 0b00000001;
                case DS3231Alarm.AlarmTwo:
                    return (rawData[0] & 0b00000010) == 0b00000010;
                default:
                    throw new ArgumentException();
            }
        }

        /// <summary>
        /// Resets the triggered state of both alarms. This must be called after every alarm
        /// trigger otherwise the alarm cannot trigger again
        /// </summary>
        public void ResetAlarmTriggeredStates()
        {
            SpanByte getData = new SpanByte(new byte[1]);
            i2cDevice.WriteByte((byte)DS3231Register.RTC_STAT_REG_ADDR);
            i2cDevice.Read(getData);

            SpanByte setData = new SpanByte(new byte[2]);
            setData[0] = (byte)DS3231Register.RTC_STAT_REG_ADDR;

            setData[1] = getData[0];
            setData[1] &= unchecked((byte)~1); // Clear A1F bit
            setData[1] &= unchecked((byte)~(1 << 1)); // Clear A2F bit

            i2cDevice.Write(setData);
        }


        internal static class NumberHelper
        {
            /// <summary>
            /// BCD To decimal
            /// </summary>
            /// <param name="bcd">BCD Code</param>
            /// <returns>decimal</returns>
            public static int Bcd2Dec(byte bcd) => ((bcd >> 4) * 10) + (bcd % 16);

            /// <summary>
            /// BCD To decimal
            /// </summary>
            /// <param name="bcds">BCD Code</param>
            /// <returns>decimal</returns>
            public static int Bcd2Dec(byte[] bcds)
            {
                int result = 0;
                foreach (byte bcd in bcds)
                {
                    result *= 100;
                    result += Bcd2Dec(bcd);
                }

                return result;
            }

            /// <summary>
            /// Decimal To BCD
            /// </summary>
            /// <param name="dec">decimal</param>
            /// <returns>BCD Code</returns>
            public static byte Dec2Bcd(int dec)
            {
                if ((dec > 99) || (dec < 0))
                {
                    throw new ArgumentException(nameof(dec), "Value must be between 0-99.");
                }

                return (byte)(((dec / 10) << 4) + (dec % 10));
            }
        }

        /// <summary>
        /// Cleanup
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Available alarms on the DS3231
    /// </summary>
    public enum DS3231Alarm
    {
        /// <summary>
        /// Indicates none of the alarms
        /// </summary>
        None,

        /// <summary>
        /// Indicates the first alarm
        /// </summary>
        AlarmOne,

        /// <summary>
        /// Indicates the second alarm
        /// </summary>
        AlarmTwo
    }

    public class DS3231AlarmOne
    {
        /// <summary>
        /// Day of month or day of week of the alarm. Which one it is depends on the match mode
        /// </summary>
        public int DayOfMonthOrWeek { get; set; }

        /// <summary>
        /// Get or set the time the alarm, Hour, Minute and Second are used
        /// </summary>
        public TimeSpan AlarmTime { get; set; }

        /// <summary>
        /// Mode to use to determine when to trigger the alarm
        /// </summary>
        public DS3231AlarmOneMatchMode MatchMode { get; set; }

        /// <summary>
        /// Creates a new instance of alarm 1 on the DS3231
        /// </summary>
        /// <param name="dayOfMonthOrWeek">Day of month or day of week of the alarm. Which one it is depends on the match mode</param>
        /// <param name="alarmTime">Time of the alarm</param>
        /// <param name="matchMode">Mode to use to determine when to trigger the alarm</param>
        public DS3231AlarmOne(int dayOfMonthOrWeek, TimeSpan alarmTime, DS3231AlarmOneMatchMode matchMode)
        {
            DayOfMonthOrWeek = dayOfMonthOrWeek;
            AlarmTime = alarmTime;
            MatchMode = matchMode;
        }
    }

    public enum DS3231AlarmOneMatchMode : byte
    {
        /// <summary>
        /// Alarm 1 triggers at the start of every second
        /// </summary>
        OncePerSecond = 0x0F,

        /// <summary>
        /// Alarm 1 triggers when the seconds match
        /// </summary>
        Seconds = 0x0E,

        /// <summary>
        /// Alarm 1 triggers when the minutes and seconds match
        /// </summary>
        MinutesSeconds = 0x0C,

        /// <summary>
        /// Alarm 1 triggers when the hours, minutes and seconds match
        /// </summary>
        HoursMinutesSeconds = 0x08,

        /// <summary>
        /// Alarm 1 triggers when the day of the month, hours, minutes and seconds match
        /// </summary>
        DayOfMonthHoursMinutesSeconds = 0x00,

        /// <summary>
        /// Alarm 1 triggers when the day of the week, hours, minutes and seconda match. Sunday is day 1
        /// </summary>
        DayOfWeekHoursMinutesSeconds = 0x10
    }

    /// <summary>
    /// Represents alarm 2 on the DS3231
    /// </summary>
    public class DS3231AlarmTwo
    {
        /// <summary>
        /// Day of month or day of week of the alarm. Which one it is depends on the match mode
        /// </summary>
        public int DayOfMonthOrWeek { get; set; }

        /// <summary>
        /// Get or set the time the alarm, Hour and Minute are used
        /// </summary>
        public TimeSpan AlarmTime { get; set; }

        /// <summary>
        /// Mode to use to determine when to trigger the alarm
        /// </summary>
        public DS3231AlarmTwoMatchMode MatchMode { get; set; }

        /// <summary>
        /// Creates a new instance of alarm 2 on the DS3231
        /// </summary>
        /// <param name="dayOfMonthOrWeek">Day of month or day of week of the alarm. Which one it is depends on the match mode</param>
        /// <param name="alarmTime">The time the alarm, Hour and Minute are used</param>
        /// <param name="matchMode">Mode to use to determine when to trigger the alarm</param>
        public DS3231AlarmTwo(int dayOfMonthOrWeek, TimeSpan alarmTime, DS3231AlarmTwoMatchMode matchMode)
        {
            DayOfMonthOrWeek = dayOfMonthOrWeek;
            AlarmTime = alarmTime;
            MatchMode = matchMode;
        }
    }

    /// <summary>
    /// Available modes for determining when alarm 2 should trigger
    /// </summary>
    public enum DS3231AlarmTwoMatchMode : byte
    {
        /// <summary>
        /// Alarm 2 triggers at the start of every minute
        /// </summary>
        OncePerMinute = 0x07,

        /// <summary>
        /// Alarm 2 triggers when the minutes match
        /// </summary>
        Minutes = 0x06,

        /// <summary>
        /// Alarm 2 triggers when the hours and minutes match
        /// </summary>
        HoursMinutes = 0x04,

        /// <summary>
        /// Alarm 2 triggers when the day of the month, hours and minutes match
        /// </summary>
        DayOfMonthHoursMinutes = 0x00,

        /// <summary>
        /// Alarm 2 triggers when the day of the week, hours and minutes match. Sunday is day 1
        /// </summary>
        DayOfWeekHoursMinutes = 0x08
    }
}

