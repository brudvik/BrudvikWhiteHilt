# ⚓ Plan: damage at sea and new ship weapons

Implementation plan for ideas 1 and 2 under *More for the sea battles* in [Ideas for new content](../ideas.md). It
builds on the sea battles of 0.128.0 ([sea-battles.md](../sea-battles.md), plan and log in
[sea-battles.md](sea-battles.md)). Written for an AI agent that carries it out phase by phase without the conversation
behind it. Read all of it once before you start, and read section 11 of [sea-battles.md](sea-battles.md) for what the
existing code really does.

## 1. What the player gets

**Damage at sea** (every ship, during a sea battle):

- **Holes.** A heavy hit on a ship's hull during a sea battle (a Nail Ship within range) knocks a hole in it, at the
  waterline on the side that was hit. Rams always make holes. A hole shows as splintered planks with water spraying in.
- **Flooding.** Holes let water in. The more water, the lower the ship lies and the slower it sails; you see the water
  inside the hull. A full ship takes damage until it sinks. White Hilt ships cannot sink (they are indestructible), but
  they flood and slow down like any other.
- **Patching and bailing.** Use a hole to patch it (wood and resin from your inventory; it also mends some hull health).
  Shift+Use on board a flooded ship bails water (costs stamina). Water also drains slowly once every hole is patched.
- **Deck fires.** Pitch pots set the deck alight where they land. Fire hurts those who stand in it and burns the hull,
  dies out after a while, and can be stamped out with Use (costs stamina). Rain shortens it.
- **Nail Ships fight back.** Their crew throws pitch pots at your deck, and from Moder on (setting) a Nail Ship carries
  its own ballista that shoots at your hull.

**New ship weapons:**

- **Pitch Pot.** A throwable clay pot of pine tar (clone of the vanilla Ooze Bomb). Sets decks and ground alight.
- **Chain Shot.** Ballista ammunition: two iron balls on a chain. A hit on a ship tears its sail: it sails at half force
  for a while. Little hull damage.
- **Grapnel Bolt.** Ballista ammunition with a rope. A hit on a ship binds the two ships together and pulls them close,
  so you can board. A grappled Nail Ship cannot flee. Use the rope's cleat on your rail to cut it.
- **Ammunition choice.** While manning the ballista, the secondary attack (right mouse / gamepad) switches between the
  ammunition you carry. The crosshair shows which and how many.
- **Ram Prow.** A new ship upgrade (White Hilt Ship and Skidbladnir): an iron-shod prow. Striking a ship or a creature
  bow-first hurts it in proportion to your speed and always holes a ship.

Everything is admin-configurable and on by default. No new 3D models: looks are copied from vanilla prefabs, made in
code, or recoloured.

## 2. Decisions already made

| Decision | Choice | Why |
|---|---|---|
| Biome | Swamp tier or earlier, like the rest of the sea battles. Pitch Pot: Pine Tar, Resin, Leather Scraps. Chain Shot: Chain, Iron. Grapnel Bolt: Iron, Bog Iron, Deer Hide. Ram Prow: Iron, Bog Iron, Fine Wood, Chain | Biome rule; ocean counts as Swamp |
| When holes happen | Only "in battle": the hit ship is a Nail Ship, or a loaded Nail Ship is within `BattleRange` of it; and always for rams (`HitType.Boat`) | Ordinary serpent fights and vanilla sailing do not change |
| Hit test | Prefix on `WearNTear.RPC_Damage` (runs on the owner), reading the hit **before** resistances | Indestructible White Hilt ships are immune to the damage but still get holes and fire |
| Indestructible ships | Flood, slow down and burn players, but never sink and lose no health | They are sold as indestructible |
| Flood | One float 0–1 in the ship's ZDO, written by the owner, simulated on world time | Owner rule, survives ownership changes |
| Sinking | At full flood a destructible ship takes `FloodDamagePerSecond` until its `WearNTear` is destroyed (vanilla drops the cargo crate) | Reuses vanilla destruction |
| Deck fires | A list of ship-local points with expiry in the ship's ZDO; every client draws them; no networked fire objects | Fires move with the ship; no parenting of `ZNetView` objects |
| New ballista ammo | Same ammo type as the Ship Bolt (`$ammo_whitehilt_shipbolt`), told apart by prefab; each has its own projectile clone with a hit handler | `GetAmmoItem` finds them all; one code path |
| Grapple physics | Each ship's owner pulls only its own body toward the other | Owner rule; no cross-owner physics |
| Ram Prow | Upgrade bit 10, `WhiteHiltShipUpgradeBase.Count` 10 → 11 | Same pattern as the ballista |
| Release | Changelog under `## Unreleased`. New RPCs → the release is a MINOR bump (0.129.0) | Copilot instructions |

## 3. Rules for the implementer

Section 3 of [sea-battles.md](sea-battles.md) applies in full: settings for every gameplay number with Norwegian labels
(`cfg_keys.py --check`), class-level `[HarmonyPatch]` on every patch class, `ZNetScene.instance.Destroy` instead of
`Object.Destroy` for anything with a `ZNetView`, owner-only simulation and ZDO writes, headless guards with try/catch
for every visual, no file edits from the terminal, verify vanilla names (`.venv\Scripts\python.exe
AssetSource\Tools\vanilla_prefab.py <name> --values [--all-bundles]`), read the decompile at `%TEMP%\wh_tame\full\*.cs`
before guessing, build and test after each phase, no commit/push/deploy, XML docs, deviations in the log (section 11).
Also:

- Inside `BrudvikWhiteHilt.Patches.*` namespaces, write `global::BrudvikWhiteHilt.SeaBattles.X` (otherwise
  `SeaBattles` resolves to `Patches.SeaBattles`).
- Everything that runs per frame or per physics step starts with a cheap static check (`RaiderShip.Loaded`,
  `ShipDamage.Any`, `DeckFires.Any`, `GrappleLink.Any`) and returns.
- Pure logic goes in static classes that use only `Vector2`/`Vector3`/`Mathf` and plain values, with tests in
  `BrudvikWhiteHilt.Tests` (the game's `Ship`/`Player` types cannot load in the test project; see
  `RaiderDeckTests.cs` for stand-ins).
- Existing pieces to reuse, not copy: `ShipBallista` (firing, aim, gunner ZDO), `BallistaModel.Build`, `RaiderShip`
  (`Is`, `Of`, `Loaded`, `Spawn`, the AI tick), `RaiderDeck.TryDeckPoint`, `RaiderTactics`, `ShipMooring` (rope
  `LineRenderer`), `HornCaller`, `WhiteHiltShipUpgradeSetup.RootBounds`/`HullTemplate`, `KrakenPatches`
  (`WearNTear.Damage` prefix pattern), `DragonGroundFire` (ground fire), `Treasure`/`Items.ShipUpgrades` item patterns.

## 4. Settings

All admin only. Section labels `whitehilt_cfgsec_<section>`; every key `whitehilt_cfg_<section>_<key>` + `_desc`.

Section `SeaBattles.Damage`:

| Key | Default | Range | Meaning |
|---|---|---|---|
| `Enabled` | true | | Holes, flooding and deck fires happen in sea battles |
| `BattleRange` | 200 | 50–500 | A ship within this many metres of a loaded Nail Ship is in battle |
| `HoleMinDamage` | 60 | 1–1000 | Least blunt+pierce+slash+chop of one hit (before resistances) that makes a hole |
| `MaxHoles` | 6 | 1–20 | Holes a ship can have at once |
| `FloodPerHole` | 0.01 | 0–0.2 | Share of a full ship that one hole lets in per second |
| `DrainPerSecond` | 0.005 | 0–0.1 | Share that drains per second once no hole is open |
| `BailAmount` | 0.04 | 0–0.5 | Share one bail removes |
| `BailStamina` | 8 | 0–100 | Stamina per bail |
| `BailSeconds` | 0.6 | 0.1–5 | Seconds between two bails |
| `SinkDepth` | 0.8 | 0–3 | Metres a full ship lies lower |
| `FloodSpeedLoss` | 0.6 | 0–1 | Share of sail force lost when full |
| `FloodDamagePerSecond` | 25 | 0–1000 | Hull damage per second while full (destructible ships only) |
| `PatchWood` | 2 | 0–20 | Wood per patch |
| `PatchResin` | 1 | 0–20 | Resin per patch |
| `PatchRepair` | 0.1 | 0–1 | Share of the hull's health one patch mends |
| `DeckFireSeconds` | 30 | 1–300 | Seconds a deck fire burns (half in rain) |
| `DeckFireRadius` | 1.5 | 0.5–5 | Metres around a fire that burn |
| `DeckFireDamage` | 8 | 0–200 | Fire damage per second to a player in it |
| `DeckFireHullDamage` | 4 | 0–200 | Fire damage per second to the hull, per fire |
| `MaxDeckFires` | 6 | 1–20 | Fires a ship can have at once |
| `StampStamina` | 10 | 0–100 | Stamina to stamp out a fire |

Section `SeaBattles.Weapons` (Nail Ship weapons and the Pitch Pot):

| Key | Default | Range | Meaning |
|---|---|---|---|
| `PitchPots` | true | | Nail Ship crews throw pitch pots |
| `PitchPotSeconds` | 12 | 2–120 | Seconds between two pots from one Nail Ship |
| `PitchPotRange` | 35 | 5–80 | Metres within which they throw |
| `PitchPotDamage` | 20 | 0–500 | Fire damage of a pot's hit |
| `PitchPotsPerCraft` | 2 | 1–20 | Pots made at once |
| `NailBallistaKey` | `defeated_dragon` | | Global key from which Nail Ships carry a ballista; empty = always |
| `NailBallistaSeconds` | 10 | 2–120 | Seconds between two shots |
| `NailBallistaRange` | 60 | 10–150 | Metres within which it shoots |
| `NailBallistaAccuracy` | 3 | 0–20 | Spread in degrees |

Section `Ships.Ballista` (new keys next to the existing ones):

| Key | Default | Range | Meaning |
|---|---|---|---|
| `ChainShotDamage` | 20 | 0–1000 | Pierce damage of a chain shot |
| `SailTearShare` | 0.5 | 0–1 | Share of sail force a torn sail loses |
| `SailTearSeconds` | 45 | 1–600 | Seconds a sail stays torn |
| `ChainShotsPerCraft` | 5 | 1–50 | Chain shots made at once |
| `GrapnelDamage` | 10 | 0–1000 | Pierce damage of a grapnel bolt |
| `GrappleLength` | 6 | 2–30 | Metres the rope pulls the ships to |
| `GrappleBreak` | 60 | 10–200 | Metres at which the rope snaps |
| `GrappleSeconds` | 90 | 5–600 | Seconds the rope holds before it frays |
| `GrapplePull` | 1.5 | 0–10 | Pull strength (acceleration per metre of slack, m/s² per m) |
| `GrapnelsPerCraft` | 3 | 1–50 | Grapnel bolts made at once |

Section `Ships.Ram`:

| Key | Default | Range | Meaning |
|---|---|---|---|
| `Enabled` | true | | The Ram Prow can be made and works |
| `MinSpeed` | 3 | 0–20 | Least closing speed in m/s for a ram |
| `DamagePerSpeed` | 40 | 0–1000 | Blunt + chop damage per m/s of closing speed |
| `SelfDamageShare` | 0.1 | 0–1 | Share of that damage the ramming ship takes (destructible ships only) |
| `Cooldown` | 1.5 | 0.1–10 | Seconds before the same target can be rammed again |

