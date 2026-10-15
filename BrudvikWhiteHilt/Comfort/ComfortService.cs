using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Comfort;

/// <summary>
/// Comfort over a whole room: the reach of the local player (the book of the hall and Homecraft), the Book of the Hall
/// item, the Homecraft skill, and the list of comfort pieces the game counts, widened by the player's own reach and by
/// every Husvette whose room the player stands in. The game still counts one piece per comfort group.
/// </summary>
public static class ComfortService
{
    /// <summary>Prefab name of the Book of the Hall.</summary>
    public const string BookPrefabName = "WhiteHilt_HallBook";

    /// <summary>The player unique key set once the book is read.</summary>
    public const string BookKey = "whitehilt_hallbook";

    private const string SkillIdentifier = "com.jotunn.BrudvikWhiteHilt.homecraft";
    private const string SkillNameKey = "whitehilt_skill_homecraft";
    private const string BookSource = "LeatherScraps";
    private const float BookLength = 0.3f;
    private const float RestTick = 10f;

    // A point this close to the local player is the local player's own count.
    private const float OwnPointRange = 0.5f;

    private static readonly List<Piece> pieces = new();
    private static readonly List<Piece> roomPieces = new();
    private static SkillConfig skillConfig;
    private static float restedFor;

    /// <summary>The Homecraft skill type, valid after <see cref="Initialize"/>.</summary>
    public static Skills.SkillType Homecraft { get; private set; }

    /// <summary>
    /// Binds the settings, registers the skill, the book and the texts. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        ComfortSettings.Initialize();
        HusvetteVoice.RegisterTranslations();
        Translations.AddEnglishNameAndDescription(SkillNameKey, "Homecraft",
            "Making a home. Your own reach of comfort grows with it, so the pieces of a larger room count for you.");
        skillConfig = new SkillConfig
        {
            Identifier = SkillIdentifier,
            Name = Translations.Token(SkillNameKey),
            Description = Translations.Token($"{SkillNameKey}_description"),
            IncreaseStep = 1f
        };
        Homecraft = SkillManager.Instance.AddSkill(skillConfig);

