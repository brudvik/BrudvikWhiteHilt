using System;

namespace BrudvikWhiteHilt.Items.Meads.HuldraDraught;

/// <summary>
/// The Huldra Draught's veil. It runs on the drinker's own machine, but creatures may be run by another, so it marks the
/// drinker's network data while it lasts; the patches on the creatures' senses (<c>HuldraVeilPatches</c>) read that mark
/// wherever they run.
/// </summary>
public class HuldraVeilEffect : SE_Stats
{
    /// <summary>ZDO key that marks a veiled player.</summary>
    public static readonly int VeiledKey = "whitehilt_huldra_veiled".GetStableHashCode();

    /// <summary>Metres within which creatures still sense a veiled player; set by the draught from its config.</summary>
    public static Func<float> RevealRange = () => 2.5f;

    /// <summary>
    /// Whether a creature is a veiled player that the creature at <paramref name="from"/> cannot sense.
    /// </summary>
    /// <param name="target">The creature sensed.</param>
    /// <param name="from">Where the senses are.</param>
    /// <returns>True when the target is hidden from there.</returns>
    public static bool HiddenFrom(Character target, UnityEngine.Vector3 from)
    {
        if (target is not Player || target.m_nview == null || !target.m_nview.IsValid() || !target.m_nview.GetZDO().GetBool(VeiledKey))
        {
            return false;
        }

        float range = RevealRange();
        return (target.transform.position - from).sqrMagnitude > range * range;
    }

    /// <inheritdoc/>
    public override void Setup(Character character)
    {
        base.Setup(character);
        Mark(true);
    }

    /// <inheritdoc/>
    public override void Stop()
    {
        Mark(false);
        base.Stop();
    }

    private void Mark(bool veiled)
    {
        if (m_character != null && m_character.m_nview != null && m_character.m_nview.IsValid() && m_character.m_nview.IsOwner())
        {
            m_character.m_nview.GetZDO().Set(VeiledKey, veiled);
        }
    }
}
