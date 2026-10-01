using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Portals.WhiteHiltPortal;

/// <summary>
/// Lights the rim of the dark stone under a White Hilt Rune Circle: faint from afar, a little stronger as the local
/// player comes close. Each portal glows on its own copy of the material, so the hammer's highlight still shows on it.
/// </summary>
public class PortalBaseGlow : MonoBehaviour
{
    /// <summary>
    /// Name of the stone model under the portal.
    /// </summary>
    public const string ModelName = "portalbase_model";

    /// <summary>
    /// Glow strength seen from afar, also used on the prefab itself (icon, placement ghost).
    /// </summary>
    public const float FarStrength = 0.4f;

    private const float NearStrength = 1.6f;
    private const float NearDistance = 3f;
    private const float FarDistance = 12f;
    private const float FadeSpeed = 1.5f;

    /// <summary>
    /// Colour of the glow.
    /// </summary>
    public static readonly Color GlowColor = new(0.2f, 0.5f, 1f);

    private static readonly int emissionColor = Shader.PropertyToID("_EmissionColor");

    private Material material;
    private float strength = -1f;

    private void Awake()
    {
        Renderer stone = transform.Find(ModelName)?.GetComponent<Renderer>();
        if (stone == null)
        {
            enabled = false;
            return;
        }

        material = stone.material;
    }

    private void OnDestroy()
    {
        if (material != null)
        {
            Destroy(material);
        }
    }

    private void Update()
    {
        Player player = Player.m_localPlayer;
        float distance = player != null ? Vector3.Distance(player.transform.position, transform.position) : FarDistance;
        float closeness = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(FarDistance, NearDistance, distance));
        float goal = Mathf.Lerp(FarStrength, NearStrength, closeness);
        float next = strength < 0f ? goal : Mathf.MoveTowards(strength, goal, FadeSpeed * Time.deltaTime);
        if (Mathf.Approximately(next, strength))
        {
            return;
        }

        strength = next;
        material.SetColor(emissionColor, GlowColor * strength);
    }
}
