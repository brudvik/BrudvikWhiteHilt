using BrudvikWhiteHilt.Building.Groups;
using BrudvikWhiteHilt.Crafting;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Building.Terrain;

/// <summary>
/// What paving and raising ground cost, worked out from the vanilla hoe's own paved road and raise ground so the tools
/// cost the same per square and cubic metre. Lowering, levelling, dirt and cultivating are free, as with the hoe.
/// </summary>
public static class TerrainCost
{
    private const string PavedPrefab = "paved_road_v2";
    private const string RaisePrefab = "raise_v2";
    private const float FallbackPavedPerSquareMetre = 0.2f;
    private const float FallbackRaisePerCubicMetre = 0.5f;

    private static bool derived;
    private static ItemDrop stone;
    private static float pavedPerSquareMetre;
    private static float raisePerCubicMetre;

    /// <summary>The resource paving and raising cost, normally stone.</summary>
    public static ItemDrop Stone
    {
        get
        {
            Derive();
            return stone;
        }
    }

    /// <summary>
    /// Stone for a job.
    /// </summary>
    /// <param name="pavedSquareMetres">Square metres paved with stone.</param>
    /// <param name="raisedCubicMetres">Cubic metres of ground raised.</param>
    /// <returns>Whole stones.</returns>
    public static int Amount(float pavedSquareMetres, float raisedCubicMetres)
    {
        Derive();
        float cost = (pavedSquareMetres * pavedPerSquareMetre + raisedCubicMetres * raisePerCubicMetre) * TerrainSettings.CostMultiplier.Value;
        return Mathf.CeilToInt(cost - 0.001f);
    }

    /// <summary>
    /// Text for the cost, e.g. "Cost: Stone 12".
    /// </summary>
    /// <param name="amount">Whole stones.</param>
    /// <returns>The text.</returns>
    public static string Text(int amount)
    {
        if (amount <= 0 || Stone == null)
        {
            return Localization.instance.Localize("$whitehilt_terrain_free");
        }

        return string.Format(Localization.instance.Localize("$whitehilt_terrain_cost"),
            Localization.instance.Localize(Stone.m_itemData.m_shared.m_name) + " " + amount);
    }

    /// <summary>
    /// Takes the stone from the inventory first, then from nearby chests.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="amount">Whole stones.</param>
    /// <param name="again">Does the work again once stone fetched from other players' chests has arrived, or null.</param>
    /// <returns>True if paid; false (and nothing taken) if the player has too little or it is being fetched.</returns>
    public static bool TryPay(Player player, int amount, Action again = null)
    {
        return TryPayItem(player, Stone, amount, again);
    }

    /// <summary>
    /// Takes a resource from the inventory first, then from nearby chests, as building does (<see cref="ChestCost"/>):
    /// what lies in chests other players hold is fetched first, and <paramref name="again"/> does the work once it has
    /// arrived. Without it the player clicks again.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="item">The resource.</param>
    /// <param name="amount">How many.</param>
    /// <param name="again">Does the work again once fetched items have arrived, or null.</param>
    /// <returns>True if paid; false (and nothing taken) if the player has too little or it is being fetched.</returns>
    public static bool TryPayItem(Player player, ItemDrop item, int amount, Action again = null)
    {
        if (amount <= 0 || player.m_noPlacementCost || item == null)
        {
            return true;
        }

        string name = item.m_itemData.m_shared.m_name;
        List<(string Name, int Amount)> needs = new() { (name, amount) };
        switch (ChestCost.Gather(player, NearbyContainers.Use.Building, needs, again))
        {
            case ChestCost.Outcome.Ready:
                ChestCost.Take(player, NearbyContainers.Use.Building, needs);
                return true;
            case ChestCost.Outcome.Lacking:
                player.Message(MessageHud.MessageType.Center,
                    string.Format(Localization.instance.Localize("$msg_whitehilt_terrain_missing"), Localization.instance.Localize(name), amount));
                return false;
            default:
                ChestCost.ShowWaiting(player);
                return false;
        }
    }

    /// <summary>
    /// Gives back stone after an undo.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="item">The resource.</param>
    /// <param name="amount">How many.</param>
    public static void Refund(Player player, ItemDrop item, int amount)
    {
        GroupPlacer.Give(player, new[] { new Piece.Requirement { m_resItem = item, m_amount = amount, m_recover = true } }, onlyRecoverable: false);
    }

    // Works out the stone cost from the vanilla hoe pieces instead of fixed numbers: stone per square metre from
    // paving, per cubic metre from raising. If the game changes those pieces, the tools cost the same as doing the work
    // with the vanilla hoe.
    private static void Derive()
    {
        if (derived || ZNetScene.instance == null)
        {
            return;
        }

        derived = true;
        pavedPerSquareMetre = FallbackPavedPerSquareMetre;
        raisePerCubicMetre = FallbackRaisePerCubicMetre;
        stone = ObjectDB.instance?.GetItemPrefab("Stone")?.GetComponent<ItemDrop>();

        Piece paved = ZNetScene.instance.GetPrefab(PavedPrefab)?.GetComponent<Piece>();
        TerrainOp pavedOp = paved != null ? paved.GetComponent<TerrainOp>() : null;
        if (paved != null && pavedOp != null && paved.m_resources.Length > 0 && paved.m_resources[0].m_resItem != null)
        {
            stone = paved.m_resources[0].m_resItem;
            float radius = Mathf.Max(0.5f, pavedOp.m_settings.m_paintRadius);
            pavedPerSquareMetre = paved.m_resources[0].m_amount / (Mathf.PI * radius * radius);
        }

        Piece raise = ZNetScene.instance.GetPrefab(RaisePrefab)?.GetComponent<Piece>();
        TerrainOp raiseOp = raise != null ? raise.GetComponent<TerrainOp>() : null;
        if (raise != null && raiseOp != null && raise.m_resources.Length > 0 && raiseOp.m_settings.m_raiseDelta > 0f)
        {
            float radius = Mathf.Max(0.5f, raiseOp.m_settings.m_raiseRadius);
            float volume = Mathf.PI * radius * radius * raiseOp.m_settings.m_raiseDelta;
            raisePerCubicMetre = raise.m_resources[0].m_amount / volume;
        }

        Jotunn.Logger.LogInfo($"Terrain tools: {pavedPerSquareMetre:0.###} stone per m² paved, {raisePerCubicMetre:0.###} per m³ raised");
    }
}
