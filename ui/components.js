/* =============================================================================
   SoulSync — reusable UI components (vanilla, return HTML strings).
   Brand-aware: logo, wordmark, hex buttons, panels, player header, mon slot.
   ============================================================================= */
const SS = {
  // --- asset helpers ---
  trainer: n => `https://play.pokemonshowdown.com/sprites/trainers/${n}.png`,
  sprite: (id, sh) => `https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/versions/generation-v/black-white/animated/${sh ? 'shiny/' : ''}${id}.gif`,
  spriteFb: (id, sh) => `https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/${sh ? 'shiny/' : ''}${id}.png`,
  item: n => `https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/items/${n}.png`,
  cover: code => `https://art.gametdb.com/ds/box/US/${code}.png`,

  // --- brand ---
  logo: (size = 110) => `<img class="ss-logo" src="logo.svg" width="${size}" height="${size}" alt="SoulSync">`,
  wordmark: (size = 64) => `<span class="ss-wm" style="font-size:${size}px"><span class="s">Soul</span><span class="y">Sync</span></span>`,

  // --- atoms ---
  hex: ({ label, variant = 'braise', size = '', nav = '', action = '', block = false }) =>
    `<a class="hx ${variant} ${size} ${block ? 'block' : ''}"${nav ? ` data-nav="${nav}"` : ''}${action ? ` data-action="${action}"` : ''}>` +
    `<span class="o"><span class="i">${label}</span></span></a>`,

  panel: (inner, cls = '') => `<div class="panel ${cls}"><div class="in">${inner}</div></div>`,

  iconBtn: (icon, nav = '') => `<a class="iconbtn"${nav ? ` data-nav="${nav}"` : ''}>${icon}</a>`,

  badge: (text, kind = '') => `<span class="ss-badge ${kind}">${text}</span>`,

  // --- molecules ---
  playerHeader: p =>
    `<div class="phead"><span class="pava" style="border-color:${p.color}">` +
    `<img src="${SS.trainer(p.avatar)}" onerror="this.style.display='none'"></span>` +
    `<span class="pname" style="color:${p.color}">${p.name}</span></div>`,

  // a party slot (banner) — m = {id,nick,lvl,hp,max,zone,shiny,status,ball}
  monSlot: (m, linkIdx) => {
    const r = m.max ? m.hp / m.max : 0, dead = m.hp <= 0;
    const cls = r > .5 ? '' : r > .2 ? 'low' : 'crit';
    const st = m.status ? `<span class="st ${m.status}">${m.status.toUpperCase()}</span>` : '';
    const sh = m.shiny ? `<span class="shiny">✨</span>` : '';
    const ball = m.ball ? `<img class="ball" src="${SS.item(m.ball)}" onerror="this.style.display='none'">` : '';
    return `<div class="slot ${dead ? 'fainted' : ''}"><div class="in">` +
      `<div class="pic">${ball}${sh}<img src="${SS.sprite(m.id, m.shiny)}" onerror="this.onerror=null;this.src='${SS.spriteFb(m.id, m.shiny)}'"></div>` +
      `<div style="flex:1;min-width:0">` +
      `<div class="topline"><span class="nick">${m.nick}</span>${st}</div>` +
      `<div class="hp"><i class="${cls}" style="width:${Math.round(r * 100)}%"></i></div>` +
      `<div class="botline"><span>Lv.${m.lvl}${linkIdx ? ` <span class="link">🔗${linkIdx}</span>` : ''}</span><span>${dead ? 'KO' : m.hp + '/' + m.max}</span></div>` +
      (m.zone ? `<div class="zone">📍 <b>${m.zone}</b></div>` : '') +
      `</div></div></div>`;
  },
};

// --- form components ---
SS.optRow = (label, control, sub) =>
  `<div class="opt"><span class="l">${label}${sub ? `<small>${sub}</small>` : ''}</span>${control}</div>`;
SS.toggle = (on = false) =>
  `<label class="ss-toggle"><input type="checkbox"${on ? ' checked' : ''}><span class="tr"></span></label>`;
SS.select = (opts) => `<select class="ss-select">${opts.map(o => `<option>${o}</option>`).join('')}</select>`;
SS.range = (min, max, val) => `<input class="ss-range" type="range" min="${min}" max="${max}" value="${val}">`;
SS.progress = (pct = 0) => `<div class="ss-progress"><i style="width:${pct}%"></i></div>`;

// tiny i18n (falls back to the key). Loaded locale set via SS.setLocale.
SS.t = (k, vars) => {
  let s = (SS._loc && SS._loc[k]) || k;
  if (vars) for (const v in vars) s = s.replace(`{${v}}`, vars[v]);
  return s;
};
SS.setLocale = obj => { SS._loc = obj; };
