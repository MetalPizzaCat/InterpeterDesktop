using System.Text.RegularExpressions;
using Avalonia.Data;
using System;
using System.ComponentModel;
/// <summary>
/// Represents row of the data grid that referrers to memory
/// But has input checks
/// </summary>
public class MemoryGridRow : INotifyPropertyChanged
{
    public delegate void RowValueChangedEventHandler(int address, byte value);

    public event RowValueChangedEventHandler? OnRowValueChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    private byte[] _memory = new byte[0x10];

    public byte this[int i]
    {
        get => _memory[i];
        set
        {
            _memory[i] = value;
            PropertyChanged?.Invoke($"X{i.ToString("X")}", new PropertyChangedEventArgs($"X{i}"));
        }
    }

    public int AddressValue = 0;

    public MemoryGridRow(int addressValue)
    {
        AddressValue = addressValue;
    }

    public string Address => AddressValue.ToString("X4");

    private void _validate(int offset, string value)
    {
        if (!Regex.IsMatch(value, "^[0-9A-F]+$"))
        {
            throw new DataValidationException("Must only contain hex characters");
        }
        try
        {
            _memory[offset] = Convert.ToByte(value, 16);

            OnRowValueChanged?.Invoke(AddressValue + offset, _memory[offset]);
        }
        catch (OverflowException e)
        {
            throw new DataValidationException("Value must be in 0 to FF range");
        }
    }

    //yep, it's just copy paster code
    //generated via c# script
    public string X0
    {
        get => _memory[0].ToString("X2");
        set
        {
            _validate(0, value);
        }
    }

    public string X1
    {
        get => _memory[1].ToString("X2");
        set
        {
            _validate(1, value);
        }
    }

    public string X2
    {
        get => _memory[2].ToString("X2");
        set
        {
            _validate(2, value);
        }
    }

    public string X3
    {
        get => _memory[3].ToString("X2");
        set
        {
            _validate(3, value);
        }
    }

    public string X4
    {
        get => _memory[4].ToString("X2");
        set
        {
            _validate(4, value);
        }
    }

    public string X5
    {
        get => _memory[5].ToString("X2");
        set
        {
            _validate(5, value);
        }
    }

    public string X6
    {
        get => _memory[6].ToString("X2");
        set
        {
            _validate(6, value);
        }
    }

    public string X7
    {
        get => _memory[7].ToString("X2");
        set
        {
            _validate(7, value);
        }
    }

    public string X8
    {
        get => _memory[8].ToString("X2");
        set
        {
            _validate(8, value);
        }
    }

    public string X9
    {
        get => _memory[9].ToString("X2");
        set
        {
            _validate(9, value);
        }
    }

    public string XA
    {
        get => _memory[10].ToString("X2");
        set
        {
            _validate(10, value);
        }
    }

    public string XB
    {
        get => _memory[11].ToString("X2");
        set
        {
            _validate(11, value);
        }
    }

    public string XC
    {
        get => _memory[12].ToString("X2");
        set
        {
            _validate(12, value);
        }
    }

    public string XD
    {
        get => _memory[13].ToString("X2");
        set
        {
            _validate(13, value);
        }
    }

    public string XE
    {
        get => _memory[14].ToString("X2");
        set
        {
            _validate(14, value);
        }
    }

    public string XF
    {
        get => _memory[15].ToString("X2");
        set
        {
            _validate(15, value);
        }
    }
}