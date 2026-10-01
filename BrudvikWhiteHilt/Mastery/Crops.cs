using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Farming;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Mastery;

/// <summary>
/// The Farming milestones on plants and crops: seed stars and the farmer's level go from the planted seed to the grown
/// crop, which can grow faster or into a giant.
/// </summary>
public static class Crops
{
    /// <summary>ZDO key with the stars of the seed a plant grew from.</summary>
    public const string SeedStarsKey = "whitehilt_seedstars";

    /// <summary>ZDO key with the Farming level of the player who planted.</summary>
    public const string FarmerKey = "whitehilt_farmer";

    /// <summary>ZDO key on a giant crop.</summary>
    public const string GiantKey = "whitehilt_giant";

    private const float GiantScale = 1.6f;

    private static readonly List<ZNetView> pending = new();
    private static int pendingFrame;

    /// <summary>
    /// Registers the English text. Call from the plugin's Awake.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglish("whitehilt_giant_crop", "Giant");
    }

    /// <summary>
    /// Notes the planter's level on a plant the local player just placed, and waits for the seed it is paid with.
    /// </summary>
    /// <param name="piece">The placed piece.</param>
    public static void OnPlanted(Piece piece)
    {
        Player player = Player.m_localPlayer;
        ZNetView nview = piece.GetComponent<ZNetView>();
        if (player == null || piece.GetComponent<Plant>() == null || nview == null || !nview.IsValid() || !nview.IsOwner())
        {
            return;
        }

        nview.GetZDO().Set(FarmerKey, Mathf.FloorToInt(player.GetSkillLevel(Skills.SkillType.Farming)));
        if (pendingFrame != Time.frameCount)
        {
            pending.Clear();
            pendingFrame = Time.frameCount;
        }

        pending.Add(nview);
    }

    /// <summary>
    /// Gives the plants placed this frame the stars of the seeds just paid.
    /// </summary>
    /// <param name="stars">Average stars of the seeds.</param>
    public static void OnSeedsPaid(int stars)
    {
        if (pendingFrame == Time.frameCount && stars > 0)
        {
            foreach (ZNetView nview in pending)
            {
                if (nview != null && nview.IsValid() && nview.IsOwner())
                {
                    nview.GetZDO().Set(SeedStarsKey, stars);
                }
            }
        }

        pending.Clear();
    }

    /// <summary>
    /// Carries a plant's seed stars and planter to the crop it grew into, and maybe makes it a giant.
    /// </summary>
    /// <param name="seedStars">Stars of the seed.</param>
    /// <param name="farmer">The planter's Farming level.</param>
    /// <param name="grown">The grown crop.</param>
    public static void OnGrown(int seedStars, int farmer, GameObject grown)
    {
        ZNetView nview = grown != null ? grown.GetComponent<ZNetView>() : null;
        if (nview == null || !nview.IsValid() || !nview.IsOwner() || grown.GetComponent<Pickable>() == null)
        {
            return;
        }

        ZDO zdo = nview.GetZDO();
        zdo.Set(SeedStarsKey, seedStars);
        zdo.Set(FarmerKey, farmer);
        if (Perks.GiantCrops.ReachedAt(farmer) && Random.value < MasterySettings.GiantCropChance.Value)
        {
            zdo.Set(GiantKey, true);
            nview.SetLocalScale(grown.transform.localScale * GiantScale);
        }
    }

    /// <summary>
    /// How long a plant takes to grow, as a share of vanilla: faster for a green thumb and near compost.
    /// </summary>
    /// <param name="plant">The plant.</param>
    /// <returns>The multiplier.</returns>
    public static float GrowTimeFactor(Plant plant)
    {
        ZDO zdo = plant.m_nview != null && plant.m_nview.IsValid() ? plant.m_nview.GetZDO() : null;
        float factor = 1f;
        if (zdo != null && Perks.GreenThumb.ReachedAt(zdo.GetInt(FarmerKey)))
        {
            factor *= 1f - MasterySettings.GreenThumbBonus.Value;
        }

        return factor * CompostBinComponent.GrowTimeFactor(plant.transform.position);
    }

    /// <summary>
    /// Sets up the stars for a crop being picked, and returns the extra items of a giant.
    /// </summary>
    /// <param name="pickable">The crop, owned here.</param>
    /// <param name="picker">The player picking.</param>
    /// <returns>Extra items.</returns>
    public static int PrepareHarvest(Pickable pickable, Player picker)
    {
        ZDO zdo = pickable.m_nview.GetZDO();
        float level = SkillLevels.Get(picker, Skills.SkillType.Farming);
        int seedStars = zdo.GetInt(SeedStarsKey);
        int max = Stars.MaxAt(level, Perks.StarredCrops, Perks.GreenThumb, Perks.MasterFarmer);
        if (max > 0)
        {
            float bonus = seedStars * MasterySettings.SeedStarBonus.Value;
            Stars.DropStars = _ => Stars.Roll(level, max, 1f, bonus);
        }

        return zdo.GetBool(GiantKey) ? pickable.m_amount * (MasterySettings.GiantCropYield.Value - 1) : 0;
    }

    /// <summary>
    /// True if the crop is a giant.
    /// </summary>
    /// <param name="pickable">The crop.</param>
    /// <returns>True for a giant.</returns>
    public static bool IsGiant(Pickable pickable)
    {
        return pickable.m_nview != null && pickable.m_nview.IsValid() && pickable.m_nview.GetZDO().GetBool(GiantKey);
    }
}
