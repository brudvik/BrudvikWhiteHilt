namespace BrudvikWhiteHilt.Companions;

/// <summary>
/// Short things the dog does on its own or on command, shown by <see cref="DogExpression"/> on every client.
/// </summary>
public enum DogAction
{
    /// <summary>Nothing.</summary>
    None = 0,

    /// <summary>Sits and gives its paw.</summary>
    Paw = 1,

    /// <summary>Lies down and rolls over.</summary>
    Roll = 2,

    /// <summary>Shakes the water off.</summary>
    Shake = 3,

    /// <summary>Yawns.</summary>
    Yawn = 4,

    /// <summary>Stretches with its front legs forward.</summary>
    Stretch = 5,

    /// <summary>Sits and scratches behind the ear with a hind leg.</summary>
    Scratch = 6,

    /// <summary>Rolls onto its back for a belly rub.</summary>
    Belly = 7
}

/// <summary>
/// Timing and poses of the <see cref="DogAction"/>s.
/// </summary>
public static class DogActions
{
    /// <summary>
    /// Seconds an action lasts.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <returns>Its length.</returns>
    public static float Duration(DogAction action)
    {
        return action switch
        {
            DogAction.Paw => 3f,
            DogAction.Roll => 2.2f,
            DogAction.Shake => 1.4f,
            DogAction.Yawn => 2f,
            DogAction.Stretch => 2.5f,
            DogAction.Scratch => 3f,
            DogAction.Belly => 6f,
            _ => 0f
        };
    }

    /// <summary>
    /// The resting pose the action is done in, or <see cref="RestPose.Pose.None"/> for standing.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <returns>The pose.</returns>
    public static RestPose.Pose PoseFor(DogAction action)
    {
        return action switch
        {
            DogAction.Paw or DogAction.Scratch => RestPose.Pose.Sit,
            DogAction.Roll or DogAction.Belly => RestPose.Pose.Lie,
            _ => RestPose.Pose.None
        };
    }
}
