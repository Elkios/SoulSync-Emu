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

        // World / battle addresses (absolute DS addresses; see docs/RAM_B2W2.md).
        private const long MapIdAddr = 0x02246848;   // u16, current map header (parent)
        private const long ChildMapAddr = 0x02246860; // u16, current map header (child)
        private const long InBattleAddr = 0x021B5178; // u16, == 0x2100 while in battle
        private const long WildFlagAddr = 0x02257332; // u16 "enemyTID", 0 == wild
        private const long EnemyBase = 0x02258874;    // decode like a party slot (valid in battle)
        private const ushort InBattleMagic = 0x2100;

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

        // ---------- world / battle reads ----------

        /// <summary>Current map id (the player's position). Drives zone-entry notifications.</summary>
        public int ReadMapId(IMemoryReader mem) => U16(mem, MapIdAddr);

        /// <summary>
        /// Current location NAME (the Soul Link zone), resolved from the live map header.
        /// Child map takes precedence over parent; sub-maps of one area share a name.
        /// Returns null for unknown/transition map headers (so they don't create spurious zones).
        /// </summary>
        public string? ReadLocationName(IMemoryReader mem)
        {
            var child = U16(mem, ChildMapAddr);
            if (BlackWhite2Maps.Names.TryGetValue(child, out var n)) return n;
            var parent = U16(mem, MapIdAddr);
            return BlackWhite2Maps.Names.TryGetValue(parent, out n) ? n : null;
        }

        /// <summary>True while the game is in a battle (HP only settles at battle end in Gen 5).</summary>
        public bool IsInBattle(IMemoryReader mem) => U16(mem, InBattleAddr) == InBattleMagic;

        /// <summary>True if the current battle is a wild encounter (enemy trainer id == 0).
        /// Only meaningful while <see cref="IsInBattle"/> is true.</summary>
        public bool IsWildBattle(IMemoryReader mem) => U16(mem, WildFlagAddr) == 0;

        /// <summary>National Dex id of the enemy Pokémon. Only valid while in battle.</summary>
        public int ReadEnemySpecies(IMemoryReader mem)
        {
            var pid = U32(mem, EnemyBase);
            if (pid == 0) return 0;
            return ReadSpecies(mem, EnemyBase, pid);
        }

        // ---------- slot decode ----------

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

            // Decrypt the shuffled block region (0x08..0x87) once and read the logical fields.
            var blocks = DecryptBlocks(mem, slot);
            var shuffle = (int)(((pid >> 13) & 31) % 24);

            int species = ReadBlockU16(blocks, shuffle, 0x08);
            int tid = ReadBlockU16(blocks, shuffle, 0x0C);
            int sid = ReadBlockU16(blocks, shuffle, 0x0E);
            int metLocation = ReadBlockU16(blocks, shuffle, 0x80);
            int metLevel = ReadBlockU16(blocks, shuffle, 0x84) & 0xFF;
            bool shiny = IsShiny(tid, sid, pid);

            return new Mon(pid, species, level, curHp, maxHp, InParty: true,
                MetLocation: metLocation, MetLevel: metLevel, Shiny: shiny);
        }

        /// <summary>Shiny derivation: (TID ^ SID ^ PID_hi ^ PID_lo) &lt; 8.</summary>
        private static bool IsShiny(int tid, int sid, uint pid)
            => (tid ^ sid ^ (int)(pid >> 16) ^ (int)(pid & 0xFFFF)) < 8;

        /// <summary>
        /// Decrypts the 0x80-byte shuffled block region (logical 0x08..0x87, i.e. 64 u16 words)
        /// using the checksum-seeded LCG keystream. Returns the plaintext indexed by *physical*
        /// u16 index (0..63), exactly as it sits in RAM after the PID block-shuffle.
        /// </summary>
        private static ushort[] DecryptBlocks(IMemoryReader mem, long slot)
        {
            var plain = new ushort[64];
            uint seed = U16(mem, slot + 0x06); // checksum
            for (var i = 0; i < 64; i++)
            {
                seed = Gen5Crypto.Lcg(seed);
                var key = (ushort)(seed >> 16);
                plain[i] = (ushort)(U16(mem, slot + 0x08 + i * 2) ^ key);
            }
            return plain;
        }

        /// <summary>
        /// Reads a u16 at an absolute logical offset (0x08..0x87) from the decrypted block region,
        /// resolving the PID block-shuffle. Logical layout: A=0x08-0x27, B=0x28-0x47,
        /// C=0x48-0x67, D=0x68-0x87. Each block is 0x20 bytes (16 u16).
        /// </summary>
        private static int ReadBlockU16(ushort[] plain, int shuffle, int logicalOffset)
        {
            var blockLetter = (logicalOffset - 0x08) / 0x20;     // 0=A,1=B,2=C,3=D
            var offsetInBlock = (logicalOffset - 0x08) % 0x20;   // byte offset within the block
            var physBlock = Gen5Crypto.BlockPos[shuffle * 4 + blockLetter];
            var physIndex = physBlock * 16 + offsetInBlock / 2;
            return plain[physIndex];
        }

        /// <summary>
        /// Species (National Dex id) sits at the start of logical Block A. Standalone decode used
        /// for the in-battle enemy slot (which is not part of the party buffer).
        /// </summary>
        private static int ReadSpecies(IMemoryReader mem, long slot, uint pid)
        {
            var blocks = DecryptBlocks(mem, slot);
            var shuffle = (int)(((pid >> 13) & 31) % 24);
            return ReadBlockU16(blocks, shuffle, 0x08);
        }
    }
}
