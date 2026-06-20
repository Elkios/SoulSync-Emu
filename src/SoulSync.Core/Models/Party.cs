using System.Collections.Generic;

namespace SoulSync.Core.Models
{
    /// <summary>A normalized snapshot of a player's party, produced by a game adapter.</summary>
    /// <param name="GameId">Adapter id, e.g. "b2w2".</param>
    /// <param name="Region">Detected region/version label, e.g. "White2".</param>
    public sealed record Party(
        string GameId,
        string Region,
        IReadOnlyList<Mon> Members);
}
