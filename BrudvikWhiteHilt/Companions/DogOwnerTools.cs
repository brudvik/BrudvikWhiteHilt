using BrudvikWhiteHilt.Helpers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Companions;

/// <summary>
/// The dog seen from its owner's side: map pins for the dog and its house, sniffing out forage while it follows,
/// and the whistle. Runs on the owner's own client only.
/// </summary>
public static class DogOwnerTools
{
    private const float PinSeconds = 2f;
    private const float SniffSeconds = 20f;
    private const float SniffRange = 25f;
    private const float SniffFollowRange = 20f;
    private const float FoundPinSeconds = 60f;
    private const float FetchRange = 25f;
    private const float ThrowSpeed = 14f;
    private const int MaxSniffed = 2000;
    private const string DogPositionKey = "whitehilt_dog_pos";
    private const string HomePositionKey = "whitehilt_dog_homepos";
    private const string DogNameKey = "whitehilt_dog_mapname";

    private static readonly List<KeyValuePair<Minimap.PinData, float>> foundPins = new();
    private static readonly HashSet<int> sniffed = new();
    private static Minimap minimap;
    private static Minimap.PinData dogPin;
    private static Minimap.PinData homePin;
    private static float nextPins;
    private static float nextSniff;

    /// <summary>
    /// Updates pins and sniffing. Call every frame for the local player.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Update(Player player)
    {
        if (Minimap.instance == null)
        {
            return;
        }

        // A new game session has a new map, and the old pins are gone with the old one.
        if (Minimap.instance != minimap)
        {
            minimap = Minimap.instance;
            dogPin = null;
            homePin = null;
            foundPins.Clear();
        }

        float now = Time.time;
        if (now >= nextPins)
        {
            nextPins = now + PinSeconds;
            UpdatePins(player);
            RemoveOldFinds(now);
        }

        if (now >= nextSniff)
        {
            nextSniff = now + SniffSeconds;
            Sniff(player);
        }
    }

    /// <summary>
    /// Blows the whistle: calls a dog that stays behind, or sends a following dog home.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void UseWhistle(Player player)
    {
        long playerId = player.GetPlayerID();
        DogCompanion dog = DogCompanion.FindOwnedBy(playerId);
        if (dog == null)
        {
            player.Message(MessageHud.MessageType.Center, Translations.Token("whitehilt_dog_not_near"));
            return;
        }

        string key;
        if (dog.IsPuppy)
        {
            key = "whitehilt_dog_too_small";
        }
        else if (dog.FollowsPlayerId == playerId)
        {
            dog.SendHome();
            key = "whitehilt_dog_goes_home";
        }
        else
        {
            dog.Call(player);
            key = "whitehilt_dog_comes";
        }

        player.Message(MessageHud.MessageType.Center, Localization.instance.Localize(Translations.Token(key), dog.DogName));
    }

    /// <summary>
    /// Throws a stick for the player's grown dog to fetch.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="inventory">The inventory holding the stick.</param>
    /// <param name="item">The stick.</param>
    public static void ThrowStick(Player player, Inventory inventory, ItemDrop.ItemData item)
    {
        long playerId = player.GetPlayerID();
        DogCompanion dog = DogCompanion.FindOwnedBy(playerId);
        DogActivities activities = dog != null ? dog.GetComponent<DogActivities>() : null;
        if (activities == null || dog.IsPuppy || dog.FollowsPlayerId != playerId
            || Vector3.Distance(dog.transform.position, player.transform.position) > FetchRange)
        {
            player.Message(MessageHud.MessageType.Center, Translations.Token("whitehilt_dog_no_fetch"));
            return;
        }

        ItemDrop.ItemData thrown = item.Clone();
        thrown.m_stack = 1;
        inventory.RemoveOneItem(item);
        Vector3 look = player.GetLookDir();
        ItemDrop stick = ItemDrop.DropItem(thrown, 1, player.GetEyePoint() + look * 0.8f, Quaternion.LookRotation(look));
        stick.OnPlayerDrop();
        Rigidbody body = stick.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.linearVelocity = look * ThrowSpeed + Vector3.up * 3f;
        }

