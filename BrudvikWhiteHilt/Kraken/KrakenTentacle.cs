using BrudvikWhiteHilt.Items.ShipUpgrades;
using BrudvikWhiteHilt.Pieces.Ships.WhiteHiltShip;
using UnityEngine;

namespace BrudvikWhiteHilt.Kraken;

/// <summary>
/// Sits on a Kraken tentacle. It dies with its Kraken, and when the Kraken sinks back into the deep.
/// </summary>
public class KrakenTentacle : MonoBehaviour
{
    /// <summary>ZDO key of the Kraken the tentacle belongs to.</summary>
    public const string BodyKey = "whitehilt_kraken";

    /// <summary>ZDO key selecting the encounter's single tent sweeper.</summary>
    public const string SweeperKey = "whitehilt_kraken_sweeper";

    private const string SweepKey = "whitehilt_kraken_sweep";
    private const string EntryKey = "whitehilt_kraken_sweep_entry";
    private const string ExitKey = "whitehilt_kraken_sweep_exit";
    private const string NextSweepKey = "whitehilt_kraken_sweep_next";

    // A tentacle without a Kraken (e.g. spawned by hand) lets go after this long.
    private const float LoneSeconds = 120f;

    private ZNetView nview;
    private Character character;
    private float nextTick;
    private float born;
    private Transform visual;
    private Vector3 visualPosition;
    private Quaternion visualRotation;
    private Vector3 visualScale;
    private float visualLength;
    private float visualWidth;
    private long warnedSweep;
    private long observedSweep;
    private float previousReach;

