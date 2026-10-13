# ⚓ Plan: sea battles and Viking raids

Implementation plan for one feature from [Ideas for new content](../ideas.md). It is written for an AI agent (or a
person) who carries it out phase by phase without the conversation that produced it. Read it all once before you start.
Back to [Ideas](../ideas.md) · [Documentation](../README.md)

## 1. What the player gets

- **Nail Ships.** On the open sea, after Bonemass is slain, a Nail Ship from Naglfar's fleet may come for your ship: a
  dark longship crewed by draugr. A horn sounds and it shows on the map. It sails up, lies alongside at bow-shot range
  while its archers shoot, then closes in so its warriors can leap aboard. You can outrun it, sink it, or kill its crew.
- **Ship Ballista.** A new ship upgrade for the White Hilt Ship and Skidbladnir: a ballista on the forecastle. Use it to
  man it, aim with the camera and fire Ship Bolts with the attack button. It also works against serpents and the Kraken.
- **Boarding leap.** Standing on a ship's deck within reach of another ship, look at it and press Use to leap aboard.
- **The prize.** When the whole crew is dead the Nail Ship is yours: open its hold, take the helm and sail it. Naglfar's
  nails do not hold for long, so it sinks after a while (the hold drops as a cargo crate).
- **Raids (strandhogg).** A Nail Ship's hold may carry a Raid Chart. Using it marks a draugr harbour on an explored coast:
  a moored Nail Ship, guards on the shore and a hoard chest that only opens once the guards are dead.
- **Saga.** Taking or sinking a Nail Ship and sacking a harbour are deeds in the [Saga](../saga.md), with renown.

Nothing here needs new 3D models: the Nail Ship is a recoloured clone of the vanilla longship, the ballista uses the
vanilla ballista's meshes, the crew are vanilla creatures.

## 2. Decisions already made

| Decision | Choice | Why |
|---|---|---|
| Biome | Ocean counts as Swamp (biome rule). Recipes use Swamp materials or earlier; encounters need `defeated_bonemass` | Same as the Kraken and the ship upgrades |
| Crew | Vanilla creatures by progression tier, from a setting. Default tiers: draugr after Bonemass, more and elite draugr after Moder, fulings after Yagluth | Enemies scale with the world, recipes do not |
| Enemy ship | Clone of vanilla `VikingShip` with vanilla ship physics. A transpiler lets it sail without players aboard | Reuses buoyancy, sail, rudder and network sync |
| Steering | The existing `ShipAssist.SteerTowards`, `ShallowAhead` and `Blocked` | Already tested for the route autopilot |
| Ballista | A ship upgrade (bit 9), player-manned, not an automatic turret | A vanilla `Turret` needs its own `ZNetView`; upgrades share the ship's |
| Ballista ammo | Own item `WhiteHiltShipBolt`, cloned from `TurretBoltWood`, own ammo type, made at the Shipwright's Bench | Keeps it out of the Mistlands turret |
| Raid sites | Created by the server as ZDOs, like the treasure mounds (`TreasureService.TryBury`) | The server may not have the zone loaded |
| Defaults | `[SeaBattles] Enabled` and `[Ships.Ballista] Enabled` default **true** | Decided by the user 2026-10-13 |
| Release | Not part of this plan. Changelog under `## Unreleased`; a later release is a MINOR bump (new RPCs) | Copilot instructions |

## 3. Rules for the implementer

Read [.github/copilot-instructions.md](../../.github/copilot-instructions.md) and [architecture.md](../architecture.md)
first. On top of those:

1. **Every gameplay number is a setting** (`WhiteHiltConfig.BindAdminOnly`), with a Norwegian label and description in
   `BrudvikWhiteHilt/Translations/Norwegian.json` as `whitehilt_cfg_<section>_<key>` and `..._desc` (section and key
   lowercased, dots become `_`, e.g. `whitehilt_cfg_seabattles_chanceperminute`), a section label
   `whitehilt_cfgsec_<section>`, and a row in the feature page's settings table. Check with
   `python AssetSource\Tools\cfg_keys.py --check` (0 missing). Use literal `const string Section = "..."` values;
   the checker cannot follow concatenated section names.
2. **Every patch class carries a class-level `[HarmonyPatch]`.** `PatchAll` silently skips a class without it.
   `PatchTests` fails if you forget.
3. **Never `Object.Destroy` a GameObject with a `ZNetView`.** Use `ZNetScene.instance.Destroy(go)` (owner only).
4. **Owner rules.** Only the ZDO owner simulates an object and writes its ZDO. Start such code with
   `if (!nview.IsOwner()) return;`. The raider ship's owner and its crew's owners can be different clients.
5. **Headless servers draw nothing.** Every recolour, icon, mesh copy and sound clone returns at once when
   `VisualHelper.IsHeadless`, and catches every exception, keeping the vanilla look.
6. **Do not edit files from the terminal** (PowerShell 5.1 corrupts UTF-8 Norwegian text). Use the editor tools.
   Never write `.new`/`.tmp` files and move them.
7. **BepInEx keys and sections** must not contain `= \n \t \ " ' [ ]`.
8. **Verify vanilla names before using them.** `python AssetSource\Tools\vanilla_prefab.py <prefab> --values`
   (add `--all-bundles` for pieces and ships; it takes a minute). Names already verified for this plan:
   `TurretBoltWood` (ammo type `$ammo_turretbolt`, pierce 75, projectile `Turret_projectile`, velocity 40,
   accuracy 2), `BoltIron` (projectile `arbalest_projectile_iron`). Everything else in this plan marked
   *(verify)* must be checked.
9. **Read the game's code before guessing.** A full decompile may be at `%TEMP%\wh_tame\full\*.cs`. If it is gone:
   `dotnet tool install ilspycmd --tool-path %TEMP%\ilspy`, then
   `%TEMP%\ilspy\ilspycmd -p -o %TEMP%\wh_tame\full "C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data\Managed\assembly_valheim.dll"`.
10. **Build and test after each phase** (section 9). `get_errors` in the editor is not enough; a real build is.
11. **Do not commit, push, deploy or release.** Leave the work uncommitted for the user.
12. **Deviations.** If something in this plan turns out wrong, choose the smallest change that keeps the described
    behaviour, and write it in the log at the end of this file (section 11). Do not change player-visible behaviour
    beyond this plan without asking.
13. XML doc comments on every public/protected member; a short what-and-why `//` comment above longer private
    methods (which machine runs it and why). Class member order: constants, fields, properties, constructor, public,
    protected, private.

## 4. Files

New (namespace follows the folder):

| File | Purpose |
|---|---|
| `SeaBattles/SeaBattleSettings.cs` | Sections `SeaBattles` and `SeaBattles.Raids` |
| `SeaBattles/CrewTable.cs` | Parses and picks the crew tier (pure, tested) |
| `SeaBattles/RaiderTactics.cs` | Decides state, course and sail from plain values (pure, tested) |
| `SeaBattles/NailShipRegistry.cs` | Clones `VikingShip` into `WhiteHilt_NailShip`, its look and loot; the Raid Chart and hoard prefabs |
| `SeaBattles/RaiderShip.cs` | On the Nail Ship: state, AI on the owner, crew spawning, prize, sinking |
| `SeaBattles/RaiderCrew.cs` | On each crew creature: holds its post, boarding leaps |
| `SeaBattles/RaiderDeck.cs` | Finds standing places on a ship's deck by raycast |
| `SeaBattles/SeaBattleService.cs` | On the `Game` object: encounter requests, server roll and cooldown, admin summon |
| `SeaBattles/SeaBattleCommands.cs` | Console command `whitehilt_seabattle [status|summon|raid]` |
| `SeaBattles/BoardingLeap.cs` | The local player's leap from one ship to another |
| `SeaBattles/Raids/RaidService.cs` | Server side of raid charts: picks the site, creates its ZDOs |
| `SeaBattles/Raids/RaidHoard.cs` | On the hoard chest: locked while guards live, fills itself once, the saga deed |
| `Pieces/Ships/Ballista/ShipBallista.cs` | The ballista on the ship: hover, man it, aim, fire, pose |
| `Pieces/Ships/Ballista/BallistaSettings.cs` | Section `Ships.Ballista` |
| `Pieces/Ships/Ballista/BallistaAim.cs` | Clamps aim to the firing arc (pure, tested) |
| `Items/ShipUpgrades/ShipBolts.cs` | The `WhiteHiltShipBolt` ammo item |
| `Patches/SeaBattles/RaiderShipPatches.cs` | Transpiler on `Ship.CustomFixedUpdate`, helm and hold locks, crew AI postfix |
| `Patches/SeaBattles/BallistaPatches.cs` | Blocks the player's own attack while manning; crosshair prompt |
| `Patches/SeaBattles/SeaBattleGamePatches.cs` | Adds the services to `Game` in `Game.Start` |
| `BrudvikWhiteHilt.Tests/SeaBattleTests.cs` | Tests for the pure parts |
| `docs/sea-battles.md` | The feature page |

Changed: `BrudvikWhiteHilt.cs` (Initialize calls), `Items/ShipUpgrades/WhiteHiltShipUpgradeBase.cs` (`Count` 9 → 10),
`Items/ShipUpgrades/ShipUpgrades.cs` (new `ShipBallistaUpgrade`), `Pieces/Ships/WhiteHiltShip/WhiteHiltShipUpgrades.cs`
and `WhiteHiltShipUpgradeSetup.cs` (ballista part), `Patches/Navigation/ShipRoutePaddlePatch.cs` (`Steered`),
`Patches/Ships/TailwindPatch.cs`, `Pieces/Ships/ShipAssist.cs` (no hover/push on raiders),
`Pieces/Ships/ManOverboard.cs` and `Pieces/Ships/HarbourAnchor/HarbourAnchor.cs` (skip raiders),
`Kraken/KrakenService.cs` (no Kraken on a raider deck), `Mastery/MapPings.cs` (`seaMonsters` list),
`Saga/SagaLog.cs` + `Saga/SagaSettings.cs` (two new deeds), `Treasure/TreasureLoot.cs` (public roll from a given list),
`Translations/Norwegian.json`, `docs/README.md`, `docs/ships.md`, `docs/saga.md`, `docs/ideas.md`, `README.MD`,
`CHANGELOG.md`.

The csproj is SDK-style and picks up new `.cs` files by itself.

## 5. Names, keys and texts

Prefabs: `WhiteHilt_NailShip`, `WhiteHiltShipBallista` (upgrade item), `WhiteHiltShipBolt`, `WhiteHiltRaidChart`,
`WhiteHilt_RaidHoard`.

ZDO keys (hash once in a `static readonly int` with `GetStableHashCode()`):

| Key | On | Type | Meaning |
|---|---|---|---|
| `whitehilt_raider_target` | ship | ZDOID | The ship it hunts |
| `whitehilt_raider_state` | ship | int | `RaiderState` |
| `whitehilt_raider_side` | ship | int | -1 port, 1 starboard of the target |
| `whitehilt_raider_tier` | ship | int | Crew tier index |
| `whitehilt_raider_crewsize` | ship | int | Crew spawned; 0 = not yet |
| `whitehilt_raider_prize` | ship | long | World ticks when it became a prize; 0 = not |
| `whitehilt_raider_seen` | ship | long | World ticks a player was last within `DespawnRange` |
| `whitehilt_raider_saga` | ship | bool | Saga deed already announced |
| `whitehilt_raider_leap` | ship | int | Index of the melee crew member whose turn it is to leap; -1 = nobody |
| `whitehilt_raider_site` | ship, crew, hoard | string | Raid site id; empty at sea |
| `whitehilt_raider_ship` | crew | ZDOID | Its ship; None for shore guards |
| `whitehilt_raider_post` | crew | Vector3 | Post, ship-local for crew, world for guards |
| `whitehilt_raider_role` | crew | int | 0 melee, 1 archer |
| `whitehilt_raider_index` | crew | int | Its number among the ship's melee crew, from 0 |
| `whitehilt_raider_boarded` | crew | bool | Has leapt onto another ship |
| `whitehilt_ballista_ship` | player | ZDOID | Ship whose ballista the player mans; None if not |
| `whitehilt_ballista_aim` | player | Vector3 | x = yaw, y = pitch (ship-local degrees) |
| `whitehilt_raid_filled` | hoard | bool | Loot put in |
| `whitehilt_raid_done` | hoard | bool | Saga deed announced |

