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
    [System.Serializable]
    public class ProtectedMemoryWriteException : System.Exception
    {
        public ProtectedMemoryWriteException() { }
        public ProtectedMemoryWriteException(string message) : base(message) { }
        public ProtectedMemoryWriteException(string message, System.Exception inner) : base(message, inner) { }
        protected ProtectedMemoryWriteException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    /// <summary>
    /// Stores memory information and provides interface for 
    /// </summary>
    public class Memory : IProcessorComponent
    {
        /// <summary>
        /// Where does addressing for the processor start
        /// </summary>
        private int _startAddress = 0x000;
        /// <summary>
        /// Where does addressing for the processor end
        /// </summary>
        private int _endAddress = 0xbb0;
        /// <summary>
        /// Where does the actual usable space begin<para/>
        /// Could be used to split memory in chunks
        /// </summary>
        private int _addressOffset = 0x800;
        /// <summary>
        /// Data grid friendly version of the memory data representation
        /// </summary>
        /// <returns></returns>
        private ObservableCollection<MemoryGridRow> _memoryDisplayGrid = new ObservableCollection<MemoryGridRow>();
        /// <summary>
        /// Memory data
        /// </summary>
        private byte[] _memory = new byte[944];

        /// <summary>
        /// Memory that program reserves for itself when it's assembled<para/>
        /// This memory can not be written to and can only be used for execution
        /// </summary>
        private int _protectedMemoryLength = 0;

        public int AddressOffset => _addressOffset;

        public int ProtectedMemoryLength
        {
            get => _protectedMemoryLength;
            set => _protectedMemoryLength = value;
        }

        public ObservableCollection<MemoryGridRow> MemoryDisplayGrid { get => _memoryDisplayGrid; /*set => _memoryDisplayGrid = value;*/ }
        public void OnMemoryValueChanged(int address, byte value)
        {
            _memory[address - _addressOffset] = value;
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
                if (i <= _protectedMemoryLength)
                {
                    throw new ProtectedMemoryWriteException($"Attempted to write at {(i + _addressOffset).ToString("X4")}. But 0x800 to {(_protectedMemoryLength + _addressOffset).ToString("X4")} is reserved memory");
                }
                int ind = i - (i / 16) * 16;
                _memoryDisplayGrid[i / 16][i - (i / 16) * 16] = value;
                _memory[i] = value;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="startingAddress">At what address the memory starts at</param>
        /// <param name="endAddress">At what address the memory ends</param>
        /// <param name="offset">What offset does the memory has. Must be less or equal to startingAddress, used for array index correction</param>
        public Memory(int startingAddress = 0x800, int endAddress = 0xbb0, int offset = 0x800)
        {
            _startAddress = startingAddress;
            _endAddress = endAddress;
            if (_endAddress <= _startAddress)
            {
                throw new Exception("Invalid memory size provided for the processor");
            }
            _memory = new byte[endAddress - startingAddress + 1];

            _addressOffset = offset;
            _memoryDisplayGrid.CollectionChanged += _onCollectionChanged;
            for (int i = _startAddress; i < _endAddress; i += 0x10)
            {
                MemoryGridRow row = new MemoryGridRow(i);
                for (int j = 0; j <= 0xf; j++)
                {
                    row[j] = _memory[((i + j) - _addressOffset)];
                }
                row.OnRowValueChanged += OnMemoryValueChanged;
                _memoryDisplayGrid.Add(row);
            }
        }
    }
}