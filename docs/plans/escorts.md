# ⚓ Plan: escorting trading ships

Implementation plan for *Escorts* under *More for the sea battles* in [Ideas for new content](../ideas.md). It builds on
the sea battles (0.128.0), damage at sea (0.129.x), fleets (0.130.0), friendly-fire immunity and the pursuit speed cap
(both after 0.131.0). Their plans and logs are [sea-battles.md](sea-battles.md), [sea-damage-and-weapons.md](sea-damage-and-weapons.md)
and [fleets.md](fleets.md). Written for an AI agent that carries it out phase by phase without the conversation behind
it. Read all of it once before you start, and read section 11 of the earlier plans for what the existing code really
does.

## 1. What the player gets

- **An Escort Contract.** Haldor sells an *Escort Contract* once the sea battles are open to the world (the same key
  as the Nail Ships). Using it while aboard your own ship at open sea calls a **trading ship** that sails up beside you.
- **The trading ship.** A merchant longship with a striped sail and cargo on deck. It sails on its own, a little slower
  than a longship at full sail, to a **destination** marked on your map 1–2 km away across deep water. Its helm and
  hold are shut: you cannot take it over.
- **The attacks.** On the way, Nail Ships come for the *trading ship*, not for you: one or two waves, and a whole fleet
  once the world allows fleets. They chase it, shoot at it and leap aboard. You beat them back: sink or take the Nail
  Ships, kill the boarders on the merchant's deck, and patch its holes.
- **The cargo.** Every living raider standing on the merchant's deck carries cargo off. The cargo (100 % at the start)
  sets the pay. If the cargo reaches 0, or the trading ship sinks, the contract fails.
- **Staying with it.** If no player is near the trading ship for too long, the merchant gives up and the contract fails.
- **The pay.** When the trading ship reaches its destination, every player within reach gets coins in proportion to
  the cargo left, a chance of a Raid Chart, and a Saga deed. The trading ship then sails out of sight.
- **Console.** `whitehilt_seabattle escort` starts an escort from the ship you are on, without a contract, and
  `whitehilt_seabattle escort wave` sends the next wave at once. Both are admin-only.

## 2. Decisions already made

| Decision | Choice | Why |
|---|---|---|
| Where the job comes from | An item, `WhiteHilt_EscortContract`, sold by Haldor (vanilla trader prefab `Haldor`, *(verify)*), used like the Raid Chart | Reuses the tested Raid Chart flow (item use → server request → reply), and needs no dialogue UI |
| The trading ship | Its own prefab `WhiteHilt_TradeShip`, a clone of `VikingShip` made in a new `TradeShipRegistry`, the way `NailShipRegistry` clones it, with a non-persistent `ZNetView` | Same tested path as the Nail Ship; a non-persistent ship cannot linger in the saved world |
| Who sails it | A new component `TradeShip`, a much smaller sibling of `RaiderShip`: owner-only, it thinks 4 times a second and steers with `ShipAssist.SteerTowards`. It is registered in `SailingShips`, so the physics does not stop it and the Nail Ship tailwind patch gives it wind from behind | No new physics patches needed |
| No NPC on deck | No Haldor model aboard; a striped sail and cargo props (crates and barrels) show that it is a merchant | Avoids a networked NPC on a moving deck. A skipper can come later |
| Who the Nail Ships hunt | The trading ship. Waves are raised with the existing `RaiderShip.SpawnFleet(target: tradeShip, ...)` | All the raider AI, boarding, weapons and fleets work unchanged |
| Who decides | The server approves a contract and picks the destination. The trading ship's owner asks the server for each wave when it is due, and the server checks the request and orders the spawn, as for natural encounters | Same "server decides, owner simulates" rule as the rest |
| Natural encounters | No natural Nail Ship encounter is rolled for a ship within 400 m of an active trading ship | Two attacks at once would swamp the escort |
| Failure | The trading ship sinks (its hull is destroyed), the cargo reaches 0, or no player is within `AbandonRange` for `AbandonSeconds` | Clear, readable outcomes |
| Pay | Coins = `Pay` × cargo share, to every player within `RewardRange` when it arrives, plus a chance of a Raid Chart (roll per player), plus a Saga deed `SagaKind.Escort` | Shared reward, the same way the raid hoard gives its deed |
| Contract on failure | Lost. On a refusal by the server (no deep water, cooldown, etc.) it is kept | A refusal must not cost the player |
| Biome | Escorts are encounters, not recipes. The contract costs coins only. Loot stays at Swamp tier or earlier, as for every Nail Ship | Biome rule |
| Release | Changelog under `## Unreleased` in `CHANGELOG.md`. New RPCs and a new item make this a MINOR bump | Copilot instructions |

## 3. Rules for the implementer

Section 3 of [sea-battles.md](sea-battles.md) applies in full:

- Settings with Norwegian labels, checked with `cfg_keys.py --check`.
- A class-level `[HarmonyPatch]` on every patch class.
- `ZNetScene.instance.Destroy`, never `Object.Destroy`, on anything with a `ZNetView`.
- Only the owner writes a ZDO.
- Headless guards with try/catch.
- No file edits from the terminal.
- Verify vanilla names before using them, and read the decompiled game at `%TEMP%\wh_tame\full\*.cs`.
- Build and test after each phase.
- No commit, push, deploy or release.
- XML docs on every public member.
- Deviations go in the log of this file.

On top of those:

- Inside `BrudvikWhiteHilt.Patches.*` namespaces write `global::BrudvikWhiteHilt.SeaBattles.X`.
- Pure logic goes in static classes with plain values, tested in `BrudvikWhiteHilt.Tests`. No `Ship`, `Player` or other
  game types there; pass functions in for anything the world answers, as `EncounterRules.TryPickSpawn` takes its
  deep-water test.
- **Read before you write.** These files are your templates:

  | For | Read |
  |---|---|
  | Prefab clone, sail look, non-persistent ZDO | `SeaBattles/NailShipRegistry.cs`, `SeaBattles/NailShipLook.cs` |
  | Sailing on its own, steering, despawn | `SeaBattles/RaiderShip.cs`, `SeaBattles/SailingShips.cs`, `Pieces/Ships/ShipAssist.cs` (`SteerTowards`, `Blocked`, `ShallowAhead`, `Lookahead`). `RaiderShip.ChooseCourse` is the obstacle-avoidance to copy |
  | Helm and hold locked | `Patches/SeaBattles/RaiderShipPatches.cs` (`Helm`, `Hold`) |
  | Server request, checks, spawn order to a client | `SeaBattles/SeaBattleService.cs` (`RequestRpc`, `SpawnRpc`, `RPC_Spawn`, `AboardRange`, cooldowns) |
  | Item used to ask the server, map pin on reply | `Patches/SeaBattles/RaidPatches.cs`, `SeaBattles/Raids/RaidService.cs` (Raid Chart) |
  | Adding goods to a trader, idempotent, existing traders too | `Companions/DogRegistry.Setup.cs` (`AddToTrader`, `UpdateTraders`, `UpdateTrader`) |
  | Saga deed | `SeaBattles/Raids/RaidHoard.cs:124` (`SagaLog.Announce`), `Saga/SagaLog.cs` (`SagaKind`, renown switch), `Saga/SagaSettings.cs` |
  | Boarded raiders on a ship | `SeaBattles/Pursuit.cs` (how it finds live `RaiderCrew` with `Boarded` standing on a ship via `GetStandingOnShip()`) |
  | Holes and flooding | `SeaBattles/Damage/ShipDamage.cs`: find out how a ship gets its `ShipDamage` component, and make sure the trading ship gets one, so it can be holed, can flood, and can be patched by players |
  | Console | `SeaBattles/SeaBattleCommands.cs` |
  | Fleet size by world stage | `SeaBattles/Fleets/FleetRules.cs`, `FleetSettings.cs` (`FleetTiers`) |

