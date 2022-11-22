#define ALLOW_WRITES_OUTSIDE_RAM

using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;
using System.Linq;
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

    public static class Converter
    {

        private static List<string> _registerNames = new List<string> { "b", "c", "d", "e", "h", "l", "m", "a" };
        private static List<string> _registerPairNames = new List<string> { "b", "d", "h" };
        public static Regex CommentRegex = new Regex("( *)(;)(.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Regex OperationSeparator = new Regex(@"([A-z\d]+)|('(([A-z\d]+)( *|,)+([A-z\d]+)*)')", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Regex LabelDefinitionRegex = new Regex(@"(([A-z]|\d)+(?=:))", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Regex ShortNumberRegex = new Regex(@"^(0x((\d|[A-F]){1,4}))|(\d{1,5})$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Regex LabelRegex = new Regex(@"([A-z]|\d)+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Checks if given collection of regex matches is a valid operation
        /// </summary>
        /// <param name="input">Collection of reg ex matches that should contain [OPNAME], [Arg1] ...[ArgN]</param>
        /// <param name="info">Processor commands info config that has information about commands</param>
        /// <returns>Null if no errors were found, or error message</returns>
        private static string? _checkInputValidity(MatchCollection input, ProcessorCommandsInfo info, MemorySegmentationData memory)
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
        /// <summary>
        /// Converts input code into operations
        /// </summary>
        /// <param name="code"></param>
        public static ProcessedCodeInfo Prepare(string code, MemorySegmentationData memory)
        {
            string infoText = System.IO.File.ReadAllText("./Configuration/CommandInfo.json");
            ProcessorCommandsInfo info = Newtonsoft.Json.JsonConvert.DeserializeObject<ProcessorCommandsInfo>(infoText) ?? throw new NullReferenceException("Unable to process configuration");

            ProcessedCodeInfo result = new ProcessedCodeInfo();
            //jumps that were referred to by call/jump commands
            //used for checking if jump destination is valid at assemble time
            Dictionary<int, string> referredJumps = new Dictionary<int, string>();
            Dictionary<int, int> referredAddresses = new Dictionary<int, int>();
            //Key is jump label, value is where to place address of the jump label
            Dictionary<int, string> jumps = new Dictionary<int, string>();
            //List of all user defined assemble time constants. They function similarly to #define in c++
            Dictionary<string, string> defines = new Dictionary<string, string>();
            int lineId = 0;
            int address = 0;
            string[] lines = code.Split("\n");
            foreach (string line in lines)
            {
                string cleanLine = CommentRegex.Replace(line, "");
                foreach (var define in defines)
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
                    result.JumpDestinations.Add(LabelDefinitionRegex.Match(cleanLine).Value, result.CommandBytes.Count);
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
                string name = matches[0].Value.ToLower();
                if (name == "set")
                {
                    if (matches.Count != 3)
                    {
                        result.Errors.Add(lineId, "Pseudo operation set needs define name and value");
                        lineId++;
                        continue;
                    }
                    string clearValue = matches[2].Value.Replace("'", string.Empty);
                    defines.Add(matches[1].Value, clearValue);
                    lineId++;
                    continue;
                }
                if (name == "ds")
                {
                    if (matches.Count != 3)
                    {
                        result.Errors.Add(lineId, "Pseudo operation db needs string and address");
                        lineId++;
                        continue;
                    }
                    string clearValue = matches[1].Value.Replace("'", string.Empty);
                    int destination = 0x4000;
                    try
                    {
                        destination = Convert.ToUInt16(matches[2].Value, 16);
                    }
                    catch (OverflowException e)
                    {
                        result.Errors.Add(lineId, "Expected 16bit address for string literal destination");
                    }

                    lineId++;
                    result.StringLiterals.Add(destination, clearValue);
                    continue;
                }
                if (name == "db")
                {
                    foreach (Match match in matches.Skip(1))
                    {
                        try
                        {
                            if (Regex.IsMatch(match.Value, @"(0x((\d|[A-F])(\d|[A-F])?))"))
                            {
                                result.CommandBytes.Add(Convert.ToByte(match.Value, 16));//write the argument
                            }
                            else
                            {
                                result.CommandBytes.Add(Convert.ToByte(match.Value));//write the argument
                            }
                        }
                        catch (OverflowException e)
                        {
                            result.Errors.Add(lineId, "Expected 8bit number got 16bit or more");
                        }
                    }
                    continue;
                }
                string? error = _checkInputValidity(matches, info, memory);
                if (error != null)
                {
                    result.Errors.Add(lineId, error);
                    lineId++;
                    continue;
                }
                if (info.StaticAddressCommands.Contains(name))
                {
                    referredAddresses.Add(lineId, Convert.ToUInt16(matches[1].Value, 16));
                }
                // handle commands that use different opcodes for combinations of registers
                switch (name)
                {
                    case "ldax":
                        switch (matches[1].Value)
                        {
                            case "b":
                                result.CommandBytes.Add(0x0A);
                                break;
                            case "d":
                                result.CommandBytes.Add(0x1A);
                                break;
                        }
                        break;
                    case "stax":
                        switch (matches[1].Value)
                        {
                            case "b":
                                result.CommandBytes.Add(0x02);
                                break;
                            case "d":
                                result.CommandBytes.Add(0x12);
                                break;
                        }
                        break;
                    //Every other operation will just have it's byte written down and arguments written out 
                    default:
                        {
                            CommandInfo op = info.Commands[name];
                            int opCode = Convert.ToByte(op.OpCode, 16);
                            int commandLocation = result.CommandBytes.Count;
                            result.CommandBytes.Add((byte)opCode);
                            for (int i = 1; i < matches.Count; i++)
                            {
                                switch (op.Arguments[i - 1])
                                {
                                    case CommandArgumentType.RegisterName:
                                        {
                                            opCode += _registerNames.IndexOf(matches[i].Value.ToLower()) * (op?.RegisterNameArgumentOffsets[i - 1] ?? 0);
                                            result.CommandBytes[commandLocation] = (byte)opCode;
                                        }
                                        break;
                                    case CommandArgumentType.RegisterPairName:
                                        {
                                            int id = _registerPairNames.IndexOf(matches[i].Value.ToLower());
                                            opCode += (id < 0 ? _registerPairNames.Count : id) * (op?.RegisterNameArgumentOffsets[i - 1] ?? 0);
                                            result.CommandBytes[commandLocation] = (byte)opCode;
                                        }
                                        break;
                                    case CommandArgumentType.Int8:
                                        try
                                        {
                                            if (Regex.IsMatch(matches[i].Value, @"(0x((\d|[A-F])(\d|[A-F])?))"))
                                            {
                                                result.CommandBytes.Add(Convert.ToByte(matches[i].Value, 16));//write the argument
                                            }
                                            else
                                            {
                                                result.CommandBytes.Add(Convert.ToByte(matches[i].Value));//write the argument
                                            }
                                        }
                                        catch (OverflowException e)
                                        {
                                            result.Errors.Add(lineId, "Expected 8bit number got 16bit or more");
                                        }
                                        break;
                                    case CommandArgumentType.Int16:
                                        if (ShortNumberRegex.IsMatch(matches[i].Value))
                                        {
                                            try
                                            {
                                                ushort val;
                                                if (Regex.IsMatch(matches[i].Value, @"(0x((\d|[A-F]){1,4}))"))
                                                {
                                                    //First store argument's L then H 
                                                    val = Convert.ToUInt16(matches[i].Value, 16);

                                                }
                                                else
                                                {
                                                    val = Convert.ToUInt16(matches[i].Value);
                                                }
                                                result.CommandBytes.Add((byte)(val & 0xff));
                                                result.CommandBytes.Add((byte)((val & 0xff00) >> 8));
                                            }
                                            catch (OverflowException e)
                                            {
                                                result.Errors.Add(lineId, "Expected 16bit number got larger");
                                            }
                                            break;
                                        }
                                        if (LabelRegex.IsMatch(matches[i].Value))
                                        {
                                            referredJumps.Add(lineId, matches[i].Value);
                                            jumps.Add(result.CommandBytes.Count, matches[i].Value);
                                            result.CommandBytes.Add(0);
                                            result.CommandBytes.Add(0);
                                            break;
                                        }
                                        break;
                                }
                            }
                        }/*
                        if (info.JumpCommands.Contains(name))
                        {
                            referredJumps.Add(lineId, matches[1].Value);
                            jumps.Add(result.CommandBytes.Count, matches[1].Value);
                            result.CommandBytes.Add(0);
                            result.CommandBytes.Add(0);
                        }*/
                        break;
                }
                address += info.Commands[name].Arguments.Count + 1;
                result.Length += info.Commands[name].Arguments.Count + 1;
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
                if (addresses.Value < memory.RomSize)
                {
                    result.Errors.Add(addresses.Key, $"Illegal write address.  0000 to {(memory.RomSize).ToString("X4")} is READ only memory");
                }
#if FORBID_WRITES_OUTSIDE_RAM
                else if (addresses.Value < memory.RamStart || addresses.Value > memory.RamEnd)
                {
                    result.Errors.Add(addresses.Key, $"Illegal write address.  Only writes to RAM ({memory.RamStart} to {memory.RamEnd}");
                }
#endif
            }
            foreach (var jump in jumps)
            {
                if (!result.JumpDestinations.ContainsKey(jump.Value))
                {
                    continue;
                }
                ushort dest = (ushort)result.JumpDestinations[jump.Value];
                result.CommandBytes[jump.Key] = (byte)(dest & 0xff);
                result.CommandBytes[jump.Key + 1] = (byte)((dest & 0xff00) >> 8);
            }
            if (result.StringLiterals.Count > 0)
            {
                int furthestStringAddress = result.StringLiterals.Keys.Max() + result.StringLiterals[result.StringLiterals.Keys.Max()].Length + 1;
                result.CommandBytes.AddRange(new byte[furthestStringAddress - result.CommandBytes.Count]);
                foreach ((int addr, string value) in result.StringLiterals)
                {
                    ushort currentAddress = (ushort)addr;
                    for (int i = 0; i < value.Length; i++)
                    {
                        result.CommandBytes[currentAddress++] = ((byte)value[i]);
                    }
                    result.CommandBytes[currentAddress] = 0;
                }
            }
            result.Success = result.Errors.Count == 0;
            return result;
        }
    }
}