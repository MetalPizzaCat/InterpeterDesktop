
using System.ComponentModel;
namespace Interpreter
{
    public class Registers : INotifyPropertyChanged, IProcessorComponent
    {
        private byte _a = 0;
        private byte _b = 0;
        private byte _c = 0;
        private byte _d = 0;
        private byte _e = 0;
        private byte _h = 0;
        private byte _l = 0;

        public byte A
        {
            get => _a;
            set
            {
                _a = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("A"));
            }
        }



        public byte B
        {
            get => _b;
            set
            {
                _b = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("B"));
            }
        }


        public byte C
        {
            get => _c;
            set
            {
                _c = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("C"));
            }
        }


        public byte D
        {
            get => _d;
            set
            {
                _d = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("D"));
            }
        }


        public byte E
        {
            get => _e;
            set
            {
                _e = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("E"));
            }
        }


        public byte H
        {
            get => _h;
            set
            {
                _h = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("H"));
            }
        }


        public byte L
        {
            get => _l;
            set
            {
                _l = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("L"));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Reset()
        {
            A = 0;
            B = 0;
            C = 0;
            D = 0;
            E = 0;
            H = 0;
            L = 0;
        }
    }
}