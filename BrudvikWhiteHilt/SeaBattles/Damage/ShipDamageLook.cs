using BrudvikWhiteHilt.Helpers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BrudvikWhiteHilt.SeaBattles.Damage;

/// <summary>
/// What a damaged ship looks like on one client: a splintered hole and a jet of water at each hole, and a sheet of
/// water over the deck that grows with the flood. Everything is plain child objects of the ship without a network view,
/// made from code, so it exists only on the machine that draws it and costs nothing on a dedicated server. The owner
/// <see cref="ShipDamage"/> builds the holes again when the list changes and moves the water when the flood changes.
/// </summary>
internal sealed class ShipDamageLook
{
    // The decal is this big, in metres across the hull and up it.
    private const float HoleWidth = 0.9f;
    private const float HoleHeight = 0.7f;

    // The decal sits this far off the hull's side so the planks do not hide it.
    private const float DecalLift = 0.02f;

    // Water on a full ship stands this high over the deck, and over the deck this much of the hull's width and length.
    private const float FloodDepth = 0.45f;
    private const float FloodWidthShare = 0.85f;
    private const float FloodLengthShare = 0.6f;
    private const float FloodLift = 0.03f;
    private const float FloodVisibleAbove = 0.02f;
    private const float FloodSmallestShare = 0.35f;

    // The deck is looked for with rays from above, this high over the hull's middle, at these places along and across it.
    private const float DeckRayHeight = 6f;
    private const float DeckMinUp = 0.7f;
    private const float DeckAboveWaterline = 0.64f;

    private const int TextureSize = 64;

    private static readonly float[] deckAlong = { -0.2f, -0.1f, 0.1f, 0.2f };
    private static readonly float[] deckAcross = { -0.5f, 0.5f };
    private static readonly RaycastHit[] deckHits = new RaycastHit[16];
    private static int deckMask;

    private static Mesh verticalQuad;
    private static Mesh horizontalQuad;
    private static Material holeMaterial;
    private static Material waterMaterial;
    private static Material fallbackSpray;
    private static Material vanillaSpray;

    private readonly ShipDamage damage;
    private readonly Transform ship;
    private readonly List<GameObject> marks = new();
    private GameObject root;
    private GameObject water;
    private float deck = float.NaN;

    /// <summary>
    /// Creates the look of one ship.
    /// </summary>
    /// <param name="damage">The ship's damage.</param>
    public ShipDamageLook(ShipDamage damage)
    {
        this.damage = damage;
        ship = damage.transform;
    }

    /// <summary>
    /// Draws a hole at each point and removes the ones drawn before.
    /// </summary>
    /// <param name="holes">The holes, in the ship's local space.</param>
    public void Rebuild(IReadOnlyList<Vector3> holes)
    {
        foreach (GameObject mark in marks)
        {
            if (mark != null)
            {
                Object.Destroy(mark);
            }
        }

        marks.Clear();
        EnsureRoot();
        for (int i = 0; i < holes.Count; i++)
        {
            marks.Add(BuildMark(holes[i]));
        }
    }

    /// <summary>
    /// Raises or lowers the sheet of water over the deck; it is hidden while the ship is nearly dry.
    /// </summary>
    /// <param name="flood">The flood, 0 to 1.</param>
    public void SetFlood(float flood)
    {
        if (flood < FloodVisibleAbove)
        {
            if (water != null && water.activeSelf)
            {
                water.SetActive(false);
            }

            return;
        }

        EnsureRoot();
        if (water == null)
        {
            water = CreateWater();
        }

        Vector3 centre = damage.HullCentre;
        Vector3 size = damage.HullSize;
        float deckY = Deck();
        float width = damage.HalfWidth(1f, deckY, centre.z) * 2f * FloodWidthShare;
        float grow = Mathf.Lerp(FloodSmallestShare, 1f, Mathf.Sqrt(Mathf.Clamp01(flood)));
        water.transform.localPosition = new Vector3(0f, deckY + FloodLift + FloodModel.Offset(flood, FloodDepth), centre.z);
        water.transform.localScale = new Vector3(width * grow, 1f, size.z * FloodLengthShare * grow);
        if (!water.activeSelf)
        {
            water.SetActive(true);
        }
    }

    /// <summary>
    /// Removes everything this look made.
    /// </summary>
    public void Dispose()
    {
        if (root != null)
        {
            Object.Destroy(root);
        }

        marks.Clear();
        water = null;
    }

