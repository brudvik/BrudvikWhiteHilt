# 🏡 Plan: the household (farmhands and huskarls)

Plan for **people who live and work on your farm**. Today everything in a White Hilt base is done by the player: the
smelters are filled by hand, the fields are picked and sown by hand, the animals are fed by hand and the base is
defended by whoever happens to be home. The household adds people who do it: **farmhands** who carry, harvest and
tend, and **huskarls** who guard. They are hired at the trading posts or freed from the draugr harbours, and they
live in your house: they need a bed, food, comfort and wages. They can be wounded and they can die.

It builds on what is there: the player body of the traders (`TradingPosts/TradingPostTrader.cs`), the dog's follow,
home and rest (`Companions/`), production status (`Production/ProductionReader.cs`), crafting from nearby chests and
the chests (`Chests/`), planting and crops (`Planting/`, `Mastery/Crops.cs`), the Feeding Trough and animal care
(`Ranching/`), comfort counting (`Comfort/ComfortService.cs`), the Alarm Bell and the guestbook's raid detection
(`Pieces/Defenses/Siege/AlarmBell.cs`, `Guestbook/`), the trading posts' stores and the raid harbours
(`TradingPosts/`, `SeaBattles/Raids/`) and the Saga (`Saga/`).

The user chose (2026-10-17): all four jobs (production, fields, animals, guard), both ways of recruiting (hiring and
freeing captives), **heavy** upkeep (food, bed, comfort and wages; they can die) and **text only** (no new voices: all
30 ElevenLabs voice slots are used).

Written for an AI agent that carries it out phase by phase without the conversation behind it. Read all of it once
before you start. Like the trading posts, the work starts with rounds agreed with the user (section 6) before code.

## 1. What the player gets

- **The High Seat** (*Høgsetet*). A carved seat between two high-seat pillars, the master's place in the hall. It
  marks the farm: everything within its reach (`FarmRadius`, 80 m) is the farm the household works on, shown as a
  ring like the Husvette's. Sitting in it opens the **household panel**: who lives here, what each one does, their
  needs and their mood, and the farm's stores of food and coins for wages. One High Seat per farm; several farms per
  world are allowed.
- **Farmhands** (*gardsfolk*). Men and women in the player's body, each with a Norse name, their own look and a job
  set in the panel:
  - **Production**: carries ore, coal, wood, honey jars, barley and the rest from the farm's chests to smelters,
    kilns, the blast furnace, the spinning wheel, windmills, fermenters, the eitr refinery and the cooking stations,
    and carries what is done back to the chests. Takes from and puts into chests the way crafting from nearby chests
    reads them, and never touches a chest marked as private (see section 2).
  - **Fields**: picks ripe crops within the farm and sows the same seed again in the same place; picks berry bushes
    and mushrooms planted with the cultivator. Puts the harvest in the chests.
  - **Animals**: fills the Feeding Troughs from the chests, milks the lox, gathers eggs and keeps tamed animals
    within the farm (a tethered or penned animal stays put).
