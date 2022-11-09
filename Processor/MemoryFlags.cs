
using System.ComponentModel;
namespace Emulator
{
    public class ProcessorFlags : INotifyPropertyChanged, IProcessorComponent
    {
        private bool _s = false;
        private bool _z = false;
        private bool _ac = false;
        private bool _p = false;
        private bool _c = false;


        public bool S
        {
            get => _s;
            set
            {
                _s = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("S"));
            }
        }


        public bool Z
        {
            get => _z;
            set
            {
                _z = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Z"));
            }
        }


        public bool Ac
        {
            get => _ac;
            set
            {
                _ac = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Ac"));
            }
        }


        public bool P
        {
            get => _p;
            set
            {
                _p = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("P"));
            }
        }


        public bool C
        {
            get => _c;
            set
            {
                _c = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("C"));
            }
        }


        public event PropertyChangedEventHandler? PropertyChanged;

        public void Reset()
        {
            S = false;
            Z = false;
            Ac = false;
            P = false;
            C = false;
        }
    }
}