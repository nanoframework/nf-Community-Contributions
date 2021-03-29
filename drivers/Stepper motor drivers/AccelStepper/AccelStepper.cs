/*
* AccelStepper, stepper motor driver in C# for nanoFramework
* 
* Original C++ source by http://www.airspayce.com
* 
* Copyright (C) Mike McCauley
* See http://www.airspayce.com/mikem/arduino/AccelStepper/index.html
* for licencing information and documentation
* 
* This C# translation has only been tested with the ULN2003 driver board.
* 
* Change history:
* 29 Mar 2021: Translated from C++ to C# for nanoFramework by H Braasch
*/

using System;
using System.Device.Gpio;

namespace Device.AccelStepper_NF
{
    public class AccelStepper
    {
        public enum MotorInterfaceType
        {
            FUNCTION = 0, ///< Use the functional interface, implementing your own driver functions (internal use only)
            DRIVER = 1, ///< Stepper Driver, 2 driver pins required
            FULL2WIRE = 2, ///< 2 wire stepper, 2 motor pins required
            FULL3WIRE = 3, ///< 3 wire stepper, such as HDD spindle, 3 motor pins required
            FULL4WIRE = 4, ///< 4 wire full stepper, 4 motor pins required
            HALF3WIRE = 6, ///< 3 wire half stepper, such as HDD spindle, 3 motor pins required
            HALF4WIRE = 8  ///< 4 wire half stepper, 4 motor pins required
        }

        public enum Direction
        {
            DIRECTION_CCW = 0,  ///< Counter-Clockwise
            DIRECTION_CW = 1   ///< Clockwise
        }

        private class Pin
        {
            public byte Num { get; set; }
            public GpioPin GpioPin { get; set; }
        }

        private const byte LOW = 0x0;
        private const byte HIGH = 0x1;

        /// Number of pins on the stepper motor. Permits 2 or 4. 2 pins is a
        /// bipolar, and 4 pins is a unipolar.
        private MotorInterfaceType _interface;          // 0, 1, 2, 4, 8, See MotorInterfaceType

        /// Arduino pin number assignments for the 2 or 4 pins required to interface to the
        /// stepper motor or driver
        private Pin[] _pin  = new Pin[4];

        /// Whether the _pins is inverted or not
        private byte[] _pinInverted = new byte[4];

        /// The current absolution position in steps.
        private long _currentPos;    // Steps

        /// The target position in steps. The AccelStepper library will move the
        /// motor from the _currentPos to the _targetPos, taking into account the
        /// max speed, acceleration and deceleration
        private long _targetPos;     // Steps

        /// The current motos speed in steps per second
        /// Positive is clockwise
        private float _speed;         // Steps per second

        /// The maximum permitted speed in steps per second. Must be > 0.
        private float _maxSpeed;

        /// The acceleration to use to accelerate or decelerate the motor in steps
        /// per second per second. Must be > 0
        private float _acceleration;

        /// The current interval between steps in microseconds.
        /// 0 means the motor is currently stopped with _speed == 0
        private ulong _stepInterval;

        /// The last step time in microseconds
        private ulong _lastStepTime;

        /// The minimum allowed pulse width in microseconds
        private uint _minPulseWidth;

        /// Is the enable pin inverted?
        private byte _enableInverted;

        /// Enable pin for stepper driver, or 0xFF if unused.
        private Pin _enablePin;

        /// The pointer to a forward-step procedure
        public  delegate void ForwardHandler ();
        ForwardHandler _forward;

        /// The pointer to a backward-step procedure
        public delegate void BackwardHandler ();
        BackwardHandler _backward;

        // Function that provides microseconds since startup
        public delegate ulong GetMicroSecondsHandler();
        GetMicroSecondsHandler _getMicroSecondsHandler;

        /// The step counter for speed calculations
        private long _n;

        /// Initial step size in microseconds
        private float _c0;

        /// Last step size in microseconds
        private float _cn;

