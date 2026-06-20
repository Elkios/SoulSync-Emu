using System.Collections.Generic;
using SoulSync.Core.Memory;
using SoulSync.Core.Models;

namespace SoulSync.Games.Gen5
{
    /// <summary>
    /// Pokémon Black 2 / White 2 (Gen 5, NDS). Reads the persistent party buffer from Main RAM
    /// and decrypts each slot. Ported from the validated Lua tracker.
    ///
    /// Party base differs by version (Black2 vs White2); region (PAL/FR/US/JP) shares the same
    /// address (per yPokeStats). Detection = whichever base holds a coherent slot 0.
    /// </summary>
    public sealed class BlackWhite2Adapter : IGameAdapter
    {
        public string Id => "b2w2";
        public EmuSystem System => EmuSystem.NDS;
        public IReadOnlyList<string> Regions { get; } = new[] { "INTL", "JP" };

        private const string Domain = "Main RAM";
        private const long MainRamBase = 0x02000000;
        private const int SlotSize = 0xDC;   // 220 bytes per party Pokémon
        private const int PartyMax = 6;

        private static readonly (string Region, long Base)[] Candidates =
        {
            ("Black2", 0x0221E3EC),
            ("White2", 0x0221E42C),
        };

        // --- little-endian helpers, addresses are absolute DS addresses ---
        private static ushort U16(IMemoryReader m, long abs) => m.ReadU16(Domain, abs - MainRamBase);
        private static uint U32(IMemoryReader m, long abs) => m.ReadU32(Domain, abs - MainRamBase);

        public bool Matches(IMemoryReader mem, RomInfo rom)
        {
            if (rom.System != EmuSystem.NDS) return false;
            foreach (var c in Candidates)
                if (ReadSlot(mem, c.Base, 0) is not null) return true;
            return false;
        }

        public Party ReadParty(IMemoryReader mem)
        {
            foreach (var (region, baseAddr) in Candidates)
            {
                if (ReadSlot(mem, baseAddr, 0) is null) continue;
                var members = new List<Mon>();
                for (var i = 0; i < PartyMax; i++)
                {
                    var mon = ReadSlot(mem, baseAddr, i);
                    if (mon is not null) members.Add(mon);
                }
                return new Party(Id, region, members);
            }
            return new Party(Id, "Unknown", new List<Mon>());
        }

        /// <summary>Read &amp; decrypt one slot; null if empty/incoherent.</summary>
        private static Mon? ReadSlot(IMemoryReader mem, long baseAddr, int index)
        {
            var slot = baseAddr + index * SlotSize;
            var pid = U32(mem, slot);
            if (pid == 0) return null;

            // Battle-stats region (offset 0x88+) is XOR-encrypted with a PID-seeded LCG keystream.
            uint seed = pid;
            var s = new ushort[5];
            for (var i = 0; i < 5; i++)
            {
                seed = Gen5Crypto.Lcg(seed);
                var key = (ushort)(seed >> 16);
                s[i] = (ushort)(U16(mem, slot + 0x88 + i * 2) ^ key);
            }
            int level = s[2] & 0xFF;   // low byte of u16 #2
            int curHp = s[3];
            int maxHp = s[4];

            if (level < 1 || level > 100 || maxHp < 1 || maxHp > 999 || curHp > maxHp)
                return null;

            int species = ReadSpecies(mem, slot, pid);
            return new Mon(pid, species, level, curHp, maxHp, InParty: true);
        }

        /// <summary>
        /// Species (National Dex id) sits at the start of logical Block A, whose physical
        /// position depends on the PID block-shuffle. Block region (0x08+) is XOR-encrypted
        /// with a checksum-seeded LCG keystream.
        /// </summary>
        private static int ReadSpecies(IMemoryReader mem, long slot, uint pid)
        {
            var shuffle = (int)(((pid >> 13) & 31) % 24);
            var physA = Gen5Crypto.BlockPos[shuffle * 4];
            var u16Index = physA * 16; // (0x20 * physA) / 2

            uint seed = U16(mem, slot + 0x06); // checksum
            ushort key = 0;
            for (var k = 0; k <= u16Index; k++)
            {
                seed = Gen5Crypto.Lcg(seed);
                key = (ushort)(seed >> 16);
            }
            return (ushort)(U16(mem, slot + 0x08 + u16Index * 2) ^ key);
        }
    }
}
