using UnityEngine;

namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// Sits on every plague's monster (<see cref="TradingPostTerror"/>). On its owner it keeps the monster at three stars,
/// takes it back to its lair by day when it has been down at the post and nobody is near, and lets it leave the land
/// when the plague's time is up.
/// </summary>
public class TerrorBehaviour : MonoBehaviour
{
    private const float CheckInterval = 5f;
    private const float QuietRange = 60f;
    private const float HomeRange = 80f;
    private const string LeaveEffect = "vfx_odin_despawn";

    private ZNetView view;
    private Character character;
    private float nextCheck;

    private void Awake()
    {
        view = GetComponent<ZNetView>();
        character = GetComponent<Character>();
        nextCheck = Time.time + 1f;
    }

    private void Update()
    {
        if (Time.time < nextCheck)
        {
            return;
        }

        nextCheck = Time.time + CheckInterval;
        if (view == null || !view.IsValid() || !view.IsOwner() || character == null || character.IsDead() || ZNet.instance == null)
        {
            return;
        }

        if (character.GetLevel() != TradingPostTerrors.Level)
        {
            character.SetLevel(TradingPostTerrors.Level);
        }

        ZDO zdo = view.GetZDO();
        long leaves = zdo.GetLong(TradingPostKeys.TerrorLeaves);
        if (leaves == 0 || ZNet.instance.GetTimeSeconds() >= leaves)
        {
            Leave();
            return;
        }

        Vector3 lair = zdo.GetVec3(TradingPostKeys.TerrorHome, transform.position);
        if (EnvMan.IsNight() || Utils.DistanceXZ(transform.position, lair) < HomeRange || Player.IsPlayerInRange(transform.position, QuietRange))
        {
            return;
        }

        // Back to the lair by day, out of everybody's sight.
        transform.position = lair;
        zdo.SetPosition(lair);
        zdo.Set(ZDOVars.s_spawnPoint, lair);
        BaseAI ai = character.GetBaseAI();
        if (ai != null)
        {
            ai.m_spawnPoint = lair;
            ai.SetHuntPlayer(false);
        }
    }

    // When the plague is over, the monster vanishes in a puff.
    private void Leave()
    {
        GameObject effect = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(LeaveEffect) : null;
        if (effect != null)
        {
            Instantiate(effect, transform.position, Quaternion.identity);
        }

        view.Destroy();
    }
}
