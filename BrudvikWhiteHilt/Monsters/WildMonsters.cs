using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Monsters;

/// <summary>
/// The wild monsters of the Mistlands, the Ashlands, the ocean and the Nail flagships, and the names of their loot.
/// Numbers here are the defaults of the settings; the sizes and clip times come from the models (see the Phase A log in
/// the plan).
/// </summary>
internal static class WildMonsters
{
    // The Night Garm is the Garm Hound grown to a bear's size, and this much stronger.
    private const float NightGarmSize = 1.4f;
    private const float NightGarmStrength = 2.5f;

    /// <summary>Prefab name of Mist Stag Meat.</summary>
    public const string MistStagMeat = "WhiteHilt_MistStagMeat";

    /// <summary>Prefab name of the Mist Stag trophy.</summary>
    public const string MistStagTrophy = "WhiteHilt_TrophyMistStag";

    /// <summary>Prefab name of the Brood Scale.</summary>
    public const string BroodScale = "WhiteHilt_BroodScale";

    /// <summary>Prefab name of the Nidhogg Spawn trophy.</summary>
    public const string SpawnTrophy = "WhiteHilt_TrophyNidhoggSpawn";

    /// <summary>Prefab name of Living Heartwood.</summary>
    public const string LivingHeartwood = "WhiteHilt_LivingHeartwood";

    /// <summary>Prefab name of the Yggdrasil Warden trophy.</summary>
    public const string WardenTrophy = "WhiteHilt_TrophyYggdrasilWarden";

    /// <summary>Prefab name of the Garm Fang.</summary>
    public const string GarmFang = "WhiteHilt_GarmFang";

    /// <summary>Prefab name of the Garm Hound trophy.</summary>
    public const string GarmTrophy = "WhiteHilt_TrophyGarmHound";

    /// <summary>Prefab name of the Night Garm trophy.</summary>
    public const string NightGarmTrophy = "WhiteHilt_TrophyNightGarm";

    /// <summary>Prefab name of Shark Meat.</summary>
    public const string SharkMeat = "WhiteHilt_SharkMeat";

    /// <summary>Prefab name of the Greenland Shark trophy.</summary>
    public const string SharkTrophy = "WhiteHilt_TrophyGreenlandShark";

    /// <summary>Prefab name of the Draugr Jarl trophy.</summary>
    public const string JarlTrophy = "WhiteHilt_TrophyDraugrJarl";

    /// <summary>Prefab name of Mare Hair.</summary>
    public const string MareHair = "WhiteHilt_MareHair";

    /// <summary>Prefab name of the Mare trophy.</summary>
    public const string MareTrophy = "WhiteHilt_TrophyMare";

    /// <summary>Prefab name of Huldra Bark.</summary>
    public const string HuldraBark = "WhiteHilt_HuldraBark";

    /// <summary>Prefab name of the Huldra trophy, her cow's tail.</summary>
    public const string HuldraTrophy = "WhiteHilt_TrophyHuldra";

    /// <summary>Prefab name of the roast Mist Stag.</summary>
    public const string CookedMistStag = "WhiteHilt_CookedMistStag";

    /// <summary>Prefab name of the hákarl.</summary>
    public const string Hakarl = "WhiteHilt_Hakarl";

    /// <summary>Prefab name of the Brood Scale Charm.</summary>
    public const string BroodCharm = "WhiteHilt_BroodCharm";

    /// <summary>Prefab name of the Garm Fang Arrow.</summary>
    public const string GarmFangArrow = "WhiteHilt_GarmFangArrow";

    /// <summary>Prefab name of the Heartwood Draught.</summary>
    public const string HeartwoodDraught = "WhiteHiltHeartwoodDraught";

