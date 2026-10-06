using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Companions;

/// <summary>
/// Lays a creature with the vanilla wolf rig down by bending its bones over the running animation, since the game has
/// no lying animation for it. The pose lives in the creature's ZDO, so every client shows it.
/// </summary>
public sealed class RestPose : MonoBehaviour
{
    /// <summary>
    /// ZDO key that holds the current <see cref="Pose"/>.
    /// </summary>
    public const string ZdoKey = "whitehilt_rest";

    private const float BlendSeconds = 1.2f;

    private static readonly int zdoHash = ZdoKey.GetStableHashCode();

    // Offsets are applied after each bone's rest rotation. Worked out offline on the exported wolf rig.
    private static readonly BoneOffset[] hindLegs =
    {
        new("L Thigh", new Vector3(0f, 0f, -74f)), new("R Thigh", new Vector3(0f, 0f, -74f)),
        new("L Calf", new Vector3(0f, 0f, 70f)), new("R Calf", new Vector3(0f, 0f, 70f)),
        new("L HorseLink", new Vector3(0f, 0f, -75f)), new("R HorseLink", new Vector3(0f, 0f, -75f)),
        new("L Foot", new Vector3(0f, 0f, 80f)), new("R Foot", new Vector3(0f, 0f, 80f))
    };

    private static readonly BoneOffset[] lieOffsets = Combine(hindLegs, new BoneOffset[]
    {
        new("CG", Vector3.zero, new Vector3(0f, -0.33f, 0f)),
        new("L UpperArm", new Vector3(0f, 0f, -31f)), new("R UpperArm", new Vector3(0f, 0f, -31f)),
        new("L Forearm", new Vector3(0f, 0f, 113f)), new("R Forearm", new Vector3(0f, 0f, 113f)),
        new("L Hand", new Vector3(0f, 0f, -80f)), new("R Hand", new Vector3(0f, 0f, -80f)),
        new("Head", new Vector3(0f, 0f, -15f)),
        new("Tail", new Vector3(0f, 0f, 25f)),
        new("Tail1", new Vector3(0f, 30f, 0f)), new("Tail2", new Vector3(0f, 30f, 0f)), new("Tail3", new Vector3(0f, 30f, 0f))
    });

    // The front legs hang from the neck, so lowering the head lifts them: the clavicles move down and the arms turn back.
    private static readonly BoneOffset[] sleepOffsets = Combine(hindLegs, new BoneOffset[]
    {
        new("CG", Vector3.zero, new Vector3(0f, -0.33f, 0f)),
        new("Neck", new Vector3(0f, 0f, -30f)),
        new("Head", new Vector3(0f, 0f, 12f)),
        new("L Clavicle", Vector3.zero, new Vector3(0f, 0.06f, 0f)), new("R Clavicle", Vector3.zero, new Vector3(0f, 0.06f, 0f)),
        new("L UpperArm", new Vector3(0f, 0f, -1f)), new("R UpperArm", new Vector3(0f, 0f, -1f)),
        new("L Forearm", new Vector3(0f, 0f, 113f)), new("R Forearm", new Vector3(0f, 0f, 113f)),
        new("L Hand", new Vector3(0f, 0f, -80f)), new("R Hand", new Vector3(0f, 0f, -80f)),
        new("Tail", new Vector3(0f, 0f, 25f)),
        new("Tail1", new Vector3(0f, 35f, 0f)), new("Tail2", new Vector3(0f, 35f, 0f)), new("Tail3", new Vector3(0f, 35f, 0f))
    });

    // The body tips back around the hips; the neck lowers the head again, which also brings the front legs back down.
    private static readonly BoneOffset[] sitOffsets =
    {
        new("CG", new Vector3(0f, 40f, 0f), new Vector3(0f, -0.33f, 0f)),
        new("Neck", new Vector3(0f, 0f, -35f)),
        new("L UpperArm", new Vector3(0f, 0f, -5f)), new("R UpperArm", new Vector3(0f, 0f, -5f)),
        new("L Thigh", new Vector3(0f, 0f, -34f)), new("R Thigh", new Vector3(0f, 0f, -34f)),
        new("L Calf", new Vector3(0f, 0f, 97f)), new("R Calf", new Vector3(0f, 0f, 97f)),
        new("L HorseLink", new Vector3(0f, 0f, -104f)), new("R HorseLink", new Vector3(0f, 0f, -104f)),
        new("L Foot", new Vector3(0f, 0f, 80f)), new("R Foot", new Vector3(0f, 0f, 80f)),
        new("Tail", new Vector3(0f, 0f, -35f)),
        new("Tail1", new Vector3(0f, 30f, 0f)), new("Tail2", new Vector3(0f, 30f, 0f))
    };

    private readonly Dictionary<Pose, BoneTarget[]> targets = new();
    private ZNetView nview;
    private Character character;
    private Pose shown;
    private float weight;

    /// <summary>
    /// The resting poses.
    /// </summary>
    public enum Pose
    {
        /// <summary>Standing, animated as usual.</summary>
        None = 0,

        /// <summary>Lying down with the head up.</summary>
        Lie = 1,

        /// <summary>Lying down with the head low on the front paws.</summary>
        Sleep = 2,

        /// <summary>Sitting on its haunches.</summary>
        Sit = 3
    }

    /// <summary>
    /// The pose stored in the ZDO.
    /// </summary>
    public Pose Current => nview != null && nview.IsValid() ? (Pose)nview.GetZDO().GetInt(zdoHash) : Pose.None;

