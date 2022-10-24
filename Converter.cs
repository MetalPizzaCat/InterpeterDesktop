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
    public static class Converter
    {
        public static Regex CommentRegex = new Regex("( *)(;)(.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Regex OperationSeparator = new Regex(@"([a-z\d]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        /// <summary>
        /// Converts input code into operations
        /// </summary>
        /// <param name="code"></param>
        public static List<OperationBase> Prepare(string code, Interpreter interpreter)
        {
            string infoText = System.IO.File.ReadAllText("./Configuration/CommandInfo.json");
            ProcessorCommandsInfo info = Newtonsoft.Json.JsonConvert.DeserializeObject<ProcessorCommandsInfo>(infoText) ?? throw new NullReferenceException("Unable to process configuration");

            List<OperationBase> operations = new List<OperationBase>();
            string[] lines = code.Split("\n");
            foreach (string line in lines)
            {
                string cleanLine = CommentRegex.Replace(line, "");
                //ignore lines that have only comments or whitespaces
                if (string.IsNullOrWhiteSpace(cleanLine))
                {
                    continue;
                }
                MatchCollection matches = OperationSeparator.Matches(cleanLine);
                if (matches.Count == 0)
                {
                    throw new InterpreterInvalidOperationException();
                }
                if (info.Commands.ContainsKey(matches[0].Value))
                {
                    int argumentCount = info.Commands[matches[0].Value].Arguments.Count;
                    if (argumentCount != matches.Count - 1)
                    {
                        throw new InterpreterInvalidOperationException($"Operation expected {argumentCount} found {matches.Count - 1}");
                    }
                    for (int i = 1; i < matches.Count; i++)
                    {
                        switch (info.Commands[matches[0].Value].Arguments[i - 1])
                        {
                            case CommandArgumentType.RegisterName:
                                if (!Regex.IsMatch(matches[i].Value, "[A-z]"))
                                {
                                    throw new InterpreterInvalidOperationException($"Argument {i - 1} can only contain one letter: name of the register");
                                }
                                break;
                            case CommandArgumentType.Int8:
                                if (!Regex.IsMatch(matches[i].Value, @"\d"))
                                {
                                    throw new InterpreterInvalidOperationException($"Argument {i - 1} can only contain numbers");
                                }
                                break;
                            case CommandArgumentType.Int16:
                                if (!Regex.IsMatch(matches[i].Value, @"\d"))
                                {
                                    throw new InterpreterInvalidOperationException($"Argument {i - 1} can only contain numbers");
                                }
                                ushort addr = Convert.ToUInt16(matches[i].Value, 16);
                                if (addr < 0x800 || addr > 0xbb0)
                                {
                                    throw new InterpreterInvalidOperationException($"Argument {i - 1} is an address and must be in 800 to bb0 range");
                                }
                                break;
                        }
                    }

                    switch (matches[0].Value)
                    {
                        case "mov":
                            operations.Add(new RegisterMemoryMoveOperation(matches[1].Value, matches[2].Value, interpreter));
                            break;
                        case "mvi":
                            operations.Add(new RegisterMemoryAssignOperation(matches[1].Value, Convert.ToByte(matches[2].Value, 16), interpreter));
                            break;
                        case "sta":
                            ushort addr = Convert.ToUInt16(matches[1].Value, 16);
                            operations.Add(new StoreAccumulatorOperation(addr, interpreter));
                            break;
                        case "lda":
                            break;
                        case "hlt":
                            operations.Add(new HaltOperation(interpreter));
                            break;
                    }
                }
                else
                {
                    throw new InterpreterInvalidOperationException("Unknown operation encountered");
                }

                foreach (Match match in matches)
                {
                    System.Console.Write($"{match.Value}<=>");
                }
                System.Console.WriteLine();

            }
            return operations;
        }
    }
}