    /// <summary>The Mist Stag.</summary>
    public static readonly WildMonster MistStag = new()
    {
        Id = "miststag",
        PrefabName = "WhiteHilt_MistStag",
        EnglishName = "Mist Stag",
        Faction = Character.Faction.MistlandsMonsters,
        SoundPrefix = "miststag",
        ModelScale = 1.55f,
        SizeText = "about 2.6 m to the antler tips",
        CapsuleCenter = new Vector3(0f, 1.1f, 0f),
        CapsuleRadius = 0.8f,
        CapsuleHeight = 2.2f,
        Eye = new Vector3(0f, 1.9f, 1.2f),
        WalkSpeed = 3f,
        RunSpeed = 9f,
        TurnSpeed = 120f,
        RunTurnSpeed = 180f,
        Health = 900f,
        Modifiers = Mods(pierce: HitData.DamageModifier.Resistant, frost: HitData.DamageModifier.Resistant, spirit: HitData.DamageModifier.Weak),
        Attacks = new[]
        {
            new WildAttack
            {
                Name = "Charge", Trigger = "stag_charge", Description = "Pierce damage of the Mist Stag's charge.",
                Type = HitData.DamageType.Pierce, Damage = 80f, Range = 3f, Height = 1.6f, Angle = 70f, RayWidth = 0.8f, Interval = 4f, Force = 150f
            },
            new WildAttack
            {
                Name = "Stomp", Trigger = "stag_stomp", Description = "Blunt damage of the Mist Stag's stomp.",
                Type = HitData.DamageType.Blunt, Damage = 100f, Range = 2.8f, Height = 1.4f, Angle = 100f, RayWidth = 1f, Interval = 5f, Force = 60f
            }
        },
        Drops = new[] { new WildDrop { Item = MistStagMeat, Min = 2, Max = 3 } },
        Trophy = MistStagTrophy,
        // Pale as mist: washed-out colour, a little brighter, a cool tint.
        Look = new WildLook { Tint = new Color(0.82f, 0.9f, 1f), Saturation = -0.4f, Value = 0.2f },
        Biome = Heightmap.Biome.Mistlands,
        SpawnChance = 15f,
        SpawnSeconds = 300f,
        Configure = WildMonsterRegistry.ConfigureStag
    };

    /// <summary>The Nidhogg Spawn.</summary>
    public static readonly WildMonster NidhoggSpawn = new()
    {
        Id = "nidhoggspawn",
        PrefabName = "WhiteHilt_NidhoggSpawn",
        EnglishName = "Nidhogg Spawn",
        Faction = Character.Faction.MistlandsMonsters,
        SoundPrefix = "nidhogg",
        ModelScale = 6.5e-5f,
        SizeText = "about 5 m long",
        CapsuleCenter = new Vector3(0f, 1.1f, 0f),
        CapsuleRadius = 1f,
        CapsuleHeight = 2.2f,
        Eye = new Vector3(0f, 1.8f, 2f),
        WalkSpeed = 2.5f,
        RunSpeed = 8f,
        TurnSpeed = 100f,
        RunTurnSpeed = 140f,
        Health = 1800f,
        Modifiers = Mods(slash: HitData.DamageModifier.Resistant, pierce: HitData.DamageModifier.Resistant, fire: HitData.DamageModifier.Weak,
            poison: HitData.DamageModifier.Immune),
        Attacks = new[]
        {
            new WildAttack
            {
                Name = "Bite", Trigger = "spawn_bite", Description = "Slash damage of the Nidhogg Spawn's bite.",
                Type = HitData.DamageType.Slash, Damage = 95f, ExtraType = HitData.DamageType.Poison, ExtraDamage = 30f,
                Range = 3.6f, Height = 1.8f, Angle = 70f, RayWidth = 0.9f, Interval = 2.5f, Force = 40f
            },
            new WildAttack
            {
                Name = "Crush", Trigger = "spawn_crush", Description = "Blunt damage of the Nidhogg Spawn's crushing blow.",
                Type = HitData.DamageType.Blunt, Damage = 150f, Range = 3.4f, Height = 1.6f, Angle = 90f, RayWidth = 1.2f, Interval = 6f, Force = 120f
            }
        },
        Drops = new[] { new WildDrop { Item = BroodScale, Min = 2, Max = 3 } },
        Trophy = SpawnTrophy,
        // Near-black with a dark red cast, and a dull skin instead of the model's metallic shine. Its red eyes are left alone.
        Look = new WildLook { Tint = new Color(0.5f, 0.4f, 0.4f), Value = -0.25f, Glossiness = 0.12f, Matte = true },
        Biome = Heightmap.Biome.Mistlands,
        SpawnAtDay = false,
        SpawnChance = 10f,
        SpawnSeconds = 400f,
        Configure = WildMonsterRegistry.ConfigureSpawn
    };

