using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Weapons;

/// <summary>
/// Puts a coloured flame on the head of a White Hilt staff, lit while the staff is held.
/// The flame is the Staff of Embers' own, recoloured, so the three staffs share one look.
/// </summary>
public static class StaffFlame
{
    // Just above the scepter head in attach space, from AssetSource/Models/whstaff.weapon.json.
    private static readonly Vector3 headTip = new(0f, 0f, 1.16f);
    private static readonly Dictionary<Texture, Material> greyMaterials = new();

    /// <summary>
    /// Replaces the vanilla staff effects under <paramref name="model"/>'s attach child with a flame in <paramref name="colour"/>.
    /// </summary>
    /// <param name="model">The staff model under the attach child.</param>
    /// <param name="colour">Flame and light colour.</param>
    /// <param name="tip">Where the flame burns in attach space; by default just above the scepter head.</param>
    /// <returns>The object shown only while the staff is held, or null if there is no flame.</returns>
    public static GameObject Apply(GameObject model, Color colour, Vector3? tip = null)
    {
        Transform attach = model.transform.parent;
        DestroyChild(model.transform, "effects");
        DestroyChild(attach, "equiped");

        Transform source = Source(out Transform flames);
        if (flames == null)
        {
            Jotunn.Logger.LogWarning("StaffFireball has no attach/equiped/flames; the staff gets no flame.");
            return null;
        }

        // VisEquipment switches on a child named "equiped" only in the hand, like the vanilla staff flames.
        GameObject equipped = Object.Instantiate(source.gameObject, attach, false);
        equipped.name = "equiped";
        equipped.SetActive(false);
        equipped.transform.localRotation = Quaternion.identity;
        equipped.transform.localPosition = (tip ?? headTip) - flames.localPosition;

        foreach (ParticleSystem particles in equipped.GetComponentsInChildren<ParticleSystem>(true))
        {
            Recolor(particles, colour);
        }

        GameObject lightObject = new("Point light");
        lightObject.transform.SetParent(equipped.transform, false);
        lightObject.transform.localPosition = flames.localPosition;
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = colour;
        light.range = 3f;
        light.intensity = 1.5f;
        light.shadows = LightShadows.None;
        return equipped;
    }

    /// <summary>
    /// A loose flame of the Staff of Embers in a colour, centred on its object, e.g. for a flame in the world.
    /// </summary>
    /// <param name="colour">Flame colour.</param>
    /// <param name="parent">The object to burn on.</param>
    /// <returns>The flame, or null if the vanilla staff has none.</returns>
    public static GameObject CreateFlame(Color colour, Transform parent)
    {
        Source(out Transform flames);
        if (flames == null)
        {
            return null;
        }

        GameObject flame = Object.Instantiate(flames.gameObject, parent, false);
        flame.name = "flame";
        flame.transform.localPosition = Vector3.zero;
        flame.transform.localRotation = Quaternion.identity;
        foreach (ParticleSystem particles in flame.GetComponentsInChildren<ParticleSystem>(true))
        {
            Recolor(particles, colour);
        }

        return flame;
    }

    private static Transform Source(out Transform flames)
    {
        Transform source = PrefabManager.Instance.GetPrefab("StaffFireball")?.transform.Find("attach/equiped");
        flames = source?.Find("flames");
        return source;
    }

    private static void DestroyChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
        {
            Object.DestroyImmediate(child.gameObject);
        }
    }

    /// <summary>
    /// Recolours a flame's particles. The fire textures carry their own orange, so a grey copy of the material is made
    /// (once per texture) and the colour comes from the particles instead.
    /// </summary>
    /// <param name="particles">The flame's particle system.</param>
    /// <param name="colour">The new colour.</param>
    public static void Recolor(ParticleSystem particles, Color colour)
    {
        ParticleSystem.MainModule main = particles.main;
        main.startColor = new ParticleSystem.MinMaxGradient(colour);

        ParticleSystem.ColorOverLifetimeModule overLifetime = particles.colorOverLifetime;
        if (overLifetime.enabled)
        {
            Gradient original = overLifetime.color.gradient ?? overLifetime.color.gradientMax;
            Gradient gradient = new();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                original?.alphaKeys ?? new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            overLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        // The fire textures carry their own orange, so they are made grey and take the colour from the particles.
        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        Material source = renderer != null ? renderer.sharedMaterial : null;
        if (source?.mainTexture == null)
        {
            return;
        }

        if (!greyMaterials.TryGetValue(source.mainTexture, out Material grey))
        {
            Texture2D texture = Helpers.VisualHelper.RecolorTexture(source.mainTexture, pixel =>
            {
                byte level = (byte)Mathf.Max(pixel.r, pixel.g, pixel.b);
                return new Color32(level, level, level, pixel.a);
            });
            grey = new Material(source) { name = $"{source.name}_grey", mainTexture = texture };
            foreach (string property in new[] { "_TintColor", "_Color", "_EmissionColor" })
            {
                if (grey.HasProperty(property))
                {
                    Color tint = grey.GetColor(property);
                    float level = Mathf.Max(tint.r, tint.g, tint.b);
                    grey.SetColor(property, new Color(level, level, level, tint.a));
                }
            }

            greyMaterials[source.mainTexture] = grey;
        }

        renderer.sharedMaterial = grey;
    }
}
