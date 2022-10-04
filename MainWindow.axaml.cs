using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.ObjectModel;
namespace InterpeterDesktop
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
    public partial class MainWindow : Window
    {
        private int _programCounter = 0;
        private int _stackPointer = 0;

        private Registers _registers;
        private ProcessorFlags _flags;

        private Memory _memory;

        public MainWindow()
        {
            InitializeComponent();
            _memory = new Memory();
            MemoryGrid.Columns.Add(new DataGridTextColumn { Header = $"Address", Binding = new Avalonia.Data.Binding($"[{0}]"), IsReadOnly = true });
            for (int i = 0; i <= 0xf; i++)
            {
                MemoryGrid.Columns.Add(new DataGridTextColumn { Header = $"{i.ToString("X2")}", Binding = new Avalonia.Data.Binding($"[{i + 1}]") });
            }
            MemoryGrid.AutoGenerateColumns = false;
            MemoryGrid.Items = _memory.MemoryDisplayGrid;
            this.DataContext = this;
        }

        private void _onExitRequested(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}