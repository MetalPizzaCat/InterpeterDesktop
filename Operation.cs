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

    public abstract class OperationBase
    {
        /// <summary>
        /// Name of the operation in the assembly
        /// </summary>
        public string Name;

        protected Interpreter Interpreter;

        protected OperationBase(string name, Interpreter interpreter)
        {
            Name = name;
            Interpreter = interpreter;
        }

        public abstract void Execute();
    }

    public class RegisterMemoryMoveOperation : OperationBase
    {
        public readonly string Destination;
        public readonly string Source;

        public RegisterMemoryMoveOperation(string destination, string source, Interpreter interpreter) : base("mov", interpreter)
        {
            Destination = destination;
            Source = source;
        }

        public override void Execute()
        {
            Interpreter.SetRegisterValue(Destination, Interpreter.GetRegisterValue(Source));
        }
    }

    public class RegisterMemoryAssignOperation : OperationBase
    {
        public readonly string Destination;
        public readonly byte Source;

        public RegisterMemoryAssignOperation(string destination, byte source, Interpreter interpreter) : base("mvi", interpreter)
        {
            Destination = destination;
            Source = source;
        }

        public override void Execute()
        {
            Interpreter.SetRegisterValue(Destination, Source);
        }
    }

    public class StoreAccumulatorOperation : OperationBase
    {
        public readonly ushort Destination;
        public StoreAccumulatorOperation(ushort destination, Interpreter interpreter) : base("sta", interpreter)
        {
            Destination = destination;
            if (Destination < 0x800 || Destination > 0xbb0)
            {
                throw new System.Exception("Address must be in 800 to bb0 range");
            }
        }

        public override void Execute()
        {
            Interpreter.Memory[(ushort)(Destination - 0x800)] = Interpreter.GetRegisterValue("A");
        }
    }

    public class LoadAccumulatorOperation : OperationBase
    {
        public readonly ushort Source;
        public LoadAccumulatorOperation(ushort source, Interpreter interpreter) : base("sta", interpreter)
        {
            Source = source;
            if (Source < 0x800 || Source > 0xbb0)
            {
                throw new System.Exception("Address must be in 800 to bb0 range");
            }
        }

        public override void Execute()
        {
            Interpreter.SetRegisterValue("A", Interpreter.Memory[(ushort)(Source - 0x800)]);
        }
    }

    /// <summary>
    /// This operation stops execution of code
    /// </summary>
    public class HaltOperation : OperationBase
    {
        public HaltOperation(Interpreter interpreter) : base("hlt", interpreter)
        {

        }

        public override void Execute()
        {
            //TODO: ehm, idk do smth
            //throw new System.NotImplementedException();
        }
    }
}