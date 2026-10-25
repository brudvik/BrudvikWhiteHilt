using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Difficulty.Beasts;

/// <summary>
/// Sits on every beast. By day, when no player is near, the beast sinks into the ground, so none linger in the world. A
/// pack hunts the nearest player out in the open and gives up once every one of them has found a fire or a roof.
/// </summary>
public class BeastBehaviour : MonoBehaviour
{
    private const float CheckInterval = 5f;
    private const float QuietRange = 40f;
    private const string DespawnEffect = "vfx_odin_despawn";
    private const float HuntRange = 120f;
    private const int LostChecks = 4;

    private static readonly HashSet<BeastBehaviour> instances = new();

    private ZNetView nview;
    private Character character;
    private float nextCheck;
    private bool pack;
    private int lostChecks;

    /// <summary>Returns Odin's retained healing share near a living black beast.</summary>
    /// <param name="target">The character benefiting from Odin.</param>
    /// <returns>The configured share within range, or full strength otherwise.</returns>
    public static float OdinHealingFactor(Character target)
    {
        float range = DifficultySettings.OdinHealingRange.Value;
        if (target == null || range <= 0f)
        {
            return 1f;
        }

        foreach (BeastBehaviour beast in instances)
        {
            if (beast != null && beast.nview != null && beast.nview.IsValid()
                && beast.character != null && !beast.character.IsDead()
                && (target.transform.position - beast.transform.position).sqrMagnitude < range * range)
            {
                return Mathf.Clamp01(DifficultySettings.OdinHealingShare.Value);
            }
        }

        return 1f;
    }

    /// <summary>
    /// The nearest living pack beast.
    /// </summary>
    /// <param name="position">Where from.</param>
    /// <param name="range">How far to look.</param>
    /// <returns>The beast, or null.</returns>
    public static Transform NearestPack(Vector3 position, float range)
    {
        Transform best = null;
        float bestDistance = range * range;
        foreach (BeastBehaviour beast in instances)
        {
            if (beast == null || !beast.pack || beast.character == null || beast.character.IsDead())
            {
                continue;
            }

            float distance = (beast.transform.position - position).sqrMagnitude;
            if (distance < bestDistance)
            {
                best = beast.transform;
                bestDistance = distance;
            }
        }

        return best;
    }

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        character = GetComponent<Character>();
        pack = BeastDefinition.ByPrefab(Utils.GetPrefabName(gameObject))?.Pack == true;
        nextCheck = Time.time + CheckInterval;
        instances.Add(this);
    }

    private void OnDestroy()
    {
        instances.Remove(this);
    }

    // On the owner: when night ends and no player is near, the beast vanishes in a puff.
    private void Update()
    {
        if (Time.time < nextCheck)
        {
            return;
        }

        nextCheck = Time.time + CheckInterval;
        if (nview == null || !nview.IsValid() || !nview.IsOwner() || character == null || character.IsDead())
        {
            return;
        }

        if (pack && Hunt())
        {
            return;
        }

        float quietRange = nview.GetZDO().GetBool(Items.Summoning.SummoningHornService.SummonedKey)
            ? Items.Summoning.SummoningHornService.EncounterRange.Value : QuietRange;
        if (!DifficultySettings.DespawnAtDawn.Value || DifficultyState.IsNightHour(DifficultyState.Hours())
            || Player.IsPlayerInRange(transform.position, quietRange))
        {
            return;
        }

        Vanish();
    }

    // A pack goes for the nearest player out in the open. Once none is left, it waits a little and then is gone.
    // Returns true when it is gone.
    private bool Hunt()
    {
        Player prey = null;
        float best = HuntRange;
        foreach (Player player in Player.GetAllPlayers())
        {
            float distance = player != null ? Vector3.Distance(player.transform.position, transform.position) : float.MaxValue;
            if (distance < best && !player.IsDead() && !Darkness.DeepNorthNight.Sheltered(player))
            {
                prey = player;
                best = distance;
            }
        }

        if (prey != null)
        {
            lostChecks = 0;
            if (character.GetBaseAI() is MonsterAI ai && ai.m_targetCreature == null)
            {
                ai.m_targetCreature = prey;
                ai.SetAlerted(true);
            }

            return false;
        }

        if (++lostChecks < LostChecks)
        {
            return false;
        }

        Vanish();
        return true;
    }

    private void Vanish()
    {
        GameObject effect = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(DespawnEffect) : null;
        if (effect != null)
        {
            Instantiate(effect, transform.position, Quaternion.identity);
        }

        nview.Destroy();
    }
}