    /// <summary>The Yggdrasil Warden.</summary>
    public static readonly WildMonster YggdrasilWarden = new()
    {
        Id = "yggdrasilwarden",
        PrefabName = "WhiteHilt_YggdrasilWarden",
        EnglishName = "Yggdrasil Warden",
        Faction = Character.Faction.MistlandsMonsters,
        SoundPrefix = "warden",
        ModelScale = 0.55f,
        SizeText = "about 4 m tall",
        CapsuleCenter = new Vector3(0f, 1.9f, 0f),
        CapsuleRadius = 0.9f,
        CapsuleHeight = 3.8f,
        Eye = new Vector3(0f, 3.4f, 0.4f),
        WalkSpeed = 1.2f,
        RunSpeed = 2.5f,
        TurnSpeed = 40f,
        RunTurnSpeed = 60f,
        Health = 3000f,
        Modifiers = Mods(pierce: HitData.DamageModifier.Resistant, fire: HitData.DamageModifier.VeryWeak, frost: HitData.DamageModifier.Resistant,
            lightning: HitData.DamageModifier.Weak, poison: HitData.DamageModifier.Immune),
        Attacks = new[]
        {
            new WildAttack
            {
                Name = "Slam", Trigger = "warden_slam", Description = "Blunt damage of the Yggdrasil Warden's slam.",
                Type = HitData.DamageType.Blunt, Damage = 160f, Range = 4.5f, Height = 2f, Angle = 120f, RayWidth = 1.5f, Interval = 4f, Force = 100f
            }
        },
        Drops = new[] { new WildDrop { Item = LivingHeartwood, Min = 1, Max = 2 } },
        Trophy = WardenTrophy,
        // Darker bark; the model's own emission map keeps its runes glowing.
        Look = new WildLook { Tint = new Color(0.75f, 0.68f, 0.6f), Value = -0.1f, Skip = new string[0] },
        Biome = Heightmap.Biome.Mistlands,
        SpawnChance = 6f,
        SpawnSeconds = 600f,
        WakeRange = 8f,
        CorpseSinkAfter = 12f,
        CorpseLifetime = 18f,
        Configure = WildMonsterRegistry.ConfigureWarden
    };

    /// <summary>The Garm Hound.</summary>
    public static readonly WildMonster GarmHound = new()
    {
        Id = "garmhound",
        PrefabName = "WhiteHilt_GarmHound",
        EnglishName = "Garm Hound",
        Faction = Character.Faction.Demon,
        SoundPrefix = "garm",
        ModelScale = 90f,
        SizeText = "about 1.5 m at the head and 2.6 m long",
        CapsuleCenter = new Vector3(0f, 0.8f, 0f),
        CapsuleRadius = 0.7f,
        CapsuleHeight = 1.6f,
        Eye = new Vector3(0f, 1.3f, 1f),
        WalkSpeed = 4f,
        RunSpeed = 10f,
        TurnSpeed = 200f,
        RunTurnSpeed = 300f,
        Health = 500f,
        Modifiers = Mods(fire: HitData.DamageModifier.Immune, frost: HitData.DamageModifier.Weak, spirit: HitData.DamageModifier.Weak,
            poison: HitData.DamageModifier.Resistant),
        Attacks = new[]
        {
            new WildAttack
            {
                Name = "Bite", Trigger = "garm_bite", Description = "Slash damage of the Garm Hound's bite.",
                Type = HitData.DamageType.Slash, Damage = 60f, ExtraType = HitData.DamageType.Fire, ExtraDamage = 45f,
                Range = 2.2f, Height = 1.2f, Angle = 70f, RayWidth = 0.5f, Interval = 1.6f, Force = 30f
            }
        },
        Drops = new[] { new WildDrop { Item = GarmFang, Min = 1, Max = 2 } },
        Trophy = GarmTrophy,
        // Charcoal. The yellow eyes are left alone.
        Look = new WildLook { Tint = new Color(0.55f, 0.5f, 0.5f), Value = -0.15f, Glossiness = 0.12f, Matte = true },
        Biome = Heightmap.Biome.AshLands,
        SpawnAtDay = false,
        SpawnChance = 15f,
        SpawnSeconds = 250f,
        MaxSpawned = 3,
        GroupMin = 2,
        GroupMax = 3,
        Configure = WildMonsterRegistry.ConfigureGarm
    };

