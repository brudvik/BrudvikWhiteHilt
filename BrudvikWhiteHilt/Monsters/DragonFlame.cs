using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Monsters;

/// <summary>
/// Sits on each flame of a dragon's breath: it leaves the mouth about as wide as the mouth and swells as it flies, so the
/// stream widens towards the ground, and its hit radius grows with it. Runs on every client from the flame's own age.
/// </summary>
public class DragonFlame : MonoBehaviour
{
    /// <summary>Size of the fire when it leaves the mouth, as a multiple of the vanilla fireball.</summary>
    public float StartSize = 0.7f;

    /// <summary>Size of the fire at <see cref="GrowSeconds"/>, as a multiple of the vanilla fireball.</summary>
    public float EndSize = 4f;

    /// <summary>Hit radius, in metres, when it leaves the mouth.</summary>
    public float StartRadius = 0.3f;

    /// <summary>Hit radius, in metres, at <see cref="GrowSeconds"/>.</summary>
    public float EndRadius = 1.5f;

    /// <summary>Seconds it takes to reach its full size.</summary>
    public float GrowSeconds = 1f;

    private Projectile projectile;
    private Transform[] parts;
    private Vector3[] scales;
    private float born;

    private void Awake()
    {
        born = Time.time;
        projectile = GetComponent<Projectile>();
        parts = GetComponentsInChildren<ParticleSystem>(true).Select(system => system.transform).ToArray();
        scales = parts.Select(part => part.localScale).ToArray();
        Grow(0f);
    }

    private void Update()
    {
        Grow(Mathf.Clamp01((Time.time - born) / Mathf.Max(0.05f, GrowSeconds)));
    }

    private void Grow(float progress)
    {
        float size = Mathf.Lerp(StartSize, EndSize, progress);
        for (int i = 0; i < parts.Length; i++)
        {
            parts[i].localScale = scales[i] * size;
        }

        if (projectile != null)
        {
            projectile.m_rayRadius = Mathf.Lerp(StartRadius, EndRadius, progress);
        }
    }
}
