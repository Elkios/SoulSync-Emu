using System.Collections.Generic;
using System.Linq;

namespace SoulSync.Core.Engine
{
    /// <summary>Live party snapshot row fed by the RAM layer (see Sync).</summary>
    public sealed record MonSnap(uint Pid, int Species, string Nickname, int Level, int Hp, int MaxHp, string Zone, bool Shiny = false, bool InParty = true);

    /// <summary>
    /// Game-agnostic Soul Link rules engine (v2). Pokémon are linked BY ZONE.
    /// Pure logic, no I/O — see docs/ENGINE.md. Event methods return the notifications
    /// they produced; <see cref="State"/> returns the current snapshot for the UI.
    /// </summary>
    public sealed class SoulLinkEngine
    {
        private readonly Dictionary<string, string> _players = new();      // id -> name
        private readonly List<TrackedMon> _mons = new();
        private readonly Dictionary<string, ZoneStatus> _zone = new();     // "player|zone" -> status
        private readonly HashSet<string> _owned = new();                   // "player|species"
        private readonly HashSet<string> _linkAnnounced = new();           // zones announced Linked
        private bool _protectActive;
        private bool _gameOver;

        public SoulLinkRules Rules { get; private set; }

        public SoulLinkEngine(SoulLinkRules? rules = null)
        {
            Rules = rules ?? new SoulLinkRules();
            _protectActive = Rules.FirstBattleProtect;
        }

        public void SetRules(SoulLinkRules rules) { Rules = rules; _protectActive = rules.FirstBattleProtect && _protectActive; }
        public void AddPlayer(string id, string name) => _players[id] = name;
        public void LiftProtection() => _protectActive = false;
        public bool GameOver => _gameOver;

        private static string ZK(string p, string z) => p + "|" + z;
        private static string SK(string p, int s) => p + "|" + s;
        private ZoneStatus ZoneOf(string p, string z) => _zone.TryGetValue(ZK(p, z), out var s) ? s : ZoneStatus.Unseen;
        private TrackedMon? MonByPid(uint pid) => _mons.FirstOrDefault(m => m.Pid == pid);
        private string Name(string p) => _players.TryGetValue(p, out var n) ? n : p;

        // ---------- events ----------

        public IReadOnlyList<Note> EnterZone(string player, string zone)
        {
            var n = new List<Note>();
            var st = ZoneOf(player, zone);
            if (st == ZoneStatus.Unseen)
            {
                _zone[ZK(player, zone)] = ZoneStatus.Open;
                n.Add(new Note("zone", player, $"New zone: {zone} — you can catch one here!"));
            }
            else if (st == ZoneStatus.Caught)
            {
                var m = _mons.FirstOrDefault(x => x.Player == player && x.Zone == zone);
                var ls = LinkStatusOf(zone);
                n.Add(ls is LinkStatus.Void or LinkStatus.Dead
                    ? new Note("zone", player, $"{zone}: link broken — {m?.Nickname} is boxed.")
                    : new Note("zone", player, $"{zone}: already caught ({m?.Nickname})."));
            }
            else if (st == ZoneStatus.Failed)
                n.Add(new Note("zone", player, $"{zone}: encounter missed — nothing to catch."));
            else
                n.Add(new Note("zone", player, $"{zone}: catch your first encounter!"));
            return n;
        }

        public IReadOnlyList<Note> Encounter(string player, string zone, int species)
        {
            var n = new List<Note>();
            if (Rules.DupesClause && _owned.Contains(SK(player, species)) && ZoneOf(player, zone) != ZoneStatus.Caught)
                n.Add(new Note("dupe", player, $"{zone}: duplicate species — you may re-roll the encounter."));
            return n;
        }

        public IReadOnlyList<Note> Catch(string player, uint pid, int species, string nickname, int level, int maxHp, string zone, bool shiny = false)
        {
            var n = new List<Note>();
            if (_gameOver || MonByPid(pid) != null) return n;
            if (Rules.SpeciesClause && _owned.Contains(SK(player, species)))
                n.Add(new Note("dupe", player, $"Species clause: {nickname} duplicates an owned species."));
            _mons.Add(new TrackedMon { Player = player, Pid = pid, Species = species, Nickname = nickname, Level = level, Hp = maxHp, MaxHp = maxHp, Zone = zone, Shiny = shiny });
            _owned.Add(SK(player, species));
            _zone[ZK(player, zone)] = ZoneStatus.Caught;
            n.Add(new Note("catch", player, $"🎉 {nickname} was caught on {zone}!" + (shiny ? " ✨" : "")));
            Recompute(zone, n);
            return n;
        }

        public IReadOnlyList<Note> Fail(string player, string zone)
        {
            var n = new List<Note>();
            if (_gameOver) return n;
            _zone[ZK(player, zone)] = ZoneStatus.Failed;
            n.Add(new Note("void", player, $"{Name(player)} missed the encounter on {zone}."));
            Recompute(zone, n);
            return n;
        }

