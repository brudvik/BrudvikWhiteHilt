using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Performance;

/// <summary>
/// Each machine's own switches for the fixes of small faults in Valheim itself: work it does again and again for
/// nothing, memory it throws away every frame, a freeze it causes on the hour, and three ways it loses what players did.
/// Section "Performance". None of them changes what the game does, only what it costs, except the three that keep
/// things from being lost. They are each machine's own: a fix that runs on the server is set in the server's config.
/// </summary>
public static class VanillaFixSettings
{
    private const string Section = "Performance";

    /// <summary>Writing one network package into another copies no bytes it does not have to.</summary>
    public static VanillaFix PackageWrite { get; private set; }

    /// <summary>Asking whether a ZDO value changed does not box the value.</summary>
    public static VanillaFix ZdoWrite { get; private set; }

    /// <summary>The hourly unloading of unused assets waits for a quiet moment.</summary>
    public static VanillaFix AssetUnload { get; private set; }

    /// <summary>The hotbar's key names are made once instead of every frame.</summary>
    public static VanillaFix HotbarKeys { get; private set; }

    /// <summary>The worn gear's modifiers are read without reflection.</summary>
    public static VanillaFix EquipmentModifiers { get; private set; }

    /// <summary>The distance test for the active area makes no array.</summary>
    public static VanillaFix ZoneDistance { get; private set; }

    /// <summary>An item's recipe is found by name instead of by searching all recipes.</summary>
    public static VanillaFix RecipeLookup { get; private set; }

    /// <summary>A status effect is found by name instead of by searching all status effects.</summary>
    public static VanillaFix StatusEffectLookup { get; private set; }

    /// <summary>Assets load at full speed while a loading screen hides the world.</summary>
    public static VanillaFix LoadingPriority { get; private set; }

    /// <summary>The server saves what players changed, not only what it changed itself.</summary>
    public static VanillaFix SaveClientChanges { get; private set; }

    /// <summary>The player who feeds a fire, smelter or kiln takes it over first, so the fuel or ore is not lost.</summary>
    public static VanillaFix FeedOwnership { get; private set; }

    /// <summary>An object destroyed behind the game's back no longer holds up a loading screen.</summary>
    public static VanillaFix DeadObjects { get; private set; }

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        PackageWrite = new VanillaFix(WhiteHiltConfig.BindLocal(Section, "PackageWrite", true,
            "When the game puts one network message inside another, as it does for the world data it sends each player, "
            + "write the inner bytes straight across instead of copying them to a new array first. What is sent is the "
            + "same, byte for byte. Matters most on a server."), "package write", "ZPackageWriteAllocPatch");
        ZdoWrite = new VanillaFix(WhiteHiltConfig.BindLocal(Section, "ZdoWrite", true,
            "When an object's network data is set, compare the old and new number as numbers instead of as objects, which "
            + "made a scrap of memory to clear up for every value set. The answer is the same."), "ZDO write", "ZdoValueWriteAllocPatch");
        AssetUnload = new VanillaFix(WhiteHiltConfig.BindLocal(Section, "AssetUnload", true,
            "Every hour the game unloads unused assets, which freezes it for a moment. On a dedicated server, wait until no "
            + "one has been on it for two minutes (at most 12 hours). On a client, wait for a loading screen, sleep or the "
            + "menu (at most 3 hours)."), "hourly asset unload", "HourlyAssetUnloadPatch");
        HotbarKeys = new VanillaFix(WhiteHiltConfig.BindLocal(Section, "HotbarKeys", true,
            "Name the hotbar's keys once instead of writing 16 new texts every frame to check them."), "hotbar key", "HotbarKeyAllocPatch");
        EquipmentModifiers = new VanillaFix(WhiteHiltConfig.BindLocal(Section, "EquipmentModifiers", true,
            "Add up the worn gear's movement and stamina modifiers by reading the values directly instead of through "
            + "reflection, which the game does up to 88 times per physics step."), "equipment modifier", "EquipmentModifierAllocPatch");
        ZoneDistance = new VanillaFix(WhiteHiltConfig.BindLocal(Section, "ZoneDistance", true,
            "Test whether an object is in the active area without making a new array each time. The game makes this test "
            + "for many objects every frame, a server for every object near every player."), "zone distance", "ChebyshevDistanceAllocPatch");
        RecipeLookup = new VanillaFix(WhiteHiltConfig.BindLocal(Section, "RecipeLookup", true,
            "Find an item's recipe by its name instead of searching all recipes, which the crafting window does for every "
            + "worn item every frame to light the repair button. Rebuilt when recipes are added."), "recipe lookup", "RecipeLookupPatch");
        StatusEffectLookup = new VanillaFix(WhiteHiltConfig.BindLocal(Section, "StatusEffectLookup", true,
            "Find a status effect by its name instead of searching all of them each time one is added. Rebuilt when status "
            + "effects are added."), "status effect lookup", "ObjectDBStatusEffectPatch");
        LoadingPriority = new VanillaFix(WhiteHiltConfig.BindLocal(Section, "LoadingPriority", true,
            "While the screen is black for a portal or a respawn, let Unity load assets at full speed. In the world it "
            + "keeps the game's low priority, which keeps loading from stealing frame time."), "loading priority", null);
        SaveClientChanges = new VanillaFix(WhiteHiltConfig.BindLocal(Section, "SaveClientChanges", true,
            "Server: mark what a player built or changed for the next world save. The game saves only the parts of the "
            + "world it marked as changed, and it never marks a change that comes from a player, so a chest a player "
            + "filled could lose its contents when the server restarted."), "unsaved client changes", "ReceivedZdoDirtyPatch");
        FeedOwnership = new VanillaFix(WhiteHiltConfig.BindLocal(Section, "FeedOwnership", true,
            "Take over a fire, smelter, kiln or oven before putting fuel or ore in it. The game takes the item from you "
            + "and asks the player who runs the object to add it; when that player has just left or handed it on, the item "
            + "was lost."), "fuel and ore loss", "FuelLossPatch");
        DeadObjects = new VanillaFix(WhiteHiltConfig.BindLocal(Section, "DeadObjects", true,
            "Forget an object that was destroyed without the game being told, such as the coin effect when another player "
            + "loots an infested mine's treasure, so it is made again. The game counted it as not yet loaded, and a portal "
            + "or respawn next to it waited forever on the loading screen."), "loading screen hang", "DeadInstancePatch");
    }

    /// <summary>
    /// Works out again which fixes run, now that every plugin has loaded or a world has started. Valheim Community
    /// Patch decides some of its fixes only then.
    /// </summary>
    public static void Refresh()
    {
        if (PackageWrite == null)
        {
            return;
        }

        PackageWrite.Refresh();
        ZdoWrite.Refresh();
        AssetUnload.Refresh();
        HotbarKeys.Refresh();
        EquipmentModifiers.Refresh();
        ZoneDistance.Refresh();
        RecipeLookup.Refresh();
        StatusEffectLookup.Refresh();
        LoadingPriority.Refresh();
        SaveClientChanges.Refresh();
        FeedOwnership.Refresh();
        DeadObjects.Refresh();
    }
}