        Translations.AddEnglishNameAndDescription(Translations.ItemKey(BookPrefabName), "Book of the Hall",
            "How a great hall is furnished so that its comfort reaches every bench. Read it once: your own reach of comfort grows by {0} m.");
        Translations.AddEnglish("msg_whitehilt_hallbook_read", "You know how a hall is furnished: your comfort reaches $1 m.");
        Translations.AddEnglish("msg_whitehilt_hallbook_known", "You already know this book.");
        PrefabManager.OnVanillaPrefabsAvailable += AddBook;
        PrefabManager.OnPrefabsRegistered += AddToHildir;
    }

    /// <summary>
    /// Gives the skill the chair's icon. Must happen before the first player spawns.
    /// </summary>
    public static void SetSkillIcon()
    {
        Sprite icon = PrefabManager.Instance.GetPrefab("piece_chair")?.GetComponent<Piece>()?.m_icon;
        if (skillConfig != null && skillConfig.Icon == null && icon != null)
        {
            skillConfig.Icon = icon;
        }
    }

    /// <summary>
    /// The local player's own reach of comfort, in metres.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The reach.</returns>
    public static float PersonalReach(Player player)
    {
        if (player == null || !ComfortSettings.Enabled.Value)
        {
            return ComfortReach.GameReach;
        }

        return ComfortReach.Personal(player.HaveUniqueKey(BookKey), player.GetSkillLevel(Homecraft), ComfortSettings.BookBonus.Value,
            ComfortSettings.SkillBonus.Value, ComfortSettings.PersonalMax.Value);
    }

    /// <summary>
    /// The comfort pieces the game counts at a point: those within the reach of the player standing there, and those within
    /// every Husvette whose reach the point is in. The game only asks for a player under a roof.
    /// </summary>
    /// <param name="point">Where the player stands.</param>
    /// <returns>The pieces, in a list the caller may sort.</returns>
    public static List<Piece> NearbyComfortPieces(Vector3 point)
    {
        pieces.Clear();
        Player player = Player.m_localPlayer;
        float reach = player != null && Vector3.Distance(player.transform.position, point) < OwnPointRange ? PersonalReach(player) : ComfortReach.GameReach;
        Piece.GetAllComfortPiecesInRadius(point, reach, pieces);
        if (!ComfortSettings.Enabled.Value)
        {
            return pieces;
        }

        float radius = ComfortSettings.HusvetteRadius.Value;
        foreach (Husvette husvette in Husvette.Loaded)
        {
            if (husvette == null || !husvette.IsBuilt || Vector3.Distance(husvette.transform.position, point) > radius)
            {
                continue;
            }

            roomPieces.Clear();
            Piece.GetAllComfortPiecesInRadius(husvette.transform.position, radius, roomPieces);
            foreach (Piece piece in roomPieces)
            {
                if (!pieces.Contains(piece))
                {
                    pieces.Add(piece);
                }
            }
        }

        return pieces;
    }

    /// <summary>
    /// Raises Homecraft while the local player rests: a little every ten seconds of resting.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="dt">Seconds since the last update.</param>
    public static void TickResting(Player player, float dt)
    {
        if (!ComfortSettings.Enabled.Value || player == null || !player.GetSEMan().HaveStatusEffect(SEMan.s_statusEffectResting))
        {
            restedFor = 0f;
            return;
        }

        restedFor += dt;
        if (restedFor >= RestTick)
        {
            restedFor -= RestTick;
            player.RaiseSkill(Homecraft, ComfortSettings.RestExperience.Value);
        }
    }

    /// <summary>
    /// Raises Homecraft when the local player builds a piece that gives comfort.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="piece">The piece's prefab.</param>
    public static void Built(Player player, Piece piece)
    {
        if (ComfortSettings.Enabled.Value && player != null && piece != null && piece.m_comfort > 0)
        {
            player.RaiseSkill(Homecraft, piece.m_comfort * ComfortSettings.BuildExperience.Value);
        }
    }

    /// <summary>
    /// Reads the Book of the Hall: the player knows it from now on, and the book is used up.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="book">The book.</param>
    public static void Read(Player player, ItemDrop.ItemData book)
    {
        if (player.HaveUniqueKey(BookKey))
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_hallbook_known");
            return;
        }

        player.AddUniqueKey(BookKey);
        player.GetInventory().RemoveOneItem(book);
        player.Message(MessageHud.MessageType.Center,
            Localization.instance.Localize("$msg_whitehilt_hallbook_read", Mathf.RoundToInt(PersonalReach(player)).ToString()));
    }

    /// <summary>
    /// Whether an item is the Book of the Hall.
    /// </summary>
    /// <param name="item">The item, or null.</param>
    /// <returns>True for the book.</returns>
    public static bool IsBook(ItemDrop.ItemData item) => item?.m_dropPrefab != null && item.m_dropPrefab.name == BookPrefabName;

    /// <summary>
    /// The book as a store item, for the trading posts' traders.
    /// </summary>
    /// <returns>The trade item, or null when the book is missing or comfort is off.</returns>
    public static Trader.TradeItem TradeItem()
    {
        ItemDrop book = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(BookPrefabName)?.GetComponent<ItemDrop>() : null;
        return book == null || !ComfortSettings.Enabled.Value
            ? null
            : new Trader.TradeItem { m_prefab = book, m_stack = 1, m_tooltip = string.Empty, m_price = ComfortSettings.BookPrice.Value, m_buyPlayerEffects = new EffectList() };
    }

    // A book drawn as the treasure maps are, tinted a deep red, with the bonus in its description.
    private static void AddBook()
    {
        PrefabManager.OnVanillaPrefabsAvailable -= AddBook;
        try
        {
            ItemConfig config = new()
            {
                Name = Translations.Token(Translations.ItemKey(BookPrefabName)),
                Description = Translations.Token(Translations.ItemKey(BookPrefabName) + "_description")
            };
            CustomItem item = new(BookPrefabName, BookSource, config);
            ItemDrop.ItemData.SharedData shared = item.ItemDrop.m_itemData.m_shared;
            shared.m_weight = 0.5f;
            shared.m_maxStackSize = 1;
            shared.m_itemType = ItemDrop.ItemData.ItemType.Misc;
            shared.m_value = 0;
            shared.m_teleportable = true;
            Translations.AddDynamic(Translations.ItemKey(BookPrefabName) + "_description", () => new object[] { Mathf.RoundToInt(ComfortSettings.BookBonus.Value) });
            if (!VisualHelper.IsHeadless)
            {
                try
                {
                    VisualHelper.ReplaceMesh(item.ItemPrefab, ForagingAssets.LoadMesh("treasuremap"), ForagingAssets.LoadTexture("treasuremap_albedo"), size: BookLength);
                    VisualHelper.Tint(item.ItemPrefab, new Color(0.62f, 0.3f, 0.24f));
                    Sprite icon = VisualHelper.RenderIcon(item.ItemPrefab);
                    if (icon != null)
                    {
                        shared.m_icons = Enumerable.Repeat(icon, Mathf.Max(1, shared.m_icons?.Length ?? 0)).ToArray();
                    }
                }
                catch (Exception ex)
                {
                    Jotunn.Logger.LogWarning($"{BookPrefabName}: no custom look: {ex.Message}");
                }
            }

            ItemManager.Instance.AddItem(item);
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{BookPrefabName} failed to load: {ex.Message}");
        }
    }

    private static void AddToHildir()
    {
        Trader hildir = PrefabManager.Instance.GetPrefab("Hildir")?.GetComponent<Trader>();
        ItemDrop book = PrefabManager.Instance.GetPrefab(BookPrefabName)?.GetComponent<ItemDrop>();
        if (hildir == null || book == null || hildir.m_items.Any(entry => entry.m_prefab == book) || !ComfortSettings.Enabled.Value)
        {
            return;
        }

        hildir.m_items.Add(new Trader.TradeItem { m_prefab = book, m_stack = 1, m_tooltip = string.Empty, m_price = ComfortSettings.BookPrice.Value, m_buyPlayerEffects = new EffectList() });
    }
}
