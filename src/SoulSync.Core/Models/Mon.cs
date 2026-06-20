namespace SoulSync.Core.Models
{
    /// <summary>
    /// A single party Pokémon, normalized across all games. Downstream code (rules engine,
    /// UI, networking) only ever sees this — never game-specific encrypted bytes.
    /// </summary>
    /// <param name="Pid">Permanent unique id (used for soul-link pairing; never changes, even on evolution).</param>
    /// <param name="Species">National Dex id (0 if unknown).</param>
    public sealed record Mon(
        uint Pid,
        int Species,
        int Level,
        int Hp,
        int MaxHp,
        bool InParty)
    {
        public bool IsFainted => Hp <= 0;
    }
}