    private void EnsureRoot()
    {
        if (root == null)
        {
            root = new GameObject("WhiteHiltShipDamage");
            root.transform.SetParent(ship, false);
        }
    }

    // A hole: a splintered decal on the hull's outside and a jet of water rising from just inside it.
    private GameObject BuildMark(Vector3 hole)
    {
        GameObject mark = new("WhiteHiltHole");
        mark.transform.SetParent(root.transform, false);
        mark.transform.localPosition = hole;

        Vector3 outward = new(hole.x >= 0f ? 1f : -1f, 0f, 0f);

        // The hull flares outward with height, so the decal leans back with it and lies flat on the planks.
        Vector3 normal = new Vector3(outward.x, -damage.Flare(outward.x, hole.y, hole.z), 0f).normalized;
        GameObject decal = new("Decal");
        decal.transform.SetParent(mark.transform, false);
        decal.transform.localPosition = outward * HoleRules.SideInset + normal * DecalLift;
        decal.transform.localRotation = Quaternion.LookRotation(normal);
        decal.transform.localScale = new Vector3(HoleWidth, HoleHeight, 1f);
        decal.AddComponent<MeshFilter>().sharedMesh = Quad(true);
        MeshRenderer renderer = decal.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = HoleMaterial();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        BuildSpray(mark.transform, (Vector3.up - outward * 0.6f).normalized);
        return mark;
    }

    // The water from the vanilla bow spray's material when the ship has one, else a soft white puff.
    private void BuildSpray(Transform parent, Vector3 direction)
    {
        GameObject spray = new("Spray");
        spray.transform.SetParent(parent, false);
        spray.transform.localRotation = Quaternion.LookRotation(direction);

        ParticleSystem particles = spray.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 3.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
        main.startColor = new Color(0.85f, 0.92f, 1f, 0.7f);
        main.gravityModifier = 0.9f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 80;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 30f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 14f;
        shape.radius = 0.08f;

        ParticleSystem.ColorOverLifetimeModule fade = particles.colorOverLifetime;
        fade.enabled = true;
        Gradient gradient = new();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;

        ParticleSystemRenderer renderer = spray.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = SprayMaterial();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        particles.Play();
    }

    private GameObject CreateWater()
    {
        GameObject sheet = new("Flood");
        sheet.transform.SetParent(root.transform, false);
        sheet.AddComponent<MeshFilter>().sharedMesh = Quad(false);
        MeshRenderer renderer = sheet.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = WaterMaterial();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return sheet;
    }

    // The deck's height in the ship's space: the middle of what rays from above hit along the hull's middle, so the mast,
    // a crate or the rail do not decide it. Measured once. Without a hit it is a longship's deck above the waterline.
    private float Deck()
    {
        if (!float.IsNaN(deck))
        {
            return deck;
        }

        deck = damage.HullCentre.y + DeckAboveWaterline;
        Rigidbody body = damage.Body;
        if (body == null)
        {
            return deck;
        }

        if (deckMask == 0)
        {
            deckMask = LayerMask.GetMask("vehicle", "piece", "static_solid", "Default", "Default_small");
        }

        List<float> heights = new();
        Vector3 centre = damage.HullCentre;
        Vector3 size = damage.HullSize;
        foreach (float along in deckAlong)
        {
            foreach (float across in deckAcross)
            {
                Vector3 origin = ship.TransformPoint(new Vector3(across, centre.y + DeckRayHeight, centre.z + along * size.z));
                int count = Physics.RaycastNonAlloc(origin, -ship.up, deckHits, DeckRayHeight * 2f, deckMask, QueryTriggerInteraction.Ignore);
                float nearest = float.MaxValue;
                float height = float.NaN;
                for (int i = 0; i < count; i++)
                {
                    RaycastHit hit = deckHits[i];
                    if (hit.collider.attachedRigidbody == body && hit.distance < nearest && Vector3.Dot(hit.normal, ship.up) >= DeckMinUp)
                    {
                        nearest = hit.distance;
                        height = ship.InverseTransformPoint(hit.point).y;
                    }
                }

                if (!float.IsNaN(height))
                {
                    heights.Add(height);
                }
            }
        }

        if (heights.Count > 0)
        {
            heights.Sort();
            deck = heights[heights.Count / 2];
        }

        return deck;
    }