## 4. Settings

New section `SeaBattles.Escorts`. All settings are admin-only and get Norwegian labels. Use the literal
`const string Section = "SeaBattles.Escorts"`.

| Key | Default | Range | Meaning |
|---|---|---|---|
| `Enabled` | true | | Escort contracts can be bought and used |
| `ContractPrice` | 300 | 0–5000 | Haldor's price in coins |
| `MinDistance` | 1200 | 400–5000 | Least straight-line distance to the destination, in metres |
| `MaxDistance` | 2000 | 400–8000 | Most distance to the destination |
| `MinDepth` | 6 | 2–30 | The whole straight line to the destination must be at least this deep (world generator) |
| `SailForceFactor` | 0.8 | 0.3–1.5 | Multiplies the cloned longship's sail force: a bit slower than a longship |
| `HullHealth` | 1500 | 200–10000 | The trading ship's hull health |
| `Waves` | 2 | 0–4 | Attacks on the way |
| `WaveStart` | 0.25 | 0–0.9 | Share of the route at which the first wave can come |
| `WaveEnd` | 0.8 | 0.1–1 | Share of the route by which the last wave has come |
| `PlunderPerRaider` | 0.5 | 0–10 | Percent of the cargo one living raider on the deck carries off per second |
| `AbandonRange` | 300 | 50–1000 | A player must be this close to the trading ship |
| `AbandonSeconds` | 120 | 10–900 | How long nobody may be in range before the merchant gives up |
| `ArriveRadius` | 60 | 20–200 | The trading ship has arrived within this distance of the destination |
| `Pay` | 600 | 0–10000 | Coins for a full cargo, to each player in reach |
| `RewardRange` | 150 | 20–500 | Players this close to the trading ship when it arrives are paid |
| `RaidChartChance` | 25 | 0–100 | Percent chance per paid player of a Raid Chart |
| `CooldownMinutes` | 10 | 0–240 | Least time between two contracts for one player |
| `NoEncounterRange` | 400 | 0–2000 | No natural Nail Ship encounter is rolled for a ship this close to an active trading ship |

Add a renown setting `RenownEscort` (default the same as `RenownRaid`) in `SagaSettings`, the same way as the others.

## 5. Names, keys and texts

- Prefabs: `WhiteHilt_TradeShip` and `WhiteHilt_EscortContract`. The contract is a clone of a vanilla paper or
  map-like item. Look at what the Raid Chart is cloned from, and use the same base with its own icon tint or name.
- ZDO keys on the trading ship go in a new `TradeKeys` class, like `RaiderKeys`, with `whitehilt_trade_*` names:
  - `Destination` (Vector3)
  - `Start` (Vector3, where it began, for progress)
  - `Contractor` (long, the player id)
  - `State` (int: `TradeState.Sailing`, `Arrived`, `Lost`)
  - `Cargo` (float, 0–1)
  - `WavesSent` (int)
  - `WaveAt` (float array or one float per wave: the progress share at which wave *i* comes, rolled at spawn)
  - `Seen` (long ticks, the last time a player was in range)
  - `EndedAt` (long ticks)
- RPCs, prefixed `WhiteHiltEscort`:
  - `Request(shipId)`: client → server.
  - `Start(shipId, destination)`: server → client that asked; it raises the trading ship and consumes the contract.
  - `Refused(reasonToken)`: server → client.
  - `Wave(tradeShipId, waveIndex)`: trade ship owner → server. The server checks the ZDO's `WavesSent` and the prefab,
    then sends the existing Nail Ship spawn order to that owner with the trading ship as target and a fleet size from
    `FleetTiers`. Read `RPC_Spawn` and adjust it if it refuses a target with no player aboard.
  - `Paid(cargoShare)`: trade ship owner → each paid player's peer. That client adds the coins (dropping them at the
    player's feet if the inventory is full), rolls the Raid Chart, and announces the deed.
- English texts in the settings or registry file, as in `SeaBattleSettings`, and Norwegian in `Translations/Norwegian.json`:

  | Token | English |
  |---|---|
  | `whitehilt_tradeship` | Trading Ship |
  | `item_whitehilt_escortcontract` | Escort Contract |
  | `item_whitehilt_escortcontract_desc` | Haldor's seal on a merchant's passage. Use it aboard your ship at open sea, and see his trading ship safely to port. |
  | `msg_whitehilt_escort_started` | A trading ship joins you. Its port is marked on your map. |
  | `msg_whitehilt_escort_wave` | Nail Ships! They are after the merchant. |
  | `msg_whitehilt_escort_plunder` | Raiders on the merchant's deck! The cargo is being carried off. |
  | `msg_whitehilt_escort_arrived` | The trading ship is safe in port. Haldor pays you {0} coins. |
  | `msg_whitehilt_escort_sunk` | The trading ship is lost. The contract has failed. |
  | `msg_whitehilt_escort_plundered` | The cargo is gone. The contract has failed. |
  | `msg_whitehilt_escort_abandoned` | Left alone, the merchant turns back. The contract has failed. |
  | `msg_whitehilt_escort_refused_sea` | You must be aboard your own ship at open sea. |
  | `msg_whitehilt_escort_refused_route` | No clear deep-water passage from here. Sail further out. |
  | `msg_whitehilt_escort_refused_busy` | You already have a merchant to guard, or one sailed with you too recently. |
  | `msg_whitehilt_trade_helm_held` | The merchant's skipper keeps the helm. |
  | `whitehilt_escort_pin` | Merchant's port |
  | `whitehilt_trade_hover` | Cargo {0} %, {1} m to port |
  | `whitehilt_saga_escort` | (follow the wording style of the other deed texts in `SagaLog`) |

  Write the Norwegian in the voice of the other `*_raider_*` texts.

## 6. Phases

### Phase 1: settings, texts and pure logic

1. `SeaBattles/Escorts/EscortSettings.cs` with the table in section 4. Call `Initialize()` from `BrudvikWhiteHilt.Awake`
   next to `FleetSettings`.