- **Huskarls** (*huskarer*). Armed guards. They wear the weapons and armour you hand them (from your inventory, like
  the dog's collar), walk a round of the farm by day and keep watch by night, keep torches and fires burning, ring the
  Alarm Bell when the guestbook sees a raid, and fight. They follow you if you call them, like the dog.
- **Upkeep (heavy).** Each one needs, every day of the world clock:
  - **a bed** of their own within the farm (claimed in the panel, or by the first free one), which they go to at night;
  - **food**: two meals a day, eaten from the farm's pantry chests (any food; better food is liked better);
  - **comfort** at their bed, counted the game's way (the Husvette counts);
  - **wages**: coins a day (`WagePerDay`), taken from the farm's coin chest; a freed captive works without wages for
    the first `FreedGraceDays` days.

  These give **mood** (0–100). A content household works faster; an unhappy one works slowly, then stops, and a
  member who has been miserable for `LeaveAfterDays` days leaves and is written in the Saga.
- **Danger.** They are on the players' side: monsters, raids and the blood moon go for them. Badly hurt they **fall
  and lie wounded** for a while (a farmhand or huskarl can be lifted and carried to a bed, like the dog). If struck
  while down, or if the setting says so, they die: a grave with their gear in a chest, a memorial stone can be raised
  to them, and the Saga tells it.
- **Getting them.**
  - **Hiring**: every trading post's trader sells a **Hiring Contract** (*leigebrev*). Used at your High Seat, a
    farmhand or huskarl arrives at the farm the next morning. Price per biome, rising with the number you already have.
  - **Freeing captives**: the draugr harbours hold one or two captives in chains. Free them and they make their own
    way to your High Seat (they arrive the next morning), work `FreedGraceDays` days without wages and start in a
    better mood.
- **Growing.** Each person has a skill in their job that rises as they work (faster carrying, more per trip, better
  aim). It shows in the panel; the highest is written in the Saga when it reaches a milestone.
- **Talk.** Short lines over the head, as the traders have them: greetings, complaints when a need is unmet, warnings
  in a raid. English and Norwegian text, no voice.

## 2. Decisions already made

| Decision | Choice | Why |
|---|---|---|
| Jobs | Production, fields, animals, guard, all in the first version | The user's choice |
| Recruiting | Hiring at the trading posts and freeing captives in the draugr harbours | The user's choice |
| Upkeep | Heavy: bed, food, comfort, wages; mood; they can leave and they can die | The user's choice |
| Voices | Text only | The user's choice; no free ElevenLabs voice slots |
| Body | A clone of the player prefab, as the traders, but **keeping movement** with `Humanoid` + `MonsterAI` in place of `Player` | The look matches the traders and the player; to be proved in phase 0 |
| Saved state | All in the person's ZDO (role, farm, bed, mood, needs times, skill, name, look); the farm's in the High Seat's ZDO | Multiplayer rule; nothing in fields |
| Simulation | The ZDO's owner runs the person's work loop | Multiplayer rule |
| Time | Needs and wages are counted on the world clock (`ZNet.instance.GetTime()`); when the farm loads after a while, the days missed are settled at once | Works while nobody is near |
| Work while away | **Continuous**: when the farm loads after a while, the work they would have done is settled at once (section 4) | The user's choice (2026-10-17) |
| Names | The household / *husstanden*, farmhands / *gardsfolk*, huskarls / *huskarer*, the High Seat / *Høgsetet*, Hiring Contract / *leigebrev* | The user's choice |
| Biome | The High Seat in the Black Forest (Fine Wood, Core Wood, Bronze nails); farmhands from the start; huskarls from the Swamp (`ProgressionTier.Swamp`) | The user's choice |
| Death | On by default (`CanDie`) | The user's choice |
| Private chests | A chest is the household's to use if it is within the farm and not marked private; the panel or the chest's hover marks it | The player must be able to keep things away from them |
| Performance | Work targets listed every few seconds per farm (not per person), cached and shared; one task per target at a time (reservations); measured with `whitehilt_perf` | A large base must not cost frames |

## 3. Settings (`[Household]`, admin-only)

| Setting | Default | What it does |
|---|---|---|
| `Enabled` | true | The whole feature |
| `FarmRadius` | 80 | Reach of a High Seat, in metres (15–200) |
| `MaxPerFarm` | 6 | Most people a High Seat holds |
| `ContractPrice` | 250 | Price of a Hiring Contract in the Meadows; later biomes multiply it |
| `PriceRisePerMember` | 0.25 | How much dearer each contract is per member you already have |
| `WagePerDay` | 10 | Coins a member takes a day |
| `MealsPerDay` | 2 | Food items a member eats a day |
| `FreedGraceDays` | 5 | Days a freed captive works without wages |
| `LeaveAfterDays` | 3 | Days of misery before a member leaves |
| `CanDie` | true | Whether they can die; off, they only fall wounded |
| `WoundedMinutes` | 3 | How long a fallen member lies before getting up |
| `WorkSpeed` | 1.0 | Scales how fast all of them work |
| `CaptivesPerHarbour` | 1–2 | Captives in each draugr harbour |
| `AwayCapDays` | 10 | Most days of work settled when the farm loads again |

## 4. How it works (for the agent)

- **The body.** `CreateClonedPrefab(name, "Player")`; remove `Player`, `PlayerController` and what only a player
  needs, add `Humanoid` (fields copied from the removed `Player`: eye, animator events, hit effects), `MonsterAI`
  (faction `Players`, `m_enableHuntPlayer = false`), `CharacterDrop` off, `ZSyncTransform` and `ZSyncAnimation` kept.
  Looks from a table like `TraderLooks` (preview script writes it), randomised per person and stored in the ZDO.
- **The work loop.** A component per person, run by the owner from a `MonsterAI.UpdateAI` postfix as `RaiderCrew`
  does: if a need calls (night → bed, hungry → pantry), go to it; else ask the farm for a task of its job; walk there
  (`MoveTo`), play the act (emote or attack animation), finish. A carried load shows as an item in the hand.
- **The farm.** `HouseholdFarm` on the High Seat: lists its chests, stations, crops, troughs and beds every few
  seconds; hands out tasks with reservations; settles needs and wages on the world clock. Pure parts go in plain
  classes with tests (section 5).
- **Production tasks** read the station the way `ProductionReader` does (fuel, ore, queue full, output waiting) and
  use the station's own RPCs to add fuel and ore, so the owner of the station stays the one that changes it.
- **Field tasks** use `Pickable` on ripe crops and place the seed's `Plant` prefab as the cultivator does (cost: one
  seed from the chests).
