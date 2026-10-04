using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;
using TMPro;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Stonework;

/// <summary>
/// A raised memorial stone, a bauta, with an inscription of your own on its face, like the runestones raised for the dead
/// and for great deeds. It works as the vanilla sign: use it to carve the text, and the hover text shows it.
/// </summary>
public class MemorialStone : StoneworkPieceBase
{
    // build_defenses.py MEMORIAL_TEXT: the middle of the stone's flatter face.
    private static readonly Vector3 TextPosition = new(0f, 1.45f, 0.5f);

    // Dark carved letters, a little larger than on a sign.
    private static readonly Color TextColor = new(0.12f, 0.1f, 0.09f);
    private const float TextScale = 1.3f;

    /// <summary>
    /// Constructor for the MemorialStone class. Registers the English text.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    public MemorialStone(PieceManager instance) : base(instance)
    {
    }

    /// <inheritdoc/>
    protected override string LayoutName => "bautastein";

    /// <inheritdoc/>
    protected override string FullName => "Memorial Stone";

    /// <inheritdoc/>
    protected override string Description => "A tall raised stone, a bauta, to remember the dead or a great deed. Use it to carve an inscription of your own on its face.";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = "Stone", Amount = 20, Recover = true }
    };

    /// <inheritdoc/>
    protected override float Health => 2000f;

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Start;

    /// <inheritdoc/>
    protected override string Category => PieceCategories.Misc;

    /// <summary>
    /// Moves the sign's text onto the stone's face and lets it hold a longer inscription.
    /// </summary>
    /// <param name="prefab">The piece prefab.</param>
    protected override void CustomizeStone(GameObject prefab)
    {
        Sign sign = prefab.GetComponent<Sign>();
        sign.m_name = Translations.Token(PrefabName);
        sign.m_defaultText = string.Empty;
        sign.m_characterLimit = 100;

        Transform canvas = prefab.transform.Find("Canvas");
        if (canvas == null)
        {
            return;
        }

        canvas.localPosition = TextPosition;
        canvas.localScale *= TextScale;
        TextMeshProUGUI text = canvas.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null)
        {
            text.color = TextColor;
        }
    }
}
