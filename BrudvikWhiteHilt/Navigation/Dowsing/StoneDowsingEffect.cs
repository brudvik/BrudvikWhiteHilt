using UnityEngine;

namespace BrudvikWhiteHilt.Navigation.Dowsing;

/// <summary>
/// The Stone Dowser's equip effect. Like the Wishbone it pings toward the nearest rock within range, faster the closer
/// you come, and it keeps <see cref="StoneDowsingService"/> asking the server for the nearest clearing with rocks left.
/// </summary>
public class StoneDowsingEffect : StatusEffect
{
    private float lookTimer;
    private float pingTimer;
    private Pickable target;

    /// <summary>Ping close to a rock.</summary>
    public EffectList PingNear { get; set; } = new();

    /// <summary>Ping at middle distance.</summary>
    public EffectList PingMedium { get; set; } = new();

    /// <summary>Ping far from a rock.</summary>
    public EffectList PingFar { get; set; } = new();

    /// <summary>Seconds between pings right beside a rock.</summary>
    public float CloseInterval { get; set; } = 1f;

    /// <summary>Seconds between pings at the edge of the range.</summary>
    public float DistantInterval { get; set; } = 5f;

    /// <summary>
    /// Pings toward the nearest rock and asks for the nearest clearing now and then.
    /// </summary>
    /// <param name="dt">Seconds since the last update.</param>
    public override void UpdateStatusEffect(float dt)
    {
        base.UpdateStatusEffect(dt);
        if (m_character == null || m_character != Player.m_localPlayer)
        {
            return;
        }

        Transform body = m_character.transform;
        StoneDowsingService.Tick(body.position);

        float range = StoneDowsingSettings.PingRange.Value;
        lookTimer += dt;
        if (lookTimer > 1f)
        {
            lookTimer = 0f;
            Pickable found = StoneDowsingTargets.FindClosest(body.position, range);
            if (found != target)
            {
                target = found;
                pingTimer = 0f;
            }
        }

        if (target == null)
        {
            return;
        }

        float share = Mathf.Clamp01(Utils.DistanceXZ(body.position, target.transform.position) / range);
        pingTimer += dt;
        if (pingTimer <= Mathf.Lerp(CloseInterval, DistantInterval, share))
        {
            return;
        }

        pingTimer = 0f;
        EffectList ping = share < 0.2f ? PingNear : share < 0.6f ? PingMedium : PingFar;
        ping.Create(body.position, body.rotation, body, 1f, -1, m_character.GetZDOID());
    }

    /// <summary>
    /// Takes the clearing off the map when the dowser is taken off.
    /// </summary>
    public override void Stop()
    {
        base.Stop();
        if (m_character != null && m_character == Player.m_localPlayer)
        {
            StoneDowsingService.Clear();
        }
    }
}
