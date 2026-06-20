/* =============================================================================
   SoulSync — screen router + screens. Screens are built from SS.* components.
   ============================================================================= */

// --- demo state (replaced by live data from the C# host later) ---
const STATE = {
  me: { name: 'mathys', color: '#e6392c', avatar: 'nate' },
  players: [
    { name: 'mathys', color: '#e6392c', avatar: 'nate', ready: true, host: true, rom: 'White 2' },
    { name: 'flo', color: '#36d1dc', avatar: 'rosa', ready: true, rom: 'White 2' },
    { name: 'quentin', color: '#a78bfa', avatar: 'hugh', ready: false, rom: null },
    { name: 'lea', color: '#ff9f45', avatar: 'bianca', ready: true, rom: 'Black 2' },
  ],
  room: { name: "Mathys' Soul Run", code: 'K7F2QX', pub: true },
  rooms: [
    { nm: "Mathys' Soul Run", host: 'mathys', avs: ['nate', 'rosa', 'hugh', 'bianca'], n: 4, max: 4, st: 'full' },
    { nm: 'Friday Nuzlocke', host: 'kevin', avs: ['hilbert', 'hilda'], n: 2, max: 4, st: 'open' },
    { nm: 'Chill duo', host: 'sara', avs: ['may', 'brendan'], n: 2, max: 2, st: 'open', priv: true },
    { nm: 'Hardcore cap run', host: 'leo', avs: ['n'], n: 1, max: 3, st: 'open' },
  ],
  rules: [['Soul Link', 1], ['Species clause', 1], ['1st battle non-lethal', 1], ['Level cap', 0]],
};

// --- screens ---
const Screens = {
  home() {
    return `<div class="screen-center"><div style="text-align:center">
      <div class="topbar" style="position:fixed;top:14px;right:16px">
        <span class="muted">🌐 EN</span>${SS.iconBtn('⚙️', 'settings')}
      </div>
      ${SS.logo(120)}
      <div style="margin:10px 0 0">${SS.wordmark(84)}</div>
      <p class="ss-tag" style="font-size:26px;margin:4px 0 30px">Your souls, linked in real time.</p>
      <div class="stack" style="width:300px;margin:0 auto">
        ${SS.hex({ label: 'HOST', variant: 'braise', size: 'lg', block: true, nav: 'lobby' })}
        ${SS.hex({ label: 'JOIN', variant: 'blue', size: 'lg', block: true, nav: 'rooms' })}
        ${SS.hex({ label: 'SOLO', variant: 'green', size: 'lg', block: true, nav: 'lobby' })}
        <div style="margin-top:6px">${SS.hex({ label: '▶ RESUME LAST RUN', variant: 'gold', block: true, action: 'resume' })}</div>
      </div>
    </div></div>`;
  },

  rooms() {
    const row = r => SS.panel(
      `<div style="display:flex;align-items:center;gap:12px">
        <div style="display:flex">${r.avs.slice(0, 4).map((a, i) => `<span class="pava" style="width:32px;height:32px;margin-left:${i ? '-10px' : '0'};border-color:var(--p${(i % 4) + 1})"><img src="${SS.trainer(a)}" onerror="this.style.display='none'"></span>`).join('')}</div>
        <div style="flex:1;min-width:0"><div class="pname" style="font-size:20px">${r.priv ? '🔒 ' : ''}${r.nm}</div>
          <div class="muted" style="font-size:12px">host: ${r.host} · ${r.n}/${r.max}</div></div>
        ${SS.badge(r.st === 'open' ? '● OPEN' : 'FULL', r.st === 'open' ? 'ok' : 'host')}
        ${SS.hex({ label: r.st === 'full' ? 'WATCH' : 'JOIN', variant: 'blue', nav: 'lobby' })}
      </div>`, 'room');
    return `<div class="screen">
      <div class="topbar" style="margin-bottom:16px">${SS.iconBtn('←', 'home')}<h1 class="pname" style="font-size:28px">Browse rooms</h1><div class="spacer"></div>${SS.hex({ label: '+ CREATE', variant: 'green', nav: 'lobby' })}</div>
      <div class="stack">${STATE.rooms.map(row).join('')}</div>
    </div>`;
  },

  lobby() {
    const pl = p => SS.panel(
      `<div style="display:flex;align-items:center;gap:12px">
        <span class="pava" style="border-color:${p.color}"><img src="${SS.trainer(p.avatar)}" onerror="this.style.display='none'"></span>
        <div style="flex:1"><div class="pname" style="color:${p.color}">${p.name}</div>
          <div class="muted" style="font-size:12px">${p.rom ? 'ROM ready · ' + p.rom : 'choosing ROM…'}</div></div>
        ${p.host ? SS.badge('HOST', 'host') : SS.badge(p.ready ? 'READY' : 'WAIT', p.ready ? 'ok' : 'wait')}
      </div>`);
    return `<div class="screen">
      <div class="topbar" style="margin-bottom:14px">${SS.iconBtn('←', 'home')}
        <h1 class="pname" style="font-size:26px">🔥 ${STATE.room.name}</h1></div>
      <div class="panel" style="margin-bottom:14px"><div class="in" style="display:flex;align-items:center;gap:10px">
        <span class="muted" style="font-size:12px">Share</span>
        <b style="color:var(--or);font-size:20px;flex:1">soulsync.gg/r/${STATE.room.code}</b>
        ${SS.iconBtn('📋', 'copy')}</div></div>
      <div class="stack">${STATE.players.map(pl).join('')}</div>
      <div class="sectit">📜 Rules <span class="muted" style="font-size:12px">(host)</span></div>
      <div style="display:flex;gap:8px;flex-wrap:wrap">${STATE.rules.map(([n, on]) => `<span class="ss-badge ${on ? 'ok' : ''}">${on ? '✓ ' : '○ '}${n}</span>`).join('')}</div>
      <div class="sectit">⚙️ Config <span class="muted" style="font-size:12px">(host)</span></div>
      <div style="display:flex;gap:10px;flex-wrap:wrap">
        ${SS.hex({ label: '🎲 RANDOMIZER', variant: 'blue', action: 'config' })}
        ${SS.hex({ label: '📜 RULES', variant: 'blue', action: 'rules' })}</div>
      <div style="margin-top:20px;text-align:center">${SS.hex({ label: '▶ START (ALL)', variant: 'braise', size: 'lg', action: 'start' })}</div>
    </div>`;
  },
};

// --- router ---
function navigate(name) {
  const s = Screens[name] || Screens.home;
  document.getElementById('app').innerHTML = s();
  window.scrollTo(0, 0);
}
document.addEventListener('click', e => {
  const nav = e.target.closest('[data-nav]');
  if (nav) { navigate(nav.dataset.nav); return; }
  const act = e.target.closest('[data-action]');
  if (act) { console.log('action:', act.dataset.action); /* wired to C# later */ }
});

navigate('home');
