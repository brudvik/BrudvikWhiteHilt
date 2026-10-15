using BrudvikWhiteHilt.Helpers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Storms;

/// <summary>
/// What a storm throws at you besides the cold: lightning that strikes the tallest thing close by (a tree, the ground,
/// or you, out in the open with metal in hand) and gusts that fell trees around you. Each client looks after the trees
/// it is the closest player to, so two players near each other do not double it.
/// </summary>
public class StormHazards : MonoBehaviour
{
    private const float StrikeNearest = 12f;
    private const float StrikeFarthest = 45f;
    private const float StrikeRadius = 3f;
    private const float WarningSeconds = 1.5f;
    private const float TreeNearest = 10f;
    private const float TreeFarthest = 35f;
    private const float CrackSeconds = 1.2f;
    private const float BoltHeight = 140f;
    private const float BoltSeconds = 0.25f;
    private const float ThunderstoneChance = 0.15f;

    private static readonly string[] metals = { "iron", "bronze", "silver", "blackmetal", "flametal", "copper", "metal" };
    private static readonly string[] strikes = { "sfx_storm_strike", "sfx_storm_strike_2" };
    private static readonly string[] cracks = { "sfx_storm_treecrack", "sfx_storm_treecrack_2" };
    private static readonly Collider[] nearby = new Collider[256];

    private static Material boltMaterial;
    private float nextStrike;
    private float nextTree;
    private bool wasRaging;

    private void Update()
    {
        Player player = Player.m_localPlayer;
        bool raging = player != null && StormClient.RagingHere && !player.IsDead();
        if (!raging)
        {
            wasRaging = false;
            return;
        }

        // A storm that has just broken waits a little before the first strike and the first tree.
        if (!wasRaging)
        {
            wasRaging = true;
            nextStrike = Time.time + Random.Range(5f, 15f);
            nextTree = Time.time + Random.Range(10f, 25f);
        }

        StormProfile profile = StormClient.Profile;
        if (StormSettings.Lightning.Value && profile.Thunders && Time.time >= nextStrike)
        {
            nextStrike = Time.time + StormSettings.LightningSeconds.Value * Random.Range(0.5f, 1.5f);
            StartCoroutine(Strike(player));
        }

        if (StormSettings.FallingTrees.Value && profile.Kind != StormKind.Sand && profile.Kind != StormKind.Fire && Time.time >= nextTree)
        {
            nextTree = Time.time + StormSettings.TreeFallSeconds.Value * Random.Range(0.5f, 1.5f);
            TreeBase tree = PickTree(player.transform.position, TreeNearest, TreeFarthest);
            if (tree != null)
            {
                StartCoroutine(Topple(tree));
            }
        }
    }

    // Lightning seeks the player out in the open now and then, more often with metal in hand or on a ship; else a
    // tree, else the ground. Aimed at the player it crackles first: whoever moves away in time is not hit.
    private IEnumerator Strike(Player player)
    {
        bool exposed = StormClient.Cover == StormCover.Open && !player.InInterior();
        float chance = StormSettings.PlayerStrikeChance.Value / 100f * (HoldsMetal(player) ? 2f : 1f) * (Ship.GetLocalShip() != null ? 1.5f : 1f);
        if (exposed && Random.value < chance)
        {
            Vector3 aim = player.transform.position;
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_storm_static");
            PlayAt("sfx_storm_static", player.transform.position, 5f, 20f);
            yield return new WaitForSeconds(WarningSeconds);
            Bolt(aim, null);
            yield break;
        }

        TreeBase tree = PickTree(player.transform.position, StrikeNearest, StrikeFarthest);
        if (tree != null)
        {
            Bolt(tree.transform.position + Vector3.up * 6f, tree);
            yield break;
        }

        Vector2 around = Random.insideUnitCircle.normalized * Random.Range(StrikeNearest, StrikeFarthest);
        Vector3 point = player.transform.position + new Vector3(around.x, 0f, around.y);
        if (ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(point, out float ground))
        {
            point.y = Mathf.Max(ground, ZoneSystem.instance.m_waterLevel);
        }

        Bolt(point, null);
    }

    // The bolt itself: seen, heard, felt by anyone close and a tree it hits comes down.
    private void Bolt(Vector3 point, TreeBase tree)
    {
        Player player = Player.m_localPlayer;
        if (player == null)
        {
            return;
        }

        float distance = Vector3.Distance(player.transform.position, point);
        if (!VisualHelper.IsHeadless)
        {
            StartCoroutine(DrawBolt(point));
        }

        StormVision.Flash(player.InInterior() ? 0f : player.InShelter() ? 0.5f : 1f);
        PlayAt(strikes[Random.Range(0, strikes.Length)], point, 20f, 150f);
        if (tree != null)
        {
            Fell(tree, (tree.transform.position - player.transform.position).normalized);
        }

        if (StormSettings.Finds.Value)
        {
            StormFinds.AfterStrike(tree != null ? tree.transform.position + Vector3.up * 0.5f : point + Vector3.up * 0.5f, ThunderstoneChance);
        }

        if (distance <= StrikeRadius && !player.IsDead())
        {
            HitData hit = new()
            {
                m_point = player.GetCenterPoint(),
                m_dir = (player.transform.position - point + Vector3.up).normalized,
                m_pushForce = 40f,
                m_staggerMultiplier = 2f
            };
            hit.m_damage.m_lightning = player.GetMaxHealth() * StormSettings.LightningDamage.Value * (1f - distance / (StrikeRadius * 2f));
            hit.m_damage.m_fire = 5f;
            player.Damage(hit);
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_storm_struck");
        }
    }

