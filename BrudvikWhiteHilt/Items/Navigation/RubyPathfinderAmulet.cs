using BrudvikWhiteHilt.Backpack;
using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Progression;
using Jotunn.Configs;
using Jotunn.Managers;

namespace BrudvikWhiteHilt.Items.Navigation;

/// <summary>
/// The Pathfinder's Ruby Amulet: the Pathfinder's Amulet with a ruby in the middle of the valknut. It does everything
/// the Pathfinder does, and while it is worn the player can set a target on the map that an arrow leads to
/// (<see cref="global::BrudvikWhiteHilt.Navigation.Waypoints.WaypointGuide"/>). Made from a Pathfinder's Amulet once
/// the
/// player has the Ruby Pathfinder milestone in Exploration.
/// </summary>
public class RubyPathfinderAmulet : PathfinderAmulet
{
    /// <summary>
    /// Prefab name of the ruby amulet.
    /// </summary>
    public const string RubyPrefabName = "WhiteHiltPathfinderRuby";

    private const string Description = "The Pathfinder's Amulet with a ruby set in the middle of the valknut. It does all the Pathfinder does, uncovering the map up to {0} m and letting Odin's ravens show you {1} m around, and it finds the way: Shift + click the large map to set a target, or a pin to head for it. An arrow at the top of the screen and on the minimap leads you there, and the ruby tells you when you walk away from it.";

    /// <inheritdoc/>
    public override ProgressionTier DefaultTier => ProgressionTier.Swamp;

    /// <inheritdoc/>
    public override string Id => RubyPrefabName;

    /// <inheritdoc/>
    public override string DisplayName => "Pathfinder's Ruby Amulet";

    /// <inheritdoc/>
    protected override string EnglishDescription => Description;

    /// <inheritdoc/>
    protected override string EffectKey => "se_whitehiltpathfinderruby";

    /// <inheritdoc/>
    protected override string EffectName => "Ruby Pathfinder";

    /// <inheritdoc/>
    protected override string EffectTooltip => "The map uncovers further around you, and the ruby leads you to the target you set on the map.";

    /// <inheritdoc/>
    protected override string Layout => "stifinner_rubin";

    /// <inheritdoc/>
    protected override RequirementConfig[] Requirements => new RequirementConfig[]
    {
        new() { Item = PrefabName, Amount = 1 },
        new() { Item = "Ruby", Amount = 3 },
        new() { Item = "Iron", Amount = 2 }
    };

    /// <summary>
    /// Creates the definition. The plugin does this for every such class by reflection when it starts; the game sees
    /// the result only once Add has run. Registers the English text.
    /// </summary>
    /// <param name="instance">The item manager.</param>
    public RubyPathfinderAmulet(ItemManager instance) : base(instance)
    {
    }

    /// <summary>
    /// Returns true if the player wears the ruby amulet, as trinket or in an extra accessory slot.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>True while it is worn.</returns>
    public static bool WearsRuby(Player player)
    {
        if (player == null)
        {
            return false;
        }

        string name = Translations.Token(Translations.ItemKey(RubyPrefabName));
        return (player.m_trinketItem != null && player.m_trinketItem.m_shared.m_name == name) || UtilitySlots.IsWornExtra(player, name);
    }
}
