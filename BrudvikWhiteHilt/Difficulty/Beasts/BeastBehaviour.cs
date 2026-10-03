using UnityEngine;

namespace BrudvikWhiteHilt.Difficulty.Beasts;

/// <summary>
/// Sits on every beast. By day, when no player is near, the beast sinks into the ground, so none linger in the world.
/// </summary>
public class BeastBehaviour : MonoBehaviour
{
    private const float CheckInterval = 5f;
    private const float QuietRange = 40f;
    private const string DespawnEffect = "vfx_odin_despawn";

    private ZNetView nview;
    private Character character;
    private float nextCheck;

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        character = GetComponent<Character>();
        nextCheck = Time.time + CheckInterval;
    }

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

        float quietRange = nview.GetZDO().GetBool(Items.Summoning.SummoningHornService.SummonedKey)
            ? Items.Summoning.SummoningHornService.EncounterRange.Value : QuietRange;
        if (!DifficultySettings.DespawnAtDawn.Value || DifficultyState.IsNightHour(DifficultyState.Hours())
            || Player.IsPlayerInRange(transform.position, quietRange))
        {
            return;
        }

        GameObject effect = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(DespawnEffect) : null;
        if (effect != null)
        {
            Instantiate(effect, transform.position, Quaternion.identity);
        }

        nview.Destroy();
    }
}