    /// <summary>
    /// The Night Garm: the leader of the Garm Hounds, as big as a bear and two and a half times as strong, which only
    /// hunts by night in bad weather. It is the Garm Hound's model grown, darker, alone.
    /// </summary>
    public static readonly WildMonster NightGarm = new()
    {
        Id = "nightgarm",
        Model = "garmhound",
        PrefabName = "WhiteHilt_NightGarm",
        EnglishName = "Night Garm",
        Faction = Character.Faction.Demon,
        SoundPrefix = "garm",
        ModelScale = 90f * NightGarmSize,
        SizeText = "about 2.1 m at the head and 3.6 m long, as big as a bear",
        CapsuleCenter = new Vector3(0f, 0.8f, 0f) * NightGarmSize,
        CapsuleRadius = 0.7f * NightGarmSize,
        CapsuleHeight = 1.6f * NightGarmSize,
        Eye = new Vector3(0f, 1.3f, 1f) * NightGarmSize,
        WalkSpeed = 4f,
        RunSpeed = 10f,
        TurnSpeed = 160f,
        RunTurnSpeed = 240f,
        Health = 500f * NightGarmStrength,
        Modifiers = Mods(fire: HitData.DamageModifier.Immune, frost: HitData.DamageModifier.Weak, spirit: HitData.DamageModifier.Weak,
            poison: HitData.DamageModifier.Resistant),
        Attacks = new[]
        {
            new WildAttack
            {
                Name = "Bite", Trigger = "garm_bite", Description = "Slash damage of the Night Garm's bite.",
                Type = HitData.DamageType.Slash, Damage = 60f * NightGarmStrength, ExtraType = HitData.DamageType.Fire, ExtraDamage = 45f * NightGarmStrength,
                Range = 2.2f * NightGarmSize, Height = 1.2f * NightGarmSize, Angle = 70f, RayWidth = 0.5f * NightGarmSize, Interval = 1.6f, Force = 60f
            }
        },
        Drops = new[] { new WildDrop { Item = GarmFang, Min = 3, Max = 5 } },
        Trophy = NightGarmTrophy,
        TrophyChance = 25f,
        // Near black, with the glow of the eyes left alone.
        Look = new WildLook { Tint = new Color(0.3f, 0.27f, 0.28f), Value = -0.3f, Glossiness = 0.1f, Matte = true },
        StarScale = 0.1f,
        Biome = Heightmap.Biome.AshLands,
        SpawnAtDay = false,
        BadWeatherOnly = true,
        SpawnChance = 10f,
        SpawnSeconds = 400f,
        SpawnDistance = 50f,
        MaxSpawned = 1,
        Configure = WildMonsterRegistry.ConfigureGarm
    };

    /// <summary>The Greenland Shark.</summary>
    public static readonly WildMonster GreenlandShark = new()
    {
        Id = "greenlandshark",
        PrefabName = "WhiteHilt_GreenlandShark",
        EnglishName = "Greenland Shark",
        BaseCreature = "Serpent",
        TemplateCreature = "Serpent",
        Faction = Character.Faction.SeaMonsters,
        SoundPrefix = "shark",
        ModelScale = 0.7f,
        SizeText = "about 5 m long",
        CapsuleCenter = Vector3.zero,
        CapsuleRadius = 0.9f,
        CapsuleHeight = 1.8f,
        Eye = new Vector3(0f, 0.5f, 1.8f),
        WalkSpeed = 4f,
        RunSpeed = 6f,
        TurnSpeed = 60f,
        RunTurnSpeed = 90f,
        Health = 450f,
        Modifiers = Mods(frost: HitData.DamageModifier.Immune, lightning: HitData.DamageModifier.Weak, poison: HitData.DamageModifier.Resistant),
        Attacks = new[]
        {
            new WildAttack
            {
                Name = "Bite", Trigger = "shark_bite", Description = "Pierce damage of the Greenland Shark's bite.",
                Type = HitData.DamageType.Pierce, Damage = 60f, Range = 3.6f, Height = 1.5f, Angle = 60f, RayWidth = 0.8f, Interval = 3f, Force = 60f
            }
        },
        Drops = new[] { new WildDrop { Item = SharkMeat, Min = 2, Max = 4 } },
        Trophy = SharkTrophy,
        // Dark grey-green; its milky eyes are left alone.
        Look = new WildLook { Tint = new Color(0.6f, 0.7f, 0.66f), Value = -0.1f },
        Biome = Heightmap.Biome.Ocean,
        BiomeArea = Heightmap.BiomeArea.Median,
        MinAltitude = -1000f,
        MaxAltitude = -5f,
        GroundOffset = 0f,
        SpawnDistance = 50f,
        GroupRadius = 3f,
        SpawnChance = 8f,
        SpawnSeconds = 300f,
        RequiredKey = "defeated_bonemass",
        CorpseSinkAfter = 3f,
        CorpseSinkSpeed = 0.8f,
        CorpseLifetime = 10f,
        Configure = WildMonsterRegistry.ConfigureShark
    };

