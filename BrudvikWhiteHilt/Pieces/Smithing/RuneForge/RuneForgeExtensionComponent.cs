using BrudvikWhiteHilt.Items.Binding;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Smithing.RuneForge;

/// <summary>
/// A Rune Forge extension the player uses an item on, with the White Hilt weapon or shield to work on in hand.
/// Only works while a Rune Forge stands within the extension's reach. The vanilla <see cref="StationExtension"/> on the
/// same object is the first <see cref="Hoverable"/>, so its hover text is replaced with this one by a patch.
/// </summary>
public abstract class RuneForgeExtensionComponent : MonoBehaviour, Hoverable, Interactable
{
    private Piece piece;
    private StationExtension extension;

    /// <inheritdoc/>
    public abstract string GetHoverText();

    /// <inheritdoc/>
    public string GetHoverName()
    {
        return piece != null ? piece.m_name : string.Empty;
    }

    /// <inheritdoc/>
    public float GetHoverOffset()
    {
        return 0f;
    }

    /// <inheritdoc/>
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold || user is not Player player)
        {
            return false;
        }

        player.Message(MessageHud.MessageType.Center, UsageMessage);
        return true;
    }

    /// <inheritdoc/>
    public abstract bool UseItem(Humanoid user, ItemDrop.ItemData item);

    /// <summary>
    /// Message shown when the extension is used without an item.
    /// </summary>
    protected abstract string UsageMessage { get; }

    /// <summary>
    /// The Rune Forge this extension works with, or null if none is in reach.
    /// </summary>
    protected CraftingStation Forge => extension != null ? extension.FindClosestStationInRange(transform.position) : null;

    /// <summary>
    /// Start of the hover text: the name, and a warning when no Rune Forge is in reach.
    /// </summary>
    /// <returns>Unlocalized text.</returns>
    protected string HoverHeader()
    {
        string text = GetHoverName();
        if (Forge == null)
        {
            text += "\n<color=orange>$whitehilt_runeforge_ext_noforge</color>";
        }

        return text;
    }

    /// <summary>
    /// Checks that the player may use the extension here, and tells them if not.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="forge">The Rune Forge in reach.</param>
    /// <returns>True if it can be used.</returns>
    protected bool CanUse(Player player, out CraftingStation forge)
    {
        forge = Forge;
        if (!PrivateArea.CheckAccess(transform.position))
        {
            return false;
        }

        if (forge == null)
        {
            player.Message(MessageHud.MessageType.Center, "$whitehilt_runeforge_ext_noforge");
            return false;
        }

        return true;
    }

    /// <summary>
    /// The White Hilt weapon in the player's hands, or else the White Hilt shield.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="shields">Whether a shield counts.</param>
    /// <returns>The item, or null.</returns>
    protected static ItemDrop.ItemData HeldGear(Player player, bool shields)
    {
        ItemDrop.ItemData weapon = player.GetCurrentWeapon();
        if (GearBinding.CanInfuse(weapon))
        {
            return weapon;
        }

        ItemDrop.ItemData left = player.m_leftItem;
        return shields && GearBinding.CanBind(left) ? left : null;
    }

    /// <summary>
    /// Plays the Rune Forge's crafting effect at the extension.
    /// </summary>
    /// <param name="forge">The Rune Forge.</param>
    protected void PlayEffect(CraftingStation forge)
    {
        forge.m_craftItemEffects?.Create(transform.position, Quaternion.identity);
    }

    private void Awake()
    {
        piece = GetComponent<Piece>();
        extension = GetComponent<StationExtension>();
    }
}
