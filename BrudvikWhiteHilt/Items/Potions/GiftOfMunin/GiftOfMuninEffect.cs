using BrudvikWhiteHilt.Extensions;
using BrudvikWhiteHilt.Helpers;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfMunin;

/// <summary>
/// The status effect of the Gift of Munin potion. It is a vanilla <c>SE_Stats</c>: its fields, set in <c>OnEnable</c>,
/// give the duration and the plain stat changes, and the game calls its overridden hooks (<c>Modify...</c>,
/// <c>OnDamaged</c>) while it is active, so it can change stamina use, damage and the like as they happen.
/// </summary>
public class GiftOfMuninEffect : SE_Stats
{
    /// <summary>
    /// The player character that the effect is applied to.
    /// </summary>
    private Player player;

    /// <summary>
    /// Initializes the effect with the given name.
    /// </summary>
    /// <param name="effectName">Name of the potion: the effect's name and the subject of its start and stop messages.</param>
    public void Initialize(string effectName)
    {
        base.name = effectName;
        m_name = effectName;
        m_startMessageType = MessageHud.MessageType.Center;
        m_startMessage = $"You have been bestowed the {effectName}!";
        m_tooltip = effectName;
    }

    /// <summary>
    /// Enables the effect - this is an instant effect with no duration.
    /// </summary>
    public void OnEnable()
    {
        m_activationAnimation = "emote_challenge";
        m_ttl = 1f; // Instant effect, just need a brief duration
    }

    /// <summary>
    /// Sets the icon for the effect.
    /// </summary>
    /// <param name="path">Embedded resource name of the icon image.</param>
    public void SetIcon(string path)
    {
        m_icon = AssetUtilsExtended.LoadTextureFromEmbeddedResource(path).ConvertToSprite();
    }

    /// <summary>
    /// Sets up the effect for the character. This is called when the effect is applied to a character.
    /// </summary>
    /// <param name="character">The character the effect is put on.</param>
    public override void Setup(Character character)
    {
        base.Setup(character);
        player = character as Player;
        if (player == null)
        {
            return;
        }

        // Adding to the known set directly avoids one unlock popup per material.
        bool learned = false;
        foreach (var item in ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Material, ""))
        {
            learned |= player.m_knownMaterial.Add(item.m_itemData.m_shared.m_name);
        }

        if (learned)
        {
            player.UpdateKnownRecipesList();
            MessageHud.instance?.m_unlockMsgQueue.Clear();
        }
    }

}
