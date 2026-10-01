using BrudvikWhiteHilt.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Mastery;

/// <summary>
/// The junk filter: Shift+E on an item on the ground marks that kind of item as junk, which is no longer picked up
/// by walking over it. The list is kept per character.
/// </summary>
public static class JunkFilter
{
    private const string Key = "whitehilt_junk";

    private static readonly List<ItemDrop> silenced = new();
    private static readonly Collider[] colliders = new Collider[128];

    /// <summary>
    /// Registers the English text. Call from the plugin's Awake.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglish("whitehilt_junk_mark", "Mark as junk");
        Translations.AddEnglish("whitehilt_junk_unmark", "Not junk");
        Translations.AddEnglish("whitehilt_junk", "Junk: not picked up by walking over it");
        Translations.AddEnglish("msg_whitehilt_junk_on", "{0} is junk now and is left lying");
        Translations.AddEnglish("msg_whitehilt_junk_off", "{0} is picked up again");
    }

    /// <summary>
    /// True if the player has marked the item as junk.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="sharedName">Shared item name.</param>
    /// <returns>True for junk.</returns>
    public static bool IsJunk(Player player, string sharedName)
    {
        return player != null && Names(player).Contains(sharedName);
    }

    /// <summary>
    /// Marks or unmarks an item as junk.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="sharedName">Shared item name.</param>
    public static void Toggle(Player player, string sharedName)
    {
        HashSet<string> names = Names(player);
        bool junk = !names.Remove(sharedName);
        if (junk)
        {
            names.Add(sharedName);
        }

        player.m_customData[Key] = string.Join(",", names);
        player.Message(MessageHud.MessageType.Center, Perks.Text(junk ? "$msg_whitehilt_junk_on" : "$msg_whitehilt_junk_off", Localization.instance.Localize(sharedName)));
    }

    /// <summary>
    /// The hover hint for an item on the ground.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="sharedName">Shared item name.</param>
    /// <returns>Localized hint.</returns>
    public static string Hint(Player player, string sharedName)
    {
        string action = IsJunk(player, sharedName) ? "$whitehilt_junk_unmark" : "$whitehilt_junk_mark";
        return Localization.instance.Localize($"\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] {action}");
    }

    /// <summary>
    /// Stops auto pickup of junk around the player for this frame.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void SilenceAround(Player player)
    {
        silenced.Clear();
        if (!player.m_customData.ContainsKey(Key))
        {
            return;
        }

        HashSet<string> names = Names(player);
        int count = Physics.OverlapSphereNonAlloc(player.transform.position + Vector3.up, player.m_autoPickupRange, colliders, player.m_autoPickupMask);
        for (int i = 0; i < count; i++)
        {
            ItemDrop drop = colliders[i].attachedRigidbody != null ? colliders[i].attachedRigidbody.GetComponent<ItemDrop>() : null;
            if (drop != null && drop.m_autoPickup && names.Contains(drop.m_itemData.m_shared.m_name))
            {
                drop.m_autoPickup = false;
                silenced.Add(drop);
            }
        }
    }

    /// <summary>
    /// Lets the silenced items be picked up again by others.
    /// </summary>
    public static void Restore()
    {
        foreach (ItemDrop drop in silenced.Where(drop => drop != null))
        {
            drop.m_autoPickup = true;
        }

        silenced.Clear();
    }

    private static HashSet<string> Names(Player player)
    {
        return player.m_customData.TryGetValue(Key, out string text)
            ? new HashSet<string>(text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            : new HashSet<string>();
    }
}
