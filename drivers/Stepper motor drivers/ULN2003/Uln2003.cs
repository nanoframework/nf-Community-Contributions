/*
 * ULN2003 stepper motor driver for nanoFramework
 * 
 * Original dotnet/iot source https://github.com/dotnet/iot/tree/main/src/devices/Uln2003
 * 
 * Change history:
 * 28 Mar 2021: Modified for nanoFramework by H Braasch
 *  9 Mar 2021: Non-blocking methods added by H Braasch
 */

using System;
using System.Device.Gpio;

namespace Device.ULN2003_NF
{
    /// <summary>
    /// This class is for controlling stepper motors that are controlled by a 4 pin controller board.
    /// </summary>
    /// <remarks>It is tested and developed using the 28BYJ-48 stepper motor and the ULN2003 driver board.</remarks>
    public class Uln2003 : IDisposable
    {
        /// <summary>
        /// The 28BYJ-48 motor has 512 full engine rotations to rotate the drive shaft once.
        /// In half-step mode these are 8 x 512 = 4096 steps for a full rotation.
        /// In full-step mode these are 4 x 512 = 2048 steps for a full rotation.
        /// </summary>
        public enum StepperMode
        {
            /// <summary>Half step mode</summary>
            HalfStep,

            /// <summary>Full step mode (single phase)</summary>
            FullStepSinglePhase,

            /// <summary>Full step mode (dual phase)</summary>
            FullStepDualPhase
        }

        /// <summary>
        /// Default delay in microseconds.
        /// </summary>
        private const long StepperMotorDefaultDelay = 1000;

        class ByteArray
        {
            public bool[] Column;

            public ByteArray(bool[] column)
            {
                Column = column;
            }
        }

        static ByteArray[] _halfStepSequenceRow = new ByteArray[4] {
            new ByteArray(new bool[8] { true, true, false, false, false, false, false, true }),
            new ByteArray(new bool[8] { false, true, true, true, false, false, false, false }),
            new ByteArray(new bool[8] { false, false, false, true, true, true, false, false }),
            new ByteArray(new bool[8] { false, false, false, false, false, true, true, true })};

        static ByteArray[] _fullStepSinglePhaseSequenceRow = new ByteArray[4] {
            new ByteArray(new bool[8] { true, false, false, false, true, false, false, false }),
            new ByteArray(new bool[8] { false, true, false, false, false, true, false, false }),
            new ByteArray(new bool[8] { false, false, true, false, false, false, true, false }),
            new ByteArray(new bool[8] { false, false, false, true, false, false, false, true })};

        static ByteArray[] _fullStepDualPhaseSequenceRow = new ByteArray[4] {
            new ByteArray(new bool[8] { true, false, false, true, true, false, false, true }),
            new ByteArray(new bool[8] { true, true, false, false, true, true, false, false }),
            new ByteArray(new bool[8] { false, true, true, false, false, true, true, false }),
            new ByteArray(new bool[8] { false, false, true, true, false, false, true, true })};

        private int _pin1;
        private int _pin2;
        private int _pin3;
        private int _pin4;
        private int _steps = 0;
        private int _engineStep = 0;
        private int _currentStep = 0;
        private int _stepsToRotate = 4096;
        private int _stepsToRotateInMode = 4096;
        private StepperMode _mode = StepperMode.HalfStep;
        private ByteArray[] _currentSwitchingSequenceRow = _halfStepSequenceRow;
        private bool _isClockwise = true;
        private GpioController _controller;
        private bool _shouldDispose;
        private IUln2003StopWatch _stopwatch;
        private long _stepMicrosecondsDelay;