2. Texts, English and Norwegian, as in section 5.
3. `SeaBattles/Escorts/EscortRules.cs`, pure and static:
   - `TryPickDestination(Vector2 from, float heading, float min, float max, IReadOnlyList<float> bearings, Func<Vector2,bool> deep, float step, out Vector2 destination)`.
     Try each bearing offset from the heading and each distance from max down to min in 200 m steps. A destination is
     good when every point on the straight line, every `step` metres (50), is deep.
   - `RollWaves(int count, float start, float end, Func<float> random)` returns sorted progress shares, spread so no
     two are closer than `(end - start) / (count * 2)`.
   - `Progress(Vector2 start, Vector2 destination, Vector2 now)`, from 0 to 1, along the line from start to destination
     (a projection, clamped).
   - `WaveDue(float progress, IReadOnlyList<float> waveAt, int sent)` returns true when `sent < count` and
     `progress >= waveAt[sent]`.
   - `Plunder(float cargo, int raidersAboard, float percentPerRaiderPerSecond, float dt)` returns the new cargo,
     clamped at 0.
   - `Outcome(...)` returns `Sailing`, `Arrived`, `Sunk`, `Plundered` or `Abandoned`, from: the distance to the
     destination against the arrive radius, the hull destroyed, the cargo at 0, and the seconds since a player was in
     range against the abandon time. The checks run in that order of priority.
   - `Pay(int full, float cargo)` returns the coins, rounded down.
4. Tests in `BrudvikWhiteHilt.Tests/EscortTests.cs` for each function, including the edge cases: no deep line, zero
   waves, cargo exactly 0, arrive and sink in the same tick (sink wins), and progress beyond the destination.

### Phase 2: the trading ship

1. `SeaBattles/Escorts/TradeShipRegistry.cs`:
   - Clone `VikingShip` the way `NailShipRegistry.Add` does.
   - Set `m_persistent = false`, `m_canBeRemoved = false` and no resources.
   - Set the hull health from the setting, and multiply the sail force by `SailForceFactor`.
   - Add the `TradeShip` component.
   - Apply a merchant look with a `TradeShipLook` class modelled on `NailShipLook`: a striped sail (cream and blue, or
     Haldor's colours) and three or four crate and barrel props on deck, copied from vanilla meshes as plain
     non-networked children. Headless servers skip the look entirely.
   - Register the prefab from `BrudvikWhiteHilt.Awake`.
2. `SeaBattles/Escorts/TradeShip.cs` (MonoBehaviour), keeping a static registry like `RaiderShip.byShip` with these
   statics: `Of`, `Is`, `Loaded`, `AnyNear(position, range)` and `All`.
   - **Awake/OnDestroy:** register and unregister, and attach `ShipAssist`. Add the ship's `m_players` to
     `SailingShips` while it sails, and remove it when it ends or is destroyed.
   - **Owner only, every 0.25 s:**
     1. Compute the progress.
     2. Steer to the destination with a copy of the `ChooseCourse` avoidance, at `Ship.Speed.Full`.
     3. Count the boarded raiders and plunder the cargo.
     4. Update `Seen`.
     5. Ask for a wave when one is due (phase 4).
     6. Decide the outcome (phase 4).
   - **When it ends:** stop it (`Ship.Speed.Stop`), write the state and `EndedAt`, and send the end message. 60 s
     later it sails off: steer away from the nearest player at full speed, then `ZNetScene.instance.Destroy` it.
   - **Hover text:** the ship name, the cargo % and the metres to port.
   - **Locks:** in `RaiderShipPatches.Helm` and `Hold`, refuse a `TradeShip` too, with its own helm message. Do not
     create a second patch on the same methods.
   - **Holes:** make sure it gets `ShipDamage`, so it holes and floods like any ship.
3. Console: `whitehilt_seabattle escort` raises a trading ship beside the admin's ship, going straight through
   `TradeShip.Raise(ship, destination)` on the server's approval path, so the same code is tested.
   `SeaBattleCommands` describe output lists the loaded trading ships with their state, cargo, waves and distance.
4. Build and test, then smoke-test the pieces you can without the game (pure functions).

### Phase 3: the contract and the server flow

1. The contract item, registered and sold by Haldor following the `DogRegistry.Setup` trader pattern: idempotent,
   existing traders updated too, and removed when `Enabled` is false. It needs the `SeaBattleSettings.RequiredKey`
   global key, using the trader item's `m_requiredGlobalKey`.
2. Using the item, patched like the Raid Chart in `RaidPatches`: the local player must be aboard a ship they are
   attached to or standing on, the ship must not be a Nail Ship or a trading ship, and it must be at open sea
   (`SeaBattleService.IsOcean`). Then send `Request(shipId)`. Do not consume the item yet.
3. The server's `Request` checks:
   - the feature is on;
   - the sender is within `AboardRange` of the ship;
   - the sender's cooldown has run out (a dictionary of player id → time, kept only in memory);
   - the sender has no active trading ship (contractor in any loaded or known trade ship ZDO; memory is enough);
   - the ship is at open sea;
   - `TryPickDestination` with `SeaBattleService.DeepWater` succeeds.
   Then it replies `Start` or `Refused`.
4. The client's `Start` handler:
   - consumes one contract;
   - raises the trading ship 25 m to the side of the player's ship, in deep water, facing the destination;
   - fills the ZDO (start, destination, contractor, cargo 1, waves rolled with `RollWaves`);
   - adds a map pin at the destination (`Minimap.PinType.Icon3`, not saved), and removes it when the escort ends
     (`Minimap.RemovePin`);
   - shows `msg_whitehilt_escort_started`.

   The pin is local to the contractor. Other players see the ship's hover text.
5. Suppress natural encounters: in the server's encounter roll in `SeaBattleService`, skip a ship within
   `NoEncounterRange` of an active trading ship (`TradeShip.AnyNear`). The server may not have the trading ship loaded,
   so use the ZDO positions of `WhiteHilt_TradeShip` instances that are in state `Sailing`. Look at how
   `SeaBattleService` finds ZDOs elsewhere, or keep a server-side set of active trading ship ZDOIDs from the `Start`
   handshake. Pick whichever is simpler and log it.

### Phase 4: waves, plunder, outcome and pay

1. **Waves.** When `WaveDue`, the owner sends `Wave(tradeShipId, sent)`. It sends the request once per index, and sends
   it again after 10 s if `WavesSent` has not moved. The server checks:
   - the ZDO exists and is a `WhiteHilt_TradeShip`;
   - its state is `Sailing`;
   - the index equals `WavesSent`;
   - the sea battles are on.

   It writes nothing (it does not own the ZDO). It sends the existing Nail Ship spawn order to the owner with the size
   from `FleetTiers` (a lone ship when no tier is set). The owner increments `WavesSent` when the spawn order arrives
   and shows `msg_whitehilt_escort_wave` to players within `RewardRange`. The crew bonus is the number of players
   within `AboardRange` of the trading ship.

   Check `RPC_Spawn`, `RaiderShip.SpawnFleet` and the raider's despawn logic (`ShouldDespawn`,
   `TargetMissingTooLong`) for assumptions that the target has players aboard. The raiders must keep hunting an empty
   trading ship as long as players are near. If a fix is needed, make it in the smallest place and log it.
