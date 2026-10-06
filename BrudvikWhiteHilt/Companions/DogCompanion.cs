using BrudvikWhiteHilt.Helpers;
using Jotunn.Managers;
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
public sealed class DogCompanion : MonoBehaviour
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

    private int GreyStep
    {
        get
        {
            float life = LifeDays > 0f ? LifeDays : DefaultLifeDays;
            float greying = (DaysLived / life - GreyStartLife) / (GreyFullLife - GreyStartLife);
            return Mathf.Clamp(Mathf.CeilToInt(greying * DogRegistry.GreySteps), 0, DogRegistry.GreySteps);
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

    /// <summary>
    /// Makes the dog bark on every client.
    /// </summary>
    public void Bark()
    {
        PlaySound("dogbark");
    }

    /// <summary>
    /// Makes the dog glad on every client: a happy sound and a wagging tail.
    /// </summary>
    public void Happy()
    {
        if (Zdo != null)
        {
            nview.InvokeRPC(ZNetView.Everybody, HappyRpc);
        }
    }

    /// <summary>
    /// Plays one of the dog's sounds on every client.
    /// </summary>
    /// <param name="sound">Sound name, e.g. dogwhine.</param>
    public void PlaySound(string sound)
    {
        if (Zdo != null)
        {
            nview.InvokeRPC(ZNetView.Everybody, SoundRpc, sound);
        }
    }

    /// <summary>
    /// Adds bond experience, on the dog's owner.
    /// </summary>
    /// <param name="xp">Experience to add.</param>
    public void AddXp(float xp)
    {
        if (Zdo != null)
        {
            nview.InvokeRPC(XpRpc, xp);
        }
    }

    /// <summary>
    /// Puts a collar on the dog, on the dog's owner.
    /// </summary>
    /// <param name="collar">1 for leather, 2 for iron.</param>
    /// <returns>The collar the dog wore before, 0 for none.</returns>
    public int SetCollar(int collar)
    {
        int old = Zdo?.GetInt(collarKey) ?? 0;
        if (Zdo != null)
        {
            nview.InvokeRPC(CollarRpc, collar);
        }

        return old;
    }

    /// <summary>
    /// Moves the dog's home to a dog house, on the dog's owner.
    /// </summary>
    /// <param name="position">Position of the dog house.</param>
    public void SetHome(Vector3 position)
    {
        if (Zdo != null)
        {
            nview.InvokeRPC(HomeRpc, position);
        }
    }

    /// <summary>
    /// Calls the dog to follow the player, from the whistle. A far dog is brought over at once.
    /// </summary>
    /// <param name="player">The local player.</param>
    public void Call(Player player)
    {
        nview.ClaimOwnership();
        ai.SetFollowTarget(player.gameObject);
        Zdo.Set(followKey, player.GetPlayerID());
        rest.SetPose(RestPose.Pose.None);
        if (Vector3.Distance(transform.position, player.transform.position) > FarAway)
        {
            TeleportTo(player.transform.position - player.transform.forward * 2f);
        }

        Bark();
    }

    /// <summary>
    /// Sends the dog home, from the whistle. A far dog is brought home at once.
    /// </summary>
    public void SendHome()
    {
        nview.ClaimOwnership();
        ai.SetFollowTarget(null);
        Zdo.Set(followKey, 0L);
        Vector3 home = Home;
        ai.SetPatrolPoint(home);
        if (Vector3.Distance(transform.position, home) > FarAway)
        {
            TeleportTo(home + new Vector3(1.5f, 0f, 1.5f));
        }
    }

    /// <summary>
    /// Stops following and stays where it is, from the stay trick. Call on the owner.
    /// </summary>
    public void Stay()
    {
        ai.SetFollowTarget(null);
        Zdo.Set(followKey, 0L);
        ai.SetPatrolPoint(transform.position);
    }

    /// <summary>
    /// Moves the dog, also into an area that is not loaded; it appears there when the area loads.
    /// </summary>
    /// <param name="position">Where to.</param>
    public void TeleportTo(Vector3 position)
    {
        nview.ClaimOwnership();
        transform.position = position;
        if (character.m_body != null)
        {
            character.m_body.position = position;
            character.m_body.linearVelocity = Vector3.zero;
        }

        Zdo.SetPosition(position);
    }

    /// <summary>
    /// Remembers which player the dog follows, so it keeps following when it is loaded again. Call on the owner.
    /// </summary>
    public void RememberFollow()
    {
        Player master = ai.GetFollowTarget() != null ? ai.GetFollowTarget().GetComponent<Player>() : null;
        Zdo?.Set(followKey, master != null ? master.GetPlayerID() : 0L);
    }

    /// <summary>
    /// Walks the dog to its bed or house and lays it down there. Called from the AI tick on the owner.
    /// </summary>
    /// <param name="dt">Frame time.</param>
    public void UpdateGoal(float dt)
    {
        if (goal == null || rest.IsResting || !nview.IsOwner() || ai.m_targetCreature != null || ai.GetFollowTarget() != null || activities.IsBusy)
        {
            return;
        }

        goalWalkTime += dt;
        bool arrived = ai.MoveTo(dt, goal.Approach, 0.6f, run: false);
        if (!arrived && goalWalkTime < GiveUpWalkingSeconds)
        {
            return;
        }

        // Snap in: the dog cannot path into a house or basket, and after a long try it simply finds its way.
        Vector3 position = goal.Position;
        Quaternion rotation = goal.Rotation;
        transform.SetPositionAndRotation(position, rotation);
        Rigidbody body = character.m_body;
        if (body != null)
        {
            body.position = position;
            body.linearVelocity = Vector3.zero;
        }

        character.SetLookDir(rotation * Vector3.forward);
        ai.StopMoving();
        rest.SetPose(goalPose);
        Zdo.Set(goalRestKey, true);
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

    // Shows the collar from the network data on the neck bone, made again only when it changes.
    private void ApplyCollar()
    {
        int collar = Zdo.GetInt(collarKey);
        if (collar == shownCollar || VisualHelper.IsHeadless || neck == null)
        {
            return;
        }

        shownCollar = collar;
        if (collarModel != null)
        {
            Destroy(collarModel);
        }

        if (collar <= 0 || collar >= collarScales.Length || bodies.Length == 0)
        {
            return;
        }

        try
        {
            DogRegistry.GetCollarModel(collar, out Mesh mesh, out Texture2D texture);
            collarModel = VisualHelper.CreateModel(neck, mesh, texture, bodies[0], collarPosition, collarRotation, 1f);
            collarModel.transform.localScale = collarScales[collar];
        }
        catch (System.Exception ex)
        {
            Jotunn.Logger.LogWarning($"Dog: no collar model: {ex.Message}");
        }
    }

    // Shows the coat colour, its greying with age and a worn coat, as a material made from the original one; checked
    // now and then and only changed when one of them did.
    private void ApplyColor()
    {
        if (VisualHelper.IsHeadless || Time.time < nextColorCheck)
        {
            return;
        }

        nextColorCheck = Time.time + ColorCheckSeconds;
        int color = Zdo.GetInt(colorKey);
        int grey = GreyStep;
        int coat = care != null && care.HasCoat ? 1 : 0;
        if (color == shownColor && grey == shownGrey && coat == shownCoat)
        {
            return;
        }

        shownColor = color;
        shownGrey = grey;
        shownCoat = coat;
        for (int i = 0; i < bodies.Length; i++)
        {
            if (bodies[i] != null && originalMaterials[i] != null && originalMaterials[i].mainTexture != null)
            {
                bodies[i].sharedMaterial = DogRegistry.GetCoatMaterial(color, grey, coat == 1, originalMaterials[i]);
            }
        }
    }

    // Size follows age on every client. A puppy cannot be commanded and does not look for fights.
    private void ApplyStage()
    {
        float grown = Grown;
        float scale = Mathf.Lerp(PuppyScale, AdultScale, grown);
        if (!Mathf.Approximately(scale, shownScale))
        {
            shownScale = scale;
            transform.localScale = Vector3.one * scale;
        }

        bool puppy = grown < 1f;
        tameable.m_commandable = !puppy;
        ai.m_viewRange = puppy ? 0f : viewRange;
        ai.m_hearRange = puppy ? 0f : hearRange;
        ai.m_alertRange = puppy ? 0f : alertRange;
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

    // Once a game day for each kind of trouble, wherever the owner is.
    private void WarnOwner(float hunger, float day)
    {
        string key = hunger >= (DogSettings.StarveDays.Value - 3f) * day ? "whitehilt_dog_warn_starving"
            : Zdo.GetFloat(neglectKey) >= day ? "whitehilt_dog_warn_homeless"
            : hunger >= HungryWarningDays * day ? "whitehilt_dog_warn_hungry"
            : null;
        int today = EnvMan.instance.GetDay();
        if (key == null || Zdo.GetInt(warnedKey, -1) == today)
        {
            return;
        }

        Zdo.Set(warnedKey, today);
        DogRegistry.SendNotice(OwnerId, key, DogName, transform.position, DogRegistry.Anywhere);
    }

    private void Whine(float hunger, float day)
    {
        if (Time.time < nextWhine || (hunger < day && character.GetHealthPercentage() > 0.3f && !care.IsMiserable))
        {
            return;
        }

        nextWhine = Time.time + WhineInterval;
        PlaySound("dogwhine");
    }

    // Barks and warns the owner when an enemy comes near home, at most once in a while.
    private void Guard(Vector3 home)
    {
        if (Time.time < nextGuard)
        {
            return;
        }

        bool intruder = Character.GetAllCharacters().Any(other => other != character && !other.IsDead() && !other.IsPlayer() && !other.IsTamed()
            && BaseAI.IsEnemy(character, other) && Vector3.Distance(other.transform.position, home) <= GuardRadius);
        if (!intruder)
        {
            return;
        }

        nextGuard = Time.time + GuardCooldown;
        Bark();
        DogRegistry.SendNotice(OwnerId, "whitehilt_dog_guard", DogName, home, GuardNoticeRange);
    }

    // A grown dog that follows sits down when its master has stood still for a moment, and gets up when they walk on.
    // On a ship it boards with its master and sits on deck until they go ashore.
    private void UpdateSitting()
    {
        GameObject master = ai.GetFollowTarget();
        if (master == null || IsPuppy || activities.IsBusy)
        {
            masterStill = 0f;
            return;
        }

        Ship masterShip = FindShipUnder(master.transform.position);
        Ship ownShip = character.GetStandingOnShip();
        if (masterShip != null)
        {
            if (ownShip != masterShip && masterShip.GetSpeed() < BoardMaxShipSpeed
                && Vector3.Distance(transform.position, master.transform.position) <= BoardRange)
            {
                TeleportTo(master.transform.position - master.transform.forward * 1.2f + Vector3.up * 0.3f);
            }

            if (rest.Current != RestPose.Pose.Sit)
            {
                rest.SetPose(RestPose.Pose.Sit);
            }

            return;
        }

        if (ownShip != null && rest.Current == RestPose.Pose.Sit)
        {
            rest.SetPose(RestPose.Pose.None);
            return;
        }

        Vector3 position = master.transform.position;
        if ((position - masterPosition).sqrMagnitude > 0.04f)
        {
            masterPosition = position;
            masterStill = 0f;
        }
        else
        {
            masterStill += SitCheckSeconds;
        }

        float distance = Vector3.Distance(transform.position, position);
        RestPose.Pose pose = rest.Current;
        if (pose == RestPose.Pose.Sit && (masterStill == 0f || distance > SitRange + 1f))
        {
            rest.SetPose(RestPose.Pose.None);
        }
        else if (pose == RestPose.Pose.None && masterStill >= SitAfterSeconds && distance <= SitRange && character.GetVelocity().magnitude < 0.3f)
        {
            rest.SetPose(RestPose.Pose.Sit);
        }
    }

    // A dog house placed near home becomes the new home.
    private Vector3 UpdateHome(ZDO zdo)
    {
        Vector3 home = zdo.GetVec3(homeKey, transform.position);
        DogHomePiece house = DogHomePiece.FindNearest(DogHomeKind.House, home, HomeRadius);
        if (house != null && house.transform.position != home)
        {
            home = house.transform.position;
            zdo.Set(homeKey, home);
        }

        return home;
    }

    // Picks where the dog rests at home: beside its master's bed at night, in its own bed under a roof, or in its house
    // at night and in rain.
    private void UpdateRestGoal(bool atHome, Vector3 home, DogHomePiece house, DogHomePiece bed)
    {
        RestSpot spot = null;
        RestPose.Pose pose = RestPose.Pose.None;
        if (atHome && character.GetHealth() > 0f)
        {
            bool night = EnvMan.IsNight();
            Player master = Player.GetPlayer(OwnerId);

            // Only a bed within the home area, or the dog would stop counting as home once it lay down there.
            if (night && master != null && master.InBed() && Vector3.Distance(master.transform.position, home) <= HomeRadius)
            {
                spot = RestSpot.Beside(master);
                pose = RestPose.Pose.Sleep;
            }
            else if (night && bed != null)
            {
                spot = RestSpot.In(bed);
                pose = RestPose.Pose.Sleep;
            }
            else if ((night || EnvMan.IsWet()) && house != null)
            {
                spot = RestSpot.In(house);
                pose = night ? RestPose.Pose.Sleep : RestPose.Pose.Lie;
            }
        }

        if ((spot?.Key == goal?.Key) && pose == goalPose)
        {
            return;
        }

        // Only get up from a rest this care started; the test command's rest is left alone.
        if (rest.IsResting && Zdo.GetBool(goalRestKey))
        {
            rest.SetPose(RestPose.Pose.None);
        }

        Zdo.Set(goalRestKey, false);
        goal = spot;
        goalPose = pose;
        goalWalkTime = 0f;
    }

    // Feeds the dog. Bone broth does more: fed for two days, half its health back, no poison and faster growth for a
    // day.
    private void OnAte(ItemDrop food)
    {
        if (!nview.IsOwner())
        {
            return;
        }

        Happy();
        if (food == null || Utils.GetPrefabName(food.gameObject) != DogRegistry.DogFoodPrefabName)
        {
            Zdo.Set(hungerKey, 0f);
            return;
        }

        // Bone broth: fed for two days, half the health back, no poison and double growth for a day.
        float day = DaySeconds;
        Zdo.Set(hungerKey, -day);
        Zdo.Set(boostKey, ZNet.instance.GetTime().Ticks + System.TimeSpan.FromSeconds(day).Ticks);
        character.Heal(character.GetMaxHealth() * 0.5f);
        care.CurePoison();
    }

    private static Ship FindShipUnder(Vector3 position)
    {
        return Physics.Raycast(position + Vector3.up, Vector3.down, out RaycastHit hit, 3f, shipLayers)
            ? hit.collider.GetComponentInParent<Ship>()
            : null;
    }

    private void RPC_Sound(long sender, string sound)
    {
        // Without its own bark in the bundle, the wolf's alert sound stands in.
        if (DogRegistry.PlaySound(sound, transform.position) <= 0f && sound == "dogbark")
        {
            ai.m_alertedEffects.Create(transform.position, transform.rotation, transform);
        }
    }

    private void RPC_Happy(long sender)
    {
        expression?.MakeHappy(HappySeconds);
        DogRegistry.PlaySound("doghappy", transform.position);
        care?.OnHappy();
    }

    private void RPC_Xp(long sender, float xp)
    {
        if (nview.IsOwner() && !IsPuppy)
        {
            Zdo.Set(xpKey, Zdo.GetFloat(xpKey) + xp);
            UpdateMaxHealth();
        }
    }

    private void RPC_Collar(long sender, int collar)
    {
        if (nview.IsOwner())
        {
            Zdo.Set(collarKey, collar);
        }
    }

    private void RPC_Home(long sender, Vector3 position)
    {
        if (nview.IsOwner())
        {
            Zdo.Set(homeKey, position);
        }
    }

    // Once a night, with another player's dog nearby. The dog of the lower player ID decides, so a pair is only tried once.
    private void TryLitter(Vector3 home, bool fed)
    {
        int today = EnvMan.instance.GetDay();
        if (!DogSettings.Litters.Value || !EnvMan.IsNight() || !fed || !CanHaveLitter || Zdo.GetInt(litterTryKey, -1) == today
            || today - Zdo.GetInt(litterDayKey, -LitterCooldownDays) < LitterCooldownDays)
        {
            return;
        }

        long ownerId = OwnerId;
        DogCompanion partner = dogs.FirstOrDefault(other => other != null && other != this && other.OwnerId > ownerId && other.CanHaveLitter
            && Vector3.Distance(other.transform.position, transform.position) <= LitterRange);
        if (partner == null)
        {
            return;
        }

        Zdo.Set(litterTryKey, today);
        if (Random.value >= LitterChance)
        {
            return;
        }

        Zdo.Set(litterDayKey, today);
        DogHomePiece house = DogHomePiece.FindNearest(DogHomeKind.House, home, HomeRadius);
        Vector3 nest = house != null ? house.ApproachPosition : transform.position;
        int color = Random.value < 0.5f ? Zdo.GetInt(colorKey) : partner.Zdo.GetInt(colorKey);
        DogRegistry.DropItem(DogRegistry.GetPuppyPrefab(color), 1, nest);
        string names = DogName + " & " + partner.DogName;
        DogRegistry.SendNotice(ownerId, "whitehilt_dog_litter", names, nest, DogRegistry.Anywhere);
        DogRegistry.SendNotice(partner.OwnerId, "whitehilt_dog_litter", names, nest, DogRegistry.Anywhere);
        Happy();
        partner.Happy();
    }

    private void Starve()
    {
        Zdo.Set(hungerKey, float.MaxValue);
        Die();
    }

    private void Die()
    {
        HitData hit = new() { m_point = transform.position, m_dir = Vector3.down };
        hit.m_damage.m_damage = 1E+07f;
        character.Damage(hit);
    }

    // Tells the owner once that the dog has grown old, and ends its life when its time has come. True when it died.
    private bool UpdateOldAge(bool atHome)
    {
        float life = LifeDays;
        if (life <= 0f || !IsOld)
        {
            return false;
        }

        if (!Zdo.GetBool(oldNoticeKey))
        {
            Zdo.Set(oldNoticeKey, true);
            DogRegistry.SendNotice(OwnerId, "whitehilt_dog_warn_old", DogName, transform.position, DogRegistry.Anywhere);
        }

        float lived = DaysLived;
        if (lived < life || (lived < life + OldAgeGraceDays && !(atHome && rest.IsResting)))
        {
            return false;
        }

        Zdo.Set(oldAgeKey, true);
        Die();
        return true;
    }

    private void RunAway()
    {
        DogRegistry.SendGone(OwnerId, "whitehilt_dog_ranaway", DogName);
        ZNetScene.instance.Destroy(gameObject);
    }

    // When the dog dies: drops its remains with its name and age for a gravestone, and its collar and coat. A dog that
    // dies of old age leaves a puppy in its house.
    private void OnDeath()
    {
        if (!nview.IsOwner())
        {
            return;
        }

        string dogName = DogName;
        bool starved = Zdo.GetFloat(hungerKey) >= DogSettings.StarveDays.Value * DaySeconds;
        ItemDrop remains = PrefabManager.Instance.GetPrefab(DogRegistry.RemainsPrefabName)?.GetComponent<ItemDrop>();
        if (remains != null)
        {
            // Days in the world since it was let out, for the gravestone.
            long now = ZNet.instance.GetTime().Ticks;
            double lived = System.TimeSpan.FromTicks(now - Zdo.GetLong(bornKey, now)).TotalSeconds / DaySeconds;
            ItemDrop.ItemData data = remains.m_itemData.Clone();
            data.m_dropPrefab = remains.gameObject;
            data.m_stack = 1;
            DogRegistry.SetInscription(data, dogName, Mathf.Max(0, (int)lived));
            ItemDrop.DropItem(data, 1, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        }

        // The collar and coat are not lost with the dog.
        string collar = DogRegistry.GetCollarPrefab(Zdo.GetInt(collarKey));
        GameObject collarPrefab = collar != null ? PrefabManager.Instance.GetPrefab(collar) : null;
        if (collarPrefab != null)
        {
            Instantiate(collarPrefab, transform.position + Vector3.up * 0.7f, Quaternion.identity);
        }

        if (care.HasCoat)
        {
            DogRegistry.DropItem(DogRegistry.CoatPrefabName, 1, transform.position + Vector3.up * 0.4f);
        }

        bool oldAge = Zdo.GetBool(oldAgeKey);
        DogRegistry.SendGone(OwnerId, starved ? "whitehilt_dog_starved" : oldAge ? "whitehilt_dog_old_age" : "whitehilt_dog_died", dogName);

        // A dog that lived out its life leaves a puppy of its own colour in the dog house.
        if (oldAge)
        {
            DogHomePiece house = DogHomePiece.FindNearest(DogHomeKind.House, Home, HomeRadius);
            Vector3 nest = house != null ? house.RestPosition : transform.position;
            DogRegistry.DropItem(DogRegistry.GetPuppyPrefab(Zdo.GetInt(colorKey)), 1, nest);
            DogRegistry.SendNotice(OwnerId, "whitehilt_dog_legacy", dogName, nest, DogRegistry.Anywhere);
        }
    }

    // Where the dog lies down: in its bed or house, or on the floor beside its sleeping master.
    private sealed class RestSpot
    {
        private const float BesideDistance = 1.3f;

        private static readonly int floorLayers = LayerMask.GetMask("Default", "static_solid", "terrain", "piece");

        public Object Key { get; private set; }

        public Vector3 Approach { get; private set; }

        public Vector3 Position { get; private set; }

        public Quaternion Rotation { get; private set; }

        public static RestSpot In(DogHomePiece piece)
        {
            return new RestSpot { Key = piece, Approach = piece.ApproachPosition, Position = piece.RestPosition, Rotation = piece.RestRotation };
        }

        public static RestSpot Beside(Player master)
        {
            Vector3 origin = master.transform.position;
            Vector3 side = master.transform.right * BesideDistance;
            Vector3 floor = FindFloor(origin + side) ?? FindFloor(origin - side) ?? origin;
            return new RestSpot { Key = master, Approach = floor, Position = floor, Rotation = master.transform.rotation };
        }

        private static Vector3? FindFloor(Vector3 point)
        {
            if (Physics.Raycast(point + Vector3.up * 1.2f, Vector3.down, out RaycastHit hit, 3f, floorLayers)
                && hit.collider.GetComponentInParent<Bed>() == null)
            {
                return hit.point;
            }

            return null;
        }
    }
}
