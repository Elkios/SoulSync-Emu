using System;
using SoulSync.Core.Memory;

namespace SoulSync.Tests
{
    /// <summary>
    /// Test double for <see cref="IMemoryReader"/> backed by an in-memory byte buffer.
    /// Lets game adapters be tested against a RAM dump — no BizHawk, no emulator.
    /// </summary>
    public sealed class DumpMemoryReader : IMemoryReader
    {
        private readonly byte[] _ram;

        public DumpMemoryReader(byte[] ram) => _ram = ram;

        public byte ReadU8(string domain, long address) => _ram[address];

        public ushort ReadU16(string domain, long address)
            => (ushort)(_ram[address] | (_ram[address + 1] << 8));

        public uint ReadU32(string domain, long address)
            => (uint)(_ram[address] | (_ram[address + 1] << 8) | (_ram[address + 2] << 16) | (_ram[address + 3] << 24));

        public byte[] ReadBytes(string domain, long address, int length)
        {
            var dst = new byte[length];
            Array.Copy(_ram, address, dst, 0, length);
            return dst;
        }
    }
}
