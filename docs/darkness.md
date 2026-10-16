# 🕯️ Creeping darkness

[← Back to the README](../README.MD)

## 🕯️ CREEPING DARKNESS

Below ground the dark is not to be trusted. In a crypt, a cave, a mine, a troll cave or any other dungeon the light now
and then flickers, fades and goes almost black for a while, then comes back. Only a light of your own holds it off. Out
of the light fear grows, and something comes.

### Where

Everywhere the game counts as inside: the burial chambers of the Black Forest, the sunken crypts of the Swamp, the frost
caves of the Mountain and the Deep North, the infested mines of the Mistlands and the mod's own dungeons. Troll caves,
which the game does not count as inside, are known by their cave weather.

### How it comes

The darkness of a place is the same for everyone in it, counted from when the first player went in:

- For the first **3 minutes** (`CalmMinutes`) nothing happens.
- Then it comes every **4 minutes** (`LongestGapMinutes`), and the longer the place has been in use, the more often:
  after **10 minutes** more (`RampMinutes`) every minute and a half (`ShortestGapMinutes`).
- Each darkness:

| Step | How long | What happens |
|------|----------|--------------|
| Flicker | 8 s | The place's lights waver and dip: *The lights flicker...* and the flames gutter in a draught |
| Fading | 5 s | The light sinks |
| Black | 15 s at first, up to 45 s (`ShortestBlackSeconds`, `LongestBlackSeconds`) | Almost black: *The darkness creeps in!* Every torch on the walls, every brazier and its flames are out |
| Returning | 3 s | *The light returns*, and the flames flare back |

Every gap and every black differs a little, by up to a fifth. Once a place has stood empty for 5 minutes, its darkness
starts over.

### What holds it off

- **A torch in hand** (or anything else alight in your hand).
- **A circlet**, such as the Dverger circlet, or any other light on your head.
- **A fire you built**: a campfire, a hearth, a sconce or a standing torch, burning, within 6 m. They stay lit in the
  dark, so it pays to make small camps on the way down.
- **A friend with a light**, within 5 m.

### Fear of the dark

A fear meter, shown as a status effect with its percentage, fills in the black without a light, to full in 75 seconds
(`FearSeconds`), and falls twice as fast in the light or when the dark lifts. It is gone when you leave.

| Fear | What happens |
|------|--------------|
| 25% | *You are afraid of the dark. Find a light!* Stamina comes back at half the pace |
| 50% | Your heart pounds, louder and faster as it grows, and the view trembles. A shadow may come |
| 75% | *Panic grips you!* You run a fifth more slowly |
| 100% | *The dark is killing you!* You lose 2% of your health a second (`FearDamage`) |

### Something is down there

From half fear, standing without a light in the black, once each darkness a **shadow** comes for you from behind: a
ghost gone black, with pale eyes. It hunts only players without a light and lets go of anyone who lights one. In the
dark it is very hard to hurt; within 6 m of a carried light it is easy to hurt. When the light comes back it is gone in
a puff. It grows with the world, a star for every two bosses fallen, up to two, and leaves nothing behind. There are at
most 3 in one place (`MaxShadows`). Turn them off with `Shadows`.

### The light fails

In the black a torch burns down three times as fast (`TorchWear`), and now and then it gutters out for a moment and
a half (*Your torch gutters!*). A circlet flickers while it is at its blackest.

### Eyes and sounds

In the black, every 6 to 14 seconds: a pair of pale eyes off in the dark that blinks once and is gone, claws scraping
along the stone, slow breathing right behind you, or whispers at your ear. None of it is real and no one else hears or
sees it. It is your own setting (`EyesAndSounds`), with its own volume (`Volume`). The sounds are made with ElevenLabs
(`AssetSource/Audio/audio.json`).

### Commands
- `whitehilt_darkness` or `whitehilt_darkness status` shows the place you are in, the darkness there, whether you
  have a light and your fear.
- `whitehilt_darkness start` (admin) brings the darkness to the place you are in now; `stop` lifts it, and it keeps
  away for the calm.

### Settings

Section `Darkness`. Everything but `EyesAndSounds` and `Volume` is admin-only and synced from the server.

| Setting | Default | What it does |
|---------|---------|--------------|
| `Enabled` | true | The darkness creeps in below ground at all |
| `CalmMinutes` | 3 | Minutes after the first player goes in before the first darkness |
| `LongestGapMinutes` | 4 | Minutes between darknesses at first |
| `ShortestGapMinutes` | 1.5 | Minutes between darknesses once it has crept in fully |
| `RampMinutes` | 10 | Minutes after the first darkness until it is at its worst |
| `ShortestBlackSeconds` | 15 | Seconds of black at first |
| `LongestBlackSeconds` | 45 | Seconds of black at its worst |
| `FearSeconds` | 75 | Seconds in the black without a light to full fear |
| `FearDamage` | 0.02 | Share of maximum health lost per second at full fear; 0 never hurts |
| `Shadows` | true | Shadows come for those without a light |
| `MaxShadows` | 3 | The most shadows in one place |
| `TorchWear` | 3 | How many times as fast a torch burns down in the black |
| `EyesAndSounds` | true | Eyes, scraping, breathing and whispers in the black (your own setting) |
| `Volume` | 1 | Volume of the darkness's sounds (your own setting) |

### How it works

The server remembers when each place was first gone into, keyed by the location it belongs to (`Location.GetLocation`,
which also finds dungeons), and forgets it once no one has said they are there for 5 minutes. The clients ask while
they are inside and work out the darkness from that moment and the place itself, so everyone in a place has the same
darkness at the same time without any network object being written.

On each client the ambient light, the fog's colour and the sun the game sets every frame are faded to black, and the
place's own lights within 50 m are dimmed (through `LightFlicker.m_multiplier` where they flicker) with their flame
particles hidden. Lights under a player and on pieces a player built are left alone. A player's lit state is a flag in
their network data, which the shadows read; the shadows are networked creatures run by the machine of the player they
came for, and they find their prey through a patch on `BaseAI.FindEnemy`.
