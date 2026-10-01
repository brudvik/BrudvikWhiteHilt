# 🧪 Potions

[← Back to the README](../README.MD)

## 🧪 POTIONS

All potions are crafted in the **Cauldron** as Mead Base, then fermented in the **Fermenter** to produce the final mead.

### Timed Effects (20 minutes duration)

| Potion | Effect | Mead Base Requirements |
|--------|--------|------------------------|
| **Gift of Freya** | Grants immense stamina - running, jumping, attacking, blocking, dodging, swimming, sneaking and building restore a little stamina instead of draining it, stamina refilled on drink, +40 stamina regeneration. | Honey ×20, Raspberries ×20, Blueberries ×20 |
| **Gift of Loki** | Grants endless Eitr - fills your Eitr and greatly boosts Eitr regeneration (requires Eitr from food). | Neck Tail ×20, Raspberries ×20, Eitr ×1 |
| **Gift of Odin** | God mode - 500 max HP, full heal on drink, heals 20 HP every frame, 21x health regeneration, 50% less fall damage. | Mushroom ×20, Raspberries ×20, Blueberries ×20 |
| **Gift of Sleipnir** | Grants the speed of Odin's horse - +50% movement speed, no fall damage, higher jumps. | Honey ×10, Thistle ×10, Lox Meat ×5 |
| **Gift of Ratatoskr** | Grants squirrel agility - +75% movement speed, -80% run stamina drain, higher jumps, no sneak stamina. | Resin ×5, Blueberries ×5, Mushroom ×5 |
| **Gift of Njord** | Grants the blessing of the sea god - +100% swim speed, no swim stamina, cannot drown. | Raw Anglerfish ×10, Chitin ×5, Bloodbag ×5 |
| **Gift of Surt** | Grants fire giant power - immune to fire and frost damage. | Surtling Core ×5, Coal ×20, Flametal ×2 |
| **Gift of Skadi** | Grants winter goddess blessing - immune to frost, removes freezing/cold effects. | Wood ×5, Stone ×5, Mushroom ×5 |
| **Gift of Baldur** | Grants near-invisibility - enemies can barely detect you, -99% noise, no sneak stamina. | Wood ×5, Stone ×5, Resin ×5 |
| **Gift of Thor** | Grants thunder god strength - double chopping and mining damage, -50% attack stamina, -90% building/farming stamina, lightning immune. | Thunderstone ×3, Iron ×10, Honey ×10 |
| **Gift of Brokkr** | Grants dwarf-smith skill - +25 to all skills (max 100), no stamina for building. | Wood ×5, Raspberries ×5, Blueberries ×5 |
| **Gift of Tyr** | Grants war god steadfastness - no block stamina, -75% dodge stamina, 90% less knockback, +100 carry weight. | Wood ×5, Raspberries ×5, Resin ×5 |
| **Gift of Fenrir** | Grants wolf ferocity - +50% attack speed, -50% attack stamina, +25% movement speed, heals 15% of damage dealt. | Wood ×5, Mushroom ×5, Dandelion ×5 |
| **Gift of Freyr** | Grants fertility god blessing - 2x health/stamina regen, +150 carry weight, no building/farming stamina, +1 HP/s and +5 stamina/s. | Stone ×5, Raspberries ×5, Mushroom ×5 |
| **Gift of Mimir** | Grants ancient wisdom - reveals the map in a 200m radius on drink, then 150m around you every 5 seconds, and marks creatures within 100m on the minimap. | Yggdrasil Wood ×10, Sap ×10, Eitr ×2 |

### Extended Duration (40 minutes)

| Potion | Effect | Mead Base Requirements |
|--------|--------|------------------------|
| **Gift of Idunn** | Grants eternal youth - 5x health/stamina/eitr regeneration, +1 HP/s. | Resin ×5, Raspberries ×5, Dandelion ×5 |

### Special Effects (30 minutes or until triggered)

| Potion | Effect | Mead Base Requirements |
|--------|--------|------------------------|
| **Gift of Hel** | Grants death protection - when a hit would be fatal or health drops below 10%, heals to full instead (one-time use). | Stone ×5, Blueberries ×5, Dandelion ×5 |

### Instant/Permanent Effects

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
| Fenrir | `AttackSpeed` 1.5, `LifeSteal` 0.15, `SpeedModifier` 0.25, `AttackStaminaMultiplier` 0.5 |
| Freya | `BonusStamina` 400, `StaminaUse` -0.9 (negative restores stamina), `StaminaRegenBonus` 40 |
| Freyr | `HealthRegenMultiplier` 2, `StaminaRegenMultiplier` 2, `CarryWeight` 150, `HomeItemStaminaMultiplier` 0, `HealPerSecond` 1, `StaminaPerSecond` 5 |
| Hel | `DurationMinutes` 30, `TriggerHealthFraction` 0.1 |
| Hugin | `SkillLevel` 100 (no duration) |
| Idunn | `DurationMinutes` 40, `HealthRegenMultiplier` 5, `StaminaRegenMultiplier` 5, `EitrRegenMultiplier` 5, `HealPerSecond` 1 |
| Loki | `BonusEitr` 500, `EitrRegenBonus` 80 |
| Mimir | `InitialRevealRadius` 200, `RevealRadius` 150, `RevealIntervalSeconds` 5, `CreatureRange` 100, `CreatureRefreshSeconds` 1 |
| Njord | `SwimSpeedModifier` 1, `SwimStaminaMultiplier` 0, `MinSwimStamina` 20, `SwimStaminaRefill` 50 |
| Odin | `MaxHealth` 500, `FallDamageMultiplier` 0.5, `HealthRegenBonus` 20, `HealPerFrame` 20 |
| Ratatoskr | `SpeedModifier` 0.75, `RunStaminaDrainModifier` -0.8, `JumpModifier` 0.5, `SneakStaminaMultiplier` 0 |
| Skadi, Surt | `DurationMinutes` only |
| Sleipnir | `SpeedModifier` 0.5, `JumpModifier` 1.5, `FallDamageMultiplier` 0 |
| Thor | `HomeItemStaminaMultiplier` 0.1, `AttackStaminaMultiplier` 0.5, `ChopDamageMultiplier` 2, `PickaxeDamageMultiplier` 2 |
| Tyr | `CarryWeight` 100, `BlockStaminaMultiplier` 0, `DodgeStaminaMultiplier` 0.25, `PushForceMultiplier` 0.1 (knockback taken) |

Changes apply on the next drink. The foraging meads are set in [Foraging & food](foraging.md).
