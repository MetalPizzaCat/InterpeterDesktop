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

        public string ProgramCounter { get; set; } = "00";

        private int _emulationSleepLength = 1;

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
            _emulator.OnInputResetRequested += _resetInput;
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

        private void _resetInput()
        {
            InputTable.ResetPorts();
        }

        private void _displayFatalError(string msg)
        {
            ErrorMsgBox.Text = msg;
            ErrorMsgBox.Foreground= new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Red);
        }
        private void _displayErrors(IEnumerable<string> errors)
        {
            ErrorMsgBox.Text = string.Join("\n", errors);
            ErrorMsgBox.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Black);
        }

        private void _clearErrors()
        {
            ErrorMsgBox.Text = string.Empty;
            ErrorMsgBox.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Black);
            _errors.Clear();
        }

        public async Task RunEmulator()
        {
            try
            {
                while (_emulator.IsRunning)
                {
                    _emulator.Step();
                    ProgramCounterLabel.Text = _emulator.ProgramCounter.ToString("X2");
                    if (_emulator.CurrentStepCounter >= _emulator.StepsBeforeSleep)
                    {
                        await Task.Delay(_emulationSleepLength);
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
            _clearErrors();
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
                Emulator.Converter converter = new Emulator.Converter(CodeInputBox.Text, _emulator.Memory.MemoryData);
                _clearErrors();
                if (converter.Success)
                {
                    _emulator.SetCode(converter.Result);
                    _emulator.ResetProcessor();
                }
                else
                {
                    _recordErrors(converter.Errors);
                }
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

        private async void _displaySettings()
        {
            SettingsWindow settings = new SettingsWindow();
            await settings.ShowDialog(this);
            _emulationSleepLength = settings.EmulationSpeed;
        }

        private async void _displayHelp()
        {
            HelpWindow help = new HelpWindow();
            await help.ShowDialog(this);
        }
    }
}