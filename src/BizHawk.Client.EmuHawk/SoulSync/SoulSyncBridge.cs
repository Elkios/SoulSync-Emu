#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

using BizHawk.Emulation.Common;

using SoulSync.Core.Engine;
using SoulSync.Games;
using SoulSync.Games.Gen5;

namespace BizHawk.Client.EmuHawk.SoulSync
{
	/// <summary>
	/// The LIVE data pipeline: on a periodic tick it reads the running game's memory through a
	/// <see cref="BizHawkMemoryReader"/> + game adapter, feeds the snapshot to a
	/// <see cref="SoulLinkEngine"/>, then serializes the resulting state (and any notifications)
	/// to JSON and pushes it to the WebView dashboard via <see cref="SoulSyncPanel.PushToUi"/>.
	/// <para>
	/// MVP scope: a single LOCAL player ("me"). Multiplayer (SoulSync.Net) comes later — it will
	/// simply add more players to the same engine/state.
	/// </para>
	/// </summary>
	public sealed class SoulSyncBridge : IDisposable
	{
		private const string LocalPlayerId = "me";
		private const string MainRamDomain = "Main RAM";

		private readonly Func<IEmulator?> _getEmulator;
		private readonly SoulSyncPanel _panel;
		private readonly SoulLinkEngine _engine;
		private readonly BizHawkMemoryReader _reader;
		private readonly Timer _timer;

		private static readonly JsonSerializerOptions JsonOpts = new()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		};

		private BlackWhite2Adapter? _adapter;
		private int _lastMapId = -1;
		private bool _started;

		/// <summary>Display name for the local player, surfaced in the dashboard.</summary>
		public string LocalPlayerName { get; set; } = "Player 1";

		public SoulSyncBridge(Func<IEmulator?> getEmulator, SoulSyncPanel panel)
		{
			_getEmulator = getEmulator ?? throw new ArgumentNullException(nameof(getEmulator));
			_panel = panel ?? throw new ArgumentNullException(nameof(panel));
			_engine = new SoulLinkEngine();
			_reader = new BizHawkMemoryReader(GetMainRamDomains);
			_timer = new Timer { Interval = 250 }; // ~4 Hz; HP only settles at battle end anyway
			_timer.Tick += (_, _) => Tick();
		}

		public SoulLinkEngine Engine => _engine;

		/// <summary>Begins the periodic read/sync/push loop (idempotent).</summary>
		public void Start()
		{
			if (_started) return;
			_started = true;
			_engine.AddPlayer(LocalPlayerId, LocalPlayerName);
			_timer.Start();
		}

		public void Stop()
		{
			_timer.Stop();
			_started = false;
		}

		private IMemoryDomains? GetMainRamDomains()
		{
			var emu = _getEmulator();
			return emu is not null && emu.HasMemoryDomains() ? emu.AsMemoryDomains() : null;
		}

		private void Tick()
		{
			try
			{
				var emu = _getEmulator();
				if (emu is null || !emu.HasMemoryDomains()) return;

				var domains = emu.AsMemoryDomains();
				if (!domains.Has(MainRamDomain)) return;

				// Detect the active game once (cached). Adapters self-report via coherent memory.
				if (_adapter is null)
				{
					var rom = new RomInfo(string.Empty, MapSystem(emu.SystemId));
					_adapter = GameRegistry.Detect(_reader, rom) as BlackWhite2Adapter;
					if (_adapter is null) return;
				}

				var notes = new List<Note>();

				// Zone entry: the current map id drives "new zone" notifications.
				var mapId = _adapter.ReadMapId(_reader);
				if (mapId != _lastMapId)
				{
					_lastMapId = mapId;
					notes.AddRange(_engine.EnterZone(LocalPlayerId, mapId.ToString()));
				}

				// Live party snapshot → engine derives catches, faints, link cascade, blackout.
				var party = _adapter.ReadParty(_reader);
				var snaps = party.Members.Select(m => new MonSnap(
					Pid: m.Pid,
					Species: m.Species,
					Nickname: string.Empty,            // nickname decode is optional (not yet read)
					Level: m.Level,
					Hp: m.Hp,
					MaxHp: m.MaxHp,
					Zone: m.MetLocation.ToString(),    // capture zone = met location = link key
					Shiny: m.Shiny,
					InParty: m.InParty)).ToList();
				notes.AddRange(_engine.Sync(LocalPlayerId, snaps));

				// Push the full state every tick, then any notifications produced this tick.
				_panel.PushToUi(SerializeState());
				foreach (var note in notes) _panel.PushToUi(SerializeNote(note));
			}
			catch
			{
				// Never let a transient bad read (e.g. mid load-state) crash the UI tick.
			}
		}

		private static EmuSystem MapSystem(string systemId) => systemId switch
		{
			"NDS" => EmuSystem.NDS,
			"GBA" => EmuSystem.GBA,
			_ => EmuSystem.GBA, // anything non-NDS: B2W2 adapter will simply not match
		};

		// ---------- JSON contract (camelCase) ----------

		private string SerializeState()
		{
			var state = _engine.State;
			var msg = new StateMessage(
				GameOver: state.GameOver,
				Players: state.Players.Select(p => new PlayerDto(
					p.Id,
					p.Name,
					p.Mons.Select(m => new MonDto(
						m.Pid, m.Species, m.Nickname, m.Level, m.Hp, m.MaxHp,
						m.Zone, m.Shiny, m.InParty, m.State, m.LinkStatus)).ToList())).ToList());
			return JsonSerializer.Serialize(msg, JsonOpts);
		}

		private static string SerializeNote(Note note)
			=> JsonSerializer.Serialize(new NoteMessage(note.Kind, note.Player, note.Text), JsonOpts);

		public void Dispose()
		{
			_timer.Stop();
			_timer.Dispose();
		}

		// DTOs mirror the contract documented for the WebView (System.Text.Json -> camelCase).
		private sealed record class StateMessage(bool GameOver, IReadOnlyList<PlayerDto> Players)
		{
			public string Type => "state";
		}

		private sealed record class PlayerDto(string Id, string Name, IReadOnlyList<MonDto> Mons);

		private sealed record class MonDto(
			uint Pid, int Species, string Nickname, int Level, int Hp, int MaxHp,
			string Zone, bool Shiny, bool InParty, string State, string LinkStatus);

		private sealed record class NoteMessage(string Kind, string Player, string Text)
		{
			public string Type => "note";
		}
	}
}
