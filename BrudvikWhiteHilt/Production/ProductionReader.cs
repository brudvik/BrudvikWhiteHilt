using BrudvikWhiteHilt.Companions;
using BrudvikWhiteHilt.Mastery;
using BrudvikWhiteHilt.Patches.Ranching;
using BrudvikWhiteHilt.Pieces.EternalFire;
using BrudvikWhiteHilt.Ranching;
using System;
using UnityEngine;

namespace BrudvikWhiteHilt.Production;

/// <summary>
/// Reads what a station is doing from its ZDO, the same way vanilla counts: time left, fuel, and why it has stopped.
/// Only reads; works on every machine, owner or not.
/// </summary>
public static class ProductionReader
{
    private const double BurnWarningSeconds = 20.0;
    private const double FireLowSeconds = 300.0;
    private const int StatusDone = 1;
    private const int StatusBurnt = 2;

    private const string Green = "#9CFF9C";
    private const string Blue = "#9AD9FF";
    private const string Red = "#FF7A5C";
    private const string Orange = "#FFB84D";
    private const string Grey = "#BBBBBB";

    private static readonly string[] slotKeys = new string[16];
    private static readonly string[] statusKeys = new string[16];

    /// <summary>
    /// Reads a station.
    /// </summary>
    /// <param name="source">The station component.</param>
    /// <param name="status">Filled with what it is doing.</param>
    /// <param name="detailed">True for the hover text: also works out why an animal is not breeding.</param>
    /// <returns>False if it is not a station with a timer, is turned off in the config, or has no data yet.</returns>
    public static bool Read(Component source, ProductionStatus status, bool detailed)
    {
        if (source == null || ZNet.instance == null)
        {
            return false;
        }

        return source switch
        {
            Smelter smelter => ProductionSettings.IsOn(ProductionKind.Smelter) && ReadSmelter(smelter, status),
            Fermenter fermenter => ProductionSettings.IsOn(ProductionKind.Fermenter) && ReadFermenter(fermenter, status),
            CookingStation station => ProductionSettings.IsOn(ProductionKind.Cooking) && ReadCooking(station, status),
            Beehive hive => ProductionSettings.IsOn(ProductionKind.Beehive) && ReadBeehive(hive, status),
            SapCollector collector => ProductionSettings.IsOn(ProductionKind.SapCollector) && ReadSapCollector(collector, status),
            Fireplace fire => ProductionSettings.IsOn(ProductionKind.Fire) && ReadFire(fire, status),
            EggGrow egg => ProductionSettings.IsOn(ProductionKind.Egg) && ReadEgg(egg, status),
            Procreation animal => ProductionSettings.IsOn(ProductionKind.Animal) && ReadAnimal(animal, status, detailed),
            _ => false
        };
    }

    /// <summary>
    /// Real time as "45s", "3m 05s" or "2h 10m".
    /// </summary>
    /// <param name="seconds">Seconds.</param>
    /// <returns>The text.</returns>
    public static string Time(double seconds)
    {
        int total = Mathf.Max(0, Mathf.CeilToInt((float)seconds));
        if (total < 60)
        {
            return total + "s";
        }

        return total < 3600 ? $"{total / 60}m {total % 60:00}s" : $"{total / 3600}h {total / 60 % 60:00}m";
    }

    /// <summary>
    /// Localizes a token with {0}-style arguments, then any tokens inside the arguments.
    /// </summary>
    /// <param name="token">The token, with $.</param>
    /// <param name="args">The arguments.</param>
    /// <returns>The text.</returns>
    public static string Text(string token, params object[] args)
    {
        Localization localization = Localization.instance;
        return args.Length == 0 ? localization.Localize(token) : localization.Localize(string.Format(localization.Localize(token), args));
    }

    /// <summary>
    /// Wraps a text in a colour tag.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="color">Colour as #RRGGBB.</param>
    /// <returns>The coloured text.</returns>
    public static string Colored(string text, string color)
    {
        return $"<color={color}>{text}</color>";
    }