        /// <summary>
        /// Initialize a Uln2003 class.
        /// </summary>
        /// <param name="pin1">The GPIO pin number which corresponds pin A on ULN2003 driver board.</param>
        /// <param name="pin2">The GPIO pin number which corresponds pin B on ULN2003 driver board.</param>
        /// <param name="pin3">The GPIO pin number which corresponds pin C on ULN2003 driver board.</param>
        /// <param name="pin4">The GPIO pin number which corresponds pin D on ULN2003 driver board.</param>
        /// <param name="controller">The controller.</param>
        /// <param name="shouldDispose">True to dispose the Gpio Controller</param>
        /// <param name="stepsToRotate">Amount of steps needed to rotate motor once in HalfStepMode.</param>
        public Uln2003(int pin1, int pin2, int pin3, int pin4, IUln2003StopWatch stopwatch = null, GpioController controller = null, bool shouldDispose = true, int stepsToRotate = 4096)
        {
            _pin1 = pin1;
            _pin2 = pin2;
            _pin3 = pin3;
            _pin4 = pin4;

            _controller = controller ?? new GpioController();
            _shouldDispose = shouldDispose || controller is null;
            _stepsToRotate = stepsToRotate;

            _controller.OpenPin(_pin1, PinMode.Output);
            _controller.OpenPin(_pin2, PinMode.Output);
            _controller.OpenPin(_pin3, PinMode.Output);
            _controller.OpenPin(_pin4, PinMode.Output);

            
            _stopwatch = stopwatch?? new DefaultStopwatch();
        }

        /// <summary>
        /// Sets the motor speed to revolutions per minute.
        /// </summary>
        /// <remarks>Default revolutions per minute for 28BYJ-48 is approximately 15.</remarks>
        public float SpeedAsRPM { get; set; }

        private const float DEG_PER_SEC2RPM = 0.166667f;
        public float SpeedAsDegPerSec 
        { 
            get {
                return SpeedAsRPM / DEG_PER_SEC2RPM;
            } 
            set {
                SpeedAsRPM = DEG_PER_SEC2RPM * value;
            } 
        }

        /// <summary>
        /// Sets the stepper's mode.
        /// </summary>
        public StepperMode Mode
        {
            get => _mode;
            set
            {
                _mode = value;

                switch (_mode)
                {
                    case StepperMode.HalfStep:
                        _currentSwitchingSequenceRow = _halfStepSequenceRow;
                        _stepsToRotateInMode = _stepsToRotate;
                        break;
                    case StepperMode.FullStepSinglePhase:
                        _currentSwitchingSequenceRow = _fullStepSinglePhaseSequenceRow;
                        _stepsToRotateInMode = _stepsToRotate / 2;
                        break;
                    case StepperMode.FullStepDualPhase:
                        _currentSwitchingSequenceRow = _fullStepDualPhaseSequenceRow;
                        _stepsToRotateInMode = _stepsToRotate / 2;
                        break;
                }
            }
        }

        /// <summary>
        /// Stop the motor.
        /// </summary>
        public void Stop()
        {
            _steps = 0;
            _stopwatch?.Stop();
            _controller.Write(_pin1, PinValue.Low);
            _controller.Write(_pin2, PinValue.Low);
            _controller.Write(_pin3, PinValue.Low);
            _controller.Write(_pin4, PinValue.Low);
        }

        /// <summary>
        /// Moves the motor. If the number is negative, the motor moves in the reverse direction. This method blocks execution.
        /// </summary>
        /// <param name="steps">Number of steps.</param>
        public void StepAndWait(int steps)
        {
            double lastStepTime = 0;
            _stopwatch.Restart();
            _isClockwise = steps >= 0;
            _steps = Math.Abs(steps);
            _stepMicrosecondsDelay = SpeedAsRPM > 0 ? (long)(60 * 1000 * 1000 / _stepsToRotateInMode / SpeedAsRPM) : StepperMotorDefaultDelay;
            _currentStep = 0;

            while (_currentStep < _steps)
            {
                double elapsedMicroseconds = _stopwatch.TotalMicroSeconds;

                if (elapsedMicroseconds - lastStepTime >= _stepMicrosecondsDelay)
                {
                    lastStepTime = elapsedMicroseconds;

                    if (_isClockwise)
                    {
                        _engineStep = _engineStep - 1 < 1 ? 8 : _engineStep - 1;
                    }
                    else
                    {
                        _engineStep = _engineStep + 1 > 8 ? 1 : _engineStep + 1;
                    }

                    ApplyEngineStep();
                    _currentStep++;
                }
            }
        }

