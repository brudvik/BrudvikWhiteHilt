using BepInEx.Bootstrap;
using BrudvikWhiteHilt.Helpers;
using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Planting;

/// <summary>
/// Lets the cultivator plant vanilla berry bushes, mushrooms, flowers, debris and decorative flora. The vanilla prefabs get a
/// <see cref="Piece"/> and join the cultivator's piece table; growth, respawn times and yields stay vanilla. Clients without
/// the mod still see and pick what was planted, since the prefabs are vanilla.
/// </summary>
public static class Plantables
{
    private const string PlantEverythingGuid = "advize.PlantEverything";
    private const string CultivatorPrefab = "Cultivator";
    private const string PlaceEffectTemplate = "sapling_carrot";
    private const string VinesPrefab = "vines";
    private const string SnapPointName = "whitehilt_snappoint";
    private const float MaxRayDistance = 50f;

    private static readonly Vector3[] vineSnapPoints = { new(1f, 0.5f, 0f), new(-1f, 0.5f, 0f), new(1f, -1f, 0f), new(-1f, -1f, 0f) };
    private static readonly Dictionary<string, PlantableDefinition> definitions = PlantingSettings.Definitions.ToDictionary(d => d.Prefab);
    private static readonly Dictionary<string, Piece> pieces = new();
    private static readonly HashSet<string> warned = new();

    private static bool? otherModInstalled;
    private static int removeMask;

    /// <summary>
    /// Adds the pieces to the vanilla prefabs and puts the enabled ones in the cultivator. Safe to call again: after every
    /// ObjectDB load and whenever the config changes.
    /// </summary>
    public static void Apply()
    {
        if (ObjectDB.instance == null || ZNetScene.instance == null || OtherModInstalled())
        {
            return;
        }

        PieceTable table = CultivatorTable();
        if (table == null)
        {
            return;
        }

        Piece template = ZNetScene.instance.GetPrefab(PlaceEffectTemplate)?.GetComponent<Piece>();
        bool active = PlantingSettings.Enabled.Value;
        bool changed = false;
        foreach (PlantableDefinition definition in PlantingSettings.Definitions)
        {
            // Our sapling clones reach ZNetScene only when it next wakes up.
            GameObject prefab = ZNetScene.instance.GetPrefab(definition.Prefab)
                ?? (definition is SaplingDefinition ? PrefabManager.Instance.GetPrefab(definition.Prefab) : null);
            Piece piece = prefab != null ? Prepare(definition, prefab, template) : null;
            if (piece == null)
            {
                WarnOnce(definition.Prefab, prefab == null ? "is not in the game" : "could not be given a piece");
                continue;
            }

            Configure(definition, piece);
            bool ready = definition is not SaplingDefinition sapling || Saplings.Configure(sapling, piece);
            bool wanted = active && ready && KindEnabled(definition) && SetRequirements(definition, piece);
            changed |= SetInTable(table, prefab, wanted);
        }

        if (changed && Player.m_localPlayer != null)
        {
            Player.m_localPlayer.UpdateAvailablePiecesList();
        }
    }

    /// <summary>
    /// True for a plantable that grows wild, i.e. was not placed by a player.
    /// </summary>
    /// <param name="piece">The piece.</param>
    /// <returns>True if so.</returns>
    public static bool IsWild(Piece piece)
    {
        return piece != null && pieces.ContainsKey(Utils.GetPrefabName(piece.gameObject)) && !piece.IsPlacedByPlayer();
    }