`RaiderState`: `Approach = 0, Broadside = 1, Board = 2, Flee = 3, Prize = 4, Moored = 5`.

Routed RPCs (registered in the services' `Start`): `WhiteHiltSeaBattleRequest`, `WhiteHiltSeaBattleSpawn`,
`WhiteHiltSeaBattleAdmin`, `WhiteHiltSeaBattleReply`, `WhiteHiltRaidRequest`, `WhiteHiltRaidReply`. Object RPC on the
hoard: `WhiteHiltRaidDone`.

English texts (register with `Translations.AddEnglish` next to the code) and their Norwegian ones:

| Key | English | Norwegian |
|---|---|---|
| `whitehilt_nailship` | Nail Ship | Naglskip |
| `msg_whitehilt_raider_sighted` | Sails on the horizon! A Nail Ship comes for you. | Seil i horisonten! Et naglskip kommer for dere. |
| `msg_whitehilt_raider_flees` | The Nail Ship turns and runs. | Naglskipet snur og flykter. |
| `msg_whitehilt_raider_prize` | The crew is dead. The Nail Ship is yours, but its nails will not hold for long. | Mannskapet er dødt. Naglskipet er ditt, men naglene holder ikke lenge. |
| `whitehilt_raider_prize_hover` | Prize: sinks in {0} min | Prise: synker om {0} min |
| `msg_whitehilt_raider_helm_held` | The dead still hold the helm. | De døde holder fortsatt roret. |
| `msg_whitehilt_raider_hold_guarded` | The crew guards the hold. | Mannskapet vokter lasten. |
| `whitehilt_boarding_leap` | Leap aboard | Hopp om bord |
| `whitehilt_ballista_man` | Man the ballista | Bemann ballistaen |
| `msg_whitehilt_ballista_noammo` | No Ship Bolts | Ingen skipsbolter |
| `msg_whitehilt_raid_marked` | A draugr harbour is marked on your map. | En draugrhavn er merket på kartet ditt. |
| `msg_whitehilt_raid_nosite` | No coast on this chart is known to you. | Du kjenner ingen kyst på dette kartet. |
| `msg_whitehilt_raid_guarded` | The guards still live. | Vaktene lever ennå. |
| `whitehilt_raid_pin` | Draugr harbour | Draugrhavn |
| `whitehilt_saga_seabattle` | A Nail Ship was taken: {0} | Et naglskip ble tatt: {0} |
| `whitehilt_saga_raid` | A draugr harbour was sacked | En draugrhavn ble herjet |

Items (name and description via `Translations.AddEnglishNameAndDescription`):

| Prefab | English name | English description | Norwegian name |
|---|---|---|---|
| `WhiteHiltShipBallista` | Ship Ballista | A ballista for the forecastle of the White Hilt Ship or Skidbladnir. Use it on the mast; then use the ballista to man it and shoot Ship Bolts. | Skipsballista |
| `WhiteHiltShipBolt` | Ship Bolt | A heavy iron-tipped bolt for the Ship Ballista. | Skipsbolt |
| `WhiteHiltRaidChart` | Raid Chart | A sea chart in a dead man's hand. Use it to mark a draugr harbour on a coast you know. | Strandhoggkart |

Write the Norwegian descriptions in the same plain style as the rest of `Norwegian.json`.

## 6. Phases

Each phase ends with the checks in section 9. Stop after a phase if a check fails and fix it before going on.

### Phase 1: settings, texts and skeleton

1. `SeaBattles/SeaBattleSettings.cs`, static class like `Kraken/KrakenSettings.cs`, `Initialize()` called from
   `BrudvikWhiteHilt.Awake` right after `Kraken.KrakenSettings.Initialize()`. All admin only:

   Section `SeaBattles`:

   | Key | Default | Range | Meaning |
   |---|---|---|---|
   | `Enabled` | true | | Nail Ships can come, and raid charts work |
   | `RequiredKey` | `defeated_bonemass` | | Global key needed; empty = from the start |
   | `ChancePerMinute` | 4 | 0–100 | Percent per minute for one player while the conditions hold |
   | `ChancePerExtraPlayer` | 2 | 0–100 | Extra points per extra player aboard |
   | `CooldownMinutes` | 45 | 0–1440 | Real minutes before the next Nail Ship anywhere |
   | `MinDepth` | 20 | 5–200 | Least water depth under the player's ship |
   | `MinShoreDistance` | 100 | 0–500 | No land within this many metres |
   | `SpawnDistance` | 180 | 80–400 | Metres from the target where the Nail Ship appears |
   | `CrewTiers` | see below | | Crew per progression key |
   | `CrewPerExtraPlayer` | 1 | 0–5 | Extra melee crew per extra player aboard the target |
   | `HullHealth` | 1200 | 100–20000 | Health of a Nail Ship |
   | `SailForceFactor` | 1 | 0.2–5 | Multiplier on the vanilla longship's sail force |
   | `Tailwind` | true | | Nail Ships always have the wind behind them |
   | `BroadsideDistance` | 25 | 10–60 | Metres it keeps alongside while its archers shoot |
   | `BroadsideSeconds` | 40 | 0–300 | Seconds alongside before it closes in to board; 0 = never boards |
   | `BoardDistance` | 6 | 3–15 | Metres within which its warriors leap across |
   | `BoardIntervalSeconds` | 4 | 1–30 | Seconds between two warriors leaping |
   | `FleeCrewShare` | 0.25 | 0–1 | It flees when this share of its crew is left |
   | `FleeHullShare` | 0.2 | 0–1 | It flees when this share of its hull health is left |
   | `DespawnRange` | 300 | 100–1000 | Metres; with no player this close it sinks out of sight |
   | `DespawnSeconds` | 60 | 10–600 | Seconds without a player in range before it goes |
   | `PrizeMinutes` | 10 | 1–60 | Minutes a prize floats before it sinks |
   | `Loot` | see below | | Loot list for the hold, format of `[Treasure] Loot` |
   | `LootRolls` | 4 | 0–20 | Draws from the loot list |
   | `RaidChartChance` | 0.35 | 0–1 | Chance the hold carries a Raid Chart |
   | `BoardingLeapRange` | 10 | 3–25 | Metres a player can leap from deck to deck |

   `CrewTiers` default (verify each prefab name):
   `defeated_bonemass=Draugr:3,Draugr_Ranged:2; defeated_dragon=Draugr:3,Draugr_Ranged:2,Draugr_Elite:1; defeated_goblinking=Goblin:3,GoblinArcher:2,GoblinBrute:1`.
   The last tier whose key is set wins. A prefab name containing `Ranged` or `Archer` gets role archer, others melee.
   Unknown prefabs are skipped with a warning.

   `Loot` default (Swamp tier or earlier, verify names):
   `Coins:40-120:10, Iron:3-8:6, Chain:1-2:4, WitheredBone:4-8:6, Amber:1-2:4, AmberPearl:1-2:3, Ruby:1-2:2, WhiteHiltShipBolt:5-10:6, TrophyDraugr:1-1:2`.

   Section `SeaBattles.Raids`:

   | Key | Default | Range | Meaning |
   |---|---|---|---|
   | `MinDistance` | 400 | 100–3000 | Least distance from the chart's user to the harbour |
   | `MaxDistance` | 1500 | 200–5000 | Farthest distance |
   | `Guards` | `Draugr:4,Draugr_Ranged:2,Draugr_Elite:1` | | Shore guards |
   | `GuardRadius` | 12 | 4–40 | Metres round the hoard the guards stand |
   | `GuardHoldRadius` | 8 | 2–40 | Metres a guard walks from its post before it turns back |
   | `ShipSearchRadius` | 80 | 20–200 | Metres from the hoard to look for deep water for the moored ship |
   | `Loot` | as the sea loot plus `GiftOfOdin:1-1:2` | | Hoard loot |
   | `LootRolls` | 8 | 0–30 | Draws |
   | `MaxActive` | 6 | 1–50 | Raid sites in the world at most |

   Section `Ships.Ballista` (in `Pieces/Ships/Ballista/BallistaSettings.cs`, Initialize after `ShipSettings`):

   | Key | Default | Range | Meaning |
   |---|---|---|---|
   | `Enabled` | true | | The ballista upgrade and bolts can be made, and the ballista used |
   | `Damage` | 80 | 1–1000 | Pierce damage of a Ship Bolt |
   | `ReloadSeconds` | 4 | 0.5–30 | Seconds between shots |
   | `BoltSpeed` | 60 | 10–150 | Metres per second |
   | `Accuracy` | 1 | 0–10 | Random spread in degrees |
   | `YawArc` | 70 | 10–170 | Degrees either side of straight ahead it can turn |
   | `PitchMin` / `PitchMax` | -10 / 25 | -45–60 | Degrees it can aim down and up |
   | `BoltsPerCraft` | 10 | 1–50 | Bolts made at once |

   Tie the upgrade item and the bolt to the switch with `WhiteHiltConfig.AddFeatureGate(id, BallistaSettings.Enabled)`
   (see how Skidbladnir does it), and the Raid Chart to `SeaBattleSettings.Enabled`.

2. `SeaBattles/CrewTable.cs`: `public static IReadOnlyList<CrewEntry> Parse(string text)` returning tiers
   (`Key`, list of `(Prefab, Count, Archer)`), and `public static CrewTier Pick(IReadOnlyList<CrewTier> tiers,
   Func<string, bool> hasKey)`. Tolerant: trims spaces, skips broken parts, never throws.
3. `SeaBattles/RaiderTactics.cs`, pure static logic. Only `Vector2` and `Mathf` from Unity (the tests can use those,
   see `GeometryTests`), no game types, so `Ship.Speed` is not used here:

   ```csharp
   public enum RaiderSail { Stop, Half, Full }
   public readonly struct RaiderInput    // world XZ as Vector2 (x, z); headings in degrees from north
   {
       RaiderState State; Vector2 Self; Vector2 Target; Vector2 TargetVelocity; float TargetHeading;
       float CrewShare; float HullShare; float SecondsInState; int Side; bool HasMelee;
   }
   public readonly struct RaiderOrder { RaiderState State; float Course; RaiderSail Sail; }
   public static RaiderOrder Decide(RaiderInput input, RaiderTuning tuning)
   ```

   Rules: Flee wins when crew or hull share is at or below its setting. Approach → Broadside when within
   `1.5 × BroadsideDistance`; course to the lead point `Target + TargetVelocity × min(distance / 8, 15)`; Full sail.
   Broadside: aim for the point `BroadsideDistance` to the `Side` of the target (perpendicular to `TargetHeading`);
   once within 10 m of that point, steer the target's heading; Half sail, Full when more than `2 × BroadsideDistance`
   away. Broadside → Board after `BroadsideSeconds` if `HasMelee` and `BroadsideSeconds > 0`. Board: the same with
   `BoardDistance × 0.6` instead of `BroadsideDistance`; Half sail. Flee: course away from the target, Full. Prize and
   Moored: Stop, course unchanged. `RaiderTuning` holds the settings as plain floats so the tests need no config.
   `RaiderShip` maps `RaiderSail` to `Ship.Speed`.
4. Translations for everything in section 5 (English in code, Norwegian in the JSON). Config labels for every key.
5. `SagaKind.SeaBattle` and `SagaKind.Raid` in `Saga/SagaLog.cs`, renown settings `RenownSeaBattle` (25) and
   `RenownRaid` (40) in `SagaSettings` (copy `RenownTreasure`), and their cases in `RPC_Deed`. Texts
   `whitehilt_saga_seabattle`/`whitehilt_saga_raid` as above.
6. `TreasureLoot`: add `public static void Roll(Inventory inventory, string list, int rolls)` that uses the existing
   private `Parse` and draw loop; make `Fill` call it. Behaviour of treasures must not change.
7. Tests in `BrudvikWhiteHilt.Tests/SeaBattleTests.cs`: crew table parsing (default text, spaces, broken parts, last
   matching tier wins, archer detection), tactics (each transition, flee wins, lead point, never NaN when distance is
   0). Look at `GeometryTests.cs` for how tests reach `internal` types.

### Phase 2: Ship Ballista and Ship Bolts

1. `Items/ShipUpgrades/ShipBolts.cs` implements `IWhiteHiltCustomItem, IWhiteHiltConfigurable` (copy the structure of
   `WhiteHiltShipUpgradeBase`, without the bit). `CustomItem("WhiteHiltShipBolt", "TurretBoltWood", ItemConfig {
   CraftingStation = ShipwrightBench.StationPrefabName, Amount = BoltsPerCraft, Requirements = Wood 4, Iron 1,
   Feathers 2 })`. In `Add`: `m_ammoType = "$ammo_whitehilt_shipbolt"`, `m_damages.m_pierce = Damage`,
   `m_attack.m_projectileVel = BoltSpeed`, `m_attack.m_projectileAccuracy = Accuracy`; recolour/icon only when not
   headless. `ApplyConfig` sets the same live. `DefaultTier` Swamp.
2. `ShipBallistaUpgrade` in `ShipUpgrades.cs`: `Bit = 9`, raise `WhiteHiltShipUpgradeBase.Count` to 10. Requirements:
   Fine Wood 12, Iron 4, Chain 2, Bog Iron 4, Pine Tar 2 (use `BogIron.PrefabName`, `RoofMaterials.PineTar` like the
   others). `CopyFrom` `"Chain"`. Check every loop over upgrade bits still works with 10 (`Enumerable.Range(0,
   Count)`), and that the ZDO mask is an int (it is).
3. The look. In `WhiteHiltShipUpgradeSetup` (find how the brazier or lantern part is built and made a child that
   `WhiteHiltShipUpgrades` switches with `SetActive(part, Has(bit))`), add a `ballista` part:
   - Find the vanilla ballista prefab *(verify: `piece_turret`)* with `vanilla_prefab.py piece_turret --all-bundles`.
     Its `Turret` component names the turning transforms (`m_turretBody`, `m_turretNeck`, `m_eye`). Copy only the
     MeshFilter/MeshRenderer children (no `ZNetView`, `Piece`, `WearNTear`, `Turret`, colliders) under a new child
     `WhiteHiltBallista`, keeping the body and neck hierarchy so yaw and pitch can turn them, plus an empty `Muzzle`
     at the eye. Scale it down if it is too big for the forecastle (aim for about 2 m long).
   - Place it on the forecastle: on `Start` (every client, not on the prefab), find the ship's front holdfast
     (`attach_dragon` child, verify), take its ship-local position, move 1.5 m toward the stern, and raycast down in
     ship space from 4 m above onto the ship's own colliders (`hit.collider.attachedRigidbody == ship.m_body`) to set
     the height. The same code serves the White Hilt Ship and Skidbladnir. If neither is found, log a warning and
     leave the part inactive.
   - Add a box collider and a child `ShipBallista` component, the way `ShipLanternHover` and `ShipMastHover` give a
     ship part its own hover.
4. `Pieces/Ships/Ballista/ShipBallista.cs` (`MonoBehaviour, Hoverable, Interactable`):
   - Hover: `$whitehilt_ballista_man` with the Use key, plus the bolts the player carries.
   - Interact: if `BallistaSettings.Enabled`, nobody else mans it (scan `Player.GetAllPlayers()` for a player ZDO with
     `whitehilt_ballista_ship` == this ship), attach the player with `Player.AttachStart(attachPoint, null,
     hideWeapons: true, isBed: false, onShip: true, attachAnimation, detachOffset)`. Take the animation and offset
     from the longship's mast holdfast `Chair` *(verify with `vanilla_prefab.py VikingShip --values --all-bundles`)*.
     The attach point is an empty child behind the ballista. Write `whitehilt_ballista_ship` on the player's own ZDO
     (the player owns it).
   - While the local player is attached to this attach point (`player.IsAttached()` and `player.m_attachPoint ==
     attachPoint`): every frame turn the camera's forward (`GameCamera.instance.transform.forward`) into ship-local
     yaw/pitch, clamp with `BallistaAim.Clamp` (tested), pose the ballista, and write the aim to the player's ZDO at
     most 10 times a second. On the attack button (`ZInput.GetButtonDown("Attack")` or `"JoyAttack"`) and reload done:
     take one `WhiteHiltShipBolt` from the inventory or show `msg_whitehilt_ballista_noammo`; fire.
   - Fire exactly like vanilla `Turret.ShootProjectile` (copied in section 10), with the bolt's `ItemData` as `ammo`,
     the local player as `owner` (kills count for the saga, damage obeys PvP), and the vanilla turret's
     `m_shootEffect` copied from the `piece_turret` prefab's `Turret` at registration.
   - On other clients: find the player who mans this ship's ballista and pose it from their ZDO aim, smoothed.
   - When the player detaches (no longer attached here), clear `whitehilt_ballista_ship`.
5. `Patches/SeaBattles/BallistaPatches.cs`: if attacks still start while attached *(check `Humanoid.StartAttack` and
   `Player.SetControls` in the decompile)*, a prefix on `Humanoid.StartAttack` returns false for the local player while
   manning. Keep the prefix cheap: a static `ShipBallista.LocalManned` flag.
6. `BallistaAim.Clamp(float yaw, float pitch, float yawArc, float pitchMin, float pitchMax)` with tests (wrap-around at
   ±180°, arcs at the limits).
7. Docs: a row for the upgrade in the upgrade table of `docs/ships.md`, the bolt below it, and the `[Ships.Ballista]`
   settings table.

In-game check for the user (write it in the log): the ballista stands on the forecastle of both ships, does not block
the walkway, turns with the camera, shoots, uses bolts, hurts a serpent, and others see it turn and the bolt fly.

### Phase 3: the Nail Ship and its crew

1. `SeaBattles/NailShipRegistry.cs`, `Initialize()` from `Awake` (after `KrakenRegistry.Initialize()`), subscribes to
   `PrefabManager.OnVanillaPrefabsAvailable` (unsubscribe in the handler):
   - `PrefabManager.Instance.CreateClonedPrefab("WhiteHilt_NailShip", "VikingShip")`, then
     `PrefabManager.Instance.AddPrefab(new CustomPrefab(clone, false))`.
   - `Piece`: `m_canBeRemoved = false`, `m_resources` empty, so the hammer cannot take it apart for materials; it is in
     no piece table.
   - `WearNTear.m_health = HullHealth`; read its damage modifiers *(verify)* and set pierce to Normal so bolts work.
   - `Ship.m_sailForceFactor *= SailForceFactor`.
   - `ZNetView`: look at what the Kraken prefab does about `m_persistent` and do the same for sea encounters; raid
     ships are created by the server with `Persistent = true` on their ZDO (phase 5).
   - Add `RaiderShip` to the root.
   - Look, only when not headless: hull and sail recoloured dark and grey-green with `VisualHelper.Recolor` (see
     `KrakenRegistry.AddTrophy` and `WhiteHiltShipLook`; use `WhiteHiltShipUpgradeSetup.HullTemplate` to find the hull,
     never the first renderer), shields shown and bleached like bone (see how `WhiteHiltShipLook` shows the
     `Customize` shields). Keep the water mask material untouched (skip materials without `_MainTex`).
2. `Patches/SeaBattles/RaiderShipPatches.cs`:
   - **Transpiler** on `Ship.CustomFixedUpdate`: every `callvirt List<Player>.get_Count` directly after
     `ldfld Ship::m_players` becomes `call RaiderShip.CrewCount(List<Player>)`. Log a warning if none was replaced
     (the game changed). `CrewCount` returns `players.Count`, or 1 when the list is empty and belongs to a sailing
     raider (a static `HashSet<List<Player>>` of `ship.m_players` lists, filled in `RaiderShip.OnEnable` while its AI
     sails and cleared on prize, mooring and `OnDisable`). This keeps vanilla from stopping and braking an empty ship.
     `ShipRoutePaddlePatch` already transpiles the same method; two transpilers are fine.
   - `ShipRoutePaddlePatch.Steered`: also true when `RaiderShip.Sails(ship)`, so the rudder holds.
   - `TailwindPatch`: also true for raiders when `SeaBattleSettings.Tailwind`.
   - Prefix on `ShipControlls.Interact`: refuse with `msg_whitehilt_raider_helm_held` while the raider's crew lives.
   - Prefix on `Container.Interact` for the hold of a raider: refuse with `msg_whitehilt_raider_hold_guarded` while
     the crew lives.
   - Postfix on `MonsterAI.UpdateAI`: return at once when `RaiderCrew.Count == 0`; otherwise let the creature's
     `RaiderCrew` hold its post (below).
   - Postfix on `Character.Awake`: when the ZDO has `whitehilt_raider_ship` or `whitehilt_raider_site`, add
     `RaiderCrew`. (The ZDO is valid in `Awake` for objects created from the network.)
3. `SeaBattles/RaiderDeck.cs`: `public static List<Vector3> Posts(Ship ship, int count)` returns ship-local standing
   places: a grid over the float collider (`ship.m_floatCollider`), 1.2 m apart, raycast down from 5 m above in ship
   space, keep hits on the ship's own colliders with an upward normal, drop points within 1 m of the mast, helm and
   hold, spread the picks over the deck, deterministic order. `public static bool TryDeckPoint(Ship ship, Vector3 near,
   out Vector3 world)` gives the deck point closest to a world point (used by leaps).
4. `SeaBattles/RaiderShip.cs` (`MonoBehaviour`), on every client; the simulation only on the owner:
   - `Awake`: cache `Ship`, `ZNetView`, `ShipAssist`, `WearNTear`, hold `Container`. Static registry `All` for cheap
     checks and `static bool Is(Ship ship)`.
   - Owner, once, 1 s after it appears with `crewsize == 0`: spawn the crew from the stored tier
     (`CrewTable.Pick` result was chosen by the server and stored in `whitehilt_raider_tier`) at `RaiderDeck.Posts`,
     between `CreatureStars.BeginSpawn(false)` and `CreatureStars.EndSpawn()` (see `LindormService`), facing outboard.
     Write each creature's `whitehilt_raider_ship`, `_post`, `_role` and, for melee crew, `_index`; same persistence
     as the ship. Fill the hold with
     `TreasureLoot.Roll(hold.GetInventory(), SeaBattleSettings.Loot.Value, LootRolls)` and, by chance, one
     `WhiteHiltRaidChart`. Then write `crewsize`.
   - Owner, every 0.25 s: read the target ship (`ZNetScene.instance.FindInstance(zdoid)`), count living crew (loaded
     characters whose `RaiderCrew.ShipId` is this ship), build `RaiderInput`, call `RaiderTactics.Decide`, write the
     state, set `ship.m_speed`, and steer with `ShipAssist.SteerTowards(course)`. If `ShipAssist.ShallowAhead()` or
     `Blocked(course, Lookahead())` (ignore the target ship's own colliders), try ±30°, ±60°, ±90° like
     `ShipRoute.FindWayAround` does. When the state changes to Flee, message everyone within 100 m.
   - Owner: when no player is within `DespawnRange` for `DespawnSeconds` (keep `whitehilt_raider_seen`), or the
     target has been unloaded that long, destroy its crew still on board and the ship with
     `ZNetScene.instance.Destroy`. Never despawn while a player stands on it.
   - Owner: crew 0 after having had crew → state Prize, write `whitehilt_raider_prize`, stop, leave the sailing set,
     announce the saga deed once (`SagaLog.Announce(SagaKind.SeaBattle, "$whitehilt_nailship", position)`, set
     `whitehilt_raider_saga`), message everyone within 100 m. After `PrizeMinutes` (world time, so it also counts while
     nobody is near), destroy it through its `WearNTear` *(verify the destroy method and that the hold drops as
     `CargoCrate`)*. If the hull is destroyed earlier (sunk in battle), announce the deed in a `WearNTear.m_onDestroyed`
     handler if not yet done.
   - Every client: hover text on the hull while a prize: `whitehilt_raider_prize_hover` with the minutes left.
5. `SeaBattles/RaiderCrew.cs` (`MonoBehaviour`) on the creature:
   - Static `Count` (for the early return) and `ShipId`, `Post`, `Role`, `SiteId` read once from the ZDO.
   - Called from the `MonsterAI.UpdateAI` postfix, owner of the creature only:
     - On a ship, not boarded: if no player stands on this raider's deck (`ship.m_players.Count == 0`), call
       `ai.StopMoving()` so it keeps its post and fights from there (archers shoot, melee wait). If it is more than
       1.5 m from its post, move its body back there (owner of the creature only). If players are on its deck, melee
       crew may move, but if they leave the float collider's bounds, stop and move back.
     - Boarding: the ship's owner, while in Board and within `BoardDistance` of the target, sets
       `whitehilt_raider_leap` to the next living melee crew member's `whitehilt_raider_index` every
       `BoardIntervalSeconds` (and -1 otherwise). A crew member whose index matches and that has not boarded leaps:
       its owner sets its position to `RaiderDeck.TryDeckPoint(target, ...)`, plays a splash effect and sets
       `whitehilt_raider_boarded`. From then on it is an ordinary creature on the player's ship. Index and turn order
       keep two owners from sending the same creature twice.
     - Shore guard (`SiteId` set, no ship): stop moving when farther than `GuardHoldRadius` from its post and no
       player within it; otherwise vanilla AI.
6. Small exclusions: `ShipAssist` shows no push hint and takes no interaction on raiders; `ManOverboard` ignores
   falls from raiders; `HarbourAnchor` does not list them; `KrakenService.Conditions` returns false on a raider deck;
   add `WhiteHilt_NailShip` to `MapPings.seaMonsters`.
7. Admin test path before phase 4: `whitehilt_seabattle summon` (phase 4 adds the service; for now the command may
   spawn locally when `ZNet.instance.IsServer()` or the player is admin, to test the ship).

In-game check for the user: the Nail Ship sails without anyone aboard, turns, keeps its distance, archers shoot, melee
crew stay aboard and leap when close, the hold is locked while the crew lives, the helm is held, the prize sinks after
the set minutes and drops its hold, nothing is left behind when everyone sails away.

### Phase 4: encounters, boarding leap and saga

1. `SeaBattles/SeaBattleService.cs`, a copy of the `KrakenService` pattern, added in `Game.Start` by
   `Patches/SeaBattles/SeaBattleGamePatches.cs`:
   - Client: every 60 s, if it owns a ship (`Ship.GetLocalShip()`, not a raider) with the local player aboard, the
     feature is on, the key is set, no raider and no Kraken is loaded, the ship is on `Heightmap.Biome.Ocean`, the
     water is deeper than `MinDepth` and no land lies within `MinShoreDistance` (sample `WorldGenerator.GetHeight` on
     16 directions), send `WhiteHiltSeaBattleRequest(shipId, playersAboard)` to the server.
   - Server: checks the feature, the cooldown, the chance, and that the ship's ZDO exists and lies in deep ocean
     (`ZDOMan.instance.GetZDO(id)`, `WorldGenerator`); picks the crew tier from the global keys
     (`ZoneSystem.instance.GetGlobalKey`), then sends `WhiteHiltSeaBattleSpawn(shipId, tier, crewBonus)` to the
     sender. Starts the cooldown only when it sends.
   - Client on spawn order (sender must be the server): pick a point `SpawnDistance` away, ahead-quarter of the
     target's heading, in deep water (`KrakenSpawner.DeepEnough`), at water level; `Instantiate` the Nail Ship facing
     the target; write target, tier, side (random), state Approach, and the crew bonus; play the horn sound used by
     the Dragon Horn's `HornCaller` at the target ship (find the effect it uses); message
     `msg_whitehilt_raider_sighted` to everyone aboard; a temporary map pin is optional.
   - Admin: `WhiteHiltSeaBattleAdmin(shipId)` skips chance and cooldown, checks admin like `KrakenService.IsAdmin`.
2. `SeaBattles/SeaBattleCommands.cs`: `whitehilt_seabattle status` prints the conditions (like `KrakenService.Describe`),
   `summon` asks the server, `raid` (phase 5) creates a raid site near the admin.
3. `SeaBattles/BoardingLeap.cs`, local player only, called from the same place `ManOverboard` ticks (find where
   `ManOverboard` is updated each frame and call next to it):
   - Target: the player stands on a ship (`Player.GetStandingOnShip()` *(verify name)*) and looks at another ship's
     collider within `BoardingLeapRange` (raycast from the camera on the vehicle layer, other rigidbody).
   - Prompt under the crosshair `whitehilt_boarding_leap` with the Use key (add it in the same `Hud.UpdateCrosshair`
     postfix `ManOverboard` uses, or a new one in `BallistaPatches`).
   - On Use: `RaiderDeck.TryDeckPoint(other, hitPoint, out point)`, move the player there (owner of its own body),
     carry the other ship's velocity, short cooldown (1 s).
   - It works between any two ships; that is intended (also to help a friend's ship).
4. Saga: `RPC_Deed` handles the new kinds; renown per deed from the new settings.

In-game check for the user: with `ChancePerMinute 100` and `CooldownMinutes 0` a Nail Ship comes within a minute on
deep sea; in multiplayer the second player sees the same ship and crew, the request goes to the server, the cooldown
holds; boarding leap both ways; saga line for all within witness range.

### Phase 5: raids

1. `WhiteHiltRaidChart` item in `NailShipRegistry` (clone of `LeatherScraps` tinted like the treasure map; see
   `TreasureRegistry.AddMap`). Use it (prefix on `Humanoid.UseItem`, see `Patches/Treasure`) → `RaidService.Request`.
2. `SeaBattles/Raids/RaidService.cs` on `Game`:
   - Client: `TreasureSiteFinder.Find(origin, random, MaxDistance)`, keep candidates with `NearWater`, send them in a
     `ZPackage` (see `TreasureService.RequestSite`).
   - Server: refuse when `MaxActive` sites exist (scan ZDOs of the hoard prefab, like `TreasureService.RefreshMounds`).
     For each candidate (use the existing private checks of `TreasureService` as a model; copy, do not make them
     public unless it is small): search up to `ShipSearchRadius` toward the sea for a point deeper than `MinDepth`.
     Create ZDOs like `TreasureService.TryBury` (prefab hash, `Persistent = true`, rotation, `SetOwnerInternal(0)`):
     the hoard at the candidate, the Nail Ship at the deep point (state Moored, tier from the guards' tier, site id),
     and each guard from `Guards` on a ring of `GuardRadius` (height from `WorldGenerator.GetHeight`, post = its
     position, site id). Reply with the site position and id; the chart is used up only when a site was made.
   - Client on reply: map pin `whitehilt_raid_pin` (`Minimap.instance.AddPin(..., save: true)`), and
     `WaypointTarget.Set(player, position, name)` if waypoints are available to the player; message
     `msg_whitehilt_raid_marked`. On no site: give the chart back with `msg_whitehilt_raid_nosite`.
3. `WhiteHilt_RaidHoard`: clone of a vanilla treasure chest *(verify a Swamp one, e.g. `TreasureChest_swamp`)*, with
   `RaidHoard`. On the owner, once: fill with `TreasureLoot.Roll(..., Raids.Loot, Raids.LootRolls)`, set
   `whitehilt_raid_filled`. Prefix on `Container.Interact` for hoards: refuse with `msg_whitehilt_raid_guarded` while a
   living loaded character with the same site id is within `GuardRadius + GuardHoldRadius + 10`. The first time it
   opens with no guards: invoke `WhiteHiltRaidDone` on the hoard; its owner sets `whitehilt_raid_done` and calls
   `SagaLog.Announce(SagaKind.Raid, "$whitehilt_raid_pin", position)` once.
4. Moored Nail Ship: state Moored, never in the sailing set, physics held like the drift anchor
   (`WhiteHiltShipUpgrades.HoldAnchor` shows how), crew spawned on first load by its owner as at sea, persistent.
   When its crew dies it becomes a prize like any other.
5. `whitehilt_seabattle raid`: admin, creates a site near the player for testing.

In-game check for the user: a chart marks a harbour on a known coast; the guards stand their ground; the hoard opens
only after they die; the moored ship's crew shoots from the deck; leaving and coming back keeps the site; a dedicated
server creates it without anyone near.

### Phase 6: documentation and wrap-up

1. `docs/sea-battles.md`: `# ⚓ Sea battles & raids`, link back to `../README.MD`, sections: The Nail Ship, The fight,
   Boarding and the prize, The Ship Ballista (or a link to ships.md), Raids, Saga, Console, Settings (tables for
   `[SeaBattles]` and `[SeaBattles.Raids]`, every key). Plain English, short sentences.
2. `docs/README.md`: a row under *Sea & travel*. `README.MD`: a feature card (copy an existing card in the same
   table; no image is needed if none exists, or ask the user for an in-game screenshot). `docs/ships.md`: the ballista
   upgrade, bolts and `[Ships.Ballista]`. `docs/saga.md`: the two new deeds and renown settings.
3. `docs/ideas.md`: replace the *Sea battles and Viking raids (planned)* section with a one-line pointer to the new
   page, or remove it.
4. `CHANGELOG.md` under `## Unreleased` → `### Added`: Nail Ships, Ship Ballista and Ship Bolts, boarding leap, prize
   ships, raid charts and draugr harbours, two saga deeds.
5. Update `docs/architecture.md` only if a pattern it describes changed (likely not; the transpiler could get one
   sentence in *Harmony patches* as an example of swapping a field read).

## 7. Multiplayer notes

- The raider ship's owner is whoever the game gives it to (usually the nearest client). It decides everything about
  the ship. Crew creatures are owned separately; their own owner holds their post and leaps. They only read the ship's
  ZDO.
- The aim of a ballista lives on the gunner's own player ZDO, so no ownership of the ship is needed to shoot.
- Projectiles are vanilla networked objects; other clients see them fly.
- The server alone rolls encounters and creates raid sites, and checks every request (feature on, cooldown, ship ZDO
  exists and is at sea, sender's chart request comes with candidates it re-checks).
- Use world time (`ZNet.instance.GetTime()`) for the prize timer, `seen` and leaps.

## 8. Performance notes

- `MonsterAI.UpdateAI` postfix: first line `if (RaiderCrew.Count == 0) return;`.
- `RaiderShip` decides every 0.25 s, not every frame; crew counting uses the static registry, not
  `FindObjectsOfType`.
- `RaiderDeck.Posts` raycasts once per spawn, never per frame.
- Ballista aim writes to the ZDO at most 10 times a second; `ZDO.Set` already skips unchanged values, do not add your
  own compare.
- `CrewCount` is called a few times per ship per physics step: a `HashSet.Contains` on a reference, nothing more.

## 9. Checks after every phase

From the repository root:

```powershell
$msbuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
& $msbuild BrudvikWhiteHilt\BrudvikWhiteHilt.csproj "/t:BeforeBuild;CoreBuild" "/p:SolutionDir=$PWD\" /p:GenerateDocumentationFile=true /v:minimal
dotnet test BrudvikWhiteHilt.Tests
python AssetSource\Tools\cfg_keys.py --check
```

- The build gives no new warnings (CS1591/CS1573/CS1572/CS1574 included).
- All tests pass, including `PatchTests` and `EveryEnglishTextHasANorwegianOne`.
- `cfg_keys.py --check` reports 0 missing.
- `grep` that every new patch class has `[HarmonyPatch]` on the class and every new `*Settings.Initialize()` and
  registry `Initialize()` is called from `BrudvikWhiteHilt.Awake`.
- If the game is running, the deploy copy fails silently; that is fine, the user deploys.

## 10. Reference: how the vanilla ballista fires

From `Turret.ShootProjectile` (Valheim 1.0). Copy this, with the bolt `ItemData` as `m_lastAmmo`, the muzzle as
`transform`, and the player as the first argument of `Setup`:

```csharp
Vector3 forward = transform.forward;
Vector3 axis = Vector3.Cross(forward, Vector3.up);
float accuracy = ammo.m_shared.m_attack.m_projectileAccuracy;
forward = Quaternion.AngleAxis(Random.Range(-accuracy, accuracy), axis) * forward;
forward = Quaternion.AngleAxis(Random.Range(-accuracy, accuracy), Vector3.up) * forward;
GameObject projectile = Object.Instantiate(ammo.m_shared.m_attack.m_attackProjectile, transform.position, transform.rotation);
HitData hit = new()
{
    m_toolTier = (short)ammo.m_shared.m_toolTier,
    m_pushForce = ammo.m_shared.m_attackForce,
    m_backstabBonus = ammo.m_shared.m_backstabBonus,
    m_staggerMultiplier = ammo.m_shared.m_attack.m_staggerMultiplier,
    m_blockable = ammo.m_shared.m_blockable,
    m_dodgeable = ammo.m_shared.m_dodgeable,
    m_skill = ammo.m_shared.m_skillType,
    m_itemWorldLevel = (byte)Game.m_worldLevel
};
hit.m_damage.Add(ammo.GetDamage());
projectile.GetComponent<IProjectile>()?.Setup(player, forward * ammo.m_shared.m_attack.m_projectileVel, hitNoise, hit, null, ammo);
```

`Projectile.Setup(Character owner, Vector3 velocity, float hitNoise, HitData hitData, ItemDrop.ItemData item,
ItemDrop.ItemData ammo)` is the verified signature. Take `hitNoise` from the vanilla turret (`Turret.m_hitNoise`).

Why a ship without players stops in vanilla (`Ship.CustomFixedUpdate`, decompiled): `if (m_players.Count == 0) {
m_speed = Speed.Stop; m_rudderValue = 0f; }`, and later `if (m_players.Count == 0) { vector9.x *= 0.1f; vector9.z *=
0.1f; }`. Both reads are what the transpiler in phase 3 replaces.

Creatures ride on a moving ship by themselves: `Character.ApplyGroundForce` adds
`m_lastGroundBody.GetPointVelocity(...)` for every character, players and creatures alike.

## 11. Log

Write here what was done per phase, what was verified, every deviation from this plan and why, and what the user must
test in the game.

- **Phase 1 (settings, texts and skeleton), 2026-10-13.**
  - Done: `SeaBattles/SeaBattleSettings.cs` (sections `SeaBattles` and `SeaBattles.Raids`, the latter as the nested
    class `SeaBattleSettings.Raids`; `SeaBattleSettings.Tuning()` hands the tactics their numbers),
    `SeaBattles/CrewTable.cs`, `SeaBattles/RaiderTactics.cs` (`RaiderState`, `RaiderSail`, `RaiderInput`,
    `RaiderOrder`, `RaiderTuning`), `Pieces/Ships/Ballista/BallistaSettings.cs` (`Ships.Ballista`; defaults
    `[SeaBattles] Enabled` and `[Ships.Ballista] Enabled` are true), `SagaKind.SeaBattle`/`Raid` with `RenownSeaBattle`
    (25) and `RenownRaid` (40) and their `RPC_Deed` cases, `TreasureLoot.Roll(Inventory, string, int)` (`Fill` calls
    it; treasure behaviour unchanged), the two `Initialize()` calls in `BrudvikWhiteHilt.Awake`, and
    `BrudvikWhiteHilt.Tests/SeaBattleTests.cs`.
  - Texts: every English text of section 5 is registered in `SeaBattleSettings.AddTranslations`, **including the three
    item names and descriptions** (`item_whitehiltshipballista`, `item_whitehiltshipbolt`, `item_whitehiltraidchart`),
    with Norwegian in `Norwegian.json`. Later phases must not register them again. Config labels
    (`whitehilt_cfg_seabattles_*`, `whitehilt_cfg_seabattles_raids_*`, `whitehilt_cfg_ships_ballista_*`, section labels
    `whitehilt_cfgsec_seabattles`, `_seabattles_raids`, `_ships_ballista`) and the saga renown labels are in
    `Norwegian.json`. The Raids keys use their full section in the label key (`..._seabattles_raids_loot`) because
    `Loot` and `LootRolls` exist in both sections with different meanings.
  - Verified: the build gives no new warnings (the remaining CS0419/CS1591 are old, in `NearbyContainers`,
    `RecipeCategories` and `DefenseModelBuilder`); `dotnet test BrudvikWhiteHilt.Tests` 255 passed, 0 failed;
    `cfg_keys.py --check` 0 missing. The default crew and loot prefab names were checked with `vanilla_prefab.py`
    (`Draugr`, `Draugr_Ranged`, `Draugr_Elite`, `Goblin`, `GoblinArcher`, `GoblinBrute`, `Coins`, `Iron`, `Chain`,
    `WitheredBone`, `Amber`, `AmberPearl`, `Ruby`, `TrophyDraugr`); `WhiteHiltShipBolt` and `GiftOfOdin` are mod items.
  - Deviations: (1) `CrewTable.Parse` returns `IReadOnlyList<CrewTier>` (the plan names the return type
    `CrewEntry`, but its text describes tiers); `CrewEntry` is one creature kind, and `CrewTier.Index` is the number the
    ship's ZDO stores in `whitehilt_raider_tier`. `CrewTable.ParseEntries` also reads a plain list for the raid guards.
    (2) `RaiderInput` has an extra `SelfHeading`, so Prize and Moored can keep the course ("course unchanged") and a
    zero distance has a course to fall back on. (3) `RaiderTactics.Decide` returns the course and sail of the state it
    moves to in the same call (Approach to Broadside gives a broadside course at once). `SecondsInState` is reset by
    `RaiderShip` when the returned state differs. Flee is permanent once entered. (4) The Saga renown settings are
    bound through `SagaSettings.BindRenown`, which `cfg_keys.py` cannot see; their Norwegian labels exist all the same.
  - For phase 2: `BallistaSettings` has no feature gate yet; call
    `WhiteHiltConfig.AddFeatureGate(id, BallistaSettings.Enabled)` for the upgrade item and the bolt, and
    `SeaBattleSettings.Enabled` for the Raid Chart. `SeaBattleSettings.DefaultLoot` already names `WhiteHiltShipBolt`,
    and `TreasureLoot.Roll` skips unknown items with a warning until the bolt exists.

- **Phase 2 (Ship Ballista and Ship Bolts), 2026-10-13.**
  - Done: `Items/ShipUpgrades/ShipBolts.cs` (clone of `TurretBoltWood`, ammo type `$ammo_whitehilt_shipbolt`, Shipwright's
    Bench, Wood 4 / Iron 1 / Feathers 2, `ApplyConfig` sets pierce, speed, accuracy and the recipe amount live, steel
    tint and icon only when not headless), `ShipBallistaUpgrade` in `ShipUpgrades.cs` (bit 9, constant `ItemName`) and
    `WhiteHiltShipUpgradeBase.Count` 9 to 10, `Pieces/Ships/Ballista/BallistaModel.cs` (the part on the prefab, called
    from `WhiteHiltShipUpgradeSetup.Prepare`), `Pieces/Ships/Ballista/ShipBallista.cs`, `BallistaAim.cs`,
    `Patches/SeaBattles/BallistaPatches.cs`, the part switched in `WhiteHiltShipUpgrades.Update` with the upgrade bit,
    `WhiteHiltShipUpgradeSetup.RootBounds` and `CrateLayer` made `internal`, `ShipBallista.ObjectName` added to
    Skidbladnir's `UpgradeObjects` (without it `SkidbladnirModel.Build` switches off every renderer and collider of the
    part), the feature gates in `BallistaSettings.Initialize`, `BallistaAimTests` (37 new tests), `docs/ships.md` (upgrade
    row, *The Ship Ballista*, the bolt, `[Ships.Ballista]` table) and an entry under `## Unreleased` in `CHANGELOG.md`
    (there was no such heading yet).
  - Looks: `BallistaModel` copies the `Base`, `Neck`, `Body` and `Bolt Black Metal` meshes and materials (`Turret_mat`)
    and the `NeckRotation`, `BodyRotation` and `Eye` pivots of `piece_turret/New`, under `WhiteHiltBallista/Visual`. It
    measures the copy and scales it so the longest horizontal side is 2 m, stands it on y 0, and sizes the hover box
    and the gunner's point (0.6 m behind the box) from the measured bounds. The part is inactive until the ship has
    the upgrade. `ShipBallista.Start` places it on every client by a ray from 4 m above the ship's front holdfast,
    1.5 m towards the stern, onto colliders whose rigidbody is the ship's (own colliders skipped, hit normal within 60
    degrees of the ship's up, deck within 3 m of the holdfast height); on failure it logs a warning and hides itself.
    A dedicated server builds no meshes and the component disables itself in `Awake`.
  - Verified vanilla facts (`vanilla_prefab.py ... --all-bundles --values`, decompile): `piece_turret` has
    `New/Base`, `New/NeckRotation/Neck`, `New/BodyRotation/{Body, Body_Unarmed (inactive), Bolt Black Metal, Bolt Wood,
    Bolt Flametal, Bolt Bloodgold, Mag (inactive), Eye}`; `BodyRotation` sits at y 1.744, `Eye` at (0, 0.305, 1.78) in its
    space, all meshes use `Turret_mat`; `Turret.m_shootEffect` exists and `m_hitNoise` is 10. The ballista turns by setting
    `BodyRotation` to yaw and pitch and `NeckRotation` to yaw only. `VikingShip` has **`interactive/front`** (a `Chair`,
    `m_attachAnimation attach_dragon`, not `m_inShip`, ship-local (0, 2.416, 9.959), its attach point 1.44 behind it in its
    own scaled space) and `interactive/mast` (`attach_mast`, `m_inShip`); `attach_dragon` is the animation name, the object
    is `front`. Skidbladnir moves the same `front` object to (0, 3.3, 10.75) + waterline offset, so one placement serves
    both. `TurretBoltWood` is item type 23 (`AmmoNonEquipable`, which `Inventory.GetAmmoItem` accepts). `Turret.ShootProjectile`
    also sets `m_statusEffectHash` and `m_hitType = HitType.Turret`; both are copied.
  - Check results: build with `GenerateDocumentationFile`: exit 0, no new warnings (the old CS0419/CS1591 in
    `NearbyContainers`, `RecipeCategories` and `DefenseModelBuilder` remain); `dotnet test BrudvikWhiteHilt.Tests`: 292
    passed, 0 failed (255 before); `cfg_keys.py --check`: 0 missing. Not tested in the game.
  - Deviations: (1) **No `Humanoid.StartAttack` prefix; a `Player.SetControls` prefix instead.** Vanilla
    `SetControls` clears `attack` and calls `AttachStop()` for an attached player who presses attack, so the gunner
    would be thrown off at once. `BallistaPatches.SetControls` takes the attack (and the hold) for the ballista while
    `ShipBallista.LocalManned`, which also keeps `StartAttack` from ever running, so it needs no prefix. This also means
    the fire request comes from the controls, not from `ZInput.GetButtonDown("Attack")`, and covers the gamepad.
    The secondary attack, move, jump, crouch and block still dismount, as in vanilla. (2) The gunner uses the **front**
    holdfast's animation `attach_dragon` (not the mast's `attach_mast`), `onShip: true`, detach offset zero, since the
    gunner stands at the bow. (3) A new ZDO key on the gunner's player, `whitehilt_ballista_shot` (float, world seconds), so
    every client knows when the ballista is loaded again; reload is checked against the world clock. (4) The shot
    effect and hit noise are read from the vanilla `piece_turret` through `ZNetScene` on the first shot instead of at
    registration. (5) `AttachStart(hideWeapons: true)` leaves the weapons put away until the player presses the hide key,
    so `ShipBallista.Release` draws them again if they were drawn when the player mounted. (6) The ballista code lives in
    `BallistaModel` (new file) and is called from `Prepare`, rather than inside `WhiteHiltShipUpgradeSetup`, which is
    already long. (7) The duplicate English texts for the ballista and the bolt are removed from
    `SeaBattleSettings.AddTranslations`; the item classes register them (identical text); the Norwegian ones are
    unchanged. (8) The Ship Ballista item clones `Chain`, as the plan says, so its icon looks like the Drift Anchor's.
    (9) The bolt flies parallel to the camera's forward, not towards what the crosshair hits; the camera sits some metres
    behind the ballista, so there is a small parallax at short range. (10) `ShipBallista.Interact` has no distance check of
    its own; vanilla's interact distance applies.
  - In-game checklist for the user: install the upgrade on the White Hilt Ship and on Skidbladnir and look at the
    forecastle: the ballista stands on the deck behind the bow holdfast, upright, about 2 m long, and does not block the
    walkway or stand inside the rail; it vanishes when the upgrade is taken off (also while someone is manning it: the
    gunner is let go). Use it: you stand behind it holding on, with the weapons put away; the hover text shows the bolts
    you carry. Move the mouse: the ballista turns with the camera, stops at `YawArc` and the pitch limits, and the
    camera is not disturbed. Press attack: one bolt is taken, the shot effect plays, the bolt flies and the loaded bolt
    on the ballista disappears for `ReloadSeconds`; with no bolts you get *No Ship Bolts*. You are not thrown off by
    the shot. Walk, jump or crouch: you step off and your weapons are drawn again. Check that the bolt does not hit
    the ship's own bow or dragon head when aiming low, and that it hurts a serpent and the Kraken. With a second client:
    it sees the ballista turn, the shot and the bolt fly; a second player is told *blocked* while you man it; the bolts
    are made at the Shipwright's Bench (10 at a time) and the Mistlands turret does not accept them. Turn
    `[Ships.Ballista] Enabled` off: the recipes go and nobody can man it.
  - For phase 3: `ShipBallista.ObjectName` and `WhiteHiltShipUpgradeSetup.CrateLayer`/`RootBounds` are usable for other
    deck parts; any new part under the ship root that must survive Skidbladnir needs its name in
    `SkidbladnirModel.UpgradeObjects`. The Nail Ship is a `VikingShip` clone, so it also gets `WhiteHiltShipUpgrades` only
    if you add it; `ShipBallista` only works through that component's mask. `BallistaPatches` already patches
    `Player.Update` (postfix, local player only) and `Player.SetControls`; a boarding-leap prompt can go beside them.
    A bolt that hits a ship damages its `WearNTear` like any projectile.

- **Phase 3 (the Nail Ship and its crew), 2026-10-13.**
  - Done: `SeaBattles/NailShipRegistry.cs` (clone of `VikingShip` as `WhiteHilt_NailShip`: `Piece` not removable and
    without resources, hull health from `HullHealth` with pierce set to Normal, sail force factor, `RaiderShip` added,
    registered from `Awake` after the Kraken), `NailShipLook.cs` (dark hull and sail, bone-white shields; only when not
    headless, each part in its own try, hull found through `WhiteHiltShipUpgradeSetup.HullTemplate`), `RaiderKeys.cs`
    (all ZDO keys), `RaiderDeck.cs` (deck probing plus the pure `Spread`, `RemoveNear`, `Nearest`, `OuterIndexes`,
    `InsideDeck`), `SailingShips.cs` (the set the transpiled physics reads), `RaiderShip.cs` (owner AI every 0.25 s, crew
    and hold spawn, prize, sinking, despawn, leap turns, hover text, `Spawn`, `Describe`), `RaiderCrew.cs` (hold post,
    leap, shore guard, orphan removal), `Patches/SeaBattles/RaiderShipPatches.cs` (transpiler on `Ship.CustomFixedUpdate`,
    `Ship.GetSailForce` prefix for the tailwind, `ShipControlls.Interact` and `Container.Interact` locks,
    `MonsterAI.UpdateAI` postfix, `Character.Awake` postfix), `SeaBattles/SeaBattleCommands.cs`
    (`whitehilt_seabattle [status|summon]`, registered from `Awake`), `ShipRoutePaddlePatch.Steered` (true while a Nail
    Ship sails), and the small exclusions: `ShipAssist` (no push, hover text from the raider; new optional `ignore`
    parameter on `Blocked`), `ManOverboard`, `PortalMapService.FindPrefabs` (the Harbour Anchor), `ShipMooring.Nearest`,
    `KrakenService.Conditions`, `MapPings` (Lookout) and `NearbyContainers.CollectUsable` (the hold cannot be used
    for crafting while the crew lives). Tests: `BrudvikWhiteHilt.Tests/RaiderDeckTests.cs` (25 new tests); the test
    project got references to `0Harmony` and `Mono.Cecil`.
  - Verified (`vanilla_prefab.py --all-bundles --values` and the decompile): `VikingShip` ZNetView `m_persistent` 1;
    `WearNTear` health 1000 and damage modifiers pierce Resistant, chop/pickaxe/poison/spirit Immune, fire Weak, frost
    Resistant (so pierce is set to Normal); its `Container` is 6x3 with `m_destroyedLootPrefab` `CargoCrate` and no
    default items; `WearNTear.Remove(bool blockDrop)` is public, runs `RPC_Remove` on the owner, then the private
    `Destroy`, which calls `m_onDestroyed` (the container then drops its items as a crate, on the owner) and
    `ZNetScene.Destroy`. `Ship.CustomFixedUpdate` reads `m_players.Count` exactly twice (a test reads the game's IL with
    Cecil and fails if that changes). `Ship.m_players` is private. `EnvMan.IsWindControllActive` is only read for
    `Ship.GetLocalShip()` (EnvMan.cs 1255). Crew prefabs `Draugr`, `Draugr_Ranged`, `Draugr_Elite`, `Goblin`,
    `GoblinArcher`, `GoblinBrute` exist and are persistent. In .NET Framework, the game's `Ship` and `Player` types cannot be
    loaded in the test project (`TypeLoadException`), so the tests use stand-in types and Cecil.
  - Check results: build with `GenerateDocumentationFile`: exit 0, only the old warnings (CS0419/CS1591 in
    `NearbyContainers`, `RecipeCategories`, `DefenseModelBuilder`); `dotnet test BrudvikWhiteHilt.Tests`: 317 passed, 0
    failed (292 before); `cfg_keys.py --check`: 0 missing (no new setting). Not tested in the game.
  - Deviations: (1) **Persistence.** The plan says to do what the Kraken does, but the Kraken's base `TentaRoot` is
    persistent (`m_persistent` 1) and only avoids leaking because the Kraken removes itself. A Nail Ship at sea would
    stay in the saved world, so the prefab gets `ZNetView.m_persistent = false`, and every crew member gets
    `ZDO.Persistent` from its ship's ZDO. Phase 5 makes the raid ships and their crew with `Persistent = true` on the
    ZDO. (2) **Tailwind.** `TailwindPatch` was not extended: `IsWindControllActive` only turns the wind of the local
    player's ship, so it cannot give another ship a tailwind, and on a prize the player sails it would turn the wind
    for them. Instead a prefix on `Ship.GetSailForce` returns the force along the heading (`RaiderShip.TailwindForce`)
    for a Nail Ship under sail when `[SeaBattles] Tailwind` is on. (3) **Lookout.** `MapPings.seaMonsters` lists
    character prefab names and a ship is not a character, so it was left alone; the Lookout's ship loop shows a Nail
    Ship with the monster icon and the name Nail Ship. (4) **Two ZDO keys more than the table in section 5:**
    `whitehilt_raider_alive` (crew counted at the owner's last tick, so a new owner that has not met the crew yet does
    not take the ship for a prize; it trusts it for 10 s) and `whitehilt_raider_bonus` (extra melee crew, written by
    whoever sends the ship). (5) The Nail Ship's hover text is not a `Hoverable` of its own: `ShipAssist` is the ship's
    hover and delegates to `RaiderShip.HoverText()`. (6) The set of sailing player lists is `SailingShips` (not a
    field of `RaiderShip`) and holds `object`, so `CrewCount` can be tested without loading `Ship`/`Player`. The patch's
    transpiler logic is `RaiderShipPatches.ReplaceCount`, which takes the members as parameters for the same reason.
    (7) Extra exclusions beyond the plan: `ShipMooring.Nearest` (a mooring post would grab a raider) and
    `NearbyContainers` (crafting from the hold). (8) A warrior leaps when his distance to the nearest deck point of
    the target is within `BoardDistance` (the plan's wording left the measure open), and the splash is the Nail Ship's
    own `m_waterImpactEffect`, since no splash prefab name was verified. (9) Sea ships and crew are not despawned by
    range in the Prize and Moored states. (10) A Nail Ship whose crew cannot be spawned (no tier, unknown prefabs, no
    place on the deck) is removed with a warning instead of sailing without a crew. (11) A shore guard that has strayed
    beyond `GuardHoldRadius` walks back to its post (the setting text says "turns back") unless a player is within that
    radius of the post. (12) `RaiderCrew.Attach` is also called by hand after a crew member is spawned, because
    `Character.Awake` ran before its ZDO was filled in. (13) The prize sinks through `WearNTear.Remove(true)`; a
    sea prize is non-persistent, so it also disappears with its hold if everyone sails out of range before the minutes
    are out.
  - In-game checklist for the user: with admin rights, board a ship in deep water (6 m or more under a point 180 m
    away) and run `whitehilt_seabattle summon` (a world without Bonemass slain uses the first tier and says so):
    a dark longship appears 180 m off in the forward quarter, facing you, with the tier's crew standing spread over
    its deck (archers along the rail), and sails towards you with nobody at the helm. It turns, keeps clear of rocks,
    comes up alongside at `BroadsideDistance`, its archers shoot, and after `BroadsideSeconds` it closes in; then one
    warrior leaps over every `BoardIntervalSeconds`, in number order, and fights as an ordinary draugr. Its helm and its
    hold answer *The dead still hold the helm* and *The crew guards the hold* while crew lives, and the hold cannot be
    used for crafting. Kill the crew (or wound the hull with arrows or ballista bolts; pierce hurts it): below a quarter
    of the crew or a fifth of the hull it turns and runs and sends a message, and stays fleeing. With the crew dead it
    stops, everyone within 100 m gets the prize message, the saga line appears once, the hover text on the hull shows
    the minutes left, the helm and hold open, and after `PrizeMinutes` it sinks and the hold drops as a cargo crate.
    Sail away from a live Nail Ship: after `DespawnSeconds` out of `DespawnRange` it and its crew are gone. Check
    that `whitehilt_seabattle status` lists it, that it does not show on a Harbour Anchor map or catch a mooring
    post, and that nothing is left in the world after a reload. In multiplayer, join a second client: the ship,
    crew, locks and messages must be the same, and when the first client leaves the area the new owner must carry on
    (a few seconds of grace before the crew count). Known limits: the Raid Chart is skipped until phase 5; a sail
    force that is too strong or weak is tuned with `SailForceFactor` (applies after a restart).
  - For phase 4: `RaiderShip.Spawn(Ship target, int crewBonus, CrewTier tier = null)` is the client-side spawn (point
    `SpawnDistance` away in the forward quarter, deep water, ZDO target/tier/side/bonus/state); the service should call it
    on the order of the server, with a `tier` from `CrewTable.Pick` (null picks the tier the world has reached; the
    admin command falls back to the first tier on a world with no key set), and add the horn and
    `msg_whitehilt_raider_sighted` (already registered, not yet sent) there. `whitehilt_seabattle summon`
    in `SeaBattleCommands.Summon()` currently spawns locally for the server or an admin; replace its body with the
    `WhiteHiltSeaBattleAdmin` request. `RaiderShip.Loaded` is the "no raider loaded" check. `RaiderShip.Is/Of/Sails/
    CrewHolds` and `RaiderCrew.All` are the lookups; `RaiderDeck.TryDeckPoint(ship, near, out world)` serves the
    player's leap. Anything on a raider's deck that must not run there (`Kraken`, man overboard, ...) already checks
    `RaiderShip.Is`. The Nail Ship's ZNetView is non-persistent; keep it so for sea encounters.

- **Phase 4 (encounters, boarding leap and saga), 2026-10-13.**
  - Done: `SeaBattles/EncounterRules.cs` (pure: `AttackChance`, `Rolled`, `CooldownOver`, `CrewBonus`, `NearestLand` and
    `ShoreIsClear` from a height delegate, `Offsets` and `TryPickSpawn` for the spawn point, `SmoothedVelocity`),
    `SeaBattles/SeaBattleService.cs` (on `Game`; RPCs `WhiteHiltSeaBattleRequest`, `...Spawn`, `...Admin`, `...Reply` and
    one more, `...Sighted`), `Patches/SeaBattles/SeaBattleGamePatches.cs` (class-level `[HarmonyPatch]`, `Game.Start` adds
    the service), `SeaBattleCommands` rewritten (`status` prints `SeaBattleService.Describe()` plus `RaiderShip.Describe()`,
    `summon` calls `SeaBattleService.Summon()`, so it is the admin RPC now), `SeaBattles/BoardingLeap.cs` (called from the
    `Player.Update` postfix in `ShipAssistPatches` after `ManOverboard.Tick`, prompt from the `Hud.UpdateCrosshair`
    postfix in the same class), `HornCaller.PlayAt(position, range)` (new, plays the Dragon Horn clip once at a place),
    `RaiderShip.Spawn` now picks its point with `EncounterRules.TryPickSpawn`. Tests: `EncounterRulesTests` in
    `SeaBattleTests.cs` (37 new).
  - Flow: the client that owns a ship with the local player aboard asks every 60 s (feature on, key set, not a raider, no
    raider or Kraken loaded, chance above 0, ocean biome, depth over `MinDepth`, no land within `MinShoreDistance`). The
    server checks the same things again from the world generator, that the sender is a real connection
    (`RoutedRpcSender.Verify`), that the ship ZDO exists, has a `Ship` prefab that is not the Nail Ship and that the
    sender's player ZDO is within 60 m of it, counts the players aboard from the player ZDOs within 60 m (the client's
    number only lowers it), checks the cooldown (real time, `Time.time`) and the chance, picks the tier with
    `CrewTable.Pick` and sends `Spawn(shipId, tierIndex, crewBonus)`. The cooldown starts only when the order is sent.
    The client accepts it only from the server, and only for a ship it owns, that is no Nail Ship, with none loaded; it
    calls `RaiderShip.Spawn`, and on success sends `Sighted(shipId)` to everybody, on which each client plays the horn at
    the hunted ship (range `2 x SpawnDistance`) and shows `msg_whitehilt_raider_sighted` when its own ship is that one.
    The admin RPC checks the sender's identity from the connection (`RoutedRpcSender.Verify` then `ZNet.IsAdmin`, the host
    is an admin), skips chance, cooldown and the ocean test, and uses the first tier when no crew key is set.
  - Saga: nothing was missing. Phase 1 already had the `SagaKind` values, the renown settings, the `RPC_Deed` cases and
    the Norwegian texts; `RaiderShip` announces a deed when the ship becomes a prize or is sunk in battle, once
    (`whitehilt_raider_saga`). The line reads "A Nail Ship was taken: Nail Ship" because of the format in the plan.
  - Verified: `Player.GetStandingOnShip()` exists (`InNumShipVolumes > 0`, on ground, `m_lastGroundBody` is a `Ship`);
    `Player.TakeInput()` and `GameCamera.m_camera` are reachable through the publicized assemblies (other code uses
    them); `ZNet.PlayerInfo.m_characterID`, `ZNet.GetPlayerList()`, `ZRoutedRpc.m_id` and `ZNet.IsAdmin(string)` exist; the
    Dragon Horn sound is not an effect prefab but an `AudioClip` (`HornCaller.dragonClip`) played on an `AudioSource`, so
    it is played the same way. The `RoutedRpcSender` patch is on `ZRoutedRpc.RPC_RoutedRPC`, so it covers every routed RPC.
  - Check results: build with `GenerateDocumentationFile`: exit 0, no warnings at all; `dotnet test
    BrudvikWhiteHilt.Tests`: 354 passed, 0 failed (317 before); `cfg_keys.py --check`: 0 missing (no new setting). Not
    tested in the game.
  - Deviations: (1) **Depth from the world generator.** The client condition, the server check and the spawn point use
    `WorldGenerator.GetHeight` (`SeaBattleService.DeepWater`) instead of `KrakenSpawner.DeepEnough`, which raycasts at the
    seabed: that finds nothing where no zone is loaded (180 m out) and cannot run on the server. The spawn point needs
    6 m of water, as before. (2) **One more RPC, `WhiteHiltSeaBattleSighted`.** `RaiderShip`'s own message RPC reaches only
    those within 100 m of the raider, which spawns 180 m away, so the "everyone aboard" message and the horn go through a
    routed RPC to everybody; each client acts only when the ship is loaded, and at most every 5 s. A temporary map pin was
    left out (it was optional). (3) **The server also checks the shore and the number aboard,** not only deep ocean as the
    plan says, because a modified client could claim both. (4) **The admin summon starts the cooldown** (as the Kraken's
    does) but ignores it. (5) **`ShipAssist.CanPush` is false while the player stands on a ship,** otherwise Use at another
    ship's hull would push it as well as leap onto it. `ManOverboard.UpdateCrosshair` now returns whether it showed its
    prompt, and the leap prompt is shown only when it did not. (6) The leap prompt is added under the hover text of the
    ship when there is one (a Nail Ship shows its name and prize time), and it needs the crosshair to point at nothing or
    at the other ship's root, so a helm, hold or door in range keeps the Use key. (7) A leap uses the other ship's physics
    velocity where this machine owns it and a measured velocity (smoothed from its movement, `EncounterRules.SmoothedVelocity`)
    where another machine does, as a ship owned elsewhere has no real velocity here. It also plays the player's jump
    effect and animation trigger. (8) The leap works only while `[SeaBattles] Enabled` is on and uses the existing
    `BoardingLeapRange`; the 1 s cooldown and the 0.1 s aim interval are constants. (9) The shore check samples rings every
    25 m in 16 directions, so an island smaller than the gap between two directions at the limit can slip through.
  - In-game checklist for the user: (a) Admin, on a ship in deep sea (`defeated_bonemass` set, or not): `whitehilt_seabattle
    status` lists every condition (ocean, depth, nearest land, key, chance); `whitehilt_seabattle summon` brings a Nail
    Ship in the forward quarter, the horn sounds, those aboard see *Sails on the horizon!*, and a non-admin gets *Only
    admins can do that.* in the console. (b) With `ChancePerMinute 100` and `CooldownMinutes 0`, a Nail Ship comes within
    a minute of sailing in deep water with land more than `MinShoreDistance` away, and not near a coast, in shallow
    water, with a Kraken or Nail Ship loaded, or when you are not aboard. (c) With the default cooldown, a second ship
    does not come inside 45 minutes anywhere in the world (check with two players on different ships). (d) In multiplayer
    the second player sees the same ship and crew, and both aboard the target get the message; the request shows in the
    server log as *Nail Ship: sending one after ...*. (e) Standing on a ship, look at another ship within
    `BoardingLeapRange`: *[E] Leap aboard* shows under the crosshair, Use moves you to its deck at the point nearest to
    where you looked, without sliding off while it moves (try a moving ship you do not own), then nothing for 1 s; it
    works both ways, onto a Nail Ship and from a Nail Ship you have taken, and pointing at its helm or hold does not
    leap. (f) Take or sink a Nail Ship near a friend: both get the saga line and renown once.
  - For phase 5: `SeaBattleService.DeepWater(x, z, depth)` is the world-generator depth test (works on the server);
    `EncounterRules.NearestLand`/`TryPickSpawn` are reusable for the harbour's ship point. The server-side validation
    pattern (verify sender, player ZDO near, `TargetShip`-like ZDO lookups) is in `SeaBattleService`; copy it for
    `WhiteHiltRaidRequest`. `RoutedRpcSender.Verify` returns null for an unknown connection, so a raid request must
    drop on null. `HornCaller.PlayAt` can sound the horn when a raid ship wakes. The raid ship is moored and persistent,
    so it must not go through `SeaBattleService`; `RaiderShip.Spawn` is for sea encounters only (it picks a point at sea
    and leaves the ZDO non-persistent). Protocol: five new routed RPCs from this phase on top of earlier ones; a MINOR
    bump at release.

- **Phase 5 (raids), 2026-10-13.**
  - Done: `SeaBattles/Raids/RaidSites.cs` (pure geometry: `GuardPosts`, `TryFindShipPoint`, `ShipHeading`, `CoastPoints`),
    `SeaBattles/Raids/RaidService.cs` (on `Game`; RPCs `WhiteHiltRaidRequest`, `WhiteHiltRaidAdmin`, `WhiteHiltRaidReply`),
    `SeaBattles/Raids/RaidHoard.cs` (object RPC `WhiteHiltRaidDone`), `Patches/SeaBattles/RaidPatches.cs` (class-level
    `[HarmonyPatch]`: `Humanoid.UseItem` prefix for the chart, `Container.Interact` prefix for the hoard),
    `NailShipRegistry` (the `WhiteHiltRaidChart` item and the `WhiteHilt_RaidHoard` prefab, each in its own try),
    `RaiderShip` (moored hold: `RefreshMooring`, `HoldMoored`, `Hunting`), a `Ship.CustomFixedUpdate` postfix in
    `RaiderShipPatches`, `whitehilt_seabattle raid` in `SeaBattleCommands`, `RaiderCrew.SiteId`, the service added in
    `SeaBattleGamePatches`, `SeaBattleService.PlayerZdo`/`IsAdmin` and `TreasureService.IsNearBuilding` made `internal`
    (a one-word change each), `TreasureSiteFinder.Find` got an optional `minDistance`, the chart gate in
    `SeaBattleSettings.Initialize`, `RaidSitesTests` (21 new tests in `RaidSiteTests.cs`). No new settings.
  - Flow. Chart: the client runs `TreasureSiteFinder.Find(origin, random, Raids.MaxDistance, Raids.MinDistance)`, keeps
    up to 16 candidates with `NearWater` and sends their positions. The server drops the request unless the sender is a
    real connection (`RoutedRpcSender.Verify`) with a living player ZDO (`SeaBattleService.PlayerZdo`), reads the
    candidates in a try (a broken package gives none), keeps those within `MinDistance - 25` and `max(MinDistance + 50,
    MaxDistance) + 25` of that player ZDO, refuses when `MaxActive` unsacked hoards exist, and then takes the first
    candidate that has dry land at least 1.5 m above the sea in a generated zone, is `TreasureSettings.MinTreasureDistance`
    from other harbours, has deep water for the ship (`RaidSites.TryFindShipPoint`: nearest ring of 10 m steps up to
    `ShipSearchRadius`, 16 bearings, at least 6 m of water by the world generator and no land within 15 m, the deepest
    place on the ring) and is clear of locations, buildings (`TreasureSettings.MinBaseDistance`) and any object within
    2.5 m. It creates persistent ZDOs with `SetOwnerInternal(0)`: the hoard, the Nail Ship (state Moored, crew tier chosen
    like a sea encounter, the first tier when no key is set, heading along the shore), and the guards from
    `Raids.Guards` on a ring (`GuardRadius`, wet posts pulled in towards the hoard), each with site id, post (world),
    role. The reply carries result, whether it came from a chart, a note, and the hoard position; the client marks a
    pin (`Icon3`, `save: true`), sets the waypoint target when the Ruby Pathfinder's Amulet is worn (the target arrow is
    that amulet's), uses up one chart (found by prefab name, only on success) and shows `msg_whitehilt_raid_marked`.
    A client that gets `NoPlace`/`Off` shows `msg_whitehilt_raid_nosite`, `TooMany` the new `msg_whitehilt_raid_toomany`.
    The client also shows `nosite` at once when it finds no candidate or the feature is off, and does not ask again for
    15 s while an answer is awaited.
  - Hoard: clone of `TreasureChest_swamp` (verified with `vanilla_prefab.py`: ZNetView persistent, Container 4x2 public,
    Piece, WearNTear; it comes with default items, which are cleared). `m_canBeRemoved` false, and the `WearNTear` is
    **removed**, otherwise a player could break the chest open and get the loot without killing the guards.
    `RaidHoard` (owner, once a second, since a server-made ZDO has no owner when it appears): fills it once with
    `TreasureLoot.Roll(..., Raids.Loot, Raids.LootRolls)` and sets `whitehilt_raid_filled`; when done and emptied and not
    in use it removes itself with `ZNetScene.Destroy`. The `Container.Interact` prefix refuses with
    `msg_whitehilt_raid_guarded` while a living, loaded **shore guard** (no ship id) with the hoard's site id is within
    `GuardRadius + GuardHoldRadius + 10` of the chest; otherwise it lets the open through and sends `WhiteHiltRaidDone`.
    The owner sets `whitehilt_raid_done` and announces `SagaKind.Raid` once, and checks for guards on its own machine too.
  - Moored ship: state Moored is never in `SailingShips`; the Think loop skips the despawn rule for it; every machine
    freezes its X/Z position and Y rotation (`RefreshMooring`, as the drift anchor does) and the owner zeroes its horizontal
    velocity after each `Ship.CustomFixedUpdate` (`HoldMoored`, a cheap postfix that returns when no Nail Ship is loaded).
    Its crew is spawned on first load by its owner through the sea path (`SpawnCrew`), with the ship's `Persistent` and
    site id copied to each creature (verified in `SpawnMember`), and its hold is filled the same way (it can also carry a
    chart). When the crew dies it becomes a prize like any other and is released; it sinks after `PrizeMinutes`.
    `RaiderShip.Hunting` (loaded Nail Ships that are not moored) replaces `Loaded` in the sea encounter service, so a
    harbour's ship does not block encounters; `Loaded` still drives the physics postfix and the status text.
  - Guards: `RaiderCrew.Attach` already takes any creature with a site id; `HoldShore` turns a guard that has walked
    farther than `GuardHoldRadius` from its world post back, unless a player stands within that radius of the post.
    Checked in the code against the ZDOs the service writes (site, post, role; no ship id).
  - Check results: build with `GenerateDocumentationFile`: exit 0, only the old warnings (CS0419/CS1591 in
    `NearbyContainers`, `RecipeCategories`, `DefenseModelBuilder`); `dotnet test BrudvikWhiteHilt.Tests`: 375 passed, 0
    failed (354 before); `cfg_keys.py --check`: 0 missing (no new setting). Not tested in the game.
  - Deviations: (1) **The chart is used up on the reply, not taken out and given back**: the plan says "give chart back on
    failure"; leaving the chart in the inventory until the server has made a site is the same behaviour with less to
    undo. (2) **New texts:** `whitehilt_raid_hoard` (the chest's name, "Draugr hoard") and `msg_whitehilt_raid_toomany`;
    both have Norwegian. A chart used while `[SeaBattles] Enabled` is off shows `msg_whitehilt_raid_nosite`. (3) **New
    RPC `WhiteHiltRaidAdmin`** (the plan lists the request and the reply only). The admin reply goes through
    `WhiteHiltRaidReply` with `chart = false` and a console note. (4) **No new settings:** harbour spacing and building
    clearance reuse `[Treasure] MinTreasureDistance` and `MinBaseDistance`; the ship's water depth (6 m), its shore
    clearance (15 m) and the 2.5 m hoard clearance are constants in `RaidSites` and `RaidService`, as the sea spawn's 6 m
    is. The ship's tier is not "the guards' tier": the guards list has no tiers, so the ship takes the tier the world
    has reached. (5) **Admin raid** finds its coast on the server (`RaidSites.CoastPoints`: low land within 40 m of
    water, rings of 25 m from 50 m out to `Raids.MaxDistance`, the nearest 200 points) and needs nothing explored; it
    respects `MaxActive` and says so in the console. (6) The hoard has no hull (see above) and is removed when sacked and
    empty; a **sacked harbour does not count** against `MaxActive` (the limit counts hoards without `whitehilt_raid_done`).
    The moored ship and its crew stay in the world until players deal with them. (7) The guards are plain ZDO-made
    creatures: they get no stars from `CreatureStars` (it only acts on spawns by the game's spawners and the mod's own).
    (8) The hoard lock counts only shore guards, not the moored ship's crew, even where the ship lies within the radius;
    the ship has its own locks. (9) Guards are not set to hunt players, unlike the ship's crew; they use the game's own
    alertness. (10) No horn when a harbour wakes.
  - In-game checklist for the user: (a) Admin, standing near a coast with at least one world area generated: `whitehilt_seabattle
    raid` prints where the harbour was made and marks it with a pin; sail or walk there. A dark Nail Ship lies moored off the
    shore, side-on, and does not drift or turn for minutes, with its crew standing on deck and the archers shooting when you
    come near; the guards stand in a ring round a chest and walk back to their posts when drawn away; the chest answers
    *The guards still live.* until the guards are dead, then opens with the loot, and the saga line appears once for
    those near; the emptied chest disappears. (b) A chart from a Nail Ship's hold: using it with a coast explored 400 to
    1500 m away marks a harbour and the chart goes; with nothing explored it says *No coast on this chart is known to you.*
    and the chart stays; with six unsacked harbours it says they are all manned and the chart stays. (c) Wearing the Ruby
    Pathfinder's Amulet, the target arrow points to the harbour. (d) Leave and come back, restart the world and the
    dedicated server: the harbour, its guards and ship are still there, with no second crew. (e) Kill the moored ship's
    crew: it becomes a prize (message, saga deed, hover text), is released from the mooring, can be sailed and sinks
    after `PrizeMinutes`. (f) A natural Nail Ship still comes while a harbour's ship is loaded near you. (g) With a second
    client, the hoard, ship and guards are the same for both, and the first client leaving hands the harbour's ship and
    guards to the other. Known limits: a harbour made on a coast where no deep water lies within `ShipSearchRadius` is
    skipped; guards may stand on a tree or rock the ZDO scan could not see (grass and clutter are not ZDOs).
  - For phase 6 (docs): the final settings are those of section 6 phase 1 (`[SeaBattles]`, `[SeaBattles.Raids]`,
    `[Ships.Ballista]`); no setting was added in phase 5. Console: `whitehilt_seabattle [status|summon|raid]`. Prefabs:
    `WhiteHilt_NailShip`, `WhiteHiltShipBallista`, `WhiteHiltShipBolt`, `WhiteHiltRaidChart`, `WhiteHilt_RaidHoard`.
    Routed RPCs in total: `WhiteHiltSeaBattleRequest`, `...Spawn`, `...Admin`, `...Reply`, `...Sighted`,
    `WhiteHiltRaidRequest`, `WhiteHiltRaidAdmin`, `WhiteHiltRaidReply`; object RPCs: `WhiteHiltRaiderMessage`,
    `WhiteHiltRaidDone`, plus the ballista's. Mention in the docs that harbour spacing and building clearance come from
    `[Treasure] MinTreasureDistance` and `MinBaseDistance`.

- **Phase 6 (documentation and wrap-up), 2026-10-13.**
  - Done: new page `docs/sea-battles.md` (the Nail Ship: when and where, crew; the fight and the boarding leap; the
    prize; the ballista with a link; raids; saga deeds; multiplayer; console; full tables for `[SeaBattles]` and
    `[SeaBattles.Raids]`, with a note that harbour spacing, building clearance and biomes come from `[Treasure]`; no
    image row, no new image). `docs/ships.md`: the ballista upgrade row, the bolt and the `[Ships.Ballista]` table were
    already right against the code; added a link to the new page, "nine upgrades" became "ten" for Skidbladnir, and the
    Harbour Anchor paragraph says Nail Ships do not show. `docs/saga.md`: two deed rows, the two renown settings and the
    witness range text. `docs/README.md`: a row under *Sea & travel*, and "sea battles and raids" left the *Ahead* row.
    `docs/ideas.md`: the planned section is gone. `README.MD`: a card in *Sea & travel* (it fills the empty cell next
    to *Bridges, jetties & quays*); it reuses `docs/images/skidbladnir.png`, the nearest fit among the existing images, as
    cards need a picture and no new one was to be made. *New in 0.127.0* is untouched: it describes the latest
    released version only. `CHANGELOG.md`: the Nail Ship, prize, leap, raid, saga, console and ballista entries under
    `## Unreleased`, plus two lines under `### Changed`. `docs/architecture.md`: one bullet in *Harmony patches* on the
    transpiler in `RaiderShipPatches.cs`. The generated `Package/README.md` and the version numbers were not touched.
  - Discrepancies found between the plan and the code (the docs follow the code): (1) the plan's `RequiredKey` text,
    "empty = from the start", is only half true: the default `CrewTiers` list starts at `defeated_bonemass`, so with an
    empty `RequiredKey` and no tier key set `CrewTable.Pick` finds no tier and the server sends nothing (the admin
    `summon` and a harbour fall back to the first tier). (2) The plan said "no Kraken on a raider deck" and the code
    stops the Kraken only for a Nail Ship as its target, which is the same thing. (3) The chance is rolled per ship
    (per request from the ship's owner), not per player; extra players only add `ChancePerExtraPlayer`. (4) The deed
    text reads "A Nail Ship was taken: Nail Ship" (the plan's format, see Phase 4). (5) The cooldown is kept in the
    server's memory, so a restart clears it. (6) `summon` on a ship that another machine owns gets "A Nail Ship is
    sent." and the cooldown starts, but nothing appears, since only the ship's owner may raise the Nail Ship.
    (7) The `[Ships.Ballista] Enabled` config text does not say that a ballista can not be manned while it is off; the
    code does refuse (`ShipBallista.Interact`), and `docs/ships.md` says so. No code was changed.
  - Checks: see the final message of the phase; `cfg_keys.py --check`, `dotnet test BrudvikWhiteHilt.Tests` and a script
    that reads every relative link in the changed Markdown files.
- **Review fixes (after phase 6)**, on the issues the docs pass found:
  - With `RequiredKey` empty and no tier key set, a natural encounter now uses the first crew tier (it sent nothing).
  - Spawn orders (natural and admin) go to the ship ZDO's owner, not to the asker (`SeaBattleService.SpawnPeer`), so
    `summon` works from a ship another machine owns.
  - Saga line is "A Nail Ship was taken" / "Et naglskip ble tatt" (the name was printed twice).
  - `ChancePerMinute` text says per ship with one player aboard; `[Ships.Ballista] Enabled` text says the ballista cannot
    be manned while off (English and Norwegian). `docs/sea-battles.md` follows.
  - Left as is: the cooldown lives in server memory and resets on restart, like the Kraken's.
  - Build exit 0, 375 tests passed, `cfg_keys.py --check` 0 missing.
