namespace SoulSync.Core.Memory
{
    /// <summary>
    /// The seam between game adapters and the emulator. Game logic depends ONLY on this
    /// abstraction, never on BizHawk, so it stays testable (with a RAM dump) and portable.
    /// Addresses are offsets within the named memory <paramref name="domain"/> (e.g. "Main RAM").
    /// All multi-byte reads are little-endian.
    /// </summary>
    public interface IMemoryReader
    {
        byte ReadU8(string domain, long address);
        ushort ReadU16(string domain, long address);
        uint ReadU32(string domain, long address);
        byte[] ReadBytes(string domain, long address, int length);
    }
}
