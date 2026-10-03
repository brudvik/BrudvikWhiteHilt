using BrudvikWhiteHilt.Chests.Collection;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Chests;

/// <summary>Connects chest handoff and hover text to the collection system.</summary>
internal static class CollectionPostPatches
{
    [HarmonyPatch(typeof(Container), "Awake"), HarmonyPostfix]
    private static void Register(Container __instance)
    {
        if (CollectionPostComponent.Module != null && CollectionPostComponent.Module.IsCollectionChest(__instance)
            && __instance.m_nview != null && __instance.m_nview.IsValid())
            __instance.gameObject.AddComponent<CollectionChestLink>();
    }

    [HarmonyPatch(typeof(StationExtension), nameof(StationExtension.GetHoverText)), HarmonyPostfix]
    private static void Hover(StationExtension __instance, ref string __result)
    {
        var collector = __instance.GetComponent<CollectionPostComponent>();
        if (collector != null) __result = collector.GetHoverText();
    }

    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.OnPlayerDrop)), HarmonyPostfix]
    private static void PlayerDrop(ItemDrop __instance)
    {
        if (__instance.m_nview != null && __instance.m_nview.IsValid() && __instance.m_nview.IsOwner())
            __instance.m_nview.GetZDO().Set("whitehilt_player_drop", true);
    }

    [HarmonyPatch(typeof(Player), "UpdatePlacementGhost"), HarmonyPostfix]
    private static void Placement(Player __instance)
    {
        if (__instance != Player.m_localPlayer || __instance.m_placementGhost == null) return;
        var collector = __instance.m_placementGhost.GetComponent<CollectionPostComponent>();
        if (collector == null) return;
        var marker = collector.transform.Find("CollectionArea");
        if (marker == null) return;
        var circle = marker.GetComponent<CircleProjector>();
        if (circle != null) circle.m_radius = CollectionSettings.Radius.Value;
        marker.gameObject.SetActive(true);
    }
}