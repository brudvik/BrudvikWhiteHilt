using Jotunn.Managers;
using UnityEngine;

namespace BrudvikWhiteHilt.Companions;

/// <summary>
/// What the dog does besides following and resting, on the ZDO owner: it greets its master coming home, a puppy plays
/// around its master at home, a grown dog fetches a thrown stick, and at home it drinks from its water bowl and now and
/// then digs something up. These steer the dog over vanilla movement.
/// </summary>
public sealed class DogActivities : MonoBehaviour
{
    /// <summary>
    /// ZDO key: true while the dog carries the stick in its mouth.
    /// </summary>
    public static readonly int CarryKey = "whitehilt_dog_carry".GetStableHashCode();

    /// <summary>
    /// ZDO key: true while the dog digs.
    /// </summary>
    public static readonly int DigKey = "whitehilt_dog_dig".GetStableHashCode();

    private const string FetchRpc = "WhiteHilt_DogFetch";
    private const float GreetSeconds = 10f;
    private const float PlaySeconds = 12f;
    private const float PlayCooldown = 90f;
    private const float PlayChance = 0.35f;
    private const float PlayRange = 12f;
    private const float PlayRadius = 3f;
    private const float AwayRange = 40f;
    private const float HomeRange = 25f;
    private const float FetchSeconds = 30f;
    private const float StickWaitSeconds = 3f;
    private static float FetchXp => DogSettings.FetchXp.Value;
    private const float DrinkEveryDays = 0.25f;
    private const float DrinkChance = 0.3f;
    private const float DrinkSeconds = 3.5f;
    private const float LapSeconds = 1.2f;
    private static float DigChance => DogSettings.DigChance.Value;
    private const float DigRadius = 6f;
    private const float DigSeconds = 4f;
    private const float DigNoticeRange = 50f;

    // From the dog's centre to its nose, at scale 1.
    private const float NoseReach = 0.6f;

    // Time to walk there and do it, whichever activity.
    private const float ErrandSeconds = 40f;

    private static readonly int drankKey = "whitehilt_dog_drank".GetStableHashCode();
    private static readonly int digDayKey = "whitehilt_dog_digday".GetStableHashCode();
    // Pieces too, so a spot under a floor or a roof is not taken for open ground.
    private static readonly int groundLayers = LayerMask.GetMask("Default", "static_solid", "terrain", "piece");

    // What a dog digs up, by weight.
    private static readonly (string Prefab, int Min, int Max, float Weight)[] finds =
    {
        ("BoneFragments", 1, 3, 45f),
        ("Coins", 5, 15, 25f),
        ("Flint", 1, 2, 15f),
        ("Amber", 1, 1, 10f),
        ("AmberPearl", 1, 1, 5f)
    };

    private ZNetView nview;
    private Character character;
    private MonsterAI ai;
    private RestPose rest;
    private DogCompanion dog;
    private Activity activity;
    private float started;
    private float nextPlay;
    private float circle;
    private float nextJump;
    private bool masterAway = true;
    private ZDOID stickId;
    private long throwerId;
    private DogHomePiece bowl;
    private Vector3 digSpot;
    private float actionStarted = -1f;
    private float nextLap;

    private enum Activity
    {
        None,
        Greet,
        Play,
        Chase,
        Return,
        Drink,
        Dig
    }

    /// <summary>
    /// True while an activity steers the dog.
    /// </summary>
    public bool IsBusy => activity != Activity.None;

    /// <summary>
    /// True when the dog has drunk from its water bowl within <paramref name="seconds"/> of game time.
    /// </summary>
    /// <param name="seconds">Game seconds.</param>
    /// <returns>Whether it drank recently.</returns>
    public bool DrankWithin(float seconds)
    {
        ZDO zdo = nview != null && nview.IsValid() ? nview.GetZDO() : null;
        long drank = zdo?.GetLong(drankKey) ?? 0L;
        return drank != 0L && System.TimeSpan.FromTicks(ZNet.instance.GetTime().Ticks - drank).TotalSeconds <= seconds;
    }

    /// <summary>
    /// Sends the dog after a thrown stick, on the dog's owner.
    /// </summary>
    /// <param name="stick">ZDO of the stick on the ground.</param>
    /// <param name="thrower">Player ID of whoever threw it.</param>
    public void Fetch(ZDOID stick, long thrower)
    {
        if (nview != null && nview.IsValid())
        {
            nview.InvokeRPC(FetchRpc, stick, thrower);
        }
    }

