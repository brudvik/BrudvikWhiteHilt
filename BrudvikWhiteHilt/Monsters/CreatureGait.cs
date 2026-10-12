using UnityEngine;

namespace BrudvikWhiteHilt.Monsters;

/// <summary>
/// Speeds up a creature's stride when it runs faster than the walk clip was made for. The Nidhogg Spawn's own run clip
/// is eight seconds of one long take that stops moving after a second and a half, so its run is the walk clip played
/// faster instead. Only the owner changes the animator's speed: the game syncs it to everyone else.
/// </summary>
public class CreatureGait : MonoBehaviour
{
    private const float Change = 6f;

    private Character character;
    private Humanoid humanoid;
    private Animator animator;
    private ZNetView view;
    private SpawnRoar roar;

    /// <summary>Speed, in metres per second, at which the walk clip plays at its normal pace.</summary>
    public float WalkSpeed = 2.5f;

    /// <summary>Most the clip is sped up, so the feet do not run away with it.</summary>
    public float MaxFactor = 2.5f;

    private void Awake()
    {
        character = GetComponent<Character>();
        humanoid = GetComponent<Humanoid>();
        animator = GetComponentInChildren<Animator>();
        view = GetComponent<ZNetView>();
        roar = GetComponent<SpawnRoar>();
    }

    private void Update()
    {
        if (view.GetZDO() == null || !view.IsOwner())
        {
            return;
        }

        // Attacks and the roar play at their own pace.
        bool plain = humanoid.InAttack() || (roar != null && roar.IsAnimating());
        Vector3 velocity = character.GetVelocity();
        float speed = new Vector2(velocity.x, velocity.z).magnitude;
        float target = plain ? 1f : Mathf.Clamp(speed / WalkSpeed, 1f, MaxFactor);
        animator.speed = Mathf.MoveTowards(animator.speed, target, Change * Time.deltaTime);
    }
}
