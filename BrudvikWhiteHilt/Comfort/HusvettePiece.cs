using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Defenses;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Comfort;

/// <summary>
/// The Husvette as a building piece: a stump from the layout (build_husvette.py) with the carved figure and the
/// porridge made in code (<see cref="HusvetteModel"/>), and the reach ring copied from the workbench's.
/// </summary>
public class HusvettePiece : DefensePieceBase
{
    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public HusvettePiece(PieceManager instance) : base(instance)
    {
        Translations.AddEnglish("whitehilt_husvette_hover", "Binds the room: comfort counts within $1 m, for everyone under its roof");
    }

    /// <inheritdoc/>
    protected override string LayoutName => "husvette";

    /// <inheritdoc/>
    protected override string FullName => "Husvette";

    /// <inheritdoc/>
    protected override string Description => "A carved house spirit with a red cap and his bowl of porridge. He binds the room: everything that gives comfort within his reach counts for everyone under its roof, and a fire burning anywhere in it keeps them warm.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "FineWood", Amount = 4, Recover = true },
        new() { Item = "Wood", Amount = 2, Recover = true },
        new() { Item = "TrollHide", Amount = 1, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => 400f;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.BlackForest;

    /// <inheritdoc/>
    protected override string Category => PieceCategories.Furniture;

    /// <inheritdoc/>
    protected override string BuildStation => CraftingStations.Workbench;

    /// <summary>
    /// Adds the figure, the reach ring and the Husvette's behaviour.
    /// </summary>
    /// <param name="prefab">The piece prefab.</param>
    /// <param name="data">The piece in the layout.</param>
    /// <param name="groups">Moving groups by name.</param>
    protected override void CustomizePrefab(GameObject prefab, DefensePieceData data, IDictionary<string, Transform> groups)
    {
        if (!VisualHelper.IsHeadless)
        {
            Transform visual = prefab.transform.Find("New");
            Renderer template = visual != null ? visual.GetComponentInChildren<MeshRenderer>(true) : null;
            if (template != null)
            {
                HusvetteModel.Attach(visual, template);
            }

            CraftingStation workbench = PrefabManager.Instance.GetPrefab("piece_workbench").GetComponent<CraftingStation>();
            GameObject marker = Object.Instantiate(workbench.m_areaMarker, prefab.transform);
            marker.name = Husvette.AreaMarkerName;
            marker.transform.localPosition = Vector3.zero;
            marker.SetActive(false);
        }

        prefab.AddComponent<Husvette>();
        prefab.AddComponent<HusvetteVoice>();
    }
}
