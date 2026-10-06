using UnityEngine;

namespace BrudvikWhiteHilt.Companions;

/// <summary>
/// The dog's small signs of life, on every client: the tail wags when its master is near or it is happy,
/// the head follows its master while it sits or lies, a puppy has a big head and big paws, a sleeping dog snores,
/// a fetching dog carries the stick in its mouth, and the <see cref="DogAction"/>s, shivering and limping are played
/// by turning bones. Runs after <see cref="RestPose"/>, on top of the pose.
/// </summary>
[DefaultExecutionOrder(100)]
public sealed class DogExpression : MonoBehaviour
{
    private const float WagRange = 6f;
    private const float WagSpeed = 11f;
    private const float WagDegrees = 28f;
    private const float LookRange = 10f;
    private const float LookDegrees = 60f;
    private const float PuppyHead = 1.35f;
    private const float PuppyPaws = 1.3f;
    private const float Blend = 3f;
    private const float DigSpeed = 9f;
    private const float DigNeckDegrees = -30f;
    private const float DigArmBase = -20f;
    private const float DigArmSwing = 35f;
    private const float DigForearmDegrees = 40f;
    private const float ActionFade = 0.35f;
    private const float BellyDegrees = 150f;
    private const float ShakeSpeed = 30f;
    private const float ShakeDegrees = 22f;
    private const float ShiverSpeed = 45f;
    private const float ShiverDegrees = 2.5f;
    private const float LimpSpeed = 0.3f;
    private const float HopSeconds = 0.55f;
    private const float HopHeight = 0.3f;
    private const float HopPitch = 12f;
    private const float HopTuck = 30f;

    /// <summary>ZDO key: game time in ticks of the dog's last playful hop, set by its owner.</summary>
    public static readonly int HopKey = "whitehilt_dog_hop".GetStableHashCode();

    private static readonly string[] tailBones = { "Tail", "Tail1", "Tail2" };
    private static readonly string[] pawBones = { "L Hand", "R Hand", "L Foot", "R Foot" };
    private static readonly string[] armBones = { "L UpperArm", "R UpperArm" };
    private static readonly string[] forearmBones = { "L Forearm", "R Forearm" };
    // The front legs hang from the neck, so the neck is left out of the twist.
    private static readonly string[] spineBones = { "Spine", "Spine1", "Spine2" };

    private DogCompanion dog;
    private ZNetView nview;
    private long lastHop;
    private bool hopSeen;
    private float hopStarted = -1f;
    private DogCare care;
    private Character character;
    private RestPose rest;
    private Renderer body;
    private Transform[] tail;
    private Transform[] paws;
    private Transform[] arms;
    private Transform[] forearms;
    private Transform[] spine;
    private Transform neck;
    private Transform head;
    private Transform jaw;
    private Transform cg;
    private Transform rightThigh;
    private Transform rightCalf;
    private Transform visual;
    private Quaternion visualRotation;
    private Vector3 visualPosition;
    private ParticleSystem splash;
    private bool splashed;
    private GameObject stick;
    private float wag;
    private float look;
    private float dig;
    private float happyUntil;
    private float nextSnore;

    /// <summary>
    /// Wags for a while, e.g. after a cuddle or a meal.
    /// </summary>
    /// <param name="seconds">How long.</param>
    public void MakeHappy(float seconds)
    {
        happyUntil = Mathf.Max(happyUntil, Time.time + seconds);
    }

    // Finds the bones the expression moves: tail, legs, spine, head, jaw and so on, by name.
    private void Awake()
    {
        dog = GetComponent<DogCompanion>();
        nview = GetComponent<ZNetView>();
        care = GetComponent<DogCare>();
        character = GetComponent<Character>();
        rest = GetComponent<RestPose>();
        body = GetComponentInChildren<SkinnedMeshRenderer>(true);
        Transform[] all = GetComponentsInChildren<Transform>(true);
        tail = System.Array.ConvertAll(tailBones, name => System.Array.Find(all, bone => bone.name == name));
        paws = System.Array.ConvertAll(pawBones, name => System.Array.Find(all, bone => bone.name == name));
        arms = System.Array.ConvertAll(armBones, name => System.Array.Find(all, bone => bone.name == name));
        forearms = System.Array.ConvertAll(forearmBones, name => System.Array.Find(all, bone => bone.name == name));
        spine = System.Array.ConvertAll(spineBones, name => System.Array.Find(all, bone => bone.name == name));
        neck = System.Array.Find(all, bone => bone.name == "Neck");
        head = System.Array.Find(all, bone => bone.name == "Head");
        jaw = System.Array.Find(all, bone => bone.name == "Jaw");
        cg = System.Array.Find(all, bone => bone.name == "CG");
        rightThigh = System.Array.Find(all, bone => bone.name == "R Thigh");
        rightCalf = System.Array.Find(all, bone => bone.name == "R Calf");
        visual = transform.Find("Visual");
        if (visual != null)
        {
            visualRotation = visual.localRotation;
            visualPosition = visual.localPosition;
        }
    }

