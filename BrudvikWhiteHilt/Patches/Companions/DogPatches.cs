using BrudvikWhiteHilt.Companions;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Companions;
using BrudvikWhiteHilt.Pieces.Portals.Effects;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Patches.Companions;

/// <summary>
/// Hooks the dog into the game: letting a puppy out of the pack, its care status on hover, walking to bed, the RPC,
/// keeping the dog's pieces unknown until the first dog, and carrying the dog's name and age from its remains
/// to the gravestone and onto the grave.
/// </summary>
[HarmonyPatch]
public static class DogPatches
{
    private const float BondRange = 30f;
    private const float PortalRange = 10f;
    private const float CuddleMood = 0.1f;

    private static readonly HashSet<string> dogPieces = new() { DogHouse.Name, DogBed.Name, DogBowl.Name, DogWaterBowl.Name, DogGrave.Name };

    private static ItemDrop.ItemData craftedFromRemains;
    private static string graveInscription;
    private static DogCompanion travellingDog;

    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    [HarmonyPostfix]
    private static void RegisterRpcs()
    {
        DogRegistry.RegisterRpcs();
    }

    /// <summary>
    /// Using a puppy item (hotbar or right-click) lets the puppy out instead.
    /// </summary>
    /// <param name="__instance">The character using the item.</param>
    /// <param name="inventory">The inventory holding the item.</param>
    /// <param name="item">The item.</param>
    /// <returns>False when a puppy was handled.</returns>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UseItem))]
    [HarmonyPrefix]
    private static bool ReleasePuppy(Humanoid __instance, Inventory inventory, ItemDrop.ItemData item)
    {
        if (__instance != Player.m_localPlayer)
        {
            return true;
        }

        if (DogRegistry.IsWhistle(item))
        {
            DogOwnerTools.UseWhistle(Player.m_localPlayer);
            return false;
        }

        if (item?.m_dropPrefab != null && item.m_dropPrefab.name == DogRegistry.StickPrefabName)
        {
            DogOwnerTools.ThrowStick(Player.m_localPlayer, inventory ?? __instance.GetInventory(), item);
            return false;
        }

        if (!DogRegistry.IsPuppy(item, out int color))
        {
            return true;
        }

        DogRegistry.Release(Player.m_localPlayer, inventory ?? __instance.GetInventory(), item, color);
        return false;
    }

    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    [HarmonyPostfix]
    private static void UpdateOwnerTools(Player __instance)
    {
        if (__instance == Player.m_localPlayer)
        {
            DogOwnerTools.Update(__instance);
        }
    }

    /// <summary>
    /// Using a collar, coat, bandage or treat on a dog (from the hotbar while looking at it) gives it to the dog.
    /// A new collar comes off with the old one back in the pack.
    /// </summary>
    /// <param name="__instance">The tameable.</param>
    /// <param name="user">The player.</param>
    /// <param name="item">The item.</param>
    /// <param name="__result">True when the item was used.</param>
    /// <returns>False when a dog item was handled.</returns>
    [HarmonyPatch(typeof(Tameable), nameof(Tameable.UseItem))]
    [HarmonyPrefix]
    private static bool GiveDogItem(Tameable __instance, Humanoid user, ItemDrop.ItemData item, ref bool __result)
    {
        if (!__instance.TryGetComponent(out DogCompanion dog) || item?.m_dropPrefab == null)
        {
            return true;
        }

        if (TryGiveCareItem(dog, user, item))
        {
            __result = true;
            return false;
        }

        int collar = DogRegistry.GetCollar(item);
        if (collar == 0)
        {
            return true;
        }

        int old = dog.SetCollar(collar);
        user.GetInventory().RemoveOneItem(item);
        string oldPrefab = DogRegistry.GetCollarPrefab(old);
        if (oldPrefab != null && ObjectDB.instance.GetItemPrefab(oldPrefab) is GameObject oldCollar)
        {
            user.GetInventory().AddItem(oldCollar, 1);
        }

        user.Message(MessageHud.MessageType.Center, Localization.instance.Localize(Translations.Token("whitehilt_dog_collar_on"), dog.DogName));
        __result = true;
        return false;
    }

