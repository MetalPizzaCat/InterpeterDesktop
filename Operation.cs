namespace Interpreter
{
    public class Operation
    {
        /// <summary>
        /// Name of the operation
        /// </summary>
        public string Name;
        /// <summary>
        /// Right hand register that will be used as argument
        /// </summary>
        public string? RightRegisterName;
        /// <summary>
        /// Left hand register that will be used as argument
        /// </summary>
        public string? LeftRegisterName;
        /// <summary>
        /// Right hand byte value that will be used as argument
        /// </summary>
        public byte? RightArgument;
        /// <summary>
        /// Left hand byte value that will be used as argument
        /// </summary>
        public byte? LeftArgument;

        public Operation(string name, string? rightRegisterName, string? leftRegisterName)
        {
            Name = name;
            RightRegisterName = rightRegisterName;
            LeftRegisterName = leftRegisterName;
            RightArgument = null;
            LeftArgument = null;
        }

        public Operation(string name, byte? rightArgument, byte? leftArgument)
        {
            Name = name;
            RightRegisterName = null;
            LeftRegisterName = null;
            RightArgument = rightArgument;
            LeftArgument = leftArgument;
        }
    }
}