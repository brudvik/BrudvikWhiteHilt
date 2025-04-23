﻿using BrudvikWhiteHilt.Extensions;
using BrudvikWhiteHilt.Helpers;
using System;

namespace BrudvikWhiteHilt.Items.Potions.GiftOfMunin;

/// <summary>
/// This class defines the effect of the Gift of Munin potion.
/// </summary>
public class GiftOfMuninEffect : StatusEffect
{
    /// <summary>
    /// The player character that the effect is applied to.
    /// </summary>
    private Player player;

    /// <summary>
    /// The hash of the effect. This is used to identify the effect.
    /// </summary>
    public int? EffectHash = null;

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

        var allItems = ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Material, "");
        foreach (var item in allItems)
        {
            try
            {
                if (!player.IsKnownMaterial(item.name))
                {
                    player.AddKnownItem(item.m_itemData);
                }
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogError($"Error adding known item {item.name}: {ex}");
            }
        }
    }

}