    // The gust takes a tree: it creaks, shakes and cracks, then falls with the wind.
    private IEnumerator Topple(TreeBase tree)
    {
        PlayAt(cracks[Random.Range(0, cracks.Length)], tree.transform.position, 15f, 80f);
        tree.Shake();
        yield return new WaitForSeconds(CrackSeconds);
        if (tree != null && tree.m_nview != null && tree.m_nview.IsValid())
        {
            Vector3 wind = EnvMan.instance != null ? EnvMan.instance.GetWindDir() : Vector3.forward;
            Fell(tree, new Vector3(wind.x, 0f, wind.z).normalized);
        }
    }

    // One hit as hard as the tree is sound, with the axe of a god; the game drops it as it would to a woodcutter.
    private static void Fell(TreeBase tree, Vector3 direction)
    {
        if (tree == null || tree.m_nview == null || !tree.m_nview.IsValid())
        {
            return;
        }

        float health = tree.m_nview.GetZDO().GetFloat(ZDOVars.s_health, tree.m_health);
        HitData hit = new()
        {
            m_point = tree.transform.position + Vector3.up,
            m_dir = direction.sqrMagnitude > 0.01f ? direction : Vector3.forward,
            m_toolTier = 100
        };
        hit.m_damage.m_chop = (health + 1f) * 2f;
        tree.Damage(hit);
    }

    // A tree between the two distances, outside any base, that no other player is closer to.
    private static TreeBase PickTree(Vector3 centre, float nearest, float farthest)
    {
        int count = Physics.OverlapSphereNonAlloc(centre, farthest, nearby, LayerMask.GetMask("Default", "static_solid"));
        List<TreeBase> trees = new();
        HashSet<TreeBase> seen = new();
        for (int i = 0; i < count; i++)
        {
            TreeBase tree = nearby[i] != null ? nearby[i].GetComponentInParent<TreeBase>() : null;
            if (tree == null || !seen.Add(tree) || tree.m_nview == null || !tree.m_nview.IsValid())
            {
                continue;
            }

            Vector3 position = tree.transform.position;
            float distance = Vector3.Distance(new Vector3(centre.x, position.y, centre.z), position);
            if (distance >= nearest && distance <= farthest
                && EffectArea.IsPointInsideArea(position, EffectArea.Type.PlayerBase) == null
                && Player.GetClosestPlayer(position, farthest * 2f) == Player.m_localPlayer)
            {
                trees.Add(tree);
            }
        }

        return trees.Count > 0 ? trees[Random.Range(0, trees.Count)] : null;
    }

    private static bool HoldsMetal(Player player)
    {
        return new[] { player.GetRightItem(), player.GetLeftItem() }
            .Where(item => item?.m_shared != null)
            .Any(item => metals.Any(metal => (item.m_shared.m_name ?? string.Empty).ToLowerInvariant().Contains(metal)
                || (item.m_dropPrefab != null && item.m_dropPrefab.name.ToLowerInvariant().Contains(metal))));
    }

    private static void PlayAt(string name, Vector3 position, float near, float far)
    {
        AudioClip clip = VisualHelper.IsHeadless ? null : AudioAssets.TryLoad(name);
        if (clip == null)
        {
            return;
        }

        GameObject holder = new($"whitehilt_sound_{name}");
        holder.transform.position = position;
        AudioSource source = holder.AddComponent<AudioSource>();
        WhiteHiltSound.Place(source, near, far);
        source.volume = StormSettings.Volume.Value;
        WhiteHiltSound.PlayFrom(source, name);
        Destroy(holder, clip.length + 0.5f);
    }

    // A jagged white line from the clouds to the point, flickering out, with a burst of light where it lands.
    private static IEnumerator DrawBolt(Vector3 point)
    {
        if (boltMaterial == null && Shader.Find("Sprites/Default") is Shader shader && shader != null)
        {
            boltMaterial = new Material(shader);
        }

        if (boltMaterial == null)
        {
            yield break;
        }

        GameObject holder = new("whitehilt_lightning");
        LineRenderer line = holder.AddComponent<LineRenderer>();
        line.material = boltMaterial;
        line.useWorldSpace = true;
        line.startWidth = 1.2f;
        line.endWidth = 0.3f;
        line.startColor = line.endColor = new Color(0.85f, 0.9f, 1f, 1f);
        const int segments = 14;
        line.positionCount = segments + 1;
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 jitter = i == 0 || i == segments ? Vector3.zero : new Vector3(Random.Range(-4f, 4f), 0f, Random.Range(-4f, 4f)) * (1f - t * 0.6f);
            line.SetPosition(i, Vector3.Lerp(point + Vector3.up * BoltHeight, point, t) + jitter);
        }

        Light light = new GameObject("whitehilt_lightning_light").AddComponent<Light>();
        light.transform.SetParent(holder.transform, false);
        light.transform.position = point + Vector3.up * 4f;
        light.type = LightType.Point;
        light.color = new Color(0.8f, 0.85f, 1f);
        light.range = 60f;
        light.intensity = 6f;

        float start = Time.time;
        while (Time.time - start < BoltSeconds)
        {
            bool on = Random.value > 0.3f;
            line.enabled = on;
            light.intensity = on ? 6f : 1.5f;
            yield return null;
        }

        Destroy(holder);
    }
}