    // Moves the bones after the animator: wagging, looking at its master, digging, tricks, hopping, snoring and
    // carrying a stick. Skipped off screen, where the animator stops writing the bones and the turns would pile up.
    private void LateUpdate()
    {
        if (dog == null || !dog.enabled || dog.OwnerId == 0L)
        {
            return;
        }

        // Off screen the animator stops writing the bones, and the turns below would pile up frame after frame.
        if (body == null || !body.isVisible)
        {
            return;
        }

        float dt = Time.deltaTime;
        Player master = Player.GetPlayer(dog.OwnerId);
        float distance = master != null ? Vector3.Distance(master.transform.position, transform.position) : float.MaxValue;

        ApplyProportions();
        Wag(dt, distance <= WagRange || Time.time < happyUntil);
        Look(dt, master, distance);
        Dig(dt);
        Act();
        Hop();
        Snore();
        CarryStick();
    }

    // Fades an action in and out over its length.
    private static float Envelope(float elapsed, float length)
    {
        if (elapsed < 0f || elapsed > length)
        {
            return 0f;
        }

        return Mathf.Clamp01(Mathf.Min(elapsed, length - elapsed) / ActionFade);
    }

    private static void Turn(Transform bone, float x, float y, float z)
    {
        if (bone != null)
        {
            bone.localRotation *= Quaternion.Euler(x, y, z);
        }
    }

    // Bones bend around their local z (legs, neck, head, jaw) and twist around their local x (spine).
    private void Act()
    {
        if (visual != null)
        {
            visual.localRotation = visualRotation;
            visual.localPosition = visualPosition;
        }

        if (care == null)
        {
            return;
        }

        DogAction action = care.GetAction(out float t);
        float length = DogActions.Duration(action);
        float amount = Envelope(t, length);
        if (action != DogAction.Shake)
        {
            splashed = false;
        }

        switch (action)
        {
            case DogAction.Paw:
                Turn(arms[1], 0f, 0f, (40f + Mathf.Sin(t * 8f) * 8f) * amount);
                Turn(forearms[1], 0f, 0f, 15f * amount);
                break;
            case DogAction.Roll:
                RollBody(t < 0f ? 0f : 360f * Mathf.SmoothStep(0f, 1f, t / length));
                break;
            case DogAction.Belly:
                RollBody(BellyDegrees * amount);
                Turn(arms[0], 0f, 0f, Mathf.Sin(t * 6f) * 15f * amount);
                Turn(arms[1], 0f, 0f, -Mathf.Sin(t * 6f) * 15f * amount);
                break;
            case DogAction.Shake:
                Shake(t, amount);
                break;
            case DogAction.Yawn:
                Turn(head, 0f, 0f, 15f * amount);
                Turn(jaw, 0f, 0f, -30f * amount);
                break;
            case DogAction.Stretch:
                Turn(cg, 0f, -18f * amount, 0f);
                Turn(arms[0], 0f, 0f, 45f * amount);
                Turn(arms[1], 0f, 0f, 45f * amount);
                Turn(neck, 0f, 0f, 20f * amount);
                break;
            case DogAction.Scratch:
                Turn(rightThigh, 0f, 0f, -65f * amount);
                Turn(rightCalf, 0f, 0f, Mathf.Sin(t * 14f) * 25f * amount);
                Turn(head, 15f * amount, 0f, 0f);
                break;
        }

        if (action == DogAction.None && care.IsCold)
        {
            float shiver = Mathf.Sin(Time.time * ShiverSpeed) * ShiverDegrees;
            foreach (Transform bone in spine)
            {
                Turn(bone, shiver, 0f, 0f);
            }
        }

        // Holds the right foreleg up while it walks on three.
        if (care.IsLimping && !rest.IsResting && character.GetVelocity().magnitude > LimpSpeed)
        {
            Turn(arms[1], 0f, 0f, 25f);
            Turn(forearms[1], 0f, 0f, 60f);
        }
    }

    // A parabolic hop of the body alone, nose up on the way up and down on the way down, front paws tucked in.
    // Each client starts it when it sees a new hop time, so a late ZDO update still plays it in full.
    private void Hop()
    {
        long hop = nview != null && nview.IsValid() ? nview.GetZDO().GetLong(HopKey) : 0L;
        if (hop != lastHop)
        {
            // A hop stored before this client saw the dog is not played.
            if (hopSeen)
            {
                hopStarted = Time.time;
            }

            lastHop = hop;
        }

        hopSeen = true;

        float u = hopStarted < 0f ? 1f : (Time.time - hopStarted) / HopSeconds;
        if (u >= 1f || visual == null)
        {
            return;
        }

        float scale = Mathf.Max(0.01f, transform.localScale.y);
        visual.localPosition += Vector3.up * (4f * u * (1f - u) * HopHeight / scale);
        if (cg != null)
        {
            visual.RotateAround(cg.position, transform.right, -HopPitch * Mathf.Sin(u * Mathf.PI * 2f));
        }

        float tuck = Mathf.Sin(u * Mathf.PI) * HopTuck;
        foreach (Transform forearm in forearms)
        {
            Turn(forearm, 0f, 0f, tuck);
        }
    }

