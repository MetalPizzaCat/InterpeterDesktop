using System.Collections.Generic;

namespace Emulator
{
    public enum CommandArgumentType
    {
        RegisterName,
        Int8,
        Int16,
        LabelName,
        RegisterPairName,
        Address,
    }

    public class CommandInfo
    {
        public string OpCode = "0";
        public List<CommandArgumentType> Arguments = new List<CommandArgumentType>();

        public Dictionary<int, int>? RegisterNameArgumentOffsets;

        /// <summary>
        /// User written note that explains what this command does
        /// </summary>
        public string? Note;
    }

    public class ProcessorCommandsInfo
    {
        public Dictionary<string, CommandInfo> Commands = new Dictionary<string, CommandInfo>();

        /// <summary>
        /// Operations that take label as an argument<para/>
        /// used for assembly time checks
        /// </summary>
        public List<string> JumpCommands = new List<string>();

        /// <summary>
        /// Operations that have their destination written in the code itself<para/>
        /// used for assembly time checks
        /// </summary>
        public List<string> StaticAddressCommands = new List<string>();

    }
}