    /// <summary>
    /// The draugr jarl who leads the crew of a Nail flagship. He never spawns in the wild: the flagship's owner raises him
    /// on its deck. He has his own sounds, a raspy undead chieftain's, and the undead look is baked into his
    /// texture. Health and damage are per level; a jarl of level 3 (the flagship's default) has three times the health.
    /// </summary>
    public static readonly WildMonster DraugrJarl = new()
    {
        Id = "draugrjarl",
        PrefabName = "WhiteHilt_DraugrJarl",
        EnglishName = "Draugr Jarl",
        TemplateCreature = "Draugr_Elite",
        Faction = Character.Faction.Undead,
        SoundPrefix = "jarl",
        ModelScale = 0.0126f,
        SizeText = "about 2.3 m tall",
        CapsuleCenter = new Vector3(0f, 1.15f, 0f),
        CapsuleRadius = 0.5f,
        CapsuleHeight = 2.3f,
        Eye = new Vector3(0f, 2.1f, 0.3f),
        WalkSpeed = 1.4f,
        RunSpeed = 2.5f,
        TurnSpeed = 150f,
        RunTurnSpeed = 200f,
        Health = 600f,
        Modifiers = Mods(pierce: HitData.DamageModifier.Resistant, fire: HitData.DamageModifier.Weak, poison: HitData.DamageModifier.Immune,
            spirit: HitData.DamageModifier.Weak),
        Attacks = new[]
        {
            new WildAttack
            {
                Name = "Chop", Trigger = "jarl_swing", Description = "Slash damage of the Draugr Jarl's overhead chop.",
                Type = HitData.DamageType.Slash, Damage = 70f, Range = 3f, Height = 2.2f, Angle = 70f, RayWidth = 1f, Interval = 3f, Force = 160f
            },
            new WildAttack
            {
                Name = "Sweep", Trigger = "jarl_sweep", Description = "Slash damage of the Draugr Jarl's sideways sweep.",
                Type = HitData.DamageType.Slash, Damage = 45f, Range = 3.2f, Height = 1.6f, Angle = 140f, RayWidth = 1f, Interval = 5f, Force = 80f
            }
        },
        Trophy = JarlTrophy,
        TrophyChance = 100f,
        // The undead colour is in the albedo (draugrjarl.creature.json); the model's materials are used as they are.
        Look = new WildLook(),
        // A little growth per star, as the jarl is big to begin with.
        StarScale = 0.08f,
        Spawns = false,
        CorpseSinkAfter = 10f,
        CorpseLifetime = 16f,
        Configure = WildMonsterRegistry.ConfigureJarl
    };

