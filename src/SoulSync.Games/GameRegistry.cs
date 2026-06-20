using System.Collections.Generic;
using System.Linq;
using SoulSync.Core.Memory;
using SoulSync.Games.Gen5;

namespace SoulSync.Games
{
    /// <summary>
    /// Holds every known <see cref="IGameAdapter"/> and picks the one matching the loaded game.
    /// To add a game: implement <see cref="IGameAdapter"/> and register it in <see cref="All"/>.
    /// (Reflection-based auto-discovery can replace this list later.)
    /// </summary>
    public static class GameRegistry
    {
        public static IReadOnlyList<IGameAdapter> All { get; } = new IGameAdapter[]
        {
            new BlackWhite2Adapter(),
            // new HeartGoldSoulSilverAdapter(),
            // new DiamondPearlPlatinumAdapter(),
        };

        /// <summary>Returns the adapter matching the loaded game, or null if none.</summary>
        public static IGameAdapter? Detect(IMemoryReader mem, RomInfo rom)
            => All.FirstOrDefault(a => a.Matches(mem, rom));
    }
}
