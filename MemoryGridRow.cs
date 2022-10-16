using System.Text.RegularExpressions;
using Avalonia.Data;
using System;
/// <summary>
/// Represents row of the data grid that referrers to memory
/// But has input checks
/// </summary>
public class MemoryGridRow
{
    public byte[] Memory = new byte[0x10];
    public int AddressValue = 0;

    public MemoryGridRow(int addressValue)
    {
        AddressValue = addressValue;
    }

    public string Address => AddressValue.ToString("X4");

    //yep, it's just copy paster code
    //generated via c# script
    public string X0
    {
        get => Memory[0].ToString("X2");
        set
        {
            if (!Regex.IsMatch(value, "^[0-9A-F]+$"))
            {
                throw new DataValidationException("Must only contain hex characters");
            }
            try
            {
                Memory[0] = Convert.ToByte(value, 16);
            }
            catch (OverflowException e)
            {
                throw new DataValidationException("Value must be in 0 to FF range");
            }
        }
    }

    public string X1
    {
        get => Memory[1].ToString("X2");
        set
        {
            if (!Regex.IsMatch(value, "^[0-9A-F]+$"))
            {
                throw new DataValidationException("Must only contain hex characters");
            }
            try
            {
                Memory[1] = Convert.ToByte(value, 16);
            }
            catch (OverflowException e)
            {
                throw new DataValidationException("Value must be in 0 to FF range");
            }
        }
    }

    public string X2
    {
        get => Memory[2].ToString("X2");
        set
        {
            if (!Regex.IsMatch(value, "^[0-9A-F]+$"))
            {
                throw new DataValidationException("Must only contain hex characters");
            }
            try
            {
                Memory[2] = Convert.ToByte(value, 16);
            }
            catch (OverflowException e)
            {
                throw new DataValidationException("Value must be in 0 to FF range");
            }
        }
    }

    public string X3
    {
        get => Memory[3].ToString("X2");
        set
        {
            if (!Regex.IsMatch(value, "^[0-9A-F]+$"))
            {
                throw new DataValidationException("Must only contain hex characters");
            }
            try
            {
                Memory[3] = Convert.ToByte(value, 16);
            }
            catch (OverflowException e)
            {
                throw new DataValidationException("Value must be in 0 to FF range");
            }
        }
    }

    public string X4
    {
        get => Memory[4].ToString("X2");
        set
        {
            if (!Regex.IsMatch(value, "^[0-9A-F]+$"))
            {
                throw new DataValidationException("Must only contain hex characters");
            }
            try
            {
                Memory[4] = Convert.ToByte(value, 16);
            }
            catch (OverflowException e)
            {
                throw new DataValidationException("Value must be in 0 to FF range");
            }
        }
    }

    public string X5
    {
        get => Memory[5].ToString("X2");
        set
        {
            if (!Regex.IsMatch(value, "^[0-9A-F]+$"))
            {
                throw new DataValidationException("Must only contain hex characters");
            }
            try
            {
                Memory[5] = Convert.ToByte(value, 16);
            }
            catch (OverflowException e)
            {
                throw new DataValidationException("Value must be in 0 to FF range");
            }
        }
    }

    public string X6
    {
        get => Memory[6].ToString("X2");
        set
        {
            if (!Regex.IsMatch(value, "^[0-9A-F]+$"))
            {
                throw new DataValidationException("Must only contain hex characters");
            }
            try
            {
                Memory[6] = Convert.ToByte(value, 16);
            }
            catch (OverflowException e)
            {
                throw new DataValidationException("Value must be in 0 to FF range");
            }
        }
    }

    public string X7
    {
        get => Memory[7].ToString("X2");
        set
        {
            if (!Regex.IsMatch(value, "^[0-9A-F]+$"))
            {
                throw new DataValidationException("Must only contain hex characters");
            }
            try
            {
                Memory[7] = Convert.ToByte(value, 16);
            }
            catch (OverflowException e)
            {
                throw new DataValidationException("Value must be in 0 to FF range");
            }
        }
    }

    public string X8
    {
        get => Memory[8].ToString("X2");
        set
        {
            if (!Regex.IsMatch(value, "^[0-9A-F]+$"))
            {
                throw new DataValidationException("Must only contain hex characters");
            }
            try
            {
                Memory[8] = Convert.ToByte(value, 16);
            }
            catch (OverflowException e)
            {
                throw new DataValidationException("Value must be in 0 to FF range");
            }
        }
    }

    public string X9
    {
        get => Memory[9].ToString("X2");
        set
        {
            if (!Regex.IsMatch(value, "^[0-9A-F]+$"))
            {
                throw new DataValidationException("Must only contain hex characters");
            }
            try
            {
                Memory[9] = Convert.ToByte(value, 16);
            }
            catch (OverflowException e)
            {
                throw new DataValidationException("Value must be in 0 to FF range");
            }
        }
    }

    public string XA
    {
        get => Memory[10].ToString("X2");
        set
        {
            if (!Regex.IsMatch(value, "^[0-9A-F]+$"))
            {
                throw new DataValidationException("Must only contain hex characters");
            }
            try
            {
                Memory[10] = Convert.ToByte(value, 16);
            }
            catch (OverflowException e)
            {
                throw new DataValidationException("Value must be in 0 to FF range");
            }
        }
    }

    public string XB
    {
        get => Memory[11].ToString("X2");
        set
        {
            if (!Regex.IsMatch(value, "^[0-9A-F]+$"))
            {
                throw new DataValidationException("Must only contain hex characters");
            }
            try
            {
                Memory[11] = Convert.ToByte(value, 16);
            }
            catch (OverflowException e)
            {
                throw new DataValidationException("Value must be in 0 to FF range");
            }
        }
    }

    public string XC
    {
        get => Memory[12].ToString("X2");
        set
        {
            if (!Regex.IsMatch(value, "^[0-9A-F]+$"))
            {
                throw new DataValidationException("Must only contain hex characters");
            }
            try
            {
                Memory[12] = Convert.ToByte(value, 16);
            }
            catch (OverflowException e)
            {
                throw new DataValidationException("Value must be in 0 to FF range");
            }
        }
    }

    public string XD
    {
        get => Memory[13].ToString("X2");
        set
        {
            if (!Regex.IsMatch(value, "^[0-9A-F]+$"))
            {
                throw new DataValidationException("Must only contain hex characters");
            }
            try
            {
                Memory[13] = Convert.ToByte(value, 16);
            }
            catch (OverflowException e)
            {
                throw new DataValidationException("Value must be in 0 to FF range");
            }
        }
    }

    public string XE
    {
        get => Memory[14].ToString("X2");
        set
        {
            if (!Regex.IsMatch(value, "^[0-9A-F]+$"))
            {
                throw new DataValidationException("Must only contain hex characters");
            }
            try
            {
                Memory[14] = Convert.ToByte(value, 16);
            }
            catch (OverflowException e)
            {
                throw new DataValidationException("Value must be in 0 to FF range");
            }
        }
    }

    public string XF
    {
        get => Memory[15].ToString("X2");
        set
        {
            if (!Regex.IsMatch(value, "^[0-9A-F]+$"))
            {
                throw new DataValidationException("Must only contain hex characters");
            }
            try
            {
                Memory[15] = Convert.ToByte(value, 16);
            }
            catch (OverflowException e)
            {
                throw new DataValidationException("Value must be in 0 to FF range");
            }
        }
    }
}