    /// <summary>Whether this tentacle is busy warning, sweeping or withdrawing.</summary>
    public bool IsSweeping => TryGetSweep(out _, out _, out _, out _, out _);

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        character = GetComponent<Character>();
        born = Time.time;
        nextTick = Time.time + 2f;
        visual = transform.Find("Visual");
        if (visual != null)
        {
            visualPosition = visual.localPosition;
            visualRotation = visual.localRotation;
            visualScale = visual.localScale;
            Bounds bounds = new(transform.position, Vector3.zero);
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>())
                bounds.Encapsulate(renderer.bounds);
            visualLength = bounds.size.y;
            visualWidth = Mathf.Max(bounds.size.x, bounds.size.z);
        }
    }

    private void Update()
    {
        if (Time.time < nextTick || !nview.IsValid() || !nview.IsOwner() || character.IsDead())
        {
            return;
        }

        nextTick = Time.time + 1f;
        ZDOID body = nview.GetZDO().GetZDOID(BodyKey);
        ZDO bodyZdo = body.IsNone() ? null : ZDOMan.instance.GetZDO(body);
        bool gone = body.IsNone() ? Time.time - born > LoneSeconds : bodyZdo == null || bodyZdo.GetLong(KrakenBody.RetreatKey) != 0L;
        if (gone)
        {
            character.ApplyDamage(new HitData { m_damage = { m_damage = 99999f }, m_point = transform.position }, showDamageText: false, triggerEffects: true);
            return;
        }
        ScheduleSweep(bodyZdo);
    }

    private WhiteHiltShipUpgrades TargetShip(ZDO body)
    {
        if (!KrakenSettings.TentSweep.Value || body == null || body.GetLong(KrakenBody.RetreatKey) != 0L
            || !nview.GetZDO().GetBool(SweeperKey) || ZNetScene.instance == null)
            return null;
        GameObject kraken = ZNetScene.instance.FindInstance(body.m_uid);
        if (kraken == null || kraken.GetComponent<Character>() == null || kraken.GetComponent<Character>().IsDead())
            return null;
        GameObject target = ZNetScene.instance.FindInstance(body.GetZDOID(KrakenBody.ShipKey));
        WhiteHiltShipUpgrades upgrades = target != null ? target.GetComponent<WhiteHiltShipUpgrades>() : null;
        return upgrades != null && upgrades.Has(ShipTent.Bit) && upgrades.m_tentSize.x > 0f
            && Vector3.Distance(kraken.transform.position, upgrades.transform.position) <= KrakenSettings.TentSweepRange.Value
            ? upgrades : null;
    }

    private void ScheduleSweep(ZDO body)
    {
        WhiteHiltShipUpgrades upgrades = TargetShip(body);
        if (upgrades == null || IsSweeping || character.InAttack())
            return;
        ZDO zdo = nview.GetZDO();
        long now = ZNet.instance.GetTime().Ticks;
        long next = zdo.GetLong(NextSweepKey);
        if (next == 0L || now >= next)
        {
            float delay = KrakenSettings.TentSweepInterval.Value + Random.Range(0f, KrakenSettings.TentSweepJitter.Value);
            zdo.Set(NextSweepKey, now + (long)(delay * System.TimeSpan.TicksPerSecond));
            if (next == 0L)
                return;
            Bounds shelter = new(upgrades.m_tentCenter, upgrades.m_tentSize);
            Ship ship = upgrades.GetComponent<Ship>();
            foreach (Player player in ship.m_players)
            {
                if (player == null || player.IsDead())
                    continue;
                Vector3 local = upgrades.transform.InverseTransformPoint(player.GetCenterPoint());
                if (!shelter.Contains(local))
                    continue;
                float radius = KrakenSettings.TentSweepRadius.Value;
                if (shelter.size.z < radius * 2f)
                    continue;
                local.z = Mathf.Clamp(local.z, shelter.min.z + radius, shelter.max.z - radius);
                float side = upgrades.transform.InverseTransformPoint(transform.position).x < shelter.center.x ? -1f : 1f;
                Vector3 entry = new(shelter.center.x + side * (shelter.extents.x + radius), local.y, local.z);
                Vector3 exit = new(shelter.center.x - side * (shelter.extents.x + radius), local.y, local.z);
                if (!FindOpening(upgrades, shelter, radius, ref entry, ref exit))
                    continue;
                zdo.Set(EntryKey, entry);
                zdo.Set(ExitKey, exit);
                zdo.Set(SweepKey, now);
                break;
            }
        }
    }

    private bool FindOpening(WhiteHiltShipUpgrades ship, Bounds shelter, float radius, ref Vector3 entry, ref Vector3 exit)
    {
        float height = entry.y;
        foreach (float offset in new[] { 0f, -radius / 2f, radius / 2f })
        {
            entry.y = exit.y = height + offset;
            if (entry.y - radius < shelter.min.y || entry.y + radius > shelter.max.y)
                continue;
            Vector3 start = ship.transform.TransformPoint(entry);
            Vector3 finish = ship.transform.TransformPoint(exit);
            bool blocked = false;
            foreach (RaycastHit hit in Physics.SphereCastAll(start, radius, (finish - start).normalized,
                Vector3.Distance(start, finish), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider != null && hit.collider.GetComponentInParent<Character>() == null)
                {
                    blocked = true;
                    break;
                }
            }
            if (!blocked)
                return true;
        }
        return false;
    }

    private bool TryGetSweep(out WhiteHiltShipUpgrades ship, out Vector3 entry, out Vector3 exit, out float phase, out long stamp)
    {
        ship = null;
        entry = exit = Vector3.zero;
        phase = 0f;
        stamp = 0L;
        if (nview == null || !nview.IsValid() || character == null || character.IsDead() || ZNet.instance == null)
            return false;
        ZDO zdo = nview.GetZDO();
        stamp = zdo.GetLong(SweepKey);
        if (stamp == 0L)
            return false;
        phase = (float)(ZNet.instance.GetTime().Ticks - stamp) / System.TimeSpan.TicksPerSecond - KrakenSettings.TentSweepWarning.Value;
        if (phase < -KrakenSettings.TentSweepWarning.Value || phase >= KrakenSettings.TentSweepSeconds.Value)
            return false;
        ship = TargetShip(ZDOMan.instance.GetZDO(zdo.GetZDOID(BodyKey)));
        if (ship == null)
            return false;
        entry = ship.transform.TransformPoint(zdo.GetVec3(EntryKey, Vector3.zero));
        exit = ship.transform.TransformPoint(zdo.GetVec3(ExitKey, Vector3.zero));
        return true;
    }

    private void LateUpdate()
    {
        if (!TryGetSweep(out WhiteHiltShipUpgrades ship, out Vector3 entry, out Vector3 exit, out float phase, out long stamp))
        {
            if (visual != null)
            {
                visual.localPosition = visualPosition;
                visual.localRotation = visualRotation;
                visual.localScale = visualScale;
            }
            return;
        }
        if (warnedSweep != stamp)
        {
            warnedSweep = stamp;
            if (Player.m_localPlayer != null && ship.GetComponent<Ship>().m_players.Contains(Player.m_localPlayer))
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$whitehilt_kraken_tentsweep");
        }
        float extension = phase < 0f ? 0f : Mathf.Sin(Mathf.PI * phase / KrakenSettings.TentSweepSeconds.Value);
        float reach = phase < KrakenSettings.TentSweepSeconds.Value / 2f ? extension : 1f;
        if (visual != null && visualLength > 0f)
        {
            Vector3 tip = Vector3.Lerp(entry, exit, extension);
            Vector3 direction = (exit - entry).normalized;
            visual.position = Vector3.Lerp(transform.TransformPoint(visualPosition), entry, phase < 0f
                ? Mathf.Clamp01(1f + phase / KrakenSettings.TentSweepWarning.Value) : 1f);
            visual.rotation = Quaternion.FromToRotation(Vector3.up, direction);
            float thickness = visualWidth > 0f ? KrakenSettings.TentSweepRadius.Value * 2f / visualWidth : 1f;
            visual.localScale = new Vector3(visualScale.x * thickness,
                visualScale.y * Mathf.Max(KrakenSettings.TentSweepRadius.Value, Vector3.Distance(entry, tip)) / visualLength,
                visualScale.z * thickness);
        }
        if (!nview.IsOwner())
            return;
        if (observedSweep != stamp)
        {
            observedSweep = stamp;
            previousReach = 0f;
        }
        if (phase >= 0f && previousReach < 1f)
            HitSweep(ship, entry, exit, previousReach, reach, stamp);
        previousReach = reach;
    }

    private void HitSweep(WhiteHiltShipUpgrades upgrades, Vector3 entry, Vector3 exit, float previous, float current, long stamp)
    {
        Bounds shelter = new(upgrades.m_tentCenter, upgrades.m_tentSize);
        Vector3 start = Vector3.Lerp(entry, exit, previous);
        Vector3 end = Vector3.Lerp(entry, exit, current);
        Vector3 segment = end - start;
        ZDO zdo = nview.GetZDO();
        foreach (Player player in upgrades.GetComponent<Ship>().m_players)
        {
            if (player == null || player.IsDead() || !shelter.Contains(upgrades.transform.InverseTransformPoint(player.GetCenterPoint())))
                continue;
            string key = "whitehilt_sweep_hit_" + player.GetZDOID();
            if (zdo.GetLong(key) == stamp)
                continue;
            Vector3 point = player.GetCenterPoint();
            float along = segment.sqrMagnitude > 0f ? Mathf.Clamp01(Vector3.Dot(point - start, segment) / segment.sqrMagnitude) : 0f;
            if (Vector3.Distance(point, start + segment * along) > KrakenSettings.TentSweepRadius.Value)
                continue;
            zdo.Set(key, stamp);
            HitData hit = new()
            {
                m_point = point,
                m_dir = (exit - entry).normalized,
                m_pushForce = KrakenSettings.TentSweepPush.Value,
                m_dodgeable = true,
                m_blockable = true,
                m_damage = { m_blunt = KrakenSettings.TentSweepDamage.Value }
            };
            hit.SetAttacker(character);
            player.Damage(hit);
        }
    }
}
