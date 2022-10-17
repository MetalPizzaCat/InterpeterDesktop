using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Timers;

namespace Interpreter
{
    [System.Serializable]
    public class InterpreterInvalidRegisterException : System.Exception
    {
        public InterpreterInvalidRegisterException() { }
        public InterpreterInvalidRegisterException(string message) : base(message) { }
        public InterpreterInvalidRegisterException(string message, System.Exception inner) : base(message, inner) { }
        protected InterpreterInvalidRegisterException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public class ProcessorFlags
    {
        public bool S = false;
        public bool Z = false;
        public bool Ac = false;
        public bool P = false;
        public bool C = false;
    }

    public class Registers
    {
        public byte A { get; set; } = 0;
        public byte B { get; set; } = 0;
        public byte C { get; set; } = 0;
        public byte D { get; set; } = 0;
        public byte E { get; set; } = 0;
        public byte H { get; set; } = 0;
        public byte L { get; set; } = 0;
    }
    public class Interpreter
    {

        private Timer _timer;
        /// <summary>
        /// Program counter value of the processor, tells which operation is executed<para/>
        /// Only exists as a representation since _operationCounter is the actual value used for picking operations 
        /// </summary>
        private int _programCounter = 0;
        /// <summary>
        /// Stack pointer of the processor
        /// </summary>
        private int _stackPointer = 0;
        /// <summary>
        /// Id of currently executed operation
        /// </summary>
        private int _operationCounter = 0;

        private Registers _registers;
        private ProcessorFlags _flags;

        private Memory _memory;

        public Memory Memory { get => _memory; set => _memory = value; }

        private List<OperationBase> _operations = new List<OperationBase>();

        public List<OperationBase> Operations { get => _operations; set => _operations = value; }

        /// <summary>
        /// Helper function that returns value stored in the register<para/>
        /// If name is invalid InterpreterInvalidRegisterException is thrown
        /// </summary>
        /// <param name="name">Name of the register</param>
        /// <returns></returns>
        public byte GetRegisterValue(string name)
        {
            name = name.ToLower();
            switch (name)
            {
                case "a":
                    return _registers.A;
                case "b":
                    return _registers.B;
                case "c":
                    return _registers.C;
                case "d":
                    return _registers.D;
                case "e":
                    return _registers.E;
                case "h":
                    return _registers.H;
                case "l":
                    return _registers.L;
                default:
                    throw new InterpreterInvalidRegisterException($"Register {name} is not part of the processor");
            }
        }

        /// <summary>
        /// Helper function that sets value stored in the register<para/>
        /// If name is invalid InterpreterInvalidRegisterException is thrown
        /// </summary>
        /// <param name="name">Name of the register</param>
        /// <param name="value">Value that will be assigned</param>
        /// <returns></returns>
        public void SetRegisterValue(string name, byte value)
        {
            name = name.ToLower();
            switch (name)
            {
                case "a":
                    _registers.A = value;
                    break;
                case "b":
                    _registers.B = value;
                    break;
                case "c":
                    _registers.C = value;
                    break;
                case "d":
                    _registers.D = value;
                    break;
                case "e":
                    _registers.E = value;
                    break;
                case "h":
                    _registers.H = value;
                    break;
                case "l":
                    _registers.L = value;
                    break;
                default:
                    throw new InterpreterInvalidRegisterException($"Register {name} is not part of the processor");
            }
        }

        public Interpreter()
        {
            _registers = new Registers();
            _flags = new ProcessorFlags();
            _memory = new Memory();
            _timer = new Timer(300.0);
            _timer.Enabled = false;
            _timer.Elapsed += _onTimerTimeout;
        }

        public void Step()
        {
            if (_operationCounter >= _operations.Count)
            {
                _timer.Enabled = false;
                Console.WriteLine("Finished execution");
                return;
                //throw new NullReferenceException("Program run out of operations but HALT was not executed");
            }
            _operations[_operationCounter].Execute();
            _operationCounter++;
        }

        private void _onTimerTimeout(object? source, ElapsedEventArgs e)
        {
            Step();
        }

        public void Run()
        {
            Step();
            _operationCounter = 0;
            _timer.Enabled = true;
            _timer.AutoReset = true;
        }
    }
}