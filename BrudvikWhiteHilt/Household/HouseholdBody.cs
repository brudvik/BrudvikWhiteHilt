using BrudvikWhiteHilt.Helpers;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrudvikWhiteHilt.Household;

/// <summary>
/// The body of a member of the household: a clone of the player prefab that keeps everything that makes it move (the
/// rigid body, the collider, the network movement and the animator), with a <see cref="Householder"/> (the game's
/// humanoid) and <see cref="MonsterAI"/> in place of <see cref="Player"/>. The traders are the same body with the movement taken away
/// (<c>TradingPostTrader.StripToBody</c>); this one walks, so it looks like the players and the traders and moves the way
/// the game's other walking creatures do.
/// </summary>
public static class HouseholdBody
{
    /// <summary>Prefab name of a member of the household.</summary>
    public const string PrefabName = "WhiteHilt_Householder";

    /// <summary>Translation key of a member's name until they are given their own.</summary>
    public const string NameKey = "whitehilt_householder";

    // The parts of the player prefab a walking body still needs; every other behaviour on it is the player's own.
    private static readonly HashSet<Type> kept = new()
    {
        typeof(ZNetView), typeof(ZSyncTransform), typeof(ZSyncAnimation), typeof(VisEquipment), typeof(Householder),
        typeof(FootStep), typeof(CharacterAnimEvent),
    };

    /// <summary>
    /// Registers the name's English text.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglish(NameKey, "Householder");
    }

    /// <summary>
    /// Makes the prefab. Call when the vanilla prefabs are available.
    /// </summary>
    public static void CreatePrefab()
    {
        GameObject prefab = PrefabManager.Instance.CreateClonedPrefab(PrefabName, "Player");
        if (prefab == null)
        {
            Jotunn.Logger.LogError("Household: the player prefab was not found, no householders.");
            return;
        }

        try
        {
            Player player = prefab.GetComponent<Player>();
            Householder human = prefab.AddComponent<Householder>();
            CopyFields(player, human, typeof(Character), typeof(Humanoid));
            StripPlayer(prefab);
            Setup(human);
            AddAI(prefab);
            // Phase 0: goals given from the console (whitehilt_household), until the household's work loop is written.
            prefab.AddComponent<HouseholdProbe>();
            prefab.AddComponent<HouseholdMember>();
            prefab.AddComponent<HouseholdDoors>();

            ZNetView view = prefab.GetComponent<ZNetView>();
            view.m_persistent = true;
            view.m_distant = false;
            view.m_syncInitialScale = false;
            // The player body sits on the "player" layer, which the game's creatures treat as a player's; this one
            // is a creature like the others.
            int layer = LayerMask.NameToLayer("character");
            prefab.layer = layer;
            PrefabManager.Instance.AddPrefab(new CustomPrefab(prefab, false));
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError("Household: the householder could not be made.");
            Jotunn.Logger.LogError(ex);
        }
    }

    // Copies the settings the player prefab has for a character and a humanoid (health, speeds, hit effects, the eye,
    // the unarmed attack) to the new humanoid. Only what Unity would save is copied: the rest is filled in at Awake.
    private static void CopyFields(Component from, Component to, params Type[] types)
    {
        foreach (Type type in types)
        {
            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                bool saved = (field.IsPublic || field.IsDefined(typeof(SerializeField), false)) && !field.IsNotSerialized;
                if (saved)
                {
                    field.SetValue(to, field.GetValue(from));
                }
            }
        }
    }

    // Removes the player's own behaviours from the body and its Visual. A behaviour another one requires cannot go
    // before it, so the removal runs until nothing more goes, as StripToBody does.
    private static void StripPlayer(GameObject prefab)
    {
        Transform visual = prefab.transform.Find("Visual");
        GameObject[] holders = new[] { prefab, visual != null ? visual.gameObject : null }.Where(holder => holder != null).ToArray();
        for (int round = 0; round < 4; round++)
        {
            foreach (MonoBehaviour behaviour in holders.SelectMany(holder => holder.GetComponents<MonoBehaviour>()))
            {
                if (!kept.Contains(behaviour.GetType()))
                {
                    Object.DestroyImmediate(behaviour);
                }
            }
        }
    }

    // A member of the household is on the players' side: the game's creatures go for them as for a player, and they
    // go for the creatures. The player's 25 health would make them fall to a single troll blow.
    private static void Setup(Householder human)
    {
        human.m_name = Translations.Token(NameKey);
        human.m_group = PrefabName;
        human.m_faction = Character.Faction.Players;
        human.m_health = 100f;
        human.m_boss = false;
        human.m_defaultItems = new GameObject[0];
        human.m_randomWeapon = new GameObject[0];
        human.m_randomArmor = new GameObject[0];
        human.m_randomShield = new GameObject[0];
        human.m_randomSets = new Humanoid.ItemSet[0];
    }

    // The game's creature AI, set for someone who lives on a farm: never hunts players, never breaks the players'
    // buildings, keeps away from fire and water, and wanders only a little when it has nothing to do.
    private static void AddAI(GameObject prefab)
    {
        MonsterAI ai = prefab.AddComponent<MonsterAI>();
        ai.m_pathAgentType = Pathfinding.AgentType.Humanoid;
        ai.m_viewRange = 30f;
        ai.m_hearRange = 30f;
        ai.m_alertRange = 20f;
        ai.m_enableHuntPlayer = false;
        ai.m_attackPlayerObjects = false;
        ai.m_aggravatable = false;
        ai.m_avoidFire = true;
        ai.m_afraidOfFire = false;
        ai.m_avoidWater = true;
        ai.m_randomMoveRange = 3f;
        ai.m_randomMoveInterval = 8f;
        ai.m_idleSoundChance = 0f;
        ai.m_fleeIfLowHealth = 0f;
        ai.m_fleeIfHurtWhenTargetCantBeReached = false;
        ai.m_circulateWhileCharging = false;
        ai.m_maxChaseDistance = 40f;
        ai.m_minAttackInterval = 1.5f;
        ai.m_consumeItems = new List<ItemDrop>();
    }
}
