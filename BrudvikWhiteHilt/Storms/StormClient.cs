using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.HudLayout;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Storms;

/// <summary>
/// Runs on the <see cref="Game"/> object of every machine with a player. Tells the player of a storm coming, breaking
/// and passing, keeps their exposure meter from where they are, and makes them tired, cold, half blind and in the end
/// dead out in the open.
/// </summary>
public class StormClient : MonoBehaviour
{
    private const float TickSeconds = 0.5f;
    private const float VisionFade = 2f;
    private const float LeeDistance = 6f;
    private const float ForestRadius = 8f;
    private const int ForestTrees = 4;
    private const float RoofReach = 30f;
    private const float WarningAlertSeconds = 60f;

    private static readonly Color stormColor = new(0.85f, 0.7f, 1f);
    private static readonly float[] leeHeights = { 0.5f, 1.2f, 1.8f };
    private static readonly Collider[] nearby = new Collider[96];
    private static int solidMask;
    private static int pieceMask;

    private float nextTick;
    private float lastTick;
    private float vision;
    private int shownNumber = -1;
    private StormPhase shownPhase = StormPhase.Calm;
    private int shownStage;
    private float owed;
    private GameObject[] suffering;
    private bool sufferingBurns;

    /// <summary>The local player's exposure, 0 to 100.</summary>
    public static float Exposure { get; private set; }

    /// <summary>Where the local player is, as the storm sees it.</summary>
    public static StormCover Cover { get; private set; }

    /// <summary>The local player's speed out in the storm, as a share.</summary>
    public static float SpeedFactor { get; private set; } = 1f;

    /// <summary>The storm where the local player is.</summary>
    public static StormProfile Profile { get; private set; } = StormProfile.For(Heightmap.Biome.Meadows);

    /// <summary>Whether a storm rages where the local player is.</summary>
    public static bool RagingHere { get; private set; }

    /// <summary>
    /// The storm on the clock: "Storm in 2:10" while it comes, "Storm, 3:05 left" while it rages.
    /// </summary>
    /// <returns>The text, or null when calm.</returns>
    public static string ClockText()
    {
        if (StormState.Phase == StormPhase.Calm || StormSettings.Enabled == null || !StormSettings.Enabled.Value)
        {
            return null;
        }

        string key = StormState.Coming ? "whitehilt_storm_clock_coming" : "whitehilt_storm_clock_raging";
        return string.Format(Translations.Word(key), StormState.Countdown(StormState.SecondsLeft()));
    }

    private void Start()
    {
        Exposure = 0f;
        Cover = StormCover.Open;
        SpeedFactor = 1f;
        solidMask = LayerMask.GetMask("Default", "static_solid", "terrain", "piece");
        pieceMask = LayerMask.GetMask("piece");
    }

    private void OnDestroy()
    {
        StopSuffering();
        StormWeather.SetVision(0f);
    }

    private void Update()
    {
        Player player = Player.m_localPlayer;
        if (player == null || ZNet.instance == null || StormSettings.Enabled == null)
        {
            return;
        }

        Announce(player);
        if (Time.time >= nextTick)
        {
            float seconds = Mathf.Clamp(Time.time - lastTick, 0f, 2f);
            lastTick = Time.time;
            nextTick = Time.time + TickSeconds;
            Tick(player, seconds);
        }

        bool outside = RagingHere && Cover is StormCover.Open or StormCover.Lee or StormCover.Forest;
        float target = player.IsDead() ? 0f : StormExposure.Vision(Exposure, Profile, outside) * StormSettings.VisionStrength.Value;
        vision = Mathf.MoveTowards(vision, target, Time.deltaTime / VisionFade);
        StormVision.Show(vision, Profile.Tint);
        StormWeather.SetVision(vision);
    }