    /// <summary>
    /// Removes the plantable the player aims at, if they hold a cultivator. Vanilla removal is kept away from these pieces,
    /// so the hammer never removes a wild bush.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="removed">True if something was removed.</param>
    /// <returns>True if this handled the removal, false to let vanilla handle it.</returns>
    public static bool TryRemove(Player player, out bool removed)
    {
        removed = false;
        if (pieces.Count == 0 || !PlantingSettings.Enabled.Value || OtherModInstalled() || GameCamera.instance == null)
        {
            return false;
        }

        PieceTable table = CultivatorTable();
        if (table == null || player.GetRightItem()?.m_shared.m_buildPieces != table)
        {
            return false;
        }

        if (removeMask == 0)
        {
            removeMask = LayerMask.GetMask("item", "piece_nonsolid", "Default_small", "Default", "static_solid", "piece", "terrain");
        }

        Transform view = GameCamera.instance.transform;
        if (!Physics.Raycast(view.position, view.forward, out RaycastHit hit, MaxRayDistance, removeMask)
            || Vector3.Distance(hit.point, player.m_eye.position) >= player.m_maxPlaceDistance)
        {
            return false;
        }

        Piece piece = hit.collider.GetComponentInParent<Piece>();
        if (piece == null || !pieces.ContainsKey(Utils.GetPrefabName(piece.gameObject))
            || !definitions.TryGetValue(Utils.GetPrefabName(piece.gameObject), out PlantableDefinition definition)
            || definition.Kind == PlantableKind.Sapling)
        {
            return false;
        }

        bool planted = piece.IsPlacedByPlayer();
        bool allowed = definition.Kind == PlantableKind.Flora ? planted && PlantingSettings.RemoveFlora.Value : planted || PlantingSettings.RemoveWild.Value;
        if (!allowed)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_plant_cantremove");
            return true;
        }

        if (Location.IsInsideNoBuildLocation(piece.transform.position))
        {
            player.Message(MessageHud.MessageType.Center, "$msg_nobuildzone");
            return true;
        }

        if (!PrivateArea.CheckAccess(piece.transform.position))
        {
            player.Message(MessageHud.MessageType.Center, "$msg_privatezone");
            return true;
        }

        ZNetView nview = piece.m_nview;
        if (nview == null || !nview.IsValid())
        {
            return true;
        }

        nview.ClaimOwnership();
        Pickable pickable = piece.GetComponent<Pickable>();
        if (planted && PlantingSettings.RecoverResources.Value)
        {
            // What it holds stays unpicked, or planting and removing again would make berries out of nothing.
            piece.DropResources();
        }
        else if (pickable != null && !pickable.m_picked)
        {
            pickable.RPC_Pick(0L, 0);
        }