    // Turns the whole body around its length, through its centre, so it rolls on the spot.
    private void RollBody(float degrees)
    {
        if (visual != null && cg != null && degrees != 0f)
        {
            visual.RotateAround(cg.position, transform.forward, degrees);
        }
    }

    private void Shake(float t, float amount)
    {
        float twist = Mathf.Sin(t * ShakeSpeed) * ShakeDegrees * amount;
        for (int i = 0; i < spine.Length; i++)
        {
            Turn(spine[i], i % 2 == 0 ? twist : -twist, 0f, 0f);
        }

        Turn(head, twist * 0.5f, 0f, 0f);
        if (!splashed && t >= 0f)
        {
            splashed = true;
            GetSplash()?.Play();
        }
    }

    // Drops of water flung off in every direction, made once per dog.
    private ParticleSystem GetSplash()
    {
        if (splash != null)
        {
            return splash;
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            return null;
        }

        GameObject holder = new("whitehilt_splash");
        holder.transform.SetParent(transform, false);
        holder.transform.localPosition = Vector3.up * 0.9f;
        splash = holder.AddComponent<ParticleSystem>();
        splash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = splash.main;
        main.duration = 1f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = 0.7f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
        main.startColor = new Color(0.8f, 0.9f, 1f, 0.7f);
        main.gravityModifier = 1.2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = 120;
        ParticleSystem.EmissionModule emission = splash.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 40), new ParticleSystem.Burst(0.3f, 30), new ParticleSystem.Burst(0.6f, 20) });
        ParticleSystem.ShapeModule shape = splash.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;
        holder.GetComponent<ParticleSystemRenderer>().sharedMaterial = new Material(shader);
        return splash;
    }

    // Head down, front paws scraping in turn; on top of the standing animation.
    private void Dig(float dt)
    {
        dig = Mathf.MoveTowards(dig, dog.IsDigging ? 1f : 0f, dt * Blend);
        if (dig <= 0f)
        {
            return;
        }

        if (neck != null)
        {
            neck.localRotation *= Quaternion.Euler(0f, 0f, DigNeckDegrees * dig);
        }

        float phase = Time.time * DigSpeed;
        for (int i = 0; i < arms.Length; i++)
        {
            float scrape = Mathf.Sin(phase + i * Mathf.PI);
            if (arms[i] != null)
            {
                arms[i].localRotation *= Quaternion.Euler(0f, 0f, (DigArmBase + DigArmSwing * scrape) * dig);
            }

            if (forearms[i] != null)
            {
                forearms[i].localRotation *= Quaternion.Euler(0f, 0f, DigForearmDegrees * Mathf.Max(0f, -scrape) * dig);
            }
        }
    }

    private void ApplyProportions()
    {
        float young = 1f - dog.GrowthFraction;
        if (head != null)
        {
            head.localScale = Vector3.one * Mathf.Lerp(1f, PuppyHead, young);
        }

        foreach (Transform paw in paws)
        {
            if (paw != null)
            {
                paw.localScale = Vector3.one * Mathf.Lerp(1f, PuppyPaws, young);
            }
        }
    }

    private void Wag(float dt, bool wanted)
    {
        wag = Mathf.MoveTowards(wag, wanted ? 1f : 0f, dt * Blend);
        if (wag <= 0f)
        {
            return;
        }

        float angle = Mathf.Sin(Time.time * WagSpeed) * WagDegrees * wag;
        for (int i = 0; i < tail.Length; i++)
        {
            if (tail[i] != null)
            {
                tail[i].localRotation *= Quaternion.Euler(0f, angle * (1f - i * 0.25f), 0f);
            }
        }
    }

    // Only the head turns, around the world's up axis, so it works on top of any pose.
    private void Look(float dt, Player master, float distance)
    {
        bool wanted = master != null && head != null && rest.IsResting && distance <= LookRange;
        look = Mathf.MoveTowards(look, wanted ? 1f : 0f, dt * Blend);
        if (look <= 0f || master == null || head == null)
        {
            return;
        }

        Vector3 facing = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        Vector3 toMaster = Vector3.ProjectOnPlane(master.GetEyePoint() - head.position, Vector3.up);
        float angle = Mathf.Clamp(Vector3.SignedAngle(facing, toMaster, Vector3.up), -LookDegrees, LookDegrees);
        head.rotation = Quaternion.AngleAxis(angle * look, Vector3.up) * head.rotation;
    }

    private void Snore()
    {
        if (rest.Current != RestPose.Pose.Sleep || Time.time < nextSnore)
        {
            return;
        }

        float length = DogRegistry.PlaySound("dogsnore", transform.position);
        nextSnore = Time.time + (length > 0f ? length + 1.5f : 30f);
    }

    private void CarryStick()
    {
        bool carrying = dog.IsCarrying;
        if (carrying == (stick != null) || jaw == null)
        {
            return;
        }

        if (!carrying)
        {
            Destroy(stick);
            stick = null;
            return;
        }

        stick = DogRegistry.CreateStickModel(jaw);
    }
}
