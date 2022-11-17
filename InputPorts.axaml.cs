using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Avalonia.Data;
using System.Collections.ObjectModel;

namespace Nema
{
    public partial class InputPorts : UserControl
    {
        public delegate void PortValueChangedEventHandler(int port, byte value);
        public event PortValueChangedEventHandler? OnPortValueChanged;
        public static readonly int PortCount = 16;
        public ObservableCollection<MemoryGridRow> Ports { get; } = new ObservableCollection<MemoryGridRow>();

        /// <summary>
        /// Changes what value is currently inputed in the table
        /// </summary>
        /// <param name="port"></param>
        /// <param name="value"></param>
        public void SetPortValue(int port, byte value)
        {
            Ports[port / 16][port - port / 16] = value;
        }

        public void ResetPorts()
        {
            for (int i = 0; i < PortCount; i++)
            {
                SetPortValue(i, 0);
            }
        }

        public InputPorts()
        {
            InitializeComponent();
            Ports.Add(new MemoryGridRow(0));
            Ports[0].OnRowValueChanged += _inputChanged;
            PortGrid.Items = Ports;
            this.DataContext = this;
        }

        private void _inputChanged(int port, byte value)
        {
            OnPortValueChanged?.Invoke(port, value);
        }
    }
}