- **Guard tasks** use `MonsterAI` combat with the gear in their own `Humanoid` inventory; the Alarm Bell's
  `RaidStarted` calls every huskarl of the farm to the bell.
- **Work while away.** The world only runs where a player is, so the household's work while nobody is near is
  settled when the farm loads again, by the High Seat's owner, before the work loop starts. The time away (capped by
  `AwayCapDays`) is gone through **day by day**, so a day without food or wages lowers mood and the work of the days
  after it:
  1. Needs: meals and wages taken from the stores; mood moved; anyone who would have left, leaves.
  2. Work capacity for the day: trips a day per member from job, skill, mood and `WorkSpeed`.
  3. Production: for each station in turn, as many products as its own speed (`m_secPerProduct`, fuel per product)
     and the day's trips allow, bounded by what the chests hold: inputs taken from the chests, outputs put back, the
     station left as full as the chests allow.
  4. Fields: each plot harvested once per grow time that fits in the day (one seed from the chests per sowing); the
     crop put in the chests and the plot left freshly sown.
  5. Animals: troughs topped up from the chests; milk and eggs gathered by the game's own timers.
  6. Huskarls: nothing (the game sends no raids where nobody is).

  The result is written to the panel as a short report (*while you were away: 120 iron bars, 60 barley, 14 meals, 80
  coins in wages*). Items that do not fit in the chests stay unmade. The pure part (the day loop and each job's
  arithmetic) goes in plain classes with tests; the game side only reads stations and chests and writes the result.
- **Captives** are static player bodies like the traders, kneeling in chains, placed by `RaidSites`; freeing them
  sends an RPC to the server, which writes them to the High Seat of the freeing player as *arriving tomorrow*.

## 5. Tests (pure logic, in `BrudvikWhiteHilt.Tests`)

- Mood from needs met and unmet over days, and the days a member stays when unhappy.
- Settling missed days: meals and wages taken, and what happens when the stores run out partway.
- Contract price by biome and members.
- Task choice and reservations: two people never get the same target; nearer and more urgent comes first.
- Settling time away: day by day, capped; stores that run out partway; outputs that do not fit; production bounded
  by station speed, trips and inputs; harvests per grow time.
- Name generator: Norse names, no repeats on one farm.

## 6. Rounds and phases

### Round A: the look (pictures only)
- The High Seat with its pillars (preview script in `AssetSource/Preview/`, as for the Husvette).
- A row of farmhands and huskarls, men and women, in plain wool and linen for farmhands and in mail for huskarls.
- A captive in chains.

Rounds with the user until agreed.

### Phase 0: proof of the walking body (go or no-go)
A test prefab that walks to a point, runs, sits, lies in a bed, swings a weapon and is seen the same on two clients
and on a dedicated server. If `Humanoid` on the player body will not work, stop and talk to the user: the fallback is
the dvergr body (`Dverger`) in new clothes.

### Phase 1: a person
The prefab, looks, names, the ZDO state, hover text and the interact menu (follow, stay, go home, hand gear), reusing
the dog's patterns in `Companions/`.

### Phase 2: the High Seat and the panel
The piece, the farm ring, the household panel (members, job, bed, needs, mood), private chests.

### Phase 3: needs, mood and wages
Beds and sleep, pantry meals, comfort at the bed, wages, settling missed days, leaving.

### Phase 4: the task framework
Scanning the farm, tasks and reservations, the work loop, carrying, `whitehilt_perf` timings.

### Phase 5: production. Phase 6: fields. Phase 7: animals. Phase 8: huskarls, wounds and death.

### Phase 8½: work while away
The day-by-day settlement and the report in the panel (section 4).

### Phase 9: recruiting
The Hiring Contract on every trader's list; captives in the draugr harbours.

### Phase 10: Saga, docs, release
Saga deeds (hired, freed, fell, left, milestones), `docs/household.md`, `docs/README.md`, the README card, the
changelog, Norwegian texts throughout.

## 7. Confirmed with the user

Names, biome, continuous work while away and death on by default (2026-10-17), see section 2.

## 8. Log

- 2026-10-17: plan written; the user chose all four jobs, both ways of recruiting, heavy upkeep and text only.
- 2026-10-17: the user agreed to the names and the biome, and chose continuous work while away and death on by
  default. Section 4 describes how the time away is settled.
- 2026-10-17, round A draft 1: `AssetSource/Preview/build_household.py` renders `household.png` (four farmhands,
  two huskarls, the captive) and `highseat.png` (the High Seat with its pillars). Waiting for the user's comments.