        piece.m_placeEffect.Create(piece.transform.position, piece.transform.rotation);
        player.m_removeEffects.Create(piece.transform.position, Quaternion.identity);
        ZNetScene.instance.Destroy(piece.gameObject);
        player.FaceLookDirection();
        player.m_zanim.SetTrigger(player.GetRightItem().m_shared.m_attack.m_attackAnimation);
        removed = true;
        return true;
    }

    // The piece that plants a plantable: its own piece for saplings, otherwise one added to the vanilla prefab, with
    // the name, category and placement rules of planting.
    private static Piece Prepare(PlantableDefinition definition, GameObject prefab, Piece template)
    {
        if (pieces.TryGetValue(definition.Prefab, out Piece piece) && piece != null)
        {
            return piece;
        }

        // Saplings are our own birch clones and keep the birch sapling's piece.
        bool clone = definition is SaplingDefinition;
        piece = prefab.GetComponent<Piece>();
        if (clone ? piece == null : piece != null || prefab.GetComponent<ZNetView>() == null)
        {
            return null;
        }

        if (piece == null)
        {
            piece = prefab.AddComponent<Piece>();
        }

        piece.m_name = Translations.Token(definition.NameKey);
        piece.m_description = Translations.Token($"piece_whitehilt_plant_{definition.Kind.ToString().ToLowerInvariant()}_description");
        pieces[definition.Prefab] = piece;
        if (clone)
        {
            piece.m_icon = null;
            return piece;
        }

        piece.m_category = Piece.PieceCategory.Misc;
        piece.m_canBeRemoved = false;
        piece.m_targetNonPlayerBuilt = false;
        piece.m_groundOnly = definition.Grounded;
        piece.m_groundPiece = definition.Grounded;
        if (template != null)
        {
            piece.m_placeEffect = template.m_placeEffect;
        }

        return piece;
    }

    private static void Configure(PlantableDefinition definition, Piece piece)
    {
        if (definition.Kind == PlantableKind.Sapling)
        {
            return;
        }

        piece.m_cultivatedGroundOnly = definition.Kind != PlantableKind.Flora && PlantingSettings.RequireCultivation.Value;
        piece.m_randomTarget = PlantingSettings.EnemiesTarget.Value;
        if (definition.Prefab == VinesPrefab)
        {
            SetSnapPoints(piece.gameObject, PlantingSettings.SnappableVines.Value);
        }
    }

    // What planting costs: the item the plant gives, and an extra item for some. Without a known item it cannot be
    // planted.
    private static bool SetRequirements(PlantableDefinition definition, Piece piece)
    {
        int cost = definition.Cost.Value;
        ItemDrop resource = cost > 0 ? FindResource(definition, piece.gameObject) : null;
        if (resource == null)
        {
            if (cost > 0)
            {
                WarnOnce(definition.Prefab, "has no known resource to cost");
            }

            return false;
        }

        bool recover = PlantingSettings.RecoverResources.Value;
        List<Piece.Requirement> requirements = new() { new Piece.Requirement { m_resItem = resource, m_amount = cost, m_recover = recover } };
        ItemDrop extra = definition.ExtraResource != null ? ObjectDB.instance.GetItemPrefab(definition.ExtraResource)?.GetComponent<ItemDrop>() : null;
        if (extra != null)
        {
            requirements.Add(new Piece.Requirement { m_resItem = extra, m_amount = definition.ExtraAmount, m_recover = recover });
        }

        piece.m_resources = requirements.ToArray();
        if (piece.m_icon == null)
        {
            piece.m_icon = VisualHelper.RenderIcon(piece.gameObject) ?? resource.m_itemData.GetIcon();
        }

        return true;
    }

    private static ItemDrop FindResource(PlantableDefinition definition, GameObject prefab)
    {
        string name = definition.Resource ?? prefab.GetComponent<Pickable>()?.m_itemPrefab?.name;
        return name != null ? ObjectDB.instance.GetItemPrefab(name)?.GetComponent<ItemDrop>() : null;
    }

    private static bool SetInTable(PieceTable table, GameObject prefab, bool wanted)
    {
        bool present = table.m_pieces.Contains(prefab);
        if (wanted && !present)
        {
            table.m_pieces.Add(prefab);
            return true;
        }

        if (!wanted && present)
        {
            table.m_pieces.Remove(prefab);
            return true;
        }

        return false;
    }

    // Gives vines snap points so they can be planted in rows, or takes them away.
    private static void SetSnapPoints(GameObject prefab, bool wanted)
    {
        List<Transform> existing = prefab.transform.Cast<Transform>().Where(child => child.name == SnapPointName).ToList();
        if (!wanted)
        {
            existing.ForEach(child => Object.DestroyImmediate(child.gameObject));
            return;
        }

        if (existing.Count > 0)
        {
            return;
        }

        foreach (Vector3 position in vineSnapPoints)
        {
            GameObject point = new(SnapPointName) { tag = "snappoint" };
            point.SetActive(false);
            point.transform.SetParent(prefab.transform, false);
            point.transform.localPosition = position;
        }
    }

    private static bool KindEnabled(PlantableDefinition definition)
    {
        return definition switch
        {
            SaplingDefinition sapling => sapling.Enabled.Value,
            _ when definition.Kind == PlantableKind.Debris => PlantingSettings.Debris.Value,
            _ when definition.Kind == PlantableKind.Flora => PlantingSettings.Flora.Value,
            _ => true
        };
    }

    private static PieceTable CultivatorTable()
    {
        return ObjectDB.instance?.GetItemPrefab(CultivatorPrefab)?.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_buildPieces;
    }

    private static void WarnOnce(string prefab, string reason)
    {
        if (warned.Add(prefab))
        {
            Jotunn.Logger.LogWarning($"Planting: {prefab} {reason}, so the cultivator cannot plant it.");
        }
    }

    // Both would add a piece to the same vanilla prefabs. Checked in game, when every plugin has loaded.
    internal static bool OtherModInstalled()
    {
        if (!otherModInstalled.HasValue)
        {
            otherModInstalled = Chainloader.PluginInfos.ContainsKey(PlantEverythingGuid);
            if (otherModInstalled.Value)
            {
                Jotunn.Logger.LogWarning("PlantEverything is installed, so White Hilt leaves planting to it.");
            }
        }

        return otherModInstalled.Value;
    }
}
