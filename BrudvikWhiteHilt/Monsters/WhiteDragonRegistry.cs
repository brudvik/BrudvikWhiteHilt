using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Summoning;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Monsters;

/// <summary>
/// Creates the White Dragon that the Dragon Horn calls: a pale clone of the Desert Dragon, tame from birth, with a
/// fire and frost breath of its own that ends in a blast (<see cref="DragonBlast"/>), a cold glow, no loot and a
/// white corpse. Also makes the blessing it gives its caller and the fear its roar puts on foes.
/// </summary>
public static class WhiteDragonRegistry
{
    /// <summary>Prefab name of the White Dragon.</summary>
    public const string DragonName = "WhiteHilt_WhiteDragon";

    private const string FlameName = "WhiteHilt_WhiteDragonFlame";
    private const string FrostName = "WhiteHilt_WhiteDragonFrost";
    private const string FrostProjectileName = "WhiteHilt_WhiteDragonFrost_projectile";
    private const string CorpseName = "WhiteHilt_WhiteDragonCorpse";
    private const string BlessingName = "SE_WhiteHilt_DragonBlessing";
    private const string TerrorName = "SE_WhiteHilt_DragonTerror";
    private const float BlessingSeconds = 3f;

    // Moder's ice spit, without the ice walls it raises where it lands.
    private const string IceProjectile = "dragon_ice_projectile";

    // The frost status effect's mist, without its network parts.
    private const string MistPrefab = "vfx_Cold";
    private const string MistChild = "soft cloud";
    private const float MistSize = 2f;

    // The vanilla summoned skeleton lends its petting effect and words.
    private const string SummonTemplate = "Skeleton_Friendly";

    private static readonly Dictionary<GameObject, GameObject> louderSounds = new();
    private static SE_Stats blessing;
    private static SE_Stats terror;

    /// <summary>
    /// Adds the names, and the prefab once the Desert Dragon exists. Call after <see cref="DesertDragonRegistry.Initialize"/>.
    /// </summary>
    public static void Initialize()
    {
        Translations.AddEnglish("enemy_whitehilt_whitedragon", "White Dragon");
        Translations.AddEnglish("se_whitehilt_dragonblessing", "Dragon's Blessing");
        Translations.AddEnglish("se_whitehilt_dragonblessing_tooltip", "Your White Dragon watches over you.");
        Translations.AddEnglish("se_whitehilt_dragonterror", "Dragon Terror");
        Translations.AddEnglish("se_whitehilt_dragonterror_tooltip", "The roar of a White Dragon.");
        CreatureManager.OnVanillaCreaturesAvailable += Add;
    }

    /// <summary>
    /// Applies changed settings to the blessing and the fear, for the next time they are given.
    /// </summary>
    public static void ApplyConfig()
    {
        if (blessing != null)
        {
            HitData.DamageModifier resistance = DragonHorn.BlessingResistance.Value;
            blessing.m_mods = new List<HitData.DamageModPair>
            {
                new() { m_type = HitData.DamageType.Blunt, m_modifier = resistance },
                new() { m_type = HitData.DamageType.Slash, m_modifier = resistance },
                new() { m_type = HitData.DamageType.Pierce, m_modifier = resistance }
            };
            blessing.m_healthRegenMultiplier = DragonHorn.BlessingRegen.Value;
            blessing.m_staminaRegenMultiplier = DragonHorn.BlessingRegen.Value;
        }

        if (terror != null)
        {
            terror.m_speedModifier = -DragonHorn.RoarSlow.Value;
            terror.m_ttl = DragonHorn.RoarSlowSeconds.Value;
        }
    }