        activities.Fetch(stick.GetComponent<ZNetView>().GetZDO().m_uid, playerId);
    }

    // Positions are kept in the player's data, so the pins stay when the dog is out of range.
    private static void UpdatePins(Player player)
    {
        if (!player.HaveUniqueKey(DogRegistry.BuyKey))
        {
            RemovePin(ref dogPin);
            RemovePin(ref homePin);
            return;
        }

        DogCompanion dog = DogCompanion.FindOwnedBy(player.GetPlayerID());
        if (dog != null)
        {
            player.m_customData[DogPositionKey] = Format(dog.transform.position);
            player.m_customData[HomePositionKey] = Format(dog.Home);
            player.m_customData[DogNameKey] = dog.DogName;
        }

        player.m_customData.TryGetValue(DogNameKey, out string dogName);
        SetPin(ref dogPin, player, DogPositionKey, Minimap.PinType.Icon3, dogName ?? string.Empty);
        SetPin(ref homePin, player, HomePositionKey, Minimap.PinType.Icon1, Localization.instance.Localize(Translations.Token("whitehilt_dog_home_pin")));
    }

    private static void SetPin(ref Minimap.PinData pin, Player player, string key, Minimap.PinType type, string name)
    {
        if (!player.m_customData.TryGetValue(key, out string text) || !TryParse(text, out Vector3 position))
        {
            RemovePin(ref pin);
            return;
        }

        if (pin == null)
        {
            pin = Minimap.instance.AddPin(position, type, name, save: false, isChecked: false);
            return;
        }

        pin.m_pos = position;
        pin.m_name = name;
    }

    private static void RemovePin(ref Minimap.PinData pin)
    {
        if (pin != null)
        {
            Minimap.instance.RemovePin(pin);
            pin = null;
        }
    }

    // A grown dog that follows its owner barks at the nearest ripe berry, mushroom or White Hilt plant and marks it on the map.
    private static void Sniff(Player player)
    {
        long playerId = player.GetPlayerID();
        DogCompanion dog = DogCompanion.FindOwnedBy(playerId);
        if (dog == null || dog.IsPuppy || dog.FollowsPlayerId != playerId
            || Vector3.Distance(dog.transform.position, player.transform.position) > SniffFollowRange)
        {
            return;
        }

        Vector3 nose = dog.transform.position;
        Pickable find = Object.FindObjectsByType<Pickable>(FindObjectsSortMode.None)
            .Where(pickable => IsForage(pickable) && !sniffed.Contains(pickable.GetInstanceID())
                && Vector3.Distance(pickable.transform.position, nose) <= SniffRange)
            .OrderBy(pickable => Vector3.Distance(pickable.transform.position, nose))
            .FirstOrDefault();
        if (find == null)
        {
            return;
        }

        sniffed.Add(find.GetInstanceID());
        if (sniffed.Count > MaxSniffed)
        {
            sniffed.Clear();
        }
        dog.Bark();
        string text = Localization.instance.Localize(Translations.Token("whitehilt_dog_found"), dog.DogName);
        foundPins.Add(new KeyValuePair<Minimap.PinData, float>(
            Minimap.instance.AddPin(find.transform.position, Minimap.PinType.Icon3, text, save: false, isChecked: false), Time.time + FoundPinSeconds));
        player.Message(MessageHud.MessageType.TopLeft, text);
    }

    private static bool IsForage(Pickable pickable)
    {
        if (pickable == null || pickable.m_itemPrefab == null || !pickable.CanBePicked())
        {
            return false;
        }

        ItemDrop item = pickable.m_itemPrefab.GetComponent<ItemDrop>();
        return pickable.m_itemPrefab.name.StartsWith("WhiteHilt", System.StringComparison.Ordinal)
            || (item != null && item.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable);
    }

    private static void RemoveOldFinds(float now)
    {
        foreach (KeyValuePair<Minimap.PinData, float> found in foundPins.Where(found => found.Value <= now).ToList())
        {
            Minimap.instance.RemovePin(found.Key);
            foundPins.Remove(found);
        }
    }

    private static string Format(Vector3 position)
    {
        return string.Join(";", new[] { position.x, position.y, position.z }.Select(value => value.ToString("F1", CultureInfo.InvariantCulture)));
    }

    private static bool TryParse(string text, out Vector3 position)
    {
        position = Vector3.zero;
        string[] parts = text.Split(';');
        if (parts.Length != 3)
        {
            return false;
        }

        float[] values = new float[3];
        for (int i = 0; i < 3; i++)
        {
            if (!float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out values[i]))
            {
                return false;
            }
        }

        position = new Vector3(values[0], values[1], values[2]);
        return true;
    }
}
