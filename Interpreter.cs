namespace Interpreter
{
    public class ProcessorFlags
    {
        public bool S = false;
        public bool Z = false;
        public bool Ac = false;
        public bool P = false;
        public bool C = false;
    }

    public class Registers
    {
        public byte A { get; set; } = 0;
        public byte B { get; set; } = 0;
        public byte C { get; set; } = 0;
        public byte D { get; set; } = 0;
        public byte E { get; set; } = 0;
        public byte H { get; set; } = 0;
        public byte L { get; set; } = 0;
    }
    public class Interpreter
    {
        private int _programCounter = 0;
        private int _stackPointer = 0;

        private Registers _registers;
        private ProcessorFlags _flags;

        private Memory _memory;

        public Memory Memory { get => _memory; set => _memory = value; }

        public Interpreter()
        {
            _registers = new Registers();
            _flags = new ProcessorFlags();
            _memory = new Memory();
        }
    }
}