using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Monsters;

/// <summary>
/// One attack of a wild monster: the animation trigger, the damage it does by default and the shape of its hit.
/// Lengths are in metres at the monster's default size.
/// </summary>
internal sealed class WildAttack
{
    /// <summary>Last part of the weapon's prefab name, e.g. <c>Charge</c>.</summary>
    public string Name;

    /// <summary>Animation trigger set in the controller of the model, e.g. <c>stag_charge</c>.</summary>
    public string Trigger;

    /// <summary>What the damage of the attack is called in the settings, e.g. "Pierce damage of the charge".</summary>
    public string Description;

    /// <summary>Kind of the main damage.</summary>
    public HitData.DamageType Type;

    /// <summary>Default main damage.</summary>
    public float Damage;

    /// <summary>Kind of the extra damage, e.g. poison; only used when <see cref="ExtraDamage"/> is above zero.</summary>
    public HitData.DamageType ExtraType;

    /// <summary>Default extra damage dealt with the main damage, or zero for none.</summary>
    public float ExtraDamage;

    /// <summary>How far the hit reaches.</summary>
    public float Range;

    /// <summary>Height of the hit area.</summary>
    public float Height;

    /// <summary>Angle of the hit area, in degrees.</summary>
    public float Angle;

    /// <summary>Width of the hit's ray.</summary>
    public float RayWidth;

    /// <summary>Seconds before the monster uses this attack again.</summary>
    public float Interval;

    /// <summary>How hard a hit pushes its target.</summary>
    public float Force = 40f;
}

/// <summary>
/// What a wild monster leaves behind.
/// </summary>
internal sealed class WildDrop
{
    /// <summary>Prefab name of the item.</summary>
    public string Item;

    /// <summary>Fewest dropped.</summary>
    public int Min;

    /// <summary>Most dropped.</summary>
    public int Max;
}

/// <summary>
/// How a monster is coloured when it is made: the model keeps the author's textures, and these values are set on its
/// materials at runtime, so the source files stay as downloaded. Values are the creature shader's own offsets.
/// </summary>
internal sealed class WildLook
{
    /// <summary>Multiplied into the colour of every material.</summary>
    public Color Tint = Color.white;

    /// <summary>Added to the hue (the shader wraps it round).</summary>
    public float Hue;

    /// <summary>Added to the saturation.</summary>
    public float Saturation;

    /// <summary>Added to the brightness.</summary>
    public float Value;

    /// <summary>New smoothness, or negative to leave it as it is.</summary>
    public float Glossiness = -1f;

    /// <summary>Whether the metallic shine of the shader's template is switched off.</summary>
    public bool Matte;

    /// <summary>Materials whose name contains one of these are left alone: eyes, which glow in their own colour, and leaves.</summary>
    public string[] Skip = { "eye" };
}

/// <summary>
/// A wild monster: its names, the creature it is cloned from, how it looks and fights, where it spawns and what it
/// drops. The registry makes all five from these, so they share one code path; behaviour that only one of them has
/// sits in <see cref="Configure"/> and in small components.
/// </summary>
internal sealed class WildMonster
{
    /// <summary>Name of the creature in texts, settings and sounds, and of its model unless <see cref="Model"/> names another.</summary>
    public string Id;

    /// <summary>Name of the model in the bundle (<c>&lt;model&gt;_visual</c>) when it is another monster's, or null for its own.</summary>
    public string Model;

    /// <summary>Prefab name of the creature.</summary>
    public string PrefabName;

    /// <summary>Name in English.</summary>
    public string EnglishName;

    /// <summary>Vanilla creature the prefab is cloned from: it brings the body, the AI and the drops.</summary>
    public string BaseCreature = "Greyling";

    /// <summary>Vanilla creature whose material is the template of the model, and whose hit effects it borrows.</summary>
    public string TemplateCreature = "Wolf";

    /// <summary>
    /// Whether the model keeps the materials it was built with instead of copies of the template's: the Mare's are
    /// see-through, which the game's creature shader cannot be.
    /// </summary>
    public bool KeepModelMaterials;

    /// <summary>Who the monster is friends with.</summary>
    public Character.Faction Faction;

    /// <summary>
    /// Start of the names of its sounds in the bundle, e.g. <c>nidhogg</c> for <c>nidhoggidle</c>; null for a monster that
    /// keeps the sounds of the creature it is cloned from.
    /// </summary>
    public string SoundPrefix;

    /// <summary>Metres per unit of the model at the default size.</summary>
    public float ModelScale = 1f;

    /// <summary>How big the monster is at the default size, for the Scale setting's description.</summary>
    public string SizeText;

    /// <summary>Middle of the capsule that is the monster's body, at the default size.</summary>
    public Vector3 CapsuleCenter;

    /// <summary>Radius of the capsule.</summary>
    public float CapsuleRadius;

    /// <summary>Height of the capsule.</summary>
    public float CapsuleHeight;

