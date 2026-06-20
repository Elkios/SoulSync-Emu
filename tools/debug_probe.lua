--[[ SoulSync — DEBUG PROBE (B2W2)
  Décode l'équipe + teste TOUTES les adresses dont on a besoin (zone, combat,
  ennemi, TID) + dump un JSON lisible par l'agent. Joue en vitesse max et bouge /
  entre en combat / capture : on regarde les valeurs bouger pour valider chaque adresse.
  Sortie : C:\Projets\soulsync-design\debug.json  (réécrit ~3x/s) + overlay.
============================================================================ ]]

local OUT = [[C:\Projets\soulsync-design\debug.json]]
local MAINRAM = 0x02000000

-- API mémoire (BizHawk Main RAM)
pcall(function() memory.usememorydomain("Main RAM") end)
local function r8(a)  return memory.read_u8(a  - MAINRAM) end
local function r16(a) return memory.read_u16_le(a - MAINRAM) end
local function r32(a) return memory.read_u32_le(a - MAINRAM) end

-- crypto Gen5
local function lcg(s) return (s * 0x41C64E6D + 0x6073) % 0x100000000 end
local function xor16(a,b) return (a ~ b) & 0xFFFF end
local BLOCKPOS = {
  0,1,2,3, 0,1,3,2, 0,2,1,3, 0,3,1,2, 0,2,3,1, 0,3,2,1,
  1,0,2,3, 1,0,3,2, 2,0,1,3, 3,0,1,2, 2,0,3,1, 3,0,2,1,
  1,2,0,3, 1,3,0,2, 2,1,0,3, 3,1,0,2, 2,3,0,1, 3,2,0,1,
  1,2,3,0, 1,3,2,0, 2,1,3,0, 3,1,2,0, 2,3,1,0, 3,2,1,0 }

local SLOT = 0xDC
local CANDS = { {name="Blanche2", base=0x0221E42C}, {name="Noire2", base=0x0221E3EC} }

-- stats de combat (0x88+) chiffrées par le PID
local function stats(slot)
  local pid = r32(slot); if pid == 0 then return nil end
  local seed, s = pid, {}
  for i=0,4 do seed=lcg(seed); s[i]=xor16(r16(slot+0x88+i*2), math.floor(seed/65536)) end
  return pid, s[2]%256, s[3], s[4]  -- pid, level, hp, maxhp
end

-- décode les 4 blocs (0x08..0x87) en ORDRE LOGIQUE -> 64 u16 (clé = checksum)
local function blocks(slot)
  local pid = r32(slot)
  local seed = r16(slot+0x06)
  local phys = {}
  for i=0,63 do seed=lcg(seed); phys[i]=xor16(r16(slot+0x08+i*2), math.floor(seed/65536)) end
  local sv = (math.floor(pid/8192) % 32) % 24
  local logic = {}
  for lb=0,3 do
    local pb = BLOCKPOS[sv*4 + lb + 1]
    for u=0,15 do logic[lb*16+u] = phys[pb*16+u] end
  end
  return logic  -- logic[i] = u16 at logical offset 0x08 + i*2
end
local function lu16(logic, off) return logic[(off-0x08)//2] or 0 end  -- u16 at logical byte offset
local function hexdump(logic) local t={} for i=0,63 do t[#t+1]=string.format("%04X",logic[i]) end return table.concat(t," ") end

-- détecte la base d'équipe active
local function findBase()
  for _,c in ipairs(CANDS) do local pid=r32(c.base); if pid~=0 then local _,lv=stats(c.base); if lv and lv>=1 and lv<=100 then return c end end end
  return nil
end

-- adresses RECHERCHÉES à vérifier (raw)
local PROBES = {
  {n="map_a_B2", a=0x02246848, w=2}, {n="map_b_B2", a=0x02246860, w=2},
  {n="map_a_W2", a=0x022468C8, w=2}, {n="map_b_W2", a=0x022468E0, w=2},
  {n="battle_B2",a=0x021B5138, w=2}, {n="battle_W2",a=0x021B5178, w=2},
  {n="enemyTID", a=0x02257332, w=2},
}

local function jstr(s) return '"'..tostring(s):gsub('\\','\\\\'):gsub('"','\\"')..'"' end
local function writeJson(t)
  local f=io.open(OUT,"w"); if not f then return end
  f:write(t); f:close()
end

local frame=0
while true do
  frame = frame + 1
  if frame % 20 == 0 then
    local base = findBase()
    local parts = {}
    parts[#parts+1] = '"frame":'..frame
    if base then
      parts[#parts+1] = '"game":'..jstr(base.name)
      local s0 = blocks(base.base)
      parts[#parts+1] = '"trainer":{"tid":'..lu16(s0,0x0C)..',"sid":'..lu16(s0,0x0E)..'}'
      local slots = {}
      for i=0,5 do
        local slot = base.base + i*SLOT
        local pid,lv,hp,mhp = stats(slot)
        if pid then
          local logic = blocks(slot)
          slots[#slots+1] = '{"i":'..i..',"species":'..lu16(logic,0x08)..',"level":'..lv..',"hp":'..hp..',"maxhp":'..mhp
            ..',"met80":'..lu16(logic,0x80)..',"met84":'..lu16(logic,0x84)..'}'
        end
      end
      parts[#parts+1] = '"party":['..table.concat(slots,",")..']'
      -- SCAN du buffer ennemi (combat) : on cherche un Pokémon valide hors équipe
      local scan = {}
      local a = 0x02257000
      while a < 0x0225A000 and #scan < 25 do
        local pid = r32(a)
        if pid ~= 0 then
          local _,lv,hp,mhp = stats(a)
          if lv and lv>=2 and lv<=100 and mhp and mhp>=5 and mhp<=999 and hp<=mhp then
            local sp = lu16(blocks(a),0x08)
            if sp>=1 and sp<=900 then scan[#scan+1] = string.format('{"a":"0x%08X","sp":%d,"lv":%d}', a, sp, lv) end
          end
        end
        a = a + 4
      end
      parts[#parts+1] = '"scan":['..table.concat(scan,",")..']'
    else
      parts[#parts+1] = '"game":"(no party)"'
    end
    local pr = {}
    for _,p in ipairs(PROBES) do pr[#pr+1] = '"'..p.n..'":'..r16(p.a) end
    parts[#parts+1] = '"probes":{'..table.concat(pr,",")..'}'
    writeJson('{'..table.concat(parts,",")..'}')

    -- overlay
    if gui and gui.text then
      gui.text(2,2,"SoulSync DEBUG  frame "..frame)
      if base then local pid,lv,hp,mhp=stats(base.base); gui.text(2,16,base.name.." slot0 Niv"..(lv or 0).." "..(hp or 0).."/"..(mhp or 0)) end
      local y=32
      for _,p in ipairs(PROBES) do gui.text(2,y,p.n.." = "..string.format("0x%04X",r16(p.a))); y=y+12 end
    end
  end
  emu.frameadvance()
end
