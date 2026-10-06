using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Armors.WhiteHiltUniform;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Armors.WhiteHiltBannerCape;

/// <summary>
/// A white cape with the White Hilt logo on the back, made like the banners from the troll hide cape.
/// </summary>
public class WhiteHiltBannerCape : IWhiteHiltCustomItem
{
    private const string BaseName = "WhiteHiltBannerCape";
    private const string FullName = "White Hilt Banner Cape";
    private const string Description = "A white cape with the White Hilt on the back.";
    private const string CopyFrom = "CapeTrollHide";
    private const int TextureSize = 512;

    private static readonly Color cream = new(0.94f, 0.91f, 0.84f);

    private readonly ItemManager instance;

    /// <inheritdoc/>
    public bool Enabled => true;

    /// <inheritdoc/>
    public ProgressionTier DefaultTier => ProgressionTier.BlackForest;

    /// <inheritdoc/>
    public string Id => BaseName;

    /// <inheritdoc/>
    public string DisplayName => FullName;

    /// <inheritdoc/>
    public string NameToken => Translations.Token(Translations.ItemKey(BaseName));

    /// <inheritdoc/>
    public string GatedPrefabName => BaseName;

    /// <summary>
    /// Registers the English text.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public WhiteHiltBannerCape(ItemManager instance)
    {
        this.instance = instance;
        Translations.AddEnglishNameAndDescription(Translations.ItemKey(BaseName), FullName, Description);
    }

    /// <summary>
    /// Adds the cape to the game.
    /// </summary>
    public void Add()
    {
        try
        {
            ItemConfig itemConfig = new()
            {
                Name = Translations.Token(Translations.ItemKey(BaseName)),
                Description = Translations.Token($"{Translations.ItemKey(BaseName)}_description"),
                CraftingStation = CraftingStations.Workbench,
                MinStationLevel = 2,
                Requirements = new RequirementConfig[]
                {
                    new() { Item = "TrollHide", Amount = 4, Recover = true },
                    new() { Item = "LeatherScraps", Amount = 4, Recover = true },
                    new() { Item = "BoneFragments", Amount = 2, Recover = true }
                }
            };

            CustomItem item = new(BaseName, CopyFrom, itemConfig);
            ItemDrop.ItemData.SharedData shared = item.ItemDrop.m_itemData.m_shared;

            // Not part of the troll set, so it gives no sneak bonus with the troll armour.
            shared.m_setName = "";
            shared.m_setSize = 0;
            shared.m_setStatusEffect = null;

            TryApplyVisual(item);
            instance.AddItem(item);

            Jotunn.Logger.LogInfo($"{FullName} added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{FullName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    // Gives the cloned troll hide cape the banner's look: the cloth worn on the back gets its own material, the dropped
    // item is whitened. Should anything be missing, the troll hide look stays.
    private static void TryApplyVisual(CustomItem item)
    {
        if (VisualHelper.IsHeadless)
        {
            return;
        }

        try
        {
            GameObject prefab = item.ItemPrefab;
            Transform worn = prefab.transform.Find("attach_skin") ?? throw new InvalidOperationException("attach_skin not found");
            foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true).Where(renderer => renderer is MeshRenderer || renderer is SkinnedMeshRenderer))
            {
                Material source = renderer.sharedMaterial;
                if (source == null || source.mainTexture == null)
                {
                    continue;
                }

                renderer.sharedMaterial = renderer.transform.IsChildOf(worn)
                    ? CreateWornMaterial(source)
                    : new Material(source) { mainTexture = VisualHelper.RecolorTexture(source.mainTexture, Whiten) };
            }

            Sprite icon = VisualHelper.RenderIcon(prefab);
            if (icon != null)
            {
                // Keep one entry per vanilla variant: GetIcon indexes m_icons by the saved m_variant.
                ItemDrop.ItemData.SharedData shared = item.ItemDrop.m_itemData.m_shared;
                shared.m_icons = Enumerable.Repeat(icon, Math.Max(1, shared.m_icons?.Length ?? 0)).ToArray();
            }
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: keeping the troll hide look: {ex.Message}");
        }
    }

    // The troll texture is tiled onto the cape and shared with the Troll, so the cape gets its own texture in its mesh UVs,
    // with the grain of the hide it had.
    private static Material CreateWornMaterial(Material source)
    {
        Texture hide = source.mainTexture;
        Vector2 scale = source.mainTextureScale;
        Vector2 offset = source.mainTextureOffset;
        Color32[] hidePixels = VisualHelper.ReadPixels(hide, hide.width, hide.height);

        Color32[] pixels = new Color32[TextureSize * TextureSize];
        for (int y = 0; y < TextureSize; y++)
        {
            float v = (y + 0.5f) / TextureSize;
            int hideY = Mathf.Clamp((int)(Mathf.Repeat(v * scale.y + offset.y, 1f) * hide.height), 0, hide.height - 1);
            for (int x = 0; x < TextureSize; x++)
            {
                float u = (x + 0.5f) / TextureSize;
                int hideX = Mathf.Clamp((int)(Mathf.Repeat(u * scale.x + offset.x, 1f) * hide.width), 0, hide.width - 1);
                pixels[y * TextureSize + x] = Whiten(hidePixels[hideY * hide.width + hideX]);
            }
        }

        WhiteHiltLogo.Paint(pixels, TextureSize, TextureSize, UniformLook.CapeLogoCentre, UniformLook.CapeLogoRadius);
        Texture2D texture = VisualHelper.CreateTexture("whitehilt_bannercape", TextureSize, TextureSize, pixels, hide);
        Material material = VisualHelper.CreateTexturedMaterial(source, texture, "whitehilt_bannercape");
        material.mainTextureScale = Vector2.one;
        material.mainTextureOffset = Vector2.zero;
        return material;
    }

    private static Color32 Whiten(Color32 pixel)
    {
        Color.RGBToHSV(pixel, out _, out _, out float value);
        Color white = cream * Mathf.Clamp01(value * 0.6f + 0.55f);
        white.a = pixel.a / 255f;
        return white;
    }
}
