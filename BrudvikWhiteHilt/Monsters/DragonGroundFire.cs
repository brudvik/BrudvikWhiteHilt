using BrudvikWhiteHilt.Helpers;
using Jotunn.Entities;
using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Monsters;

/// <summary>A short-lived, networked ground fire patch without terrain changes or spreading.</summary>
public class DragonGroundFire : MonoBehaviour
{
    /// <summary>Network prefab identifier.</summary>
    public const string PrefabName = "WhiteHilt_DragonGroundFire";

    private static readonly HashSet<DragonGroundFire> instances = new();
    private ZNetView view;
    private ParticleSystem[] particles;
    private bool visualsActive;
    private float visualRadius;

    /// <summary>Loaded ground fire patches.</summary>
    public static IEnumerable<DragonGroundFire> Instances => instances;

    /// <summary>Whether any loaded patches need damage checks.</summary>
    public static bool HasInstances => instances.Count > 0;

    /// <summary>The dragon that created this patch.</summary>
    public ZDOID Source => view.GetZDO().GetZDOID("whitehilt_fire_source");

    /// <summary>The saved patch radius.</summary>
    public float Radius => view.GetZDO().GetFloat("whitehilt_fire_radius");

    /// <summary>The saved scaled fire damage per second.</summary>
    public float Damage => view.GetZDO().GetFloat("whitehilt_fire_damage");

    /// <summary>Whether the patch is initialized and has not expired.</summary>
    public bool IsActive => view != null && view.IsValid() && MonsterSettings.DragonGroundFire.Value &&
        view.GetZDO().GetLong("whitehilt_fire_until") > ZNet.instance.GetTime().Ticks;

    /// <summary>Registers a nonpersistent patch using a lightweight copy of vanilla flame particles.</summary>
    /// <param name="projectile">The vanilla flame projectile to borrow particles from.</param>
    public static void Register(GameObject projectile)
    {
        GameObject prefab = PrefabManager.Instance.CreateEmptyPrefab(PrefabName);
        foreach (Component component in prefab.GetComponents<Component>())
        {
            if (component is MeshRenderer || component is MeshFilter || component is Collider)
            {
                DestroyImmediate(component);
            }
        }
        prefab.GetComponent<ZNetView>().m_persistent = false;
        prefab.AddComponent<DragonGroundFire>();
        if (!VisualHelper.IsHeadless)
        {
            ParticleSystem template = projectile.GetComponentsInChildren<ParticleSystem>(true)
                .FirstOrDefault(system => system.name.IndexOf("flame", System.StringComparison.OrdinalIgnoreCase) >= 0);
            if (template != null)
            {
                GameObject flames = Instantiate(template.gameObject, prefab.transform);
                flames.name = "flames";
                while (flames.transform.childCount > 0)
                {
                    DestroyImmediate(flames.transform.GetChild(0).gameObject);
                }
                foreach (Component component in flames.GetComponents<Component>())
                {
                    if (component is not Transform && component is not ParticleSystem && component is not ParticleSystemRenderer)
                    {
                        DestroyImmediate(component);
                    }
                }
                flames.transform.localPosition = Vector3.zero;
                flames.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                flames.transform.localScale = Vector3.one;
                ParticleSystem system = flames.GetComponent<ParticleSystem>();
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ParticleSystem.MainModule main = system.main;
                main.loop = true;
                main.startLifetime = 0.8f;
                main.startSpeed = 0.2f;
                main.startSize = 0.7f;
                main.maxParticles = 64;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.playOnAwake = false;
                ParticleSystem.EmissionModule emission = system.emission;
                emission.enabled = true;
                emission.rateOverTime = 18f;
                emission.burstCount = 0;
                ParticleSystem.ShapeModule shape = system.shape;
                shape.enabled = true;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 0f;
                shape.radius = MonsterSettings.DragonGroundRadius.Value;
                ParticleSystem.CollisionModule collision = system.collision;
                collision.enabled = false;
                ParticleSystem.LightsModule lights = system.lights;
                lights.enabled = false;
                ParticleSystem.SubEmittersModule subEmitters = system.subEmitters;
                subEmitters.enabled = false;
                ParticleSystem.TrailModule trails = system.trails;
                trails.enabled = false;
                ParticleSystemRenderer renderer = flames.GetComponent<ParticleSystemRenderer>();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }
        PrefabManager.Instance.AddPrefab(new CustomPrefab(prefab, false));
    }

    /// <summary>Scales ground damage by the final breath damage, including stars and beast bonuses.</summary>
    /// <param name="groundDamage">Configured ground damage per second.</param>
    /// <param name="projectileDamage">The final projectile fire damage.</param>
    /// <param name="baseBreathDamage">The weapon's unscaled fire damage.</param>
    /// <returns>Scaled damage, or zero when the breath deals no fire damage.</returns>
    public static float ScaledDamage(float groundDamage, float projectileDamage, float baseBreathDamage)
    {
        return baseBreathDamage > 0f ? Mathf.Max(0f, groundDamage * projectileDamage / baseBreathDamage) : 0f;
    }

    /// <summary>Tests a ground-level point against a spherical patch.</summary>
    /// <param name="point">The target's lowest point.</param>
    /// <returns>Whether the target touches active fire.</returns>
    public bool Contains(Vector3 point)
    {
        return IsActive && (point - transform.position).sqrMagnitude <= Radius * Radius;
    }

    /// <summary>Sets the source and damage before this patch is replicated.</summary>
    /// <param name="source">The attacking dragon.</param>
    /// <param name="damage">Scaled fire damage per second.</param>
    public void Initialize(Character source, float damage)
    {
        ZDO data = view.GetZDO();
        data.Set("whitehilt_fire_source", source.GetZDOID());
        data.Set("whitehilt_fire_prefab", Utils.GetPrefabName(source.gameObject).GetStableHashCode());
        data.Set("whitehilt_fire_radius", MonsterSettings.DragonGroundRadius.Value);
        Refresh(damage);
    }

    /// <summary>Refreshes a nearby impact without creating another patch.</summary>
    /// <param name="damage">Scaled damage of the new impact.</param>
    public void Refresh(float damage)
    {
        if (view.IsOwner())
        {
            view.GetZDO().Set("whitehilt_fire_damage", Mathf.Max(Damage, damage));
            view.GetZDO().Set("whitehilt_fire_until", ZNet.instance.GetTime().AddSeconds(MonsterSettings.DragonGroundSeconds.Value).Ticks);
        }
    }

    /// <summary>Finds the source for hostility checks even after the dragon has died.</summary>
    /// <returns>The live dragon, or its prefab for faction checks.</returns>
    public Character GetSource()
    {
        GameObject live = ZNetScene.instance.FindInstance(Source);
        GameObject source = live != null ? live : ZNetScene.instance.GetPrefab(view.GetZDO().GetInt("whitehilt_fire_prefab"));
        return source != null ? source.GetComponent<Character>() : null;
    }

    private void Awake()
    {
        view = GetComponent<ZNetView>();
        particles = VisualHelper.IsHeadless ? new ParticleSystem[0] : GetComponentsInChildren<ParticleSystem>();
        if (view.IsValid())
        {
            instances.Add(this);
        }
    }

    // Removes a burnt-out patch on its owner, and fits the flames to the patch's size on every machine.
    private void Update()
    {
        bool active = IsActive;
        if (view.IsValid() && view.IsOwner() && !active)
        {
            ZNetScene.instance.Destroy(gameObject);
        }
        float radius = view.IsValid() ? Radius : 0f;
        if (active != visualsActive || radius != visualRadius)
        {
            foreach (ParticleSystem system in particles)
            {
                ParticleSystem.ShapeModule shape = system.shape;
                shape.radius = radius;
                if (active)
                {
                    system.Play();
                }
                else
                {
                    system.Stop();
                }
            }
            visualsActive = active;
            visualRadius = radius;
        }
    }

    private void OnDestroy()
    {
        instances.Remove(this);
    }
}

/// <summary>Applies only the strongest overlapping fire on each target's network owner.</summary>
public class DragonGroundFireDamage : MonoBehaviour
{
    private float nextTick;