## 5. Names, keys and texts

Prefabs: `WhiteHiltPitchPot`, `WhiteHiltChainShot`, `WhiteHiltGrapnelBolt`, `WhiteHiltShipRam` (upgrade item);
projectile clones `WhiteHilt_PitchPot_projectile`, `WhiteHilt_ChainShot_projectile`, `WhiteHilt_Grapnel_projectile`.

ZDO keys on the ship (hash once):

| Key | Type | Meaning |
|---|---|---|
| `whitehilt_ship_holes` | byte[] | `ZPackage`: count, then per hole a ship-local `Vector3` |
| `whitehilt_ship_flood` | float | 0–1 |
| `whitehilt_ship_floodtick` | long | World ticks of the last flood step |
| `whitehilt_ship_fires` | byte[] | `ZPackage`: count, then per fire a ship-local `Vector3` and an expiry in world ticks |
| `whitehilt_ship_torn` | long | World ticks until which the sail is torn |
| `whitehilt_ship_grapple` | byte[] | `ZPackage`: other ship `ZDOID`, local point here, local point there, start ticks; empty = none |

Object RPCs on a ship's `ZNetView` (registered for every ship in the `Ship.Awake` postfix that attaches
`ShipAssist`, or in a new component added there): `WhiteHiltShipHole(Vector3 local)`,
`WhiteHiltShipPatch(int index)`, `WhiteHiltShipBail()`, `WhiteHiltShipFire(Vector3 local)`,
`WhiteHiltShipStamp(int index)`, `WhiteHiltShipTear()`, `WhiteHiltShipGrapple(ZDOID other, Vector3 here, Vector3
there)`, `WhiteHiltShipCut()`. All go to the owner, who checks them (index in range, player near, counts capped).

English texts, with Norwegian:

| Key | English | Norwegian |
|---|---|---|
| `whitehilt_ship_hole` | Hole in the hull | Hull i skroget |
| `whitehilt_ship_patch` | Patch ({0} wood, {1} resin) | Tett ({0} ved, {1} kvae) |
| `whitehilt_ship_bail` | Bail water | Øs vann |
| `whitehilt_ship_flood_hud` | Water in the hull: {0}% | Vann i skroget: {0} % |
| `msg_whitehilt_ship_holed` | The hull is holed! | Skroget er slått hull på! |
| `msg_whitehilt_ship_sinking` | She is going down! | Hun går ned! |
| `msg_whitehilt_ship_nopatch` | You need wood and resin to patch it | Du trenger ved og kvae for å tette det |
| `whitehilt_ship_fire` | Fire on deck | Brann på dekk |
| `whitehilt_ship_stamp` | Stamp out | Trå ut |
| `msg_whitehilt_ship_torn` | The sail is torn! | Seilet er revet! |
| `whitehilt_ship_grapple` | Grapnel rope | Entrehaketau |
| `whitehilt_ship_cut` | Cut the rope | Kutt tauet |
| `msg_whitehilt_ship_grappled` | The ships are bound together! | Skipene er bundet sammen! |
| `msg_whitehilt_ship_rope_snaps` | The rope snaps. | Tauet ryker. |
| `whitehilt_ballista_ammo` | Ammunition: {0} ({1}) | Ammunisjon: {0} ({1}) |
| `whitehilt_ballista_switch` | Switch ammunition | Bytt ammunisjon |

Items (`Translations.AddEnglishNameAndDescription`; register each once):

| Prefab | English name | English description | Norwegian name |
|---|---|---|---|
| `WhiteHiltPitchPot` | Pitch Pot | A clay pot of hot pine tar with a burning rag. Throw it at a deck to set it alight. | Bekkrukke |
| `WhiteHiltChainShot` | Chain Shot | Two iron balls on a chain, for the Ship Ballista. Tears a ship's sail. | Kjedekule |
| `WhiteHiltGrapnelBolt` | Grapnel Bolt | A hooked bolt on a rope, for the Ship Ballista. Binds a ship to yours and pulls it close. | Entrehakebolt |
| `WhiteHiltShipRam` | Ram Prow | An iron-shod prow for the White Hilt Ship or Skidbladnir. Use it on the mast. Ramming bow-first hurts in proportion to your speed. | Rambukkstavn |

Write Norwegian descriptions in the plain style of `Norwegian.json`.

## 6. Files

New:

| File | Purpose |
|---|---|
| `SeaBattles/Damage/DamageSettings.cs` | Section `SeaBattles.Damage` |
| `SeaBattles/Damage/HoleRules.cs` | Pure: whether a hit makes a hole, where on the hull it goes, nearest hole to a point |
| `SeaBattles/Damage/FloodModel.cs` | Pure: one flood step from holes, drain, elapsed seconds; buoyancy offset and speed factor |
| `SeaBattles/Damage/ShipDamage.cs` | Component on every ship: holes, flood, patch, bail, sinking, visuals, hover |
| `SeaBattles/Damage/DeckFires.cs` | Component on every ship: fires list, visuals, damage, stamping |
| `SeaBattles/Damage/ShipShot.cs` | Static: fire a ballista-like projectile from a point (refactored out of `ShipBallista.Shoot`) |
| `SeaBattles/Damage/Ballistics.cs` | Pure: launch velocity for a lobbed projectile with gravity, with lead |
| `SeaBattles/Weapons/WeaponSettings.cs` | Section `SeaBattles.Weapons` |
| `SeaBattles/Weapons/NailShipWeapons.cs` | On the Nail Ship (owner): pitch pots and its ballista |
| `SeaBattles/Weapons/ShotHits.cs` | Hit handlers for the new projectiles (pitch pot, chain shot, grapnel) |
| `SeaBattles/Weapons/GrappleLink.cs` | Component on every ship: reads the grapple, pulls its own body, draws the rope, cut hover |
| `SeaBattles/Weapons/GrappleRules.cs` | Pure: pull acceleration from distance, length, strength; snap test |
| `Items/ShipUpgrades/PitchPot.cs`, `ChainShot.cs`, `GrapnelBolt.cs` | The items |
| `Pieces/Ships/Ram/RamSettings.cs`, `ShipRam.cs`, `RamRules.cs` | Section `Ships.Ram`, the collision handler, pure damage maths |
| `Patches/SeaBattles/ShipDamagePatches.cs` | `WearNTear.RPC_Damage` prefix, `Ship.GetSailForce` postfix, `Ship.CustomFixedUpdate` postfix, crosshair |
| `BrudvikWhiteHilt.Tests/SeaDamageTests.cs` | Tests for the pure parts |

Changed: `BrudvikWhiteHilt.cs` (Initialize calls), `Pieces/Ships/Ballista/ShipBallista.cs` (ammo choice, uses
`ShipShot`), `BallistaSettings.cs`, `Items/ShipUpgrades/ShipUpgrades.cs` + `WhiteHiltShipUpgradeBase.cs` (Ram Prow,
`Count` 11), `WhiteHiltShipUpgradeSetup.cs`/`WhiteHiltShipUpgrades.cs`/`Skidbladnir*.cs` (prow part; list it in
`SkidbladnirModel.UpgradeObjects`), `SeaBattles/RaiderShip.cs` (weapons, grappled → no flee), the patch that attaches
`ShipAssist` on `Ship.Awake` (also attach `ShipDamage`, `DeckFires`, `GrappleLink`), `Translations/Norwegian.json`,
docs.

## 7. Phases

Each phase ends with the checks in section 9. Do not start the next phase while a check fails.

### Phase 1: settings, texts and pure logic

1. `DamageSettings`, `WeaponSettings`, `RamSettings` and the new `BallistaSettings` keys (section 4), `Initialize()`
   from `Awake` next to `SeaBattleSettings.Initialize()` / `BallistaSettings.Initialize()`. Feature gates:
   Pitch Pot → `WeaponSettings` has no switch; tie it to `SeaBattleSettings.Enabled`; Chain Shot and Grapnel →
   `BallistaSettings.Enabled`; Ram Prow → `RamSettings.Enabled` (`WhiteHiltConfig.AddFeatureGate`).
2. All texts of section 5 (English in code, Norwegian in `Norwegian.json`), config labels for every key.
3. Pure logic with tests:
   - `HoleRules.MakesHole(float rawPhysical, float minDamage, bool inBattle, bool isRam)`;
     `HoleRules.Place(Vector3 localHit, Vector3 hullCentre, Vector3 hullSize, float waterlineY)` → a point on the
     nearer side of the hull, inside it by 0.15 m, at `waterlineY + 0.25`, z clamped to the hull with a 0.5 m margin;
     `HoleRules.Nearest(IReadOnlyList<Vector3> points, Vector3 local, float maxDistance)` → index or -1.
   - `FloodModel.Step(float flood, int holes, float seconds, float perHole, float drain)` (clamped 0–1, drains only
     with 0 holes); `FloodModel.Offset(float flood, float sinkDepth)`; `FloodModel.SailFactor(float flood, float loss)`.
   - `Ballistics.LaunchVelocity(Vector3 from, Vector3 to, float speed, float gravity, bool high, out Vector3 velocity)`
     → false when out of reach; `Ballistics.Lead(Vector3 target, Vector3 targetVelocity, float flightSeconds)`.
   - `GrappleRules.Pull(float distance, float length, float strength)` (0 when slack) and
     `GrappleRules.Snaps(float distance, float breakDistance, float ageSeconds, float maxSeconds)`.
   - `RamRules.ClosingSpeed(Vector3 relativeVelocity, Vector3 forward)`,
     `RamRules.IsBowContact(Vector3 localContact, float hullLength)` (front quarter),
     `RamRules.Damage(float closingSpeed, float minSpeed, float perSpeed)` (0 below `minSpeed`).

### Phase 2: holes, flooding, patching, bailing

1. Attach `ShipDamage` to every ship in the existing `Ship.Awake` postfix (find where `ShipAssist.Attach` is called).
   `ShipDamage` registers the hole/patch/bail RPCs, keeps a static registry (`Any`), and caches the base
   `Ship.m_waterLevelOffset`.
2. Prefix on `WearNTear.RPC_Damage` (owner side, before resistances; see the vanilla code in section 10):
   if `DamageSettings.Enabled`, the `WearNTear` belongs to a ship (`GetComponent<Ship>()`), and
   `HoleRules.MakesHole(hit.m_damage.m_blunt + m_pierce + m_slash + m_chop, ..., inBattle, hit.m_hitType ==
   HitData.HitType.Boat)` → `ShipDamage.AddHole(hit.m_point)` (owner: place with `HoleRules.Place`, cap at `MaxHoles`,
   write the package, message `msg_whitehilt_ship_holed` to players aboard via a routed or object RPC). `inBattle` =
   the ship is a Nail Ship, or any loaded `RaiderShip` lies within `BattleRange`. Never change the vanilla damage.
   The hull geometry comes from `ship.m_floatCollider` (centre, size); the waterline is the float collider's centre y
   in ship space unless the decompile shows a better value; verify on both the longship and Skidbladnir with a log line.
3. Owner, every 0.5 s: `FloodModel.Step` with world time from `whitehilt_ship_floodtick` (cap the step at 60 s so a
   ship loaded after a long time does not flood at once), write `whitehilt_ship_flood`. Destructible ships (their
   `WearNTear` is not immune to blunt; check its `m_damages`) at flood 1 take `FloodDamagePerSecond` through
   `WearNTear.ApplyDamage` (owner) and show `msg_whitehilt_ship_sinking` once. White Hilt ships never take it.