    /// <summary>
    /// Starts greeting, playing, drinking or digging when the time is right. Called from the dog's care tick on the owner.
    /// </summary>
    /// <param name="home">Where the dog lives.</param>
    /// <param name="atHome">True when the dog is at home and not following.</param>
    /// <param name="waterBowl">The water bowl at home, or null.</param>
    /// <param name="daySeconds">Length of a game day in seconds.</param>
    public void Tick(Vector3 home, bool atHome, DogHomePiece waterBowl, float daySeconds)
    {
        Player master = Player.GetPlayer(dog.OwnerId);
        float masterFromHome = master != null ? Vector3.Distance(master.transform.position, home) : float.MaxValue;
        bool arrived = masterAway && masterFromHome <= HomeRange;
        if (masterFromHome > AwayRange)
        {
            masterAway = true;
        }
        else if (masterFromHome <= HomeRange)
        {
            masterAway = false;
        }

        if (IsBusy || !atHome || EnvMan.IsNight())
        {
            return;
        }

        if (master != null && arrived && !dog.IsPuppy)
        {
            Begin(Activity.Greet);
            dog.Happy();
        }
        else if (master != null && dog.IsPuppy && Time.time >= nextPlay && Vector3.Distance(master.transform.position, transform.position) <= PlayRange
            && Random.value < PlayChance)
        {
            nextPlay = Time.time + PlayCooldown;
            Begin(Activity.Play);
            dog.Happy();
        }
        else if (waterBowl != null && !DrankWithin(DrinkEveryDays * daySeconds) && Random.value < DrinkChance)
        {
            bowl = waterBowl;
            Begin(Activity.Drink);
        }
        else
        {
            TryDig(home);
        }
    }

    /// <summary>
    /// Steers the dog. Called from the AI tick on the owner, after vanilla chose its movement.
    /// </summary>
    /// <param name="dt">Frame time.</param>
    public void UpdateActivity(float dt)
    {
        if (!IsBusy || !nview.IsOwner())
        {
            return;
        }

        if (character.IsDead() || Time.time - started > Duration(activity))
        {
            End();
            return;
        }

        switch (activity)
        {
            case Activity.Greet:
                Greet(dt);
                break;
            case Activity.Play:
                Play(dt);
                break;
            case Activity.Chase:
                Chase(dt);
                break;
            case Activity.Return:
                Return(dt);
                break;
            case Activity.Drink:
                Drink(dt);
                break;
            case Activity.Dig:
                Dig(dt);
                break;
        }
    }

