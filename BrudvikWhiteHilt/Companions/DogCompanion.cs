using BrudvikWhiteHilt.Helpers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Companions;

/// <summary>
/// A player's dog. It grows from a puppy to a grown dog while it is cared for, keeps to its home while small,
/// sleeps in its bed (or beside its master's bed) at night and in its house in the rain, guards its home, sits when its
/// master stops, greys around the muzzle with age,
/// grows its bond by fighting, runs away without a home and starves without food.
/// All state lives in the ZDO; the owner of the ZDO runs the care ticks, every client shows colour and size.
/// </summary>
public sealed partial class DogCompanion : MonoBehaviour
{
    /// <summary>
    /// Metres around home the dog counts as being home.
    /// </summary>
    public const float HomeRadius = 30f;

    /// <summary>
    /// Highest bond level.
    /// </summary>
    public const int MaxBondLevel = 10;

    private const float PuppyScale = 0.3f;

    /// <summary>
    /// Size of a grown dog compared to a wolf.
    /// </summary>
    public const float AdultScale = 0.6f;

    private const float StarvingWarningDays = 10f;

    private const float HungryWarningDays = 3f;

    private const float TickSeconds = 5f;

    private const float GiveUpWalkingSeconds = 45f;

    private static float GuardRadius => DogSettings.GuardRadius.Value;

    private const float GuardCooldown = 60f;

    private const float GuardNoticeRange = 150f;

    private const float FollowRestoreRange = 60f;

    private const float FarAway = 50f;

    private const float SitCheckSeconds = 0.5f;

    private const float SitAfterSeconds = 3f;

    private const float SitRange = 5f;

    private const float BoardRange = 25f;

    private const float BoardMaxShipSpeed = 2f;

    private const float WhineInterval = 45f;

    private const float WaterGrowthFactor = 1.25f;

    private const float ColorCheckSeconds = 10f;

    // The muzzle greys between these fractions of the dog's life; without old age, of a default life.
    private const float GreyStartLife = 0.4f;

    private const float GreyFullLife = 0.75f;

    private const float DefaultLifeDays = 160f;

    // Old from this fraction of its life; it dies at the end of it once it lies down at home, or anywhere a few days later.
    private const float OldLife = 0.85f;

    private const float LifeSpread = 0.1f;

    private const float OldAgeGraceDays = 3f;

    // Two grown, fed, happy dogs with a strong bond, together at night, may have a puppy; at most one litter in so many days.
    private const float LitterRange = 8f;

    private static int LitterMinBond => DogSettings.LitterMinBond.Value;

    private const float LitterMinMood = 0.6f;

    private static float LitterChance => DogSettings.LitterChance.Value;

    private static int LitterCooldownDays => DogSettings.LitterCooldownDays.Value;

    private const float LonelyMood = 0.25f;

    private const float HappySeconds = 8f;

    private static float XpPerLevelSquared => DogSettings.BondXpPerLevelSquared.Value;

    private const string SoundRpc = "WhiteHilt_DogSound";

    private const string HappyRpc = "WhiteHilt_DogHappy";

    private const string XpRpc = "WhiteHilt_DogXp";

    private const string CollarRpc = "WhiteHilt_DogCollar";

    private const string HomeRpc = "WhiteHilt_DogHome";

    // A tick never counts more than a day, so time away from an unloaded dog barely counts.
    private const float MaxTickDays = 1f;

    private const int NeedsHouse = 1;

    private const int NeedsBed = 2;

    private static readonly List<DogCompanion> dogs = new();

    private static readonly int ownerKey = "whitehilt_dog_owner".GetStableHashCode();

    private static readonly int colorKey = "whitehilt_dog_color".GetStableHashCode();

    private static readonly int ageKey = "whitehilt_dog_age".GetStableHashCode();

    private static readonly int hungerKey = "whitehilt_dog_hunger".GetStableHashCode();

    private static readonly int neglectKey = "whitehilt_dog_neglect".GetStableHashCode();

    private static readonly int needsKey = "whitehilt_dog_needs".GetStableHashCode();

    private static readonly int tickKey = "whitehilt_dog_tick".GetStableHashCode();

    private static readonly int homeKey = "whitehilt_dog_home".GetStableHashCode();

