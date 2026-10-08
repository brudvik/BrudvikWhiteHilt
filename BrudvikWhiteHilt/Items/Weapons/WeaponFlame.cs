using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Painting;
using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Weapons;

/// <summary>
/// The flame a Flame Rune etches into a White Hilt weapon or shield, in a colour of the player's own. Gear that burns
/// already (the White Hilt Sword's Dyrnwyn fire, the staffs' flames) has its own flames recoloured; other gear gets
/// Dyrnwyn's fire along its head or the top of its blade. The flame can also be put out, own flames included, and put
/// back as it was. The colour is kept on the item, the holder writes what is in their hands to their ZDO, and every
/// machine shows it, as with <see cref="WeaponGlow"/>.
/// </summary>
public static class WeaponFlame
{
    private const string ColorKey = "whitehilt_flame";
    private const string OffValue = "off";
    private const string AddedName = "whitehilt_flame";
    private const string FlameSource = "SwordDyrnwyn";
    private const int OffPacked = 1;
    private const float HeadShare = 0.4f;

    private static readonly string[] ownFlameRoots = { "Burny vfx", "equiped" };
    private static readonly int[] slotKeys = GearSlots.Keys("whitehilt_flame");

    /// <summary>
    /// Registers the English texts.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglish("whitehilt_flame_line", "Flame: {0}");
        Translations.AddEnglish("whitehilt_flame_off", "Flame: put out");
    }

    /// <summary>
    /// The flame colour etched into an item.
    /// </summary>
    /// <param name="item">The item, or null.</param>
    /// <param name="color">The colour.</param>
    /// <returns>False if the item has no coloured flame.</returns>
    public static bool TryGetColor(ItemDrop.ItemData item, out Color32 color)
    {
        color = default;
        return item?.m_customData != null && item.m_customData.TryGetValue(ColorKey, out string hex) && PaintColor.TryParseHex(hex, out color);
    }

    /// <summary>
    /// Whether an item's flames, its own ones included, are put out.
    /// </summary>
    /// <param name="item">The item, or null.</param>
    /// <returns>True if put out.</returns>
    public static bool IsOff(ItemDrop.ItemData item)
    {
        return item?.m_customData != null && item.m_customData.TryGetValue(ColorKey, out string value) && value == OffValue;
    }

    /// <summary>
    /// Etches a flame colour into an item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="color">The colour.</param>
    public static void SetColor(ItemDrop.ItemData item, Color32 color)
    {
        item.m_customData[ColorKey] = PaintColor.ToHex(color);
    }

    /// <summary>
    /// Puts an item's flames out, its own ones included.
    /// </summary>
    /// <param name="item">The item.</param>
    public static void PutOut(ItemDrop.ItemData item)
    {
        item.m_customData[ColorKey] = OffValue;
    }

    /// <summary>
    /// Gives an item back the flames it was made with: none for most gear, its own for the sword and the staffs.
    /// </summary>
    /// <param name="item">The item.</param>
    public static void Restore(ItemDrop.ItemData item)
    {
        item.m_customData.Remove(ColorKey);
    }

    /// <summary>
    /// The tooltip line of an item with an etched flame.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The line, or nothing.</returns>
    public static string TooltipText(ItemDrop.ItemData item)
    {
        Localization localization = Localization.instance;
        if (TryGetColor(item, out Color32 color))
        {
            return "\n" + string.Format(localization.Localize("$whitehilt_flame_line"), PaintColor.Swatch(color));
        }

        return IsOff(item) ? "\n" + localization.Localize("$whitehilt_flame_off") : string.Empty;
    }

    /// <summary>
    /// Keeps the flames of the gear a character carries, in the hands and on the back, as etched: the owner writes them
    /// to their ZDO, and every machine shows them. Called every frame for every character's equipment.
    /// </summary>
    /// <param name="equipment">The character's equipment visuals.</param>
    public static void Refresh(VisEquipment equipment)
    {
        GearSlots.Refresh<WeaponFlameState>(equipment, slotKeys, Value, Apply);
    }

    /// <summary>
    /// Shows the etched flame of an item lying on the ground.
    /// </summary>
    /// <param name="item">The dropped item.</param>
    public static void ApplyDropped(ItemDrop item)
    {
        int value = Value(item.m_itemData);
        if (value != 0)
        {
            Apply(item.gameObject, value);
        }
    }

    // 0 for the flames the item was made with, 1 for put out, otherwise the packed colour.
    private static int Value(ItemDrop.ItemData item)
    {
        if (TryGetColor(item, out Color32 color))
        {
            return PaintColor.Pack(color, PaintMode.Paint);
        }

        return IsOff(item) ? OffPacked : 0;
    }

    // Shows a model's flames as a packed value says: as made, put out, or in a colour.
    private static void Apply(GameObject root, int value)
    {
        if (root == null)
        {
            return;
        }

        Transform added = root.transform.Find(AddedName);
        FlameOriginals originals = root.GetComponent<FlameOriginals>();
        if (originals == null)
        {
            if (value == 0)
            {
                return;
            }

            originals = root.AddComponent<FlameOriginals>();
            originals.Keep(OwnFlames(root, out List<Light> lights), lights);
        }

        originals.Restore();
        if (value == 0 || value == OffPacked)
        {
            if (added != null)
            {
                Object.Destroy(added.gameObject);
            }

            if (value == OffPacked)
            {
                originals.Hide();
            }

            return;
        }

        PaintColor.Unpack(value, out Color32 color32, out _);
        Color color = color32;
        if (originals.HasOwnFlames)
        {
            originals.Recolor(color);
            return;
        }

        if (added == null)
        {
            added = CreateFlame(root);
        }

        if (added == null)
        {
            return;
        }

        foreach (ParticleSystem particles in added.GetComponentsInChildren<ParticleSystem>(true))
        {
            StaffFlame.Recolor(particles, color);
        }

        foreach (Light light in added.GetComponentsInChildren<Light>(true))
        {
            light.color = color;
        }
    }

    // The flames a model was made with: the particles under the Dyrnwyn fire or a staff's flame, and their lights.
    private static List<ParticleSystem> OwnFlames(GameObject root, out List<Light> lights)
    {
        List<Transform> flameRoots = root.GetComponentsInChildren<Transform>(true)
            .Where(child => ownFlameRoots.Contains(child.name) && child.name != AddedName).ToList();
        List<ParticleSystem> particles = flameRoots.SelectMany(flame => flame.GetComponentsInChildren<ParticleSystem>(true)).Distinct().ToList();
        lights = flameRoots.SelectMany(flame => flame.GetComponentsInChildren<Light>(true))
            .Concat(root.GetComponentsInChildren<LightFlicker>(true).Select(flicker => flicker.GetComponent<Light>()))
            .Where(light => light != null).Distinct().ToList();
        return particles;
    }

    // Dyrnwyn's fire spread over the head of the model, the top part of its length.
    private static Transform CreateFlame(GameObject root)
    {
        Transform source = PrefabManager.Instance.GetPrefab(FlameSource)?.transform.Find("attach/Burny vfx");
        if (source == null)
        {
            Jotunn.Logger.LogWarning($"{FlameSource} has no attach/Burny vfx; no flame can be added.");
            return null;
        }

        Bounds head = HeadBounds(root);
        GameObject flame = new(AddedName) { layer = source.gameObject.layer };
        flame.transform.SetParent(root.transform, false);
        flame.transform.localPosition = head.center;

        GameObject fire = Object.Instantiate(source.gameObject, flame.transform, false);
        fire.transform.localPosition = Vector3.zero;
        fire.transform.localRotation = Quaternion.identity;
        foreach (ParticleSystem particles in fire.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.position = Vector3.zero;
            shape.rotation = Vector3.zero;
            shape.scale = head.size;
        }

        Transform sourceLight = source.parent.Find("Point Light");
        if (sourceLight != null)
        {
            GameObject light = Object.Instantiate(sourceLight.gameObject, flame.transform, false);
            light.transform.localPosition = Vector3.zero;
        }

        return flame.transform;
    }

    // The top part of the model along its length (z), in the root's space.
    private static Bounds HeadBounds(GameObject root)
    {
        Bounds? bounds = null;
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            Mesh mesh = renderer is SkinnedMeshRenderer skinned ? skinned.sharedMesh : renderer.GetComponent<MeshFilter>()?.sharedMesh;
            if (mesh == null || renderer is ParticleSystemRenderer)
            {
                continue;
            }

            Matrix4x4 toRoot = root.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = toRoot.MultiplyPoint3x4(mesh.bounds.center + Vector3.Scale(mesh.bounds.extents, new Vector3(
                    (corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f)));
                Bounds next = bounds ?? new Bounds(point, Vector3.zero);
                next.Encapsulate(point);
                bounds = next;
            }
        }

        Bounds whole = bounds ?? new Bounds(Vector3.zero, Vector3.one * 0.2f);
        float length = whole.size.z * HeadShare;
        Vector3 centre = new(whole.center.x, whole.center.y, whole.max.z - length / 2f);
        return new Bounds(centre, new Vector3(whole.size.x, whole.size.y, length));
    }
}

