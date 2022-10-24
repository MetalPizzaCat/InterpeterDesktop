#define USE_DEBUG_TOOLS
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.ObjectModel;


namespace InterpreterDesktop
{

    public partial class MainWindow : Window
    {

        private Interpreter.Interpreter _interpreter;

        public Interpreter.ProcessorFlags Flags => _interpreter.Flags;
        public Interpreter.Registers Registers => _interpreter.Registers;
        public MainWindow()
        {
            InitializeComponent();
            _interpreter = new Interpreter.Interpreter();
            MemoryGrid.Items = _interpreter.Memory.MemoryDisplayGrid;
            this.DataContext = this;
            //MemoryGrid.DataContext = _interpreter.Memory.MemoryDisplayGrid;
        }

        private void _runButtonPressed(object? sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(CodeInputBox.Text))
            {
                _interpreter.Operations = Interpreter.Converter.Prepare(CodeInputBox.Text, _interpreter);
                _interpreter.Run();
            }
        }

        private void _assembleButtonPressed(object? sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(CodeInputBox.Text))
            {
                _interpreter.Operations = Interpreter.Converter.Prepare(CodeInputBox.Text, _interpreter);
            }
        }

        private void _stepButtonPressed(object? sender, RoutedEventArgs e)
        {
            _interpreter.Step();
        }

        private void _onExitRequested(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}