        /// Min step size in microseconds based on maxSpeed
        private float _cmin; // at max speed

        /// Current direction motor is spinning in
        /// Protected because some peoples subclasses need it to be so
        Direction _direction; // 1 == CW

        GpioController gpioController = new GpioController();
        const long TicksPerMicroSeconds = TimeSpan.TicksPerMillisecond / 1000;

        /// <summary>
        /// Constructor. You can have multiple simultaneous steppers, all moving
        /// at different speeds and accelerations, provided you call their run()
        /// functions at frequent enough intervals. Current Position is set to 0, target
        /// position is set to 0. MaxSpeed and Acceleration default to 1.0.
        /// The motor pins will be initialised to OUTPUT mode during the
        /// constructor by a call to enableOutputs().
        /// \param[in] interface Number of pins to interface to. Integer values are
        /// supported, but it is preferred to use the \ref MotorInterfaceType symbolic names. 
        /// AccelStepper::DRIVER (1) means a stepper driver (with Step and Direction pins).
        /// If an enable line is also needed, call setEnablePin() after construction.
        /// You may also invert the pins using setPinsInverted().
        /// AccelStepper::FULL2WIRE (2) means a 2 wire stepper (2 pins required). 
        /// AccelStepper::FULL3WIRE (3) means a 3 wire stepper, such as HDD spindle (3 pins required). 
        /// AccelStepper::FULL4WIRE (4) means a 4 wire stepper (4 pins required). 
        /// AccelStepper::HALF3WIRE (6) means a 3 wire half stepper, such as HDD spindle (3 pins required)
        /// AccelStepper::HALF4WIRE (8) means a 4 wire half stepper (4 pins required)
        /// Defaults to AccelStepper::FULL4WIRE (4) pins.
        /// </summary>
        /// <param name="motorInterface">Defines the motor type</param>
        /// <param name="pin1"></param>
        /// <param name="pin2"></param>
        /// <param name="pin3"></param>
        /// <param name="pin4"></param>
        /// <param name="enable">Defines whether the outputs are powered or unpowered after instantiation</param>
        /// <param name="getMicroSeconds">Custom time method. To be used when hardware allows time measurement with less latency then DateTime.UtcNow.Ticks</param>
        public AccelStepper(MotorInterfaceType motorInterface, byte pin1, byte pin2, byte pin3, byte pin4, bool enable, GetMicroSecondsHandler getMicroSeconds = null)
        {
            _interface = motorInterface;
            _currentPos = 0;
            _targetPos = 0;
            _speed = 0.0f;
            _maxSpeed = 1.0f;
            _acceleration = 0.0f;
            _stepInterval = 0;
            _minPulseWidth = 1;
            _enablePin = new Pin { Num = 0xff };
            _lastStepTime = 0;
            _pin[0] = new Pin { Num = pin1 };
            _pin[1] = new Pin { Num = pin2 };
            _pin[2] = new Pin { Num = pin3 };
            _pin[3] = new Pin { Num = pin4 };
            _enableInverted = 0;
            _getMicroSecondsHandler = getMicroSeconds ?? defaultGetMicroSeconds;

            // NEW
            _n = 0;
            _c0 = 0.0f;
            _cn = 0.0f;
            _cmin = 1.0f;
            _direction = Direction.DIRECTION_CCW;

            int i;
            for (i = 0; i < 4; i++)
                _pinInverted[i] = 0x00;
            if (enable)
                EnableOutputs();
            // Some reasonable default
            SetAcceleration(1);
        }

