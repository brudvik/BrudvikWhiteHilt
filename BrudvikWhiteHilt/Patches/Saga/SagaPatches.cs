using BrudvikWhiteHilt.Saga;
using HarmonyLib;

namespace BrudvikWhiteHilt.Patches.Saga;

/// <summary>
/// Feeds the saga: slain foes, new biomes and skill milestones, and adds the rewards of renown.
/// </summary>
[HarmonyPatch]
public static class SagaPatches
{
    /// <summary>
    /// Registers the deed RPC.
    /// </summary>
    [HarmonyPatch(typeof(Game), nameof(Game.Start))]
    [HarmonyPostfix]
    public static void GameStart()
    {
        SagaLog.RegisterRpc();
    }

    /// <summary>
    /// A slain boss, black beast or listed creature is a deed for everyone near.
    /// </summary>
    /// <param name="__instance">The character that died.</param>
    [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
    [HarmonyPrefix]
    public static void OnDeath(Character __instance)
    {
        if (__instance.m_nview != null && __instance.m_nview.IsValid() && __instance.m_nview.IsOwner())
        {
            SagaLog.OnDeath(__instance);
        }
    }

    /// <summary>
    /// Remembers whether a biome is new before the game adds it.
    /// </summary>
    /// <param name="__instance">The player.</param>
    /// <param name="biome">The biome.</param>
    /// <param name="__state">True if it was new.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.AddKnownBiome))]
    [HarmonyPrefix]
    public static void BeforeBiome(Player __instance, BiomeSector biome, out bool __state)
    {
        __state = !__instance.m_knownBiome.Contains(biome.GetName());
    }

    /// <summary>
    /// The first steps into a biome are a deed.
    /// </summary>
    /// <param name="__instance">The player.</param>
    /// <param name="biome">The biome.</param>
    /// <param name="__state">True if it was new.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.AddKnownBiome))]
    [HarmonyPostfix]
    public static void AfterBiome(Player __instance, BiomeSector biome, bool __state)
    {
        if (__state && biome.Biome != Heightmap.Biome.None && biome.Biome != Heightmap.Biome.Meadows)
        {
            SagaLog.OnBiome(__instance, biome.GetName());
        }
    }

    /// <summary>
    /// Remembers a skill's level before it is raised.
    /// </summary>
    /// <param name="__instance">The skills.</param>
    /// <param name="skillType">The skill.</param>
    /// <param name="__state">Its level before.</param>
    [HarmonyPatch(typeof(Skills), nameof(Skills.RaiseSkill))]
    [HarmonyPrefix]
    public static void BeforeRaise(Skills __instance, Skills.SkillType skillType, out float __state)
    {
        __state = __instance.GetSkillLevel(skillType);
    }

    /// <summary>
    /// A skill reaching 25, 50, 75 or 100 is a deed.
    /// </summary>
    /// <param name="__instance">The skills.</param>
    /// <param name="skillType">The skill.</param>
    /// <param name="__state">Its level before.</param>
    [HarmonyPatch(typeof(Skills), nameof(Skills.RaiseSkill))]
    [HarmonyPostfix]
    public static void AfterRaise(Skills __instance, Skills.SkillType skillType, float __state)
    {
        SagaLog.OnSkill(__instance.m_player, skillType, __state, __instance.GetSkillLevel(skillType));
    }

    /// <summary>
    /// Adds the carry weight of the player's rank.
    /// </summary>
    /// <param name="__instance">The player.</param>
    /// <param name="__result">The carry weight.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.GetMaxCarryWeight))]
    [HarmonyPostfix]
    public static void CarryWeight(Player __instance, ref float __result)
    {
        __result += SagaLog.Rank(__instance) * SagaSettings.CarryPerRank.Value;
    }

    /// <summary>
    /// Adds the stamina of the player's rank.
    /// </summary>
    /// <param name="__instance">The player.</param>
    /// <param name="stamina">Max stamina.</param>
    [HarmonyPatch(typeof(Player), nameof(Player.GetTotalFoodValue))]
    [HarmonyPostfix]
    public static void Stamina(Player __instance, ref float stamina)
    {
        stamina += SagaLog.Rank(__instance) * SagaSettings.StaminaPerRank.Value;
    }
}
