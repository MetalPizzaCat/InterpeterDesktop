using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Nema
{
    public partial class SettingsWindow : Window
    {
        private int _emulationSpeed = 100;
        public int EmulationSpeed { get; set; } = 100;
        public SettingsWindow()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        private void _exit()
        {
            Close();
        }
    }
}