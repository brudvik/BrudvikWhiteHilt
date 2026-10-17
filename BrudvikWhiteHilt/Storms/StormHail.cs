using UnityEngine;

namespace BrudvikWhiteHilt.Storms;

/// <summary>
/// Hail showers in the forest storm, at sea and in the blizzard: for a few seconds hailstones beat down on whoever is
/// in the open or only in the lee, as blunt hits a helmet and armour soften, and young crops with no roof over them break.
/// Trees, a roof or a cave keep it off. Each client hurts its own player and breaks the crops it owns.
/// On the <see cref="Game"/> object.
/// </summary>
public class StormHail : MonoBehaviour
{
    private const float HitShare = 0.015f;
    private const float HitSeconds = 1f;
    private const float CropReach = 30f;
    private const float CropChance = 0.25f;

    private float nextShower;
    private float showerEnds = float.NegativeInfinity;
    private float nextHit;
    private bool wasRaging;

    /// <summary>True while a hail shower beats down where the local player is.</summary>
    public static bool Hailing { get; private set; }

    private void Update()
    {
        Player player = Player.m_localPlayer;
        StormKind kind = StormClient.Profile.Kind;
        bool raging = player != null && StormClient.RagingHere && !player.IsDead() && StormSettings.Hail.Value
            && kind is StormKind.Tempest or StormKind.Sea or StormKind.Blizzard;
        if (!raging)
        {
            wasRaging = false;
            Hailing = false;
            return;
        }

        if (!wasRaging)
        {
            wasRaging = true;
            nextShower = Time.time + Random.Range(20f, 45f);
        }

        if (!Hailing && Time.time >= nextShower)
        {
            Hailing = true;
            showerEnds = Time.time + Random.Range(8f, 15f);
            nextHit = Time.time + 1f;
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_storm_hail");
            BreakCrops(player);
        }

        if (!Hailing)
        {
            return;
        }

        if (Time.time >= showerEnds)
        {
            Hailing = false;
            nextShower = Time.time + Random.Range(40f, 90f) / Mathf.Max(1f, StormClient.Fury);
            return;
        }

        if (Time.time >= nextHit)
        {
            nextHit = Time.time + HitSeconds;
            Hit(player);
        }
    }

    // One beat of hailstones, out in the open or in the lee only: the lee keeps the wind off, not what falls.
    private static void Hit(Player player)
    {
        if (StormClient.Cover is not (StormCover.Open or StormCover.Lee) || player.InGodMode())
        {
            return;
        }

        float amount = player.GetMaxHealth() * HitShare * Mathf.Max(1f, StormClient.Fury) * StormSettings.DamageMultiplier.Value;
        if (!StormSettings.CanKill.Value)
        {
            amount = Mathf.Min(amount, player.GetHealth() - 1f);
        }

        if (amount <= 0f)
        {
            return;
        }

        HitData hit = new()
        {
            m_point = player.GetHeadPoint(),
            m_dir = Vector3.down
        };
        hit.m_damage.m_blunt = amount;
        player.Damage(hit);
    }

    // Young crops this client owns, with no roof over them, may break when a shower starts.
    private static void BreakCrops(Player player)
    {
        if (ZNetScene.instance == null)
        {
            return;
        }

        foreach (Plant plant in FindObjectsByType<Plant>(FindObjectsSortMode.None))
        {
            if (plant == null || !plant.m_needCultivatedGround || plant.m_nview == null || !plant.m_nview.IsValid() || !plant.m_nview.IsOwner()
                || Vector3.Distance(plant.transform.position, player.transform.position) > CropReach || Random.value > CropChance)
            {
                continue;
            }

            Cover.GetCoverForPoint(plant.transform.position + Vector3.up, out _, out bool underRoof);
            if (!underRoof)
            {
                ZNetScene.instance.Destroy(plant.gameObject);
            }
        }
    }
}