4. Physics on the owner: postfix on `Ship.CustomFixedUpdate` sets `m_waterLevelOffset = base - FloodModel.Offset(...)`
   when the flood is above 0 and restores the base at 0 (early return when `ShipDamage.Any` is false). Speed: postfix on
   `Ship.GetSailForce` multiplies `__result` by `FloodModel.SailFactor` (and by the torn-sail factor in phase 4). Check
   the existing prefix on `GetSailForce` (Nail Ship tailwind) still works with a postfix added.
5. Every client: hole visuals as children of the ship (no `ZNetView`): a dark splintered decal (procedural texture on
   a quad, material copied from `WhiteHiltShipUpgradeSetup.HullTemplate` and tinted, or `Sprites/Default`) and a water
   spray copied from a vanilla splash particle on the ship prefab *(verify: the `watereffects` child of `VikingShip`)*;
   a small box collider per hole with a `Hoverable`/`Interactable` child component (as `ShipLanternHover` does). The
   flood shows as a flat tinted plane inside the hull rising with the flood (`FloodModel.Offset` scale), hidden at 0.
   Rebuild visuals only when the ZDO's data revision changes. Skip everything when headless.
6. Hover on a hole: `whitehilt_ship_hole`, `[E] whitehilt_ship_patch`, `[Shift+E] whitehilt_ship_bail`. Use: take
   `PatchWood` Wood and `PatchResin` Resin from the inventory (or show `msg_whitehilt_ship_nopatch`), send
   `WhiteHiltShipPatch(index)`; owner removes the hole and, for destructible ships, mends `PatchRepair` of the health
   (set `ZDOVars.s_health`, capped at `m_health`, as `WearNTear.RPC_Repair` does).
7. Bailing anywhere aboard: when the local player stands on a flooded ship and hovers nothing else, the crosshair shows
   `[Shift+E] whitehilt_ship_bail` and the flood in percent (`whitehilt_ship_flood_hud`). Alt-Use takes `BailStamina`
   and sends `WhiteHiltShipBail()` at most every `BailSeconds`. Find where `BoardingLeap.UpdateCrosshair` is called
   and add the prompt there without stealing the plain Use from the leap. The owner subtracts `BailAmount` and checks the
   sender is aboard (player ZDO within 15 m).
8. HUD: when on a flooded ship, `ShipHud` (or the crosshair line) shows `whitehilt_ship_flood_hud`.

### Phase 3: fire, pitch pots and the Nail Ship's weapons

1. `PitchPot` item: `CustomItem("WhiteHiltPitchPot", "BombOoze")` *(verified: `BombOoze` throws
   `oozebomb_projectile` at 20 m/s, blunt 5, attack animation `throw_bomb`)*. Clone the projectile into
   `WhiteHilt_PitchPot_projectile` (`PrefabManager.Instance.CreateClonedPrefab`), remove its ooze `m_spawnOnHit` (the
   poison cloud) and set fire damage `PitchPotDamage` on the item. Recipe at the Workbench: Pine Tar 1, Resin 2,
   Leather Scraps 1 → `PitchPotsPerCraft`. Tint dark, own icon (not headless).
2. `ShotHits`: a component on each new projectile clone that subscribes `Projectile.m_onHit` (signature
   `(Collider collider, Vector3 hitPoint, bool water)`) in `Awake`. It runs on the machine that simulates the
   projectile. For the pitch pot: if the collider's `attachedRigidbody` has a `Ship`, send `WhiteHiltShipFire(local
   point)` to it; else, on dry ground, start a ground fire with `DragonGroundFire` if its API allows a source-less fire
   (read the class; otherwise skip ground fires and log it as a deviation). Water hits do nothing.
3. `DeckFires` on every ship: owner keeps the list (cap `MaxDeckFires`, drop expired, rain halves the remaining time:
   `EnvMan.instance.IsWet()` *(verify)*), every 1 s applies `DeckFireHullDamage × fires` fire damage to the hull through
   `WearNTear.Damage` with `HitType.Burning` (immune ships ignore it as usual). Every client draws a fire at each point:
   copy the flame particles and light of a vanilla fire *(verify: `fire_pit` → `_enabled_high` has
   `Particles/{flare, flames (1), sparcs (1)}`, `Point light`, `sfx_fire_loop`; do not copy `FireWarmth`/`FireBurn`)*.
   The local player standing within `DeckFireRadius` of a fire on the ship they are on takes `DeckFireDamage` fire per
   second (`player.Damage`, own body). Hover a fire: `whitehilt_ship_fire`, `[E] whitehilt_ship_stamp` → stamina, RPC
   `WhiteHiltShipStamp(index)`.
4. `ShipShot.Fire(Vector3 from, Vector3 direction, ItemDrop.ItemData ammo, Character owner, float hitNoise)`: move the
   body of `ShipBallista.Shoot` here (with `m_hitType = HitData.HitType.Turret`), and make `ShipBallista` call it. No
   behaviour change for the player's ballista.
5. `NailShipWeapons` on the Nail Ship prefab (owner only, ticked from `RaiderShip`'s AI tick or its own 0.25 s timer):
   - Pitch pots: in Broadside or Board, target within `PitchPotRange`, every `PitchPotSeconds`: pick a living crew
     member, throw from its chest height toward a deck point of the target (`RaiderDeck.TryDeckPoint`) led by the
     target's velocity, using `Ballistics.LaunchVelocity` (high arc if the low one is blocked or out of reach) and the
     projectile's gravity *(read `Projectile.m_gravity` of the clone)*; `owner` = that crew member, so the pot does
     not hurt its own side.
   - Ballista: when the world has `NailBallistaKey` (or it is empty), the Nail Ship gets a ballista look on its bow
     (`BallistaModel.Build`, placed like `ShipBallista.Place` does on the longship) and shoots a Ship Bolt
     (`ObjectDB` item `WhiteHiltShipBolt`, its `ItemData` cloned) at the target's hull at the waterline every
     `NailBallistaSeconds` within `NailBallistaRange`, spread `NailBallistaAccuracy`, through `ShipShot.Fire` with
     `owner` = null. Turn the model toward the target on every client (the target id is already in the ZDO).
6. Settings and docs rows for everything new.

### Phase 4: chain shot, grapnel and the ammunition choice

1. Items `WhiteHiltChainShot` (Chain 1, Iron 1 → `ChainShotsPerCraft`) and `WhiteHiltGrapnelBolt` (Iron 2, Bog Iron 1,
   Deer Hide 1 → `GrapnelsPerCraft`) at the Shipwright's Bench, cloned like `ShipBolts` (same ammo type,
   `$ammo_whitehilt_shipbolt`), each with its own projectile clone of the bolt's projectile carrying `ShotHits`, and
   damage from settings. Tint the chain shot dark iron and the grapnel with a rope colour; own icons.
2. Ammunition choice in `ShipBallista`: the local gunner's selected prefab (static, default Ship Bolt). Secondary
   attack (read how `BallistaPatches` takes the attack in `Player.SetControls`; take `secondaryAttack` the same way)
   cycles through the ship-ammo prefabs the player carries. Fire uses the selected one if carried, else the first that
   is. The crosshair or hover line while manning: `whitehilt_ballista_ammo` with name and count, and the switch key.
3. Chain shot hit on a ship → `WhiteHiltShipTear()`; the owner sets `whitehilt_ship_torn` = now + `SailTearSeconds`
   and messages those aboard. `Ship.GetSailForce` postfix multiplies by `1 - SailTearShare` while torn. A torn Nail Ship
   in Approach or Flee is slower, which is the point.
4. Grapnel hit on another ship within `GrappleBreak` → `WhiteHiltShipGrapple(other, here, there)` sent to the
   **shooter's** ship (the ship the gunner mans), whose owner writes `whitehilt_ship_grapple`. `GrappleLink` on every
   ship, every physics step on each ship's owner: if this ship holds a grapple, or another loaded ship's grapple names
   this ship (keep a static list of loaded links), pull its own body toward the other end with
   `GrappleRules.Pull` (an acceleration along the rope, `ForceMode.Acceleration`, shared half each way), and damp the
   relative speed a little. The holder's owner clears the grapple when `GrappleRules.Snaps` (message
   `msg_whitehilt_ship_rope_snaps`) or either ship is gone. Rope: `LineRenderer` between the two points on every client
   (copy the look of `ShipMooring`'s rope). A small hover box at the rope's end on the holder's rail:
   `whitehilt_ship_grapple`, `[E] whitehilt_ship_cut` → `WhiteHiltShipCut()`.
5. `RaiderShip`: while it is the other end of a grapple, never enter Flee and prefer Board (it pulls alongside);
   message `msg_whitehilt_ship_grappled` to those aboard both ships when the link is made.

### Phase 5: the Ram Prow

1. `ShipRamUpgrade` in `ShipUpgrades.cs`: bit 10, `WhiteHiltShipUpgradeBase.Count` 10 → 11 (check every loop over
   bits). Requirements: Iron 6, Bog Iron 4, Fine Wood 8, Chain 2. `CopyFrom` `"Iron"`. Feature gate
   `RamSettings.Enabled`.
2. Look: an iron-shod stem on the bow of the White Hilt Ship and Skidbladnir, switched with the bit like the other
   parts. Build it from a vanilla iron mesh *(verify a suitable one, e.g. the iron band parts of `woodiron_beam` or
   `iron_wall_1x1`)*, placed at the bow along the stem from the front holdfast (`interactive/front`, as the ballista
   finds it) forward to the bow. List it in `SkidbladnirModel.UpgradeObjects`.
3. `ShipRam` component on the White Hilt ships (added where the other upgrade parts are set up), active only with the
   bit. `OnCollisionEnter` on the ship's owner: contact in the bow quarter (`RamRules.IsBowContact`), closing speed
   along the bow (`RamRules.ClosingSpeed` of `collision.relativeVelocity`) of at least `MinSpeed`, target not hit within
   `Cooldown`. A ship (`attachedRigidbody` has `Ship`, not this one): `WearNTear.Damage` with half blunt, half chop,
   `m_hitType = HitData.HitType.Boat`, `m_point` the contact, `m_dir` the bow (the hole rule in phase 2 then always makes
   a hole). A creature (`Character`, e.g. a serpent): `Character.Damage` with blunt and a push. The ramming ship takes
   `SelfDamageShare` of it if destructible (White Hilt ships are not). A camera shake and the vanilla ship impact
   effect *(verify `Ship.m_waterImpactEffect` or the `WearNTear.m_hitEffect`)* at the contact.
4. Ship upgrade docs row.

### Phase 6: documentation

1. `docs/sea-battles.md`: new sections *Damage at sea* (holes, flooding, patching, bailing, sinking, White Hilt ships),
   *Fire* (pitch pots, deck fires, stamping), *The Nail Ship fights back* (pots, its ballista from Moder), and settings
   tables for `[SeaBattles.Damage]` and `[SeaBattles.Weapons]`. Every number checked against the code.
2. `docs/ships.md`: Ram Prow in the upgrade table, Chain Shot and Grapnel Bolt next to the Ship Bolt, the ammunition
   choice, the new `[Ships.Ballista]` keys and a `[Ships.Ram]` table. Skidbladnir's upgrade count (eleven).
3. A Pitch Pot row where throwables or ship gear are listed (`docs/equipment.md` or `docs/sea-battles.md`; follow
   where the Ooze-bomb-like items of the mod are documented, if any).
4. `docs/ideas.md`: remove the two *(planned)* bullets under *More for the sea battles* (they are built) and leave no
   link to `plans/`.
5. `README.MD`: update the Sea battles card line if it no longer covers the feature. Do not touch "New in …" (the
   release step does that).
6. `CHANGELOG.md` under `## Unreleased` → `### Added` (holes, flooding, patching and bailing; deck fires and pitch pots;
   Nail Ship pots and ballista; chain shot; grapnel; ammunition choice; Ram Prow), linking the docs pages.
