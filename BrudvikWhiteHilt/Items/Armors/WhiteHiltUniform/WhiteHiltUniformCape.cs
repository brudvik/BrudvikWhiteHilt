using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Items.Indestructible;
using Jotunn.Configs;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Armors.WhiteHiltUniform;

/// <summary>
/// The black White Hilt uniform cape with a gold edge and the logo on the back, on the troll hide cape like the
/// White Hilt Banner Cape.
/// </summary>
public class WhiteHiltUniformCape : WhiteHiltArmorBase
{
    /// <summary>
    /// Creates the uniform cape.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public WhiteHiltUniformCape(ItemManager instance) : base(instance) { }

    /// <inheritdoc/>
    protected override string BaseName => "WhiteHiltUniformCape";

    /// <inheritdoc/>
    protected override string FullName => "White Hilt Uniform Cape";

    /// <inheritdoc/>
    protected override string Description => "The indestructible black cape of Dyrnwyn, edged with gold, with the White Hilt on the back.";

    /// <inheritdoc/>
    protected override string CopyFrom => "CapeTrollHide";

    // Same stats as the White Hilt Cape.
    /// <inheritdoc/>
    protected override string StatsFrom => "CapeFeather";

    /// <inheritdoc/>
    protected override bool CopyEquipEffect => false;

    /// <inheritdoc/>
    protected override string CraftingStation => CraftingStations.Workbench;

    /// <inheritdoc/>
    protected override int MinStationLevel => 2;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "TrollHide", Amount = 4, Recover = false },
        new() { Item = "Coal", Amount = 4, Recover = false },
        new() { Item = "Coins", Amount = 30, Recover = false }
    };

    /// <inheritdoc/>
    public override bool Enabled => true;

    /// <inheritdoc/>
    protected override void ApplyVisual(IndestructibleItem item)
    {
        try
        {
            GameObject prefab = item.ItemPrefab;
            Transform worn = prefab.transform.Find("attach_skin") ?? throw new InvalidOperationException("attach_skin not found");
            Func<Color32, Color32> recolor = UniformLook.BlackGold(lightToGold: false);
            foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true).Where(renderer => renderer is MeshRenderer || renderer is SkinnedMeshRenderer))
            {
                Material source = renderer.sharedMaterial;
                if (source == null || source.mainTexture == null)
                {
                    continue;
                }

                renderer.sharedMaterial = renderer.transform.IsChildOf(worn)
                    ? UniformLook.CreateCapeMaterial(source)
                    : new Material(source) { mainTexture = VisualHelper.RecolorTexture(source.mainTexture, recolor) };
            }

            UniformLook.RenderIcon(prefab, item.ItemData);
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogWarning($"{FullName}: keeping the troll hide look: {ex.Message}");
        }
    }
}