    private static bool ReadSmelter(Smelter smelter, ProductionStatus status)
    {
        if (!Valid(smelter.m_nview) || smelter.m_maxOre <= 0 || smelter.m_secPerProduct <= 0f)
        {
            return false;
        }

        status.Reset(smelter, ProductionKind.Smelter, smelter.m_name);
        ZDO zdo = smelter.m_nview.GetZDO();
        int queue = smelter.GetQueueSize();
        if (queue <= 0)
        {
            if (zdo.GetInt(ZDOVars.s_spawnAmount) > 0)
            {
                SetReady(status, "$whitehilt_prod_ready");
            }

            return true;
        }

        float power = smelter.m_windmill != null ? smelter.m_windmill.GetPowerOutput() : 1f;
        bool usesFuel = smelter.m_maxFuel > 0 && smelter.m_fuelItem != null && !EternalFireRules.Applies(smelter);
        string fuelName = smelter.m_fuelItem != null ? smelter.m_fuelItem.m_itemData.m_shared.m_name : string.Empty;
        float fuel = smelter.GetFuel();
        if (usesFuel && fuel <= 0f)
        {
            return Stop(status, Text("$whitehilt_prod_nofuel", fuelName));
        }

        if (smelter.m_requiresRoof && !smelter.m_haveRoof)
        {
            return Stop(status, Text("$piece_smelter_reqroof"));
        }

        if (smelter.m_blockedSmoke)
        {
            return Stop(status, Text("$whitehilt_prod_smoke"));
        }

        if (smelter.m_windmill != null && power <= 0f)
        {
            return Stop(status, Text("$whitehilt_prod_nowind"));
        }

        float per = smelter.m_secPerProduct;
        float bake = zdo.GetFloat(ZDOVars.s_bakeTimer);
        double next = Math.Max(0.0, (per - bake) / power);
        double all = next + (queue - 1) * per / power;
        status.State = ProductionState.Working;
        status.Seconds = all;
        status.Summary = Colored(queue > 1 ? $"{Time(next)} · {queue}" : Time(next), Green);
        status.Lines.Add(Colored(Text("$whitehilt_prod_next", Time(next)), Green));
        if (queue > 1)
        {
            status.Lines.Add(Colored(Text("$whitehilt_prod_all", Time(all), queue), Green));
        }

        if (smelter.m_windmill != null)
        {
            status.Lines.Add(Colored(Text("$whitehilt_prod_wind"), Grey));
        }

        if (usesFuel && smelter.m_fuelPerProduct > 0)
        {
            // Vanilla burns fuelPerProduct over secPerProduct seconds of smelting.
            float fuelPerSecond = smelter.m_fuelPerProduct / per;
            float needed = (queue * per - bake) * fuelPerSecond;
            if (fuel + 0.001f < needed)
            {
                double lasts = fuel / fuelPerSecond / power;
                status.Lines.Add(Colored(Text("$whitehilt_prod_fuel_short", Time(lasts), fuelName, Mathf.CeilToInt(needed - fuel)), Orange));
                status.Attention = true;
            }
            else
            {
                status.Lines.Add(Colored(Text("$whitehilt_prod_fuel_enough", fuelName), Green));
            }
        }

        return true;
    }

    private static bool ReadFermenter(Fermenter fermenter, ProductionStatus status)
    {
        if (!Valid(fermenter.m_nview))
        {
            return false;
        }

        status.Reset(fermenter, ProductionKind.Fermenter, fermenter.m_name);
        if (fermenter.GetContent() == 0)
        {
            return true;
        }

        double time = Math.Max(0.0, fermenter.GetFermentationTime());
        if (time > fermenter.m_fermentationDuration)
        {
            return SetReady(status, "$whitehilt_prod_ready");
        }

        if (!fermenter.m_hasRoof || fermenter.m_exposed)
        {
            Stop(status, Text(fermenter.m_hasRoof ? "$piece_fermenter_exposed" : "$piece_fermenter_needroof"));
            // Vanilla's hover text already names the reason.
            status.Lines.Clear();
            status.Lines.Add(Colored(Text("$whitehilt_prod_ferment_reset"), Orange));
            return true;
        }

        double left = fermenter.m_fermentationDuration - time;
        status.State = ProductionState.Working;
        status.Seconds = left;
        status.Summary = Colored(Time(left), Green);
        status.Lines.Add(Colored(Text("$whitehilt_prod_ready_in", Time(left)), Green));
        return true;
    }

