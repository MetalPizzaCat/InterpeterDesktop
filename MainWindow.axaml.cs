#define USE_DEBUG_TOOLS
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.ObjectModel;


namespace InterpeterDesktop
{

    public partial class MainWindow : Window
    {

        private Interpreter.Interpreter _interpreter;

        public MainWindow()
        {
            InitializeComponent();
            _interpreter = new Interpreter.Interpreter();
            /*
            MemoryGrid.Columns.Add(new DataGridTextColumn { Header = $"Address", Binding = new Avalonia.Data.Binding($"[{0}]"), IsReadOnly = true });
            for (int i = 0; i <= 0xf; i++)
            {
                MemoryGrid.Columns.Add(new DataGridTextColumn { Header = $"{i.ToString("X2")}", Binding = new Avalonia.Data.Binding($"[{i + 1}]") });
            }*/
            //MemoryGrid.AutoGenerateColumns = false;
            MemoryGrid.Items = _interpreter.Memory.MemoryDisplayGrid;
            this.DataContext = this;
        }

        private void _runButtonPressed(object? sender, RoutedEventArgs e)
        {
            
        }

        private void _onExitRequested(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}