using Jotunn.Managers;

namespace BrudvikWhiteHilt.Monsters;

/// <summary>
/// The trophies of the mod's own creatures drop as the game's own have since Valheim 1.0.20: each star raises the
/// chance (twice at one star, four times at two, the difficulty's loot multiplier above that), and a trophy still drops
/// one at a time. Without this the configured chance would hold at every star, and a starred monster of the mod would
/// be harder to get a trophy from than a vanilla one.
/// </summary>
public static class TrophyDrops
{
    private const string OwnPrefix = "WhiteHilt_";
    private const string Trophy = "Trophy";

    /// <summary>
    /// Hooks the pass over the creatures once every prefab is registered. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        PrefabManager.OnPrefabsRegistered += Align;
    }

    // Runs for every ZNetScene, so it must be idempotent: it only sets two flags.
    private static void Align()
    {
        if (ZNetScene.instance == null)
        {
            return;
        }

        int aligned = 0;
        foreach (UnityEngine.GameObject prefab in ZNetScene.instance.m_prefabs)
        {
            CharacterDrop drops = prefab != null && prefab.name.StartsWith(OwnPrefix, System.StringComparison.Ordinal) ? prefab.GetComponent<CharacterDrop>() : null;
            if (drops == null)
            {
                continue;
            }

            foreach (CharacterDrop.Drop drop in drops.m_drops)
            {
                if (drop.m_prefab != null && drop.m_prefab.name.Contains(Trophy))
                {
                    drop.m_levelMultiplier = true;
                    drop.m_levelMultiplierNotAmount = true;
                    aligned++;
                }
            }
        }

        Jotunn.Logger.LogInfo($"Trophies: {aligned} trophy drops of the mod's creatures scale with stars, as the game's own do.");
    }
}
