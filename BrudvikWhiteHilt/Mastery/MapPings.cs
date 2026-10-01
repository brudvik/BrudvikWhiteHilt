using BrudvikWhiteHilt.Helpers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Mastery;

/// <summary>
/// Map pins that are not saved and go away by themselves.
/// </summary>
public static class TemporaryPins
{
    private static readonly List<(Minimap.PinData Pin, float Until)> pins = new();

    /// <summary>
    /// Adds a pin for a while.
    /// </summary>
    /// <param name="position">World position.</param>
    /// <param name="type">Pin icon.</param>
    /// <param name="name">Text under the pin.</param>
    /// <param name="seconds">How long it stays.</param>
    public static void Add(Vector3 position, Minimap.PinType type, string name, float seconds)
    {
        if (Minimap.instance == null)
        {
            return;
        }

        Minimap.PinData pin = Minimap.instance.AddPin(position, type, name, save: false, isChecked: false);
        pins.Add((pin, Time.time + seconds));
    }

    /// <summary>
    /// Removes the pins whose time is up. Call every frame.
    /// </summary>
    public static void Update()
    {
        if (pins.Count == 0 || Minimap.instance == null)
        {
            pins.Clear();
            return;
        }

        for (int i = pins.Count - 1; i >= 0; i--)
        {
            if (Time.time >= pins[i].Until)
            {
                Minimap.instance.RemovePin(pins[i].Pin);
                pins.RemoveAt(i);
            }
        }
    }
}

/// <summary>
/// The Ore Echo: striking rock makes ore deposits nearby show on the map for a minute.
/// </summary>
public static class OreEcho
{
    private const float PinSeconds = 60f;

    private static float nextPing;

    /// <summary>
    /// Registers the English text. Call from the plugin's Awake.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglish("msg_whitehilt_oreecho", "The rock rings: {0} deposits nearby");
    }

    /// <summary>
    /// Pings for ore if the player has the milestone and the echo is not resting.
    /// </summary>
    /// <param name="player">The local player striking rock.</param>
    public static void TryPing(Player player)
    {
        if (player != Player.m_localPlayer || Time.time < nextPing || !Perks.OreEcho.Has(player))
        {
            return;
        }

        nextPing = Time.time + MasterySettings.OreEchoCooldown.Value;
        float range = Perks.Prospector.Has(player) ? MasterySettings.ProspectorRange.Value : MasterySettings.OreEchoRange.Value;
        Vector3 origin = player.transform.position;
        int found = 0;
        foreach ((Vector3 position, string ore) in Deposits(origin, range))
        {
            TemporaryPins.Add(position, Minimap.PinType.Icon2, ore, PinSeconds);
            found++;
        }

        if (found > 0)
        {
            player.Message(MessageHud.MessageType.TopLeft, Perks.Text("$msg_whitehilt_oreecho", found.ToString()));
        }
    }

    /// <summary>
    /// True for ore and metal scrap.
    /// </summary>
    /// <param name="prefabName">Prefab name of a drop.</param>
    /// <returns>True for ore.</returns>
    public static bool IsOre(string prefabName)
    {
        return prefabName != null && (prefabName.Contains("Ore") || prefabName.Contains("Scrap") || prefabName == "Obsidian");
    }

    private static IEnumerable<(Vector3, string)> Deposits(Vector3 origin, float range)
    {
        foreach (MineRock5 rock in Object.FindObjectsByType<MineRock5>(FindObjectsSortMode.None))
        {
            if (Near(rock, origin, range) && OreName(rock.m_dropItems) is string ore)
            {
                yield return (rock.transform.position, ore);
            }
        }

        foreach (MineRock rock in Object.FindObjectsByType<MineRock>(FindObjectsSortMode.None))
        {
            if (Near(rock, origin, range) && OreName(rock.m_dropItems) is string ore)
            {
                yield return (rock.transform.position, ore);
            }
        }

        foreach (DropOnDestroyed drops in Object.FindObjectsByType<DropOnDestroyed>(FindObjectsSortMode.None))
        {
            if (Near(drops, origin, range) && drops.GetComponent<Destructible>() != null && OreName(drops.m_dropWhenDestroyed) is string ore)
            {
                yield return (drops.transform.position, ore);
            }
        }
    }

    private static bool Near(Component component, Vector3 origin, float range)
    {
        return component != null && Vector3.Distance(component.transform.position, origin) <= range;
    }

    private static string OreName(DropTable table)
    {
        GameObject ore = table?.m_drops?.Select(drop => drop.m_item).FirstOrDefault(item => item != null && IsOre(item.name));
        ItemDrop drop = ore != null ? ore.GetComponent<ItemDrop>() : null;
        return drop != null ? Localization.instance.Localize(drop.m_itemData.m_shared.m_name) : null;
    }
}

/// <summary>
/// The Lookout: the map opens up around the player, and sea monsters and ships show on it for a while.
/// </summary>
public static class Lookout
{
    private const float PinSeconds = 60f;

    private static readonly string[] seaMonsters = { "Serpent", Kraken.KrakenRegistry.BodyName, "BonemawSerpent" };
    private static float nextUse;

    /// <summary>
    /// Registers the English text. Call from the plugin's Awake.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglish("msg_whitehilt_lookout", "You look out over the land and sea");
        Translations.AddEnglish("msg_whitehilt_lookout_rest", "Your eyes need rest ({0} s)");
        Translations.AddEnglish("whitehilt_lookout_ship", "Ship");
    }

    /// <summary>
    /// Looks out when the key is pressed. Call every frame for the local player.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Update(Player player)
    {
        if (Backpack.BackpackInput.Typing() || !Backpack.BackpackInput.Pressed(MasterySettings.KeyLookout) || !Perks.Lookout.Has(player))
        {
            return;
        }

        if (Time.time < nextUse)
        {
            player.Message(MessageHud.MessageType.TopLeft,
                Perks.Text("$msg_whitehilt_lookout_rest", Mathf.CeilToInt(nextUse - Time.time).ToString()));
            return;
        }

        nextUse = Time.time + MasterySettings.LookoutCooldownMinutes.Value * 60f;
        float radius = MasterySettings.LookoutRadius.Value * (1f + Perks.Factor(SkillLevels.Get(player, Perks.Lookout.Skill)));
        Vector3 origin = player.transform.position;
        Minimap.instance?.Explore(origin, radius);
        player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_lookout");

        foreach (Character character in Character.GetAllCharacters())
        {
            string name = Utils.GetPrefabName(character.gameObject);
            if (seaMonsters.Any(monster => name.StartsWith(monster)) && Vector3.Distance(character.transform.position, origin) <= radius)
            {
                TemporaryPins.Add(character.transform.position, Minimap.PinType.Icon3, Localization.instance.Localize(character.m_name), PinSeconds);
            }
        }

        foreach (Ship ship in Ship.Instances.OfType<Ship>())
        {
            if (ship != null && Vector3.Distance(ship.transform.position, origin) <= radius && !ship.IsPlayerInBoat(player))
            {
                TemporaryPins.Add(ship.transform.position, Minimap.PinType.Icon4, Localization.instance.Localize("$whitehilt_lookout_ship"), PinSeconds);
            }
        }
    }
}
