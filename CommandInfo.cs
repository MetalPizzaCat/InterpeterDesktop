using System.Collections.Generic;

namespace Interpreter
{
    public enum CommandArgumentType
    {
        RegisterName,
        Int8,
        Int16
    }
    public class CommandInfo
    {
        public List<CommandArgumentType> Arguments = new List<CommandArgumentType>();
    }

    public class ProcessorCommandsInfo
    {
        public Dictionary<string, CommandInfo> Commands = new Dictionary<string, CommandInfo>();
    }
}