using BrudvikWhiteHilt.Darkness;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Storms;

/// <summary>
/// Who of Ragnarok's host a creature is.
/// </summary>
public enum RagnarokHostKind
{
    /// <summary>The dead who rise from the earth.</summary>
    Dead,

    /// <summary>Surt's host out of Muspelheim.</summary>
    Surt,

    /// <summary>Fenrir.</summary>
    Fenrir
}

/// <summary>
/// A creature of Ragnarok's host. It keeps the number of the storm that brought it and goes back where it came from
/// when that storm passes, or when no player is near. The dead and Surt's host hunt players and break what is built
/// in their way; Fenrir hunts the nearest player out in the open, loses the scent of one who reaches a roof, a fire
/// or a cave, and goes when no one is left in the open. No weapon bites on Fenrir. Only the owner thinks.
/// </summary>
public class RagnarokHost : MonoBehaviour
{
    /// <summary>Who of the host it is; set on the prefab.</summary>
    public RagnarokHostKind m_kind;

    private const string StormKey = "whitehilt_ragnarok_storm";
    private const float TickSeconds = 3f;
    private const float PlayerReach = 150f;
    private const int FenrirPatience = 3;

    private static readonly List<RagnarokHost> loaded = new();

    private ZNetView view;
    private Character character;
    private MonsterAI ai;
    private float nextTick;
    private int withoutPrey;

    /// <summary>The host loaded on this machine.</summary>
    public static IReadOnlyList<RagnarokHost> Loaded => loaded;

    /// <summary>Whether this is Fenrir, whom no weapon bites on.</summary>
    public bool IsFenrir => m_kind == RagnarokHostKind.Fenrir;

    /// <summary>
    /// How many of a kind are loaded within a distance of a point.
    /// </summary>
    /// <param name="kind">The kind.</param>
    /// <param name="point">The point.</param>
    /// <param name="range">The distance.</param>
    /// <returns>The count.</returns>
    public static int CountNear(RagnarokHostKind kind, Vector3 point, float range)
    {
        return loaded.Count(host => host != null && host.m_kind == kind && Vector3.Distance(host.transform.position, point) <= range);
    }

    /// <summary>
    /// The nearest loaded Fenrir within a distance, or null.
    /// </summary>
    /// <param name="point">The point.</param>
    /// <param name="range">The distance.</param>
    /// <returns>Fenrir.</returns>
    public static RagnarokHost NearestFenrir(Vector3 point, float range)
    {
        return loaded.Where(host => host != null && host.IsFenrir && Vector3.Distance(host.transform.position, point) <= range)
            .OrderBy(host => Vector3.Distance(host.transform.position, point))
            .FirstOrDefault();
    }

    /// <summary>
    /// Marks a creature just made as come in the storm now. Call on the machine that made it.
    /// </summary>
    /// <param name="creature">The creature.</param>
    public static void Mark(Character creature)
    {
        ZDO zdo = creature != null && creature.m_nview != null ? creature.m_nview.GetZDO() : null;
        zdo?.Set(StormKey, StormState.Number);
    }

    private void Awake()
    {
        view = GetComponent<ZNetView>();
        character = GetComponent<Character>();
        ai = GetComponent<MonsterAI>();
        nextTick = Time.time + TickSeconds;
    }

    private void OnEnable()
    {
        loaded.Add(this);
    }

    private void OnDisable()
    {
        loaded.Remove(this);
    }

    private void Update()
    {
        if (Time.time < nextTick || view == null || !view.IsValid() || !view.IsOwner())
        {
            return;
        }

        nextTick = Time.time + TickSeconds;
        if (!StormHolds() || Player.GetClosestPlayer(transform.position, PlayerReach) == null)
        {
            Leave();
            return;
        }

        if (IsFenrir)
        {
            Hunt();
        }
    }

    // While the storm that brought it still rages, or gathers again after its eye.
    private bool StormHolds()
    {
        return StormSettings.Enabled.Value && StormState.Ragnarok && StormState.Raging
            && view.GetZDO().GetInt(StormKey) == StormState.Number;
    }

    // Fenrir goes for the nearest player out in the open; with no one left in the open, he gives up.
    private void Hunt()
    {
        Player prey = Player.GetAllPlayers()
            .Where(player => player != null && !player.IsDead() && !DeepNorthNight.Sheltered(player)
                             && Vector3.Distance(player.transform.position, transform.position) <= PlayerReach)
            .OrderBy(player => Vector3.Distance(player.transform.position, transform.position))
            .FirstOrDefault();
        if (prey == null)
        {
            if (++withoutPrey >= FenrirPatience)
            {
                Leave();
            }

            return;
        }

        withoutPrey = 0;
        if (ai != null && ai.GetTargetCreature() != prey)
        {
            ai.m_targetCreature = prey;
            ai.SetAlerted(true);
        }
    }

    // Back into the earth, the fire or the dark it came from.
    private void Leave()
    {
        string effect = m_kind == RagnarokHostKind.Dead ? "vfx_MudDestroyed" : "vfx_odin_despawn";
        GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(effect) : null;
        if (prefab != null)
        {
            Instantiate(prefab, transform.position, Quaternion.identity);
        }

        view.Destroy();
    }
}