    // Now and then burns what stands in a fire patch: characters owned here, if the dragon that lit it is their enemy,
    // and building pieces.
    private void Update()
    {
        if (Time.time < nextTick)
        {
            return;
        }
        float interval = MonsterSettings.DragonGroundTickSeconds.Value;
        nextTick = Time.time + interval;
        if (!MonsterSettings.DragonGroundFire.Value || !DragonGroundFire.HasInstances)
        {
            return;
        }
        foreach (Character target in Character.GetAllCharacters())
        {
            if (target.m_nview == null || !target.m_nview.IsValid() || !target.m_nview.IsOwner() || target.IsDead())
            {
                continue;
            }
            DragonGroundFire strongest = null;
            Character attacker = null;
            foreach (DragonGroundFire patch in DragonGroundFire.Instances)
            {
                if (!patch.Contains(target.transform.position) || (strongest != null && patch.Damage <= strongest.Damage))
                {
                    continue;
                }
                Character source = patch.GetSource();
                if (source != null && BaseAI.IsEnemy(source, target))
                {
                    strongest = patch;
                    attacker = source;
                }
            }
            if (strongest != null)
            {
                HitData hit = new() { m_point = target.transform.position, m_hitType = HitData.HitType.EnemyHit };
                hit.m_damage.m_fire = strongest.Damage * interval;
                if (attacker.m_nview != null && attacker.m_nview.IsValid())
                {
                    hit.SetAttacker(attacker);
                }
                target.Damage(hit);
            }
        }
        if (MonsterSettings.DragonBurnsBuildings.Value)
        {
            foreach (WearNTear piece in WearNTear.GetAllInstances())
            {
                if (piece.m_nview == null || !piece.m_nview.IsValid() || !piece.m_nview.IsOwner())
                {
                    continue;
                }
                float damage = 0f;
                foreach (DragonGroundFire patch in DragonGroundFire.Instances)
                {
                    if (patch.Contains(piece.transform.position))
                    {
                        damage = Mathf.Max(damage, patch.Damage);
                    }
                }
                if (damage > 0f)
                {
                    HitData hit = new() { m_point = piece.transform.position, m_hitType = HitData.HitType.EnemyHit };
                    hit.m_damage.m_fire = damage * interval;
                    piece.Damage(hit);
                }
            }
        }
    }
}