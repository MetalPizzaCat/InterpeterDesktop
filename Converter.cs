using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;

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
                switch (matches[0].Value)
                {
                    case "mov":
                        {
                            if (matches.Count != 3)
                            {
                                throw new InterpreterInvalidOperationException("Mov operation needs 2 arguments, name of the destination and name of the source register");
                            }
                            if (Regex.IsMatch(matches[1].Value, "[A-z]") && Regex.IsMatch(matches[2].Value, "[A-z]"))
                            {
                                operations.Add(new RegisterMemoryMoveOperation(matches[1].Value, matches[2].Value, interpreter));
                            }
                            else
                            {
                                throw new InterpreterInvalidOperationException("Arguments for mov operation must be one letter register names");
                            }
                        }
                        break;
                    case "mvi":
                        {
                            if (matches.Count != 3)
                            {
                                throw new InterpreterInvalidOperationException("Mov operation needs 2 arguments, name of the destination and value");
                            }
                            if (Regex.IsMatch(matches[1].Value, "[A-z]") && Regex.IsMatch(matches[2].Value, @"\d"))
                            {
                                operations.Add(new RegisterMemoryAssignOperation(matches[1].Value, Convert.ToByte(matches[2].Value, 16), interpreter));
                            }
                            else
                            {
                                throw new InterpreterInvalidOperationException("Arguments for mov operation must be one letter register name and byte value");
                            }
                        }
                        break;
                    case "sta":
                        if (matches.Count != 2)
                        {
                            throw new InterpreterInvalidOperationException("Sta operation needs only destination address in 800 to b00 range");
                        }
                        if (Regex.IsMatch(matches[1].Value, @"\d"))
                        {
                            ushort addr = Convert.ToUInt16(matches[1].Value, 16);
                            if (addr < 0x800 || addr > 0xbb0)
                            {
                                throw new InterpreterInvalidOperationException("Address out of range");
                            }
                            operations.Add(new StoreAccumulatorOperation(addr, interpreter));
                        }
                        break;
                    case "hlt":
                        if (matches.Count != 1)
                        {
                            throw new InterpreterInvalidOperationException("HTL has no arguments");
                        }
                        operations.Add(new HaltOperation(interpreter));
                        break;
                    default:
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