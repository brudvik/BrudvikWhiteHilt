namespace BrudvikWhiteHilt.Monsters;

/// <summary>
/// Sits on the Lava Worm. It rises out of the lava like the Lindorm out of the ground, day or night, and sinks back when
/// it has lost its prey or followed it too far from the lava it rose from.
/// </summary>
public class LavaWormBurrow : LindormBurrow
{
    /// <inheritdoc/>
    protected override float Depth => 3.5f * LavaWormSettings.Scale.Value;

    /// <inheritdoc/>
    protected override bool LeavesAtDawn => false;

    /// <inheritdoc/>
    protected override float GiveUpSeconds => LavaWormSettings.GiveUpSeconds.Value;

    /// <inheritdoc/>
    protected override float LeashRange => LavaWormSettings.LeashRange.Value;

    /// <inheritdoc/>
    protected override string RiseMessage => "$msg_whitehilt_lavaworm_rises";
}
