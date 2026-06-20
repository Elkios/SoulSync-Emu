using System.Collections.Generic;
using SoulSync.Core.Memory;
using SoulSync.Core.Models;

namespace SoulSync.Games
{
    /// <summary>
    /// The multi-game contract. One implementation per supported game. An adapter knows how to
    /// detect its game/version and how to read &amp; decrypt the party into the normalized model.
    /// It depends only on <see cref="IMemoryReader"/> — never on BizHawk — so it can be unit-tested
    /// against a RAM dump without launching the emulator.
    /// </summary>
    public interface IGameAdapter
    {
        /// <summary>Stable id, e.g. "b2w2", "hgss", "dppt".</summary>
        string Id { get; }

        EmuSystem System { get; }

        /// <summary>Regions/versions this adapter can handle (informational).</summary>
        IReadOnlyList<string> Regions { get; }

        /// <summary>True if this adapter matches the currently loaded game (ROM info + live memory).</summary>
        bool Matches(IMemoryReader mem, RomInfo rom);

        /// <summary>Read and decrypt the current party into the normalized model.</summary>
        Party ReadParty(IMemoryReader mem);
    }
}