    // Tells the player when a storm is coming, when it breaks and when it passes.
    private void Announce(Player player)
    {
        if (StormState.Number == shownNumber && StormState.Phase == shownPhase)
        {
            return;
        }

        StormPhase was = shownPhase;
        shownNumber = StormState.Number;
        shownPhase = StormState.Phase;
        if (!StormSettings.Enabled.Value)
        {
            return;
        }

        bool here = StormSettings.HasStorms(player.GetCurrentBiome());
        switch (StormState.Phase)
        {
            case StormPhase.Warning:
                string text = here
                    ? string.Format(Localization.instance.Localize("$msg_whitehilt_storm_warning"), StormState.Countdown(StormState.SecondsLeft()))
                    : Localization.instance.Localize("$msg_whitehilt_storm_warning_safe");
                player.Message(MessageHud.MessageType.Center, text);
                if (HudSettings.WhiteHilt)
                {
                    HudAlerts.Warn(player, text, stormColor, WarningAlertSeconds);
                }

                StormAmbience.PlayWarning();
                break;
            case StormPhase.Raging when here:
                string kind = StormProfile.For(player.GetCurrentBiome()).Kind.ToString().ToLowerInvariant();
                player.Message(MessageHud.MessageType.Center, $"$msg_whitehilt_storm_{kind}");
                StormAmbience.Thunder(true);
                break;
            case StormPhase.Calm when was == StormPhase.Raging:
                player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_storm_end");
                break;
        }
    }

    private void Tick(Player player, float seconds)
    {
        Heightmap.Biome biome = player.GetCurrentBiome();
        Profile = StormProfile.For(biome);
        RagingHere = StormSettings.Enabled.Value && StormState.Raging && StormSettings.HasStorms(biome) && !player.IsDead();
        Cover = FindCover(player);
        Exposure = player.IsDead()
            ? 0f
            : StormExposure.Step(Exposure, seconds, RagingHere, Profile, Cover, StormSettings.MinutesToFull.Value, StormSettings.MinutesToRecover.Value);
        SpeedFactor = RagingHere && Cover is StormCover.Open or StormCover.Lee or StormCover.Forest ? Profile.Speed : 1f;

        UpdateEffect(player);
        WarnOfStage(player);
        Hurt(player, seconds);
        UpdateSuffering(player);
    }

    private StormCover FindCover(Player player)
    {
        bool interior = player.InInterior();
        bool roof = player.InShelter() || StormLeanToArea.IsUnder(player.transform.position);
        bool fire = player.m_nearFireTimer < 0.25f;
        if (!RagingHere)
        {
            return StormExposure.Resolve(Profile, interior, roof, fire, true, false, false);
        }

        bool stone = roof && Profile.NeedsStoneRoof && StoneRoof(player);
        bool shelters = StormSettings.LeeShelters.Value && !roof && !interior;
        bool lee = shelters && InLee(player);
        bool forest = shelters && !lee && Profile.ForestShelters && AmongTrees(player);
        return StormExposure.Resolve(Profile, interior, roof, fire, stone, lee, forest);
    }

