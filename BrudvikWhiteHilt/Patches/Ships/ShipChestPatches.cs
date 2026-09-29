using BrudvikWhiteHilt.Pieces.Ships.WhiteHiltShip;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Ships;

/// <summary>
/// Gives the White Hilt Ship's chest its own items and RPCs, since the ship's cargo hold already uses the vanilla ones
/// on the same network object.
/// </summary>
[HarmonyPatch(typeof(Container))]
public static class ShipChestPatches
{
    /// <summary>
    /// Sets the chest up with its own RPCs.
    /// </summary>
    /// <param name="__instance">The container.</param>
    /// <returns>False for the ship chest.</returns>
    [HarmonyPatch(nameof(Container.Awake))]
    [HarmonyPrefix]
    public static bool Awake(Container __instance)
    {
        if (__instance.GetComponent<ShipChest>() == null)
        {
            return true;
        }

        ShipChest.Setup(__instance);
        return false;
    }

    /// <summary>
    /// Opens the chest through its own RPC.
    /// </summary>
    /// <param name="__instance">The container.</param>
    /// <param name="hold">True while the key is held.</param>
    /// <param name="__result">True when handled.</param>
    /// <returns>False for the ship chest.</returns>
    [HarmonyPatch(nameof(Container.Interact))]
    [HarmonyPrefix]
    public static bool Interact(Container __instance, bool hold, ref bool __result)
    {
        if (__instance.GetComponent<ShipChest>() == null)
        {
            return true;
        }

        __result = !hold;
        if (!hold && __instance.m_nview != null && __instance.m_nview.IsValid())
        {
            ShipChest.Open(__instance);
        }

        return false;
    }

    /// <summary>
    /// Saves the chest under its own key.
    /// </summary>
    /// <param name="__instance">The container.</param>
    /// <returns>False for the ship chest.</returns>
    [HarmonyPatch(nameof(Container.Save))]
    [HarmonyPrefix]
    public static bool Save(Container __instance)
    {
        if (__instance.GetComponent<ShipChest>() == null)
        {
            return true;
        }

        ShipChest.Save(__instance);
        return false;
    }

    /// <summary>
    /// Loads the chest from its own key.
    /// </summary>
    /// <param name="__instance">The container.</param>
    /// <param name="__result">True if the data changed.</param>
    /// <returns>False for the ship chest.</returns>
    [HarmonyPatch(nameof(Container.Load))]
    [HarmonyPrefix]
    public static bool Load(Container __instance, ref bool __result)
    {
        if (__instance.GetComponent<ShipChest>() == null)
        {
            return true;
        }

        __result = ShipChest.Load(__instance);
        return false;
    }

    /// <summary>
    /// Holding Use on the open chest places stacks in it directly; the vanilla RPC would go to the hold.
    /// </summary>
    /// <param name="__instance">The container.</param>
    /// <returns>False for the ship chest.</returns>
    [HarmonyPatch(nameof(Container.StackAll))]
    [HarmonyPrefix]
    public static bool StackAll(Container __instance)
    {
        if (__instance.GetComponent<ShipChest>() == null)
        {
            return true;
        }

        if (__instance.IsOwner() && Player.m_localPlayer != null)
        {
            __instance.GetInventory().StackAll(Player.m_localPlayer.GetInventory(), message: true);
        }

        return false;
    }
}
