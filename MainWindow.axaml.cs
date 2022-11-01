#define USE_DEBUG_TOOLS
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace InterpreterDesktop
{

    public partial class MainWindow : Window
    {

        private Interpreter.Interpreter _interpreter;

        public Interpreter.Interpreter InterpreterObject => _interpreter;
        private bool _displayOutAsText = false;

        private ObservableCollection<string> _errors = new ObservableCollection<string>();
        private ObservableCollection<string> _output = new ObservableCollection<string>();
        public ObservableCollection<string> Output => _output;

        public ObservableCollection<string> Errors => _errors;
        public Interpreter.ProcessorFlags Flags => _interpreter.Flags;
        public Interpreter.Registers Registers => _interpreter.Registers;

        public bool DisplayOutAsText
        {
            get => _displayOutAsText;
            set
            {
                _displayOutAsText = value;

                if (value)
                {
                    int port = 0;
                    foreach (byte b in _interpreter.OutputPorts)
                    {
                        _output[port++] = System.Text.Encoding.ASCII.GetString(new[] { b });
                    }
                }
                else
                {
                    int port = 0;
                    foreach (byte b in _interpreter.OutputPorts)
                    {
                        _output[port++] = b.ToString("X2");
                    }
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            _interpreter = new Interpreter.Interpreter();
            _interpreter.OnOutPortValueChanged += _onOutPortValueChanged;
            MemoryGrid.Items = _interpreter.Memory.MemoryDisplayGrid;
            List<string> temp = new List<string>();
            foreach (byte b in _interpreter.OutputPorts)
            {
                temp.Add(b.ToString("X2"));
            }
            _output = new ObservableCollection<string>(temp);
            this.DataContext = this;
        }

        public async Task RunEmulator()
        {
            while (_interpreter.IsRunning)
            {
                _interpreter.Step();
                if (_interpreter.CurrentStepCounter >= _interpreter.StepsBeforeSleep)
                {
                    await Task.Delay(1);
                    _interpreter.ResetStepCounter();
                }
            }
            System.Console.WriteLine("Exited execution");
        }

        private void _onOutPortValueChanged(int port, byte value)
        {
            _output[port] = _displayOutAsText ? System.Text.Encoding.ASCII.GetString(new[] { value }) : value.ToString("X2");
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

                    Dispatcher.UIThread.Post(() => RunEmulator(), DispatcherPriority.Background);
                    //_interpreter.Run();
                }
            }
        }

        private void _clearRomPressed(object? sender, RoutedEventArgs e)
        {
            
        }

        private async void _onSettingsButtonPressed(object? sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            await settingsWindow.ShowDialog(this);
        }

        private void _stopButtonPressed(object? sender, RoutedEventArgs e)
        {
            _interpreter.Stop();
        }

        private void _assembleButtonPressed(object? sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(CodeInputBox.Text))
            {
                Interpreter.ProcessedCodeInfo code = Interpreter.Converter.Prepare(CodeInputBox.Text, _interpreter);
                _displayErrors(code.Errors);
                _interpreter.SetCode(code);
                _interpreter.ResetProcessor();
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