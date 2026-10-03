namespace BrudvikWhiteHilt.Items.Meads.LabradorTeaBrew;

/// <summary>
/// Marks the drinker in their player data, so creatures on any machine can tell that biting insects should leave them alone.
/// </summary>
public class InsectWardEffect : SE_Stats
{
    /// <summary>
    /// Player data key that is true while the brew is active.
    /// </summary>
    public const string WardKey = "whitehilt_insectward";

    private float refreshTimer;

    /// <inheritdoc/>
    public override void Setup(Character character)
    {
        base.Setup(character);
        SetWard(true);
    }

    /// <inheritdoc/>
    public override void UpdateStatusEffect(float dt)
    {
        base.UpdateStatusEffect(dt);
        refreshTimer -= dt;
        if (refreshTimer <= 0f)
        {
            refreshTimer = 5f;
            SetWard(true);
        }
    }

    /// <inheritdoc/>
    public override void Stop()
    {
        SetWard(false);
        base.Stop();
    }

    private void SetWard(bool on)
    {
        ZNetView view = m_character != null ? m_character.m_nview : null;
        if (view != null && view.IsValid() && view.IsOwner() && view.GetZDO().GetBool(WardKey) != on)
        {
            view.GetZDO().Set(WardKey, on);
        }
    }
}
