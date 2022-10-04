using System.Collections.ObjectModel;

/// <summary>
/// Stores memory information and provides interface for 
/// </summary>
public class Memory
{
    /// <summary>
    /// Data grid friendly version of the memory data representation
    /// </summary>
    /// <returns></returns>
    private ObservableCollection<ObservableCollection<string>> _memoryDisplayGrid = new ObservableCollection<ObservableCollection<string>>();
    /// <summary>
    /// Memory data
    /// </summary>
    private byte[] _memory = new byte[944];
    public ObservableCollection<ObservableCollection<string>> MemoryDisplayGrid { get => _memoryDisplayGrid; set => _memoryDisplayGrid = value;}
    public Memory()
    {
        for (int i = 0; i < 944 / 10; i++)
        {
            var row = new ObservableCollection<string> { $"{(0x800 + i * 0x10).ToString("X2")}" };
            for (int j = 0; j < 0xf; j++)
            {
                row.Add(j.ToString("X2"));
            }
            _memoryDisplayGrid.Add(row);
        }
    }
}