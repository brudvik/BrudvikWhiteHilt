using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Painting;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Weapons;

/// <summary>
/// The glow a Glow Rune etches into a White Hilt weapon or shield: its colour is kept on the item, the holder writes the
/// colours of what is in their hands to their ZDO, and every machine lights those models with it. A model with an
/// emission map (its own, or one made from the "glow" boxes of its paint.json) glows only where the map is lit; every
/// glowing model also gets a soft light in the colour.
/// </summary>
public static class WeaponGlow
{
    private const string ColorKey = "whitehilt_glow";
    private const string LightName = "whitehilt_glow_light";
    private const float Intensity = 1.2f;
    private const float LightRange = 2f;
    private const float LightIntensity = 1f;
    private const int DarkSum = 24;

    private static readonly int rightKey = "whitehilt_glow_right".GetStableHashCode();
    private static readonly int leftKey = "whitehilt_glow_left".GetStableHashCode();
    private static readonly Dictionary<string, Texture2D> emissionByMesh = new();

    /// <summary>
    /// Registers the English texts.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglish("whitehilt_glow_line", "Glow: {0}");
    }

    /// <summary>
    /// Whether a colour is too dark to glow; etching it puts a glow out.
    /// </summary>
    /// <param name="color">The colour.</param>
    /// <returns>True for black or nearly black.</returns>
    public static bool IsDark(Color32 color) => color.r + color.g + color.b < DarkSum;

    /// <summary>
    /// The glow colour etched into an item.
    /// </summary>
    /// <param name="item">The item, or null.</param>
    /// <param name="color">The colour.</param>
    /// <returns>False if the item does not glow.</returns>
    public static bool TryGetColor(ItemDrop.ItemData item, out Color32 color)
    {
        color = default;
        return item?.m_customData != null && item.m_customData.TryGetValue(ColorKey, out string hex) && PaintColor.TryParseHex(hex, out color);
    }

    /// <summary>
    /// Etches a glow colour into an item, or puts its glow out for a dark colour.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="color">The colour.</param>
    public static void SetColor(ItemDrop.ItemData item, Color32 color)
    {
        if (IsDark(color))
        {
            item.m_customData.Remove(ColorKey);
        }
        else
        {
            item.m_customData[ColorKey] = PaintColor.ToHex(color);
        }
    }

    /// <summary>
    /// Takes the glow off an item.
    /// </summary>
    /// <param name="item">The item.</param>
    public static void Clear(ItemDrop.ItemData item)
    {
        item.m_customData.Remove(ColorKey);
    }

    /// <summary>
    /// The tooltip line of a glowing item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The line, or nothing if it does not glow.</returns>
    public static string TooltipText(ItemDrop.ItemData item)
    {
        return TryGetColor(item, out Color32 color)
            ? "\n" + string.Format(Localization.instance.Localize("$whitehilt_glow_line"), PaintColor.Swatch(color))
            : string.Empty;
    }

    /// <summary>
    /// Keeps the models in a character's hands glowing: the holder writes the glow of what they hold to their ZDO, and
    /// every machine lights the models with it. Called every frame for every character's equipment.
    /// </summary>
    /// <param name="equipment">The character's equipment visuals.</param>
    public static void Refresh(VisEquipment equipment)
    {
        ZNetView nview = equipment.m_nview;
        if (nview == null || !nview.IsValid())
        {
            return;
        }

        ZDO zdo = nview.GetZDO();
        if (nview.IsOwner() && equipment.TryGetComponent(out Player player))
        {
            Write(zdo, rightKey, Value(player.m_rightItem));
            Write(zdo, leftKey, Value(player.m_leftItem));
        }

        int right = zdo.GetInt(rightKey);
        int left = zdo.GetInt(leftKey);
        WeaponGlowState state = equipment.GetComponent<WeaponGlowState>();
        if (state == null)
        {
            if (right == 0 && left == 0)
            {
                return;
            }

            state = equipment.gameObject.AddComponent<WeaponGlowState>();
        }

        if (state.Right != right || state.RightInstance != equipment.m_rightItemInstance)
        {
            state.Right = right;
            state.RightInstance = equipment.m_rightItemInstance;
            Apply(equipment.m_rightItemInstance, right);
        }

        if (state.Left != left || state.LeftInstance != equipment.m_leftItemInstance)
        {
            state.Left = left;
            state.LeftInstance = equipment.m_leftItemInstance;
            Apply(equipment.m_leftItemInstance, left);
        }
    }

    /// <summary>
    /// Lights a dropped item that glows.
    /// </summary>
    /// <param name="item">The dropped item.</param>
    public static void ApplyDropped(ItemDrop item)
    {
        if (TryGetColor(item.m_itemData, out _))
        {
            Apply(item.gameObject, Value(item.m_itemData));
        }
    }

    // The packed colour of an item's glow for the ZDO, 0 for none.
    private static int Value(ItemDrop.ItemData item)
    {
        return TryGetColor(item, out Color32 color) ? PaintColor.Pack(color, PaintMode.Paint) : 0;
    }

    private static void Write(ZDO zdo, int key, int value)
    {
        if (zdo.GetInt(key) != value)
        {
            zdo.Set(key, value);
        }
    }

    // Lights a model in a colour (a packed value), or puts its glow out for 0.
    private static void Apply(GameObject root, int value)
    {
        if (root == null)
        {
            return;
        }

        bool glows = PaintColor.Unpack(value, out Color32 color32, out _);
        Color color = color32;
        Bounds? bounds = null;
        foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            Mesh mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
            Texture2D emission = mesh != null ? Emission(mesh.name) : null;
            if (emission == null)
            {
                continue;
            }

            GlowOriginal original = renderer.GetComponent<GlowOriginal>();
            if (!glows)
            {
                // Only a model this lit before is put back as it was, so a model's own glow (the Ice Sword's) stays.
                original?.Restore(renderer.material);
                continue;
            }

            Material material = renderer.material;
            if (!material.HasProperty("_EmissionMap"))
            {
                continue;
            }

            if (original == null)
            {
                renderer.gameObject.AddComponent<GlowOriginal>().Keep(material);
            }

            material.EnableKeyword("_EMISSION");
            material.SetTexture("_EmissionMap", emission);
            material.SetColor("_EmissionColor", color * Intensity);

            Bounds local = renderer.bounds;
            bounds = bounds.HasValue ? Encapsulate(bounds.Value, local) : local;
        }

        SetLight(root, glows, color, bounds);
    }

    // A soft light in the glow's colour at the glowing part, or at the model's middle.
    private static void SetLight(GameObject root, bool glows, Color color, Bounds? glowBounds)
    {
        Transform existing = root.transform.Find(LightName);
        if (!glows)
        {
            if (existing != null)
            {
                existing.gameObject.SetActive(false);
            }

            return;
        }

        Light light = existing != null ? existing.GetComponent<Light>() : null;
        if (light == null)
        {
            GameObject lightObject = new(LightName);
            lightObject.transform.SetParent(root.transform, false);
            light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.shadows = LightShadows.None;
            light.range = LightRange;
            light.intensity = LightIntensity;
        }

        Bounds bounds = glowBounds ?? ModelBounds(root);
        light.transform.position = bounds.center;
        light.color = color;
        light.gameObject.SetActive(true);
    }

    private static Bounds ModelBounds(GameObject root)
    {
        Bounds? bounds = null;
        foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>())
        {
            bounds = bounds.HasValue ? Encapsulate(bounds.Value, renderer.bounds) : renderer.bounds;
        }

        return bounds ?? new Bounds(root.transform.position, Vector3.zero);
    }

    private static Bounds Encapsulate(Bounds a, Bounds b)
    {
        a.Encapsulate(b);
        return a;
    }

    // The emission map of a White Hilt model (<mesh>_emission), or null for a model without one.
    private static Texture2D Emission(string meshName)
    {
        if (emissionByMesh.TryGetValue(meshName, out Texture2D texture))
        {
            return texture;
        }

        try
        {
            texture = ForagingAssets.LoadTexture($"{meshName}_emission");
        }
        catch (InvalidOperationException)
        {
            texture = null;
        }

        emissionByMesh[meshName] = texture;
        return texture;
    }
}

