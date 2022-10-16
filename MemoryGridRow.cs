using System.Text.RegularExpressions;
using Avalonia.Data;
using System;
/// <summary>
/// Represents row of the data grid that referrers to memory
/// But has input checks
/// </summary>
public class MemoryGridRow
{
    public delegate void RowValueChangedEventHandler(int address, byte value);

    public event RowValueChangedEventHandler? OnRowValueChanged;
    public byte[] Memory = new byte[0x10];
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
            Memory[offset] = Convert.ToByte(value, 16);
            OnRowValueChanged?.Invoke(AddressValue + offset, Memory[offset]);
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
        get => Memory[0].ToString("X2");
        set
        {
            _validate(0, value);
        }
    }

    public string X1
    {
        get => Memory[1].ToString("X2");
        set
        {
            _validate(1, value);
        }
    }

    public string X2
    {
        get => Memory[2].ToString("X2");
        set
        {
            _validate(2, value);
        }
    }

    public string X3
    {
        get => Memory[3].ToString("X2");
        set
        {
            _validate(3, value);
        }
    }

    public string X4
    {
        get => Memory[4].ToString("X2");
        set
        {
            _validate(4, value);
        }
    }

    public string X5
    {
        get => Memory[5].ToString("X2");
        set
        {
            _validate(5, value);
        }
    }

    public string X6
    {
        get => Memory[6].ToString("X2");
        set
        {
            _validate(6, value);
        }
    }

    public string X7
    {
        get => Memory[7].ToString("X2");
        set
        {
            _validate(7, value);
        }
    }

    public string X8
    {
        get => Memory[8].ToString("X2");
        set
        {
            _validate(8, value);
        }
    }

    public string X9
    {
        get => Memory[9].ToString("X2");
        set
        {
            _validate(9, value);
        }
    }

    public string XA
    {
        get => Memory[10].ToString("X2");
        set
        {
            _validate(10, value);
        }
    }

    public string XB
    {
        get => Memory[11].ToString("X2");
        set
        {
            _validate(11, value);
        }
    }

    public string XC
    {
        get => Memory[12].ToString("X2");
        set
        {
            _validate(12, value);
        }
    }

    public string XD
    {
        get => Memory[13].ToString("X2");
        set
        {
            _validate(13, value);
        }
    }

    public string XE
    {
        get => Memory[14].ToString("X2");
        set
        {
            _validate(14, value);
        }
    }

    public string XF
    {
        get => Memory[15].ToString("X2");
        set
        {
            _validate(15, value);
        }
    }
}