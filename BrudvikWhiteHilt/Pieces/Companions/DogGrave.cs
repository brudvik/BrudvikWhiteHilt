using BrudvikWhiteHilt.Companions;
using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Companions;

/// <summary>
/// A rune stone over a dog's grave, raised from a gravestone cut at the stonecutter. The dog's name and age are
/// chiselled into its plain face; the carved face looks the other way. A memorial: the dog does not come back.
/// </summary>
public class DogGrave : DogPieceBase
{
    /// <summary>
    /// Prefab name of the grave.
    /// </summary>
    public const string Name = "piece_whitehilt_doggrave";

    // The model is 1 high, 0.75 wide and 0.44 deep; its carved face is +z, the plain face lies at z -0.21.
    private const float Size = 0.9f;

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public DogGrave(PieceManager instance) : base(instance)
    {
    }

    /// <inheritdoc/>
    protected override string PrefabName => Name;

    /// <inheritdoc/>
    protected override string FullName => "Dog's Grave";

    /// <inheritdoc/>
    protected override string Description => "Raise the gravestone with the dog's name and age over its grave.";

    /// <inheritdoc/>
    protected override string BasePrefab => "sign";

    /// <inheritdoc/>
    protected override string CraftingStation => null;

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = DogRegistry.GravestonePrefabName, Amount = 1, Recover = true }
    };

    /// <inheritdoc/>
    protected override void KeepBeforeStrip(GameObject prefab, Transform visual)
    {
        // The sign's text canvas carries the inscription; it is put on the stone's plain face, which is turned to +z.
        Transform canvas = prefab.transform.Find("Canvas");
        if (canvas == null)
        {
            Jotunn.Logger.LogWarning($"{FullName}: the sign has no Canvas, the inscription will not show on the stone");
            return;
        }

        canvas.SetParent(visual, false);
        canvas.localPosition = new Vector3(0f, 0.42f, 0.2f);
        canvas.localScale = Vector3.one * 0.55f;
    }

    /// <inheritdoc/>
    protected override void Configure(GameObject prefab)
    {
        Sign sign = prefab.GetComponent<Sign>();
        if (sign != null)
        {
            sign.m_name = Translations.Token(Name);
            sign.m_defaultText = string.Empty;
            sign.m_characterLimit = 60;
        }

        AddBox(prefab, "piece", new Vector3(0f, 0.45f, 0f), new Vector3(0.62f, 0.9f, 0.36f));
        prefab.AddComponent<DogMemorial>();
    }

    /// <inheritdoc/>
    protected override void ApplyVisual(Transform visual)
    {
        VisualHelper.CreateModel(visual, ForagingAssets.LoadMesh("valkyriestone"), ForagingAssets.LoadTexture("valkyriestone_albedo"),
            Template("stone_wall_1x1", "new"), Vector3.zero, Quaternion.Euler(0f, 180f, 0f), Size);
    }
}