    /// <summary>
    /// The Huldra: a fair maiden who stands in the Mistlands' fog at night and sings. She does not attack; she waves and
    /// blows kisses and draws whoever stands and listens slowly towards her (<see cref="HuldraLure"/>). Come too close or
    /// hurt her, and she turns her back, hollow as a rotten tree, and becomes the <see cref="HuldraTrueForm"/>. Her claw is
    /// only used should she be cornered without turning.
    /// </summary>
    public static readonly WildMonster Huldra = new()
    {
        Id = "huldra",
        PrefabName = "WhiteHilt_Huldra",
        EnglishName = "Huldra",
        Faction = Character.Faction.MistlandsMonsters,
        SoundPrefix = "huldra",
        ModelScale = 1f,
        SizeText = "about 1.75 m tall",
        CapsuleCenter = new Vector3(0f, 0.9f, 0f),
        CapsuleRadius = 0.35f,
        CapsuleHeight = 1.8f,
        Eye = new Vector3(0f, 1.62f, 0.1f),
        WalkSpeed = 1.3f,
        RunSpeed = 4.5f,
        TurnSpeed = 200f,
        RunTurnSpeed = 300f,
        Health = 400f,
        Modifiers = Mods(fire: HitData.DamageModifier.Weak, poison: HitData.DamageModifier.Resistant, spirit: HitData.DamageModifier.Weak),
        Attacks = new[]
        {
            new WildAttack
            {
                Name = "Claw", Trigger = "huldra_attack", Description = "Slash damage of the Huldra's claw, should she fight without turning.",
                Type = HitData.DamageType.Slash, Damage = 60f, Range = 1.8f, Height = 1.5f, Angle = 70f, RayWidth = 0.5f, Interval = 2f, Force = 30f
            }
        },
        Trophy = HuldraTrophy,
        // Her own colours: the fair hair, the blue eyes and the pale skin are painted into her texture.
        Look = new WildLook(),
        StarScale = 0.04f,
        Biome = Heightmap.Biome.Mistlands,
        SpawnAtDay = false,
        SpawnChance = 8f,
        SpawnSeconds = 500f,
        CorpseSinkAfter = 6f,
        CorpseLifetime = 12f,
        Configure = WildMonsterRegistry.ConfigureHuldra
    };

    /// <summary>
    /// The Huldra as she truly is: a hag of roots and rotten bark, on the Yggdrasil Warden's model, smaller, darker and
    /// rotten. She never spawns on her own; the Huldra becomes her.
    /// </summary>
    public static readonly WildMonster HuldraTrueForm = new()
    {
        Id = "huldraroot",
        Model = "yggdrasilwarden",
        PrefabName = "WhiteHilt_HuldraTrueForm",
        EnglishName = "Unmasked Huldra",
        // Its own copies of the materials, so the colours below do not reach the Yggdrasil Warden's.
        TemplateCreature = "Greydwarf",
        Faction = Character.Faction.MistlandsMonsters,
        SoundPrefix = "huldraroot",
        ModelScale = 0.38f,
        SizeText = "about 2.8 m tall",
        CapsuleCenter = new Vector3(0f, 1.35f, 0f),
        CapsuleRadius = 0.7f,
        CapsuleHeight = 2.7f,
        Eye = new Vector3(0f, 2.4f, 0.3f),
        WalkSpeed = 2f,
        RunSpeed = 5f,
        TurnSpeed = 90f,
        RunTurnSpeed = 140f,
        Health = 2400f,
        Modifiers = Mods(pierce: HitData.DamageModifier.Resistant, fire: HitData.DamageModifier.VeryWeak, frost: HitData.DamageModifier.Resistant,
            poison: HitData.DamageModifier.Immune, spirit: HitData.DamageModifier.Weak),
        Attacks = new[]
        {
            new WildAttack
            {
                Name = "Slam", Trigger = "warden_slam", Description = "Blunt damage of the Unmasked Huldra's root slam.",
                Type = HitData.DamageType.Blunt, Damage = 130f, ExtraType = HitData.DamageType.Poison, ExtraDamage = 40f,
                Range = 4.2f, Height = 2f, Angle = 110f, RayWidth = 1.3f, Interval = 3f, Force = 90f
            }
        },
        Drops = new[] { new WildDrop { Item = HuldraBark, Min = 2, Max = 3 } },
        Trophy = HuldraTrophy,
        TrophyChance = 25f,
        // Rotten: dark, sickly green-grey bark; the runes' glow is the model's own emission map.
        Look = new WildLook { Tint = new Color(0.5f, 0.55f, 0.45f), Saturation = -0.25f, Value = -0.25f, Skip = new string[0] },
        StarScale = 0.1f,
        Spawns = false,
        CorpseSinkAfter = 10f,
        CorpseLifetime = 16f,
        Configure = WildMonsterRegistry.ConfigureHuldraRoot
    };

