using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoulSync.Games;
using SoulSync.Games.Gen5;

namespace SoulSync.Tests
{
    /// <summary>
    /// Validates the B2W2 adapter end-to-end (detection + Gen 5 stat decryption) against a
    /// synthetic RAM buffer — proving game logic is testable without launching the emulator.
    /// </summary>
    [TestClass]
    public sealed class BlackWhite2AdapterTests
    {
        private const long MainRamBase = 0x02000000;
        private const long White2Base = 0x0221E42C;

        /// <summary>Writes an ENCRYPTED party slot (same XOR keystream as decryption).</summary>
        private static void WriteSlot(byte[] ram, long slotAbs, uint pid, int level, int hp, int maxHp)
        {
            var off = slotAbs - MainRamBase;
            // PID (u32 LE) at +0x00
            ram[off + 0] = (byte)pid;
            ram[off + 1] = (byte)(pid >> 8);
            ram[off + 2] = (byte)(pid >> 16);
            ram[off + 3] = (byte)(pid >> 24);
            // battle-stats region at +0x88 : u16 #0..4 = status, status, level, curHP, maxHP
            var plain = new ushort[] { 0, 0, (ushort)level, (ushort)hp, (ushort)maxHp };
            uint seed = pid;
            for (var i = 0; i < 5; i++)
            {
                seed = Gen5Crypto.Lcg(seed);
                var key = (ushort)(seed >> 16);
                var enc = (ushort)(plain[i] ^ key);
                ram[off + 0x88 + i * 2] = (byte)enc;
                ram[off + 0x88 + i * 2 + 1] = (byte)(enc >> 8);
            }
        }

        [TestMethod]
        public void Lcg_MatchesKnownSequence()
        {
            // X = 0x41C64E6D*X + 0x6073 (mod 2^32), seed 0
            Assert.AreEqual(0x00006073u, Gen5Crypto.Lcg(0));
            Assert.AreEqual(Gen5Crypto.Lcg(Gen5Crypto.Lcg(0)), Gen5Crypto.Lcg(0x6073u));
        }

        [TestMethod]
        public void ReadsWhite2PartyFromDump()
        {
            var ram = new byte[0x400000]; // 4 MB NDS main RAM
            WriteSlot(ram, White2Base, pid: 0xA9A7A0B5, level: 5, hp: 21, maxHp: 21);

            var mem = new DumpMemoryReader(ram);
            var adapter = new BlackWhite2Adapter();

            Assert.IsTrue(adapter.Matches(mem, new RomInfo("IREO", EmuSystem.NDS)), "should detect B2W2");

            var party = adapter.ReadParty(mem);
            Assert.AreEqual("b2w2", party.GameId);
            Assert.AreEqual("White2", party.Region);
            Assert.AreEqual(1, party.Members.Count);

            var mon = party.Members[0];
            Assert.AreEqual(0xA9A7A0B5u, mon.Pid);
            Assert.AreEqual(5, mon.Level);
            Assert.AreEqual(21, mon.Hp);
            Assert.AreEqual(21, mon.MaxHp);
            Assert.IsFalse(mon.IsFainted);
        }

        [TestMethod]
        public void EmptyRam_NoMatch()
        {
            var mem = new DumpMemoryReader(new byte[0x400000]);
            Assert.IsFalse(new BlackWhite2Adapter().Matches(mem, new RomInfo("IREO", EmuSystem.NDS)));
        }
    }
}