    /// <summary>
    /// True while the creature is told to rest.
    /// </summary>
    public bool IsResting => Current != Pose.None;

    /// <summary>
    /// Stores a new pose. Only the ZDO owner can do this.
    /// </summary>
    /// <param name="pose">The pose to show.</param>
    public void SetPose(Pose pose)
    {
        if (nview != null && nview.IsValid() && nview.IsOwner())
        {
            nview.GetZDO().Set(zdoHash, (int)pose);
        }
    }

    /// <summary>
    /// Puts a wolf rig that is still in its rest pose into a pose at once, e.g. to bake a static model of it.
    /// </summary>
    /// <param name="root">Any object above the rig's bones.</param>
    /// <param name="pose">The pose.</param>
    public static void ApplyTo(Transform root, Pose pose)
    {
        BoneOffset[] offsets = pose switch
        {
            Pose.Lie => lieOffsets,
            Pose.Sleep => sleepOffsets,
            Pose.Sit => sitOffsets,
            _ => new BoneOffset[0]
        };

        Dictionary<string, Transform> bones = new();
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (!bones.ContainsKey(child.name))
            {
                bones.Add(child.name, child);
            }
        }

        foreach (BoneOffset offset in offsets)
        {
            if (bones.TryGetValue(offset.Bone, out Transform bone))
            {
                bone.localRotation *= Quaternion.Euler(offset.Rotation);
                bone.localPosition += offset.Position;
            }
        }
    }

    // Builds the lie, sleep and sit poses from the bones' rest pose, which still holds as the animator has not run yet.
    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        character = GetComponent<Character>();

        // Runs before the animator's first update, so the bones still hold the rest pose.
        Dictionary<string, Transform> bones = new();
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (!bones.ContainsKey(child.name))
            {
                bones.Add(child.name, child);
            }
        }

        targets[Pose.Lie] = Build(lieOffsets, bones);
        targets[Pose.Sleep] = Build(sleepOffsets, bones);
        targets[Pose.Sit] = Build(sitOffsets, bones);

        if (character != null)
        {
            character.m_onDamaged += OnDamaged;
        }
    }

    private void OnDestroy()
    {
        if (character != null)
        {
            character.m_onDamaged -= OnDamaged;
        }
    }

    // Blends the bones into the pose over the animator's output; when the pose changes, the dog stands up fully first.
    private void LateUpdate()
    {
        Pose wanted = character != null && character.IsDead() ? Pose.None : Current;
        float step = Time.deltaTime / BlendSeconds;

        // Stand up fully before switching to another pose.
        if (wanted != shown && weight > 0f)
        {
            weight = Mathf.Max(0f, weight - step);
        }
        else if (wanted != shown)
        {
            shown = wanted;
        }
        else if (shown != Pose.None)
        {
            weight = Mathf.Min(1f, weight + step);
        }

        if (shown == Pose.None)
        {
            return;
        }

        float blend = Mathf.SmoothStep(0f, 1f, weight);
        foreach (BoneTarget target in targets[shown])
        {
            target.Apply(blend);
        }
    }

    private void OnDamaged(float damage, Character attacker)
    {
        SetPose(Pose.None);
    }

    private BoneTarget[] Build(BoneOffset[] offsets, Dictionary<string, Transform> bones)
    {
        List<BoneTarget> result = new();
        foreach (BoneOffset offset in offsets)
        {
            if (!bones.TryGetValue(offset.Bone, out Transform bone))
            {
                Jotunn.Logger.LogWarning($"Rest pose: {name} has no bone {offset.Bone}");
                continue;
            }

            result.Add(new BoneTarget(bone, offset));
        }

        return result.ToArray();
    }

    private static BoneOffset[] Combine(BoneOffset[] first, BoneOffset[] second)
    {
        BoneOffset[] result = new BoneOffset[first.Length + second.Length];
        first.CopyTo(result, 0);
        second.CopyTo(result, first.Length);
        return result;
    }

    private readonly struct BoneOffset
    {
        public readonly string Bone;
        public readonly Vector3 Rotation;
        public readonly Vector3 Position;

        public BoneOffset(string bone, Vector3 rotation, Vector3 position = default)
        {
            Bone = bone;
            Rotation = rotation;
            Position = position;
        }
    }

    private sealed class BoneTarget
    {
        private readonly Transform bone;
        private readonly Quaternion restRotation;
        private readonly Quaternion rotation;
        private readonly Vector3 restPosition;
        private readonly Vector3 position;
        private readonly bool rotates;
        private readonly bool moves;
        private Quaternion writtenRotation;
        private Vector3 writtenPosition;
        private bool written;

        public BoneTarget(Transform bone, BoneOffset offset)
        {
            this.bone = bone;
            restRotation = bone.localRotation;
            restPosition = bone.localPosition;
            rotation = restRotation * Quaternion.Euler(offset.Rotation);
            position = restPosition + offset.Position;
            rotates = offset.Rotation != Vector3.zero;
            moves = offset.Position != Vector3.zero;
        }

        public void Apply(float blend)
        {
            // A bone the animator does not drive still holds last frame's value; blend that one from rest instead.
            if (rotates)
            {
                Quaternion from = written && bone.localRotation == writtenRotation ? restRotation : bone.localRotation;
                bone.localRotation = writtenRotation = Quaternion.Slerp(from, rotation, blend);
            }

            if (moves)
            {
                Vector3 from = written && bone.localPosition == writtenPosition ? restPosition : bone.localPosition;
                bone.localPosition = writtenPosition = Vector3.Lerp(from, position, blend);
            }

            written = true;
        }
    }
}
