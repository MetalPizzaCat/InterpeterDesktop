#define ALLOW_WRITES_OUTSIDE_RAM

using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;
using System.Linq;
using Newtonsoft.Json;

namespace Emulator
{
    /// <summary>
    /// Thrown when converter encounters invalid operation line
    /// </summary>
    [System.Serializable]
    public class InterpreterInvalidOperationException : System.Exception
    {
        public InterpreterInvalidOperationException() { }
        public InterpreterInvalidOperationException(string message) : base(message) { }
        public InterpreterInvalidOperationException(string message, System.Exception inner) : base(message, inner) { }
        protected InterpreterInvalidOperationException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public class ProcessedCodeInfo
    {
        public Dictionary<string, int> JumpDestinations;
        /// <summary>
        /// How many bytes of memory this program will occupy
        /// </summary>
        public int Length;

        /// <summary>
        /// List of bytes that represent the actual program 
        /// </summary>
        public List<byte> CommandBytes;

        public Dictionary<int, string> Errors;

        /// <summary>
        /// List of all string literals written by developer. Used for avoiding to have manually type out text in ROM
        /// </summary>
        public Dictionary<int, string> StringLiterals;
        public bool Success;

        public ProcessedCodeInfo()
        {
            JumpDestinations = new Dictionary<string, int>();
            Length = 0;
            CommandBytes = new List<byte>();
            Errors = new Dictionary<int, string>();
            Success = false;
            StringLiterals = new Dictionary<int, string>();
        }
    }

    public class Converter
    {

