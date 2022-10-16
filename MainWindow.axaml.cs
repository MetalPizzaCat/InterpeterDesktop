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