namespace Interpreter
{

    public abstract class OperationBase
    {
        /// <summary>
        /// Name of the operation in the assembly, must match name in the configuration
        /// </summary>
        public string Name;

        protected Interpreter Interpreter;
        /// <summary>
        /// Byte code that this operation will produce when written to rom
        /// </summary>
        /// <value></value>
        public virtual byte[] ByteCode => new byte[1] { 0 };

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
        }

        public override void Execute()
        {
            Interpreter.Memory[Destination] = Interpreter.GetRegisterValue("A");
        }
    }

    public class LoadAccumulatorOperation : OperationBase
    {
        public readonly ushort Source;
        public LoadAccumulatorOperation(ushort source, Interpreter interpreter) : base("lda", interpreter)
        {
            Source = source;
        }

        public override void Execute()
        {
            Interpreter.SetRegisterValue("A", Interpreter.Memory[Source]);
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
    public class AddAccumulatorConstOperation : OperationBase
    {
        public readonly byte Value;
        public AddAccumulatorConstOperation(byte value, Interpreter interpreter) : base("adi", interpreter)
        {
            Value = value;
        }

        public override void Execute()
        {
            int result = ((Interpreter.GetRegisterValue("A") + Value));
            Interpreter.CheckFlags((ushort)result);
            Interpreter.SetRegisterValue("A", (byte)(result & 0xFF));
        }
    }

    /// <summary>
    /// Adds register to accumulator, discarding carry
    /// </summary>
    public class AddAccumulatorConstCarryOperation : OperationBase
    {
        public readonly byte Value;
        public AddAccumulatorConstCarryOperation(byte value, Interpreter interpreter) : base("aci", interpreter)
        {
            Value = value;
        }

        public override void Execute()
        {
            int result = ((Interpreter.GetRegisterValue("A") + Value)) + (Interpreter.Flags.C ? 1 : 0);
            Interpreter.CheckFlags((ushort)result);
            Interpreter.SetRegisterValue("A", (byte)(result & 0xFF));
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

    public class CompareOperation : OperationBase
    {
        public string Source;
        public CompareOperation(string name, Interpreter interpreter) : base("cmp", interpreter)
        {
            Source = name;
        }

        public override void Execute()
        {
            int result = Interpreter.Registers.A - Interpreter.GetRegisterValue(Source);
            Interpreter.Flags.Z = result == 0;
            Interpreter.Flags.S = 0x80 == (result & 0x80);
            Interpreter.Flags.P = Interpreter.Parity((ushort)result);
            Interpreter.Flags.C = Interpreter.Registers.A < Interpreter.GetRegisterValue(Source);
        }
    }

    public class CompareConstOperation : OperationBase
    {
        public byte Value;
        public CompareConstOperation(byte value, Interpreter interpreter) : base("cpi", interpreter)
        {
            Value = value;
        }

        public override void Execute()
        {
            int result = Interpreter.Registers.A - Value;
            Interpreter.Flags.Z = result == 0;
            Interpreter.Flags.S = 0x80 == (result & 0x80);
            Interpreter.Flags.P = Interpreter.Parity((ushort)result);
            Interpreter.Flags.C = Interpreter.Registers.A < Value;
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
            return !Interpreter.Flags.Z;
        }
    }
    public class JumpIfZeroOperation : JumpOperation
    {
        public JumpIfZeroOperation(string dst, Interpreter interpreter) : base(dst, interpreter)
        {
        }

        protected override bool CanJump()
        {
            return Interpreter.Flags.Z;
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

    public sealed class OutOperation : OperationBase
    {
        private byte _index = 0;
        public OutOperation(byte index, Interpreter interpreter) : base("out", interpreter)
        {
            _index = index;
        }

        public override void Execute()
        {
            Interpreter.SetOut((int)_index, Interpreter.Registers.A);
        }
    }
    /// <summary>
    /// Variation of the out that instead writes value of the given register at whichever port a points at
    /// </summary>
    public sealed class OutExtendedOperation : OperationBase
    {
        private string _name;
        public OutExtendedOperation(string name, Interpreter interpreter) : base("outd", interpreter)
        {
            _name = name;
        }

        public override void Execute()
        {
            Interpreter.SetOut((int)Interpreter.Registers.A, Interpreter.GetRegisterValue(_name));
        }
    }

    public class PushOperation : OperationBase
    {
        private string _registerName;
        public PushOperation(string name, Interpreter interpreter) : base("push", interpreter)
        {
            _registerName = name;
        }

        public override void Execute()
        {
            Interpreter.PushStack(_registerName);
        }
    }

    public class PopOperation : OperationBase
    {
        private string _registerName;
        public PopOperation(string name, Interpreter interpreter) : base("pop", interpreter)
        {
            _registerName = name;
        }

        public override void Execute()
        {
            Interpreter.PopStack(_registerName);
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

    public class NoOperation : OperationBase
    {
        public NoOperation(Interpreter interpreter) : base("nop", interpreter)
        {
        }

        public override void Execute()
        {
            //we do nothing cause no operation :)
        }
    }
}