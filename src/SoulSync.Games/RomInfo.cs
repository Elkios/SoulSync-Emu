namespace SoulSync.Games
{
    /// <summary>
    /// Minimal info about the loaded ROM, passed to adapters for detection
    /// (e.g. internal game code / system) before/alongside memory probing.
    /// </summary>
    /// <param name="GameCode">NDS/GBA internal game code if available (may be empty).</param>
    /// <param name="System">Console family of the loaded ROM.</param>
    public sealed record RomInfo(string GameCode, EmuSystem System);
}
