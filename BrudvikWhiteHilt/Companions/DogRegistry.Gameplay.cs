using BrudvikWhiteHilt.Helpers;
using Jotunn.Entities;
using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Companions;

public static partial class DogRegistry
{
    /// <summary>
    /// Gives the local player the cuddle buff, once per game day.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="dogName">Name of the dog.</param>
    public static void Cuddle(Player player, string dogName)
    {
        if (EnvMan.instance == null)
        {
            return;
        }

        string today = EnvMan.instance.GetDay().ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (cuddleEffect == null || (player.m_customData.TryGetValue(CuddleDayKey, out string day) && day == today))
        {
            return;
        }

        player.m_customData[CuddleDayKey] = today;
        cuddleEffect.m_ttl = DogSettings.CuddleMinutes.Value * 60f;
        player.GetSEMan().AddStatusEffect(cuddleEffect.NameHash(), resetTime: true);
        player.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize(Translations.Token("whitehilt_dog_cuddled"), dogName));
    }

    /// <summary>
    /// Gives the local player Good Memories at a dog's grave, once per game day.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="dogName">Name on the grave.</param>
    public static void Remember(Player player, string dogName)
    {
        if (EnvMan.instance == null || memoriesEffect == null)
        {
            return;
        }

        string today = EnvMan.instance.GetDay().ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (player.m_customData.TryGetValue(MemoriesDayKey, out string day) && day == today)
        {
            return;
        }

        player.m_customData[MemoriesDayKey] = today;
        player.GetSEMan().AddStatusEffect(memoriesEffect.NameHash(), resetTime: true);
        player.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize(Translations.Token("whitehilt_dog_remember"), dogName));
    }

    /// <summary>
    /// Tells the dog's owner something about it, if they are within <paramref name="range"/> of <paramref name="position"/>.
    /// </summary>
    /// <param name="ownerId">Player ID of the owner.</param>
    /// <param name="messageKey">Translation key; $1 is the dog's name.</param>
    /// <param name="dogName">Name of the dog.</param>
    /// <param name="position">Where it happens.</param>
    /// <param name="range">How far away the owner hears of it, or <see cref="Anywhere"/>.</param>
    public static void SendNotice(long ownerId, string messageKey, string dogName, Vector3 position, float range)
    {
        ZRoutedRpc.instance?.InvokeRoutedRPC(ZRoutedRpc.Everybody, NoticeRpc, ownerId, messageKey, dogName, position, range);
    }

    /// <summary>
    /// Plays one of the dog's sounds here, on this client only.
    /// </summary>
    /// <param name="name">Sound name, e.g. dogbark.</param>
    /// <param name="position">Where.</param>
    /// <returns>Length of the sound in seconds, or 0 when it is not in the bundle.</returns>
    public static float PlaySound(string name, Vector3 position)
    {
        if (!sounds.TryGetValue(name, out GameObject effect) || effect == null)
        {
            return 0f;
        }

        UnityEngine.Object.Instantiate(effect, position + Vector3.up * 0.5f, Quaternion.identity);
        return soundLengths[name];
    }

    private static void RPC_Notice(long sender, long ownerId, string messageKey, string dogName, Vector3 position, float range)
    {
        Player player = Player.m_localPlayer;
        if (player != null && player.GetPlayerID() == ownerId && Vector3.Distance(player.transform.position, position) <= range)
        {
            MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, Localization.instance.Localize(Translations.Token(messageKey), dogName));
        }
    }

    /// <summary>
    /// Lets a puppy out of the player's pack in front of them.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="inventory">The inventory holding the puppy.</param>
    /// <param name="item">The puppy item.</param>
    /// <param name="color">Index of the coat colour.</param>
    public static void Release(Player player, Inventory inventory, ItemDrop.ItemData item, int color)
    {
        if (player.InInterior() || player.IsSwimming())
        {
            player.Message(MessageHud.MessageType.Center, Translations.Token("whitehilt_dog_release_blocked"));
            return;
        }

        // One dog per player: a puppy from a litter or left behind by an old dog must wait, or go to a friend.
        if (player.HaveUniqueKey(AliveKey) || DogCompanion.FindOwnedBy(player.GetPlayerID()) != null)
        {
            player.Message(MessageHud.MessageType.Center, Translations.Token("whitehilt_dog_release_has_dog"));
            return;
        }

        GameObject prefab = PrefabManager.Instance.GetPrefab(DogPrefabName);
        if (prefab == null)
        {
            Jotunn.Logger.LogError($"Dog: prefab {DogPrefabName} not found, the puppy cannot be let out");
            return;
        }

        Vector3 forward = player.transform.forward;
        Vector3 position = player.transform.position + forward * 1.5f + Vector3.up * 0.3f;
        GameObject dog = UnityEngine.Object.Instantiate(prefab, position, Quaternion.LookRotation(-forward));
        dog.GetComponent<DogCompanion>().InitializeNew(player.GetPlayerID(), color);
        inventory.RemoveOneItem(item);
        player.AddUniqueKey(KnownKey);
        player.AddUniqueKey(BuyKey);
        player.AddUniqueKey(AliveKey);
        player.Message(MessageHud.MessageType.Center, Translations.Token("whitehilt_dog_released"));
        TextInput.instance.RequestText(dog.GetComponent<Tameable>(), "$hud_rename", 20);
    }

    /// <summary>
    /// Registers the routed RPC. Call once per game session.
    /// </summary>
    public static void RegisterRpcs()
    {
        ZRoutedRpc.instance?.Register<long, string, string>(GoneRpc, RPC_Gone);
        ZRoutedRpc.instance?.Register<long, string, string, Vector3, float>(NoticeRpc, RPC_Notice);
    }

    /// <summary>
    /// Tells the dog's owner, wherever they are, that the dog is gone, so they may buy a new one.
    /// </summary>
    /// <param name="ownerId">Player ID of the owner.</param>
    /// <param name="messageKey">Translation key of the message; $1 is the dog's name.</param>
    /// <param name="dogName">Name of the dog.</param>
    public static void SendGone(long ownerId, string messageKey, string dogName)
    {
        ZRoutedRpc.instance?.InvokeRoutedRPC(ZRoutedRpc.Everybody, GoneRpc, ownerId, messageKey, dogName);
    }

    private static void RPC_Gone(long sender, long ownerId, string messageKey, string dogName)
    {
        Player player = Player.m_localPlayer;
        if (player == null || player.GetPlayerID() != ownerId)
        {
            return;
        }

        player.RemoveUniqueKey(BuyKey);
        player.RemoveUniqueKey(AliveKey);
        string text = Localization.instance.Localize(Translations.Token(messageKey), dogName);
        MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, text);
    }
}
