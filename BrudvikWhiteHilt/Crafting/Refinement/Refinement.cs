using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Crafting.Refinement;

/// <summary>
/// The Forge of Potential under White Hilt's rules (<see cref="RefinementSettings"/>). The game rolls one number per try
/// and compares it with the idol's chances (<c>InventoryGui.DoCrafting</c>); here the outcome is decided first, by a
/// true roll, the player's skills, the failed tries and any extra idols, and the game is then steered to it: the idol's
/// chances are set for the one try so the game's own branch runs, with its effects, messages and returned materials.
/// </summary>
public static class Refinement
{
    private const string FailuresKeyPrefix = "whitehilt_refine_";

    private static readonly System.Random random = new();

    private static Attempt attempt;
    private static bool offerExtra;

    /// <summary>
    /// Remembers whether Shift was held as the refinement was begun, which offers extra idols.
    /// </summary>
    /// <param name="gui">The inventory screen.</param>
    public static void OnCraftPressed(InventoryGui gui)
    {
        offerExtra = gui.m_craftUpgradeItem != null && (ZInput.GetButton("AltPlace") || ZInput.GetButton("JoyLStick"));
    }

    /// <summary>
    /// Decides the try the game is about to make and returns the roll that leads the game to it. Runs in place of the
    /// game's own roll, which is kept only with <see cref="RefinementRoll.Vanilla"/>.
    /// </summary>
    /// <param name="player">The player whose roll it is.</param>
    /// <param name="vanillaRoll">The game's roll.</param>
    /// <returns>The roll the game goes on with.</returns>
    public static float Steer(Player player, float vanillaRoll)
    {
        Restore();
        InventoryGui gui = InventoryGui.instance;
        if (!RefinementSettings.Enabled.Value || player == null || player != Player.m_localPlayer || gui == null)
        {
            return vanillaRoll;
        }

        Recipe recipe = gui.m_craftRecipe;
        ItemDrop.ItemData item = gui.m_craftUpgradeItem;
        Piece.Requirement idol = recipe?.m_resources?.FirstOrDefault(requirement => requirement.m_upgraderResource && requirement.m_resItem != null);
        if (item == null || idol == null || recipe.m_item == null)
        {
            return vanillaRoll;
        }

        int level = item.m_quality + 1;
        ItemDrop.ItemData.SharedData idolData = idol.m_resItem.m_itemData.m_shared;
        string key = FailuresKey(recipe.m_item.gameObject.name, level);
        int failures = RefinementSettings.RollMode.Value == RefinementRoll.FairPity ? Failures(player, key) : 0;
        int extra = offerExtra ? ExtraIdols(player, idolData.m_name, idol.GetAmount(level)) : 0;
        RefinementSkills skills = SkillsOf(player, item);
        RefinementBonuses bonuses = RefinementSettings.Bonuses;
        float success = RefinementOdds.SuccessChance(idolData.m_upgradeChance, skills, failures, extra, bonuses);
        float breaking = RefinementOdds.BreakChance(idolData.m_breakChance, skills, bonuses);
        double roll = RefinementSettings.RollMode.Value == RefinementRoll.Vanilla ? vanillaRoll : random.NextDouble();
        RefinementOutcome outcome = RefinementOdds.Decide(roll, success, breaking, RefinementSettings.FailureMode.Value);
        Jotunn.Logger.LogInfo($"Refinement: {recipe.m_item.gameObject.name} to level {level}, chance {success:0.00} (break {breaking:0.00}, " +
            $"{failures} failed, {extra} extra idols), roll {roll:0.00}: {outcome}");

        attempt = new Attempt(idolData, outcome, level, item.m_shared.m_name, recipe.m_item.gameObject.name, item.m_gridPos, extra, key);

        // The game succeeds when the idol's chance reaches the roll, else breaks the item when its break chance does.
        idolData.m_upgradeChance = outcome == RefinementOutcome.Success ? 1f : 0f;
        idolData.m_breakChance = outcome == RefinementOutcome.Break ? 1f : 0f;
        return 0.5f;
    }

    /// <summary>
    /// After the game's try: gives the item its level back when it should keep it, counts the failed tries, takes the
    /// extra idols and says what happened where the game's words do not fit.
    /// </summary>
    /// <param name="player">The player who refined.</param>
    public static void Finish(Player player)
    {
        Attempt done = attempt;
        if (done == null || player == null)
        {
            return;
        }

        Inventory inventory = player.GetInventory();
        if (done.Outcome == RefinementOutcome.Success)
        {
            player.m_customData.Remove(done.FailuresKey);
        }
        else
        {
            player.m_customData[done.FailuresKey] = (Failures(player, done.FailuresKey) + 1).ToString(CultureInfo.InvariantCulture);
        }

        if (done.Extra > 0)
        {
            inventory.RemoveItem(done.Idol.m_name, done.Extra);
            player.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize("$msg_whitehilt_refine_extra", done.Extra.ToString()));
        }