        double lastStepTime = 0;
        /// <summary>
        /// Moves the motor. If the number is negative, the motor moves in the reverse direction. 
        /// This method does NOT blocks execution.
        /// This method MUST be used in conjunction with the IsDone() and Run() methods.
        /// </summary>
        /// <param name="steps">Number of steps.</param>
        public void StepNoWait(int steps)
        {
            
            _stopwatch.Restart();
            _isClockwise = steps >= 0;
            _steps = Math.Abs(steps);
            _stepMicrosecondsDelay = SpeedAsRPM > 0 ? (long)(60 * 1000 * 1000 / _stepsToRotateInMode / SpeedAsRPM) : StepperMotorDefaultDelay;
            _currentStep = 0;
            lastStepTime = 0;
        }

        /// <summary>
        //  Indicates if target position is reached
        /// </summary>
        /// <returns></returns>
        public bool IsTargetPositionReached()
        {
            return _currentStep >= _steps;
        }

        /// <summary>
        // Run the motor to implement speed in order to reach the target position
        // You must call this at least once per step, preferably in your main loop.
        /// </summary>
        /// <returns>true if the motor was stepped</returns>
        public bool Run()
        {
            double elapsedMicroseconds = _stopwatch.TotalMicroSeconds;

            if (elapsedMicroseconds - lastStepTime >= _stepMicrosecondsDelay)
            {
                lastStepTime = elapsedMicroseconds;

                if (_isClockwise)
                {
                    _engineStep = _engineStep - 1 < 1 ? 8 : _engineStep - 1;
                }
                else
                {
                    _engineStep = _engineStep + 1 > 8 ? 1 : _engineStep + 1;
                }

                ApplyEngineStep();
                _currentStep++;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Rotates the motor. If the number is negative, the motor moves in the reverse direction.
        /// </summary>
        /// <param name="rotations">Number of rotations.</param>
        public void Rotate(int rotations)
        {
            StepAndWait(rotations * _stepsToRotateInMode);
        }

        private void ApplyEngineStep()
        {
            _controller.Write(_pin1, _currentSwitchingSequenceRow[0].Column[_engineStep - 1]);
            _controller.Write(_pin2, _currentSwitchingSequenceRow[1].Column[_engineStep - 1]);
            _controller.Write(_pin3, _currentSwitchingSequenceRow[2].Column[_engineStep - 1]);
            _controller.Write(_pin4, _currentSwitchingSequenceRow[3].Column[_engineStep - 1]);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Stop();
            if (_shouldDispose)
            {
                _controller?.Dispose();
                _controller = null;
            }
        }

        public interface IUln2003StopWatch
        {
            void Stop();
            void Restart();
            ulong TotalMicroSeconds { get; }
        }

        private class DefaultStopwatch : IUln2003StopWatch
        {

            long startTicks;
            long stopTicks;
            bool isRunning = false;
            int ticksPerMicrosecond = (int)TimeSpan.TicksPerMillisecond / 1000;
            
            public void Restart()
            {
                startTicks = DateTime.UtcNow.Ticks;
                isRunning = true;
            }

            public void Stop()
            {
                isRunning = false;
                stopTicks = DateTime.UtcNow.Ticks;
            }
            public ulong TotalMicroSeconds => isRunning ? (ulong)((DateTime.UtcNow.Ticks - startTicks) / ticksPerMicrosecond) : (ulong)((stopTicks - startTicks) / ticksPerMicrosecond);

        }
    }


}
