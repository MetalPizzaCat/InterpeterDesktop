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
        public LoadAccumulatorOperation(ushort source, Interpreter interpreter) : base("lda", interpreter)
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
    /// Adds register to accumulator, discarding carry
    /// </summary>
    public class AddAccumulatorOperation : OperationBase
    {
        public readonly string Source;
        public AddAccumulatorOperation(string source, Interpreter interpreter) : base("add", interpreter)
        {
            Source = source;
        }

        public override void Execute()
        {
            int value = ((Interpreter.GetRegisterValue("A") + Interpreter.GetRegisterValue(Source)));
            Interpreter.CheckFlags((ushort)value);
            Interpreter.SetRegisterValue("A", (byte)(value & 0xFF));
        }
    }

    /// <summary>
    /// Adds register to accumulator, discarding carry
    /// </summary>
    public class AddAccumulatorCarryOperation : OperationBase
    {
        public readonly string Source;
        public AddAccumulatorCarryOperation(string source, Interpreter interpreter) : base("adc", interpreter)
        {
            Source = source;
        }

        public override void Execute()
        {
            int value = ((Interpreter.GetRegisterValue("A") + Interpreter.GetRegisterValue(Source))) + (Interpreter.Flags.C ? 1 : 0);
            Interpreter.CheckFlags((ushort)value);
            Interpreter.SetRegisterValue("A", (byte)(value & 0xFF));
        }
    }

    public class SetCarryBitOperation : OperationBase
    {
        public SetCarryBitOperation(Interpreter interpreter) : base("stc", interpreter)
        {
        }

        public override void Execute()
        {
            Interpreter.Flags.C = true;
        }
    }

    public class JumpOperation : OperationBase
    {
        public string Destination;

        protected virtual bool CanJump() => true;

        public JumpOperation(string dst, Interpreter interpreter) : base("jmp", interpreter)
        {
            Destination = dst;
        }

        public override void Execute()
        {
            if (CanJump())
            {
                Interpreter.JumpTo(Destination);
            }
        }
    }

    public class JumpIfNotZeroOperation : JumpOperation
    {
        public JumpIfNotZeroOperation(string dst, Interpreter interpreter) : base(dst, interpreter)
        {
        }

        protected override bool CanJump()
        {
            return Interpreter.Flags.Z;
        }
    }
    public class JumpIfZeroOperation : JumpOperation
    {
        public JumpIfZeroOperation(string dst, Interpreter interpreter) : base(dst, interpreter)
        {
        }

        protected override bool CanJump()
        {
            return !Interpreter.Flags.Z;
        }
    }

    public class JumpIfCarryOperation : JumpOperation
    {
        public JumpIfCarryOperation(string dst, Interpreter interpreter) : base(dst, interpreter)
        {
        }

        protected override bool CanJump()
        {
            return Interpreter.Flags.C;
        }
    }

    public class JumpIfNotCarryOperation : JumpOperation
    {
        public JumpIfNotCarryOperation(string dst, Interpreter interpreter) : base(dst, interpreter)
        {
        }

        protected override bool CanJump()
        {
            return !Interpreter.Flags.C;
        }
    }

    public class JumpIfParityEvenOperation : JumpOperation
    {
        public JumpIfParityEvenOperation(string dst, Interpreter interpreter) : base(dst, interpreter)
        {
        }

        protected override bool CanJump()
        {
            return Interpreter.Flags.P;
        }
    }

    public class JumpIfParityOddOperation : JumpOperation
    {
        public JumpIfParityOddOperation(string dst, Interpreter interpreter) : base(dst, interpreter)
        {
        }

        protected override bool CanJump()
        {
            return !Interpreter.Flags.P;
        }
    }

    public class JumpIfPositiveOperation : JumpOperation
    {
        public JumpIfPositiveOperation(string dst, Interpreter interpreter) : base(dst, interpreter)
        {
        }

        protected override bool CanJump()
        {
            return Interpreter.Flags.S;
        }
    }

    public class JumpIfNegativeOperation : JumpOperation
    {
        public JumpIfNegativeOperation(string dst, Interpreter interpreter) : base(dst, interpreter)
        {
        }

        protected override bool CanJump()
        {
            return !Interpreter.Flags.S;
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