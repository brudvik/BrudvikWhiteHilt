using BrudvikWhiteHilt.Difficulty.Beasts;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Monsters;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Storms;

/// <summary>
/// Ragnarok's host as clones of the game's creatures, each with a <see cref="RagnarokHost"/> that sends it back where
/// it came from when the storm passes: the dead who rise from the earth (the draugr), Surt's host out of Muspelheim
/// (the charred warriors of the Ashlands), and Fenrir, a wolf the size of a house that no weapon bites on.
/// </summary>
public static class RagnarokRegistry
{
    /// <summary>A draugr risen in Ragnarok.</summary>
    public const string Draugr = "WhiteHilt_Risen_Draugr";

    /// <summary>A draugr archer risen in Ragnarok.</summary>
    public const string DraugrArcher = "WhiteHilt_Risen_DraugrRanged";

    /// <summary>A draugr elite risen in Ragnarok.</summary>
    public const string DraugrElite = "WhiteHilt_Risen_DraugrElite";

    /// <summary>A warrior of Surt's host.</summary>
    public const string SurtWarrior = "WhiteHilt_Surt_Warrior";

    /// <summary>An archer of Surt's host.</summary>
    public const string SurtArcher = "WhiteHilt_Surt_Archer";

    /// <summary>Fenrir.</summary>
    public const string Fenrir = "WhiteHilt_Fenrir";

    private const string FenrirName = "whitehilt_fenrir";
    private const float FenrirScale = 3.2f;
    private const float FenrirRun = 6.8f;

    /// <summary>
    /// Adds the host once the vanilla creatures can be cloned, and its English names.
    /// </summary>
    public static void Initialize()
    {
        Translations.AddEnglish(FenrirName, "Fenrir");
        CreatureManager.OnVanillaCreaturesAvailable += AddHost;
    }

    private static void AddHost()
    {
        CreatureManager.OnVanillaCreaturesAvailable -= AddHost;
        Add(Draugr, "Draugr", RagnarokHostKind.Dead);
        Add(DraugrArcher, "Draugr_Ranged", RagnarokHostKind.Dead);
        Add(DraugrElite, "Draugr_Elite", RagnarokHostKind.Dead);
        Add(SurtWarrior, "Charred_Melee", RagnarokHostKind.Surt);
        Add(SurtArcher, "Charred_Archer", RagnarokHostKind.Surt);
        try
        {
            AddFenrir();
            Jotunn.Logger.LogInfo("Fenrir added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError("Fenrir failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    // A clone that keeps its look and its drops, attacks what players have built in its way and goes when the storm does.
    private static void Add(string name, string source, RagnarokHostKind kind)
    {
        try
        {
            CustomCreature creature = new(name, source, new CreatureConfig());
            GameObject prefab = creature.Prefab;
            MonsterAI ai = prefab.GetComponent<MonsterAI>();
            if (ai != null)
            {
                ai.m_attackPlayerObjects = true;
                ai.m_sleeping = false;
            }

            prefab.AddComponent<RagnarokHost>().m_kind = kind;
            CreatureManager.Instance.AddCreature(creature);
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"Ragnarok: {name} from {source} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    // A black wolf three times the size of a man, slower than a wolf but as fast as a running player, with eyes of fire.
    private static void AddFenrir()
    {
        CustomCreature creature = new(Fenrir, "Wolf", new CreatureConfig { Name = Translations.Token(FenrirName) });
        GameObject prefab = creature.Prefab;
        prefab.transform.localScale = Vector3.one * FenrirScale;
        Character character = prefab.GetComponent<Character>();
        character.m_name = Translations.Token(FenrirName);
        character.m_boss = false;
        character.m_runSpeed = FenrirRun;
        character.m_speed = Mathf.Min(character.m_speed, FenrirRun);
        character.m_swimSpeed *= 1.5f;

        foreach (Procreation procreation in prefab.GetComponents<Procreation>())
        {
            UnityEngine.Object.DestroyImmediate(procreation);
        }

        foreach (Tameable tameable in prefab.GetComponents<Tameable>())
        {
            UnityEngine.Object.DestroyImmediate(tameable);
        }

        CharacterDrop drops = prefab.GetComponent<CharacterDrop>();
        if (drops != null)
        {
            UnityEngine.Object.DestroyImmediate(drops);
        }

        MonsterAI ai = prefab.GetComponent<MonsterAI>();
        if (ai != null)
        {
            ai.m_sleeping = false;
            ai.m_circulateWhileCharging = false;
            ai.m_fleeIfLowHealth = 0f;
            ai.m_viewRange = 80f;
            ai.m_hearRange = 120f;
        }

        BeastRegistry.Darken(prefab);
        AddEyes(prefab);
        MonsterRegistry.AddCloneSounds(prefab, "blackwolf");
        prefab.AddComponent<ArrivalSound>().clip = "arrival_beast";
        prefab.AddComponent<RagnarokHost>().m_kind = RagnarokHostKind.Fenrir;
        CreatureManager.Instance.AddCreature(creature);
    }

    // A red fire in its head, seen far off in the dark.
    private static void AddEyes(GameObject prefab)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        GameObject glow = new("whitehilt_fenrir_eyes");
        glow.transform.SetParent(prefab.transform, false);
        glow.transform.localPosition = new Vector3(0f, 0.9f, 0.7f);
        Light light = glow.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.15f, 0.05f);
        light.range = 12f;
        light.intensity = 2.5f;
        light.shadows = LightShadows.None;
    }
}
