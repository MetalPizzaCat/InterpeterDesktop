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

        private string _lineCountText = string.Empty;
        private string _codeText = string.Empty;

        public string LineNumberText => _lineCountText;

        public string CodeText
        {
            get => _codeText;
            set
            {
                _codeText = value;
                int lines = value.Split("\n").Length;
                _lineCountText = string.Empty;
                for (int i = 0; i < lines; i++)
                {
                    _lineCountText += i.ToString() + "\n";
                }
                LineNumberBox.Text = _lineCountText;
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

        private void _displayFatalError(string msg)
        {
            ErrorMsgBox.Text = msg;
            ErrorMsgBox.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.DarkRed);
        }
        private void _displayErrors(IEnumerable<string> errors)
        {
            ErrorMsgBox.Text = string.Join("\n", errors);
            ErrorMsgBox.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.White);
        }

        public async Task RunEmulator()
        {
            try
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
            catch (Interpreter.ProtectedMemoryWriteException e)
            {
                _displayFatalError($"Execution error : {e.Message}");
                return;
            }
        }

        private void _onOutPortValueChanged(int port, byte value)
        {
            _output[port] = _displayOutAsText ? System.Text.Encoding.ASCII.GetString(new[] { value }) : value.ToString("X2");
        }

        private void _recordErrors(Dictionary<int, string> errors)
        {
            _errors.Clear();
            foreach (KeyValuePair<int, string> error in errors)
            {
                _errors.Add($"Line: {error.Key}. Error: {error.Value}");
            }
            _displayErrors(_errors);
        }

        /// <summary>
        /// Simply resets memory and starts execution
        /// </summary>
        private void _runButtonPressed(object? sender, RoutedEventArgs e)
        {
            _interpreter.SoftResetProcessor();
            Dispatcher.UIThread.Post(() => RunEmulator(), DispatcherPriority.Background);
        }

        private async void _loadRomPressed(object? sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filters?.Add(new FileDialogFilter() { Name = "Binary files", Extensions = { "bin" } });
            dialog.Filters?.Add(new FileDialogFilter() { Name = "ROM file", Extensions = { "rom" } });
            string? file = await dialog.ShowAsync(this);
            if (file == null)
            {
                return;
            }
            byte[] rom = System.IO.File.ReadAllBytes(file);
            _interpreter.Memory.WriteRom(rom);
            _interpreter.ResetProcessor();
        }

        private async void _dumpRomPressed(object? sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filters?.Add(new FileDialogFilter() { Name = "Binary files", Extensions = { "bin", "dat" } });
            dialog.Filters?.Add(new FileDialogFilter() { Name = "ROM file", Extensions = { "rom" } });
            dialog.Filters?.Add(new FileDialogFilter() { Name = "Any", Extensions = { "*" } });
            string? file = await dialog.ShowAsync(this);
            if (file == null)
            {
                return;
            }
            await System.IO.File.WriteAllBytesAsync(file, _interpreter.Memory.ReadRom());
        }

        private async void _dumpRamPressed(object? sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filters?.Add(new FileDialogFilter() { Name = "Binary files", Extensions = { "bin", "dat" } });
            dialog.Filters?.Add(new FileDialogFilter() { Name = "Any", Extensions = { "*" } });
            string? file = await dialog.ShowAsync(this);
            if (file == null)
            {
                return;
            }
            await System.IO.File.WriteAllBytesAsync(file, _interpreter.Memory.ReadRam());
        }

        private async void _loadFilePressed(object? sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filters?.Add(new FileDialogFilter() { Name = "Text files", Extensions = { "txt" } });
            dialog.Filters?.Add(new FileDialogFilter() { Name = "Assembly file", Extensions = { "asm", "80asm", "nema" } });
            dialog.Filters?.Add(new FileDialogFilter() { Name = "Any", Extensions = { "*" } });
            string? file = await dialog.ShowAsync(this);
            if (file == null)
            {
                return;
            }
            string text = System.IO.File.ReadAllText(file);
            CodeInputBox.Text = text;
        }

        private async void _saveFilePressed(object? sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filters?.Add(new FileDialogFilter() { Name = "Text files", Extensions = { "txt" } });
            dialog.Filters?.Add(new FileDialogFilter() { Name = "Assembly file", Extensions = { "asm", "80asm", "nema" } });
            dialog.Filters?.Add(new FileDialogFilter() { Name = "Any", Extensions = { "*" } });
            string? file = await dialog.ShowAsync(this);
            if (file == null)
            {
                return;
            }
            System.IO.File.WriteAllText(file, CodeInputBox.Text);
        }

        private void _clearRomPressed(object? sender, RoutedEventArgs e)
        {
            _interpreter.Memory.ClearMemory();
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
                _recordErrors(code.Errors);
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