    private Material SprayMaterial()
    {
        if (vanillaSpray == null)
        {
            // The longship's bow spray; the ship's own copy is used so no prefab lookup is needed.
            ParticleSystemRenderer source = ship.Find("watereffects/SpeedWake/GameObject")?.GetComponent<ParticleSystemRenderer>();
            if (source != null && source.sharedMaterial != null)
            {
                vanillaSpray = source.sharedMaterial;
                Jotunn.Logger.LogInfo($"Ship damage: hole spray uses the material '{vanillaSpray.name}' of the ship's bow spray.");
            }
        }

        if (vanillaSpray != null)
        {
            return vanillaSpray;
        }

        if (fallbackSpray == null)
        {
            fallbackSpray = new Material(Shader.Find("Sprites/Default")) { name = "WhiteHiltSpray", mainTexture = SprayTexture(), hideFlags = HideFlags.DontUnloadUnusedAsset };
        }

        return fallbackSpray;
    }

    private static Material HoleMaterial()
    {
        if (holeMaterial == null)
        {
            holeMaterial = new Material(Shader.Find("Sprites/Default")) { name = "WhiteHiltHole", mainTexture = HoleTexture(), hideFlags = HideFlags.DontUnloadUnusedAsset };
        }

        return holeMaterial;
    }

    private static Material WaterMaterial()
    {
        if (waterMaterial == null)
        {
            waterMaterial = new Material(Shader.Find("Sprites/Default")) { name = "WhiteHiltFlood", color = new Color(0.16f, 0.34f, 0.38f, 0.55f), hideFlags = HideFlags.DontUnloadUnusedAsset };
        }

        return waterMaterial;
    }

    // A flat square, upright (in the x-y plane) or lying (in the x-z plane). The materials draw both sides.
    private static Mesh Quad(bool upright)
    {
        ref Mesh cached = ref upright ? ref verticalQuad : ref horizontalQuad;
        if (cached == null)
        {
            cached = new Mesh
            {
                name = upright ? "WhiteHiltUprightQuad" : "WhiteHiltLyingQuad",
                hideFlags = HideFlags.DontUnloadUnusedAsset,
                vertices = upright
                    ? new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f) }
                    : new[] { new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(-0.5f, 0f, 0.5f) },
                uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) },
                triangles = new[] { 0, 1, 2, 0, 2, 3 }
            };
            cached.RecalculateBounds();
        }

        return cached;
    }

    // A ragged hole: dark inside, broken planks round it, clear outside. The edge swings in and out round the circle so
    // it reads as splinters.
    private static Texture2D HoleTexture()
    {
        Texture2D texture = new(TextureSize, TextureSize, TextureFormat.RGBA32, false)
        {
            name = "WhiteHiltHole",
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontUnloadUnusedAsset
        };
        Color32[] pixels = new Color32[TextureSize * TextureSize];
        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                float u = (x + 0.5f) / TextureSize * 2f - 1f;
                float v = (y + 0.5f) / TextureSize * 2f - 1f;
                float radius = Mathf.Sqrt(u * u + v * v);
                float angle = Mathf.Atan2(v, u);
                float edge = 0.62f + 0.22f * Mathf.Abs(Mathf.Sin(angle * 3.5f)) + 0.08f * Mathf.Sin(angle * 11f + 1.3f);
                Color32 colour;
                if (radius > edge)
                {
                    colour = new Color32(0, 0, 0, 0);
                }
                else if (radius > edge - 0.18f)
                {
                    byte grain = (byte)(110 + 25 * Mathf.Sin(x * 0.9f));
                    colour = new Color32(grain, (byte)(grain * 0.72f), (byte)(grain * 0.43f), 255);
                }
                else
                {
                    colour = new Color32(15, 10, 8, 255);
                }

                pixels[y * TextureSize + x] = colour;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    // A soft round puff for the spray when the ship has no spray material to borrow.
    private static Texture2D SprayTexture()
    {
        Texture2D texture = new(TextureSize, TextureSize, TextureFormat.RGBA32, false)
        {
            name = "WhiteHiltSpray",
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontUnloadUnusedAsset
        };
        Color32[] pixels = new Color32[TextureSize * TextureSize];
        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                float u = (x + 0.5f) / TextureSize * 2f - 1f;
                float v = (y + 0.5f) / TextureSize * 2f - 1f;
                float fall = Mathf.Clamp01(1f - Mathf.Sqrt(u * u + v * v));
                pixels[y * TextureSize + x] = new Color32(255, 255, 255, (byte)(255f * fall * fall));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }
}
