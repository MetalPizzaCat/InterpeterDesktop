#define USE_DEBUG_TOOLS
//#define RESET_INPUT_UPON_READING
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace Nema
{

    public partial class MainWindow : Window
    {

        private Emulator.Emulator _emulator;

        public Emulator.Emulator InterpreterObject => _emulator;
        private bool _displayOutAsText = false;

        private ObservableCollection<string> _errors = new ObservableCollection<string>();
        private ObservableCollection<string> _output = new ObservableCollection<string>();
        public ObservableCollection<string> Output => _output;

        public ObservableCollection<string> Errors => _errors;
        public Emulator.ProcessorFlags Flags => _emulator.Flags;
        public Emulator.Registers Registers => _emulator.Registers;

        private string? _currentFile;

        public bool DisplayOutAsText
        {
            get => _displayOutAsText;
            set
            {
                _displayOutAsText = value;

                if (value)
                {
                    int port = 0;
                    foreach (byte b in _emulator.OutputPorts)
                    {
                        _output[port++] = System.Text.Encoding.ASCII.GetString(new[] { b });
                    }
                }
                else
                {
                    int port = 0;
                    foreach (byte b in _emulator.OutputPorts)
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
            _emulator = new Emulator.Emulator();
            _emulator.OnOutPortValueChanged += _onOutPortValueChanged;
            _emulator.OnInPortRead += _inputRead;
            MemoryGrid.Items = _emulator.Memory.MemoryDisplayGrid;
            List<string> temp = new List<string>();
            foreach (byte b in _emulator.OutputPorts)
            {
                temp.Add(b.ToString("X2"));
            }
            _output = new ObservableCollection<string>(temp);
            InputTable.OnPortValueChanged += _onInValueChanged;

            this.DataContext = this;
        }

        private void _inputRead(int port)
        {
#if RESET_INPUT_UPON_READING
            _interpreter.SetIn(port, 0);
            InputTable.SetPortValue(port, 0);
#endif
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
                while (_emulator.IsRunning)
                {

                    _emulator.Step();

                    if (_emulator.CurrentStepCounter >= _emulator.StepsBeforeSleep)
                    {
                        await Task.Delay(1);
                        _emulator.ResetStepCounter();
                    }
                }
                System.Console.WriteLine("Exited execution");
            }
            catch (Emulator.ProtectedMemoryWriteException e)
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
        private void _run()
        {
            _emulator.SoftResetProcessor();
            Dispatcher.UIThread.Post(() => RunEmulator(), DispatcherPriority.Background);
        }

        private async void _loadRom()
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
            _emulator.Memory.WriteRom(rom);
            _emulator.ResetProcessor();
        }

        private async void _dumpRom()
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
            await System.IO.File.WriteAllBytesAsync(file, _emulator.Memory.ReadRom());
        }

        private async void _dumpRam()
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filters?.Add(new FileDialogFilter() { Name = "Binary files", Extensions = { "bin", "dat" } });
            dialog.Filters?.Add(new FileDialogFilter() { Name = "Any", Extensions = { "*" } });
            string? file = await dialog.ShowAsync(this);
            if (file == null)
            {
                return;
            }
            await System.IO.File.WriteAllBytesAsync(file, _emulator.Memory.ReadRam());
        }

        private void _clearRom()
        {
            _emulator.Memory.ClearMemory();
        }

        /// <summary>
        /// Opens a file dialog for saving code to the file
        /// </summary>
        /// <param name="newFile">If true function will work as if no file has already been opened</param>
        private async void _saveFile(bool newFile)
        {
            if (!newFile && _currentFile != null)
            {
                System.IO.File.WriteAllText(_currentFile, CodeInputBox.Text);
                return;
            }
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filters?.Add(new FileDialogFilter() { Name = "Text files", Extensions = { "txt" } });
            dialog.Filters?.Add(new FileDialogFilter() { Name = "Assembly file", Extensions = { "asm", "80asm", "nema" } });
            dialog.Filters?.Add(new FileDialogFilter() { Name = "Any", Extensions = { "*" } });
            _currentFile = await dialog.ShowAsync(this);
            Title = $"NEMA-8 {_currentFile ?? string.Empty}";
            if (_currentFile == null)
            {
                return;
            }
            System.IO.File.WriteAllText(_currentFile, CodeInputBox.Text);

        }

        private async void _loadFile()
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filters?.Add(new FileDialogFilter() { Name = "Text files", Extensions = { "txt" } });
            dialog.Filters?.Add(new FileDialogFilter() { Name = "Assembly file", Extensions = { "asm", "80asm", "nema" } });
            dialog.Filters?.Add(new FileDialogFilter() { Name = "Any", Extensions = { "*" } });
            _currentFile = await dialog.ShowAsync(this);
            Title = $"NEMA-8 {_currentFile ?? string.Empty}";
            if (_currentFile == null)
            {
                return;
            }
            string text = System.IO.File.ReadAllText(_currentFile);
            CodeInputBox.Text = text;
        }

        private async void _onSettingsButtonPressed(object? sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            await settingsWindow.ShowDialog(this);
        }

        private void _stopEmulation()
        {
            _emulator.Stop();
        }

        private void _assemble()
        {
            if (!string.IsNullOrWhiteSpace(CodeInputBox.Text))
            {
                Emulator.ProcessedCodeInfo code = Emulator.Converter.Prepare(CodeInputBox.Text, _emulator);
                _recordErrors(code.Errors);
                _emulator.SetCode(code);
                _emulator.ResetProcessor();
            }
        }

        private void _onInValueChanged(int port, byte value)
        {
            _emulator.SetIn(port, value);
        }

        private void _step()
        {
            _emulator.Step();
        }

        private void _onExitRequested(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void _displayHelp()
        {
            HelpWindow help = new HelpWindow();
            await help.ShowDialog(this);
        }
    }
}