    /// <summary>Where it sees from.</summary>
    public Vector3 Eye;

    /// <summary>Walking speed, in metres per second.</summary>
    public float WalkSpeed;

    /// <summary>Running speed; a swimmer swims at this speed.</summary>
    public float RunSpeed;

    /// <summary>Degrees per second it turns when walking.</summary>
    public float TurnSpeed = 120f;

    /// <summary>Degrees per second it turns when running.</summary>
    public float RunTurnSpeed = 180f;

    /// <summary>How each kind of damage hurts it.</summary>
    public HitData.DamageModifiers Modifiers;

    /// <summary>Its attacks. The first has the settings Damage (and ExtraDamage), the second SecondDamage.</summary>
    public WildAttack[] Attacks = Array.Empty<WildAttack>();

    /// <summary>What it always drops; the trophy comes on top, by its own chance.</summary>
    public WildDrop[] Drops = Array.Empty<WildDrop>();

    /// <summary>Prefab name of its trophy.</summary>
    public string Trophy;

    /// <summary>How it is coloured.</summary>
    public WildLook Look = new();

    /// <summary>
    /// Whether it spawns in the wild. A monster that does not (the draugr jarl, who only sails on a flagship) gets no
    /// spawn data and none of the settings for spawning.
    /// </summary>
    public bool Spawns = true;

    /// <summary>Default visual growth per star, up to two stars.</summary>
    public float StarScale = 0.15f;

    /// <summary>Biome it spawns in.</summary>
    public Heightmap.Biome Biome;

    /// <summary>Part of the biome it spawns in.</summary>
    public Heightmap.BiomeArea BiomeArea = Heightmap.BiomeArea.Everything;

    /// <summary>Whether it spawns by day.</summary>
    public bool SpawnAtDay = true;

    /// <summary>Whether it spawns by night.</summary>
    public bool SpawnAtNight = true;

    /// <summary>Lowest ground (relative to the sea) it spawns on.</summary>
    public float MinAltitude = 1f;

    /// <summary>Highest ground it spawns on.</summary>
    public float MaxAltitude = 1000f;

    /// <summary>Height above the ground, or the sea floor, it spawns at.</summary>
    public float GroundOffset = 0.5f;

    /// <summary>Distance from the player it spawns at.</summary>
    public float SpawnDistance = 40f;

    /// <summary>Radius round the first one in which a group spawns.</summary>
    public float GroupRadius = 5f;

    /// <summary>Default chance per spawn check, in percent.</summary>
    public float SpawnChance;

    /// <summary>Default seconds between spawn checks.</summary>
    public float SpawnSeconds;

    /// <summary>Default most spawned near a player.</summary>
    public int MaxSpawned = 1;

    /// <summary>Default fewest in a group; a monster that comes in groups also gets the settings for them.</summary>
    public int GroupMin = 1;

    /// <summary>Default most in a group.</summary>
    public int GroupMax = 1;

    /// <summary>Default health.</summary>
    public float Health;

    /// <summary>Default chance, in percent, that the trophy drops.</summary>
    public float TrophyChance = 10f;

    /// <summary>Default global key needed before it spawns, or null when it needs none; a monster with a key also gets the setting for it.</summary>
    public string RequiredKey;

    /// <summary>Default metres at which a sleeping monster wakes, or zero when it never sleeps; it also gets the setting for it.</summary>
    public float WakeRange;

    /// <summary>Seconds before its corpse starts to sink.</summary>
    public float CorpseSinkAfter = 8f;

    /// <summary>Metres per second the corpse sinks.</summary>
    public float CorpseSinkSpeed = 0.3f;

    /// <summary>Seconds before the corpse is gone.</summary>
    public float CorpseLifetime = 14f;

    /// <summary>Name of the model in the bundle.</summary>
    public string ModelName => Model ?? Id;

    /// <summary>Whether it comes in groups.</summary>
    public bool InGroups => GroupMax > 1;

    /// <summary>Name of the settings section, e.g. <c>Monsters.MistStag</c>. A name's spaces are left out.</summary>
    public string Section => $"Monsters.{EnglishName.Replace(" ", string.Empty)}";

    /// <summary>Translation key of the name.</summary>
    public string NameKey => $"enemy_whitehilt_{Id}";

    /// <summary>Extra changes to the cloned creature, made once everything common is set.</summary>
    public Action<WildMonsterParts> Configure;
}

/// <summary>
/// The parts of a monster that is being made, handed to its own <see cref="WildMonster.Configure"/>.
/// </summary>
internal sealed class WildMonsterParts
{
    /// <summary>The monster's settings.</summary>
    public WildMonsterSettings Settings;

    /// <summary>The creature prefab.</summary>
    public GameObject Prefab;

    /// <summary>Its body.</summary>
    public Humanoid Humanoid;

    /// <summary>Its mind.</summary>
    public MonsterAI AI;

    /// <summary>The animated model.</summary>
    public GameObject Model;
}
