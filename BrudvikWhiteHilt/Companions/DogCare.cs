using BrudvikWhiteHilt.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Companions;

/// <summary>
/// The dog's well-being, run by the ZDO owner: its mood, the cold, swamp poison and injuries, its coat, and the small
/// things it does of its own accord (shaking off water, yawning, stretching, scratching). Everything lives in the ZDO,
/// so every client shows it.
/// </summary>
public sealed class DogCare : MonoBehaviour
{
    /// <summary>
    /// Mood of a new dog.
    /// </summary>
    public const float DefaultMood = 0.7f;

    private const string ActionRpc = "WhiteHilt_DogAction";
    private const string MoodRpc = "WhiteHilt_DogMood";
    private const string CareRpc = "WhiteHilt_DogCare";
    private const float CompanyRange = 30f;
    private const float MoodGainPerDay = 0.6f;
    private const float StrangerGainPerDay = 0.2f;
    private const float LonelinessPerDay = 0.3f;
    private const float DiscomfortPerDay = 0.3f;
    private const float HappyMood = 0.05f;
    private const float LimpHealth = 0.35f;
    private const float LimpRunFactor = 0.6f;
    private const float LimpWalkFactor = 0.8f;
    private static float RestHealPerTick => DogSettings.RestHealPerTick.Value;
    private static float PoisonChance => DogSettings.SwampPoisonChance.Value;
    private const float PoisonDays = 0.1f;
    private const float PoisonPerTick = 0.015f;
    private const float PoisonFloor = 0.1f;
    private const float ColdPerTick = 0.01f;
    private const float ColdFloor = 0.2f;
    private static float BandageHeal => DogSettings.BandageHeal.Value;
    private const float ShakeDelay = 0.8f;
    private const float YawnChance = 0.06f;
    private const float ScratchChance = 0.04f;
    private const float IdleScratchChance = 0.015f;
    private const float PoseDelaySeconds = 1f;

    private static readonly int moodKey = "whitehilt_dog_mood".GetStableHashCode();
    private static readonly int poisonKey = "whitehilt_dog_poison".GetStableHashCode();
    private static readonly int coldKey = "whitehilt_dog_cold".GetStableHashCode();
    private static readonly int coatKey = "whitehilt_dog_coat".GetStableHashCode();
    private static readonly int actionKey = "whitehilt_dog_action".GetStableHashCode();
    private static readonly int actionStartKey = "whitehilt_dog_actionstart".GetStableHashCode();
    private static readonly int holdUntilKey = "whitehilt_dog_holduntil".GetStableHashCode();

    private ZNetView nview;
    private Character character;
    private MonsterAI ai;
    private RestPose rest;
    private DogCompanion dog;
    private float walkSpeed;
    private float runSpeed;
    private bool wasInWater;
    private bool wasRainedOn;
    private float shakeAt = -1f;
    private RestPose.Pose lastPose;
    private RestPose.Pose poseBeforeAction;
    private bool actionChangedPose;

    /// <summary>
    /// What the dog needs from its owner's pack.
    /// </summary>
    public enum CareItem
    {
        /// <summary>A treat: a better mood.</summary>
        Treat = 0,

        /// <summary>A bandage: half its health back and no poison.</summary>
        Bandage = 1,

        /// <summary>A coat: no more freezing.</summary>
        Coat = 2
    }

    /// <summary>
    /// Mood from 0 (lonely) to 1 (happy).
    /// </summary>
    public float Mood => Zdo?.GetFloat(moodKey, DefaultMood) ?? DefaultMood;

    /// <summary>
    /// True while swamp water poisons the dog.
    /// </summary>
    public bool IsPoisoned => Zdo != null && ZNet.instance != null && ZNet.instance.GetTime().Ticks < Zdo.GetLong(poisonKey);

    /// <summary>
    /// True while the dog freezes.
    /// </summary>
    public bool IsCold => Zdo?.GetBool(coldKey) ?? false;

    /// <summary>
    /// True when the dog wears its coat.
    /// </summary>
    public bool HasCoat => Zdo?.GetBool(coatKey) ?? false;

    /// <summary>
    /// True while the dog is hurt enough to limp.
    /// </summary>
    public bool IsLimping => character != null && !character.IsDead() && character.GetHealthPercentage() < LimpHealth;

