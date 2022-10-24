using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;
using Avalonia.Data;
using System.ComponentModel;

namespace Interpreter
{
    /// <summary>
    /// Stores memory information and provides interface for 
    /// </summary>
    public class Memory : IProcessorComponent
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
        public void OnMemoryValueChanged(int address, byte value)
        {
            _memory[address - 0x800] = value;
        }


        private void _onCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Console.WriteLine("Memory modified");
        }

        public void Reset()
        {
            _memory = new byte[944];
            foreach (MemoryGridRow row in _memoryDisplayGrid)
            {
                for (int i = 0; i < 0x10; i++)
                {
                    row[i] = 0;
                }
            }
        }

        public byte this[ushort i]
        {
            get => _memory[i];
            set
            {
                int ind = i - (i / 16) * 16;
                _memoryDisplayGrid[i / 16][i - (i / 16) * 16] = value;
                _memory[i] = value;
            }
        }

        public Memory()
        {
            _memoryDisplayGrid.CollectionChanged += _onCollectionChanged;
            for (int i = 0x800; i < 0xbb0; i += 0x10)
            {
                MemoryGridRow row = new MemoryGridRow(i);
                for (int j = 0; j <= 0xf; j++)
                {
                    row[j] = _memory[((i + j) - 0x800)];
                }
                row.OnRowValueChanged += OnMemoryValueChanged;
                _memoryDisplayGrid.Add(row);
            }
        }
    }
}