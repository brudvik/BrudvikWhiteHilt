using UnityEngine;

namespace BrudvikWhiteHilt.Difficulty;

/// <summary>
/// Remembers how much a creature was grown for its stars, so a later change of level scales it from there.
/// </summary>
public class StarScale : MonoBehaviour
{
    /// <summary>
    /// Growth factor applied to the creature's root.
    /// </summary>
    public float Applied { get; set; } = 1f;
}
