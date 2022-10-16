using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;
using Avalonia.Data;

/// <summary>
/// Stores memory information and provides interface for 
/// </summary>
public class Memory
{
    /// <summary>
    /// Data grid friendly version of the memory data representation
    /// </summary>
    /// <returns></returns>
    private ObservableCollection<MemoryGridRow> _memoryDisplayGrid = new ObservableCollection<MemoryGridRow>();
    /// <summary>
    /// Memory data
    /// </summary>
    private byte[] _memory = new byte[944];
    public ObservableCollection<MemoryGridRow> MemoryDisplayGrid { get => _memoryDisplayGrid; /*set => _memoryDisplayGrid = value;*/ }
    /*
        private void _onGridChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            //TODO: Add input validation
            if (e.Action == NotifyCollectionChangedAction.Replace)
            {
                _memory[(_memoryDisplayGrid.IndexOf(sender as ObservableCollection<string>) * 0xf + e.NewStartingIndex)] = Convert.ToByte((e.NewItems[0] as string), 16);
                System.Console.WriteLine($"Changed at {((_memoryDisplayGrid.IndexOf(sender as ObservableCollection<string>) * 0xf + e.NewStartingIndex) + 0x800).ToString("X2")} to {e.OldItems}");
            }
        }*/
    public Memory()
    {
        for (int i = 0x800; i < 0xbb0; i += 0x10)
        {
            var row = new MemoryGridRow(i);
            for (int j = 0; j <= 0xf; j++)
            {
                row.Memory[j] = _memory[((i + j) - 0x800)];
            }
            //row.CollectionChanged += _onGridChanged;
            _memoryDisplayGrid.Add(row);
        }
    }
}