    // Something solid close upwind: two of three rays from the knees to the head hit it. Trees do not count.
    private static bool InLee(Player player)
    {
        Vector3 wind = EnvMan.instance != null ? EnvMan.instance.GetWindDir() : Vector3.zero;
        Vector3 upwind = new(-wind.x, 0f, -wind.z);
        if (upwind.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        upwind.Normalize();
        int hits = 0;
        foreach (float height in leeHeights)
        {
            if (Physics.Raycast(player.transform.position + Vector3.up * height, upwind, out RaycastHit hit, LeeDistance, solidMask)
                && hit.collider.GetComponentInParent<TreeBase>() == null)
            {
                hits++;
            }
        }

        return hits >= 2;
    }

    private static bool AmongTrees(Player player)
    {
        int count = Physics.OverlapSphereNonAlloc(player.transform.position, ForestRadius, nearby, solidMask);
        HashSet<TreeBase> trees = new();
        for (int i = 0; i < count; i++)
        {
            TreeBase tree = nearby[i] != null ? nearby[i].GetComponentInParent<TreeBase>() : null;
            if (tree != null && trees.Add(tree) && trees.Count >= ForestTrees)
            {
                return true;
            }
        }

        return false;
    }

    // The nearest piece overhead is stone, marble, ash stone, iron or ancient: wood burns in the fire storm.
    private static bool StoneRoof(Player player)
    {
        if (!Physics.Raycast(player.GetHeadPoint(), Vector3.up, out RaycastHit hit, RoofReach, pieceMask))
        {
            return false;
        }

        WearNTear piece = hit.collider.GetComponentInParent<WearNTear>();
        return piece != null && piece.m_materialType != WearNTear.MaterialType.Wood && piece.m_materialType != WearNTear.MaterialType.HardWood;
    }

    private static void UpdateEffect(Player player)
    {
        SEMan seman = player.GetSEMan();
        StormExposureEffect template = StormExposureEffect.Template;
        if (seman == null || template == null)
        {
            return;
        }

        bool has = seman.HaveStatusEffect(template.NameHash());
        if (Exposure > 0.01f && !has)
        {
            seman.AddStatusEffect(template);
        }
        else if (Exposure <= 0.01f && has)
        {
            seman.RemoveStatusEffect(template.NameHash(), quiet: true);
        }
    }

    // A message each time the meter climbs into a worse stage.
    private void WarnOfStage(Player player)
    {
        int stage = StormExposure.Stage(Exposure);
        if (stage <= shownStage || !RagingHere)
        {
            shownStage = Mathf.Min(shownStage, stage);
            return;
        }

        shownStage = stage;
        string token = stage switch
        {
            1 => "$msg_whitehilt_storm_tired",
            3 when Profile.Kind != StormKind.Sand => Profile.Burns ? "$msg_whitehilt_storm_burning" : "$msg_whitehilt_storm_freezing",
            4 when Profile.Hurts => "$msg_whitehilt_storm_dying",
            _ => null
        };
        if (token != null)
        {
            player.Message(MessageHud.MessageType.Center, token);
        }
    }

    // The damage is gathered until it is at least 1, as the game ignores the smallest hits.
    private void Hurt(Player player, float seconds)
    {
        float share = RagingHere ? StormExposure.DamageShare(Exposure, Profile) : 0f;
        if (share <= 0f || player.InGodMode() || player.IsDead())
        {
            owed = 0f;
            return;
        }

        owed += player.GetMaxHealth() * share * seconds * StormSettings.DamageMultiplier.Value;
        if (owed < 1f)
        {
            return;
        }

        float amount = owed;
        owed = 0f;
        if (!StormSettings.CanKill.Value)
        {
            amount = Mathf.Min(amount, player.GetHealth() - 1f);
        }

        if (amount <= 0f)
        {
            return;
        }

        HitData hit = new()
        {
            m_point = player.GetCenterPoint(),
            m_hitType = Profile.Burns ? HitData.HitType.Burning : HitData.HitType.Freezing
        };
        hit.m_damage.m_damage = amount;
        player.ApplyDamage(hit, showDamageText: false, triggerEffects: false);
    }

    // The game's own frost or flames on the player while they freeze or burn.
    private void UpdateSuffering(Player player)
    {
        bool on = RagingHere && !player.IsDead() && StormExposure.Suffering(Exposure, Profile);
        if (!on || sufferingBurns != Profile.Burns)
        {
            StopSuffering();
        }

        if (!on || suffering != null || ObjectDB.instance == null)
        {
            return;
        }

        sufferingBurns = Profile.Burns;
        StatusEffect source = ObjectDB.instance.GetStatusEffect(Profile.Burns ? SEMan.s_statusEffectBurning : SEMan.s_statusEffectFreezing);
        suffering = source != null && source.m_startEffects != null
            ? source.m_startEffects.Create(player.GetCenterPoint(), player.transform.rotation, player.transform)
            : new GameObject[0];
        if (Profile.Burns)
        {
            StormAmbience.PlayEffect("sfx_storm_ignite");
        }
    }

    private void StopSuffering()
    {
        if (suffering == null)
        {
            return;
        }

        foreach (GameObject effect in suffering)
        {
            if (effect != null)
            {
                Destroy(effect);
            }
        }

        suffering = null;
    }
}