    private static readonly int bornKey = "whitehilt_dog_born".GetStableHashCode();

    private static readonly int goalRestKey = "whitehilt_dog_goalrest".GetStableHashCode();

    private static readonly int followKey = "whitehilt_dog_follow".GetStableHashCode();

    private static readonly int xpKey = "whitehilt_dog_xp".GetStableHashCode();

    private static readonly int collarKey = "whitehilt_dog_collar".GetStableHashCode();

    private static readonly int boostKey = "whitehilt_dog_boost".GetStableHashCode();

    private static readonly int warnedKey = "whitehilt_dog_warned".GetStableHashCode();

    private static readonly int oldNoticeKey = "whitehilt_dog_oldnotice".GetStableHashCode();

    private static readonly int oldAgeKey = "whitehilt_dog_oldage".GetStableHashCode();

    private static readonly int litterDayKey = "whitehilt_dog_litterday".GetStableHashCode();

    private static readonly int litterTryKey = "whitehilt_dog_littertry".GetStableHashCode();

    private static readonly int shipLayers = LayerMask.GetMask("vehicle");

    // Fitted offline around the wolf rig's neck, in the Neck bone's space; the collar rings lie in their XZ plane.
    private static readonly Vector3 collarPosition = new(-0.0906f, 0.04664f, 0f);

    private static readonly Quaternion collarRotation = new(-0.53413f, 0.46336f, 0.53413f, 0.46336f);

    private static readonly Vector3[] collarScales = { Vector3.zero, new(0.03221f, 0.03515f, 0.03808f), new(0.13363f, 0.06f, 0.15797f) };

    private ZNetView nview;

    private Character character;

    private MonsterAI ai;

    private Tameable tameable;

    private RestPose rest;

    private SkinnedMeshRenderer[] bodies;

    private Material[] originalMaterials;

    private float viewRange;

    private float hearRange;

    private float alertRange;

    private int shownColor = -1;

    private int shownGrey = -1;

    private float nextColorCheck;

    private float shownScale = -1f;

    private float nextTick;

    private float nextGuard;

    private float nextSitCheck;

    private float masterStill;

    private Vector3 masterPosition;

    private RestSpot goal;

    private RestPose.Pose goalPose;

    private float goalWalkTime;

    private Transform neck;

    private GameObject collarModel;

    private int shownCollar;

    private DogActivities activities;

    private DogExpression expression;

    private DogCare care;

    private float nextWhine;

    private int shownCoat = -1;

    /// <summary>
    /// Every loaded dog.
    /// </summary>
    public static IReadOnlyList<DogCompanion> All => dogs;

    /// <summary>
    /// True while the dog is still growing.
    /// </summary>
    public bool IsPuppy => Grown < 1f;

    /// <summary>
    /// Player ID of the dog's owner.
    /// </summary>
    public long OwnerId => Zdo?.GetLong(ownerKey) ?? 0L;

    /// <summary>
    /// Player ID of the player the dog follows, or 0.
    /// </summary>
    public long FollowsPlayerId => Zdo?.GetLong(followKey) ?? 0L;

    /// <summary>
    /// The dog's name.
    /// </summary>
    public string DogName => tameable.GetHoverName();

    /// <summary>
    /// Where the dog lives.
    /// </summary>
    public Vector3 Home => Zdo?.GetVec3(homeKey, transform.position) ?? transform.position;

    /// <summary>
    /// Bond level from fighting together, 0 to <see cref="MaxBondLevel"/>.
    /// </summary>
    public int BondLevel => Mathf.Min(MaxBondLevel, Mathf.FloorToInt(Mathf.Sqrt((Zdo?.GetFloat(xpKey) ?? 0f) / XpPerLevelSquared)));

    /// <summary>
    /// Armour from the dog's collar.
    /// </summary>
    public float CollarArmor => DogRegistry.GetCollarArmor(Zdo?.GetInt(collarKey) ?? 0);

    /// <summary>
    /// How grown the dog is, from 0 (just bought) to 1.
    /// </summary>
    public float GrowthFraction => Grown;

    /// <summary>
    /// True while the dog carries a stick in its mouth.
    /// </summary>
    public bool IsCarrying => Zdo?.GetBool(DogActivities.CarryKey) ?? false;

