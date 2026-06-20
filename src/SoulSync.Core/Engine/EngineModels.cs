using System.Collections.Generic;

namespace SoulSync.Core.Engine
{
    /// <summary>Run rules (toggles). See docs/ENGINE.md.</summary>
    public sealed record SoulLinkRules(
        bool SoulLink = true,
        bool DupesClause = true,
        bool SpeciesClause = true,
        bool FirstBattleProtect = true,
        bool LevelCap = false,
        bool ShinyClause = true);

    public enum ZoneStatus { Unseen, Open, Caught, Failed }

    /// <summary>Forming = partial; Linked = all caught; Void = a partner failed; Dead = a member fainted.</summary>
    public enum LinkStatus { Forming, Linked, Void, Dead }

    public enum MonState { Alive, Fainted, Boxed }

    /// <summary>A caught Pokémon tracked by the engine (mutable internal state).</summary>
    public sealed class TrackedMon
    {
        public string Player = "";
        public uint Pid;
        public int Species;
        public string Nickname = "";
        public int Level;
        public int Hp;
        public int MaxHp;
        public string Zone = "";   // capture zone = link key
        public bool Shiny;
        public bool InParty = true;
        public MonState State = MonState.Alive;
    }

    /// <summary>A notification emitted by the engine.</summary>
    public sealed record Note(string Kind, string Player, string Text);

    // ---- immutable snapshot pushed to the UI ----
    public sealed record MonView(uint Pid, int Species, string Nickname, int Level, int Hp, int MaxHp,
        string Zone, bool Shiny, bool InParty, string State, string LinkZone, string LinkStatus);
    public sealed record PlayerView(string Id, string Name, IReadOnlyList<MonView> Mons);
    public sealed record EngineState(IReadOnlyList<PlayerView> Players,
        IReadOnlyDictionary<string, string> Zones, bool GameOver);
}
