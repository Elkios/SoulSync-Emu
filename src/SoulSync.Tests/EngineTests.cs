using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoulSync.Core.Engine;

namespace SoulSync.Tests
{
    [TestClass]
    public sealed class EngineTests
    {
        private static SoulLinkEngine TwoPlayers(bool protect = false)
        {
            var e = new SoulLinkEngine(new SoulLinkRules(FirstBattleProtect: protect));
            e.AddPlayer("A", "Alice"); e.AddPlayer("B", "Bob");
            return e;
        }
        private static MonView Mon(SoulLinkEngine e, string player, uint pid)
            => e.State.Players.First(p => p.Id == player).Mons.First(m => m.Pid == pid);

        [TestMethod]
        public void BothCatchSameZone_FormsLink()
        {
            var e = TwoPlayers();
            e.Catch("A", 1u, 396, "Starly", 5, 20, "Route 1");
            e.Catch("B", 2u, 399, "Bidoof", 5, 18, "Route 1");
            Assert.AreEqual("Linked", Mon(e, "A", 1u).LinkStatus);
            Assert.AreEqual("Alive", Mon(e, "A", 1u).State);
            Assert.AreEqual("Alive", Mon(e, "B", 2u).State);
        }

        [TestMethod]
        public void OneFails_VoidsAndBoxesPartner()
        {
            var e = TwoPlayers();
            e.Catch("A", 1u, 396, "Starly", 5, 20, "Route 1");
            var notes = e.Fail("B", "Route 1");
            Assert.AreEqual("Void", Mon(e, "A", 1u).LinkStatus);
            Assert.AreEqual("Boxed", Mon(e, "A", 1u).State);   // catcher must box the orphan
            Assert.IsTrue(notes.Any(x => x.Kind == "void"));
        }

        [TestMethod]
        public void Faint_CascadesToLinkedPartner()
        {
            var e = TwoPlayers();
            // give each player two linked pairs so a single faint doesn't blackout
            e.Catch("A", 1u, 396, "Starly", 5, 20, "Route 1");
            e.Catch("B", 2u, 399, "Bidoof", 5, 18, "Route 1");
            e.Catch("A", 3u, 401, "Kricketot", 5, 16, "Route 2");
            e.Catch("B", 4u, 403, "Shinx", 5, 17, "Route 2");

            var notes = e.Faint("A", 1u);
            Assert.AreEqual("Fainted", Mon(e, "A", 1u).State);
            Assert.AreEqual("Boxed", Mon(e, "B", 2u).State);   // partner cascades
            Assert.AreEqual("Alive", Mon(e, "A", 3u).State);   // other pair untouched
            Assert.IsTrue(notes.Any(x => x.Kind == "cascade"));
            Assert.IsFalse(e.GameOver);
        }

        [TestMethod]
        public void DupesClause_NotifiesOnOwnedSpecies()
        {
            var e = new SoulLinkEngine(new SoulLinkRules(DupesClause: true));
            e.AddPlayer("A", "Alice");
            e.Catch("A", 1u, 25, "Pika", 5, 20, "Route 1");
            var notes = e.Encounter("A", "Route 2", 25);
            Assert.IsTrue(notes.Any(x => x.Kind == "dupe"));
        }

        [TestMethod]
        public void FirstBattleProtection_PreventsDeath_UntilLifted()
        {
            var e = new SoulLinkEngine(new SoulLinkRules(FirstBattleProtect: true));
            e.AddPlayer("A", "Alice");
            e.Catch("A", 1u, 396, "Starly", 5, 20, "Route 1");
            e.Catch("A", 2u, 401, "Kricketot", 5, 16, "Route 2");

            var n1 = e.Faint("A", 1u);
            Assert.AreEqual("Alive", Mon(e, "A", 1u).State);
            Assert.IsTrue(n1.Any(x => x.Kind == "protected"));

            e.LiftProtection();
            e.Faint("A", 1u);
            Assert.AreEqual("Fainted", Mon(e, "A", 1u).State);
            Assert.IsFalse(e.GameOver); // still has Kricketot
        }

        [TestMethod]
        public void TeamWipe_TriggersGameOver()
        {
            var e = new SoulLinkEngine(new SoulLinkRules(FirstBattleProtect: false));
            e.AddPlayer("A", "Alice");
            e.Catch("A", 1u, 396, "Starly", 5, 20, "Route 1");
            var notes = e.Faint("A", 1u);
            Assert.IsTrue(e.GameOver);
            Assert.IsTrue(notes.Any(x => x.Kind == "gameover"));
        }

        [TestMethod]
        public void Sync_DerivesCatchThenFaint()
        {
            var e = new SoulLinkEngine(new SoulLinkRules(FirstBattleProtect: false));
            e.AddPlayer("A", "Alice");
            e.Sync("A", new[] { new MonSnap(10u, 1, "Char", 5, 20, 20, "Route 1") });
            Assert.AreEqual(1, e.State.Players.First(p => p.Id == "A").Mons.Count);
            e.Sync("A", new[] { new MonSnap(10u, 1, "Char", 6, 0, 20, "Route 1") });
            Assert.AreEqual("Fainted", Mon(e, "A", 10u).State);
            Assert.IsTrue(e.GameOver);
        }
    }
}
