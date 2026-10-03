using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Monsters;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Kraken;

/// <summary>
/// Creates the Kraken (the animated Lurker model on a stationary clone of the vanilla TentaRoot), its tentacles (bigger
/// TentaRoot clones), the corpse it leaves, its loot, and the octopus (an animated clone of the ocean fish Fish8).
/// </summary>
public static class KrakenRegistry
{
    /// <summary>Prefab name of the Kraken.</summary>
    public const string BodyName = "WhiteHilt_Kraken";

    /// <summary>Prefab name of a Kraken tentacle.</summary>
    public const string TentacleName = "WhiteHilt_KrakenTentacle";

    /// <summary>Prefab name of the octopus, which is both the fish in the water and the item.</summary>
    public const string OctopusName = "WhiteHilt_Octopus";

    /// <summary>Prefab name of the Kraken ink.</summary>
    public const string InkName = "WhiteHilt_KrakenInk";

    /// <summary>Prefab name of the Kraken tentacle meat.</summary>
    public const string MeatName = "WhiteHilt_KrakenMeat";

    /// <summary>Prefab name of the Kraken trophy.</summary>
    public const string TrophyName = "WhiteHilt_TrophyKraken";

    /// <summary>Name of the Kraken's attack trigger, set in AssetSource/Creatures/kraken.creature.json.</summary>
    public const string SlamTrigger = "kraken_slam";

    /// <summary>Size of the tentacles compared with the vanilla TentaRoot.</summary>
    public const float TentacleScale = 3f;

    /// <summary>Size of the octopus model (about 2.2 m wide as downloaded).</summary>
    public const float OctopusScale = 0.35f;

    private const string CorpseName = "WhiteHilt_KrakenCorpse";
    private const string SlamName = "WhiteHilt_KrakenSlam";
    private const string LashName = "WhiteHilt_KrakenLash";

    // The Lurker's eyes sit this high on the 10.5 m model; the Kraken's origin is at the waterline, so the model sinks by it.
    private const float EyeHeight = 7.6f;
    private const float EyesAboveWater = 1.1f;

    private static readonly Color inkTint = new(0.12f, 0.1f, 0.2f);
    private static readonly Color meatTint = new(0.75f, 0.35f, 0.45f);

    private static GameObject corpse;

    /// <summary>
    /// How far below the Kraken's origin the model stands.
    /// </summary>
    public static float ModelDepth => (EyeHeight - EyesAboveWater) * KrakenSettings.Scale.Value;

