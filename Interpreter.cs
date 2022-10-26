using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Timers;
using System.ComponentModel;
using System.Collections.Generic;

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

        public ProcessorFlags Flags => _flags;
        public Registers Registers => _registers;

        private Memory _memory;

        public Memory Memory { get => _memory; set => _memory = value; }

        private List<OperationBase> _operations = new List<OperationBase>();

        public List<OperationBase> Operations { get => _operations; set => _operations = value; }

        /// <summary>
        /// List of all available jump destinations
        /// </summary>
        public Dictionary<string, int> _jumpDestinations = new Dictionary<string, int>();

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

        public void SetCode(ProcessedCodeInfo code)
        {
            _jumpDestinations = code.JumpDestinations;
            _memory.ProtectedMemoryLength = code.Length;
            _operations = code.Operations;
        }

        public Interpreter()
        {
            _registers = new Registers();
            _flags = new ProcessorFlags();
            //TODO: uncomment to get access to full 64kb
            //_memory = new Memory(0, ushort.MaxValue, 0);
            _memory = new Memory();
            _timer = new Timer(100.0);
            _timer.Enabled = false;
            _timer.Elapsed += _onTimerTimeout;
        }

        public void JumpTo(string destination)
        {
            _operationCounter = _jumpDestinations[destination];
        }

        /// <summary>
        /// Checks parity of the number by check if the amount of 1s in the number is even <para/>
        /// I have no idea what is the parity for, but original processor had it so why not
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public bool Parity(ushort val)
        {
            int count = 0;
            for (int i = 0; i < 8; i++)
            {
                count += val & 1;
                val >>= 1;
            }
            return (count % 2) == 0;
        }

        /// <summary>
        /// Updates flags based on the value
        /// </summary>
        public void CheckFlags(ushort value)
        {
            //everything except carry works on one cell sized value
            ushort clean = (ushort)(value & 0xff);
            _flags.S = 0x80 == (clean & 0x80);//just check if the sign bit is true
            _flags.Z = clean == 0;
            _flags.Ac = clean > 0x09;
            _flags.P = Parity(clean);
            _flags.C = value > 0xff;
        }

        /// <summary>
        /// Set group of flags, intended for cmp operation
        /// </summary>
        /// <param name="z"></param>
        /// <param name="s"></param>
        /// <param name="p"></param>
        /// <param name="cy"></param>
        public void SetFlags(bool z, bool s, bool p, bool c)
        {
            _flags.Z = z;
            _flags.S = s;
            _flags.P = p;
            _flags.C = c;
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

        public void Stop()
        {
             _timer.Enabled = false;
        }

        private void _onTimerTimeout(object? source, ElapsedEventArgs e)
        {
            Step();
        }

        public void ResetProcessor()
        {
            _registers.Reset();
            _memory.Reset();
            _flags.Reset();
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