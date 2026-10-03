# 🧪 Potions

[← Back to the README](../README.MD)

## 🧪 POTIONS

All potions are crafted in the **Cauldron** as Mead Base, then fermented in the **Fermenter** to produce the final mead.

Several of them are brewed from what the mod adds to the world: lingonberries for Idunn, crowberries and roseroot for Skadi, sphagnum moss and bog bean for Eir, meadowsweet and angelica for Kvasir, juniper and angelica for Ullr, angelica for Heimdall, henbane, ergot and rock lichen for the Völva ([Foraging & food](foraging.md)), spider silk for Loki, who made the first fishing net, and a spider's poison gland for Hel ([Lindorm & giant spiders](monsters.md)).

### Timed Effects (20 minutes duration)

| Potion | Effect | Mead Base Requirements |
|--------|--------|------------------------|
| **Gift of Freya** | Grants immense stamina - running, jumping, attacking, blocking, dodging, swimming, sneaking and building restore a little stamina instead of draining it, stamina refilled on drink, +40 stamina regeneration. | Honey ×20, Raspberries ×20, Blueberries ×20 |
| **Gift of Loki** | Grants endless Eitr - fills your Eitr and greatly boosts Eitr regeneration (requires Eitr from food). | Neck Tail ×20, Spider Silk ×5, Eitr ×1 |
| **Gift of Sleipnir** | Grants the speed of Odin's horse - +50% movement speed, no fall damage, higher jumps. | Honey ×10, Thistle ×10, Lox Meat ×5 |
| **Gift of Ratatoskr** | Grants squirrel agility - +75% movement speed, -80% run stamina drain, higher jumps, no sneak stamina. | Resin ×5, Blueberries ×5, Mushroom ×5 |
| **Gift of Njord** | Grants the blessing of the sea god - +100% swim speed, no swim stamina, cannot drown. | Raw Anglerfish ×10, Chitin ×5, Bloodbag ×5 |
| **Gift of Surt** | Grants fire giant power - immune to fire and frost damage. | Surtling Core ×5, Coal ×20, Flametal ×2 |
| **Gift of Skadi** | Grants winter goddess blessing - immune to frost, removes freezing/cold effects. | Crowberries ×5, Roseroot ×5, Mushroom ×5 |
| **Gift of Baldur** | Grants near-invisibility - enemies can barely detect you, -99% noise, no sneak stamina. | Wood ×5, Stone ×5, Resin ×5 |
| **Gift of Thor** | Grants thunder god strength - double chopping and mining damage, -50% attack stamina, -90% building/farming stamina, lightning immune. | Thunderstone ×3, Iron ×10, Honey ×10 |
| **Gift of Brokkr** | Grants dwarf-smith skill - +25 to all skills (max 100), no stamina for building. | Wood ×5, Raspberries ×5, Blueberries ×5 |
| **Gift of Tyr** | Grants war god steadfastness - no block stamina, -75% dodge stamina, 90% less knockback, +100 carry weight. | Wood ×5, Raspberries ×5, Resin ×5 |
| **Gift of Fenrir** | Grants wolf ferocity - +50% attack speed, -50% attack stamina, +25% movement speed, heals 15% of damage dealt. | Wood ×5, Mushroom ×5, Dandelion ×5 |
| **Gift of Freyr** | Grants fertility god blessing - 2x health/stamina regen, +150 carry weight, no building/farming stamina, +1 HP/s and +5 stamina/s. | Stone ×5, Raspberries ×5, Mushroom ×5 |
| **Gift of Idunn** | Grants youthful vigour - 1.25x health and 1.5x stamina/eitr regeneration. | Lingonberries ×10, Honey ×5, Dandelion ×5 |
| **Gift of Kvasir** | The mead of poetry - every skill rises 50% faster. | Meadowsweet ×10, Honey ×10, Angelica ×3 |
| **Gift of Ullr** | The hunter god of bow and ski - +25% bow damage, +15% movement speed, harder to notice. | Juniper Berries ×5, Angelica ×5, Feathers ×10 |

### Short Duration (10 minutes)

| Potion | Effect | Mead Base Requirements |
|--------|--------|------------------------|
| **Gift of Odin** | The Allfather's vigour - full heal on drink, +50 max HP, +2 HP/s, 2x health regeneration, 50% less fall damage. The strongest health potion, but you can still die. | Mushroom ×20, Raspberries ×20, Blueberries ×20 |
| **Gift of Eir** | The healer among the gods - heals half your health on drink, ends poison, fire, frost, shock, tar and smoke and keeps them off, 2x health regeneration. | Sphagnum Moss ×10, Bog Bean ×5, Honey ×10 |
| **Gift of Heimdall** | The watchman who hears the grass grow - every foe within 60 m shows on the map. | Angelica ×5, Crowberries ×10, Crystal ×2 |
| **Gift of the Völva** | The seeress's draught - the 40 nearest things to pick within 50 m, and unopened chests the world placed there, show on the map. | Henbane ×4, Ergot ×3, Rock Lichen ×5 |