        private static List<string> _registerNames = new List<string> { "b", "c", "d", "e", "h", "l", "m", "a" };
        private static List<string> _registerPairNames = new List<string> { "b", "d", "h" };
        public static Regex CommentRegex = new Regex("( *)(;)(.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Regex OperationSeparator = new Regex(@"([A-z\d]+)|('(([A-z\d]+)( *|,)+([A-z\d]+)*)')", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Regex LabelDefinitionRegex = new Regex(@"(([A-z]|\d)+(?=:))", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Regex ShortNumberRegex = new Regex(@"^((0x((\d|[A-F]){1,4}))|(\d{1,5}))$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Regex LabelRegex = new Regex(@"([A-z]|\d)+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public Converter(string code, MemorySegmentationData memory)
        {
            _code = code;
            _memory = memory;
            _result = new ProcessedCodeInfo();
            string infoText = System.IO.File.ReadAllText("./Configuration/CommandInfo.json");
            _info = JsonConvert.DeserializeObject<ProcessorCommandsInfo>(infoText) ?? throw new NullReferenceException("Unable to process configuration");
            _success = _prepare();
        }

        private MemorySegmentationData _memory;
        private string _code;
        private ProcessorCommandsInfo _info;
        private ProcessedCodeInfo _result;
        private bool _success = false;
        //jumps that were referred to by call/jump commands
        //used for checking if jump destination is valid at assemble time
        private Dictionary<int, string> _referredJumps = new Dictionary<int, string>();
        private Dictionary<int, int> _referredAddresses = new Dictionary<int, int>();
        //Key is jump label, value is where to place address of the jump label
        private Dictionary<int, string> _jumps = new Dictionary<int, string>();
        //List of all user defined assemble time constants. They function similarly to #define in c++
        private Dictionary<string, string> _defines = new Dictionary<string, string>();

        public ProcessedCodeInfo? Result => _success ? _result : null;
        public Dictionary<int, string> Errors => _result.Errors;
        public bool Success => _success;

        /// <summary>
        /// Checks if given collection of regex matches is a valid operation
        /// </summary>
        /// <param name="input">Collection of reg ex matches that should contain [OPNAME], [Arg1] ...[ArgN]</param>
        /// <param name="_info">Processor commands info config that has information about commands</param>
        /// <returns>Null if no errors were found, or error message</returns>
        private string? _checkInputValidity(MatchCollection input)
        {
            if (_info.Commands.ContainsKey(input[0].Value))
            {
                int argumentCount = _info.Commands[input[0].Value].Arguments.Count;
                if (argumentCount != input.Count - 1)
                {
                    return $"Operation expected {argumentCount} found {input.Count - 1}";
                }
                for (int i = 1; i < input.Count; i++)
                {
                    switch (_info.Commands[input[0].Value].Arguments[i - 1])
                    {
                        case CommandArgumentType.RegisterName:
                            if (!Regex.IsMatch(input[i].Value, "[A-z]"))
                            {
                                return ($"Argument {i - 1} can only contain one letter: name of the register");
                            }
                            break;
                        case CommandArgumentType.Int8:
                            if (!Regex.IsMatch(input[i].Value, @"(0x(\d|[A-F]){1,2})|(\d{1,3})"))
                            {
                                return ($"Argument {i - 1} can only contain numbers");
                            }
                            break;
                        case CommandArgumentType.Int16:
                            if (!Regex.IsMatch(input[i].Value, "[A-z]+") && !ShortNumberRegex.IsMatch(input[i].Value))
                            {
                                return ($"Argument {i - 1} can only contain name of the label or 16bit address");
                            }
                            break;
                    }
                }
            }
            else
            {
                return ($"Unknown operation encountered: {input[0]}");
            }
            return null;
        }


        private void _processDb(int lineId, MatchCollection matches)
        {
            foreach (Match match in matches.Skip(1))
            {
                try
                {
                    if (ShortNumberRegex.IsMatch(match.Value))
                    {
                        _result.CommandBytes.Add(Convert.ToByte(match.Value, 16));//write the argument
                    }
                    else if (Regex.IsMatch(match.Value, @"(0x(\d|[A-F]){1,2})|(\d{1,3})"))
                    {
                        _result.CommandBytes.Add(Convert.ToByte(match.Value));//write the argument
                    }
                    else
                    {
                        _result.Errors.Add(lineId, "Unexpected value encountered");
                    }
                }
                catch (OverflowException e)
                {
                    _result.Errors.Add(lineId, "Expected 8bit number got 16bit or more");
                }
            }
        }

        private void _processDs(int lineId, MatchCollection matches)
        {
            if (matches.Count != 3)
            {
                _result.Errors.Add(lineId, "Pseudo operation ds needs string and address");
                lineId++;
                return;
            }
            string clearValue = matches[1].Value.Replace("'", string.Empty);
            int destination = 0x4000;
            try
            {
                destination = Convert.ToUInt16(matches[2].Value, 16);
            }
            catch (OverflowException e)
            {
                _result.Errors.Add(lineId, "Expected 16bit address for string literal destination");
            }

            lineId++;
            _result.StringLiterals.Add(destination, clearValue);
        }

        private void _addByteToResult(int lineId, string value)
        {
            try
            {
                if (Regex.IsMatch(value, @"(0x((\d|[A-F]|[a-f])(\d|[A-F]|[a-f])?))"))
                {
                    _result.CommandBytes.Add(Convert.ToByte(value, 16));//write the argument
                }
                else
                {
                    _result.CommandBytes.Add(Convert.ToByte(value));//write the argument
                }
            }
            catch (OverflowException e)
            {
                _result.Errors.Add(lineId, "Expected 8bit number got 16bit or more");
            }
            catch (System.FormatException e)
            {
                _result.Errors.Add(lineId, "Expected 8bit number as an argument, got invalid value");
            }
        }

        private void _addShortToResult(int lineId, string value)
        {
            if (ShortNumberRegex.IsMatch(value))
            {
                try
                {
                    ushort val;
                    if (Regex.IsMatch(value, @"(0x((\d|[A-F]|[a-f]){1,4}))"))
                    {
                        //First store argument's L then H 
                        val = Convert.ToUInt16(value, 16);

                    }
                    else
                    {
                        val = Convert.ToUInt16(value);
                    }
                    _result.CommandBytes.Add((byte)(val & 0xff));
                    _result.CommandBytes.Add((byte)((val & 0xff00) >> 8));
                }
                catch (OverflowException e)
                {
                    _result.Errors.Add(lineId, "Expected 16bit number got larger");
                }
                // catch (System.FormatException e)
                // {
                //     _result.Errors.Add(lineId, "Expected 16bit number as an argument, got invalid value");
                // }
            }
            else if (LabelRegex.IsMatch(value))
            {
                _referredJumps.Add(lineId, value);
                _jumps.Add(_result.CommandBytes.Count, value);
                _result.CommandBytes.Add(0);
                _result.CommandBytes.Add(0);
                return;
            }
            else
            {
                _result.Errors.Add(lineId, "Expected 16bit address value");
            }
        }

        private void _applyJumps()
        {
            foreach (var (line, dest) in _referredJumps)
            {
                if (!_result.JumpDestinations.ContainsKey(dest))
                {
                    // handle a bug where sta can cause two errors for the price of one
                    // passing invalid value(such as df5000) to sta as argument would cause error of invalid argument
                    // and because it expects 16bit value it would make it think that it's an address, which if address is not present
                    // would cause another issue
                    // system is not meant to handle more then one error per line because i'm too lazy to fix it
                    if (_result.JumpDestinations.ContainsKey(dest))
                    {
                        continue;
                    }
                    _result.Errors.Add(line, $"Invalid jump destination. No label \"{dest}\" is present in the code");
                }
            }

            foreach (var jump in _jumps)
            {
                if (!_result.JumpDestinations.ContainsKey(jump.Value))
                {
                    continue;
                }
                ushort dest = (ushort)_result.JumpDestinations[jump.Value];
                _result.CommandBytes[jump.Key] = (byte)(dest & 0xff);
                _result.CommandBytes[jump.Key + 1] = (byte)((dest & 0xff00) >> 8);
            }
        }

        private void _applyAddresses()
        {
            foreach (var addresses in _referredAddresses)
            {
                if (addresses.Value <= _memory.RomSize)
                {
                    _result.Errors.Add(addresses.Key, $"Illegal write address.  0x0000 to 0x{(_memory.RomSize).ToString("X4")} is READ only memory");
                }
#if FORBID_WRITES_OUTSIDE_RAM
                else if (addresses.Value < memory.RamStart || addresses.Value > memory.RamEnd)
                {
                    _result.Errors.Add(addresses.Key, $"Illegal write address.  Only writes to RAM ({memory.RamStart} to {memory.RamEnd}");
                }
#endif
            }
        }

        private void _applyStringLiterals()
        {
            if (_result.StringLiterals.Count > 0)
            {
                int furthestStringAddress = _result.StringLiterals.Keys.Max() + _result.StringLiterals[_result.StringLiterals.Keys.Max()].Length + 1;
                _result.CommandBytes.AddRange(new byte[furthestStringAddress - _result.CommandBytes.Count]);
                foreach ((int addr, string value) in _result.StringLiterals)
                {
                    ushort currentAddress = (ushort)addr;
                    for (int i = 0; i < value.Length; i++)
                    {
                        _result.CommandBytes[currentAddress++] = ((byte)value[i]);
                    }
                    _result.CommandBytes[currentAddress] = 0;
                }
            }
        }

        private void _addAddress(int lineId, string value)
        {
            if (ShortNumberRegex.IsMatch(value))
            {
                try
                {
                    ushort val;
                    if (Regex.IsMatch(value, @"(0x((\d|[A-F]|[a-f]){1,4}))"))
                    {
                        //First store argument's L then H 
                        val = Convert.ToUInt16(value, 16);

                    }
                    else
                    {
                        val = Convert.ToUInt16(value);
                    }
                    _referredAddresses.Add(lineId, val);
                }
                catch (OverflowException e)
                {
                    _result.Errors.Add(lineId, "Expected 16bit number got larger");
                }
            }
            else
            {
                _result.Errors.Add(lineId, "Expected 16bit address value");
            }
        }

        /// <summary>
        /// Converts input code into operations
        /// </summary>
        /// <param name="_code"></param>
        private bool _prepare()
        {
            int lineId = 0;
            int address = 0;
            string[] lines = _code.Split("\n");
            foreach (string line in lines)
            {
                string cleanLine = CommentRegex.Replace(line, "");
                foreach (var define in _defines)
                {
                    cleanLine = Regex.Replace(cleanLine, $@"{define.Key}\b", define.Value);
                }
                //ignore lines that have only comments or whitespaces
                if (string.IsNullOrWhiteSpace(cleanLine))
                {
                    lineId++;
                    continue;
                }
                if (LabelDefinitionRegex.IsMatch(cleanLine))
                {
                    _result.JumpDestinations.Add(LabelDefinitionRegex.Match(cleanLine).Value, _result.CommandBytes.Count);
                    lineId++;
                    continue;
                }
                MatchCollection matches = OperationSeparator.Matches(cleanLine);
                if (matches.Count == 0)
                {
                    _result.Errors.Add(lineId, "Line contains no valid assembly code");
                    lineId++;
                    continue;
                }
                string name = matches[0].Value.ToLower();
                if (name == "set")
                {
                    if (matches.Count != 3)
                    {
                        _result.Errors.Add(lineId, "Pseudo operation set needs define name and value");
                        lineId++;
                        continue;
                    }
                    string clearValue = matches[2].Value.Replace("'", string.Empty);
                    _defines.Add(matches[1].Value, clearValue);
                    lineId++;
                    continue;
                }
                if (name == "ds")
                {
                    _processDs(lineId, matches);
                    continue;
                }
                if (name == "db")
                {
                    _processDb(lineId, matches);
                    continue;
                }
                string? error = _checkInputValidity(matches);
                if (error != null)
                {
                    _result.Errors.Add(lineId, error);
                    lineId++;
                    continue;
                }
                if (_info.StaticAddressCommands.Contains(name))
                {
                    _addAddress(lineId, matches[1].Value);
                }
                //handle stax and ldax separetely cause they are only two commands that share this special argument type
                if (name == "ldax")
                {
                    switch (matches[1].Value)
                    {
                        case "b":
                            _result.CommandBytes.Add(0x0A);
                            break;
                        case "d":
                            _result.CommandBytes.Add(0x1A);
                            break;
                    }
                    continue;
                }
                else if (name == "stax")
                {
                    switch (matches[1].Value)
                    {
                        case "b":
                            _result.CommandBytes.Add(0x02);
                            break;
                        case "d":
                            _result.CommandBytes.Add(0x12);
                            break;
                    }
                    continue;
                }
                CommandInfo op = _info.Commands[name];
                int opCode = Convert.ToByte(op.OpCode, 16);
                int commandLocation = _result.CommandBytes.Count;
                _result.CommandBytes.Add((byte)opCode);
                for (int i = 1; i < matches.Count; i++)
                {
                    switch (op.Arguments[i - 1])
                    {
                        case CommandArgumentType.RegisterName:
                            {
                                opCode += _registerNames.IndexOf(matches[i].Value.ToLower()) * (op?.RegisterNameArgumentOffsets[i - 1] ?? 0);
                                _result.CommandBytes[commandLocation] = (byte)opCode;
                            }
                            break;
                        case CommandArgumentType.RegisterPairName:
                            {
                                int id = _registerPairNames.IndexOf(matches[i].Value.ToLower());
                                opCode += (id < 0 ? _registerPairNames.Count : id) * (op?.RegisterNameArgumentOffsets[i - 1] ?? 0);
                                _result.CommandBytes[commandLocation] = (byte)opCode;
                            }
                            break;
                        case CommandArgumentType.Int8:
                            _addByteToResult(lineId, matches[i].Value);
                            break;
                        case CommandArgumentType.Int16:
                            _addShortToResult(lineId, matches[i].Value);
                            break;
                    }
                }
                address += _info.Commands[name].Arguments.Count + 1;
                _result.Length += _info.Commands[name].Arguments.Count + 1;
                lineId++;
            }
            _applyJumps();
            _applyAddresses();
            _applyStringLiterals();

            _result.Success = _result.Errors.Count == 0;
            return _result.Success;
        }
    }
}