2. **Plunder.** Count the boarded live raiders on the trading ship's deck, as `Pursuit.cs` counts them. If that helper
   is private, move it to a public static on `RaiderCrew` (for example `RaiderCrew.AliveAboard(Ship)`) and use it from
   both. Lower the cargo with `Plunder`. Show `msg_whitehilt_escort_plunder` to nearby players when the first raider
   lands, at most once per 20 s.
3. **Outcome.** Sunk means the hull `WearNTear` is gone or its health is ≤ 0, or the ship has been destroyed. Hook the
   trading ship's own destruction (OnDestroy on the owner with state still `Sailing` and health ≤ 0) so a sinking
   counts. Arrived, Plundered and Abandoned come from `Outcome`.
4. **Pay** on `Arrived`:
   - For each player within `RewardRange` (`Player.GetAllPlayers()` on the owner), send `Paid(cargo)` to that player's
     peer (find the peer as other code does: `ZNet.instance.GetPeer` / the player's ZDO owner).
   - That client adds `Pay(EscortSettings.Pay, cargo)` coins (`Coins` prefab).
   - It rolls `RaidChartChance` for a Raid Chart (`NailShipRegistry.RaidChartName`).
   - It shows the arrived message with the coins.
   - The deed is announced once with `SagaLog.Announce(SagaKind.Escort, "$whitehilt_tradeship", position)`.

   Add `Escort` at the end of `SagaKind`, never in the middle, since it is saved as an int. Wire its renown in the
   `SagaLog` switch, and give it its deed text like the others.
5. **Failure messages** go to players within `AbandonRange × 2`, and to the contractor wherever they are, with a routed
   RPC by player id if needed.
6. **Interplay to check and log:**
   - The pursuit cap (`Pursuit.cs`) now caps the trading ship, which is the hunted ship. That is harmless, but confirm
     it does not throttle the trading ship below the floor in a way that stops it.
   - Friendly fire (`FriendlyFire.cs`) lets Nail crew damage the trading ship. That is wanted.
   - The player's own ship is not hunted, so it is not capped.

### Phase 5: documentation

1. `docs/sea-battles.md`: a section *Escorts* in the style of the *Fleets* section, with the settings table (section 4)
   and Norwegian-free English text.
2. `docs/ideas.md`: remove the *Escorts* bullet.
3. `CHANGELOG.md`: an entry under `## Unreleased`. Create the heading at the top if it is missing, in the style of the
   earlier entries.
4. Do **not** edit `README.MD` or anything under `Package/`.

## 7. Multiplayer and performance notes

- The trading ship's owner is whoever the game gives it to, usually the nearest client. It decides everything about
  the ship. Everything it does must survive an owner change: progress, cargo, waves and `Seen` live in the ZDO.
- The server never writes the trading ship's ZDO. It only approves, and orders spawns.
- The trading ship thinks every 0.25 s. Counting the boarders uses the `RaiderCrew` registry, never
  `FindObjectsOfType`.
- The encounter-suppression check runs once per encounter roll (every 60 s), not per frame.
- `SailingShips` membership makes the existing `CrewCountTranspiler` and `Tailwind` patches cover the trading ship
  with no new physics patch.

## 8. Checks after every phase

As in section 9 of [sea-battles.md](sea-battles.md). From the repository root:

```powershell
$msbuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
& $msbuild BrudvikWhiteHilt\BrudvikWhiteHilt.csproj "/t:BeforeBuild;CoreBuild" "/p:SolutionDir=$PWD\" /p:GenerateDocumentationFile=true /v:minimal
dotnet test BrudvikWhiteHilt.Tests
python AssetSource\Tools\cfg_keys.py --check
```

- No new warnings.
- All tests pass, including `PatchTests` and `EveryEnglishTextHasANorwegianOne`.
- `cfg_keys.py --check` reports 0 missing.
- Every new patch class has `[HarmonyPatch]` on the class.
- Every new `Initialize()` is called from `BrudvikWhiteHilt.Awake`.

## 9. What cannot be verified without the game

List these in the log, honestly, for the user to test:

- The trading ship sails and steers around land.
- Raiders board it and keep hunting it with nobody aboard.
- Plunder ticks.
- The pay reaches every player.
- The pin is removed.
- The sail and props look right.

## 10. Reference

- Vanilla names to verify with `python AssetSource\Tools\vanilla_prefab.py <name> --values`: `Haldor` (Trader
  component, `m_items`), `Coins`, `VikingShip`, and the crate and barrel props you pick.
- Raid Chart: `NailShipRegistry.RaidChartName`, `RaidPatches`, `RaidService`.

## 11. Log

(The implementer writes here, per phase: what was done, deviations and why, what could not be verified.)

### Phase 1: settings, texts and pure logic (2026-10-14)

**Done.** New: `SeaBattles/Escorts/EscortSettings.cs` (section `SeaBattles.Escorts`, the 19 keys of section 4,
`Initialize()` called in `Awake` right after `FleetSettings.Initialize()`; it also registers every English text of
section 5 except the saga line), `SeaBattles/Escorts/EscortRules.cs` (pure: `TryPickDestination`, `RollWaves`,
`Progress`, `WaveDue`, `Plunder`, `Outcome`, `Pay`, plus `AnyWithin` for the encounter suppression of phase 3, and the
`EscortOutcome` enum), `BrudvikWhiteHilt.Tests/EscortTests.cs` (53 tests). Changed: `Saga/SagaLog.cs` (`SagaKind.Escort`
added last, and its `RPC_Deed` case), `Saga/SagaSettings.cs` (`RenownEscort`, default 40 like `RenownRaid`, and the
English line `whitehilt_saga_escort` "A trading ship was seen safely to port"), `Translations/Norwegian.json` (all
texts, a label and description for every key, the section label `whitehilt_cfgsec_seabattles_escorts`, the renown
label).

**Checks.** Build with `GenerateDocumentationFile`: exit 0, no new warnings (only the old CS0419/CS1591 in
`NearbyContainers`, `RecipeCategories`, `DefenseModelBuilder`). `dotnet test BrudvikWhiteHilt.Tests`: 846 passed, 0 failed
(793 before, 53 new). `cfg_keys.py --check`: 0 missing (it said 4 missing before this phase, see below).

**Deviations**
- **`Outcome` priority.** Section 6 says the checks run "in that order" (arrived, sunk, plundered, abandoned) and also
  that a ship that arrives and sinks in the same tick is Sunk. These cannot both hold. The explicit edge case wins:
  Sunk, then Plundered, then Arrived, then Abandoned. A ship that reaches port with an empty hold therefore fails
  (section 1: "if the cargo reaches 0 ... the contract fails"), and arriving beats the merchant's patience.
- **`TryPickDestination`** also tries the minimum distance when the 200 m steps from the maximum do not land on it; a
  maximum below the minimum counts as the minimum; a line is good when every `step` metres before the end and the end
  itself are deep (the start is not tested, the ship floats there).
- **`RollWaves`**: the stretch is cut into one slot per wave and each wave lies in its slot, kept a quarter slot from
  the slot edges it shares. That keeps any two waves at least half a slot, `(end - start) / (count * 2)`, apart, as
  the plan asks, and the waves sorted without sorting.
- **Item texts** are registered by `EscortSettings`, under the keys the plan names (`item_whitehilt_escortcontract`
  and `item_whitehilt_escortcontract_description`; the plan's `_desc` suffix is replaced by `_description`, which is
  what every item of the mod and `Translations.AddEnglishNameAndDescription` use). Phase 3's item must not register
  them again.
- **Pre-existing gap fixed.** `cfg_keys.py --check` already reported 4 missing before this work: the labels and
  descriptions of `[SeaBattles] PursuitEnabled`, `PursuitShockSeconds`, `PursuitSpeedFactor` and `PursuitFloorSpeed`
  (added by the pursuit cap commit without Norwegian). They are added to `Norwegian.json` so the check can reach 0.
- No feature gate (`AddFeatureGate`) for the contract: it has no recipe, so Haldor's list is what `Enabled` controls.

**Not verified.** Nothing in this phase runs in the game: it is settings, texts and pure functions.

### Phase 2: the trading ship (2026-10-14)

**Done.** New in `SeaBattles/Escorts/`: `TradeKeys.cs` (the nine ZDO keys, `whitehilt_trade_*`, and the `TradeState` enum
with the numbers 0 Sailing, 1 Arrived, 2 Lost; the waves' shares are stored one float per wave under
`whitehilt_trade_waveat0..3`, with `whitehilt_trade_waves` for the count), `TradeShipRegistry.cs` (clone of `VikingShip` as
`WhiteHilt_TradeShip`), `TradeShipLook.cs`, `TradeShip.cs` (the component: statics `Of`, `Is`, `Loaded`, `All`,
`AnyNear`, `Describe`, `TryPickDestination`, `Raise`; the owner's 0.25 s `Think`). Changed: `RaiderCrew.AliveAboard(Ship)`
(public static; `Pursuit.EnemiesAboard` now uses it, same behaviour), `RaiderShipPatches` (`Helm`, `Hold` refuse a
trading ship too, and `Tailwind` covers it), `ShipAssist` (hover text, no push), `ShipMooring.Nearest`,
`NearbyContainers` (the hold is no crafting source), `SeaBattleService` (`Summon`, `SummonFleet`, `TargetShip` and the
client's `Conditions` skip a trading ship; `Conditions` also skips a ship within `NoEncounterRange` of a loaded trading ship
on its way), `SeaBattleCommands` (`escort`, see below, and `status` lists the trading ships), `BrudvikWhiteHilt.Awake`
(`TradeShipRegistry.Initialize()` right after the Nail Ship's), two more texts (`msg_whitehilt_trade_hold_shut`,
`msg_whitehilt_escort_refused_off`) with Norwegian, and 5 tests in `EscortTests.cs`.

**How it works.** The prefab: `Piece` not removable and without resources, hull health from `[SeaBattles.Escorts] HullHealth`
with pierce set to Normal (as the Nail Ship's, so arrows and bolts hurt), sail force times `SailForceFactor`, `ZNetView.m_persistent`
false, `TradeShip` added. Every `Ship` gets `ShipDamage` (and `ShipAssist`) from the existing `Ship.Awake` postfix, so the
merchant gets holes, floods and can be patched with no new code. `TradeShip.Think` (owner only, 0.25 s): course to the port by
`RaiderTactics.CourseTo` and a copy of `ChooseCourse`, sail Full (Stop when no clear course within 90 degrees); counts the
living boarded raiders on its deck with `RaiderCrew.AliveAboard` and lowers the cargo with `EscortRules.Plunder` (written when it
changed); renews `Seen` when a player is aboard or within `AbandonRange` (written at most every 5 s); asks `EscortRules.Outcome`.
`Finish` writes the state (Arrived or Lost) and `EndedAt` once, stops the ship and takes it out of `SailingShips`
(so the game's drag holds it still in port). After the end the owner keeps it still for 60 world seconds, then it steers away
from the nearest player at full speed (back in `SailingShips`) and is removed with `ZNetScene.instance.Destroy` when no
player is within `[SeaBattles] DespawnRange`, or 120 s later at the latest; it is removed at once when no player is within
`AbandonRange` (which is how an abandoned ship goes) and never while a player stands on the deck. The hull's `m_onDestroyed` is
hooked on the owner and calls `Finish(Sunk)`.

**Deviations**
- **The look copies nothing new.** The longship's own `ship/visual/Customize/storage` group already holds trader dressing:
  `barrell`, `barrell (1)`, `barrell (2)` (mesh with `barrel_iron`) and ten `default (n)` braided crates, plus the shields,
  all switched off in the prefab. `TradeShipLook.ShowCargo` switches on only the barrels and the crates (the Nail Ship's look
  switches on only the shields, the same way) instead of copying meshes into new children. That is plain, non-networked,
  identical to the plan's intent, and needs no mesh lookup. The sail: the vanilla red and white sail is repainted per pixel,
  red to blue and white to cream, keeping the cloth's grain (same method as the White Hilt ship's sail). Neither has been seen
  in the game.
- **Console `escort` is a local path for now.** The server flow does not exist before phase 3, so `escort` picks the port
  with `TradeShip.TryPickDestination` (the same function the server will use) and calls `TradeShip.Raise` on the machine of an
  admin or host (`ZNet.LocalPlayerIsAdminOrHost`). Phase 3 replaces its body with the admin RPC. `escort wave` comes with phase 4.
- **Tailwind for a trading ship ignores `[SeaBattles] Tailwind`** (the Nail Ships' switch): a merchant that has to reach a
  port must not stall in a headwind. Only the condition in the `Tailwind` prefix changed.
- `Raise` tries the right side, then the left, at 25 m, 40 m and 60 m, for 6 m of water; if none is deep enough the ship is
  not raised. The plan's "25 m to the side" is the first try.
- `TradeKeys` has `Waves` (count) in addition to the plan's list, and `WaveAt` is one key per wave (at most 4, the setting's
  maximum).
- The merchant's hold answers `msg_whitehilt_trade_hold_shut` ("The merchant's cargo is not yours to open."), a text the plan
  did not list; the helm uses the plan's `msg_whitehilt_trade_helm_held`. `msg_whitehilt_escort_refused_off` is for phase 3
  (the server says the contract is off or the world key is not reached).
- **Not covered by the trading ship (left as they are):** `ManOverboard` (a player who falls off a trading ship gets the usual
  man-overboard handling, which is wanted), the Lookout pin (shows it as an ordinary ship), the Harbour Anchor listing
  (it lists persistent ships; this one is not).

**Checks.** Build with `GenerateDocumentationFile`: exit 0, no new warnings. `dotnet test BrudvikWhiteHilt.Tests`: 851 passed,
0 failed (846 before, 5 new). `cfg_keys.py --check`: 0 missing. Every patch class touched already has `[HarmonyPatch]` on the
class; no new patch class in this phase.

**Facts checked** (`vanilla_prefab.py VikingShip --all-bundles`): `Customize` is inactive and holds the tents, the lamp and
`storage`, which is active and holds the barrels and crates named above and 14 shields; the crates' mesh renderer uses
`braded_box`, the barrels `barrel_iron`, all on layer 28 with box colliders. `Ship.CustomFixedUpdate` reads `m_players.Count` for
the stop and the drag, both replaced by the existing transpiler, so the trading ship counts as crewed while it is in
`SailingShips`. The `Haldor` prefab has a `Trader` with `m_items` (checked for phase 3).

**Not verified (needs the game).** That the ship sails and steers round land; that the crates and barrels stand on the deck
without blocking the walkway and that the sail looks striped blue and cream; that the hover text shows; that `escort` finds a
port; that the helm and the hold refuse; that a stopped ship in port stays still; that the sail-off and removal work.

### Phase 3: the contract and the server flow (2026-10-14)

**Done.** New: `SeaBattles/Escorts/EscortContract.cs` (the item and Haldor's list), `SeaBattles/Escorts/EscortService.cs`
(on the `Game` object: the client request, the server's checks, the answer, the raising, the pin),
`Patches/SeaBattles/EscortPatches.cs` (class-level `[HarmonyPatch]`, prefix on `Humanoid.UseItem`). Changed: `SeaBattleGamePatches`
(adds the service), `SeaBattleService` (`IsOcean`, `HasKey`, `TargetShip`, `PlayersNear`, `PickFleetSize`, `Reply` and
`AboardRange` made `internal`; its request handler skips a ship near a merchant on its way), `SeaBattleCommands` (`escort` now
sends the admin RPC; the local path of phase 2 is gone), `BrudvikWhiteHilt.cs` (`EscortContract.Initialize()` in `Awake`,
`EscortContract.ApplyConfig()` in `RefreshConfig`), and one more test.

**Flow.** Item: `WhiteHilt_EscortContract`, a clone of `LeatherScraps` drawn with the treasure map mesh and tinted blue, exactly
like the Raid Chart (stack 10, Misc, no recipe). Haldor: `Haldor` is the vanilla trader prefab (checked with
`vanilla_prefab.py Haldor --values`: `Trader` with `m_items`, each entry with `m_requiredGlobalKey` and `m_buyPlayerEffects`).
`EscortContract.UpdateTrader` is the `TreasureRegistry` pattern: it runs for every `ZNetScene` through
`OnPrefabsRegistered`, for traders already in the world from `ApplyConfig`, adds the entry at `ContractPrice` with
`m_requiredGlobalKey` = `[SeaBattles] RequiredKey` (trimmed, empty means no key), and removes it when `[SeaBattles.Escorts]
Enabled` or `[SeaBattles] Enabled` is off. Use: the prefix asks `EscortService.Request` and returns false for the local player;
the client refuses at once (message, no RPC) when escorts are off or it is not aboard an ordinary ship at open sea
(`SeaBattleService.IsOcean`), else sends `WhiteHiltEscortRequest(shipId)`; it does not ask again for 20 s while awaiting an answer.
Server (`Approve`, also for the admin RPC `WhiteHiltEscortAdmin(shipId, mode)`): server only; sender must be a real connection
(`RoutedRpcSender.Verify`) with a living player ZDO; feature on and sea battles on; a player (not an admin) also needs the world key
of the sea battles; the ship must be known, no Nail Ship or trading ship, within `AboardRange` (60 m) of the player and
at open sea; a player (not an admin) also needs no merchant on its way (server memory) and a spent cooldown; then
`TradeShip.TryPickDestination` from the ship's position and rotation, which uses the world generator. Answers:
`WhiteHiltEscortRefused(reason)` with `off`, `sea`, `route` or `busy` (the client shows `msg_whitehilt_escort_refused_<reason>`),
or `WhiteHiltEscortStart(shipId, port, useContract)`. The cooldown starts when the server approves; a refusal costs nothing. The
client (`RPC_Start`, only from the server and only while an answer is awaited) raises the ship with `TradeShip.Raise`, then uses up
one contract (not for an admin), adds a pin (`Minimap.PinType.Icon3`, not saved, name `whitehilt_escort_pin`), shows
`msg_whitehilt_escort_started`, and tells the server the new ship's id with `WhiteHiltEscortRaised`. If the ship or the contract is
gone, or there is no deep water beside the ship, the contract is kept and `WhiteHiltEscortRaised(none)` gives the server its memory
and the cooldown back. The pin is dropped when the ship's data on this client says the escort ended, or when the data has been
unknown for 60 s (a notice from the ship's owner will drop it at once, phase 4).

**Suppressing natural encounters (decision, as the plan asks to log which way).** The server keeps its own memory
(`EscortService.active`, by the contractor's player id, with the ship's id from the `Raised` message), not a scan of the world's
ZDOs. `EscortService.Quiet(position)` reads the position of each remembered merchant that is still sailing from its ZDO and is
asked once per natural request (`SeaBattleService.RPC_Request`, after its own checks). A record without a ship stops counting after
45 s, a ship whose data never reaches the server after 60 s, and a ship that is no longer sailing at once. A client-side check
(`TradeShip.AnyNear` on loaded ships) also keeps the owner of a ship from asking at all, which saves the request. Why not a scan:
a scan of every ZDO per request is O(world), and the record is exact for every merchant that was started through a contract or the
admin command (the only ways).

**Checks.** Build: exit 0, no new warnings. `dotnet test`: 852 passed, 0 failed (851 before). `cfg_keys.py --check`: 0 missing. The
new patch class has `[HarmonyPatch]` on the class, and `EscortContract.Initialize()` is called from `Awake`.

**Deviations**
- **Reply to an admin** goes through the existing `WhiteHiltSeaBattleReply` (made `internal`), not a new RPC. A refusal for an admin
  is the same `Refused` message as for a player, shown on screen only.
- **The server does not scan the ZDOs** (see above). A server restart forgets the cooldowns and the merchants on their way, as the
  Nail Ships' cooldown does; a merchant started before a restart is not in the memory (its ZDO is not persistent anyway).
- **`m_requiredGlobalKey`** uses `[SeaBattles] RequiredKey`, as the plan says; if that setting is empty Haldor sells at once.
- **The contract is used up after the ship is raised**, so a failed raising costs nothing; the plan said "consumes one contract".
  An admin's escort never uses one up.
- Refusal reasons: `off` was added (see phase 2) for the case where the feature is off or the world key is missing, as the
  plan's three reasons did not cover it.

**Not verified (needs the game).** That Haldor sells the contract (Black Forest, behind the key, and for a Haldor already
standing in a loaded world); that using it in the inventory reaches the prefix; that the server and the client agree on the ship
(single player, a hosted game and a dedicated server); that the pin shows and goes; that the cooldown and the busy check refuse;
that a Nail Ship encounter really stays away near the merchant.

### Phase 4: waves, plunder, outcome and pay (2026-10-14)

**Done.** Changed: `SeaBattles/Escorts/EscortService.cs` (the `EscortNotice` enum, `RequestWave`, `Notice`, `Pay`, and the RPCs
`WhiteHiltEscortWave`, `...Spawn`, `...Paid`, `...Notice`; the admin wave), `SeaBattles/Escorts/TradeShip.cs` (waves asked for from
the owner's tick, the plunder warning, `Announce` at the end, `WaveSpawned`, `Id`, `IsOwner`), `SeaBattleCommands` (`escort wave`),
one more test. The ZDO keys, texts and settings of these steps were all made in phases 1 and 2.

**Waves.** The owner works out the progress along the route every tick; when `EscortRules.WaveDue` is true it sends
`WhiteHiltEscortWave(shipId, index)` to the server, once for each index and again after 10 s if `WavesSent` has not moved. The
server (`RPC_Wave`) drops it unless: server, real connection, escorts and sea battles on, the ZDO exists, is a trading ship, is still
`Sailing`, belongs to the sender (`ZDO.GetOwner() == sender`), and the index is the ZDO's `WavesSent` and below its wave count. It
writes nothing. It picks the tier (the world's keys; the first tier when none applies, so a world without the key can be tried with
the admin command), the extra crew (`EncounterRules.CrewBonus` of the players within `AboardRange` of the merchant, counted from the
player ZDOs), and the fleet size (`SeaBattleService.PickFleetSize`, so `FleetTiers` and `[SeaBattles.Fleets] Enabled` decide; a lone
ship when no tier applies), and sends `WhiteHiltEscortSpawn(shipId, index, tier, bonus, size)` to the ZDO's owner. The owner accepts it
only from the server, for a trading ship it owns that is still sailing and whose `WavesSent` equals the index, calls
`RaiderShip.SpawnFleet(tradeShip, ...)`, and on success increments `WavesSent` and sends the wave notice (the horn and
`msg_whitehilt_escort_wave` to every player within `RewardRange`). A spawn that finds no deep water leaves `WavesSent` alone so the
owner asks again. Admin: `whitehilt_seabattle escort wave` sends the server the admin RPC with mode 1; the server (admin check)
finds the nearest merchant of its memory and orders the next wave at once, whatever the progress.

**Checked in the code (the plan asked): do the raiders need a player aboard their target?** No.
- `RaiderShip.SpawnFleet` needs the target `Ship` with a valid `ZNetView`, a heading and deep water in its forward arc; nothing about
  players. `SeaBattleService.RPC_Spawn` was **not** used: it refuses while a Nail Ship is hunting (`RaiderShip.Hunting > 0`), which
  would stop a second wave, and it sends the "sighted" message to the target ship's own players, which would never see it here. The
  escort has its own order (above), so no change was needed in `RPC_Spawn`.
- The raider's despawn rules: `ShouldDespawn` keeps the ship while any player is within `DespawnRange` (300 m) of the raider, which is
  where the players escorting the merchant are, and `TargetMissingTooLong` only fires when the target ship is not loaded on the
  raider's owner for `DespawnSeconds`; an empty trading ship is loaded as long as players are near. So the raiders keep hunting an
  empty merchant. `Sail` and `RaiderTactics` read the target's position and heading only. `NailShipWeapons.TryDeckSpot` throws a pot
  at a random point of the deck when no player is aboard, so pots, bolts and fire reach the merchant. `RaiderCrew.TryLeap` leaps to
  the target's deck by `RaiderDeck.TryDeckPoint`, which needs only the ship's float box.

**Plunder.** `RaiderCrew.AliveAboard(ship)` (phase 2) counts the living boarded raiders on the deck by `RaiderCrew` registry, which
is a dictionary on the crew's AI, never `FindObjectsOfType`; it is the helper the plan wanted public, and `Pursuit` uses it too.
`EscortRules.Plunder` runs on every owner tick with the real time passed (at most 1 s), the cargo is written when it changed, and the
first raider to land (none before) is told to the players within `RewardRange`, at most once per 20 s.

**Outcome.** `EscortRules.Outcome` each tick (Sunk, Plundered, Arrived, Abandoned; see phase 1). The hull's `m_onDestroyed` calls
`Finish(Sunk)` on the owner, so a sinking counts (flood, fire, ram or bolts). `Finish` writes the state and the end time once, stops
the ship and calls `Announce`.

**Pay.** For an arrival the owner sends `WhiteHiltEscortPaid(shipId, cargo)` to the peer of each player it has loaded within
`RewardRange` (`Player.GetOwner()`), announces `SagaLog.Announce(SagaKind.Escort, "$whitehilt_tradeship", position)` once, and sends
the arrival notice (which only drops the pin; the players' text comes with the pay). Each paid client (`RPC_Paid`) adds
`EscortRules.Pay(Pay, cargo)` coins and rolls `RaidChartChance` for one Raid Chart (`Humanoid.PickupPrefab`, which shows the usual
pickup text; what does not fit in the pack is dropped at the player's feet with `ItemDrop.DropItem`, as `GroupPlacer.Give`
does), and shows `msg_whitehilt_escort_arrived` with the coins.

**Failure messages.** One routed RPC to everybody, `WhiteHiltEscortNotice(kind, shipId, contractorId, position, range)`: shown to players
within `AbandonRange x 2` of the ship and to the contractor wherever they are (compared with `Player.GetPlayerID()`), and every client
that has the merchant's pin drops it. The text is chosen by the number, never taken from the message.

**Interplay.**
- Pursuit cap: the trading ship is a hunted ship, so `Pursuit.Step` caps its speed to the raiders' speed times 0.85, never below
  `PursuitFloorSpeed` (4 m/s), and only while it is hunted; it cannot stop the ship (whether the merchant at `SailForceFactor` 0.8 is slower than
  the cap anyway was not measured). `Pursuit`'s HUD line and messages are for the local player standing on the hunted ship, so nobody
  sees them for an empty merchant.
- Friendly fire: `FriendlyFire` ignores hits only on Nail Ships (`RaiderShip.Is`), so the crew's arrows, pots and bolts hurt the
  merchant; ram and flood work as on any ship (it is not immune).
- The player's ship is not hunted, so it is not capped; Nail Ships never target it while the merchant lives (their target is fixed
  when they are raised), and `SeaBattleService`'s natural roll skips ships within `NoEncounterRange` of the merchant.

**Checks.** Build: exit 0, no new warnings. `dotnet test`: 853 passed, 0 failed (852 before). `cfg_keys.py --check`: 0 missing.

**Deviations**
- **A new spawn RPC** (`WhiteHiltEscortSpawn`) instead of the existing Nail Ship spawn order, for the reasons above.
- **`Paid(shipId, cargo)`, not `Paid(cargoShare)`.** A client that trusted a bare number could be sent coins by anyone. The
  receiver now checks the ship's data: the sender owns it, it is a trading ship, it is within `RewardRange` plus 50 m, it was not paid
  before, and the cargo is no more than the data's cargo plus 5 %. The ship's data is read, not its state, since the state may not
  have reached the client yet.
- **One notice RPC** for every message of the owner (waves, plunder, failures, the pin), so the contractor is reached wherever they
  are, as the plan asks, without finding a peer by player id.
- **No `OnDestroy` fallback for a sinking.** The hull's `m_onDestroyed` covers every way the game destroys a hull. A ship that
  vanishes because its owner left and nobody else loaded it sends no message; the contractor's pin goes after 60 s and the server's
  memory forgets it after 60 s, but there is no fail text. (Checking in `OnDestroy` is unreliable, since the ZDO is already gone.)
- **Players beyond the owner's loaded area are not seen.** The owner looks at `Player.GetAllPlayers()`, the players its machine has
  loaded; an `AbandonRange` or `RewardRange` above that (the settings allow 1000 m and 500 m) counts only players the machine knows.
- **The deed is seen only within `[Saga] WitnessRange` (50 m)** of the ship, as every deed is; the pay goes to players within
  `RewardRange` (150 m). Players between the two are paid but get no saga line.
- The admin `escort wave` increments `WavesSent` like a normal wave (it uses up the next planned wave); once all planned waves are
  sent it still sends one, and `WavesSent` then passes the wave count.

**Not verified (needs the game).** That raiders board the merchant and the plunder ticks; that the wave arrives from ahead and the horn
sounds; that the pay reaches every player in reach and the chart roll works, also with a full pack; that the pin goes and the messages
show to the contractor far away; that a second wave works with the first still alive; the behaviour after an owner change in the
middle of an escort; that nothing is left in the world after an escort ends.

### Phase 5: documentation (2026-10-14)

**Done.** Only documents changed in this phase; no code. `docs/sea-battles.md`: an intro paragraph, a new section *Escorts* after
*Raids* (the contract item and its recipe row, *The contract*, *The trading ship*, *The attacks*, *The end*, *The pay*), a deed row
in *Saga deeds*, an *Escorts* bullet in *Multiplayer*, the `escort` and `escort wave` rows and the `status` note in *Console*, the
full `[SeaBattles.Escorts]` table (all 19 keys with default, range and meaning) and `RenownEscort` in the closing paragraph.
`docs/ideas.md`: the *Escorts* bullet is gone (the sentence about a horn for another companion keeps its own mention of an escort,
which is a different idea). `CHANGELOG.md`: a new `## Unreleased` at the top with `### Added`, `### Changed` and `### Fixed`.
`docs/saga.md` (not in the plan): the deed row, `WitnessRange` and `RenownEscort`, so the saga page does not disagree with the
sea battles page. `README.MD`, `Package/` and the version numbers are untouched; `docs/README.md` and `docs/architecture.md` needed
no change (the page's one-line description still fits, and no pattern the guide describes moved).

Every number, default and range in the new text was read from `EscortSettings`, `EscortRules`, `TradeShip` and `EscortService`, and not
from this plan. A read-only check of the anchors the new text uses (`#escorts`, `#fleets`, `#patching-and-bailing`, `#saga-deeds`)
found no broken link.

**Things in the code worth knowing (reported, not changed)**
- The CHANGELOG had no entry for the two commits after v0.131.0 (*Nail Ships no longer damage themselves or each other*, *Hunted ships
  cannot outrun the Nail Ships at first*); only the Norwegian labels for the pursuit settings are mentioned, under *Fixed*.
- The hull-sinking announcement depends on `WearNTear.m_onDestroyed`; a merchant that vanishes because its owner left and no one
  loaded it gives no fail text, only a lost pin (see phase 4).
- The server's memory of merchants and cooldowns is lost on a restart, like the Nail Ship cooldown.

## What must be tested in the game

1. **Haldor.** In Black Forest after Bonemass: the Escort Contract is on his list for 300 coins, with the blue map icon and the tooltip.
   Before Bonemass it is not. Change `ContractPrice` / switch `Enabled` off with the world loaded: the list follows.
2. **Using it.** Aboard a ship in deep ocean: *A trading ship joins you*, a longship with a blue and cream sail and barrels and crates on
   deck appears 25 m to the side, facing a **Merchant's port** pin 1200 to 2000 m away, and one contract is gone. Refusals keep the
   contract: ashore or on the Nail Ship (*aboard your own ship at open sea*), too near land (*no clear deep-water passage*), a second call
   at once or within 10 minutes (*already have a merchant*). `whitehilt_seabattle escort` as admin does it without a contract.
3. **Sailing.** It sails by itself to the pin at a speed you can keep up with, steers round shallows and rocks, stops rather than ground
   on a coast. Its helm and hold refuse (the two messages), it is not pushable or mooring-post-able, the hover text shows cargo and
   distance. Look at the sail and the deck props: nothing floating or in the way on the deck.
4. **Waves.** After about a quarter of the route a horn sounds and Nail Ships (a fleet once Moder is down) come from ahead, lie
   alongside the merchant and board it; a second wave later. `whitehilt_seabattle escort wave` sends one at once, also while a first is
   alive. Pots light the merchant's deck, bolts and arrows hole it: patch it. No natural Nail Ship near it.
5. **Plunder.** Raiders on the merchant's deck lower its cargo (hover text) about 0.5 % a second each, and *Raiders on the merchant's
   deck!* shows once. Killing them stops it. At 0 % the contract fails.
6. **Ending.** In port (60 m of the pin): the ship stops, every player within 150 m gets coins by the cargo, maybe a Raid Chart,
   *safe in port*, a saga line (within 50 m); a minute later it sails off and goes. Also try: letting it sink (fire, bolts, a ram:
   fail text and the pin goes), sailing 300 m away for two minutes (abandoned, and it is gone), and keeping a player on its deck when it
   has arrived. Check the pack-full case (coins at your feet).
7. **Two players / dedicated server.** Both see the ship, the waves and the messages; the one who did not call it is paid too; the
   contractor far away still gets the fail text; the server log shows `Escort: ...` lines; the merchant's owner changes (the first
   player leaves the area) and the escort carries on with the right cargo and waves; a client without the mod version is not
   compatible. Restart the server mid-escort: nothing is left over, and the cooldown starts fresh.
8. **Settings.** `Waves` 0, `Enabled` off, `NoEncounterRange` 0, a short `AbandonSeconds`, a high `PlunderPerRaider`.