        public IReadOnlyList<Note> Faint(string player, uint pid)
        {
            var n = new List<Note>();
            var m = MonByPid(pid);
            if (m == null || m.State != MonState.Alive) return n;
            if (Rules.FirstBattleProtect && _protectActive)
            {
                n.Add(new Note("protected", player, $"{m.Nickname} fainted — protected (first battle)."));
                return n;
            }
            m.State = MonState.Fainted;
            n.Add(new Note("faint", player, $"💀 {m.Nickname} fainted!"));
            Recompute(m.Zone, n);
            CheckBlackout(n);
            return n;
        }

        public IReadOnlyList<Note> Evolve(string player, uint pid, int species, string newName)
        {
            var n = new List<Note>();
            var m = MonByPid(pid);
            if (m == null) return n;
            m.Species = species; m.Nickname = newName; _owned.Add(SK(player, species));
            n.Add(new Note("evolve", player, $"✨ {newName} evolved!"));
            return n;
        }

        /// <summary>Live snapshot from RAM: updates hp/level, derives catches, faints, blackout.</summary>
        public IReadOnlyList<Note> Sync(string player, IReadOnlyList<MonSnap> party)
        {
            var n = new List<Note>();
            // mark party membership
            var pids = new HashSet<uint>(party.Select(s => s.Pid));
            foreach (var m in _mons.Where(m => m.Player == player)) m.InParty = pids.Contains(m.Pid);
            foreach (var s in party)
            {
                var m = MonByPid(s.Pid);
                if (m == null) { n.AddRange(Catch(player, s.Pid, s.Species, s.Nickname, s.Level, s.MaxHp, s.Zone, s.Shiny)); continue; }
                m.Level = s.Level; m.MaxHp = s.MaxHp; m.InParty = s.InParty;
                var wasAlive = m.State == MonState.Alive;
                m.Hp = s.Hp;
                if (wasAlive && s.Hp <= 0) n.AddRange(Faint(player, s.Pid));
            }
            return n;
        }

        // ---------- link logic ----------

        private LinkStatus LinkStatusOf(string zone)
        {
            var members = _mons.Where(m => m.Zone == zone).ToList();
            if (members.Count == 0 || !Rules.SoulLink) return LinkStatus.Forming;
            if (members.Any(m => m.State == MonState.Fainted)) return LinkStatus.Dead;
            if (_players.Keys.Any(p => ZoneOf(p, zone) == ZoneStatus.Failed)) return LinkStatus.Void;
            if (_players.Keys.All(p => ZoneOf(p, zone) == ZoneStatus.Caught)) return LinkStatus.Linked;
            return LinkStatus.Forming;
        }

        private void Recompute(string zone, List<Note> n)
        {
            if (!Rules.SoulLink) return;
            var members = _mons.Where(m => m.Zone == zone).ToList();
            if (members.Count == 0) return;
            var status = LinkStatusOf(zone);

            if (status is LinkStatus.Dead or LinkStatus.Void)
            {
                foreach (var m in members.Where(m => m.State == MonState.Alive))
                {
                    m.State = MonState.Boxed;
                    n.Add(status == LinkStatus.Dead
                        ? new Note("cascade", m.Player, $"🔗 {m.Nickname} falls with its soul-linked partner — boxed.")
                        : new Note("void", m.Player, $"🔗 {zone} link broken — {m.Nickname} must be boxed."));
                }
            }
            else if (status == LinkStatus.Linked && _linkAnnounced.Add(zone))
            {
                n.Add(new Note("link", "", $"🔗 Link formed on {zone}!"));
            }
        }

        private void CheckBlackout(List<Note> n)
        {
            if (_gameOver) return;
            foreach (var p in _players.Keys)
            {
                if (!_mons.Any(m => m.Player == p)) continue; // hasn't caught anything yet
                var aliveInParty = _mons.Count(m => m.Player == p && m.InParty && m.State == MonState.Alive);
                if (aliveInParty == 0)
                {
                    _gameOver = true;
                    n.Add(new Note("blackout", p, $"{Name(p)} blacked out — the whole team is down."));
                    n.Add(new Note("gameover", "", "GAME OVER — the run ends for everyone."));
                    return;
                }
            }
        }

        // ---------- snapshot for the UI ----------

        public EngineState State
        {
            get
            {
                var players = _players.Select(kv => new PlayerView(kv.Key, kv.Value,
                    _mons.Where(m => m.Player == kv.Key).Select(m => new MonView(
                        m.Pid, m.Species, m.Nickname, m.Level, m.Hp, m.MaxHp, m.Zone, m.Shiny, m.InParty,
                        m.State.ToString(), m.Zone, LinkStatusOf(m.Zone).ToString())).ToList())).ToList();
                var zones = _zone.ToDictionary(k => k.Key, v => v.Value.ToString());
                return new EngineState(players, zones, _gameOver);
            }
        }
    }
}
