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
    public record MemorySegmentationData
    {
        /// <summary>
        /// How much memory does processor get for all of it's logic
        /// </summary>
        public int TotalSize;
        /// <summary>
        /// Address where RAM begins
        /// </summary>
        public int RamStart;
        /// <summary>
        /// Address where RAM ends
        /// </summary>
        public int RamEnd;
        /// <summary>
        /// How much memory is dedicated to the program's byte code<para/>
        /// Rom starts at 0 and ends and RomSize
        /// </summary>
        public int RomSize;
        /// <summary>
        /// Address of where stack begins
        /// </summary>
        public int StackAddress;
        /// <summary>
        /// Length of the stack
        /// </summary>
        public int StackLength;
        /// <summary>
        /// Offset at which program execution starts
        /// </summary>
        public int Offset;

        /// <summary>
        /// Location in the memory where stack pointer is located<para/>
        /// This should preferably be outside of ROM and RAM
        /// </summary>
        public int StackPointerLocation;

        public MemorySegmentationData(int totalSize, int ramStart, int ramEnd, int romSize, int stackAddress, int stackLength, int offset, int stackPointerLocation)
        {
            TotalSize = totalSize;
            RamStart = ramStart;
            RamEnd = ramEnd;
            RomSize = romSize;
            StackAddress = stackAddress;
            StackLength = stackLength;
            Offset = offset;
            StackPointerLocation = stackPointerLocation;
        }
    }

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
        /// Data grid friendly version of the memory data representation
        /// </summary>
        /// <returns></returns>
        private ObservableCollection<MemoryGridRow> _memoryDisplayGrid = new ObservableCollection<MemoryGridRow>();
        /// <summary>
        /// Memory data
        /// </summary>
        private byte[] _memory;
        /// <summary>
        /// Copy of the rom that will persist thought memory resets
        /// </summary>
        private byte[] _rom;

        /// <summary>
        /// Memory that program reserves for itself when it's assembled<para/>
        /// This memory can not be written to and can only be used for execution
        /// </summary>
        private int _protectedMemoryLength = 0;

        private MemorySegmentationData _memoryData;

        public MemorySegmentationData MemoryData => _memoryData;

        public int ProtectedMemoryLength
        {
            get => _protectedMemoryLength;
            set => _protectedMemoryLength = value;
        }

        /// <summary>
        /// Overrides rom space with new rom<para/>
        /// </summary>
        /// <param name="rom"></param>
        public void WriteRom(byte[] rom)
        {
            _rom = new byte[rom.Length];
            rom.CopyTo(_rom,0);
        }
        /// <summary>
        /// Returns value in the memory that was dedicated for stack pointer
        /// </summary>
        /// <returns></returns>
        public ushort StackPointer
        {
            get => (ushort)(((int)_memory[_memoryData.StackPointerLocation + 1] << 8) | (int)_memory[_memoryData.StackPointerLocation]);
            set
            {
                if (value < _memoryData.StackAddress - _memoryData.StackLength)
                {
                    throw new Exception("Emulator stack overflow");
                }
                this[(ushort)(_memoryData.StackPointerLocation + 1)] = (byte)(value >> 8);
                this[(ushort)(_memoryData.StackPointerLocation)] = (byte)(value & 0xff);

                byte h = this[(ushort)(_memoryData.StackPointerLocation + 1)];
                byte l = this[(ushort)(_memoryData.StackPointerLocation)];

                Console.WriteLine($"{h.ToString("X2")}{l.ToString("X2")} is {StackPointer.ToString("X4")}");
            }
        }

        public ObservableCollection<MemoryGridRow> MemoryDisplayGrid { get => _memoryDisplayGrid; }
        public void OnMemoryValueChanged(int address, byte value)
        {
            _memory[address] = value;
        }

        private void _onCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Console.WriteLine("Memory modified");
        }

        public void Reset()
        {
            _memory = new byte[_memoryData.TotalSize + 1];
            _rom.CopyTo(_memory, 0);
            int writeOffset = 0;
            foreach (MemoryGridRow row in _memoryDisplayGrid)
            {
                for (int i = 0; i < 0x10; i++)
                {
                    row[i] = (writeOffset >= _rom.Length ? (byte)0 : _rom[writeOffset]);
                    writeOffset++;
                }
            }
            StackPointer = (ushort)_memoryData.StackAddress;
        }

        public byte this[ushort i]
        {
            get => _memory[i];
            set
            {
                if (i <= _memoryData.RomSize)
                {
                    throw new ProtectedMemoryWriteException($"Attempted to write at {(i).ToString("X4")}. But 0000 to {(_memoryData.RomSize).ToString("X4")} is reserved memory");
                }
                int ind = i - (i / 16) * 16;
                _memoryDisplayGrid[i / 16][i - (i / 16) * 16] = value;
                _memory[i] = value;
            }
        }

        /// <summary>
        /// Creates a new instance of the emulator and throws an error if values are invalid
        /// </summary>
        /// <param name="totalSize">Total memory available in the emulator</param>
        /// <param name="ramStart">Where does ram address start</param>
        /// <param name="ramEnd">Where ram ends</param>
        /// <param name="romSize">Amount of memory dedicated to program. ROM starts at 0</param>
        /// <param name="stackAddress">Where does stack begin</param>
        /// <param name="stackLength">How big is the start</param>
        /// <param name="offset">This will dictate where will the program start, must be less then romSize</param>
        public Memory(int totalSize = ushort.MaxValue, int ramStart = 0x0000, int ramEnd = 0xeff0, int romSize = 0x5000, int stackAddress = 0xFFFF, int stackLength = 0x0fff, int offset = 0x0000, int stackPointerLocation = 0xeff0)
        {

            _memoryData = new MemorySegmentationData(totalSize, ramStart, ramEnd, romSize, stackAddress, stackLength, offset, stackPointerLocation);
            _memoryDisplayGrid.CollectionChanged += _onCollectionChanged;
            _memory = new byte[totalSize + 1];
            for (int i = 0; i < totalSize; i += 0x10)
            {
                MemoryGridRow row = new MemoryGridRow(i);
                for (int j = 0; j <= 0xf; j++)
                {
                    row[j] = _memory[i + j];
                }
                row.OnRowValueChanged += OnMemoryValueChanged;
                _memoryDisplayGrid.Add(row);
            }
            StackPointer = (ushort)stackAddress;
        }
    }
}