namespace SoulSync.Net
{
    /// <summary>
    /// Client that connects to a relay server (local or external) and exchanges normalized
    /// state/notifications between room members. Host-authoritative: the engine on the host
    /// is the single source of truth.
    ///
    /// TODO: port the WebSocket relay client/server (relay.js, server.js) here.
    /// </summary>
    public interface IRelayClient
    {
        /// <summary>Server URL — e.g. ws://localhost:58787 (local) or wss://my-host (external).</summary>
        string ServerUrl { get; }
    }
}