    /// <summary>
    /// The Mare: a pale, see-through woman without legs who floats through the Mistlands at night and shows only up close
    /// (<see cref="MareSpirit"/>). Her cold grip gives the Nightmare, in which no stamina comes back, and she rides those
    /// who sleep in the Mistlands. Fire and spirit hurt her most; blades and arrows pass half through her.
    /// </summary>
    public static readonly WildMonster Mare = new()
    {
        Id = "mare",
        PrefabName = "WhiteHilt_Mare",
        EnglishName = "Mare",
        TemplateCreature = "Ghost",
        KeepModelMaterials = true,
        Faction = Character.Faction.Undead,
        SoundPrefix = "mare",
        ModelScale = 1f,
        SizeText = "about 1.75 m tall, floating",
        CapsuleCenter = new Vector3(0f, 1.1f, 0f),
        CapsuleRadius = 0.35f,
        CapsuleHeight = 1.6f,
        Eye = new Vector3(0f, 1.9f, 0.1f),
        WalkSpeed = 1.5f,
        RunSpeed = 5.5f,
        TurnSpeed = 200f,
        RunTurnSpeed = 300f,
        Health = 550f,
        Modifiers = Mods(blunt: HitData.DamageModifier.Resistant, slash: HitData.DamageModifier.Resistant, pierce: HitData.DamageModifier.Resistant,
            fire: HitData.DamageModifier.VeryWeak, frost: HitData.DamageModifier.Resistant, poison: HitData.DamageModifier.Immune,
            spirit: HitData.DamageModifier.VeryWeak),
        Attacks = new[]
        {
            new WildAttack
            {
                Name = "Grip", Trigger = "mare_grip", Description = "Spirit damage of the Mare's cold grip, which also gives the Nightmare.",
                Type = HitData.DamageType.Spirit, Damage = 55f, ExtraType = HitData.DamageType.Frost, ExtraDamage = 25f,
                Range = 1.9f, Height = 1.6f, Angle = 70f, RayWidth = 0.5f, Interval = 2.5f, Force = 10f
            }
        },
        Drops = new[] { new WildDrop { Item = MareHair, Min = 1, Max = 2 } },
        Trophy = MareTrophy,
        // Pale and cold; her see-through materials and glow come from the model (mare.creature.json).
        Look = new WildLook { Tint = new Color(0.82f, 0.9f, 1f) },
        StarScale = 0.05f,
        Biome = Heightmap.Biome.Mistlands,
        SpawnAtDay = false,
        SpawnChance = 10f,
        SpawnSeconds = 400f,
        CorpseSinkAfter = 2f,
        CorpseSinkSpeed = 0.5f,
        CorpseLifetime = 6f,
        Configure = WildMonsterRegistry.ConfigureMare
    };

    /// <summary>All of them, in the order they are made.</summary>
    public static readonly IReadOnlyList<WildMonster> All = new[]
    {
        MistStag, NidhoggSpawn, YggdrasilWarden, GarmHound, NightGarm, GreenlandShark, DraugrJarl, Huldra, HuldraTrueForm, Mare
    };

    // Damage modifiers where everything not named is normal, and chopping and pickaxes do nothing.
    private static HitData.DamageModifiers Mods(HitData.DamageModifier blunt = HitData.DamageModifier.Normal, HitData.DamageModifier slash = HitData.DamageModifier.Normal,
        HitData.DamageModifier pierce = HitData.DamageModifier.Normal, HitData.DamageModifier fire = HitData.DamageModifier.Normal,
        HitData.DamageModifier frost = HitData.DamageModifier.Normal, HitData.DamageModifier lightning = HitData.DamageModifier.Normal,
        HitData.DamageModifier poison = HitData.DamageModifier.Normal, HitData.DamageModifier spirit = HitData.DamageModifier.Normal)
    {
        return new HitData.DamageModifiers
        {
            m_blunt = blunt,
            m_slash = slash,
            m_pierce = pierce,
            m_chop = HitData.DamageModifier.Ignore,
            m_pickaxe = HitData.DamageModifier.Ignore,
            m_fire = fire,
            m_frost = frost,
            m_lightning = lightning,
            m_poison = poison,
            m_spirit = spirit
        };
    }
}