    /// <summary>
    /// True while the dog digs.
    /// </summary>
    public bool IsDigging => Zdo?.GetBool(DogActivities.DigKey) ?? false;

    /// <summary>
    /// Game days since the dog was let out as a puppy.
    /// </summary>
    public float DaysLived
    {
        get
        {
            ZDO zdo = Zdo;
            if (zdo == null || ZNet.instance == null)
            {
                return 0f;
            }

            long now = ZNet.instance.GetTime().Ticks;
            return (float)(System.TimeSpan.FromTicks(now - zdo.GetLong(bornKey, now)).TotalSeconds / DaySeconds);
        }
    }

    // This dog's own span, the same on every client: the setting, give or take LifeSpread.
    private float LifeDays
    {
        get
        {
            float setting = DogSettings.LifeDays.Value;
            if (setting <= 0f || Zdo == null)
            {
                return setting;
            }

            // The birth time is saved and synced, so it gives every client and session the same spread.
            long born = Zdo.GetLong(bornKey);
            float spread = (float)((ulong)born / System.TimeSpan.TicksPerSecond % 1000UL) / 999f * 2f - 1f;
            return setting * (1f + LifeSpread * spread);
        }
    }

    private bool IsOld => LifeDays > 0f && DaysLived >= OldLife * LifeDays;

    private bool IsFed => Zdo != null && Zdo.GetFloat(hungerKey) < DaySeconds;

    private bool CanHaveLitter => !IsPuppy && !IsOld && IsFed && BondLevel >= LitterMinBond && care != null && care.Mood >= LitterMinMood
        && !character.IsDead();

    private static float DaySeconds => EnvMan.instance != null ? EnvMan.instance.m_dayLengthSec : 1800f;

    private ZDO Zdo => nview != null && nview.IsValid() ? nview.GetZDO() : null;

    private float Grown => Zdo == null ? 0f : Mathf.Clamp01(Zdo.GetFloat(ageKey) / (DogSettings.GrowDays.Value * DaySeconds));

    /// <summary>
    /// Damage multiplier of a character's attacks from its bond level; 1 for anything but a dog.
    /// </summary>
    /// <param name="attacker">The attacking character.</param>
    /// <returns>The multiplier.</returns>
    public static float GetDamageFactor(Character attacker)
    {
        return attacker != null && attacker.TryGetComponent(out DogCompanion dog)
            ? (1f + DogSettings.DamagePerBondLevel.Value * dog.BondLevel) * (dog.care != null ? dog.care.MoodDamageFactor : 1f)
            : 1f;
    }

    /// <summary>
    /// Finds a loaded dog of a player.
    /// </summary>
    /// <param name="ownerId">Player ID of the owner.</param>
    /// <returns>The dog, or null.</returns>
    public static DogCompanion FindOwnedBy(long ownerId)
    {
        dogs.RemoveAll(dog => dog == null);
        return dogs.FirstOrDefault(dog => dog.OwnerId == ownerId && !dog.character.IsDead());
    }

    /// <summary>
    /// Sets up a newly released puppy. Call on the machine that spawned it.
    /// </summary>
    /// <param name="ownerId">Player ID of the owner.</param>
    /// <param name="color">Index of the coat colour.</param>
    public void InitializeNew(long ownerId, int color)
    {
        ZDO zdo = Zdo;
        if (zdo == null)
        {
            return;
        }

        DogHomePiece house = DogHomePiece.FindNearest(DogHomeKind.House, transform.position, HomeRadius);
        long now = ZNet.instance.GetTime().Ticks;
        zdo.Set(ownerKey, ownerId);
        zdo.Set(colorKey, color);
        zdo.Set(ageKey, 0f);
        zdo.Set(hungerKey, 0f);
        zdo.Set(neglectKey, 0f);
        zdo.Set(homeKey, house != null ? house.transform.position : transform.position);
        zdo.Set(tickKey, now);
        zdo.Set(bornKey, now);
        character.SetTamed(true);
        ApplyStage();
    }

