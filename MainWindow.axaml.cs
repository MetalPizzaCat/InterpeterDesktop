#define USE_DEBUG_TOOLS
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace InterpreterDesktop
{

    public partial class MainWindow : Window
    {

        private Interpreter.Interpreter _interpreter;

        private ObservableCollection<string> _errors = new ObservableCollection<string>();

        public ObservableCollection<string> Errors => _errors;
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

        private void _displayErrors(Dictionary<int, string> errors)
        {
            _errors.Clear();
            foreach (KeyValuePair<int, string> error in errors)
            {
                _errors.Add($"Line: {error.Key}. Error: {error.Value}");
            }
        }

        private void _runButtonPressed(object? sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(CodeInputBox.Text))
            {
                Interpreter.ProcessedCodeInfo code = Interpreter.Converter.Prepare(CodeInputBox.Text, _interpreter);
                _displayErrors(code.Errors);
                if (code.Success)
                {
                    _interpreter.SetCode(code);
                    _interpreter.ResetProcessor();
                    _interpreter.Run();
                }
            }
        }

        private void _assembleButtonPressed(object? sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(CodeInputBox.Text))
            {
                Interpreter.ProcessedCodeInfo code = Interpreter.Converter.Prepare(CodeInputBox.Text, _interpreter);
                _displayErrors(code.Errors);
                _interpreter.SetCode(code);
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