    private static bool ReadCooking(CookingStation station, ProductionStatus status)
    {
        if (!Valid(station.m_nview) || station.m_slots == null)
        {
            return false;
        }

        status.Reset(station, ProductionKind.Cooking, station.m_name);
        ZDO zdo = station.m_nview.GetZDO();
        Vector3 position = station.transform.position;
        float speed = Mathf.Max(0.01f, Perks.KitchenSpeed(SkillLevels.BestNear(position, CookingStars.CookRange, Skills.SkillType.Cooking)));
        bool canBurn = station.m_canOvercookItems && !CookingStars.WatchfulCookNear(position);

        int raw = 0;
        int done = 0;
        int burnt = 0;
        double nextRaw = double.MaxValue;
        double lastRaw = 0.0;
        double nextBurn = double.MaxValue;
        for (int i = 0; i < station.m_slots.Length; i++)
        {
            string item = zdo.GetString(SlotKey(i));
            if (string.IsNullOrEmpty(item))
            {
                continue;
            }

            float cooked = zdo.GetFloat(SlotKey(i));
            int slotStatus = zdo.GetInt(StatusKey(i));
            CookingStation.ItemConversion conversion = station.GetItemConversion(item);
            if (slotStatus == StatusBurnt)
            {
                burnt++;
            }
            else if (slotStatus == StatusDone)
            {
                done++;
                if (canBurn && conversion != null)
                {
                    nextBurn = Math.Min(nextBurn, Math.Max(0.0, (conversion.m_cookTime * 2f - cooked) / speed));
                }
            }
            else
            {
                raw++;
                if (conversion != null)
                {
                    double left = Math.Max(0.0, (conversion.m_cookTime - cooked) / speed);
                    nextRaw = Math.Min(nextRaw, left);
                    lastRaw = Math.Max(lastRaw, left);
                }
            }
        }

        bool eternal = EternalFireRules.Applies(station);
        float fuel = station.m_useFuel ? station.GetFuel() : 0f;
        bool lit = (station.m_requireFire && station.IsFireLit()) || (station.m_useFuel && fuel > 0f && (station.m_useFueldWhileEmpty || raw > 0));
        string fuelName = station.m_fuelItem != null ? station.m_fuelItem.m_itemData.m_shared.m_name : string.Empty;

        if (raw > 0 && !lit)
        {
            Stop(status, station.m_useFuel ? Text("$whitehilt_prod_nofuel", fuelName) : Text("$whitehilt_prod_needfire"));
        }
        else if (raw > 0)
        {
            status.State = ProductionState.Working;
            status.Seconds = lastRaw;
            status.Summary = Colored(raw > 1 ? $"{Time(nextRaw)} · {raw}" : Time(nextRaw), Green);
            status.Lines.Add(Colored(Text("$whitehilt_prod_cooking", Time(nextRaw), raw), Green));
        }
        else if (done > 0 || burnt > 0)
        {
            SetReady(status, "$whitehilt_prod_ready");
        }

        if (done > 0)
        {
            status.Lines.Add(Colored(Text("$whitehilt_prod_cooked", done), Blue));
            if (lit && nextBurn < double.MaxValue)
            {
                string burns = Colored(Text("$whitehilt_prod_burns", Time(nextBurn)), Red);
                status.Lines.Add(burns);
                status.Summary = burns;
                status.Attention = true;
                if (nextBurn <= BurnWarningSeconds)
                {
                    status.Warning = "$whitehilt_prod_msg_burning";
                }
            }
            else if (station.m_canOvercookItems && !canBurn)
            {
                status.Lines.Add(Colored(Text("$whitehilt_prod_watched"), Grey));
            }
        }

        if (burnt > 0)
        {
            status.Lines.Add(Colored(Text("$whitehilt_prod_burnt", burnt), Red));
        }

        if (station.m_useFuel && !eternal && fuel > 0f && station.m_secPerFuel > 0 && station.m_fuelItem != null)
        {
            double lasts = fuel * station.m_secPerFuel / speed;
            bool runsShort = raw > 0 && lasts < lastRaw;
            status.Attention |= runsShort;
            status.Lines.Add(Colored(Text("$whitehilt_prod_fuel_lasts", Time(lasts), fuelName), runsShort ? Orange : Green));
        }

        return true;
    }

    private static bool ReadBeehive(Beehive hive, ProductionStatus status)
    {
        if (!Valid(hive.m_nview) || hive.m_secPerUnit <= 0f || hive.m_maxHoney <= 0)
        {
            return false;
        }

        status.Reset(hive, ProductionKind.Beehive, hive.m_name);
        int level = hive.GetHoneyLevel();
        if (level >= hive.m_maxHoney)
        {
            return SetReady(status, "$whitehilt_prod_full");
        }

        if (!hive.CheckBiome())
        {
            return Stop(status, Text(hive.m_areaText));
        }

        if (!hive.HaveFreeSpace())
        {
            return Stop(status, Text(hive.m_freespaceText));
        }

        return SetFilling(status, hive.m_nview.GetZDO(), hive.m_secPerUnit, level, hive.m_maxHoney);
    }