    /// <summary>
    /// Care status for the hover text, one line per note.
    /// </summary>
    /// <returns>Localised lines.</returns>
    public string GetStatusText()
    {
        ZDO zdo = Zdo;
        if (zdo == null)
        {
            return string.Empty;
        }

        List<string> lines = new();
        float grown = Grown;
        lines.Add(grown < 1f
            ? Localization.instance.Localize(Translations.Token("whitehilt_dog_growing"), Mathf.FloorToInt(grown * 100f).ToString())
            : Localization.instance.Localize(Translations.Token("whitehilt_dog_adult")));

        int days = Mathf.FloorToInt(DaysLived);
        lines.Add(Localization.instance.Localize(Translations.Token("whitehilt_dog_age"),
            Localization.instance.Localize(Translations.Token(days == 1 ? "whitehilt_dog_day" : "whitehilt_dog_days"), days.ToString())));
        if (IsOld)
        {
            lines.Add(Translations.Token("whitehilt_dog_old"));
        }
        if (grown < 1f && activities != null && activities.DrankWithin(DaySeconds))
        {
            lines.Add(Translations.Token("whitehilt_dog_watered"));
        }

        int level = BondLevel;
        if (level > 0)
        {
            lines.Add(Localization.instance.Localize(Translations.Token("whitehilt_dog_level"), level.ToString()));
        }

        if (care != null)
        {
            lines.AddRange(care.GetStatusTokens());
        }

        float armor = CollarArmor;
        if (armor > 0f)
        {
            lines.Add(Localization.instance.Localize(Translations.Token("whitehilt_dog_collar"), armor.ToString("0")));
        }

        float day = DaySeconds;
        float hunger = zdo.GetFloat(hungerKey);
        int needs = zdo.GetInt(needsKey);
        if (hunger >= StarvingWarningDays * day)
        {
            lines.Add(Translations.Token("whitehilt_dog_starving"));
        }
        else if (hunger >= day)
        {
            lines.Add(Translations.Token("whitehilt_dog_hungry"));
        }

        if ((needs & NeedsHouse) != 0)
        {
            lines.Add(Translations.Token("whitehilt_dog_needs_house"));
        }

        if ((needs & NeedsBed) != 0)
        {
            lines.Add(Translations.Token("whitehilt_dog_needs_bed"));
        }

        if (grown < 1f && (needs != 0 || hunger >= day))
        {
            lines.Add(Translations.Token("whitehilt_dog_paused"));
        }

        if (zdo.GetFloat(neglectKey) >= day)
        {
            lines.Add(Translations.Token("whitehilt_dog_unhappy"));
        }

        return Localization.instance.Localize("\n<color=#c0c0c0>" + string.Join("\n", lines) + "</color>");
    }

    // Finds the dog's parts and bones and registers its RPCs. The ticks are spread with a random start so many dogs do
    // not all tick in the same frame.
    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        character = GetComponent<Character>();
        ai = GetComponent<MonsterAI>();
        tameable = GetComponent<Tameable>();
        rest = GetComponent<RestPose>();
        bodies = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        originalMaterials = bodies.Select(body => body.sharedMaterial).ToArray();
        neck = GetComponentsInChildren<Transform>(true).FirstOrDefault(bone => bone.name == "Neck");
        viewRange = ai.m_viewRange;
        hearRange = ai.m_hearRange;
        alertRange = ai.m_alertRange;

        if (nview == null || nview.GetZDO() == null)
        {
            return;
        }