### Special Effects (30 minutes or until triggered)

| Potion | Effect | Mead Base Requirements |
|--------|--------|------------------------|
| **Gift of Hel** | Grants death protection - when a hit would be fatal or health drops below 10%, heals to full instead (one-time use). | Poison Gland ×2, Blueberries ×5, Dandelion ×5 |

### Instant/Permanent Effects

Hugin and Munin, like Gift of Brokkr, can only be brewed in full progression; in linear mode you learn and find things yourself (see [Settings & progression](progression.md)).

| Potion | Effect | Mead Base Requirements |
|--------|--------|------------------------|
| **Gift of Hugin** | Permanently sets ALL skills to level 100. Named after Odin's raven of "thought". | Neck Tail ×20, Cloudberry ×20, Blueberries ×20 |
| **Gift of Munin** | Permanently teaches ALL materials, revealing every recipe made from materials. Named after Odin's raven of "memory". | Neck Tail ×20, Raspberries ×20, Eitr ×1 |

## Config

One section per potion, `[Potions.GiftOf<Name>]` (e.g. `[Potions.GiftOfThor]`), admin only and synced from the server. Each has `DurationMinutes` (20 unless noted) and the keys below. Multipliers on stamina use: 0 = no stamina, 1 = vanilla.

| Potion | Keys (defaults) |
|---|---|
| Baldur | `StealthModifier` -0.99, `NoiseModifier` -0.99, `Stealth` 0.01, `SneakStaminaMultiplier` 0 |
| Brokkr | `SkillBonus` 25, `MaxSkillLevel` 100, `HomeItemStaminaMultiplier` 0 |
| Eir | `DurationMinutes` 10, `HealShare` 0.5, `HealthRegenMultiplier` 2 |
| Fenrir | `AttackSpeed` 1.5, `LifeSteal` 0.15, `SpeedModifier` 0.25, `AttackStaminaMultiplier` 0.5 |
| Freya | `BonusStamina` 400, `StaminaUse` -0.9 (negative restores stamina), `StaminaRegenBonus` 40 |
| Freyr | `HealthRegenMultiplier` 2, `StaminaRegenMultiplier` 2, `CarryWeight` 150, `HomeItemStaminaMultiplier` 0, `HealPerSecond` 1, `StaminaPerSecond` 5 |
| Heimdall | `DurationMinutes` 10, `Radius` 60, `RefreshSeconds` 2 |
| Völva | `DurationMinutes` 10, `SightRadius` 50, `MaxPins` 40, `RefreshSeconds` 4 |
| Hel | `DurationMinutes` 30, `TriggerHealthFraction` 0.1 |
| Hugin | `SkillLevel` 100 (no duration) |
| Idunn | `HealthRegenMultiplier` 1.25, `StaminaRegenMultiplier` 1.5, `EitrRegenMultiplier` 1.5, `HealPerSecond` 0 |
| Kvasir | `SkillGain` 0.5 |
| Loki | `BonusEitr` 500, `EitrRegenBonus` 80 |
| Njord | `SwimSpeedModifier` 1, `SwimStaminaMultiplier` 0, `MinSwimStamina` 20, `SwimStaminaRefill` 50 |
| Odin | `DurationMinutes` 10, `BonusMaxHealth` 50, `FallDamageMultiplier` 0.5, `HealthRegenBonus` 1 (1 doubles it), `HealPerSecond` 2 |
| Ratatoskr | `SpeedModifier` 0.75, `RunStaminaDrainModifier` -0.8, `JumpModifier` 0.5, `SneakStaminaMultiplier` 0 |
| Skadi, Surt | `DurationMinutes` only |
| Sleipnir | `SpeedModifier` 0.5, `JumpModifier` 1.5, `FallDamageMultiplier` 0 |
| Thor | `HomeItemStaminaMultiplier` 0.1, `AttackStaminaMultiplier` 0.5, `ChopDamageMultiplier` 2, `PickaxeDamageMultiplier` 2 |
| Tyr | `CarryWeight` 100, `BlockStaminaMultiplier` 0, `DodgeStaminaMultiplier` 0.25, `PushForceMultiplier` 0.1 (knockback taken) |
| Ullr | `BowDamage` 0.25, `SpeedModifier` 0.15, `StealthModifier` -0.3 |

Changes apply on the next drink. The foraging meads are set in [Foraging & food](foraging.md).