7. `docs/architecture.md` only if a described pattern changed.

## 8. Multiplayer and performance notes

- The hit test runs in `RPC_Damage` on the ship's owner; holes, flood, fires, torn sail and grapple live in the ship's
  ZDO and are written only there. Players ask through object RPCs; the owner checks.
- Projectiles are vanilla network objects; their hit handlers run where the projectile is simulated and only send RPCs.
- Grapple pulls are applied by each ship's own owner to its own body.
- Every per-step or per-frame hook starts with a static count check. Visuals rebuild on ZDO data revision change, not
  every frame. Flood steps every 0.5 s, fire damage every 1 s.

## 9. Checks after every phase

```powershell
$msbuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
& $msbuild BrudvikWhiteHilt\BrudvikWhiteHilt.csproj "/t:BeforeBuild;CoreBuild" "/p:SolutionDir=$PWD\" /p:GenerateDocumentationFile=true /v:minimal
dotnet test BrudvikWhiteHilt.Tests
.venv\Scripts\python.exe AssetSource\Tools\cfg_keys.py --check
```

No new warnings, all tests pass (including `PatchTests` and `EveryEnglishTextHasANorwegianOne`), 0 missing config
labels. Run terminal commands one at a time.

## 10. Reference

`WearNTear.RPC_Damage` (Valheim 1.0, decompiled), where the prefix goes: the hit arrives raw, then
`hit.ApplyResistance(m_damages, out _)`, then `if (totalDamage <= 0f) return;` (immune ships stop here), tool tier,
then `ApplyDamage(totalDamage, hit)`. Health is `ZDOVars.s_health` on the ZDO; `RPC_Repair` sets it to `m_health`.
`WearNTear.Damage(hit)` only invokes `RPC_Damage` on the owner.

`HitData.HitType` has `Turret`, `Boat`, `Burning`, `Impact`, `Catapult` among others.

`Projectile.m_onHit` is an `OnProjectileHit` delegate invoked as `m_onHit?.Invoke(collider, hitPoint, water)` at the
end of `Projectile.OnHit`.

`BombOoze`: item type 3, attack `throw_bomb`, `m_consumeItem`, projectile `oozebomb_projectile`, velocity 20
(min 2), accuracy 5, blunt 5.

Vanilla `Ship.CustomFixedUpdate` floats the hull with `worldCenterOfMass.y - waterLevel - m_waterLevelOffset`, so a
smaller `m_waterLevelOffset` makes the ship lie lower.

## 11. Log

Write here per phase: what was done, verified vanilla facts, check results, deviations and why, and the in-game
checklist for the user.