    private static void Add()
    {
        CreatureManager.OnVanillaCreaturesAvailable -= Add;
        try
        {
            AddEffects();
            List<CustomItem> breaths = new();
            DragonBreath breath = DragonHorn.Breath.Value;
            if (breath != DragonBreath.Frost)
            {
                breaths.Add(AddFlame());
            }

            if (breath != DragonBreath.Fire)
            {
                breaths.Add(AddFrost());
            }

            AddDragon(breaths);
            Jotunn.Logger.LogInfo("White Dragon added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError("White Dragon failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    // The blessing on its caller and the fear on foes it roars at.
    private static void AddEffects()
    {
        Sprite icon = PrefabManager.Instance.GetPrefab(DragonHorn.ItemName)?.GetComponent<ItemDrop>()?.m_itemData.GetIcon()
            ?? PrefabManager.Instance.GetPrefab(DesertDragonRegistry.TrophyName)?.GetComponent<ItemDrop>()?.m_itemData.GetIcon();

        blessing = ScriptableObject.CreateInstance<SE_Stats>();
        blessing.name = BlessingName;
        blessing.m_name = Translations.Token("se_whitehilt_dragonblessing");
        blessing.m_tooltip = Translations.Token("se_whitehilt_dragonblessing_tooltip");
        blessing.m_ttl = BlessingSeconds;
        blessing.m_icon = icon;
        ItemManager.Instance.AddStatusEffect(new CustomStatusEffect(blessing, fixReference: false));

        terror = ScriptableObject.CreateInstance<SE_Stats>();
        terror.name = TerrorName;
        terror.m_name = Translations.Token("se_whitehilt_dragonterror");
        terror.m_tooltip = Translations.Token("se_whitehilt_dragonterror_tooltip");
        terror.m_icon = icon;
        ItemManager.Instance.AddStatusEffect(new CustomStatusEffect(terror, fixReference: false));

        ApplyConfig();
        WhiteDragonAlly.BlessingHash = BlessingName.GetStableHashCode();
        WhiteDragonAlly.TerrorHash = TerrorName.GetStableHashCode();
    }

    // The Desert Dragon's breath with the Dragon Horn's own damage, flames and pace.
    private static CustomItem AddFlame()
    {
        CustomItem weapon = new(FlameName, DesertDragonRegistry.FlameName);
        ItemDrop.ItemData.SharedData shared = weapon.ItemDrop.m_itemData.m_shared;
        shared.m_damages = new HitData.DamageTypes { m_fire = DragonHorn.FireDamage.Value };
        shared.m_aiAttackInterval = DragonHorn.BreathSeconds.Value;
        shared.m_attack.m_projectileBursts = DragonHorn.Flames.Value;
        shared.m_triggerEffect = Louder(shared.m_triggerEffect);
        ItemManager.Instance.AddItem(weapon);
        return weapon;
    }

    // A breath of frost shards: Moder's ice spit, without the ice walls, made to fly and widen like the fire breath.
    // Frost damage slows what it hits, and it bites on foes that shrug off fire.
    private static CustomItem AddFrost()
    {
        GameObject fire = PrefabManager.Instance.GetPrefab(DesertDragonRegistry.ProjectileName);
        GameObject projectile = PrefabManager.Instance.CreateClonedPrefab(FrostProjectileName, IceProjectile);
        Projectile shot = projectile.GetComponent<Projectile>();
        Projectile fireShot = fire != null ? fire.GetComponent<Projectile>() : null;
        shot.m_spawnOnHit = null;
        shot.m_gravity = 0f;
        if (fireShot != null)
        {
            shot.m_ttl = fireShot.m_ttl;
        }

        DragonFlame template = fire != null ? fire.GetComponent<DragonFlame>() : null;
        DragonFlame growth = projectile.AddComponent<DragonFlame>();
        if (template != null)
        {
            growth.StartSize = template.StartSize;
            growth.EndSize = template.EndSize;
            growth.StartRadius = template.StartRadius;
            growth.EndRadius = template.EndRadius;
            growth.GrowSeconds = template.GrowSeconds;
        }

        PrefabManager.Instance.AddPrefab(new CustomPrefab(projectile, false));

        CustomItem weapon = new(FrostName, DesertDragonRegistry.FlameName);
        ItemDrop.ItemData.SharedData shared = weapon.ItemDrop.m_itemData.m_shared;
        shared.m_damages = new HitData.DamageTypes { m_frost = DragonHorn.FrostDamage.Value };
        shared.m_aiAttackInterval = DragonHorn.BreathSeconds.Value;
        shared.m_attack.m_projectileBursts = DragonHorn.Flames.Value;
        shared.m_attack.m_attackProjectile = projectile;
        shared.m_triggerEffect = Louder(shared.m_triggerEffect);
        ItemManager.Instance.AddItem(weapon);
        return weapon;
    }

    // A cold glow on its body and frost mist at its mouth. The mist is the frost effect's own cloud, set to scale with
    // the bones it hangs from and sized back up from the model's tiny bone scale.
    private static void AddAura(GameObject prefab)
    {
        if (!DragonHorn.Aura.Value || VisualHelper.IsHeadless)
        {
            return;
        }

        GameObject glow = new("WhiteHiltAura");
        glow.transform.SetParent(prefab.transform, false);
        glow.transform.localPosition = new Vector3(0f, 2f, 0f);
        Light light = glow.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.7f, 0.85f, 1f);
        light.range = 14f;
        light.intensity = 1.6f;
        light.shadows = LightShadows.None;

        Transform mouth = Utils.FindChild(prefab.transform, DesertDragonRegistry.MouthBone);
        Transform mist = PrefabManager.Instance.GetPrefab(MistPrefab)?.transform.Find(MistChild);
        if (mouth == null || mist == null)
        {
            Jotunn.Logger.LogWarning("White Dragon: no mouth bone or frost mist for its aura");
            return;
        }

        GameObject copy = UnityEngine.Object.Instantiate(mist.gameObject, mouth, false);
        Vector3 lossy = mouth.lossyScale;
        float size = MistSize * DragonHorn.Size.Value;
        copy.transform.localScale = new Vector3(size / Mathf.Max(0.0001f, lossy.x), size / Mathf.Max(0.0001f, lossy.y), size / Mathf.Max(0.0001f, lossy.z));
        foreach (ParticleSystem system in copy.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = system.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.loop = true;
        }
    }

    private static void AddDragon(List<CustomItem> breaths)
    {
        string name = Translations.Token("enemy_whitehilt_whitedragon");
        CustomCreature creature = new(DragonName, DesertDragonRegistry.DragonName, new CreatureConfig { Name = name });
        // The Desert Dragon's drops are still Jotunn mocks here, and its clone would keep them unresolved.
        creature.FixReference = true;
        GameObject prefab = creature.Prefab;
        prefab.transform.localScale *= DragonHorn.Size.Value;

        Humanoid humanoid = prefab.GetComponent<Humanoid>();
        humanoid.m_name = name;
        humanoid.m_health = DragonHorn.Health.Value;
        humanoid.m_defaultItems = breaths.Select(breath => breath.ItemPrefab).ToArray();
        humanoid.m_hitEffects = Louder(humanoid.m_hitEffects);
        humanoid.m_critHitEffects = Louder(humanoid.m_critHitEffects);
        humanoid.m_deathEffects = Louder(humanoid.m_deathEffects);

        // A called friend leaves nothing to farm.
        prefab.GetComponent<CharacterDrop>()?.m_drops.Clear();

        MonsterAI ai = prefab.GetComponent<MonsterAI>();
        ai.m_idleSound = Louder(ai.m_idleSound);
        ai.m_alertedEffects = Louder(ai.m_alertedEffects);
        ai.m_despawnInDay = false;
        ai.m_fleeIfLowHealth = 0f;
        ai.m_attackPlayerObjects = false;

        Tameable tameable = prefab.AddComponent<Tameable>();
        tameable.m_startsTamed = true;
        tameable.m_commandable = false;
        // Never hungry: IsHungry counts from the year 1 when it was never fed.
        tameable.m_fedDuration = float.MaxValue;
        Tameable template = PrefabManager.Instance.GetPrefab(SummonTemplate)?.GetComponent<Tameable>();
        if (template != null)
        {
            tameable.m_petEffect = template.m_petEffect;
            tameable.m_tameText = template.m_tameText;
            tameable.m_nameBeforeText = template.m_nameBeforeText;
        }

        prefab.AddComponent<WhiteDragonAlly>();
        prefab.AddComponent<DragonBlast>();
        MonsterRegistry.AddCreatureSounds(humanoid, ai, breaths[0], "whitedragon", "attack", breaths.Count > 1 ? breaths[1] : null, breaths.Count > 1 ? "attack" : null);
        if (prefab.GetComponent<WingBeats>() == null)
        {
            prefab.AddComponent<WingBeats>();
        }
        AddAura(prefab);
        Whiten(prefab);
        WhitenCorpses(humanoid);
        CreatureManager.Instance.AddCreature(creature);
    }

    // The Desert Dragon's corpse would lie there sand-coloured.
    private static void WhitenCorpses(Character character)
    {
        foreach (EffectList.EffectData effect in character.m_deathEffects.m_effectPrefabs)
        {
            if (effect.m_prefab == null || effect.m_prefab.GetComponent<MonsterCorpse>() == null)
            {
                continue;
            }

            GameObject corpse = PrefabManager.Instance.CreateClonedPrefab(CorpseName, effect.m_prefab);
            Whiten(corpse);
            // It takes the dragon's larger size, which the other players must see too.
            ZNetView view = corpse.GetComponent<ZNetView>();
            if (view != null)
            {
                view.m_syncInitialScale = true;
            }

            PrefabManager.Instance.AddPrefab(new CustomPrefab(corpse, false));
            effect.m_prefab = corpse;
        }
    }

    // Swaps the sounds in an effect list for louder copies, made once per sound.
    private static EffectList Louder(EffectList effects)
    {
        foreach (EffectList.EffectData effect in effects?.m_effectPrefabs ?? Array.Empty<EffectList.EffectData>())
        {
            if (effect.m_prefab != null && effect.m_prefab.GetComponentInChildren<ZSFX>(true) != null)
            {
                effect.m_prefab = LouderSound(effect.m_prefab);
            }
        }

        return effects;
    }

    // A copy of a sound at full volume, heard RoarLoudness times further, with its clips pushed louder. The copy is made
    // on a dedicated server too, so every machine knows the same effects; only the clips are left alone there. It is
    // registered, as these sounds are made on the dragon's owner and a networked one must reach the other players.
    private static GameObject LouderSound(GameObject original)
    {
        if (louderSounds.TryGetValue(original, out GameObject cached))
        {
            return cached;
        }

        float loudness = DragonHorn.RoarLoudness.Value;
        GameObject copy = PrefabManager.Instance.CreateClonedPrefab(original.name + "_whitedragon", original);
        PrefabManager.Instance.AddPrefab(new CustomPrefab(copy, false));
        foreach (ZSFX sound in copy.GetComponentsInChildren<ZSFX>(true))
        {
            sound.m_minVol = 1f;
            sound.m_maxVol = 1f;
            if (!VisualHelper.IsHeadless)
            {
                sound.m_audioClips = sound.m_audioClips.Select(clip => Amplify(clip, loudness)).ToArray();
            }
        }

        foreach (AudioSource source in copy.GetComponentsInChildren<AudioSource>(true))
        {
            source.minDistance *= loudness;
            source.maxDistance *= loudness;
        }

        louderSounds[original] = copy;
        return copy;
    }

    // Pushes a clip louder through a soft limiter (tanh), so it gains loudness without clipping. Unity 6 adds span
    // overloads of GetData and SetData that net48 cannot bind, so the array ones are called by name.
    private static AudioClip Amplify(AudioClip clip, float gain)
    {
        if (clip == null || gain <= 1f)
        {
            return clip;
        }

        float[] samples = new float[clip.samples * clip.channels];
        object read = typeof(AudioClip).GetMethod(nameof(AudioClip.GetData), new[] { typeof(float[]), typeof(int) })
            ?.Invoke(clip, new object[] { samples, 0 });
        if (read is not true)
        {
            Jotunn.Logger.LogWarning($"White Dragon: could not read sound {clip.name}, kept as it is");
            return clip;
        }

        float scale = 1f / (float)Math.Tanh(gain);
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = (float)Math.Tanh(samples[i] * gain) * scale;
        }

        AudioClip louder = AudioClip.Create(clip.name + "_whitedragon", clip.samples, clip.channels, clip.frequency, false);
        typeof(AudioClip).GetMethod(nameof(AudioClip.SetData), new[] { typeof(float[]), typeof(int) })?.Invoke(louder, new object[] { samples, 0 });
        return louder;
    }

    private static void Whiten(GameObject root)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        VisualHelper.Recolor(root, pixel =>
        {
            Color.RGBToHSV(pixel, out _, out float saturation, out float value);
            Color32 white = Color.HSVToRGB(0.58f, saturation * 0.12f, Mathf.Clamp01(0.35f + value * 0.75f));
            white.a = pixel.a;
            return white;
        });
    }
}
