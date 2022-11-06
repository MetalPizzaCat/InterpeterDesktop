using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Avalonia.Data;
using System.Collections.ObjectModel;

namespace InterpreterDesktop
{
    public partial class InputPorts : UserControl
    {
        public delegate void PortValueChangedEventHandler(int port, byte value);
        public event PortValueChangedEventHandler? OnPortValueChanged;
        public static readonly int PortCount = 16;
        public ObservableCollection<byte> Ports { get; } = new ObservableCollection<byte>();

        public InputPorts()
        {
            InitializeComponent();
            for (int i = 0; i < PortCount; i++)
            {
                PortGrid.Columns.Add(new DataGridTextColumn() { Header = $"P{i}", Binding = new Binding($"[{i}]"), Width = DataGridLength.SizeToHeader });
                Ports.Add((byte)i);
            }
            PortGrid.AutoGenerateColumns = false;
            PortGrid.Items = Ports;
            this.DataContext = this;
        }
    }
}