        /// <summary>
        /// Alternate Constructor which will call your own functions for forward and backward steps. 
        /// You can have multiple simultaneous steppers, all moving
        /// at different speeds and accelerations, provided you call their run()
        /// functions at frequent enough intervals. Current Position is set to 0, target
        /// position is set to 0. MaxSpeed and Acceleration default to 1.0.
        /// Any motor initialization should happen before hand, no pins are used or initialized.
        /// </summary>
        /// <param name="forward">forward void-returning procedure that will make a forward step</param>
        /// <param name="backward">backward void-returning procedure that will make a backward step</param>
        /// <param name="getMicrosecondsHandler">Custom time method. To be used when hardware allows time measurement with less latency then DateTime.UtcNow.Ticks</param>
        public AccelStepper(ForwardHandler forward, BackwardHandler backward, GetMicroSecondsHandler getMicrosecondsHandler)
        {
            _interface = 0;
            _currentPos = 0;
            _targetPos = 0;
            _speed = 0.0f;
            _maxSpeed = 1.0f;
            _acceleration = 0.0f;
            _stepInterval = 0;
            _minPulseWidth = 1;
            _enablePin = new Pin { Num = 0xff };
            _lastStepTime = 0;
            _pin[0] = new Pin();
            _pin[1] = new Pin();
            _pin[2] = new Pin();
            _pin[3] = new Pin();
            _forward = forward;
            _backward = backward;
            _getMicroSecondsHandler = getMicrosecondsHandler;

            // NEW
            _n = 0;
            _c0 = 0.0f;
            _cn = 0.0f;
            _cmin = 1.0f;
            _direction = Direction.DIRECTION_CCW;

            int i;
            for (i = 0; i < 4; i++)
                _pinInverted[i] = 0;
            // Some reasonable default
            SetAcceleration(1);
        }
        /// <summary>
        /// Set the target position. The run() function will try to move the motor (at most one step per call)
        /// from the current position to the target position set by the most
        /// recent call to this function. Caution: moveTo() also recalculates the speed for the next step. 
        /// If you are trying to use constant speed movements, you should call setSpeed() after calling moveTo().
        /// </summary>
        /// <param name="absolute">The desired absolute position. Negative is anticlockwise from the 0 position</param>
        public void MoveTo(long absolute)
        {
            if (_targetPos != absolute)
            {
                _targetPos = absolute;
                computeNewSpeed();
                // compute new n?
            }
        }

        /// <summary>
        /// Set the target position relative to the current position
        /// </summary>
        /// <param name="relative">The desired position relative to the current position. Negative is anticlockwise from the current position.</param>
        public void Move(long relative)
        {
            MoveTo(_currentPos + relative);
        }


        /// <summary>
        // Run the motor to implement speed and acceleration in order to proceed to the target position
        // You must call this at least once per step, preferably in your main loop
        // If the motor is in the desired position, the cost is very small
        // returns true if the motor is still running to the target position.
        /// </summary>
        /// <returns>true if the motor is still running to the target position</returns>
        public bool Run()
        {
            if (RunSpeed())
                computeNewSpeed();
            return _speed != 0.0 || DistanceToGo() != 0;
        }

