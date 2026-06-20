# Hosting a server

SoulSync's multiplayer runs through a **relay server**: clients dial **out** to it, so nobody
needs port-forwarding. You can use it two ways.

> 🚧 **Status:** the relay is being ported to C# ([`SoulSync.Net`](../src/SoulSync.Net) /
> [`SoulSync.Server`](../src/SoulSync.Server)). This page describes the model and will gain exact
> commands as the implementation lands.

---

## Option A — Local (built-in)

The simplest setup for a session with friends:

1. One player picks **Host**.
2. SoulSync runs the relay **in-process** and shows a join link / address.
3. Others pick **Join** and paste it.

Best on a **LAN**, or over a virtual LAN (**Tailscale** / **ZeroTier**) for remote play with
zero router config.

---

## Option B — External (self-hosted)

Run a always-on relay that any client can reach over the internet — great for communities and
streamers.

```bash
# Build & run the standalone relay
dotnet run --project src/SoulSync.Server -c Release
```

Then point clients at its URL in **Settings → Server** (e.g. `wss://your-host:PORT`).

### Where to deploy

| Host | Notes |
|------|-------|
| **A small VPS** | Full control; run it as a service. |
| **Render** | Free tier sleeps after inactivity (fine for casual). |
| **Railway / Fly.io** | Cheap always-on. |

> Use a **high port (> 49152)** if you also forward manually — many routers (e.g. Freebox) only
> allow UPnP/forwarding above that range.

---

## How it works

- The server is a **relay with rooms**: it forwards normalized state and notifications between
  members of a room. It does **not** run game logic.
- The **host is authoritative**: the SoulLink engine on the host is the single source of truth;
  the server just fans out its updates.
- Clients send their own events (catches, HP, deaths) → host engine resolves links/cascades →
  broadcast back to everyone.

---

## Security & privacy

- No ROMs or save files ever transit the server — only small, normalized game-state messages.
- Rooms are addressed by a short code; keep it private to keep your run private.

Questions or want a managed public server? Open an [issue](../../issues).