    private static bool ReadSapCollector(SapCollector collector, ProductionStatus status)
    {
        if (!Valid(collector.m_nview) || collector.m_secPerUnit <= 0f || collector.m_maxLevel <= 0)
        {
            return false;
        }

        status.Reset(collector, ProductionKind.SapCollector, collector.m_name);
        int level = collector.GetLevel();
        if (level >= collector.m_maxLevel)
        {
            return SetReady(status, "$whitehilt_prod_full");
        }

        if (collector.m_root == null)
        {
            Stop(status, Text(collector.m_notConnectedText));
            status.Lines.Clear();
            return true;
        }

        if (!collector.m_root.CanDrain(1f))
        {
            return Stop(status, Text("$whitehilt_prod_rootempty"));
        }

        return SetFilling(status, collector.m_nview.GetZDO(), collector.m_secPerUnit, level, collector.m_maxLevel);
    }

    private static bool ReadFire(Fireplace fire, ProductionStatus status)
    {
        if (!Valid(fire.m_nview) || fire.m_infiniteFuel || !fire.m_canRefill || fire.m_secPerFuel <= 0f || fire.m_fuelItem == null)
        {
            return false;
        }

        Piece piece = fire.GetComponent<Piece>();
        if (piece == null || !piece.IsPlacedByPlayer())
        {
            return false;
        }

        status.Reset(fire, ProductionKind.Fire, fire.m_name);
        ZDO zdo = fire.m_nview.GetZDO();
        if (zdo.GetInt(ZDOVars.s_state, 1) != 1)
        {
            return true;
        }

        float fuel = zdo.GetFloat(ZDOVars.s_fuel);
        if (fuel <= 0f)
        {
            return Stop(status, Text("$whitehilt_prod_out"));
        }

        if (!fire.IsBurning())
        {
            return Stop(status, Text("$whitehilt_prod_wet"));
        }

        double lasts = fuel * fire.m_secPerFuel;
        bool low = lasts < FireLowSeconds;
        string color = low ? Orange : Green;
        status.State = ProductionState.Working;
        status.Seconds = lasts;
        status.Attention = low;
        status.Summary = Colored(Time(lasts), color);
        status.Lines.Add(Colored(Text("$whitehilt_prod_fuel_lasts", Time(lasts), fire.m_fuelItem.m_itemData.m_shared.m_name), color));
        return true;
    }

    private static bool ReadEgg(EggGrow egg, ProductionStatus status)
    {
        if (!Valid(egg.m_nview) || egg.m_item == null)
        {
            return false;
        }

        status.Reset(egg, ProductionKind.Egg, egg.m_item.m_itemData.m_shared.m_name);
        if (egg.m_item.m_itemData.m_stack > 1)
        {
            return Stop(status, Text("$whitehilt_prod_egg_stacked"));
        }

        float start = egg.m_nview.GetZDO().GetFloat(ZDOVars.s_growStart);
        if (start > 0f)
        {
            double left = Math.Max(0.0, start + egg.m_growTime - ZNet.instance.GetTimeSeconds());
            status.State = ProductionState.Working;
            status.Seconds = left;
            status.Summary = Colored(Time(left), Green);
            status.Lines.Add(Colored(Text("$whitehilt_prod_egg_hatches", Time(left)), Green));
            return true;
        }

        Vector3 position = egg.transform.position;
        if (egg.m_requireNearbyFire && !EffectArea.IsPointInsideArea(position, EffectArea.Type.Heat, 0.5f))
        {
            return Stop(status, Text("$whitehilt_prod_egg_heat"));
        }

        if (egg.m_requireUnderRoof)
        {
            Cover.GetCoverForPoint(position, out float cover, out bool underRoof, 0.1f);
            if (!underRoof || cover < egg.m_requireCoverPercentige)
            {
                return Stop(status, Text("$whitehilt_prod_egg_roof"));
            }
        }

        return true;
    }