        switch (done.Outcome)
        {
            case RefinementOutcome.KeepLevel:
                // The game put the item back a level down, where it lay.
                ItemDrop.ItemData item = inventory.GetItemAt(done.Position.x, done.Position.y);
                if (item != null && item.m_dropPrefab != null && item.m_dropPrefab.name == done.Prefab && item.m_quality == done.Level - 2)
                {
                    item.m_quality = done.Level - 1;
                    item.m_durability = item.GetMaxDurability();
                }
                else
                {
                    Jotunn.Logger.LogWarning($"Refinement: could not find {done.Prefab} to give its level back");
                }

                player.Message(MessageHud.MessageType.Center,
                    Localization.instance.Localize("$msg_whitehilt_refine_kept", done.ItemName, (done.Level - 1).ToString()));
                break;
            case RefinementOutcome.Downgrade:
                player.Message(MessageHud.MessageType.Center,
                    Localization.instance.Localize("$msg_whitehilt_refine_downgraded", done.ItemName, (done.Level - 2).ToString()));
                break;
        }
    }

    /// <summary>
    /// Gives the idol back its own chances after the try, however it ended.
    /// </summary>
    public static void Restore()
    {
        if (attempt != null)
        {
            attempt.Idol.m_upgradeChance = attempt.UpgradeChance;
            attempt.Idol.m_breakChance = attempt.BreakChance;
            attempt = null;
        }
    }

    /// <summary>
    /// Ends the try begun with <see cref="OnCraftPressed"/>, after the game has made it or given up.
    /// </summary>
    public static void EndCraft()
    {
        Restore();
        offerExtra = false;
    }

    /// <summary>
    /// The line the forge's recipe shows on how to offer extra idols, or null when none can be offered.
    /// </summary>
    /// <param name="station">The station the player stands at.</param>
    /// <returns>The text, or null.</returns>
    public static string ExtraIdolHint(CraftingStation station)
    {
        if (station == null || !station.m_upgrader || !RefinementSettings.Enabled.Value || RefinementSettings.MaxExtraIdols.Value <= 0
            || RefinementSettings.ExtraIdolBonus.Value <= 0f)
        {
            return null;
        }

        return Localization.instance.Localize("$whitehilt_refine_extra_hint", RefinementSettings.MaxExtraIdols.Value.ToString(),
            Mathf.RoundToInt(RefinementSettings.ExtraIdolBonus.Value * 100f).ToString());
    }

    private static string FailuresKey(string prefab, int level)
    {
        return $"{FailuresKeyPrefix}{prefab}_{level}";
    }

    // Failed tries on an item and level since the last success, kept with the character.
    private static int Failures(Player player, string key)
    {
        return player.m_customData.TryGetValue(key, out string text) && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int count)
            ? Mathf.Max(0, count)
            : 0;
    }

    // Idols beyond what the recipe takes that the player carries, up to the most one try may take.
    private static int ExtraIdols(Player player, string idolName, int needed)
    {
        int carried = player.GetInventory().CountItems(idolName);
        return Mathf.Clamp(carried - needed, 0, RefinementSettings.MaxExtraIdols.Value);
    }

    // Crafting, the item's own skill (or, for armour, the mean of all), and the mean of all the player's skills.
    private static RefinementSkills SkillsOf(Player player, ItemDrop.ItemData item)
    {
        global::Skills skills = player.GetSkills();
        List<global::Skills.Skill> all = skills.GetSkillList();
        float overall = all.Count > 0 ? all.Average(skill => skill.m_level) / 100f : 0f;
        global::Skills.SkillType own = item.m_shared.m_skillType;
        float weapon = own != global::Skills.SkillType.None ? skills.GetSkillFactor(own) : overall;
        return new RefinementSkills(skills.GetSkillFactor(global::Skills.SkillType.Crafting), weapon, overall);
    }

    private sealed class Attempt
    {
        public Attempt(ItemDrop.ItemData.SharedData idol, RefinementOutcome outcome, int level, string itemName, string prefab, Vector2i position,
            int extra, string failuresKey)
        {
            Idol = idol;
            UpgradeChance = idol.m_upgradeChance;
            BreakChance = idol.m_breakChance;
            Outcome = outcome;
            Level = level;
            ItemName = itemName;
            Prefab = prefab;
            Position = position;
            Extra = extra;
            FailuresKey = failuresKey;
        }

        public ItemDrop.ItemData.SharedData Idol { get; }

        public float UpgradeChance { get; }

        public float BreakChance { get; }

        public RefinementOutcome Outcome { get; }

        // The level the try aimed at.
        public int Level { get; }

        public string ItemName { get; }

        public string Prefab { get; }

        public Vector2i Position { get; }

        public int Extra { get; }

        public string FailuresKey { get; }
    }
}
