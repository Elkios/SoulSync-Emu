namespace SoulSync.Games.Gen5
{
    /// <summary>
    /// Gen 4/5 party-data crypto primitives (ported from the validated Lua tracker / PKHeX).
    /// Party Pokémon are encrypted in RAM with a 32-bit LCG keystream.
    /// </summary>
    public static class Gen5Crypto
    {
        /// <summary>Gen 4/5 LCG: X = (0x41C64E6D * X + 0x6073) mod 2^32.</summary>
        public static uint Lcg(uint seed) => unchecked(seed * 0x41C64E6Du + 0x6073u);

        /// <summary>
        /// Block-shuffle table (PKHeX blockPosition): for each of the 24 PID-derived orders,
        /// the physical index of logical block A..D. 24 rows × 4.
        /// </summary>
        public static readonly int[] BlockPos =
        {
            0,1,2,3, 0,1,3,2, 0,2,1,3, 0,3,1,2, 0,2,3,1, 0,3,2,1,
            1,0,2,3, 1,0,3,2, 2,0,1,3, 3,0,1,2, 2,0,3,1, 3,0,2,1,
            1,2,0,3, 1,3,0,2, 2,1,0,3, 3,1,0,2, 2,3,0,1, 3,2,0,1,
            1,2,3,0, 1,3,2,0, 2,1,3,0, 3,1,2,0, 2,3,1,0, 3,2,1,0,
        };
    }
}