    /// <summary>
    /// True when the dog has something to whine about.
    /// </summary>
    public bool IsMiserable => IsPoisoned || IsCold || Mood < 0.25f;

    /// <summary>
    /// Damage multiplier from the dog's mood: a happy dog fights harder.
    /// </summary>
    public float MoodDamageFactor => 0.9f + 0.2f * Mood;

    /// <summary>
    /// True while an action holds the dog still.
    /// </summary>
    public bool IsActing => GetAction(out _) != DogAction.None;

    private ZDO Zdo => nview != null && nview.IsValid() ? nview.GetZDO() : null;

    /// <summary>
    /// The action the dog is doing and how far into it it is.
    /// </summary>
    /// <param name="elapsed">Seconds since the action began; below 0 while the dog gets into position.</param>
    /// <returns>The action, or <see cref="DogAction.None"/>.</returns>
    public DogAction GetAction(out float elapsed)
    {
        elapsed = 0f;
        ZDO zdo = Zdo;
        DogAction action = zdo != null ? (DogAction)zdo.GetInt(actionKey) : DogAction.None;
        if (action == DogAction.None || ZNet.instance == null)
        {
            return DogAction.None;
        }

        elapsed = (float)System.TimeSpan.FromTicks(ZNet.instance.GetTime().Ticks - zdo.GetLong(actionStartKey)).TotalSeconds;
        return elapsed <= DogActions.Duration(action) ? action : DogAction.None;
    }

    /// <summary>
    /// Asks the dog's owner to start an action.
    /// </summary>
    /// <param name="action">The action.</param>
    public void Request(DogAction action)
    {
        if (Zdo != null)
        {
            nview.InvokeRPC(ActionRpc, (int)action);
        }
    }

    /// <summary>
    /// Changes the dog's mood, on its owner.
    /// </summary>
    /// <param name="delta">How much, from -1 to 1.</param>
    public void ChangeMood(float delta)
    {
        if (Zdo != null)
        {
            nview.InvokeRPC(MoodRpc, delta);
        }
    }

    /// <summary>
    /// Gives the dog a treat, a bandage or its coat, on its owner.
    /// </summary>
    /// <param name="item">What is given.</param>
    public void Give(CareItem item)
    {
        if (Zdo != null)
        {
            nview.InvokeRPC(CareRpc, (int)item);
        }
    }

    /// <summary>
    /// Keeps the dog in a resting pose for a while, then lets it get up. Call on the owner.
    /// </summary>
    /// <param name="pose">The pose.</param>
    /// <param name="seconds">How long.</param>
    public void HoldPose(RestPose.Pose pose, float seconds)
    {
        rest.SetPose(pose);
        Zdo?.Set(holdUntilKey, ZNet.instance.GetTime().Ticks + System.TimeSpan.FromSeconds(seconds).Ticks);
    }

    /// <summary>
    /// Care notes for the hover text.
    /// </summary>
    /// <returns>Translation tokens, one per line.</returns>
    public IEnumerable<string> GetStatusTokens()
    {
        float mood = Mood;
        yield return Translations.Token(mood >= 0.75f ? "whitehilt_dog_mood_happy" : mood >= 0.5f ? "whitehilt_dog_mood_content"
            : mood >= 0.25f ? "whitehilt_dog_mood_sad" : "whitehilt_dog_mood_lonely");
        if (IsPoisoned)
        {
            yield return Translations.Token("whitehilt_dog_poisoned");
        }

        if (IsCold)
        {
            yield return Translations.Token("whitehilt_dog_cold");
        }

        if (IsLimping)
        {
            yield return Translations.Token("whitehilt_dog_injured");
        }
    }