/// <summary>
/// What a character's gear models' flames were last shown as.
/// </summary>
public class WeaponFlameState : GearSlotState
{
}

/// <summary>
/// A model's own flames as it was made, so a colour or a put-out flame can always be undone.
/// </summary>
public class FlameOriginals : MonoBehaviour
{
    private readonly List<(ParticleSystem System, ParticleSystem.MinMaxGradient Start, bool OverLifetime, ParticleSystem.MinMaxGradient Lifetime, Material Material, bool Shown)> particles = new();
    private readonly List<(Light Light, Color Color, bool Enabled)> lights = new();

    /// <summary>
    /// Whether the model has flames of its own (the sword, the staffs).
    /// </summary>
    public bool HasOwnFlames => particles.Count > 0;

    /// <summary>
    /// Remembers the flames as they are.
    /// </summary>
    /// <param name="systems">The model's own flame particles.</param>
    /// <param name="flameLights">Their lights.</param>
    public void Keep(IEnumerable<ParticleSystem> systems, IEnumerable<Light> flameLights)
    {
        foreach (ParticleSystem system in systems)
        {
            ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
            particles.Add((system, system.main.startColor, system.colorOverLifetime.enabled, system.colorOverLifetime.color,
                renderer != null ? renderer.sharedMaterial : null, renderer == null || renderer.enabled));
        }

        foreach (Light light in flameLights)
        {
            lights.Add((light, light.color, light.enabled));
        }
    }

