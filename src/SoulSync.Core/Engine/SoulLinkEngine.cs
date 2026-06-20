using SoulSync.Core.Models;

namespace SoulSync.Core.Engine
{
    /// <summary>
    /// Game-agnostic Nuzlocke / Soul-Link rules engine. Consumes normalized <see cref="Party"/>
    /// snapshots (one per player) and applies: catch tracking, pairing by catch order,
    /// death cascade across linked players, species clause, game-over, revive, evolution.
    ///
    /// TODO: full port of the validated JS engine (engine.js) — scaffolded here as the
    /// canonical location. Pure logic, no I/O → fully unit-testable.
    /// </summary>
    public sealed class SoulLinkEngine
    {
        // Placeholder surface — implementation to follow.
        public void IngestParty(string playerId, Party party)
        {
            // catch/death/evolution detection + cascade will live here.
        }
    }
}