    /// <summary>
    /// Registers the prefabs once the vanilla creatures can be cloned.
    /// </summary>
    public static void Initialize()
    {
        Translations.AddEnglish("enemy_whitehilt_kraken", "Kraken");
        Translations.AddEnglish("enemy_whitehilt_krakententacle", "Kraken Tentacle");
        Translations.AddEnglish("msg_whitehilt_kraken_rises", "Something vast rises from the deep...");
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(OctopusName), "Octopus",
            "A soft, clever thing from the deep. It slipped the hook twice before you had it.");
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(InkName), "Kraken Ink",
            "Thick black ink from the Kraken. Stains anything, and a paint pot most of all.");
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(MeatName), "Kraken Tentacle",
            "A slab of tentacle as thick as a man's thigh, suckers and all. Cook it well.");
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(TrophyName), "Kraken Trophy",
            "Proof that you met the Kraken on a still, foggy night and lived to tell of it.");
        CreatureManager.OnVanillaCreaturesAvailable += Add;
    }

    private static void Add()
    {
        CreatureManager.OnVanillaCreaturesAvailable -= Add;
        Material template = CreatureVisual.TemplateOf(PrefabManager.Instance.GetPrefab("Serpent"));

        Try("Kraken loot", () =>
        {
            AddItem(InkName, "Ooze", inkTint);
            AddItem(MeatName, "SerpentMeat", meatTint);
            AddTrophy();
        });
        Try("Octopus", () => AddOctopus(template));
        Try("Kraken", () =>
        {
            corpse = CreateCorpse(template);
            AddBody(template);
            AddTentacle();
        });
    }

    private static void Try(string what, Action action)
    {
        try
        {
            action();
            Jotunn.Logger.LogInfo($"{what} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{what} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    private static CustomItem AddItem(string name, string copyFrom, Color tint)
    {
        CustomItem item = new(name, copyFrom);
        ItemDrop.ItemData.SharedData shared = item.ItemDrop.m_itemData.m_shared;
        string key = Translations.ItemKey(name);
        shared.m_name = Translations.Token(key);
        shared.m_description = Translations.Token($"{key}_description");
        if (!VisualHelper.IsHeadless)
        {
            VisualHelper.Tint(item.ItemPrefab, tint);
            Sprite icon = VisualHelper.RenderIcon(item.ItemPrefab);
            if (icon != null)
            {
                shared.m_icons = Enumerable.Repeat(icon, Math.Max(1, shared.m_icons.Length)).ToArray();
            }
        }

        ItemManager.Instance.AddItem(item);
        return item;
    }

    private static void AddTrophy()
    {
        CustomItem trophy = new(TrophyName, "TrophySerpent");
        ItemDrop.ItemData.SharedData shared = trophy.ItemDrop.m_itemData.m_shared;
        string key = Translations.ItemKey(TrophyName);
        shared.m_name = Translations.Token(key);
        shared.m_description = Translations.Token($"{key}_description");
        if (!VisualHelper.IsHeadless)
        {
            VisualHelper.Recolor(trophy.ItemPrefab, pixel =>
            {
                Color.RGBToHSV(pixel, out float hue, out float saturation, out float value);
                Color32 dark = Color.HSVToRGB(0.95f, Mathf.Clamp01(saturation * 0.5f + 0.25f), value * 0.45f);
                dark.a = pixel.a;
                return dark;
            });
            Sprite icon = VisualHelper.RenderIcon(trophy.ItemPrefab);
            if (icon != null)
            {
                shared.m_icons = Enumerable.Repeat(icon, Math.Max(1, shared.m_icons.Length)).ToArray();
            }
        }

        ItemManager.Instance.AddItem(trophy);
    }

    private static void AddOctopus(Material template)
    {
        CustomItem octopus = new(OctopusName, "Fish8");
        GameObject prefab = octopus.ItemPrefab;
        ItemDrop.ItemData.SharedData shared = octopus.ItemDrop.m_itemData.m_shared;
        string key = Translations.ItemKey(OctopusName);
        shared.m_name = Translations.Token(key);
        shared.m_description = Translations.Token($"{key}_description");

        Fish fish = prefab.GetComponent<Fish>();
        fish.m_name = shared.m_name;
        fish.m_pickupItem = prefab;

        foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true).Where(renderer => renderer is MeshRenderer))
        {
            renderer.enabled = false;
        }

        GameObject visual = CreatureVisual.Attach(prefab.transform, "octopus", template, Vector3.zero, Quaternion.identity, OctopusScale);
        prefab.AddComponent<OctopusSwim>().Visual = visual.transform;

        Sprite icon = VisualHelper.RenderIcon(prefab);
        if (icon != null)
        {
            shared.m_icons = Enumerable.Repeat(icon, Math.Max(1, shared.m_icons.Length)).ToArray();
        }

        ItemManager.Instance.AddItem(octopus);
    }

    private static GameObject CreateCorpse(Material template)
    {
        GameObject prefab = PrefabManager.Instance.CreateEmptyPrefab(CorpseName);
        foreach (Component part in prefab.GetComponents<Component>().Where(part => part is MeshRenderer || part is MeshFilter || part is Collider))
        {
            UnityEngine.Object.DestroyImmediate(part);
        }

        float scale = KrakenSettings.Scale.Value;
        GameObject visual = CreatureVisual.Attach(prefab.transform, "kraken", template, new Vector3(0f, -ModelDepth, 0f), Quaternion.identity, scale);
        prefab.AddComponent<KrakenCorpse>().Visual = visual.transform;
        PrefabManager.Instance.AddPrefab(new CustomPrefab(prefab, false));
        return prefab;
    }

    private static void AddBody(Material template)
    {
        float scale = KrakenSettings.Scale.Value;
        CustomItem slam = AddWeapon(SlamName, SlamTrigger, KrakenSettings.BodyDamage.Value, range: 9f * scale, height: 1.5f * scale, angle: 140f, rayWidth: 1.5f * scale);

        CustomCreature creature = new(BodyName, "TentaRoot", new CreatureConfig
        {
            Name = Translations.Token("enemy_whitehilt_kraken"),
            Faction = Character.Faction.SeaMonsters,
            DropConfigs = new[]
            {
                Drop(MeatName, 4, 6, KrakenSettings.LootMultiplier.Value),
                Drop(InkName, 3, 5, KrakenSettings.LootMultiplier.Value),
                Drop(TrophyName, 1, 1, 1f),
                Drop("Chitin", 6, 10, KrakenSettings.LootMultiplier.Value)
            }
        });
        GameObject prefab = creature.Prefab;
        Humanoid humanoid = prefab.GetComponent<Humanoid>();
        humanoid.m_name = Translations.Token("enemy_whitehilt_kraken");
        humanoid.m_health = KrakenSettings.BodyHealth.Value;
        humanoid.m_defaultItems = new[] { slam.ItemPrefab };
        humanoid.m_randomWeapon = new GameObject[0];
        humanoid.m_damageModifiers = SeaModifiers();
        humanoid.m_staggerWhenBlocked = false;
        BorrowSerpentEffects(humanoid);
        humanoid.m_deathEffects.m_effectPrefabs = humanoid.m_deathEffects.m_effectPrefabs
            .Append(new EffectList.EffectData { m_prefab = corpse, m_enabled = true, m_inheritParentRotation = true })
            .ToArray();

        SetUpStationary(prefab, capsuleCenter: new Vector3(0f, 0.3f, 1.2f) * scale, radius: 2.4f * scale, height: 7f * scale);
        MonsterAI ai = prefab.GetComponent<MonsterAI>();
        ai.m_viewRange = 80f;
        ai.m_hearRange = 80f;

        Transform eye = prefab.transform.Find("EyePos");
        if (eye != null)
        {
            eye.localPosition = new Vector3(0f, EyesAboveWater * scale, 2.4f * scale);
        }

        Transform visualRoot = ClearVisual(prefab);
        GameObject visual = CreatureVisual.Attach(visualRoot, "kraken", template, new Vector3(0f, -ModelDepth, 0f), Quaternion.identity, scale);
        visual.GetComponentInChildren<Animator>().gameObject.AddComponent<CharacterAnimEvent>().m_headRotation = false;
        if (KrakenSettings.Sounds.Value)
        {
            MonsterRegistry.AddCreatureSounds(humanoid, ai, slam, "kraken");
        }
        prefab.AddComponent<KrakenBody>();
        CreatureManager.Instance.AddCreature(creature);
    }

    private static void AddTentacle()
    {
        float scale = TentacleScale;
        CustomItem lash = AddWeapon(LashName, "attack", KrakenSettings.TentacleDamage.Value, range: 4f * scale, height: 1.5f * scale, angle: 90f, rayWidth: scale);

        CustomCreature creature = new(TentacleName, "TentaRoot", new CreatureConfig
        {
            Name = Translations.Token("enemy_whitehilt_krakententacle"),
            Faction = Character.Faction.SeaMonsters
        });
        GameObject prefab = creature.Prefab;
        prefab.transform.localScale = Vector3.one * scale;
        Humanoid humanoid = prefab.GetComponent<Humanoid>();
        humanoid.m_name = Translations.Token("enemy_whitehilt_krakententacle");
        humanoid.m_health = KrakenSettings.TentacleHealth.Value;
        humanoid.m_defaultItems = new[] { lash.ItemPrefab };
        humanoid.m_randomWeapon = new GameObject[0];
        humanoid.m_damageModifiers = SeaModifiers();

        SetUpStationary(prefab, capsuleCenter: null, radius: 0f, height: 0f);
        MonsterAI ai = prefab.GetComponent<MonsterAI>();
        ai.m_viewRange = 40f;
        ai.m_hearRange = 40f;
        if (KrakenSettings.Sounds.Value)
        {
            MonsterRegistry.AddCreatureSounds(humanoid, ai, lash, "kraken", "lash");
        }

        if (!VisualHelper.IsHeadless)
        {
            // The same dark wine red as the Kraken's hide.
            VisualHelper.Recolor(prefab, pixel =>
            {
                Color.RGBToHSV(pixel, out _, out float saturation, out float value);
                Color32 flesh = Color.HSVToRGB(0.97f, Mathf.Clamp01(0.35f + saturation * 0.3f), Mathf.Clamp01(value * 0.55f + 0.05f));
                flesh.a = pixel.a;
                return flesh;
            });
        }

        prefab.AddComponent<KrakenTentacle>();
        CreatureManager.Instance.AddCreature(creature);
    }

    private static CustomItem AddWeapon(string name, string trigger, float damage, float range, float height, float angle, float rayWidth)
    {
        CustomItem weapon = new(name, "tentaroot_attack");
        ItemDrop.ItemData.SharedData shared = weapon.ItemDrop.m_itemData.m_shared;
        shared.m_damages = new HitData.DamageTypes { m_blunt = damage };
        shared.m_attackForce = 120f;
        shared.m_aiAttackRange = range * 0.9f;
        shared.m_aiAttackInterval = 4f;
        shared.m_aiAttackMaxAngle = 35f;
        Attack attack = shared.m_attack;
        attack.m_attackAnimation = trigger;
        // Chains and random variants would append a number to the trigger name.
        attack.m_attackChainLevels = 1;
        attack.m_attackRandomAnimations = 0;
        attack.m_attackRange = range;
        attack.m_attackHeight = height;
        attack.m_attackAngle = angle;
        attack.m_attackRayWidth = rayWidth;
        attack.m_hitTerrain = false;
        ItemManager.Instance.AddItem(weapon);
        return weapon;
    }

    private static DropConfig Drop(string item, int min, int max, float multiplier)
    {
        return new DropConfig
        {
            Item = item, MinAmount = Mathf.RoundToInt(min * multiplier), MaxAmount = Mathf.RoundToInt(max * multiplier),
            Chance = multiplier > 0f ? 100f : 0f, LevelMultiplier = false
        };
    }

    private static HitData.DamageModifiers SeaModifiers()
    {
        return new HitData.DamageModifiers
        {
            m_blunt = HitData.DamageModifier.Normal,
            m_slash = HitData.DamageModifier.Normal,
            m_pierce = HitData.DamageModifier.Normal,
            m_chop = HitData.DamageModifier.Ignore,
            m_pickaxe = HitData.DamageModifier.Ignore,
            m_fire = HitData.DamageModifier.Resistant,
            m_frost = HitData.DamageModifier.Resistant,
            m_lightning = HitData.DamageModifier.Weak,
            m_poison = HitData.DamageModifier.Immune,
            m_spirit = HitData.DamageModifier.Normal
        };
    }

    private static void BorrowSerpentEffects(Humanoid humanoid)
    {
        GameObject serpent = PrefabManager.Instance.GetPrefab("Serpent");
        Character source = serpent != null ? serpent.GetComponent<Character>() : null;
        if (source == null)
        {
            return;
        }

        humanoid.m_hitEffects = source.m_hitEffects;
        humanoid.m_critHitEffects = source.m_critHitEffects;
        // The serpent's ragdoll would fall out of the sky; the Kraken leaves its own corpse.
        humanoid.m_deathEffects = new EffectList
        {
            m_effectPrefabs = source.m_deathEffects.m_effectPrefabs.Where(effect => effect.m_prefab != null && effect.m_prefab.GetComponent<Ragdoll>() == null).ToArray()
        };
        MonsterAI serpentAI = serpent.GetComponent<MonsterAI>();
        MonsterAI ai = humanoid.GetComponent<MonsterAI>();
        if (serpentAI != null && ai != null)
        {
            ai.m_alertedEffects = serpentAI.m_alertedEffects;
            ai.m_idleSound = serpentAI.m_idleSound;
        }
    }

    // Both float at the waterline: the rigidbody may not move, only turn about its up axis.
    private static void SetUpStationary(GameObject prefab, Vector3? capsuleCenter, float radius, float height)
    {
        UnityEngine.Object.DestroyImmediate(prefab.GetComponent<CharacterTimedDestruction>());
        Character character = prefab.GetComponent<Character>();
        character.m_tolerateWater = true;
        character.m_canSwim = false;

        Rigidbody body = prefab.GetComponent<Rigidbody>();
        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        MonsterAI ai = prefab.GetComponent<MonsterAI>();
        ai.m_fleeIfHurtWhenTargetCantBeReached = false;
        ai.m_fleeIfNotAlerted = false;
        ai.m_attackPlayerObjects = true;
        ai.m_enableHuntPlayer = true;

        if (capsuleCenter.HasValue)
        {
            CapsuleCollider capsule = prefab.GetComponent<CapsuleCollider>();
            capsule.center = capsuleCenter.Value;
            capsule.radius = radius;
            capsule.height = height;
            capsule.direction = 1;
        }
    }

    private static Transform ClearVisual(GameObject prefab)
    {
        Transform visual = prefab.transform.Find("Visual");
        visual.localScale = Vector3.one;
        UnityEngine.Object.DestroyImmediate(visual.GetComponent<LODGroup>());
        for (int i = visual.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.DestroyImmediate(visual.GetChild(i).gameObject);
        }

        return visual;
    }
}
