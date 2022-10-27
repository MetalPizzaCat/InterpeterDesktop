#define ALLOW_WRITES_OUTSIDE_RAM

using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;
using System.Linq;
namespace Interpreter
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
        public bool Success;

        public ProcessedCodeInfo()
        {
            JumpDestinations = new Dictionary<string, int>();
            Length = 0;
            CommandBytes = new List<byte>();
            Errors = new Dictionary<int, string>();
            Success = false;
        }
    }

    public static class Converter
    {

        private static List<string> _registerNames = new List<string> { "b", "c", "d", "e", "h", "l", "m", "a" };
        public static Regex CommentRegex = new Regex("( *)(;)(.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Regex OperationSeparator = new Regex(@"([a-z\d]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Regex JumpLabelRegex = new Regex(@"([A-z]+(?=:))", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Checks if given collection of regex matches is a valid operation
        /// </summary>
        /// <param name="input">Collection of reg ex matches that should contain [OPNAME], [Arg1] ...[ArgN]</param>
        /// <param name="info">Processor commands info config that has information about commands</param>
        /// <returns>Null if no errors were found, or error message</returns>
        private static string? _checkInputValidity(MatchCollection input, ProcessorCommandsInfo info, Interpreter interpreter)
        {
            if (info.Commands.ContainsKey(input[0].Value))
            {
                int argumentCount = info.Commands[input[0].Value].Arguments.Count;
                if (argumentCount != input.Count - 1)
                {
                    return $"Operation expected {argumentCount} found {input.Count - 1}";
                }
                for (int i = 1; i < input.Count; i++)
                {
                    switch (info.Commands[input[0].Value].Arguments[i - 1])
                    {
                        case CommandArgumentType.RegisterName:
                            if (!Regex.IsMatch(input[i].Value, "[A-z]"))
                            {
                                return ($"Argument {i - 1} can only contain one letter: name of the register");
                            }
                            break;
                        case CommandArgumentType.Int8:
                            if (!Regex.IsMatch(input[i].Value, @"\d"))
                            {
                                return ($"Argument {i - 1} can only contain numbers");
                            }
                            break;
                        case CommandArgumentType.Int16:
                            if (!Regex.IsMatch(input[i].Value, @"\d"))
                            {
                                return ($"Argument {i - 1} can only contain numbers");
                            }
                            ushort addr = Convert.ToUInt16(input[i].Value, 16);
                            if (addr >= interpreter.Memory.MemoryData.TotalSize)
                            {
                                return $"Argument {i - 1} is an address that points outside of allowed memory";
                            }
                            break;
                        case CommandArgumentType.LabelName:
                            if (!Regex.IsMatch(input[i].Value, "[A-z]+"))
                            {
                                return ($"Argument {i - 1} can only contain one letter: name of the register");
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
        /// <summary>
        /// Converts input code into operations
        /// </summary>
        /// <param name="code"></param>
        public static ProcessedCodeInfo Prepare(string code, Interpreter interpreter)
        {
            string infoText = System.IO.File.ReadAllText("./Configuration/CommandInfo.json");
            ProcessorCommandsInfo info = Newtonsoft.Json.JsonConvert.DeserializeObject<ProcessorCommandsInfo>(infoText) ?? throw new NullReferenceException("Unable to process configuration");

            ProcessedCodeInfo result = new ProcessedCodeInfo();
            List<OperationBase> operations = new List<OperationBase>();
            //jumps that were referred to by call/jump commands
            //used for checking if jump destination is valid at assemble time
            Dictionary<int, string> referredJumps = new Dictionary<int, string>();
            Dictionary<int, int> referredAddresses = new Dictionary<int, int>();
            //Key is jump label, value is where to place address of the jump label
            Dictionary<string, int> jumps = new Dictionary<string, int>();
            int lineId = 0;
            int address = 0;
            string[] lines = code.Split("\n");
            foreach (string line in lines)
            {
                string cleanLine = CommentRegex.Replace(line, "");
                //ignore lines that have only comments or whitespaces
                if (string.IsNullOrWhiteSpace(cleanLine))
                {
                    lineId++;
                    continue;
                }
                if (JumpLabelRegex.IsMatch(cleanLine))
                {
                    result.JumpDestinations.Add(JumpLabelRegex.Match(cleanLine).Value, result.Length);
                    lineId++;
                    continue;
                }
                MatchCollection matches = OperationSeparator.Matches(cleanLine);
                if (matches.Count == 0)
                {
                    result.Errors.Add(lineId, "Line contains no valid assembly code");
                    lineId++;
                    continue;
                }
                string? error = _checkInputValidity(matches, info, interpreter);
                if (error != null)
                {
                    result.Errors.Add(lineId, error);
                    lineId++;
                    continue;
                }

                if (info.StaticAddressCommands.Contains(matches[0].Value))
                {
                    referredAddresses.Add(lineId, Convert.ToUInt16(matches[1].Value, 16));
                }


                // handle commands that use different opcodes for combinations of registers
                switch (matches[0].Value)
                {
                    case "mov":
                        {
                            byte byteBase = (byte)(0x40 + _registerNames.IndexOf(matches[1].Value.ToLower()) * 0x8);
                            byteBase += (byte)_registerNames.IndexOf(matches[1].Value.ToLower());
                            result.CommandBytes.Add(byteBase);
                        }
                        break;
                    case "mvi":
                        {
                            byte byteBase = (byte)(0x06 + _registerNames.IndexOf(matches[1].Value.ToLower()) * 0x8);
                            result.CommandBytes.Add(byteBase);
                        }
                        result.CommandBytes.Add(Convert.ToByte(matches[2].Value, 16));//write the argument
                        break;

                    case "add":
                        {
                            byte byteBase = (byte)(0x80 + _registerNames.IndexOf(matches[1].Value.ToLower()));
                            result.CommandBytes.Add(byteBase);
                        }
                        break;
                    case "adc":
                        {
                            byte byteBase = (byte)(0x88 + _registerNames.IndexOf(matches[1].Value.ToLower()));
                            result.CommandBytes.Add(byteBase);
                        }
                        break;
                    case "adi":
                        {
                            byte byteBase = (byte)(0x80 + _registerNames.IndexOf(matches[1].Value.ToLower()));
                            result.CommandBytes.Add(byteBase);
                        }
                        result.CommandBytes.Add(Convert.ToByte(matches[1].Value, 16));//write the argument
                        break;
                    case "cmp":
                        {
                            byte byteBase = (byte)(0xbf + _registerNames.IndexOf(matches[1].Value.ToLower()));
                            result.CommandBytes.Add(byteBase);
                        }
                        break;
                    case "push":
                        {
                            byte byteBase = 0xc5;
                            switch (matches[1].Value)
                            {
                                case "b":
                                    byteBase += 0x00;
                                    break;
                                case "d":
                                    byteBase += 0x10;
                                    break;
                                case "h":
                                    byteBase += 0x20;
                                    break;
                            }
                            result.CommandBytes.Add(byteBase);
                        }
                        break;
                    case "pop":
                        {
                            byte byteBase = 0xc1;
                            switch (matches[1].Value)
                            {
                                case "b":
                                    byteBase += 0x00;
                                    break;
                                case "d":
                                    byteBase += 0x10;
                                    break;
                                case "h":
                                    byteBase += 0x20;
                                    break;
                            }
                            result.CommandBytes.Add(byteBase);
                        }
                        break;
                    //Every other operation will just have it's byte written down and arguments written out 
                    default:
                        {
                            CommandInfo op = info.Commands[matches[0].Value];
                            result.CommandBytes.Add(Convert.ToByte(op.OpCode, 16));//write the argument
                            for (int i = 1; i < matches.Count; i++)
                            {
                                switch (op.Arguments[i - 1])
                                {
                                    case CommandArgumentType.Int8:
                                        result.CommandBytes.Add(Convert.ToByte(matches[i].Value, 16));//write the argument
                                        break;
                                    case CommandArgumentType.Int16:
                                        //First store argument's L then H 
                                        ushort val = Convert.ToUInt16(matches[i].Value, 16);
                                        result.CommandBytes.Add((byte)(val & 0xff));
                                        result.CommandBytes.Add((byte)((val & 0xff00) >> 8));
                                        break;
                                }
                            }
                        }
                        if (info.JumpCommands.Contains(matches[0].Value))
                        {
                            referredJumps.Add(lineId, matches[1].Value);
                            jumps.Add(matches[1].Value, result.CommandBytes.Count);
                            result.CommandBytes.Add(0);
                            result.CommandBytes.Add(0);
                        }
                        break;
                }
                address += info.Commands[matches[0].Value].Arguments.Count + 1;
                result.Length += info.Commands[matches[0].Value].Arguments.Count + 1;
                lineId++;
            }
            foreach (var jump in referredJumps)
            {
                if (!result.JumpDestinations.ContainsKey(jump.Value))
                {
                    result.Errors.Add(jump.Key, $"Invalid jump destination. No label \"{jump.Value}\" is present in the code");
                }
            }
            foreach (var addresses in referredAddresses)
            {
                if (addresses.Value <= interpreter.Memory.MemoryData.RomSize)
                {
                    result.Errors.Add(addresses.Key, $"Illegal write address.  0000 to {(interpreter.Memory.MemoryData.RomSize).ToString("X4")} is READ only memory");
                }
#if FORBID_WRITES_OUTSIDE_RAM
                else if (addresses.Value < interpreter.Memory.MemoryData.RamStart || addresses.Value > interpreter.Memory.MemoryData.RamEnd)
                {
                    result.Errors.Add(addresses.Key, $"Illegal write address.  Only writes to RAM ({interpreter.Memory.MemoryData.RamStart} to {interpreter.Memory.MemoryData.RamEnd}");
                }
#endif
            }
            foreach (var jump in jumps)
            {
                if (!result.JumpDestinations.ContainsKey(jump.Key))
                {
                    continue;
                }
                ushort dest = (ushort)result.JumpDestinations[jump.Key];
                result.CommandBytes[jump.Value] = (byte)(dest & 0xff);
                result.CommandBytes[jump.Value + 1] = (byte)((dest & 0xff00) >> 8);
            }
            result.Success = result.Errors.Count == 0;
            return result;
        }
    }
}