/// <summary>
/// What a character's hand models were last lit with, so they are only lit again when that changes.
/// </summary>
public class WeaponGlowState : MonoBehaviour
{
    /// <summary>The packed glow of the right hand.</summary>
    public int Right;

    /// <summary>The packed glow of the left hand.</summary>
    public int Left;

    /// <summary>The right-hand model that was lit.</summary>
    public GameObject RightInstance;

    /// <summary>The left-hand model that was lit.</summary>
    public GameObject LeftInstance;
}

/// <summary>
/// The emission a model had before a glow lit it, to put back when the glow goes out.
/// </summary>
public class GlowOriginal : MonoBehaviour
{
    private bool emissionOn;
    private Texture emissionMap;
    private Color emissionColor;

    /// <summary>
    /// Remembers a material's emission.
    /// </summary>
    /// <param name="material">The material before it is lit.</param>
    public void Keep(Material material)
    {
        emissionOn = material.IsKeywordEnabled("_EMISSION");
        emissionMap = material.GetTexture("_EmissionMap");
        emissionColor = material.HasProperty("_EmissionColor") ? material.GetColor("_EmissionColor") : Color.black;
    }

    /// <summary>
    /// Puts a material's emission back as it was.
    /// </summary>
    /// <param name="material">The material.</param>
    public void Restore(Material material)
    {
        if (!emissionOn)
        {
            material.DisableKeyword("_EMISSION");
        }

        material.SetTexture("_EmissionMap", emissionMap);
        material.SetColor("_EmissionColor", emissionColor);
    }
}