    private static bool ReadAnimal(Procreation animal, ProductionStatus status, bool detailed)
    {
        Tameable tameable = animal.m_tameable;
        if (!Valid(animal.m_nview) || tameable == null || !tameable.IsTamed() || animal.GetComponent<DogCompanion>() != null)
        {
            return false;
        }

        status.Reset(animal, ProductionKind.Animal, animal.m_character != null ? animal.m_character.m_name : animal.name);
        ZDO zdo = animal.m_nview.GetZDO();
        Vector3 position = animal.transform.position;
        long pregnant = zdo.GetLong(ZDOVars.s_pregnant, 0L);
        if (pregnant != 0L)
        {
            float factor = HusbandrySkill.GetFactorNear(position);
            double duration = animal.m_pregnancyDuration * HusbandrySkill.PregnancyDuration(factor)
                * (AnimalCare.IsContent(tameable) ? TamingPatches.ContentBreedingFactor : 1f);
            double left = Math.Max(0.0, duration - (ZNet.instance.GetTime() - new DateTime(pregnant)).TotalSeconds);
            status.State = ProductionState.Working;
            status.Seconds = left;
            status.Summary = Colored(Time(left), Green);
            status.Lines.Add(Colored(Text("$whitehilt_prod_birth", Time(left)), Green));
            return true;
        }

        if (!detailed)
        {
            return true;
        }

        string reason = BreedingBlock(animal, tameable, zdo, position);
        string love = Text("$whitehilt_prod_love", animal.GetLovePoints(), animal.m_requiredLovePoints);
        status.Lines.Add(reason == null ? Colored(love, Grey) : Colored($"{love} - {reason}", Orange));
        return true;
    }

    // Why vanilla's Procreate would not add love now, or null.
    private static string BreedingBlock(Procreation animal, Tameable tameable, ZDO zdo, Vector3 position)
    {
        if (tameable.IsHungry())
        {
            return Text("$whitehilt_prod_hungry");
        }

        GameObject self = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(zdo.GetPrefab()) : null;
        if (self == null)
        {
            return null;
        }

        GameObject young = animal.m_offspring != null ? ZNetScene.instance.GetPrefab(Utils.GetPrefabName(animal.m_offspring)) : null;
        int herd = SpawnSystem.GetNrOfInstances(self, position, animal.m_totalCheckRange)
            + (young != null ? SpawnSystem.GetNrOfInstances(young, position, animal.m_totalCheckRange) : 0);
        if (herd >= animal.m_maxCreatures + HusbandrySkill.ExtraHerd(HusbandrySkill.GetFactorNear(position)))
        {
            return Text("$whitehilt_prod_crowded");
        }

        if (animal.m_noPartnerOffspring != null)
        {
            return null;
        }

        GameObject partner = animal.m_seperatePartner != null ? animal.m_seperatePartner : self;
        int partners = SpawnSystem.GetNrOfInstances(partner, position, animal.m_partnerCheckRange, false, true);
        bool enough = animal.m_seperatePartner != null ? partners >= 1 : partners >= 2;
        return enough ? null : Text("$whitehilt_prod_nopartner");
    }

    private static bool SetFilling(ProductionStatus status, ZDO zdo, float secPerUnit, int level, int max)
    {
        DateTime now = ZNet.instance.GetTime();
        double since = Math.Max(0.0, (now - new DateTime(zdo.GetLong(ZDOVars.s_lastTime, now.Ticks))).TotalSeconds);
        double next = Math.Max(0.0, secPerUnit - (zdo.GetFloat(ZDOVars.s_product) + since));
        double full = next + (max - level - 1) * (double)secPerUnit;
        status.State = ProductionState.Working;
        status.Seconds = full;
        status.Summary = Colored($"{Time(next)} · {level}/{max}", Green);
        status.Lines.Add(Colored(Text("$whitehilt_prod_next", Time(next)), Green));
        status.Lines.Add(Colored(Text("$whitehilt_prod_full_in", Time(full)), Green));
        return true;
    }

    private static bool SetReady(ProductionStatus status, string token)
    {
        status.State = ProductionState.Ready;
        status.Summary = Colored(Text(token), Blue);
        return true;
    }

    private static bool Stop(ProductionStatus status, string reason)
    {
        status.State = ProductionState.Stopped;
        status.Reason = reason;
        status.Summary = Colored(reason, Red);
        status.Lines.Add(Colored(Text("$whitehilt_prod_stopped", reason), Red));
        return true;
    }

    private static bool Valid(ZNetView nview)
    {
        return nview != null && nview.IsValid();
    }

    private static string SlotKey(int slot)
    {
        if (slot >= slotKeys.Length)
        {
            return "slot" + slot;
        }

        return slotKeys[slot] ??= "slot" + slot;
    }

    private static string StatusKey(int slot)
    {
        if (slot >= statusKeys.Length)
        {
            return "slotstatus" + slot;
        }

        return statusKeys[slot] ??= "slotstatus" + slot;
    }
}
