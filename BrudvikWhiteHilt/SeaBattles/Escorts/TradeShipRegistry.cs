using BrudvikWhiteHilt.Helpers;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.SeaBattles.Escorts;

/// <summary>
/// Registers the trading ship: a clone of the vanilla longship, as the Nail Ship is, with a hull of its own health, a sail
/// that pulls a little less than the longship's, no way for the hammer to take it apart, a merchant's look and the
/// <see cref="TradeShip"/> that sails it. It is no piece in any table and its network object is not persistent, so a
/// trading ship nobody sails with can never stay in the saved world.
/// </summary>
public static class TradeShipRegistry
{
    /// <summary>Prefab name of the trading ship.</summary>
    public const string ShipName = "WhiteHilt_TradeShip";

    private const string VanillaShip = "VikingShip";

    private static readonly int shipHash = ShipName.GetStableHashCode();

    /// <summary>
    /// Whether a prefab hash (as the network data of an object carries it) is the trading ship.
    /// </summary>
    /// <param name="prefabHash">The stable hash of the prefab name.</param>
    /// <returns>True for the trading ship.</returns>
    public static bool IsTradeShip(int prefabHash)
    {
        return prefabHash == shipHash;
    }

    /// <summary>
    /// Registers the prefab once the vanilla prefabs can be cloned. Call from the plugin's Awake, after the Nail Ship's.
    /// </summary>
    public static void Initialize()
    {
        PrefabManager.OnVanillaPrefabsAvailable += Add;
    }

    // Clones the longship and changes what differs. A failure is logged and leaves the mod without trading ships, not
    // without the rest of the prefabs.
    private static void Add()
    {
        PrefabManager.OnVanillaPrefabsAvailable -= Add;
        try
        {
            GameObject prefab = PrefabManager.Instance.CreateClonedPrefab(ShipName, VanillaShip);
            SetUpPiece(prefab);
            SetUpHull(prefab);
            prefab.GetComponent<Ship>().m_sailForceFactor *= EscortSettings.SailForceFactor.Value;

            // A trading ship is an encounter, not a part of the world: left alone, the clone keeps the longship's
            // persistent ZDO, and a merchant nobody sails near would sit in the saved world for ever.
            prefab.GetComponent<ZNetView>().m_persistent = false;

            prefab.AddComponent<TradeShip>();
            TradeShipLook.Apply(prefab);
            PrefabManager.Instance.AddPrefab(new CustomPrefab(prefab, false));
            Jotunn.Logger.LogInfo("Trading Ship added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError("Trading Ship failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    // It is a piece of the vanilla longship, but in no piece table: the hammer must not take it apart for materials.
    private static void SetUpPiece(GameObject prefab)
    {
        Piece piece = prefab.GetComponent<Piece>();
        piece.m_name = Translations.Token("whitehilt_tradeship");
        piece.m_canBeRemoved = false;
        piece.m_resources = Array.Empty<Piece.Requirement>();
    }

    // The longship's hull resists pierce damage; the merchant's takes it in full, as a Nail Ship's does, so the raiders'
    // arrows and bolts can hurt it.
    private static void SetUpHull(GameObject prefab)
    {
        WearNTear hull = prefab.GetComponent<WearNTear>();
        hull.m_health = EscortSettings.HullHealth.Value;
        hull.m_damages.m_pierce = HitData.DamageModifier.Normal;
    }
}
