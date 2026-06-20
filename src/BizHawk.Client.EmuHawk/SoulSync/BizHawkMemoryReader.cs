#nullable enable

using BizHawk.Emulation.Common;

using SoulSync.Core.Memory;

namespace BizHawk.Client.EmuHawk.SoulSync
{
	/// <summary>
	/// <see cref="IMemoryReader"/> implementation backed by BizHawk's <see cref="IMemoryDomains"/>
	/// (the same memory the Lua "memory" library and the Hex Editor read). This is the single
	/// place where the pure SoulSync domain meets the emulator, per the dependency rule.
	/// <para>
	/// Addresses passed in are domain-relative offsets (the game adapter already subtracts the
	/// console's RAM base, e.g. 0x02000000 for the NDS "Main RAM" domain). All reads are
	/// little-endian. If no core is loaded, the requested domain is missing, or an address is
	/// out of range, reads return 0 / empty so the bridge tick can never throw on a read.
	/// </para>
	/// </summary>
	public sealed class BizHawkMemoryReader : IMemoryReader
	{
		private readonly Func<IMemoryDomains?> _getDomains;

		public BizHawkMemoryReader(Func<IMemoryDomains?> getDomains)
			=> _getDomains = getDomains ?? throw new ArgumentNullException(nameof(getDomains));

		private MemoryDomain? Resolve(string domain)
		{
			var domains = _getDomains();
			if (domains is null) return null;
			return domains.Has(domain) ? domains[domain] : null;
		}

		private static bool InRange(MemoryDomain dom, long address, int length)
			=> address >= 0 && address + length <= dom.Size;

		public byte ReadU8(string domain, long address)
		{
			var dom = Resolve(domain);
			if (dom is null || !InRange(dom, address, 1)) return 0;
			return dom.PeekByte(address);
		}

		public ushort ReadU16(string domain, long address)
		{
			var dom = Resolve(domain);
			if (dom is null || !InRange(dom, address, 2)) return 0;
			return dom.PeekUshort(address, bigEndian: false);
		}

		public uint ReadU32(string domain, long address)
		{
			var dom = Resolve(domain);
			if (dom is null || !InRange(dom, address, 4)) return 0;
			return dom.PeekUint(address, bigEndian: false);
		}

		public byte[] ReadBytes(string domain, long address, int length)
		{
			var dom = Resolve(domain);
			if (dom is null || length <= 0 || !InRange(dom, address, length)) return Array.Empty<byte>();
			var dst = new byte[length];
			for (var i = 0; i < length; i++) dst[i] = dom.PeekByte(address + i);
			return dst;
		}
	}
}
