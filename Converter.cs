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

    public struct ProcessedCodeInfo
    {
        /// <summary>
        /// All of the operations that were in the original code
        /// </summary>
        public List<OperationBase> Operations;
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
            Operations = new List<OperationBase>();
            JumpDestinations = new Dictionary<string, int>();
            Length = 0;
            CommandBytes = new List<byte>();
            Errors = new Dictionary<int, string>();
            Success = false;
        }
    }

    public static class Converter
    {
        public static Regex CommentRegex = new Regex("( *)(;)(.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Regex OperationSeparator = new Regex(@"([a-z\d]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Regex JumpLabelRegex = new Regex(@"([A-z]+(?=:))", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Checks if given collection of regex matches is a valid operation
        /// </summary>
        /// <param name="input">Collection of reg ex matches that should contain [OPNAME], [Arg1] ...[ArgN]</param>
        /// <param name="info">Processor commands info config that has information about commands</param>
        /// <returns>Null if no errors were found, or error message</returns>
        private static string? _checkInputValidity(MatchCollection input, ProcessorCommandsInfo info)
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
                            if (addr < 0x800 || addr > 0xbb0)
                            {
                                return ($"Argument {i - 1} is an address and must be in 800 to bb0 range");
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
            int lineId = 0;
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
                    result.JumpDestinations.Add(JumpLabelRegex.Match(cleanLine).Value, result.Operations.Count - 1);
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
                string? error = _checkInputValidity(matches, info);
                if (error != null)
                {
                    result.Errors.Add(lineId, error);
                    lineId++;
                    continue;
                }

                if (info.JumpCommands.Contains(matches[0].Value))
                {
                    referredJumps.Add(lineId, matches[1].Value);
                }
                if (info.StaticAddressCommands.Contains(matches[0].Value))
                {
                    referredAddresses.Add(lineId, Convert.ToUInt16(matches[1].Value, 16));
                }

                switch (matches[0].Value)
                {
                    case "mov":
                        result.Operations.Add(new RegisterMemoryMoveOperation(matches[1].Value, matches[2].Value, interpreter));
                        break;
                    case "mvi":
                        result.Operations.Add(new RegisterMemoryAssignOperation(matches[1].Value, Convert.ToByte(matches[2].Value, 16), interpreter));
                        break;
                    case "sta":
                        result.Operations.Add(new StoreAccumulatorOperation(Convert.ToUInt16(matches[1].Value, 16), interpreter));
                        break;
                    case "lda":
                        result.Operations.Add(new LoadAccumulatorOperation(Convert.ToUInt16(matches[1].Value, 16), interpreter));
                        break;
                    case "add":
                        result.Operations.Add(new AddAccumulatorOperation(matches[1].Value, interpreter));
                        break;
                    case "adc":
                        result.Operations.Add(new AddAccumulatorCarryOperation(matches[1].Value, interpreter));
                        break;
                    case "stc":
                        result.Operations.Add(new SetCarryBitOperation(interpreter));
                        break;
                    case "jmp":
                        result.Operations.Add(new JumpOperation(matches[1].Value, interpreter));
                        break;
                    case "jnz":
                        result.Operations.Add(new JumpIfNotZeroOperation(matches[1].Value, interpreter));
                        break;
                    case "jz":
                        result.Operations.Add(new JumpIfZeroOperation(matches[1].Value, interpreter));
                        break;
                    case "jnc":
                        result.Operations.Add(new JumpIfNotCarryOperation(matches[1].Value, interpreter));
                        break;
                    case "jc":
                        result.Operations.Add(new JumpIfCarryOperation(matches[1].Value, interpreter));
                        break;
                    case "jpo":
                        result.Operations.Add(new JumpIfParityOddOperation(matches[1].Value, interpreter));
                        break;
                    case "jpe":
                        result.Operations.Add(new JumpIfParityEvenOperation(matches[1].Value, interpreter));
                        break;
                    case "jm":
                        result.Operations.Add(new JumpIfNegativeOperation(matches[1].Value, interpreter));
                        break;
                    case "jp":
                        result.Operations.Add(new JumpIfPositiveOperation(matches[1].Value, interpreter));
                        break;
                    case "hlt":
                        result.Operations.Add(new HaltOperation(interpreter));
                        break;
                }
                result.Length += info.Commands[matches[0].Value].Arguments.Count + 1;
                //TODO: Add recording of operation byte code
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
                if (addresses.Value - 0x800 < result.Length)
                {
                    result.Errors.Add(addresses.Key, $"Illegal write address.  0x800 to {(result.Length + 0x800).ToString("X4")} is reserved memory");
                }
            }
            result.Success = result.Errors.Count == 0;
            return result;
        }
    }
}