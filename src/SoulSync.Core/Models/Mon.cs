namespace SoulSync.Core.Models
{
    /// <summary>
    /// A single party Pokémon, normalized across all games. Downstream code (rules engine,
    /// UI, networking) only ever sees this — never game-specific encrypted bytes.
    /// </summary>
    /// <param name="Pid">Permanent unique id (used for soul-link pairing; never changes, even on evolution).</param>
    /// <param name="Species">National Dex id (0 if unknown).</param>
    /// <param name="MetLocation">Capture place id (Gen 5 "met location"). This is the Soul Link key. 0 if unknown.</param>
    /// <param name="MetLevel">Level when the Pokémon was caught. 0 if unknown.</param>
    /// <param name="Shiny">True if the Pokémon is shiny (derived from PID/TID/SID).</param>
    /// <remarks>
    /// The first six parameters are positional and unchanged for backward compatibility; the
    /// met/shiny data was added later as optional trailing parameters.
    /// </remarks>
    public sealed record Mon(
        uint Pid,
        int Species,
        int Level,
        int Hp,
        int MaxHp,
        bool InParty,
        int MetLocation = 0,
        int MetLevel = 0,
        bool Shiny = false)
    {
        public bool IsFainted => Hp <= 0;
    }
}