    private static float Duration(Activity activity)
    {
        return activity switch
        {
            Activity.Play => PlaySeconds,
            Activity.Greet => GreetSeconds,
            Activity.Drink or Activity.Dig => ErrandSeconds,
            _ => FetchSeconds
        };
    }

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        character = GetComponent<Character>();
        ai = GetComponent<MonsterAI>();
        rest = GetComponent<RestPose>();
        dog = GetComponent<DogCompanion>();
        if (nview != null && nview.GetZDO() != null)
        {
            nview.Register<ZDOID, long>(FetchRpc, RPC_Fetch);
        }
    }

    private void Begin(Activity next)
    {
        activity = next;
        started = Time.time;
        actionStarted = -1f;
        circle = Random.Range(0f, Mathf.PI * 2f);
        rest.SetPose(RestPose.Pose.None);
    }

    private void End()
    {
        ZDO zdo = nview.GetZDO();
        if (zdo.GetBool(CarryKey))
        {
            DropStick(transform.position + transform.forward * 0.6f);
        }

        if (zdo.GetBool(DigKey))
        {
            zdo.Set(DigKey, false);
        }

        activity = Activity.None;
    }

    private void Greet(float dt)
    {
        Player master = Player.GetPlayer(dog.OwnerId);
        if (master == null || ai.MoveTo(dt, master.transform.position, 1.8f, run: true))
        {
            End();
        }
    }

    // Round and round its master, with the odd jump.
    private void Play(float dt)
    {
        Player master = Player.GetPlayer(dog.OwnerId);
        if (master == null)
        {
            End();
            return;
        }

        circle += dt * 1.6f;
        Vector3 target = master.transform.position + new Vector3(Mathf.Cos(circle), 0f, Mathf.Sin(circle)) * PlayRadius;
        ai.MoveTo(dt, target, 0.5f, run: true);
        if (Time.time >= nextJump)
        {
            nextJump = Time.time + Random.Range(1.5f, 3f);
            nview.GetZDO().Set(DogExpression.HopKey, ZNet.instance.GetTime().Ticks);
        }
    }

    // Runs to the thrown stick and picks it up. The stick may not have reached this machine yet, so the dog waits a
    // while before giving up.
    private void Chase(float dt)
    {
        GameObject stick = ZNetScene.instance.FindInstance(stickId);
        if (stick == null)
        {
            // The thrown stick may not have reached this client yet.
            if (Time.time - started > StickWaitSeconds)
            {
                End();
            }

            return;
        }

        if (!ai.MoveTo(dt, stick.transform.position, 0.9f, run: true))
        {
            return;
        }

        ZNetView stickView = stick.GetComponent<ZNetView>();
        if (stickView != null && stickView.IsValid())
        {
            stickView.ClaimOwnership();
            ZNetScene.instance.Destroy(stick);
        }

        nview.GetZDO().Set(CarryKey, true);
        activity = Activity.Return;
    }

    // Brings the stick back to the thrower and drops it before them, for experience and a happy dog.
    private void Return(float dt)
    {
        Player thrower = Player.GetPlayer(throwerId);
        if (thrower == null)
        {
            End();
            return;
        }

        if (!ai.MoveTo(dt, thrower.transform.position, 1.8f, run: true))
        {
            return;
        }

        DropStick(thrower.transform.position + thrower.transform.forward * 0.8f);
        activity = Activity.None;
        dog.AddXp(FetchXp);
        dog.Happy();
    }

    private void DropStick(Vector3 position)
    {
        nview.GetZDO().Set(CarryKey, false);
        DogRegistry.DropItem(DogRegistry.StickPrefabName, 1, position);
    }

    // Walks up to the bowl, nose over it at any size, faces it and laps for a while.
    private void Drink(float dt)
    {
        if (bowl == null)
        {
            End();
            return;
        }

        Vector3 center = bowl.transform.position;
        if (actionStarted < 0f)
        {
            Vector3 away = Vector3.ProjectOnPlane(transform.position - center, Vector3.up);
            Vector3 stand = center + (away.sqrMagnitude > 0.01f ? away.normalized : bowl.transform.forward) * (NoseReach * transform.localScale.x);
            if (!ai.MoveTo(dt, stand, 0.1f, run: false))
            {
                return;
            }

            actionStarted = Time.time;
        }

        HoldStill(center);
        if (Time.time >= nextLap)
        {
            nextLap = Time.time + LapSeconds;
            ai.m_animator.SetTrigger("consume");
        }

        if (Time.time - actionStarted >= DrinkSeconds)
        {
            nview.GetZDO().Set(drankKey, ZNet.instance.GetTime().Ticks);
            activity = Activity.None;
        }
    }

    // Once a game day at most: sniff out a spot near home, dig, and leave what turned up.
    private void TryDig(Vector3 home)
    {
        ZDO zdo = nview.GetZDO();
        int today = EnvMan.instance.GetDay();
        if (zdo.GetInt(digDayKey, -1) == today)
        {
            return;
        }

        zdo.Set(digDayKey, today);
        if (Random.value >= DigChance)
        {
            return;
        }

        Vector2 offset = Random.insideUnitCircle * DigRadius;
        Vector3 point = home + new Vector3(offset.x, 0f, offset.y);
        if (!Physics.Raycast(point + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f, groundLayers)
            || hit.collider.GetComponentInParent<Heightmap>() == null)
        {
            // Only open, bare ground can be dug; try again tomorrow.
            return;
        }

        digSpot = hit.point;
        Begin(Activity.Dig);
    }

    // Walks to the spot, digs for a while, and turns something up by weighted chance, telling the owner if near.
    private void Dig(float dt)
    {
        ZDO zdo = nview.GetZDO();
        if (actionStarted < 0f)
        {
            if (!ai.MoveTo(dt, digSpot, 0.8f, run: false))
            {
                return;
            }

            actionStarted = Time.time;
            zdo.Set(DigKey, true);
        }

        HoldStill(digSpot + transform.forward);
        if (Time.time - actionStarted < DigSeconds)
        {
            return;
        }

        zdo.Set(DigKey, false);
        activity = Activity.None;
        float roll = Random.value * 100f;
        foreach ((string prefab, int min, int max, float weight) in finds)
        {
            roll -= weight;
            if (roll <= 0f)
            {
                DogRegistry.DropItem(prefab, Random.Range(min, max + 1), transform.position + transform.forward * 0.5f);
                break;
            }
        }

        dog.Happy();
        DogRegistry.SendNotice(dog.OwnerId, "whitehilt_dog_dug", dog.DogName, transform.position, DigNoticeRange);
    }

    private void HoldStill(Vector3 lookAt)
    {
        ai.StopMoving();
        Vector3 direction = Vector3.ProjectOnPlane(lookAt - transform.position, Vector3.up);
        if (direction.sqrMagnitude > 0.01f)
        {
            character.SetLookDir(direction.normalized);
        }
    }

    private void RPC_Fetch(long sender, ZDOID stick, long thrower)
    {
        if (!nview.IsOwner() || dog.IsPuppy || character.IsDead())
        {
            return;
        }

        if (nview.GetZDO().GetBool(CarryKey))
        {
            DropStick(transform.position + transform.forward * 0.6f);
        }

        stickId = stick;
        throwerId = thrower;
        Begin(Activity.Chase);
    }
}