    /// <summary>
    /// Petting a dog gives the cuddle buff once a day.
    /// </summary>
    /// <param name="__instance">The tameable.</param>
    /// <param name="user">The player.</param>
    /// <param name="hold">True while the key is held.</param>
    /// <param name="alt">True for the alternative use (rename).</param>
    /// <param name="__result">True when the interaction happened.</param>
    [HarmonyPatch(typeof(Tameable), nameof(Tameable.Interact))]
    [HarmonyPostfix]
    private static void Cuddle(Tameable __instance, Humanoid user, bool hold, bool alt, bool __result)
    {
        if (__result && !hold && !alt && user == Player.m_localPlayer && __instance.TryGetComponent(out DogCompanion dog))
        {
            DogRegistry.Cuddle(Player.m_localPlayer, dog.DogName);
            dog.Happy();

            // A dog lying down rolls over for a belly rub.
            DogCare care = dog.GetComponent<DogCare>();
            RestPose.Pose pose = dog.GetComponent<RestPose>().Current;
            care.ChangeMood(CuddleMood);
            if (pose == RestPose.Pose.Lie || pose == RestPose.Pose.Sleep)
            {
                care.Request(DogAction.Belly);
            }
        }
    }

    /// <summary>
    /// An emote near the player's own dog makes it do a trick, or teaches it one.
    /// </summary>
    /// <param name="__instance">The player.</param>
    /// <param name="emote">The emote's name.</param>
    /// <param name="__result">True when the emote started.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.StartEmote))]
    [HarmonyPostfix]
    private static void DoTrick(Player __instance, string emote, bool __result)
    {
        if (__result && __instance == Player.m_localPlayer)
        {
            DogTricks.OnEmote(__instance, emote);
        }
    }