    /// <summary>
    /// The slow part of the care: mood, cold, poison and healing. Called from the dog's care tick on the owner.
    /// </summary>
    /// <param name="dt">Game seconds since the last tick.</param>
    /// <param name="day">Length of a game day in seconds.</param>
    /// <param name="atHome">True when the dog is at home and not following.</param>
    /// <param name="hungry">True when the dog has not eaten today.</param>
    public void Tick(float dt, float day, bool atHome, bool hungry)
    {
        ZDO zdo = Zdo;
        Vector3 position = transform.position;
        Heightmap.Biome biome = Heightmap.FindBiome(position);

        bool cold = !HasCoat && (biome == Heightmap.Biome.Mountain || biome == Heightmap.Biome.DeepNorth || EnvMan.IsFreezing())
            && EffectArea.IsPointInsideArea(position, EffectArea.Type.Heat, 1f) == null;
        if (cold != zdo.GetBool(coldKey))
        {
            zdo.Set(coldKey, cold);
        }

        long now = ZNet.instance.GetTime().Ticks;
        if (biome == Heightmap.Biome.Swamp && character.InWater() && !IsPoisoned && Random.value < PoisonChance)
        {
            zdo.Set(poisonKey, now + System.TimeSpan.FromSeconds(PoisonDays * day).Ticks);
            dog.PlaySound("dogwhine");
        }

        float health = character.GetHealth();
        float max = character.GetMaxHealth();
        if (IsPoisoned)
        {
            health = Mathf.Max(Mathf.Min(health, max * PoisonFloor), health - max * PoisonPerTick);
        }

        if (cold)
        {
            health = Mathf.Max(Mathf.Min(health, max * ColdFloor), health - max * ColdPerTick);
        }
        else if (!IsPoisoned && atHome && rest.IsResting)
        {
            health = Mathf.Min(max, health + max * RestHealPerTick);
        }

        if (!Mathf.Approximately(health, character.GetHealth()))
        {
            character.SetHealth(health);
        }

        UpdateMood(dt, day, hungry || cold || IsPoisoned);
        UpdateSpeed();
        StartHabit(atHome);
    }

    /// <summary>
    /// The quick part: shaking off water, ending actions and held poses. Called twice a second on the owner.
    /// </summary>
    public void FastUpdate()
    {
        bool inWater = character.InWater();
        Cover.GetCoverForPoint(transform.position + Vector3.up, out _, out bool underRoof);
        bool rainedOn = EnvMan.IsWet() && !underRoof;
        if ((wasInWater && !inWater) || (wasRainedOn && !rainedOn && underRoof))
        {
            shakeAt = Time.time + ShakeDelay;
        }

        wasInWater = inWater;
        wasRainedOn = rainedOn;
        if (shakeAt > 0f && Time.time >= shakeAt && !inWater)
        {
            shakeAt = -1f;
            StartAction(DogAction.Shake);
        }

        // Stretches when it gets up from a sleep.
        RestPose.Pose pose = rest.Current;
        if (lastPose == RestPose.Pose.Sleep && pose == RestPose.Pose.None && !IsActing)
        {
            StartAction(DogAction.Stretch);
        }

        lastPose = pose;
        EndFinishedAction();
        EndHeldPose();
    }

    /// <summary>
    /// Holds the dog still while it does an action. Called from the AI tick on the owner.
    /// </summary>
    public void UpdateAI()
    {
        if (IsActing && !rest.IsResting)
        {
            ai.StopMoving();
        }
    }

    /// <summary>
    /// Clears poison, e.g. after bone broth. Call on the owner.
    /// </summary>
    public void CurePoison()
    {
        Zdo?.Set(poisonKey, 0L);
    }

