# 🌑 Difficulty, beasts & blood moon

[← Back to the README](../README.MD)

## 🌑 DIFFICULTY, BEASTS & BLOOD MOON

The world grows more dangerous as you progress. The server combines four things into a **pressure** from 0 to 1 and sends it to every player. The value changes slowly, so it does not jump when someone logs in or out:

| Factor | Measured as | Full at | Weight |
|--------|-------------|---------|--------|
| Players | Players online (the game already strengthens creatures near several players) | 5 | 0.15 |
| Time | Days in the world | 120 | 0.20 |
| Biomes | Biomes visited by the furthest player online | 7 (Meadows to Ashlands) | 0.45 |
| White Hilt gear | Share of players online wearing a White Hilt weapon or at least two White Hilt armour pieces | 100% | 0.20 |

**Stars**
- The vanilla chance of 1 and 2 stars goes from 10% up to 20% per star at full pressure.
- A creature that reaches 2 stars may go further. At full pressure the chance is 25% for a 3rd star, 20% for a 4th and 15% for a 5th, so 5 star creatures are very rare.
- The biomes you have visited set the highest star count: 4 biomes give 3 stars, 5 give 4 and 7 give 5.
- Each biome also has a limit: the Meadows stay at 2 stars, the Black Forest has at most 3 and the Swamp at most 4.
- Only one creature with 4 or 5 stars comes per spawn group.
- Bosses, tame animals, fish and raids are never changed.

**Strength, loot and size**
- Every new creature gets up to 20% more health and 10% more damage from the pressure.
- Stars above 2 add less than vanilla stars: +50% health and +25% damage each. A 5 star creature has 4.5× health and 2.75× damage, never more than 6× and 3×.
- Loot is 5×, 6× and 7× for 3, 4 and 5 stars, not 8×, 16× and 32×.
- Creatures with 3 to 5 stars grow 8%, 16% and 25% on top of the vanilla 2 star size. Large creatures such as trolls grow half as much, and nothing grows indoors.
- 3 to 5 stars are shown over the creature.

**Beasts of the dark hour**

Between 00:00 and 02:00, in bad weather (rain, storm or thunder), a black 5 star beast may come for a player who is outside, away from their base and not in the Meadows. The dark hour lasts only a minute or two of real time; the beast then stays until dawn.
- The server rolls once per player per night: 10% at pressure 0, 20% at 0.5 and 30% at full pressure.
- Only one beast comes within 150 m.
- A beast has 1.5× the health of a 5 star creature, and 15% more damage within the 3× limit. It is 10% slower, so you can get away.
- It is about as strong as the biome's boss; bring friends.
- A beast only comes once its biome's boss is defeated.
- By day it sinks into the ground when no one is near.
- It always drops its black trophy. The trophies will get a use later.

| Beast | Biome | Needs | Health (base ×) |
|-------|-------|-------|-----------------|
| **Black Troll** | Black Forest (and the Meadows in a blood moon) | The Elder | 600 × 6.75 |
| **Black Abomination** | Swamp | Bonemass | 800 × 6.75 |
| **Black Stone Golem** | Mountain | Moder | 800 × 6.75 |
| **Black Fuling Berserker** | Plains | Yagluth | 800 × 6.75 |
| **Black Seeker Soldier** | Mistlands | The Queen | 1500 × 6.75 |
| **Black Morgen** | Ashlands | Fader | 1600 × 6.75 |
| **Black Serpent** | At sea, on a ship | Bonemass | 400 × 6.75 |
| **Black Bonemaw** | Ashlands sea, on a ship | Fader | 1100 × 6.75 |

**Blood moon**

A rare night, at most one in every 6 nights and only after a boss is defeated. The chance is 2% to 6% per night, depending on the pressure.
- The moonlight, fog and aurora turn red.
- Beasts come all night, and bad weather is not needed.
- The chance of a beast is three times as high, and each player gets up to two rolls.
- Every star chance is 1.5× higher.

**Console commands:** `whitehilt_difficulty` shows the pressure and what it is made of. Admins can also use `whitehilt_bloodmoon start|stop` and `whitehilt_beast [beast or biome]`.

Everything is set in the `[Difficulty]`, `[Difficulty.Stars]`, `[Difficulty.Size]`, `[Difficulty.Beasts]` and `[Difficulty.BloodMoon]` sections. `Difficulty.OverridePressure` fixes the pressure for testing. The star part turns itself off while Creature Level and Loot Control is installed.

If the mod is removed, creatures with 3 to 5 stars that are still in the world use the vanilla formulas (up to 6× health and 32× loot). Beasts sink into the ground at dawn, so few are left.