    [HarmonyPatch(typeof(Tameable), nameof(Tameable.RPC_Command))]
    [HarmonyPostfix]
    private static void RememberFollow(Tameable __instance)
    {
        if (__instance.TryGetComponent(out DogCompanion dog))
        {
            dog.RememberFollow();
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.GetBodyArmor))]
    [HarmonyPostfix]
    private static void AddCollarArmor(Character __instance, ref float __result)
    {
        if (__instance.TryGetComponent(out DogCompanion dog))
        {
            __result += dog.CollarArmor;
        }
    }

    [HarmonyPatch(typeof(Attack), nameof(Attack.GetLevelDamageFactor))]
    [HarmonyPostfix]
    private static void AddBondDamage(Attack __instance, ref float __result)
    {
        __result *= DogCompanion.GetDamageFactor(__instance.m_character);
    }

    /// <summary>
    /// A kill by a dog, or by the player a dog follows, gives the dog bond experience: the creature's health.
    /// </summary>
    /// <param name="__instance">The creature that died.</param>
    [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
    [HarmonyPostfix]
    private static void GiveBondXp(Character __instance)
    {
        if (__instance.IsPlayer() || __instance.IsTamed() || !__instance.m_nview.IsValid() || !__instance.m_nview.IsOwner())
        {
            return;
        }

        Character attacker = __instance.m_lastHit?.GetAttacker();
        if (attacker == null)
        {
            return;
        }

        long masterId = attacker is Player player ? player.GetPlayerID() : 0L;
        foreach (DogCompanion dog in DogCompanion.All.Where(dog => dog != null && !dog.IsPuppy))
        {
            bool byDog = attacker.gameObject == dog.gameObject;
            bool byMaster = masterId != 0L && dog.FollowsPlayerId == masterId
                && Vector3.Distance(dog.transform.position, __instance.transform.position) <= BondRange;
            if (byDog || byMaster)
            {
                dog.AddXp(__instance.GetMaxHealth());
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.TeleportTo))]
    [HarmonyPrefix]
    private static void FindTravellingDog(Player __instance)
    {
        travellingDog = null;
        if (__instance != Player.m_localPlayer)
        {
            return;
        }

        DogCompanion dog = DogCompanion.FindOwnedBy(__instance.GetPlayerID());
        if (dog != null && !dog.IsPuppy && dog.FollowsPlayerId == __instance.GetPlayerID()
            && Vector3.Distance(dog.transform.position, __instance.transform.position) <= PortalRange)
        {
            travellingDog = dog;
        }
    }

    /// <summary>
    /// A following dog near the player goes through the portal too.
    /// </summary>
    /// <param name="__instance">The player.</param>
    /// <param name="pos">Where the player goes.</param>
    /// <param name="rot">The player's rotation there.</param>
    /// <param name="__result">True when the teleport started.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.TeleportTo))]
    [HarmonyPostfix]
    private static void TakeDogAlong(Player __instance, Vector3 pos, Quaternion rot, bool __result)
    {
        if (__result && travellingDog != null)
        {
            Vector3 from = travellingDog.transform.position;
            Vector3 to = pos - rot * Vector3.forward * 2f;
            travellingDog.TeleportTo(to);
            PortalFx.SendDog(from, to, PortalFx.ColourAt(__instance.transform.position));
        }

        travellingDog = null;
    }

    /// <summary>
    /// The whistle, collars, dog food and gravestone are learnt with the first dog, not from their materials.
    /// </summary>
    /// <param name="__instance">The player.</param>
    /// <param name="recipe">The recipe.</param>
    /// <param name="discover">True when checking whether the recipe becomes known.</param>
    /// <param name="__result">False while the recipe stays unknown.</param>
    /// <returns>False when the recipe stays unknown.</returns>
    [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), new[] { typeof(Recipe), typeof(bool), typeof(int), typeof(int) })]
    [HarmonyPrefix]
    private static bool KnowDogRecipes(Player __instance, Recipe recipe, bool discover, ref bool __result)
    {
        if (!discover || recipe?.m_item == null || !DogRegistry.IsDogItem(recipe.m_item.name) || DogRegistry.IsDogKnown(__instance))
        {
            return true;
        }

        __result = false;
        return false;
    }

    /// <summary>
    /// The dog house, bed, bowl and grave become known with the first dog, not from their materials.
    /// </summary>
    /// <param name="__instance">The player.</param>
    /// <param name="piece">The piece.</param>
    /// <param name="mode">What is checked.</param>
    /// <param name="__result">Whether the piece is known.</param>
    /// <returns>False when a dog piece was answered.</returns>
    [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), new[] { typeof(Piece), typeof(Player.RequirementMode) })]
    [HarmonyPrefix]
    private static bool KnowDogPieces(Player __instance, Piece piece, Player.RequirementMode mode, ref bool __result)
    {
        if (mode != Player.RequirementMode.IsKnown || piece == null || !dogPieces.Contains(piece.gameObject.name))
        {
            return true;
        }

        __result = DogRegistry.IsDogKnown(__instance);
        return false;
    }

    /// <summary>
    /// Adds the dog's growth and care needs under its name.
    /// </summary>
    /// <param name="__instance">The tameable.</param>
    /// <param name="__result">The hover text.</param>
    [HarmonyPatch(typeof(Tameable), nameof(Tameable.GetHoverText))]
    [HarmonyPostfix]
    private static void AddCareStatus(Tameable __instance, ref string __result)
    {
        if (string.IsNullOrEmpty(__result) || !__instance.TryGetComponent(out DogCompanion dog))
        {
            return;
        }

        string status = dog.GetStatusText();
        int firstLineEnd = __result.IndexOf('\n');
        __result = firstLineEnd < 0 ? __result + status : __result.Insert(firstLineEnd, status);
    }

    /// <summary>
    /// Walks a dog to its bed or house when it is time to rest.
    /// </summary>
    /// <param name="__instance">The creature's AI.</param>
    /// <param name="dt">Frame time.</param>
    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
    [HarmonyPostfix]
    private static void WalkToRest(MonsterAI __instance, float dt)
    {
        if (__instance.m_nview.IsValid() && __instance.m_nview.IsOwner() && __instance.TryGetComponent(out DogCompanion dog))
        {
            dog.UpdateGoal(dt);
            dog.GetComponent<DogActivities>()?.UpdateActivity(dt);
            dog.GetComponent<DogCare>()?.UpdateAI();
        }
    }

    /// <summary>
    /// Remembers which remains a gravestone is cut from.
    /// </summary>
    /// <param name="__instance">The crafting window.</param>
    /// <param name="player">The crafting player.</param>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
    [HarmonyPrefix]
    private static void FindRemains(InventoryGui __instance, Player player)
    {
        craftedFromRemains = null;
        if (__instance.m_craftRecipe?.m_item != null && __instance.m_craftRecipe.m_item.name == DogRegistry.GravestonePrefabName)
        {
            craftedFromRemains = FindInscribed(player, DogRegistry.RemainsPrefabName);
        }
    }

    /// <summary>
    /// Chisels the dog's name and age into the new gravestone.
    /// </summary>
    /// <param name="player">The crafting player.</param>
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
    [HarmonyPostfix]
    private static void InscribeGravestone(Player player)
    {
        if (craftedFromRemains == null)
        {
            return;
        }

        ItemDrop.ItemData stone = player.GetInventory().GetAllItems()
            .FirstOrDefault(item => IsPrefab(item, DogRegistry.GravestonePrefabName) && !DogRegistry.HasInscription(item));
        if (stone != null)
        {
            DogRegistry.CopyInscription(craftedFromRemains, stone);
        }

        craftedFromRemains = null;
    }

    /// <summary>
    /// Remembers the inscription of the gravestone a grave is raised from; the stone is used up after placing.
    /// </summary>
    /// <param name="__instance">The building player.</param>
    /// <param name="piece">The piece being placed.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
    [HarmonyPrefix]
    private static void FindGravestone(Player __instance, Piece piece)
    {
        graveInscription = piece != null && piece.gameObject.name == DogGrave.Name
            ? DogRegistry.GetInscription(FindInscribed(__instance, DogRegistry.GravestonePrefabName))
            : null;
    }

    [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
    [HarmonyFinalizer]
    private static void ForgetGravestone()
    {
        graveInscription = null;
    }

    /// <summary>
    /// Writes the inscription onto the grave just placed.
    /// </summary>
    /// <param name="__instance">The new piece.</param>
    [HarmonyPatch(typeof(Piece), nameof(Piece.SetCreator))]
    [HarmonyPostfix]
    private static void InscribeGrave(Piece __instance)
    {
        if (graveInscription != null && __instance.TryGetComponent(out Sign sign))
        {
            sign.SetText(graveInscription);
            graveInscription = null;
        }
    }

    /// <summary>
    /// Shows the dog's name and age on its remains and gravestone.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="__result">The tooltip.</param>
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip),
        new[] { typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool) })]
    [HarmonyPostfix]
    private static void ShowInscription(ItemDrop.ItemData item, ref string __result)
    {
        string inscription = DogRegistry.GetInscription(item);
        if (inscription != null)
        {
            __result += "\n\n<color=orange>" + inscription.Replace("\n", ", ") + "</color>";
        }
    }

    private static bool TryGiveCareItem(DogCompanion dog, Humanoid user, ItemDrop.ItemData item)
    {
        DogCare care = dog.GetComponent<DogCare>();
        string prefabName = item.m_dropPrefab.name;
        string message;
        if (prefabName == DogRegistry.TreatPrefabName)
        {
            care.Give(DogCare.CareItem.Treat);
            message = "whitehilt_dog_treat";
        }
        else if (prefabName == DogRegistry.BandagePrefabName)
        {
            care.Give(DogCare.CareItem.Bandage);
            message = "whitehilt_dog_bandaged";
        }
        else if (prefabName == DogRegistry.CoatPrefabName)
        {
            if (care.HasCoat)
            {
                user.Message(MessageHud.MessageType.Center, Localization.instance.Localize(Translations.Token("whitehilt_dog_has_coat"), dog.DogName));
                return true;
            }

            care.Give(DogCare.CareItem.Coat);
            message = "whitehilt_dog_coat_on";
        }
        else
        {
            return false;
        }

        user.GetInventory().RemoveOneItem(item);
        user.Message(MessageHud.MessageType.Center, Localization.instance.Localize(Translations.Token(message), dog.DogName));
        return true;
    }

    private static ItemDrop.ItemData FindInscribed(Player player, string prefabName)
    {
        return player.GetInventory().GetAllItems().FirstOrDefault(item => IsPrefab(item, prefabName) && DogRegistry.HasInscription(item));
    }

    private static bool IsPrefab(ItemDrop.ItemData item, string prefabName)
    {
        return item.m_dropPrefab != null && item.m_dropPrefab.name == prefabName;
    }
}