    /// <summary>
    /// A small lift of the mood whenever the dog is glad. Call on the owner.
    /// </summary>
    public void OnHappy()
    {
        if (Zdo != null && nview.IsOwner())
        {
            SetMood(Mood + HappyMood);
        }
    }

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        character = GetComponent<Character>();
        ai = GetComponent<MonsterAI>();
        rest = GetComponent<RestPose>();
        dog = GetComponent<DogCompanion>();
        walkSpeed = character.m_speed;
        runSpeed = character.m_runSpeed;
        if (nview != null && nview.GetZDO() != null)
        {
            nview.Register<int>(ActionRpc, RPC_Action);
            nview.Register<float>(MoodRpc, RPC_Mood);
            nview.Register<int>(CareRpc, RPC_Care);
        }
    }

    // Raises the dog's mood with its master near, less with strangers, and lowers it when alone or uncomfortable.
    private void UpdateMood(float dt, float day, bool uncomfortable)
    {
        Vector3 position = transform.position;
        Player master = Player.GetPlayer(dog.OwnerId);
        float perDay;
        if (master != null && Vector3.Distance(master.transform.position, position) <= CompanyRange)
        {
            perDay = MoodGainPerDay;
        }
        else
        {
            List<Player> near = new();
            Player.GetPlayersInRange(position, CompanyRange, near);
            perDay = near.Count > 0 ? StrangerGainPerDay : -LonelinessPerDay;
        }

        if (uncomfortable)
        {
            perDay -= DiscomfortPerDay;
        }

        SetMood(Mood + perDay * dt / day);
    }

    private void SetMood(float mood)
    {
        Zdo.Set(moodKey, Mathf.Clamp01(mood));
    }

    private void UpdateSpeed()
    {
        bool limping = IsLimping;
        character.m_speed = limping ? walkSpeed * LimpWalkFactor : walkSpeed;
        character.m_runSpeed = limping ? runSpeed * LimpRunFactor : runSpeed;
    }

    // Now and then, while it rests or stands about at home.
    private void StartHabit(bool atHome)
    {
        if (IsActing || character.IsDead() || dog.IsCarrying)
        {
            return;
        }

        RestPose.Pose pose = rest.Current;
        if ((pose == RestPose.Pose.Lie || pose == RestPose.Pose.Sit) && Random.value < YawnChance)
        {
            StartAction(DogAction.Yawn);
        }
        else if (pose == RestPose.Pose.Sit && Random.value < ScratchChance)
        {
            StartAction(DogAction.Scratch);
        }
        else if (pose == RestPose.Pose.None && atHome && character.GetVelocity().magnitude < 0.2f && Random.value < IdleScratchChance)
        {
            StartAction(DogAction.Scratch);
        }
    }

    // Starts an action, first taking the pose it needs. The start is set a little into the future when the pose
    // changes, so the action begins once the dog has got there.
    private void StartAction(DogAction action)
    {
        ZDO zdo = Zdo;
        if (zdo == null || character.IsDead())
        {
            return;
        }

        EndFinishedAction(force: true);
        RestPose.Pose pose = DogActions.PoseFor(action);
        RestPose.Pose current = rest.Current;

        // A sleeping dog rolls over as it lies, instead of getting up to lie down again.
        if (pose == RestPose.Pose.Lie && current == RestPose.Pose.Sleep)
        {
            pose = current;
        }

        float delay = 0f;
        actionChangedPose = pose != RestPose.Pose.None && current != pose;
        if (actionChangedPose)
        {
            poseBeforeAction = current;
            rest.SetPose(pose);
            delay = PoseDelaySeconds;
        }

        zdo.Set(actionKey, (int)action);
        zdo.Set(actionStartKey, ZNet.instance.GetTime().Ticks + System.TimeSpan.FromSeconds(delay).Ticks);
    }

    private void EndFinishedAction(bool force = false)
    {
        ZDO zdo = Zdo;
        if (zdo == null || zdo.GetInt(actionKey) == 0 || (!force && IsActing))
        {
            return;
        }

        zdo.Set(actionKey, 0);
        if (actionChangedPose)
        {
            actionChangedPose = false;
            rest.SetPose(poseBeforeAction);
        }
    }

    private void EndHeldPose()
    {
        long until = Zdo.GetLong(holdUntilKey);
        if (until != 0L && ZNet.instance.GetTime().Ticks >= until)
        {
            Zdo.Set(holdUntilKey, 0L);
            if (!IsActing)
            {
                rest.SetPose(RestPose.Pose.None);
            }
        }
    }

    private void RPC_Action(long sender, int action)
    {
        if (nview.IsOwner())
        {
            StartAction((DogAction)action);
        }
    }

    private void RPC_Mood(long sender, float delta)
    {
        if (nview.IsOwner())
        {
            SetMood(Mood + delta);
        }
    }

    // On the dog's owner: applies a care item: a treat cheers the dog, a bandage heals it and cures poison, a coat
    // keeps it warm.
    private void RPC_Care(long sender, int item)
    {
        if (!nview.IsOwner())
        {
            return;
        }

        switch ((CareItem)item)
        {
            case CareItem.Treat:
                SetMood(Mood + 0.08f);
                break;
            case CareItem.Bandage:
                Zdo.Set(poisonKey, 0L);
                character.Heal(character.GetMaxHealth() * BandageHeal);
                break;
            case CareItem.Coat:
                Zdo.Set(coatKey, true);
                Zdo.Set(coldKey, false);
                break;
        }

        dog.Happy();
    }
}
