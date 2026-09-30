using BrudvikWhiteHilt.Extensions;
using BrudvikWhiteHilt.Helpers;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfMunin;

/// <summary>
/// This class defines the effect of the Gift of Munin potion.
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
    /// <param name="effectName"></param>
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
    /// <param name="path"></param>
    public void SetIcon(string path)
    {
        m_icon = AssetUtilsExtended.LoadTextureFromEmbeddedResource(path).ConvertToSprite();
    }

    /// <summary>
    /// Sets up the effect for the character. This is called when the effect is applied to a character.
    /// </summary>
    /// <param name="character"></param>
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
