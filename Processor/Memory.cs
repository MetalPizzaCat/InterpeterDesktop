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

        /// <summary>
        /// Location of where in out ports starts. Must account for at least 32 bytes. With first 16 being read only<para/>
        /// Unlike in the original processor OUT doesn't use duplicated bytes system,<para/>
        /// but rather has it's own row in the RAM. This row can be written to but not read from
        /// </summary>
        public int InOutPortsStartLocation;

        public MemorySegmentationData(int totalSize,
                                        int ramStart,
                                        int ramEnd,
                                        int romSize,
                                        int stackAddress,
                                        int stackLength,
                                        int offset,
                                        int stackPointerLocation,
                                        int inOutPortsStartLocation
        )
        {
            TotalSize = totalSize;
            RamStart = ramStart;
            RamEnd = ramEnd;
            RomSize = romSize;
            StackAddress = stackAddress;
            StackLength = stackLength;
            Offset = offset;
            StackPointerLocation = stackPointerLocation;
            InOutPortsStartLocation = inOutPortsStartLocation;
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

        public delegate void OutPortValueChangedEventHandler(int port, byte value);
        public event OutPortValueChangedEventHandler? OnOutPortValueChanged;

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
        /// Protected ROM is part of the ROM that does not get reset during usual rom writes<para/>
        /// This was not part of original processor and exists only to simplify inputting values in the emulator
        /// </summary>
        private byte[] _protectedRom;

        /// <summary>
        /// How long is the protected ROM area is<para/>
        /// Protected rom area is part of the memory that doesn't get reset during ROM write. Used for user input
        /// </summary>
        private int _protectedMemoryLength = 0x1000;

        /// <summary>
        /// Where does there protected memory start<para/>
        /// Protected rom area is part of the memory that doesn't get reset during ROM write. Used for user input
        /// </summary>
        private int _protectedMemoryStart = 0x4000;

        private MemorySegmentationData _memoryData;

        public MemorySegmentationData MemoryData => _memoryData;

        /// <summary>
        /// Overrides rom space with new rom<para/>
        /// </summary>
        /// <param name="rom"></param>
        public void WriteRom(byte[] rom)
        {
            _rom = new byte[rom.Length];
            rom.CopyTo(_rom, 0);
        }

        public byte[] ReadRom()
        {
            byte[] rom = new byte[_memoryData.RomSize];
            int totalCount = 0;
            for (int i = 0; i < _memoryData.RomSize; i++)
            {
                rom[i] = _memoryDisplayGrid[i / 16][i - (i / 16) * 16];
            }
            return rom;
        }

        public byte[] ReadRam()
        {
            byte[] ram = new byte[_memoryData.RamEnd - _memoryData.RamStart];
            Array.Copy(_memory, _memoryData.RamStart, ram, 0, _memoryData.RamEnd - _memoryData.RamStart);
            return ram;
        }

        /// <summary>
        /// Clears everything including protected ROM 
        /// </summary>
        public void ClearMemory()
        {
            _memory = new byte[_memoryData.TotalSize + 1];
            foreach (MemoryGridRow row in _memoryDisplayGrid)
            {
                for (int i = 0; i <= 0xf; i++)
                {
                    row[i] = 0;
                }
            }
            StackPointer = (ushort)_memoryData.StackAddress;
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


        /// <summary>
        /// Writes to protected rom based on values in the user input table
        /// </summary>
        private void _copyRom()
        {
            _protectedRom = new byte[_protectedMemoryLength];
            for (int i = _protectedMemoryStart / 0x10; i < (_protectedMemoryStart / 0x10 + _protectedMemoryLength / 0x10); i++)
            {
                Array.Copy(_memoryDisplayGrid[i].Memory, 0, _protectedRom, i * 0x10 - _protectedMemoryStart, 0x10);
            }
        }

        public void Reset()
        {
            _copyRom();
            _memory = new byte[_memoryData.TotalSize + 1];
            Array.Copy(_protectedRom, 0, _memory, _protectedMemoryStart, _protectedMemoryLength);
            _rom.CopyTo(_memory, 0);
            int writeOffset = 0;
            foreach (MemoryGridRow row in _memoryDisplayGrid)
            {
                for (int i = 0; i <= 0xf; i++)
                {
                    row[i] = (writeOffset >= _rom.Length ? _memory[writeOffset] : _rom[writeOffset]);
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
                if (i < _memoryData.RomSize)
                {
                    throw new ProtectedMemoryWriteException($"Attempted to write at {(i).ToString("X4")}. But 0000 to {(_memoryData.RomSize).ToString("X4")} is reserved memory");
                }
                if ((i & 0xFFF0) == ((_memoryData.InOutPortsStartLocation + 0x10) & 0xFFF0))
                {
                    OnOutPortValueChanged?.Invoke(i & 0x000F, value);
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
        public Memory(int totalSize = ushort.MaxValue, int ramStart = 0x5000, int ramEnd = 0xefd0, int romSize = 0x5000, int stackAddress = 0xFFFF, int stackLength = 0x0fff, int offset = 0x0000, int stackPointerLocation = 0xeff0)
        {

            _memoryData = new MemorySegmentationData(totalSize, ramStart, ramEnd, romSize, stackAddress, stackLength, offset, stackPointerLocation, ramEnd);
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