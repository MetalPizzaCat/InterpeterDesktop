/**
Dear reader BEWARE! 
The idea behind this emulator is not greatest as instead of using actual opcodes during execution this program instead generates
an execution list with it runs instead
This approach is rather inefficient, but it was done due to how opcodes in intel 8080 are designed'
To save up on memory it has an opcode for every variation of a command, instead of encoding that in an additional byte
Because of this, executing directly from codes would imply having a case for every variation(yes it could be accounted for because of HOW
they are placed in the op code table, but by the time i'm writing this message it would be rather inefficient to turn back(?))
*/

//TODO: Refactor how opcodes are handled cause using classes for every operation is clunky
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Timers;
using System.ComponentModel;
using System.Collections.ObjectModel;
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

    public class Interpreter : INotifyPropertyChanged
    {
        private static List<string> _registerNames = new List<string> { "b", "c", "d", "e", "h", "l", "m", "a" };
        public delegate void OutPortValueChangedEventHandler(int port, byte value);
        public event OutPortValueChangedEventHandler? OnOutPortValueChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        private Timer _timer;
        /// <summary>
        /// Program counter value of the processor, tells which operation is executed<para/>
        /// Only exists as a representation since _operationCounter is the actual value used for picking operations 
        /// </summary>
        private int _programCounter = 0;

        private Registers _registers;
        private ProcessorFlags _flags;

        public ProcessorFlags Flags => _flags;
        public Registers Registers => _registers;

        private Memory _memory;

        public Memory Memory { get => _memory; set => _memory = value; }

        private byte[] _outputPorts = new byte[16];

        public byte[] OutputPorts => _outputPorts;

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
                // M register is a special case, as it is not in fact a register, but rather a memory cell at HL 
                case "m":
                    {
                        ushort address = (ushort)(((int)Registers.H << 8) | (int)Registers.L);
                        return Memory[address];
                    }
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
                // M register is a special case, as it is not in fact a register, but rather a memory cell at HL 
                case "m":
                    {
                        ushort address = (ushort)(((int)Registers.H << 8) | (int)Registers.L);
                        Memory[address] = value;
                    }
                    break;
                default:
                    throw new InterpreterInvalidRegisterException($"Register {name} is not part of the processor");
            }
        }


        public void SetCode(ProcessedCodeInfo code)
        {
            _jumpDestinations = code.JumpDestinations;
            _memory.ProtectedMemoryLength = code.Length;
            _memory.WriteRom(code.CommandBytes.ToArray());
        }

        /// <summary>
        /// Pushes value stored in the pair of registers advancing the stack pointer
        /// </summary>
        /// <param name="pairName"></param>
        public void PushStack(string pairName)
        {
            byte h = 0;
            byte l = 0;
            switch (pairName.ToLower())
            {
                case "b":
                    h = Registers.B;
                    l = Registers.C;
                    break;
                case "d":
                    h = Registers.D;
                    l = Registers.E;
                    break;
                case "h":
                    h = Registers.H;
                    l = Registers.L;
                    break;
            }
            ushort sp = _memory.StackPointer;
            _memory[sp] = h;
            _memory[(ushort)(sp - 1)] = l;
            sp -= 2;
            _memory.StackPointer = sp;
        }


        public void PopStack(string destinationPairName)
        {
            ushort sp = _memory.StackPointer;
            byte h = _memory[(ushort)(sp + 2)];
            byte l = _memory[(ushort)(sp + 1)];
            switch (destinationPairName)
            {
                case "b":
                    Registers.B = h;
                    Registers.C = l;
                    break;
                case "d":
                    Registers.D = h;
                    Registers.E = l;
                    break;
                case "h":
                    Registers.H = h;
                    Registers.L = l;
                    break;
            }
            sp += 2;
            _memory.StackPointer = sp;
        }

        public Interpreter()
        {
            _registers = new Registers();
            _flags = new ProcessorFlags();
            //TODO: uncomment to get access to full 64kb
            //_memory = new Memory(0, ushort.MaxValue, 0);
            _memory = new Memory();
            _timer = new Timer(300);
            _timer.Enabled = false;
            _timer.Elapsed += _onTimerTimeout;
        }

        public void SetOut(int port, byte value)
        {
            //because ports are meant to be dynamic this technically does write to that port
            //there is just
            //no output for it
            if (port >= _outputPorts.Length || port < 0)
            {
                return;
            }
            _outputPorts[port] = value;
            OnOutPortValueChanged?.Invoke(port, value);
        }

        public void JumpTo(string destination)
        {
            ProgramCounter = _jumpDestinations[destination];
        }

        public int ProgramCounter
        {
            get => _programCounter;
            set
            {
                _programCounter = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ProgramCounter"));
            }
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

        private bool _mov(byte op)
        {
            int movCheck = op & 0xF0;
            int movSubCheck = op & 0x0F;
            switch (movCheck)
            {
                case 0x40:
                    SetRegisterValue(movSubCheck > 0x7 ? "c" : "b", GetRegisterValue(_registerNames[movSubCheck > 0x7 ? movSubCheck - 0x8 : movSubCheck]));
                    break;
                case 0x50:
                    SetRegisterValue(movSubCheck > 0x7 ? "e" : "d", GetRegisterValue(_registerNames[movSubCheck > 0x7 ? movSubCheck - 0x8 : movSubCheck]));
                    break;
                case 0x60:
                    SetRegisterValue(movSubCheck > 0x7 ? "l" : "h", GetRegisterValue(_registerNames[movSubCheck > 0x7 ? movSubCheck - 0x8 : movSubCheck]));
                    break;
                case 0x70:
                    SetRegisterValue(movSubCheck > 0x7 ? "a" : "m", GetRegisterValue(_registerNames[movSubCheck > 0x7 ? movSubCheck - 0x8 : movSubCheck]));
                    break;
                default:
                    return false;
            }
            ProgramCounter++;
            return true;
        }

        private void _jmp()
        {
            ushort dest = (ushort)(_memory[(ushort)(ProgramCounter + 1)] | _memory[(ushort)(ProgramCounter + 2)] << 8);
            ProgramCounter = dest;
            ProgramCounter += 3;
        }

        private bool _add(byte op)
        {
            int high = op & 0xf0;
            int low = op & 0x0f;
            if (high != 0x80) // add commands are in 0x80 column
            {
                return false;
            }

            byte value = GetRegisterValue(_registerNames[low > 0x7 ? low - 0x8 : low]);
            Registers.A += (byte)(value + (low > 0x7 ? (Flags.C ? 1 : 0) : 0));
            ProgramCounter++;
            return true;
        }

        private void _compare(byte value)
        {
            int result = Registers.A - value;
            Flags.Z = result == 0;
            Flags.S = 0x80 == (result & 0x80);
            Flags.P = Parity((ushort)result);
            Flags.C = Registers.A < value;
        }

        private bool _sub(byte op)
        {
            int high = op & 0xf0;
            int low = op & 0x0f;
            if (high != 0x90) // add commands are in 0x80 column
            {
                return false;
            }

            byte value = GetRegisterValue(_registerNames[low > 0x7 ? low - 0x8 : low]);
            Registers.A -= (byte)(value + (low > 0x7 ? (Flags.C ? 1 : 0) : 0));
            ProgramCounter++;

            return true;
        }

        public void Step()
        {
            byte op = _memory[(ushort)ProgramCounter];


            switch (op)
            {
                case 0:
                    break;
                case 0x06://mvi b
                    Registers.B = _memory[(ushort)(ProgramCounter + 1)];
                    ProgramCounter += 2;
                    break;
                case 0x0E://mvi c
                    Registers.C = _memory[(ushort)(ProgramCounter + 1)];
                    ProgramCounter += 2;
                    break;
                case 0x16://mvi d
                    Registers.D = _memory[(ushort)(ProgramCounter + 1)];
                    ProgramCounter += 2;
                    break;
                case 0x1e: //mvi e
                    Registers.E = _memory[(ushort)(ProgramCounter + 1)];
                    ProgramCounter += 2;
                    break;
                case 0x26: //mvi h
                    Registers.H = _memory[(ushort)(ProgramCounter + 1)];
                    ProgramCounter += 2;
                    break;
                case 0x2e: //mvi e
                    Registers.L = _memory[(ushort)(ProgramCounter + 1)];
                    ProgramCounter += 2;
                    break;
                case 0x36: //mvi m
                    SetRegisterValue("M", _memory[(ushort)(ProgramCounter + 1)]);
                    ProgramCounter += 2;
                    break;
                case 0x3e: //mvi a
                    Registers.A = _memory[(ushort)(ProgramCounter + 1)];
                    ProgramCounter += 2;
                    break;
                case 0xc6://adi
                    Registers.A += _memory[(ushort)(ProgramCounter + 1)];
                    ProgramCounter += 2;
                    break;
                case 0xce://aci
                    Registers.A += (byte)(_memory[(ushort)(ProgramCounter + 1)] + (Flags.C ? 1 : 0));
                    ProgramCounter += 2;
                    break;
                case 0xd6://sui
                    Registers.A -= _memory[(ushort)(ProgramCounter + 1)];
                    ProgramCounter += 2;
                    break;
                case 0xde://sbi
                    Registers.A -= (byte)(_memory[(ushort)(ProgramCounter + 1)] + (Flags.C ? 1 : 0));
                    ProgramCounter += 2;
                    break;
                case 0xc3://jump
                    _jmp();
                    break;
                case 0xca://jz
                    if (_flags.Z)
                    {
                        _jmp();
                    }
                    break;
                case 0xc2://jnz
                    if (!_flags.Z)
                    {
                        _jmp();
                    }
                    break;
                case 0xf2://jp
                    if (!_flags.S)
                    {
                        _jmp();
                    }
                    break;
                case 0xfa://jm
                    if (_flags.S)
                    {
                        _jmp();
                    }
                    break;
                case 0xda://jc
                    if (_flags.C)
                    {
                        _jmp();
                    }
                    break;
                case 0xd2://jnc
                    if (!_flags.C)
                    {
                        _jmp();
                    }
                    break;
                case 0xea://jpe
                    if (!_flags.P)
                    {
                        _jmp();
                    }
                    break;
                case 0xe2://jpo
                    if (_flags.P)
                    {
                        _jmp();
                    }
                    break;
                case 0xc5://push b
                    PushStack("b");
                    ProgramCounter++;
                    break;
                case 0xd5://push d
                    PushStack("d");
                    ProgramCounter++;
                    break;
                case 0xe5://push h
                    PushStack("h");
                    ProgramCounter++;
                    break;
                case 0xc1://pop b
                    PopStack("b");
                    ProgramCounter++;
                    break;
                case 0xd1://pop d
                    PopStack("d");
                    ProgramCounter++;
                    break;
                case 0xe1://pop h
                    PopStack("h");
                    ProgramCounter++;
                    break;
                case 0x37:
                    Flags.C = true;
                    ProgramCounter++;
                    break;
                case 0x32: // sta
                    {
                        ushort dest = (ushort)(_memory[(ushort)(ProgramCounter + 1)] | _memory[(ushort)(ProgramCounter + 2)] << 8);
                        _memory[dest] = Registers.A;
                        ProgramCounter += 3;
                    }
                    break;
                case 0x3A: // lda
                    {
                        ushort dest = (ushort)(_memory[(ushort)(ProgramCounter + 1)] | _memory[(ushort)(ProgramCounter + 2)] << 8);
                        Registers.A = _memory[dest];
                        ProgramCounter += 3;
                    }
                    break;
                case 0x76://hlt
                    Stop();
                    Console.WriteLine("Finished execution");
                    break;
                default:
                    if (_mov(op)) { break; }
                    if (_add(op)) { break; }
                    if (_sub(op)) { break; }
                    throw new Exception("Processor encountered unrecognized opcode");
            }

            if (ProgramCounter >= _memory.MemoryData.TotalSize)
            {
                _timer.Enabled = false;
                Console.WriteLine("Finished execution because program counter run outside of memory");
                return;
            }
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
            _programCounter = 0;
            _registers.Reset();
            _memory.Reset();
            _flags.Reset();
        }

        public void Run()
        {
            Step();
            _timer.Enabled = true;
            _timer.AutoReset = true;
        }
    }
}