        nview.Register<string>(SoundRpc, RPC_Sound);
        nview.Register(HappyRpc, RPC_Happy);
        nview.Register<float>(XpRpc, RPC_Xp);
        nview.Register<int>(CollarRpc, RPC_Collar);
        nview.Register<Vector3>(HomeRpc, RPC_Home);
        activities = GetComponent<DogActivities>();
        expression = GetComponent<DogExpression>();
        care = GetComponent<DogCare>();
        character.m_onDeath += OnDeath;
        ai.m_onConsumedItem += OnAte;
        nextTick = Time.time + Random.Range(0f, TickSeconds);
        dogs.Add(this);
    }

    private void OnDestroy()
    {
        dogs.Remove(this);
        if (character != null)
        {
            character.m_onDeath -= OnDeath;
        }

        if (ai != null)
        {
            ai.m_onConsumedItem -= OnAte;
        }
    }

    // Shows the dog's coat, collar and size on every machine; on its owner, runs its needs and its sitting now and
    // then.
    private void Update()
    {
        if (Zdo == null)
        {
            return;
        }

        ApplyColor();
        ApplyCollar();
        ApplyStage();
        if (!nview.IsOwner())
        {
            return;
        }

        if (Time.time >= nextTick)
        {
            nextTick = Time.time + TickSeconds;
            Tick();
        }

        if (Time.time >= nextSitCheck)
        {
            nextSitCheck = Time.time + SitCheckSeconds;
            UpdateSitting();
            care.FastUpdate();
        }
    }

    // The dog's slow update on its owner: hunger, health, home, old age and care, and where to rest. Time is measured
    // in world time since the last tick (capped), so a dog that was away from players catches up.
    private void Tick()
    {
        ZDO zdo = Zdo;
        long now = ZNet.instance.GetTime().Ticks;
        long last = zdo.GetLong(tickKey, now);
        zdo.Set(tickKey, now);
        float day = DaySeconds;
        float dt = Mathf.Clamp((float)System.TimeSpan.FromTicks(now - last).TotalSeconds, 0f, MaxTickDays * day);

        float hunger = zdo.GetFloat(hungerKey) + dt;
        zdo.Set(hungerKey, hunger);
        if (hunger >= DogSettings.StarveDays.Value * day)
        {
            Starve();
            return;
        }

        RestoreFollow();
        UpdateMaxHealth();
        Vector3 home = UpdateHome(zdo);
        bool following = ai.GetFollowTarget() != null;
        bool atHome = !following && Vector3.Distance(transform.position, home) <= HomeRadius;
        if (UpdateOldAge(atHome))
        {
            return;
        }

        care.Tick(dt, day, atHome, hunger >= day);        DogHomePiece house = DogHomePiece.FindNearest(DogHomeKind.House, home, HomeRadius);
        DogHomePiece bed = DogHomePiece.FindNearest(DogHomeKind.Bed, home, HomeRadius, piece => piece.IsUnderRoof());
        DogHomePiece waterBowl = DogHomePiece.FindNearest(DogHomeKind.WaterBowl, home, HomeRadius);
        int needs = (house == null ? NeedsHouse : 0) | (bed == null ? NeedsBed : 0);
        zdo.Set(needsKey, needs);

        if (atHome)
        {
            float neglect = needs == 0 ? 0f : zdo.GetFloat(neglectKey) + dt;
            zdo.Set(neglectKey, neglect);
            if (neglect >= DogSettings.RunAwayDays.Value * day)
            {
                RunAway();
                return;
            }

            // A lonely dog does not bother to guard.
            if (care.Mood >= LonelyMood)
            {
                Guard(home);
            }

            TryLitter(home, hunger < day);
        }

        WarnOwner(hunger, day);
        Whine(hunger, day);
        activities.Tick(home, atHome, waterBowl, day);

        if (IsPuppy)
        {
            if (needs == 0 && hunger < day)
            {
                bool boosted = now < zdo.GetLong(boostKey, 0L);
                bool watered = waterBowl != null && activities.DrankWithin(day);
                float rate = (boosted ? 2f : 1f) * (watered ? WaterGrowthFactor : 1f);
                zdo.Set(ageKey, zdo.GetFloat(ageKey) + dt * rate);
            }

            if (following)
            {
                ai.SetFollowTarget(null);
                zdo.Set(followKey, 0L);
            }

            if (!ai.m_patrol || ai.m_patrolPoint != home)
            {
                ai.SetPatrolPoint(home);
            }
        }

        UpdateRestGoal(atHome, home, house, bed);
    }

    // The follow target is not saved, so a dog that was unloaded (or taken through a portal) looks for its master again.
    private void RestoreFollow()
    {
        long masterId = Zdo.GetLong(followKey);
        if (masterId == 0L || ai.GetFollowTarget() != null || IsPuppy)
        {
            return;
        }

        Player master = Player.GetPlayer(masterId);
        if (master != null && Vector3.Distance(master.transform.position, transform.position) <= FollowRestoreRange)
        {
            ai.SetFollowTarget(master.gameObject);
        }
    }

    private void UpdateMaxHealth()
    {
        float wanted = character.m_health * (1f + DogSettings.HealthPerBondLevel.Value * BondLevel);
        if (!Mathf.Approximately(character.GetMaxHealth(), wanted))
        {
            character.SetMaxHealth(wanted);
        }
    }
}
