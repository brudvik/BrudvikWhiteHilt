using BrudvikWhiteHilt.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles.Damage;

/// <summary>
/// What the fires on one ship look like on one client: the flames, sparks and light of a vanilla campfire at each fire,
/// as plain child objects of the ship without a network view. Only the picture is copied from the campfire; its warmth
/// and burn triggers, smoke and ash are left behind, since the damage of a deck fire is done by <see cref="DeckFires"/>.
/// A dedicated server never makes one.
/// </summary>
internal sealed class DeckFireLook
{
    // The vanilla campfire and the child that holds its strong fire: flames, sparks, light and the crackle.
    private const string TemplatePrefab = "fire_pit";
    private const string TemplatePath = "_enabled_high";

    // A fire sits a little above the planks so the flames do not sink into them.
    private const float Lift = 0.05f;

    private static readonly HashSet<string> keptChildren = new() { "sfx_fire_loop", "Point light", "Particles" };
    private static readonly HashSet<string> keptParticles = new() { "flare", "flames (1)", "sparcs (1)" };

    private static GameObject template;
    private static bool templateLooked;

    private readonly Transform ship;
    private readonly Dictionary<Vector3, GameObject> shown = new();
    private GameObject root;

    /// <summary>
    /// Creates the look of one ship's fires.
    /// </summary>
    /// <param name="ship">The ship.</param>
    public DeckFireLook(Transform ship)
    {
        this.ship = ship;
    }

    /// <summary>
    /// Puts out the flames that have no fire any more and lights one at each new fire. A fire that burns on keeps its flames.
    /// </summary>
    /// <param name="points">The fires, in the ship's local space.</param>
    public void Sync(IReadOnlyList<Vector3> points)
    {
        HashSet<Vector3> wanted = new(points);
        List<Vector3> gone = new();
        foreach (KeyValuePair<Vector3, GameObject> flame in shown)
        {
            if (!wanted.Contains(flame.Key))
            {
                gone.Add(flame.Key);
            }
        }

        foreach (Vector3 point in gone)
        {
            Object.Destroy(shown[point]);
            shown.Remove(point);
        }

        foreach (Vector3 point in wanted)
        {
            if (!shown.ContainsKey(point))
            {
                GameObject flame = Build(point);
                if (flame != null)
                {
                    shown[point] = flame;
                }
            }
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

        shown.Clear();
    }

    // One fire: a copy of the campfire's strong fire with only the parts that make a picture.
    private GameObject Build(Vector3 point)
    {
        GameObject source = Template();
        if (source == null)
        {
            return null;
        }

        if (root == null)
        {
            root = new GameObject("WhiteHiltDeckFires");
            root.transform.SetParent(ship, false);
        }

        // The campfire's child is switched off in its prefab, so the copy is too until it has been trimmed.
        GameObject fire = Object.Instantiate(source, root.transform);
        fire.name = "WhiteHiltDeckFire";
        fire.transform.localPosition = point + Vector3.up * Lift;
        fire.transform.localRotation = Quaternion.identity;
        Trim(fire.transform);

        foreach (Light light in fire.GetComponentsInChildren<Light>(true))
        {
            light.shadows = LightShadows.None;
        }

        foreach (TimedDestruction timer in fire.GetComponentsInChildren<TimedDestruction>(true))
        {
            Object.DestroyImmediate(timer);
        }

        fire.SetActive(true);
        return fire;
    }

    // Keeps the crackle, the light and the particles, and of the particles only the flames, flare and sparks. The rest is
    // destroyed at once, while the copy is still switched off, so the campfire's warmth and burn areas never wake.
    private static void Trim(Transform fire)
    {
        foreach (Transform child in Children(fire))
        {
            if (!keptChildren.Contains(child.name))
            {
                Object.DestroyImmediate(child.gameObject);
            }
            else if (child.name == "Particles")
            {
                foreach (Transform particle in Children(child))
                {
                    if (!keptParticles.Contains(particle.name))
                    {
                        Object.DestroyImmediate(particle.gameObject);
                    }
                }
            }
        }
    }

    private static List<Transform> Children(Transform parent)
    {
        List<Transform> children = new(parent.childCount);
        foreach (Transform child in parent)
        {
            children.Add(child);
        }

        return children;
    }

    private static GameObject Template()
    {
        if (!templateLooked && ZNetScene.instance != null)
        {
            templateLooked = true;
            GameObject prefab = ZNetScene.instance.GetPrefab(TemplatePrefab);
            template = prefab != null ? prefab.transform.Find(TemplatePath)?.gameObject : null;
            if (template == null)
            {
                Jotunn.Logger.LogWarning($"Deck fires: no {TemplatePrefab}/{TemplatePath} to copy the flames from; fires burn without a picture.");
            }
        }

        return template;
    }
}