        /// <summary>
        /// Poll the motor and step it if a step is due, implementing a constant
        /// speed as set by the most recent call to setSpeed(). You must call this as
        /// frequently as possible, but at least once per step interval,
        /// </summary>
        /// <returns>true if the motor was stepped</returns>
        public bool RunSpeed()
        {
            // Dont do anything unless we actually have a step interval
            if (_stepInterval == 0)
                return false;

            ulong time = micros();
            if (time - _lastStepTime >= _stepInterval)
            {
                if (_direction == Direction.DIRECTION_CW)
                {
                    // Clockwise
                    _currentPos += 1;
                }
                else
                {
                    // Anticlockwise  
                    _currentPos -= 1;
                }
                step(_currentPos);

                _lastStepTime = time; // Caution: does not account for costs in step()

                return true;
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// Sets the maximum permitted speed. The run() function will accelerate
        /// up to the speed set by this function.
        /// Caution: the maximum speed achievable depends on your processor and clock speed.
        /// The default maxSpeed is 1.0 steps per second.
        /// </summary>
        /// <param name="speed">The desired maximum speed in steps per second. Must be > 0. Caution: Speeds that exceed the maximum speed supported by the processor may result in non-linear accelerations and decelerations</param>
        public void SetMaxSpeed(float speed)
        {
            if (speed < 0.0)
                speed = -speed;
            if (_maxSpeed != speed)
            {
                _maxSpeed = speed;
                _cmin = 1000000.0f / speed;
                // Recompute _n from current speed and adjust speed if accelerating or cruising
                if (_n > 0)
                {
                    _n = (long)((_speed * _speed) / (2.0 * _acceleration)); // Equation 16
                    computeNewSpeed();
                }
            }
        }

        /// <summary>
        /// Returns the maximum speed configured for this stepper that was previously set by setMaxSpeed()
        /// </summary>
        /// <returns></returns>
        public float MaxSpeed()
        {
            return _maxSpeed;
        }

        /// <summary>
        /// Sets the acceleration/deceleration rate.
        /// </summary>
        /// <param name="acceleration">The desired acceleration in steps per second per second. Must be > 0.0. This is an expensive call since it requires a square root to be calculated. Dont call more often than needed</param>
        public void SetAcceleration(float acceleration)
        {
            if (acceleration == 0.0)
                return;
            if (acceleration < 0.0)
                acceleration = -acceleration;
            if (_acceleration != acceleration)
            {
                // Recompute _n per Equation 17
                _n = (long)(_n * (_acceleration / acceleration));
                // New c0 per Equation 7, with correction per Equation 15
                _c0 = (float)(0.676 * Math.Sqrt((float)(2.0 / acceleration)) * 1000000.0); // Equation 15
                _acceleration = acceleration;
                computeNewSpeed();
            }
        }

        /// <summary>
        /// Sets the desired constant speed for use with runSpeed().
        /// </summary>
        /// <param name="speed">The desired constant speed in steps per second. 
        /// Positive is clockwise. Speeds of more than 1000 steps per second are unreliable. 
        /// Very slow speeds may be set (eg 0.00027777 for once per hour, approximately. 
        /// Speed accuracy depends on the Arduino crystal. 
        /// Jitter depends on how frequently you call the runSpeed() function. 
        /// The speed will be limited by the current value of setMaxSpeed()</param>
        public void SetSpeed(float speed)
        {
            if (speed == _speed)
                return;
            speed = constrain(speed, -_maxSpeed, _maxSpeed);
            if (speed == 0.0)
                _stepInterval = 0;
            else
            {
                _stepInterval = (ulong)(Math.Abs(1000000.0f / speed));
                _direction = (speed > 0.0) ? Direction.DIRECTION_CW : Direction.DIRECTION_CCW;
            }
            _speed = speed;
        }

        /// <summary>
        /// The most recently set speed
        /// </summary>
        /// <returns>The most recent speed in steps per second</returns>
        public float Speed()
        {
            return _speed;
        }

        /// <summary>
        /// The distance from the current position to the target position.
        /// </summary>
        /// <returns>The distance from the current position to the target position
        /// in steps. Positive is clockwise from the current position.</returns>
        public long DistanceToGo()
        {
            return _targetPos - _currentPos;
        }

        /// <summary>
        /// The most recently set target position.
        /// </summary>
        /// <returns>The target position
        /// in steps. Positive is clockwise from the 0 position.</returns>
        public long TargetPosition()
        {
            return _targetPos;
        }

        /// <summary>
        /// The currently motor position.
        /// </summary>
        /// <returns>The target position
        /// in steps. Positive is clockwise from the 0 position.</returns>
        public long CurrentPosition()
        {
            return _currentPos;
        }

        /// <summary>
        /// Resets the current position of the motor, so that wherever the motor
        /// happens to be right now is considered to be the new 0 position. Useful
        /// for setting a zero position on a stepper after an initial hardware
        /// positioning move.
        /// Has the side effect of setting the current motor speed to 0.
        /// </summary>
        /// <param name="position">The position in steps of wherever the motor
        /// happens to be right now.</param>
        public void SetCurrentPosition(long position)
        {
            _targetPos = _currentPos = position;
            _n = 0;
            _stepInterval = 0;
            _speed = 0.0f;
        }

        /// <summary>
        /// Moves the motor (with acceleration/deceleration) 
        /// to the target position and blocks until it is at
        /// position. Dont use this in event loops, since it blocks.
        /// </summary>
        public void RunToPosition()
        {
            while (Run())
                ;
        }

        /// <summary>
        /// Runs at the currently selected speed until the target position is reached
        /// Does not implement accelerations.
        /// </summary>
        /// <returns>true if it stepped</returns>
        public bool RunSpeedToPosition()
        {
            if (_targetPos == _currentPos)
                return false;
            if (_targetPos > _currentPos)
                _direction = Direction.DIRECTION_CW;
            else
                _direction = Direction.DIRECTION_CCW;
            return RunSpeed();
        }

        // Blocks until the new target position is reached
        /// <summary>
        /// Moves the motor (with acceleration/deceleration)
        /// to the new target position and blocks until it is at
        /// position. Dont use this in event loops, since it blocks.
        /// </summary>
        /// <param name="position">The new target position.</param>
        public void RunToNewPosition(long position)
        {
            MoveTo(position);
            RunToPosition();
        }

        /// <summary>
        /// Sets a new target position that causes the stepper
        /// to stop as quickly as possible, using the current speed and acceleration parameters.
        /// </summary>
        public void Stop()
        {
            if (_speed != 0.0)
            {
                long stepsToStop = (long)((_speed * _speed) / (2.0 * _acceleration)) + 1; // Equation 16 (+integer rounding)
                if (_speed > 0)
                    Move(stepsToStop);
                else
                    Move(-stepsToStop);
            }
        }

        // Prevents power consumption on the outputs
        /// <summary>
        /// Disable motor pin outputs by setting them all LOW
        /// Depending on the design of your electronics this may turn off
        /// the power to the motor coils, saving power.
        /// This is useful to support Arduino low power modes: disable the outputs
        /// during sleep and then reenable with enableOutputs() before stepping
        /// again.
        /// If the enable Pin is defined, sets it to OUTPUT mode and clears the pin to disabled.
        /// </summary>
        public void DisableOutputs()
        {
            setOutputPins(0); // Handles inversion automatically
            if (_enablePin.Num != 0xff)
            {
                pinMode(_enablePin, PinMode.Output);
                digitalWrite(_enablePin, LOW ^ _enableInverted);
            }
        }

        /// <summary>
        /// Enable motor pin outputs by setting the motor pins to OUTPUT
        /// mode. Called automatically by the constructor.
        /// If the enable Pin is defined, sets it to OUTPUT mode and sets the pin to enabled.
        /// </summary>
        public void EnableOutputs()
        {
            pinMode(_pin[0], PinMode.Output);
            pinMode(_pin[1], PinMode.Output);
            if (_interface == MotorInterfaceType.FULL4WIRE || _interface == MotorInterfaceType.HALF4WIRE)
            {
                pinMode(_pin[2], PinMode.Output);
                pinMode(_pin[3], PinMode.Output);
            }
            else if (_interface == MotorInterfaceType.FULL3WIRE || _interface == MotorInterfaceType.HALF3WIRE)
            {
                pinMode(_pin[2], PinMode.Output);
            }

            if (_enablePin.Num != 0xff)
            {
                pinMode(_enablePin, PinMode.Output);
                digitalWrite(_enablePin, HIGH ^ _enableInverted);
            }
        }

        /// <summary>
        /// Sets the minimum pulse width allowed by the stepper driver. The minimum practical pulse width is 
        /// approximately 20 microseconds. Times less than 20 microseconds
        /// will usually result in 20 microseconds or so.
        /// </summary>
        /// <param name="minWidth">The minimum pulse width in microseconds.</param>
        public void SetMinPulseWidth(uint minWidth)
        {
            _minPulseWidth = minWidth;
        }

        /// <summary>
        /// Sets the enable pin number for stepper drivers.
        /// 0xFF indicates unused (default).
        /// Otherwise, if a pin is set, the pin will be turned on when 
        /// enableOutputs() is called and switched off when disableOutputs() 
        /// is called.
        /// </summary>
        /// <param name="enablePin">Digital pin number for motor enable</param>
        public void SetEnablePin(byte enablePin)
        {
            _enablePin.Num = enablePin;

            // This happens after construction, so init pin now.
            if (_enablePin.Num != 0xff)
            {
                pinMode(_enablePin, PinMode.Output);
                digitalWrite(_enablePin, HIGH ^ _enableInverted);
            }
        }

        /// <summary>
        /// Sets the inversion for stepper driver pins
        /// </summary>
        /// <param name="directionInvert">True for inverted direction pin, false for non-inverted</param>
        /// <param name="stepInvert">True for inverted step pin, false for non-inverted</param>
        /// <param name="enableInvert">True for inverted enable pin, false (default) for non-inverted</param>
        public void SetPinsInverted(bool directionInvert, bool stepInvert, bool enableInvert)
        {
            _pinInverted[0] = (byte)(stepInvert ? 0x01 : 0x00);
            _pinInverted[1] = (byte)(directionInvert ? 0x01 : 0x00);
            _enableInverted = enableInvert ? (byte)0x01 : (byte)0x00;
        }

        /// <summary>
        /// Sets the inversion for 2, 3 and 4 wire stepper pins
        /// </summary>
        /// <param name="pin1Invert">True for inverted pin1, false for non-inverted</param>
        /// <param name="pin2Invert">True for inverted pin2, false for non-inverted</param>
        /// <param name="pin3Invert">True for inverted pin3, false for non-inverted</param>
        /// <param name="pin4Invert">True for inverted pin4, false for non-inverted</param>
        /// <param name="enableInvert">True for inverted enable pin, false (default) for non-inverted</param>
        public void SetPinsInverted(bool pin1Invert, bool pin2Invert, bool pin3Invert, bool pin4Invert, bool enableInvert)
        {
            _pinInverted[0] = (byte)(pin1Invert ? 0x01 : 0x00);
            _pinInverted[1] = (byte)(pin2Invert ? 0x01 : 0x00); ;
            _pinInverted[2] = (byte)(pin3Invert ? 0x01 : 0x00); ;
            _pinInverted[3] = (byte)(pin4Invert ? 0x01 : 0x00); ;
            _enableInverted = enableInvert ? (byte)0x01 : (byte)0x00;
        }

        /// <summary>
        /// Checks to see if the motor is currently running to a target
        /// </summary>
        /// <returns>true if the speed is not zero or not at the target position</returns>
        public bool IsRunning()
        {
            return !(_speed == 0.0 && _targetPos == _currentPos);
        }

        private ulong micros()
        {
            return _getMicroSecondsHandler();
        }

        private void computeNewSpeed()
        {
            long distanceTo = DistanceToGo(); // +ve is clockwise from curent location

            long stepsToStop = (long)((_speed * _speed) / (2.0 * _acceleration)); // Equation 16

            if (distanceTo == 0 && stepsToStop <= 1)
            {
                // We are at the target and its time to stop
                _stepInterval = 0;
                _speed = 0.0f;
                _n = 0;
                return;
            }

            if (distanceTo > 0)
            {
                // We are anticlockwise from the target
                // Need to go clockwise from here, maybe decelerate now
                if (_n > 0)
                {
                    // Currently accelerating, need to decel now? Or maybe going the wrong way?
                    if ((stepsToStop >= distanceTo) || _direction == Direction.DIRECTION_CCW)
                        _n = -stepsToStop; // Start deceleration
                }
                else if (_n < 0)
                {
                    // Currently decelerating, need to accel again?
                    if ((stepsToStop < distanceTo) && _direction == Direction.DIRECTION_CW)
                        _n = -_n; // Start accceleration
                }
            }
            else if (distanceTo < 0)
            {
                // We are clockwise from the target
                // Need to go anticlockwise from here, maybe decelerate
                if (_n > 0)
                {
                    // Currently accelerating, need to decel now? Or maybe going the wrong way?
                    if ((stepsToStop >= -distanceTo) || _direction == Direction.DIRECTION_CW)
                        _n = -stepsToStop; // Start deceleration
                }
                else if (_n < 0)
                {
                    // Currently decelerating, need to accel again?
                    if ((stepsToStop < -distanceTo) && _direction == Direction.DIRECTION_CCW)
                        _n = -_n; // Start accceleration
                }
            }

            // Need to accelerate or decelerate
            if (_n == 0)
            {
                // First step from stopped
                _cn = _c0;
                _direction = (distanceTo > 0) ? Direction.DIRECTION_CW : Direction.DIRECTION_CCW;
            }
            else
            {
                // Subsequent step. Works for accel (n is +_ve) and decel (n is -ve).
                _cn = _cn - ((2.0f * _cn) / ((4.0f * _n) + 1)); // Equation 13
                _cn = Math.Max(_cn, _cmin);
            }
            _n++;
            _stepInterval = (ulong)_cn;
            _speed = 1000000.0f / _cn;
            if (_direction == Direction.DIRECTION_CCW)
                _speed = -_speed;

        }

        private float constrain(float amount, float low, float high)
        {
            return (amount < low) ? low : ((amount > high) ? high : amount);
        }

        // Subclasses can override
        private void step(long step)
        {
            switch (_interface)
            {
                case MotorInterfaceType.FUNCTION:
                    step0(step);
                    break;

                case MotorInterfaceType.DRIVER:
                    step1(step);
                    break;

                case MotorInterfaceType.FULL2WIRE:
                    step2(step);
                    break;

                case MotorInterfaceType.FULL3WIRE:
                    step3(step);
                    break;

                case MotorInterfaceType.FULL4WIRE:
                    step4(step);
                    break;

                case MotorInterfaceType.HALF3WIRE:
                    step6(step);
                    break;

                case MotorInterfaceType.HALF4WIRE:
                    step8(step);
                    break;
            }
        }

        // You might want to override this to implement eg serial output
        // bit 0 of the mask corresponds to _pin[0]
        // bit 1 of the mask corresponds to _pin[1]
        // ....
        private void setOutputPins(byte mask)
        {
            byte numpins = 2;
            if (_interface == MotorInterfaceType.FULL4WIRE || _interface == MotorInterfaceType.HALF4WIRE)
                numpins = 4;
            else if (_interface == MotorInterfaceType.FULL3WIRE || _interface == MotorInterfaceType.HALF3WIRE)
                numpins = 3;
            byte i;
            for (i = 0; i < numpins; i++)
                digitalWrite(_pin[i], ((mask & (1 << i)) > 0) ? (HIGH ^ _pinInverted[i]) : (LOW ^ _pinInverted[i]));
        }

        private void digitalWrite(Pin pin, PinValue value)
        {
            pin.GpioPin.Write(value);
        }

        // 0 pin step function (ie for functional usage)
        private void step0(long step)
        {
            if (_speed > 0)
                _forward();
            else
                _backward();
        }

        // 1 pin step function (ie for stepper drivers)
        // This is passed the current step number (0 to 7)
        // Subclasses can override
        private void step1(long step)
        {
            // _pin[0] is step, _pin[1] is direction
            setOutputPins(_direction == Direction.DIRECTION_CW ? (byte)0b10 : (byte)0b00); // Set direction first else get rogue pulses
            setOutputPins(_direction == Direction.DIRECTION_CW ? (byte)0b11 : (byte)0b01); // step HIGH
            // Caution 200ns setup time 
            // Delay the minimum allowed pulse width
            delayMicroseconds(_minPulseWidth);
            setOutputPins(_direction == Direction.DIRECTION_CW ? (byte)0b10 : (byte)0b00); // step LOW
        }

        private void delayMicroseconds(uint us)
        {
            ulong m = micros();
            if (us > 0)
            {
                ulong e = (m + us);
                if (m > e)
                { //overflow
                    while (micros() > e)
                    {
                        NOP();
                    }
                }
                while (micros() < e)
                {
                    NOP();
                }
            }
        }

        private void NOP()
        {
            ;
        }


        // 2 pin step function
        // This is passed the current step number (0 to 7)
        // Subclasses can override
        private void step2(long step)
        {
            switch (step & 0x3)
            {
                case 0: /* 01 */
                    setOutputPins(0b10);
                    break;

                case 1: /* 11 */
                    setOutputPins(0b11);
                    break;

                case 2: /* 10 */
                    setOutputPins(0b01);
                    break;

                case 3: /* 00 */
                    setOutputPins(0b00);
                    break;
            }
        }
        // 3 pin step function
        // This is passed the current step number (0 to 7)
        // Subclasses can override
        private void step3(long step)
        {
            switch (step % 3)
            {
                case 0:    // 100
                    setOutputPins(0b100);
                    break;

                case 1:    // 001
                    setOutputPins(0b001);
                    break;

                case 2:    //010
                    setOutputPins(0b010);
                    break;

            }
        }

        // 4 pin step function for half stepper
        // This is passed the current step number (0 to 7)
        // Subclasses can override
        private void step4(long step)
        {
            switch (step & 0x3)
            {
                case 0:    // 1010
                    setOutputPins(0b0101);
                    break;

                case 1:    // 0110
                    setOutputPins(0b0110);
                    break;

                case 2:    //0101
                    setOutputPins(0b1010);
                    break;

                case 3:    //1001
                    setOutputPins(0b1001);
                    break;
            }
        }

        // 3 pin half step function
        // This is passed the current step number (0 to 7)
        // Subclasses can override
        private void step6(long step)
        {
            switch (step % 6)
            {
                case 0:    // 100
                    setOutputPins(0b100);
                    break;

                case 1:    // 101
                    setOutputPins(0b101);
                    break;

                case 2:    // 001
                    setOutputPins(0b001);
                    break;

                case 3:    // 011
                    setOutputPins(0b011);
                    break;

                case 4:    // 010
                    setOutputPins(0b010);
                    break;

                case 5:    // 011
                    setOutputPins(0b110);
                    break;

            }
        }

        // 4 pin half step function
        // This is passed the current step number (0 to 7)
        // Subclasses can override
        private void step8(long step)
        {
            switch (step & 0x7)
            {
                case 0:    // 1000
                    setOutputPins(0b0001);
                    break;

                case 1:    // 1010
                    setOutputPins(0b0101);
                    break;

                case 2:    // 0010
                    setOutputPins(0b0100);
                    break;

                case 3:    // 0110
                    setOutputPins(0b0110);
                    break;

                case 4:    // 0100
                    setOutputPins(0b0010);
                    break;

                case 5:    //0101
                    setOutputPins(0b1010);
                    break;

                case 6:    // 0001
                    setOutputPins(0b1000);
                    break;

                case 7:    //1001
                    setOutputPins(0b1001);
                    break;
            }
        }

        private void pinMode(Pin pin, PinMode pinMode)
        {
           pin.GpioPin = gpioController.OpenPin(pin.Num, pinMode);
        }

        private long startTicks = DateTime.UtcNow.Ticks;
        private ulong defaultGetMicroSeconds()
        {
            return (ulong)((DateTime.UtcNow.Ticks - startTicks) / TicksPerMicroSeconds);
        }

    }
}