    /// <summary>
    /// Puts the flames back as they were.
    /// </summary>
    public void Restore()
    {
        foreach (var (system, start, overLifetime, lifetime, material, shown) in particles.Where(entry => entry.System != null))
        {
            ParticleSystem.MainModule main = system.main;
            main.startColor = start;
            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = overLifetime;
            colorOverLifetime.color = lifetime;
            ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.enabled = shown;
            }
        }

        foreach (var (light, color, enabled) in lights.Where(entry => entry.Light != null))
        {
            light.color = color;
            light.enabled = enabled;
        }
    }

    /// <summary>
    /// Hides the flames and their lights.
    /// </summary>
    public void Hide()
    {
        foreach (var entry in particles.Where(entry => entry.System != null))
        {
            ParticleSystemRenderer renderer = entry.System.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }

        foreach (var entry in lights.Where(entry => entry.Light != null))
        {
            entry.Light.enabled = false;
        }
    }

    /// <summary>
    /// Gives the flames and their lights a colour.
    /// </summary>
    /// <param name="color">The colour.</param>
    public void Recolor(Color color)
    {
        foreach (var entry in particles.Where(entry => entry.System != null))
        {
            StaffFlame.Recolor(entry.System, color);
        }

        foreach (var entry in lights.Where(entry => entry.Light != null))
        {
            entry.Light.color = color;
        }
    }
}
