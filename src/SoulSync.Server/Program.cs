using System;

namespace SoulSync.Server
{
    /// <summary>
    /// Standalone relay server for EXTERNAL hosting (VPS / Render / Railway / etc.).
    /// Locally, the host player can run the relay in-process instead.
    ///
    /// TODO: implement the WebSocket room relay (port of relay.js).
    /// </summary>
    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("SoulSync relay server — not implemented yet.");
            Console.WriteLine("See docs/HOSTING_A_SERVER.md.");
        }
    }
}