- **Phase 1 (settings, texts and pure logic), 2026-10-13.**
  - Done: `SeaBattles/Damage/DamageSettings.cs` (`SeaBattles.Damage`, 21 keys), `SeaBattles/Weapons/WeaponSettings.cs`
    (`SeaBattles.Weapons`, 9 keys), `Pieces/Ships/Ram/RamSettings.cs` (`Ships.Ram`, 5 keys), the ten new keys in
    `BallistaSettings` (`ChainShotDamage`, `SailTearShare`, `SailTearSeconds`, `ChainShotsPerCraft`, `GrapnelDamage`,
    `GrappleLength`, `GrappleBreak`, `GrappleSeconds`, `GrapplePull`, `GrapnelsPerCraft`), the `Initialize()` calls in
    `BrudvikWhiteHilt.Awake` (`DamageSettings` and `WeaponSettings` right after `SeaBattleSettings.Initialize()`, because
    `WeaponSettings` reads `SeaBattleSettings.Enabled`; `RamSettings` right after `BallistaSettings.Initialize()`), the
    pure classes `HoleRules`, `FloodModel`, `Ballistics` (`SeaBattles/Damage`), `GrappleRules` (`SeaBattles/Weapons`) and
    `RamRules` (`Pieces/Ships/Ram`), and `BrudvikWhiteHilt.Tests/SeaDamageTests.cs` (`HoleRulesTests`, `FloodModelTests`,
    `BallisticsTests`, `GrappleRulesTests`, `RamRulesTests`). Namespaces follow the folders (`...SeaBattles.Damage`,
    `...SeaBattles.Weapons`, `...Pieces.Ships.Ram`).
  - Feature gates (`WhiteHiltConfig.AddFeatureGate`, by prefab id, before the items exist): `WhiteHiltPitchPot` ties to
    `SeaBattleSettings.Enabled` (in `WeaponSettings.Initialize`), `WhiteHiltChainShot` and `WhiteHiltGrapnelBolt` to
    `BallistaSettings.Enabled` (in `BallistaSettings.Initialize`), `WhiteHiltShipRam` to `RamSettings.Enabled` (in
    `RamSettings.Initialize`). The ids are public consts: `WeaponSettings.PitchPotName`,
    `BallistaSettings.ChainShotName`, `BallistaSettings.GrapnelBoltName`, `RamSettings.UpgradeName`. The item classes
    of the later phases should use them as their `PrefabName`/`ItemName` instead of repeating the strings.
  - Texts: **every English text of section 5, including the four item names and descriptions, is registered in code in
    two places only**: `DamageSettings.AddTranslations` (hole, patch, bail, flood HUD, holed, sinking, nopatch, fire,
    stamp) and `WeaponSettings.AddTranslations` (torn, grapple, cut, grappled, rope snaps, ballista ammo and switch, and
    `item_whitehiltpitchpot`, `item_whitehiltchainshot`, `item_whitehiltgrapnelbolt`, `item_whitehiltshipram` with
    their descriptions). The item classes of phases 3 to 5 must not call `AddEnglishNameAndDescription` (unlike
    `ShipBolts` and `ShipBallistaUpgrade`, which register their own). Norwegian for all of them is in `Norwegian.json`
    after `item_whitehiltraidchart_description`, together with the config labels
    (`whitehilt_cfg_seabattles_damage_*`, `whitehilt_cfg_seabattles_weapons_*`, `whitehilt_cfg_ships_ram_*`, the new
    `whitehilt_cfg_ships_ballista_*`) and the section labels `whitehilt_cfgsec_seabattles_damage`,
    `whitehilt_cfgsec_seabattles_weapons` and `whitehilt_cfgsec_ships_ram`. The `SeaBattles.Damage` keys have their
    full section in the label key, because `Enabled` also exists in `SeaBattles`.
  - Pure-logic contracts that phase 2 to 5 rely on: `HoleRules.Place` puts the hole on the side given by the sign of
    `localHit.x - hullCentre.x` (amidships goes to +x), at `x = centre.x +- (width/2 - 0.15)`, `y = waterlineY + 0.25`,
    `z` clamped to the hull with a 0.5 m margin (a hull shorter than 1 m gives the centre z). `FloodModel.Offset`
    returns a positive depth: `m_waterLevelOffset = base - Offset(...)`. `Ballistics.LaunchVelocity` takes **gravity as a
    positive number** (`Projectile.m_gravity`), returns false for a target straight above or below the launch point
    (horizontal reach under 1 cm) and for speed 0; gravity 0 or less gives a straight shot. `RamRules.ClosingSpeed` wants
    the ramming ship's velocity minus the target's (check the sign of Unity's `Collision.relativeVelocity` in phase 5),
    and `IsBowContact` wants the contact measured from the middle of the hull (`z >= length / 4`).
  - Check results: build with `GenerateDocumentationFile`: exit 0, only the old warnings (CS0419/CS1591 in
    `NearbyContainers`, `RecipeCategories`, `DefenseModelBuilder`); `dotnet test BrudvikWhiteHilt.Tests`: 460 passed, 0
    failed; `cfg_keys.py --check`: 0 missing. Not tested in the game (nothing in this phase changes behaviour: only
    config entries, texts and gates for items that do not exist yet).
  - Deviations: (1) Two helpers beyond the plan: `Ballistics.FlightSeconds(from, to, velocity)` (horizontal distance over
    horizontal speed, so the caller can iterate `Lead` as in the tests) and public constants in `HoleRules`
    (`SideInset`, `AboveWaterline`, `EndMargin`) for the plan's fixed 0.15, 0.25 and 0.5 m; these three are geometry
    constants, not gameplay numbers. (2) `Ballistics.LaunchVelocity` accepts a discriminant down to `-1e-5 * speed^4`
    as zero so a target exactly at the limit of reach is not lost to rounding. (3) The item texts are registered in
    `WeaponSettings`, also the Ram Prow's (the plan lists all four items in one table), and not in the item classes.
  - For phase 2: the new settings are `DamageSettings.*`; `FloodModel`, `HoleRules` and `Ballistics` are ready. Nothing
    reads the settings yet. The Pitch Pot gate follows `SeaBattleSettings.Enabled`, not `DamageSettings.Enabled`.

- **Phase 2 (holes, flooding, patching, bailing), 2026-10-13.**
  - Done, new files in `SeaBattles/Damage/`: `ShipDamage` (component on every ship: ZDO holes and flood, owner flood
    step, patch/bail/message RPCs, sinking, buoyancy and sail factor), `ShipDamageLook` (client-only drawing: hole decal,
    water jet, flood sheet), `ShipDamageInput` (the local player's Use on a hole and Alt+Use to bail, prompts under the
    crosshair), `HoleList` (the holes as bytes) and `HullShape` (hull width and flare from mesh points).
    `Patches/SeaBattles/ShipDamagePatches.cs` (class-level `[HarmonyPatch]`): prefix `WearNTear.RPC_Damage`, postfix
    `Ship.CustomFixedUpdate`, postfix `Ship.GetSailForce`. Changed: `ShipAssistPatches` (attach `ShipDamage` in the
    `Ship.Awake` postfix, `ShipDamageInput.Tick` before `BoardingLeap.Tick`, the prompts after the leap prompt),
    `BoardingLeap.UsePressed` (ignores a Use that patching or bailing took, and Alt+Use), `ShipAssist.CanPush` (no push
    while a hole is aimed at), `ShipHud` (a flood line), `RaiderShip.AnyNear`, `SeaBattleCommands` (`whitehilt_seabattle
    hole [n]`, cheats only), pure additions to `HoleRules` (`Aimed`, `Mend`, `ValidIndex`, `Spacing`) and `FloodModel`
    (`ElapsedSeconds`, `HullDamage`, `Percent`), and 60 new tests in `SeaDamageTests.cs` (`HoleAimingTests`,
    `HoleMendingTests`, `FloodTimeTests`, `HoleListTests`, `HullShapeTests`). No new texts or settings.
  - How it works: the owner of the hull gets `WearNTear.RPC_Damage` (vanilla only runs it there); the prefix reads the raw
    hit (blunt+pierce+slash+chop, `HitType.Boat` = ram), skips fire, water, ash-ocean and other non-blow types, and
    `HoleRules.MakesHole` decides, with `inBattle` = the ship is a Nail Ship or a Nail Ship that is not a prize is within
    `BattleRange`. `AddHole` places the hole (`HoleRules.Place` with the hull's width at the waterline), refuses a hole
    within 1 m of another or past `MaxHoles`, writes `whitehilt_ship_holes` and sends `msg_whitehilt_ship_holed` to those
    aboard. The owner steps the flood every 0.5 s from `whitehilt_ship_floodtick` (world ticks, step capped at 60 s, tick
    cleared while the ship is dry) and a full ship that is not immune takes `FloodDamagePerSecond` through
    `WearNTear.ApplyDamage` (null hit is fine, the game's own default). Immune = `WearNTear.m_damages.m_blunt` is
    `Immune`; `WearNTearHelper.MakeIndestructible` sets that on every White Hilt ship, including Skidbladnir, through
    `IndestructiblePiece`; the Nail Ship's hull is not immune. Every client reads the ZDO every 0.25 s but only works when
    `DataRevision` changed, and draws the holes again only when the hole bytes differ (the flood and the rudder change the
    revision all the time). `m_waterLevelOffset = base - FloodModel.Offset(flood, SinkDepth)` is set in the
    `CustomFixedUpdate` postfix, the base read at `Awake`, restored when the ship is dry again. The sail postfix multiplies
    `__result` by `FloodModel.SailFactor`.
  - Verified in the decompile and the game's data: `WearNTear.RPC_Damage` order as in section 10, `ApplyDamage(float,
    HitData = null)` and `Destroy(HitData = null)` take null, `RPC_HealthChanged(float)` is registered on the same
    `ZNetView` and sent `Everybody` by `RPC_Repair`, `HitType.Boat` is used by nothing in vanilla except the damage text, so
    only a ram makes it. `Ship.CustomFixedUpdate`: `num2 = COM.y - waterAvg - m_waterLevelOffset`, the lift grows with
    `|num2|`, so a smaller offset puts the ship lower (as section 10 says). A Harmony prefix that returns false does not
    stop postfixes, so the flood factor also applies to the Nail Ship's tailwind prefix. `ZNetView.Register` adds to a
    per-view dictionary (a duplicate name would throw); the new names `WhiteHiltShipPatch`, `WhiteHiltShipBail` and
    `WhiteHiltShipMessage` are unique, so any number of components can register on a ship's view. `Player.FindHoverObject`
    takes only the first hit of the sorted ray; `Ship.OnTriggerEnter` counts any character that enters ANY trigger below
    the ship's rigidbody as aboard. Hover/Use as the game does it: `Interact(go, hold, alt)` with alt =
    `AltPlace` held (or `JoyAltKeys` with the non-classic layout).
  - Hull geometry used (logged once per ship type in the game as `Ship damage: <prefab>: float box ...`). **Longship and
    White Hilt Ship** (same clone): `FloatCollider` at the ship root, centre (0,0,0), size (8,1,17), no scale; waterline y
    0.00 (the float box centre; the prefab's `watereffects` surface is at y -0.1); deck 0.64. The float box is far wider
    than the hull, so the hole uses the readable hull mesh `ship/visual/hull_new/hull` (7523 vertices; measured offline
    with `%TEMP%\wh_hull\hull_width.py` on the bundle): root-space bounds x +-2.758, y -0.715 to 4.072, z -10.295 to
    11.237; half width (points within +-0.35 m of the height) 1.99 at y 0, 2.25 at 0.25, 2.49 at 0.5, 2.76 at 1.0, so the
    hull flares about 0.96 m per metre up at the waterline. A hole therefore sits at x about +-2.1 (half width minus 0.15,
    measured with the narrow +-0.1 m band first), y 0.25, z within +-8 (float box length 17 minus the 0.5 m margins);
    the decal leans back with the flare. **Skidbladnir**: float box centre (0,0,2), size (6.2,0.5,18), `m_waterLevelOffset`
    0 (set in `Skidbladnir.CustomizePrefab`); it has no mesh at the readable path, so the hull's half width is the float box's
    3.1 and the flare is 0 (the decal stands straight); holes at x +-2.95, y 0.25, z within 2 +- 8.5. Waterline = float box
    centre y is still the plan's assumption: the ship's real draught was not measured (the longship's keel is at y -0.715,
    so it lies roughly 0.7 m deep if the water plane is at y 0).
  - The vanilla spray: the longship has `watereffects/SpeedWake/GameObject` (and `(1)`), bow spray jets with a
    `ParticleSystem`; the hole spray takes the renderer's material from the ship's own copy (found by `Find`, no prefab
    lookup) and drives its own `ParticleSystem` from code, so the emission does not depend on the ship's speed; without that
    child (Skidbladnir might have moved it) it uses a soft white puff made in code. The first use logs which one.
  - Check results: build with `GenerateDocumentationFile`: exit 0, only the old warnings (CS0419 in `NearbyContainers` and
    `DefenseModelBuilder`, CS1591 in `RecipeCategories`); `dotnet test BrudvikWhiteHilt.Tests`: 520 passed, 0 failed (460
    before, 60 new); `cfg_keys.py --check`: 0 missing. Not tested in the game.
  - Deviations: (1) **No `WhiteHiltShipHole` RPC.** The owner makes holes only from its own `RPC_Damage` hook. An RPC any
    client could call would let anybody put holes in anybody's ship, and nothing sends it (phase 5's ram sends a vanilla
    `HitType.Boat` hit). The cheat command `whitehilt_seabattle hole [n]` (needs `devcommands`) sends such a hit to the hull
    of the ship the player stands on, so it tests the real hook. `WhiteHiltShipFire` and the others of phases 3 and 4 are
    unchanged. A message RPC `WhiteHiltShipMessage(string)` was added; it only shows texts that start with
    `$msg_whitehilt_ship_`. (2) **No hole colliders or hover components.** The plan's box collider with a `Hoverable`
    does not work here: the holes are at y 0.25 under the deck planks (deck 0.64), so a ray from the deck hits the deck
    first; a solid box would block players, and a trigger under the ship's rigidbody would be counted as "aboard" by
    `Ship.OnTriggerEnter`. Instead `ShipDamageInput` tests the camera ray against the hole points
    (`HoleRules.Aimed`, 0.9 m radius, reach measured from the eyes with `Player.m_maxInteractDistance`) and reads the Use
    key itself, as `BoardingLeap` does; the prompt is written under the crosshair after the leap prompt and only when
    nothing else is hovered (or the ship itself). So a hole can be patched from the deck by aiming at the rail above it,
    where the water jet shows, and from a boat alongside. (3) Holes are stored as plain bytes (`int` count, then three
    `float`s each, the `ZPackage` layout) and not through a `ZPackage`, so `HoleList` is tested without the game. (4) The
    hole position follows the hull mesh, not the float box, for the longship family (the box is 8 m wide, the hull 5.5 m);
    `HullShape` and `HoleRules.Spacing` (1 m: two hits that near are one hole) are additions. (5) Nail Ships that are a
    prize (crew dead) do not count as a battle. (6) Plain numbers that are not gameplay and so not settings: the 15 m the
    owner accepts a patch or bail from, half the bail delay as the owner's rate limit, the 0.9 m aim radius, the 0.5 s
    and 0.25 s step and refresh, the drawing sizes. (7) `DamageSettings.Enabled` off clears the holes and lets the water
    drain; it does not remove the ZDO keys. (8) The flood sheet is a thin quad over the deck that grows and rises with the
    flood (up to 0.45 m over the deck at full), not a plane inside the hull: the deck planks hide anything below it.
  - Not changed, for phase 3 and later: the `Ship.GetSailForce` postfix is where phase 4 multiplies in the torn-sail factor
    (`ShipDamage.SailFactor()` is the place). `ShipDamage.Of(ship)`, `ShipDamage.Of(wearNTear)` and `ShipDamage.All` are the
    lookups; a ship is "in battle" through `RaiderShip.AnyNear` (not a prize). The object RPC names a ship's `ZNetView` has
    from this phase: `WhiteHiltAutopilot`, `WhiteHiltAutopilotShallow`, `WhiteHiltShipPush`, `WhiteHiltShipHalt`,
    `WhiteHiltMoor`, `WhiteHiltShipPatch`, `WhiteHiltShipBail`, `WhiteHiltShipMessage` (plus the Nail Ship's
    `WhiteHiltRaiderMessage` and vanilla's). `ShipDamageInput.HandledFrame` and `AltHeld()` are how a later Use handler
    avoids clashing with the leap, the patch and the bail. `DeckFires` (phase 3) can borrow `ShipDamageLook`'s quad,
    texture and `Deck()` ideas; the deck height is measured there with rays from above.
  - In-game checklist (nothing here was run in the game): (1) Load a longship, stand on it, run `devcommands` and
    `whitehilt_seabattle hole 3`: three holes, "The hull is holed!", splintered decals low on the sides and water jets
    rising inside the hull at the rail; check the decal lies flat on the planks and is not floating or buried (the log
    line `Ship damage: VikingShip: ...` shows the numbers used). (2) The ship should sink slowly lower, sail slower and the
    HUD at the helm should show "Water in the hull: n%". A white water sheet grows over the deck. (3) Aim at the rail
    above a jet: the crosshair shows "Hole in the hull / [E] Patch (2 wood, 1 resin) / [Shift+E] Bail water"; Use patches
    (wood and resin leave the pack; with none: "You need wood and resin..."). The decal and jet go. Alt+Use bails
    (stamina, no faster than every 0.6 s); the water falls 4% per bail and drains by itself once no hole is open. (4)
    Repeat on the White Hilt Ship and Skidbladnir (floods and slows, never sinks or loses health) and on a Karve/Raft
    (health falls at `FloodDamagePerSecond` at 100% and the ship breaks up with "She is going down!"; a patch heals 10%
    of its health). (5) With a second client: they see the holes and the water, can patch and bail, and the message
    shows only to those aboard; hand the ship over to the other player and check the flood goes on (and that a long
    time away does not fill it at once). (6) Fight a Nail Ship (`whitehilt_seabattle summon`): a ballista bolt (pierce 60
    or more) makes holes in your ship, a serpent bite does not; with no Nail Ship near, only the command and rams do. (7)
    Check the leap (Use at another ship) still works and does not fire on Alt+Use or while a hole is aimed at; the boat
    push on shore does not fire while aiming at a hole. (8) Turn `[SeaBattles.Damage] Enabled` off: holes vanish and the
    water drains.

- **Phase 3 (fire, pitch pots and the Nail Ship's weapons), 2026-10-13.**
  - Done, new files: `Items/ShipUpgrades/PitchPot.cs` (clone of `BombOoze`, own projectile clone of `oozebomb_projectile`
    without the ooze `m_spawnOnHit`, fire damage from `PitchPotDamage`, Workbench recipe, dark tint and rendered icon, not
    on a dedicated server), `SeaBattles/Damage/FireList.cs` (`DeckFire`, bytes and list operations), `FireRules.cs`,
    `DeckFires.cs` (component on every ship), `DeckFireLook.cs` (client-only fire drawing), `DeckFireInput.cs` (the
    local player's Use on a fire), `SeaBattles/Weapons/ShotHits.cs` (`Projectile.m_onHit` subscriber and the `ShotKind`
    switch), `ShipShot.cs` (the bolt launch), `WeaponRules.cs` and `NailShipWeapons.cs` (the Nail Ship's pots and
    ballista). Changed: `ShipBallista` (`Shoot` moved to `ShipShot.Fire`; a decor mode for the Nail Ship),
    `BallistaModel.Build` (now returns the `ShipBallista`), `NailShipRegistry` (`AddWeapons` on the Nail Ship, at both
    ship-building places), `ShipAssistPatches` (attach `DeckFires`, call `DeckFireInput.Tick` after the hole input and
    its prompt after the hole prompt), `RaiderShip.TargetVelocity`, `ShipAssist.CanPush` (not while a fire is aimed at),
    `Ballistics.Intercept`, the text `msg_whitehilt_ship_fire` (English in `DamageSettings.AddTranslations`, Norwegian in
    `Norwegian.json`), and 56 new tests in `SeaDamageTests.cs` (`FireListTests`, `FireRulesTests`, `WeaponRulesTests`,
    `BallisticsInterceptTests`).
  - How it works: a pot's projectile reports its hit to `ShotHits`. On a ship, the pot's landing point (local space) goes
    to the ship's owner in `WhiteHiltShipFire`; the owner checks the point with `FireRules.ValidSpot`, snaps it to the
    deck point nearest within 2.5 m, otherwise no fire), and lights it with `FireList.Add` (cap `MaxDeckFires`, spacing
    1 m, a nearer fire is fed instead). The fire list is plain bytes in the ZDO key `whitehilt_ship_fires` with expiries in
    world ticks; the owner prunes burnt-out fires, rain takes time off (twice as fast), and every second it damages the
    hull with `WearNTear.Damage` (`HitType.Burning`, `DeckFireHullDamage` per fire); a White Hilt ship's hull is immune
    to it as it is to everything. Every client re-reads the ZDO when `DataRevision` changes and draws a copy of the vanilla
    `fire_pit` `_enabled_high` child (flames, light, crackle; not `FireWarmth` or `FireBurn`, so the fire does not warm or
    burn through game code). The local player standing within `DeckFireRadius` takes `DeckFireDamage` per second as a
    fire hit on themselves. Aim at a fire and Use ("Fire on deck / [E] Stamp out") to send `WhiteHiltShipStamp` (the point)
    to the owner, which checks stamina on the client, the distance and the fire, then removes it.
  - Nail Ship: `NailShipWeapons` runs only on the owner of the ship. In `Broadside` and `Board` (`WeaponRules.CanThrow`)
    every `PitchPotSeconds` it picks a living crew member, throws a pot from them at the target ship's deck with
    `Ballistics.Intercept` (the target's velocity from `RaiderShip.TargetVelocity`, both arcs tried), inside
    `PitchPotRange`; the projectile's owner is the crew member, so kills and aggro count as theirs. When the world has
    `NailBallistaKey` (`WeaponRules.KeyReached`) it builds the ballista with `BallistaModel.Build` at the bow, and in
    Approach, Broadside and Board (`WeaponRules.CanShoot`) shoots a Ship Bolt at the target's hull every
    `NailBallistaSeconds` within `NailBallistaRange`, through `ShipShot.Fire` with owner null. The model turns toward the
    target on every client (the target position is derived from the ZDO target id each client already reads).
  - Verified in the decompile and the game's data: `Projectile.m_onHit` is `Action<Collider, Vector3, bool>` and runs from
    `OnHit` before the AOE and before `m_spawnOnHit`; the ooze bomb's `m_spawnOnHit` is the ooze spawn, so the pitch pot's
    clone needs it cleared; `BombOoze` has the throw attack, `m_gravity` and `m_vel` the Intercept math assumes;
    `fire_pit` has `_enabled_high` with the fire particle systems, a light and an `AudioSource`; `DragonGroundFire` has
    no way to set an owner or a "no owner" source (see deviations); `WearNTear.Damage(HitData)` is the owner-side entry
    that ends in `RPC_Damage` semantics for burning hits but the hole hook ignores `Burning`.
  - Check results: build with `GenerateDocumentationFile`: exit 0, only the old warnings (CS0419 in `NearbyContainers` and
    `DefenseModelBuilder`, CS1591 in `RecipeCategories`); `dotnet test BrudvikWhiteHilt.Tests`: 576 passed, 0 failed (520
    before, 56 new); `cfg_keys.py --check`: 0 missing. Not tested in the game.
  - Deviations: (1) **No ground fire from a pot on dry land.** `DragonGroundFire` can only be set up with a source
    character, so a source-less one is not possible; the plan says to skip and log then, so `ShotHits` logs it once. (2)
    The stamp RPC sends the fire's point, not an index, because the list can change between the client's view and the
    owner's; the owner picks the fire within 0.75 m (`StampReach`) of it, and accepts the call from a player within 15 m of the
    ship. (3) One extra text, `msg_whitehilt_ship_fire` ("Fire on deck!"),
    sent to those aboard like the hole message. (4) `ShipBallista` has a decor mode on the Nail Ship: it turns freely, has no
    Use or hover, and never takes ammo from a player's pack. `BallistaModel.Build` now returns the component, and a
    `PlaceFailed` flag marks a model that could not be built. (5) `ShipShot.Fire` has an extra `accuracy` parameter, uses
    `Quaternion.LookRotation` for the bolt, and a negative `hitNoise` means "the template's own noise", so the player
    ballista is unchanged. (6) No throw animation for the crew: the pot appears at the hand height. (7) Beyond the plan:
    `Ballistics.Intercept` and `WeaponRules`. (8) A pot thrown by the Nail Ship's own crew that lands on the Nail Ship's own
    deck is ignored. (9) The fire is placed at the deck point nearest the landing within 2.5 m, otherwise none (a pot that
    falls in the sea lights nothing). (10) The docs and the CHANGELOG are left to phase 6. (11) No new settings; the
    non-gameplay constants (1 s tick, 1 m spacing, 2 m height, 1.5 m stamp reach, 2.5 m snap) are in code. (12) On a dedicated
    server nothing is drawn (the ballista model is skipped; the shot origin falls back to the holdfast).
  - For phase 4: extend the `ShotKind` enum and the `ShotHits.OnHit` switch for chain shot and grapnel (a hit is already
    identified by the projectile's prefab name); `RaiderShip.TargetVelocity` and `ShipShot.Fire` are ready to reuse; the
    ammunition choice belongs in `ShipBallista` (`TryFire` now asks `inventory.GetAmmoItem(ShipBolts.AmmoType)` and the
    Nail Ship's decor mode must not take player ammo); the torn-sail factor goes in the `ShipDamage.SailFactor()` postfix;
    the RPC names `WhiteHiltShipTear`, `WhiteHiltShipGrapple` and `WhiteHiltShipCut` are still free (this phase added
    `WhiteHiltShipFire` and `WhiteHiltShipStamp`); a new Use handler follows `DeckFireInput` and sets
    `ShipDamageInput.HandledFrame`.
  - In-game checklist (nothing here was run in the game): (1) Craft Pitch Pots at the Workbench (icon, dark tint). (2)
    Throw one onto a ship's deck: flames, light and crackle, "Fire on deck!"; the hull loses health, a player in the radius
    burns; it goes out after `DeckFireSeconds`, in half the time in rain. (3) Aim at the fire: "Fire on deck / [E] Stamp
    out"; Use costs stamina and puts it out. (4) White Hilt ships take no hull damage but the fire still burns players.
    (5) With a second client: owner and non-owner both see the fire, and a throw from the non-owner starts it. (6)
    `whitehilt_seabattle summon`: the Nail Ship's crew lob pots every `PitchPotSeconds` in Broadside or Board; a pot does not
    hurt its own side. (7) After `defeated_dragon` (or with the key emptied) the ballista appears on the Nail Ship's bow,
    turns toward the target and shoots Ship Bolts that can make holes. (8) The player's ballista still fires as before.
    (9) A pot thrown at dry ground does nothing but one log line.

- **Phase 4 (chain shot, grapnel and the ammunition choice), 2026-10-13.**
  - Done, new files: `Items/ShipUpgrades/ShipAmmoBase.cs` (abstract: the shared clone-of-`TurretBoltWood` item, own projectile
    clone of `Turret_projectile` with a `ShotHits`, tint and icon, `ApplyConfig`), `ChainShot.cs` and `GrapnelBolt.cs` (the two
    items, found by the plugin's reflection since the base is abstract), `SeaBattles/Weapons/AmmoChoice.cs` (pure: `Resolve`,
    `Next`, `KindsCarried`), `GrappleRecord.cs` (pure: the rope as 44 bytes), `GrappleLink.cs` (component on every ship),
    `GrappleInput.cs` (the cut, like `DeckFireInput`) and `Patches/SeaBattles/GrapplePatches.cs` (class-level `[HarmonyPatch]`,
    postfix on `Ship.CustomFixedUpdate`). Changed: `ShotHits` (`ShotKind.ChainShot` = 1, `ShotKind.Grapnel` = 2, `SetShooter`),
    `ShipShot.Fire` (now returns the projectile), `ShipBallista` (ammunition choice, hover and crosshair line), `BallistaPatches`
    (`SetControls` takes `secondaryAttack` too, new `Hud.UpdateCrosshair` postfix), `ShipDamage` (torn sail, `RequestTear`,
    `Tell`, `SenderNear`), `GrappleRules` (`Acceleration`, `AgeSeconds`), `WeaponRules` (`IsTorn`, `TornFactor`), `RaiderTactics`
    and `RaiderShip` (grappled), `ShipAssistPatches` (attach `GrappleLink`, `GrappleInput` tick and prompt), `ShipAssist.CanPush`,
    `BallistaSettings` (`GrappleDamping`) and `Norwegian.json`. 48 new tests in `SeaDamageTests.cs` (`AmmoChoiceTests`,
    `GrappleRecordTests`, `GrappleAccelerationTests`, `SailTearTests`, `GrappledRaiderTests`). No new texts: every text of this
    phase was registered in phase 1.
  - How it works. Items: Chain Shot (Chain 1, Iron 1, `ChainShotsPerCraft`, pierce `ChainShotDamage`, dark iron) and Grapnel Bolt
    (Iron 2, Bog Iron 1, Deer Hide 1, `GrapnelsPerCraft`, pierce `GrapnelDamage`, rope colour) at the Shipwright's Bench, tier
    Swamp, ammo type `$ammo_whitehilt_shipbolt`, bolt speed and spread from the Ship Bolt's settings. Projectile prefabs
    `WhiteHilt_ChainShot_projectile` and `WhiteHilt_Grapnel_projectile`, registered through `PrefabManager.AddPrefab` like the pot's.
    Ammunition choice: the secondary attack (`SetControls` prefix) cycles `Ship Bolt, Chain Shot, Grapnel Bolt` over the kinds
    the gunner carries (`AmmoChoice.Next`); a shot uses the chosen kind if carried, else the first carried (`Resolve`), found with
    `Inventory.GetAmmoItem(ammoType, prefabName)`. The chosen kind is a static on the local client, default Ship Bolt. While manning,
    the crosshair shows "Ammunition: <name> (<count>)" and, with two or more kinds carried, "[secondary attack] Switch ammunition";
    the hover text before manning shows the same first line. The decor ballista of the Nail Ship is untouched and never reads a pack.
    Chain shot: on a hit on a ship other than the one it was shot from, `ShotHits` sends `WhiteHiltShipTear` (registered by
    `ShipDamage`) to the owner, which accepts it from a player within 150 m, writes `whitehilt_ship_torn` (world ticks) and tells
    those aboard `msg_whitehilt_ship_torn` once; `ShipDamage.SailFactor()` multiplies in `1 - SailTearShare` while torn, so the
    existing `Ship.GetSailForce` postfix handles it, also for a Nail Ship. The tear ends by the clock on every machine (a check
    every 0.25 s, since nothing in the ZDO changes); `ShipDamage.Any` is now also true while a sail is torn. Grapnel: the hit
    asks the shooter's ship (`ShotHits.SetShooter`, set right after launch because the gunner may have let go when the bolt
    lands) through `WhiteHiltShipGrapple(other, here, there)`. `here` is a deck point on the holder's rail on the side facing the
    hit (the deck point nearest to a place 4 m outboard of the ballista, 0.8 m up), `there` the other ship's deck point nearest the
    hit, 0.8 m up. The holder's owner checks the sender (a player within 25 m of the ship), the other ship (loaded here, not the
    holder), both points (`FireRules.ValidSpot` against each ship's float box) and the distance (at most `GrappleBreak`), then
    writes `whitehilt_ship_grapple` (`GrappleRecord`: other ship id, both local points, start ticks) and tells both ships
    `msg_whitehilt_ship_grappled`. A new rope replaces an old one. Pull: each ship's owner, in a postfix on `Ship.CustomFixedUpdate`,
    adds `GrappleRules.Acceleration` with `ForceMode.Acceleration` at its own centre of mass: for a rope it holds (towards the
    other end) and for every rope of another loaded ship that names it (static list of links), half the pull each, level with the
    water, and only while taut. The holder's owner checks the rope every 0.25 s: `GrappleRules.Snaps` (stretched past
    `GrappleBreak`, or older than `GrappleSeconds` by the world clock) or the other ship gone from the world (its ZDO unknown) clears it with
    `msg_whitehilt_ship_rope_snaps` to both ships; a ship that is only unloaded here is judged from its ZDO position. Every client
    draws the rope (`LineRenderer`, `Sprites/Default`, hemp colour, like `ShipMooring`) between the two points. Cut: `GrappleInput`
    tests the camera ray against the holder's end of the rope (1 m, reach from the eyes), shows "Grapnel rope / [E] Cut the
    rope", takes Use only when nothing else is under the crosshair and no hole or fire is aimed at, sets
    `ShipDamageInput.HandledFrame` and sends `WhiteHiltShipCut`; the owner accepts it from a player within 15 m. `ShipAssist.CanPush`
    also refuses while a rope end is aimed at. `RaiderInput.Grappled` (from `GrappleLink.IsBound`, either end) makes
    `RaiderTactics.NextState` return Board (Broadside with no warriors) before anything else, so a bound Nail Ship never flees, also
    one that was already fleeing.
  - Verified in the decompile: `Player.SetControls` clears `attack`/`secondaryAttack` and calls `AttachStop` for an attached
    player who presses either (the secondary needs the same prefix as the attack; its parameter names are `secondaryAttack` and
    `secondaryAttackHold`); the controller's buttons are `SecondaryAttack` and `JoySecondaryAttack`; `Ship.CustomFixedUpdate` returns
    right after the controls for a non-owner, so a postfix runs for everyone but only an owner may add forces; `Projectile.OnHit`
    calls `m_onHit` after the hit's damage and before the noise; `Inventory.GetAmmoItem(string, string matchPrefabName)` exists and
    matches `m_dropPrefab.name`; `ZNetView.Register<T, U, V>` exists; `ZDOID(long, uint)` is public and `UserID`/`ID` are
    readable; `Hud.UpdateCrosshair` hides the crosshair only while the player has an attach camera point (the ballista has none),
    so `m_hoverName` shows. Not verified: that the text `$KEY_SecondaryAttack` resolves to the bound key (it is the button name;
    `Localization` is not in the decompile).
  - Check results: build with `GenerateDocumentationFile`: exit 0, no warnings; `dotnet test BrudvikWhiteHilt.Tests`: 624 passed, 0
    failed (576 before, 48 new); `cfg_keys.py --check`: 0 missing. Not tested in the game.
  - Deviations: (1) **One setting more: `[Ships.Ballista] GrappleDamping`** (default 0.5, 0 to 5, Norwegian label added). The plan
    asks for "mild relative damping" without a number, and every gameplay number is a setting. It takes `damping * closing speed`
    off the pull (never below zero: a rope does not push), so two ships do not overshoot. (2) **The rope's holder end is on the
    rail, found from the deck**, not at the muzzle: the ballista's hover box would swallow Use there, so the point is the deck
    point nearest to 4 m outboard of the ballista, 0.8 m up (constants `RailSearch` and `CleatHeight` in `ShotHits`); the
    other end is the deck point nearest the hit, also 0.8 m up. A ship with no deck found falls back to the raw points.
    (3) **The pull is at the centre of mass** with `AddForce`, not `AddForceAtPosition`: a force at the rope's end would also
    twist the ships, which is hard to keep stable and is not asked for; the ships swing alongside by colliding. (4) **The
    tear lives in `ShipDamage`**, not a new component: it already registers the ship's message RPC, owns `SailFactor()` and the
    `Any` count. `ShipDamage.SenderNear` is the shared range check; `DeckFires` keeps its own copy. (5) **Cutting is possible only
    at the holder's end** (as the plan says), so a rope that a Nail Ship's boarders stand next to cannot be cut from its deck. (6)
    A grapnel hit does nothing when the shooter's ship is unknown (a bolt shot by the Nail Ship, or a ballista fired through some
    other route), and a chain shot that hits the ship it was shot from does nothing. (7) No visual for the torn sail beyond the
    message and the slower ship. (8) The gunner's ammunition choice is not saved between sessions. (9) Constants that are not
    gameplay numbers, kept in code: the 0.25 s refresh and check, the 1 s gap between ropes and the 0.2 s gap between tears the
    owner accepts, the 25 m, 15 m and 150 m sender reaches, the 1 m aim radius, the rope's drawing sizes. (10) The docs and the
    CHANGELOG are left to phase 6; the three new ammunition items are already in the settings tables of phase 1 (but
    `GrappleDamping` is not in the plan's table: phase 6 must add it to `docs/ships.md`).
  - In-game checklist (nothing here was run in the game): (1) Make Chain Shots and Grapnel Bolts at the Shipwright's Bench (icons,
    dark and hemp tints; recipes in the Shipwright's Bench list). (2) Man the ballista with all three kinds in the pack: the
    crosshair shows "Ammunition: Ship Bolt (n)" and the switch line; press the secondary attack (right mouse or gamepad): it cycles
    and you are NOT thrown off the ballista; with one kind only the switch line is gone; check the key shows as a key name and
    not as `$KEY_SecondaryAttack`. Fire: the chosen kind leaves the pack; with the chosen kind gone, the next kind carried is
    fired. (3) Chain shot at another ship (a friend's, a Karve, a Nail Ship): "The sail is torn!" for those aboard, the ship loses
    half its sail force for `SailTearSeconds` and recovers; a hit on your own bow does nothing; a Nail Ship in Approach is slower.
    (4) Grapnel at a ship within `GrappleBreak`: "The ships are bound together!" on both ships, a hemp rope between a place on
    your rail and the other deck, and the ships draw together to about `GrappleLength` and then bump; check no wild swinging or
    oscillation (tune `GrapplePull` and `GrappleDamping`), also with the other ship owned by another client. (5) A Nail Ship
    that is bound does not flee at low crew or hull, keeps alongside and sends its warriors over; it also does not run when it
    was already fleeing. (6) Sail away or let the time run out: the rope snaps at `GrappleBreak` metres or after `GrappleSeconds`
    with "The rope snaps."; sink or remove the other ship and it parts too. (7) Look at the rope's end on your rail: "Grapnel
    rope / [E] Cut the rope"; Use cuts it, pulling stops, the boarding leap and the boat push are not triggered by the same press, and a hole or
    fire aimed at first keeps the key. (8) With a second client: both see the rope and the tear, and the one who does not own
    either ship can still shoot, cut and see the messages only when aboard. (9) With `[Ships.Ballista] Enabled` off the items
    cannot be made and the ropes/tears are refused by the owner.
  - For phase 5 and 6: phase 5 (the Ram Prow) needs nothing from this phase; a ram that sends a vanilla `HitType.Boat` hit
    still reaches `ShipDamage.TryHit` as before. A ship held by a rope is pulled by the postfix on `Ship.CustomFixedUpdate`
    (`GrapplePatches`), so a ram's closing speed is a little higher between two bound ships. Free RPC names are unchanged for
    phase 5. Used on a ship's `ZNetView` now: `WhiteHiltShipPatch`, `...Bail`, `...Tear`, `...Message`, `...Fire`, `...Stamp`,
    `...Grapple`, `...Cut` and the older ones. ZDO keys: `whitehilt_ship_torn` (long ticks) and `whitehilt_ship_grapple` (44 bytes).
    For the docs: the Chain Shot's and the Grapnel Bolt's recipes and defaults are in `BallistaSettings` and above; the console
    has no new command.

- **Phase 5 (the Ram Prow), 2026-10-13.**
  - Done, new files in `Pieces/Ships/Ram/`: `ShipRam.cs` (the collision handler, on the ship root) and `RamProw.cs` (the iron look and
    its placement). Changed: `Items/ShipUpgrades/ShipUpgrades.cs` (`ShipRamUpgrade`, `Bit = 10`),
    `WhiteHiltShipUpgradeBase` (`Count` 10 to 11, new `RegistersTexts` switch), `WhiteHiltShipUpgradeSetup.Prepare` (adds `ShipRam`
    and builds the prow, for both ships since Skidbladnir calls `Prepare` too), `WhiteHiltShipUpgrades` (finds the prow part and
    switches it with the bit), `Skidbladnir.cs` (`RamProw.ObjectName` in `UpgradeObjects`), `RamRules` (`ApproachSpeed`, `Push`,
    `CooledDown`), `RamSettings` (`PushPerSpeed`), `Norwegian.json` (label of `PushPerSpeed`) and 12 new tests in
    `SeaDamageTests.cs` (`RamRulesTests`).
  - How it works. Item: Ram Prow (prefab `WhiteHiltShipRam` = `RamSettings.UpgradeName`), Shipwright's Bench, Iron 6, Bog Iron 4,
    Fine Wood 8, Chain 2, clone of `Iron`, tier Swamp, gate `RamSettings.Enabled`, weight from `[Gear.ShipUpgrades] Weight` like the
    others. Its name and description are registered once, in `WeaponSettings.AddTranslations`; the new virtual
    `WhiteHiltShipUpgradeBase.RegistersTexts` is false for the Ram Prow so the constructor skips its own registration (the other
    upgrades are unchanged). Look: `RamProw.Build` copies the plate mesh and material of the vanilla `iron_wall_1x1` (`New/default`,
    material `metalwall`) into four plates: two side plates that sweep back from the stem at 25 degrees (a V), a fin along the stem
    and a beak at the foot of the stem. The part is a child of the ship root, switched off until the bit is set, and has no collider.
    When it is first shown `RamProw.Start` casts rays from ahead of the bow, at two heights below the front holdfast
    (`interactive/front`, 0.6 and 2.4 m below it), backwards along the ship at the hull's own colliders (layer `vehicle`, only those of
    this ship's body), and stands the prow on the stem between the two hits, leaning along it; with no hit it falls back to the
    holdfast's length and logs "partly guessed". The result is logged once per ship with `Ram Prow on <ship>: stem at ...`.
    Because the holdfast moves with Skidbladnir's own model (`PlaceSeats`) the same code serves both ships. Ram: `ShipRam.OnCollisionEnter`
    runs on the ship's owner only, and only with the bit and `RamSettings.Enabled`. Target: another ship (the rigidbody that was hit has a
    `Ship`, not this one) or a creature (not a player, not tamed, alive). Contact: the foremost of the contacts, measured from the float
    box's middle; `RamRules.IsBowContact` wants it in the front quarter of the float box's length. Speed: `RamRules.ApproachSpeed`
    (below) of `collision.relativeVelocity` along the ship's forward, at least `MinSpeed`; per-target cooldown (`Cooldown`) kept by
    instance id, old entries dropped from 32 entries on. A ship: `WearNTear.Damage` on the target hull with blunt and chop of half the
    damage each, `HitType.Boat`, `m_point` the contact, `m_dir` the bow flattened to the water, tool tier at the maximum so no hull is
    too hard; the hull's owner then runs the usual `RPC_Damage` hook, where `ShipDamage.TryHit` always makes a hole for `HitType.Boat`.
    A creature: `Character.Damage` with blunt damage and `m_pushForce = closing speed x PushPerSpeed`. Attacker: the local player when
    they are aboard, so kills and aggro count as theirs, otherwise none. The ramming ship takes `SelfDamageShare` of the damage through
    `WearNTear.ApplyDamage` only when its hull is not immune (the White Hilt ships always are, so in practice never). Effects: the ship's
    own `m_waterImpactEffect` (the effect it plays when it slams on a wave) at the contact, and a camera shake on the machine that
    rams (`GameCamera.AddShake`, range 40 m, strength speed/10 clamped to 0.2 to 1); the struck hull plays its own hit effect inside
    `RPC_Damage`. A dedicated server shows no effect.
  - Verified in the decompile and the game's data: (1) `Collision.relativeVelocity`: Unity's documentation only says "the relative linear
    velocity of the two colliding objects"; it names no sign and the two sides of one collision get opposite vectors, so **the sign
    cannot be relied on**. `RamRules.ClosingSpeed` stays as the plan describes it (own minus target) but `ShipRam` calls the new
    `RamRules.ApproachSpeed`, which returns the larger of the closing speed of the vector and of its negation, i.e. its size along the bow.
    That is right because a collision begins with the bodies approaching, and for a contact in the bow quarter the approach is along the
    bow. The ship's own `linearVelocity` cannot be used instead: inside `OnCollisionEnter` it is already the post-impact velocity.
    (2) `Collision`s are delivered to the object with the rigidbody; the `Ship` is on the root, the rigidbody on the root
    (`ShipBallista` finds it with `GetComponentInParent<Rigidbody>()`), so `ShipRam` on the root gets them. (3) `Character.Damage(hit)`
    invokes `RPC_Damage` on the owner; `Character.RPC_Damage` returns early when the hit has an attacker id that is not loaded, so the
    attacker is only set to a loaded local player; `ApplyPushback` moves a creature by `pushForce / m_body.mass * 2.5` m/s along the
    flattened `m_dir`. (4) `WearNTear.Damage(hit)` invokes `RPC_Damage` on the owner, which applies resistances, checks the tool tier,
    plays `m_hitEffect` at `hit.m_point` and then reaches `ApplyDamage`. (5) `GameCamera.AddShake(point, range, strength, continuous)`
    is local to the camera. (6) `Ship.m_waterImpactEffect` is an `EffectList` of two effect prefabs on `VikingShip` (the data tool does
    not print their names); `RaiderCrew` already uses it for the splash of a leap. (7) The vanilla iron mesh: `iron_wall_1x1/New/default`
    is a plate about 1.0 x 1.16 x 0.12 m with the `metalwall` material (the `woodiron_beam` mesh is a wooden beam with iron bands, so
    it was not used). (8) The longship's front holdfast is at (0, 2.416, 9.959) in the ship's space, so the search for the stem runs from
    y 1.8 down to y 0.0 on it; Skidbladnir's is at `At(0, 3.3, 10.75)`.
  - Check results: build with `GenerateDocumentationFile`: exit 0, no warnings; `dotnet test BrudvikWhiteHilt.Tests`: 636 passed, 0 failed
    (624 before, 12 new); `cfg_keys.py --check`: 0 missing. Not tested in the game.
  - Deviations: (1) **One setting more: `[Ships.Ram] PushPerSpeed`** (default 20, 0 to 500, Norwegian label added); the plan says "blunt and a
    push" without a number, and every gameplay number is a setting. (2) `RamRules.ApproachSpeed`, `Push` and `CooledDown` are extra
    pure helpers; the plan's `ClosingSpeed` is kept and still tested. (3) **The prow's place is found by rays at runtime**, not fixed
    numbers: the hull's stem was not measured on either ship and cannot be seen without the game (see the checklist). The plates are
    plain scaled copies, so a stem that is not roughly a V in plan will show the plates a little inside or proud of the planks. (4) The
    prow is a look only; it adds no collider, so the hull's collision shape is unchanged and the bow quarter test uses the ship's own
    colliders. (5) **Camera shake only on the ramming machine** (the ship's owner), because the camera shake is local and no new RPC was
    added; a second player aboard sees the splash and the damage but not the shake. (6) A Nail Ship's hull is rammed through the same
    path as any other ship; nothing special was added for its AI. (7) Players, tamed creatures and dead creatures are never rammed. (8)
    Non-gameplay constants in code: the prow's plate sizes and sweep, the ray heights and the shake range. (9) The docs row and the
    CHANGELOG are left to phase 6.
  - For phase 6 (docs): the new settings section `[Ships.Ram]` has six keys (the table in section 4 plus `PushPerSpeed`); the upgrade is
    the eleventh (bit 10), so Skidbladnir's and the White Hilt Ship's upgrade count is eleven; recipes and defaults are in the summary of
    the final report and above.
  - In-game checklist (nothing here was run in the game): (1) Make a Ram Prow at the Shipwright's Bench (Iron 6, Bog Iron 4, Fine Wood 8,
    Chain 2; icon is the iron icon); use it on the mast of the White Hilt Ship and of Skidbladnir: "added", and an iron V with a fin and
    a beak shows on the bow. The log line `Ram Prow on VikingShip...: stem at z ...` gives the numbers used; check that the plates hug
    the stem and are neither floating off it nor buried (the longship is expected near z 9 to 10 between y 0 and 1.8, Skidbladnir
    higher), and that the log does not say "partly guessed". Take the upgrade off: the prow goes. (2) Sail bow-first at a Karve, a Raft
    or a Nail Ship (`whitehilt_seabattle summon`) at 4 to 5 m/s: the target's hull takes damage of about 40 x speed, the hull gets a
    hole on its side (patch it), the splash plays and the camera shakes; a collision at less than `MinSpeed` (3 m/s) or with the side or
    stern does nothing, and the same target is not hit again within 1.5 s. (3) Ram a serpent or a boar from a boat: it takes blunt damage
    and is pushed; a kill counts for the player aboard; a tamed animal and a player in the water are not touched. (4) Ram with the White
    Hilt Ship: the ship takes no damage (immune). (5) With two clients, one owning the ship: only one ram per collision, damage and hole
    appear for both, the shake only on the owner's screen; the non-owner's view of the prow is the same. (6) Switch `[Ships.Ram] Enabled`
    off: the item cannot be made and a ship with the prow rams nothing. (7) Check a ship with the ballista and the prow together: the prow
    is on the stem and the ballista behind the holdfast, and neither blocks the other's Use.

- **Phase 6 (documentation), 2026-10-13.**
  - Done (no code changed): `docs/sea-battles.md` has new sections *Damage at sea* (*Holes*, *Flooding*, *Patching and
    bailing*), *Fire on deck*, *The Nail Ship fights back* and *The Pitch Pot* (item row with recipe and station), between
    *The Ship Ballista* and *Raids*; the ballista section and the intro mention the new ammunition and damage; the multiplayer
    list, the console table (`hole [n]`) and the config section (full `[SeaBattles.Damage]` and `[SeaBattles.Weapons]`
    tables with every key, default and range) are updated. `docs/ships.md`: the Ram Prow row in the upgrade table and a
    *The Ram Prow* section, "eleven" upgrades, Chain Shot and Grapnel Bolt rows next to the Ship Bolt, bullets for choosing
    ammunition and cutting a rope, the ten new `[Ships.Ballista]` keys with `GrappleDamping`, and a `[Ships.Ram]` table (six keys
    with `PushPerSpeed`); links to *Damage at sea*. `docs/ideas.md`: the two *(planned)* bullets are gone, and no link to
    `plans/` is left in `docs/*.md` or `README.MD`. `README.MD`: the Sea battles card line; "New in ..." untouched. `CHANGELOG.md`:
    a new `## Unreleased` with `### Added` (one bullet per feature, linked) and `### Changed`. `docs/architecture.md`: unchanged,
    no pattern it describes moved. `Package/README.md` and `Package/CHANGELOG.md` are not touched (the release step builds them).
  - Every number, default, range, recipe, station, key and text was read from the source (the settings classes, the item classes,
    `ShipDamage`, `DeckFires`, `ShotHits`, `NailShipWeapons`, `WeaponRules`, `GrappleLink`, `ShipRam`, `ShipBallista`,
    `SeaBattleCommands`, `Norwegian.json`) and not from this plan.
  - Check results: `cfg_keys.py --check`: 0 missing; `dotnet test BrudvikWhiteHilt.Tests`: 636 passed, 0 failed (no code changed);
    a read-only link and anchor check of `README.MD`, `CHANGELOG.md` and `docs/*.md`: every link and anchor added here resolves, no
    link to `plans/` (three older emoji-heading anchors in `CHANGELOG.md`, `chests.md` and `defences.md` that the script's simple
    slug rule cannot match were already there).
  - Deviations: (1) Beyond the list: a `### Changed` entry in the changelog (the secondary attack no longer lets go of the ballista;
    new network messages), the *Man it*, *Shoot* and *Let go* bullets of `docs/ships.md` (they still said the secondary attack
    makes you step away), a bullet in the White Hilt ships' feature list, and the sea-battles row of `docs/README.md`. (2) The Pitch
    Pot row is in `docs/sea-battles.md`, not `docs/equipment.md`: it is not gear, and the page that explains fire is the better
    place. (3) The docs say "Shift + Use" for bailing: the code reads the alternative-use key (`AltPlace`, Left Shift by default;
    the gamepad key differs).
  - Seen while documenting (reported, not fixed): (a) `[Ships.Ram] SelfDamageShare` has no effect in practice: only the White Hilt
    ships can have the Ram Prow and they are immune, so the page says so. (b) A Nail Ship's ballista has no on/off switch: only
    `NailBallistaKey` (empty means always) and the shot gap (at most 120 s); `PitchPots` has one. (c) The message `No Ship Bolts`
    (`msg_whitehilt_ballista_noammo`, in `SeaBattleSettings`) is now shown when none of the three kinds is carried. (d) The Pitch Pot
    can be made while `[SeaBattles.Damage] Enabled` is off (its gate is `[SeaBattles] Enabled`), but then sets no fire. (e) A patch
    with too little stamina gives no message when bailing (the key does nothing); the patch prompt shows even without wood and
    resin and only the press says so. (f) The ammunition choice is a static on the client, not saved. (g) The ram's camera shake is
    only on the machine of the ship's owner (see phase 5). (h) The crosshair's switch line uses the text `$KEY_SecondaryAttack`,
    still unverified in the game (see phase 4). (i) Any heavy hit counts as a hole while a Nail Ship that is not a prize is within
    `BattleRange`, also one from a serpent, the Kraken or a player's weapon; the docs say "a heavy hit", as the code does.