- 2026-10-17, round A draft 2: huskarls in the game's iron helmet (exported with `export_vanilla.py
  HelmetIron:attach=helmet_iron`); the captive's chains run from iron cuffs on the wrists to the stake; the High Seat
  on two steps with carved fronts, a cushion and fur, carved arms with small dragon heads, crossed gable boards over the
  back, pillars carved on two faces with dragon heads, and a log wall with shields behind.
- 2026-10-17: the user approved the look (draft 2). Next: phase 0.
- 2026-10-17, phase 0 written (not yet tried in the game): `Household/HouseholdBody` clones the player prefab, puts a
  `Householder` (a `Humanoid` with the player's sit and lie-down, `AttachStart`) and a `MonsterAI` in place of `Player`,
  and strips the player's own behaviours; `HouseholdProbe` carries out goals from `whitehilt_household` (spawn, go,
  follow, stay, sit, sleep, stand, status, remove; cheats on). Builds, tests pass. The test body's clothes are not
  saved (a non-player's inventory is not), so they are gone after a reload; phase 1 saves the gear.
- 2026-10-17, phase 1 written (not yet tried in the game): `HouseholdMember` keeps name (`HouseholdNames`, tested),
  role, home, the player followed and the gear (the inventory saved as a package in the network data, loaded and put on
  again at Start) in the ZDO; `HouseholdLooks` writes body, hair and beard from a seed and makes the four sets of farm
  clothes (dyed with `TraderClothes.Add`, now internal). The use key makes them follow or wait, alt + use sends them
  home, an item used on them is put on and what they wore goes back to the player. Whoever talks to them takes
  ownership and reads the gear from the ZDO first. Taking gear back without handing something else is left for the
  household panel (phase 2). Lines are seen only on the machine of the player who talked to them.
- 2026-10-18, tried in the game: phases 0 and 1 work. First they wandered and ignored follow and wait: an untamed
  `MonsterAI` checks avoid-fire before follow and circles any fire within 3 m. `Householder` now sets `m_tamed` before
  the base Awake (tame by default; friend and foe as for a tamed wolf) and raises no skills (no `Tameable`). After
  that the user confirmed follow, wait and go home work. Next: phase 2.
- 2026-10-18, phase 2 written (not yet tried in the game): the High Seat (`piece_whitehilt_highseat`, layout
  `highseat` from `build_household.py` in `defenses.json`, without the preview's hall wall, shields, floor rug and turned
  knob; Fine Wood 12, Core Wood 8, Bronze Nails 10; comfort 2 as a chair). `HighSeat` shows the farm's ring, refuses a
  second seat within `FarmRadius`, sits the player down and opens `HouseholdPanel` (alt opens it without sitting): each
  member with role, state, bed and lent gear, and buttons for a bed (the next free one on the farm: no player owner, no
  other member), taking lent gear back (items handed over carry `whitehilt_hh_given`; a member's own clothes stay in
  their bag and go on again) and home (before the seat). Members keep their farm and bed as ZDOIDs. Settings
  `[Household]` Enabled, FarmRadius, MaxPerFarm. Console: `spawn` joins the farm the body stands on, `join` does it for
  one already there. Private chests are moved to phase 4, where the household first takes from chests.
- 2026-10-18, first look in the game: the seat came out near black (the game multiplies a tint into the wood's own
  brown texture), so `high_seat_piece` maps the preview's colours to lighter game tints (`GAME_TINTS`); texts used
  `{0}` where the game's `Localize` fills `$1`; the player sat high above the cushion, so the place to sit moved down
  to the top step. The user asked for a larger farm: `FarmRadius` is 80 m by default, up to 200.
- 2026-10-18: the farm and the bed were forgotten after a restart: the game gives network ids anew on every load.
  A High Seat now gets a number of its own (`whitehilt_hs_id`, set by its owner at Start) which members keep
  (`whitehilt_hh_farm_id`); a bed is kept by its place (`whitehilt_hh_bed_at`, matched within 0.3 m). Members used to
  struggle to their bed: `HouseholdDoors` opens a closed door ahead of where the AI wants to go (the mod's auto-door
  closes it again; where it does not, the member shuts it behind them; key doors stay shut), and `HouseholdProbe`
  steers straight when the game finds no path and sits or lies down when stuck within 4 m of the chair or bed.
- 2026-10-18: a door detour, since the game's path finding likely takes a shut door for a wall and never leads a body
  to it: a member stuck for 2.5 s picks the shut door within 16 m that lies best on the way (shortest way there and on
  to the goal, each door tried once per goal), walks to its near side, opens it and goes on; it gives up on the door
  after 15 s. Not yet tried in the game; a last-resort move to the bed when unseen is held back until it is needed.
