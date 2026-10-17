using UnityEngine;

namespace BrudvikWhiteHilt.Monsters;

/// <summary>
/// Sits on the Muspell Giant. The ground shakes under its steps for players close by, and while it walks it leaves
/// burning pools behind it (on its owner, who makes the networked pools). It is heard coming when it spawns.
/// </summary>
public class MuspellGiant : MonoBehaviour
{
    private const string StartKey = "whitehilt_muspell_start";
    private const float ShakeRange = 40f;
    private const float ShakeSeconds = 0.9f;
    private const float AnnounceRange = 80f;
    private const float MovingSpeed = 0.8f;
    private const int MaxPools = 8;

    private ZNetView nview;
    private Character character;
    private float nextShake;
    private float nextPool;

    /// <summary>
    /// Leaves a burning pool on the ground at a point, which hurts the source's enemies. Does nothing on water, and
    /// when the source already has its most pools burning.
    /// </summary>
    /// <param name="source">The giant.</param>
    /// <param name="point">Where, at any height; it is put on the ground.</param>
    public static void LeavePool(Character source, Vector3 point)
    {
        LeavePool(source, point, MuspellSettings.PoolDamage.Value, MuspellSettings.PoolRadius.Value, MuspellSettings.PoolSeconds.Value);
    }

    /// <summary>
    /// Leaves a burning pool of a given strength, as a weapon etched with Surt's Wrath does under the foe it hits.
    /// </summary>
    /// <param name="source">Who leaves it; it burns their enemies.</param>
    /// <param name="point">Where, at any height; it is put on the ground.</param>
    /// <param name="damage">Fire damage per second.</param>
    /// <param name="radius">Radius in metres.</param>
    /// <param name="seconds">Seconds it burns.</param>
    public static void LeavePool(Character source, Vector3 point, float damage, float radius, float seconds)
    {
        GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(DragonGroundFire.PrefabName) : null;
        if (prefab == null || source == null || damage <= 0f)
        {
            return;
        }

        point.y = ZoneSystem.instance.GetGroundHeight(point);
        if (point.y <= ZoneSystem.instance.m_waterLevel)
        {
            return;
        }

        int burning = 0;
        foreach (DragonGroundFire patch in DragonGroundFire.Instances)
        {
            if (!patch.IsActive || patch.Source != source.GetZDOID())
            {
                continue;
            }

            if ((patch.transform.position - point).sqrMagnitude < radius * radius)
            {
                patch.Refresh(damage);
                return;
            }

            burning++;
        }

        if (burning < MaxPools)
        {
            DragonGroundFire pool = Instantiate(prefab, point, Quaternion.identity).GetComponent<DragonGroundFire>();
            pool.Initialize(source, damage, radius, seconds);
        }
    }

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        character = GetComponent<Character>();
        if (nview.IsValid() && nview.IsOwner() && nview.GetZDO().GetLong(StartKey) == 0L)
        {
            nview.GetZDO().Set(StartKey, ZNet.instance.GetTime().Ticks);
            ArrivalSound.Mark(nview.GetZDO());
        }
    }

    // A player close by when it has just come is told so.
    private void Start()
    {
        Player player = Player.m_localPlayer;
        long start = nview.IsValid() ? nview.GetZDO().GetLong(StartKey) : 0L;
        if (player == null || start == 0L || System.TimeSpan.FromTicks(ZNet.instance.GetTime().Ticks - start).TotalSeconds > 5.0
            || Vector3.Distance(player.transform.position, transform.position) > AnnounceRange)
        {
            return;
        }

        player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_muspell_comes");
    }

    private void Update()
    {
        if (!nview.IsValid() || character.IsDead() || !character.IsOnGround() || character.GetVelocity().magnitude < MovingSpeed)
        {
            return;
        }

        Player player = Player.m_localPlayer;
        if (Time.time >= nextShake && player != null && Vector3.Distance(player.transform.position, transform.position) < ShakeRange)
        {
            nextShake = Time.time + ShakeSeconds;
            GameCamera.instance?.AddShake(transform.position, ShakeRange, 0.6f, false);
        }

        float every = MuspellSettings.StepPoolSeconds.Value;
        if (every > 0f && nview.IsOwner() && Time.time >= nextPool)
        {
            nextPool = Time.time + every;
            LeavePool(character, transform.position);
        }
    }
}

/// <summary>
/// Spawned where a Muspell Giant's molten rock lands. The rock hands it its thrower, as it does to any projectile it
/// spawns; it leaves a burning pool there for the thrower and goes.
/// </summary>
public class MuspellRockSplash : MonoBehaviour, IProjectile
{
    // Gone in any case, should no rock set it up.
    private void Start()
    {
        Destroy(gameObject, 1f);
    }

    /// <inheritdoc/>
    public void Setup(Character owner, Vector3 velocity, float hitNoise, HitData hitData, ItemDrop.ItemData item, ItemDrop.ItemData ammo)
    {
        MuspellGiant.LeavePool(owner, transform.position);
        Destroy(gameObject);
    }

    /// <inheritdoc/>
    public string GetTooltipString(int itemQuality)
    {
        return string.Empty;
    }
}
