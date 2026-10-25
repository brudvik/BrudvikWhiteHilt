using BrudvikWhiteHilt.Helpers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Storms;

/// <summary>
/// Fire from the sky in Ragnarok: now and then a burning rock falls 15 to 40 m from the local player, often on what
/// players have built. A glowing streak comes down the sky for a couple of seconds first. Where it lands it hurts the
/// player if they stand there, and sets the wooden pieces close by burning for a while: they lose health until they
/// are repaired or have burnt down. Stone, marble and iron do not burn. Each client sees the rocks that fall near its
/// own player, as with the lightning. On the <see cref="Game"/> object.
/// </summary>
public class RagnarokFire : MonoBehaviour
{
    private const float Nearest = 15f;
    private const float Farthest = 40f;
    private const float FallSeconds = 2.4f;
    private const float FallHeight = 160f;
    private const float FallSlant = 70f;
    private const float HitRadius = 5f;
    private const float BurnRadius = 5f;
    private const float BurnSeconds = 20f;
    private const int BurnTicks = 10;
    private const float PlayerDamage = 0.35f;
    private const float OnBuildingChance = 0.6f;

    private static readonly Collider[] nearby = new Collider[256];
    private static Material streakMaterial;

    private float nextFall;
    private bool wasRaging;
    private bool told;
    private int storm;

    private void Update()
    {
        Player player = Player.m_localPlayer;
        bool raging = player != null && !player.IsDead() && StormSettings.Enabled.Value && StormSettings.RagnarokFire.Value
            && StormState.Ragnarok && StormState.Raging && !StormState.InEye && StormSettings.HasStorms(player.GetCurrentBiome())
            && !player.InInterior();
        if (StormState.Number != storm)
        {
            storm = StormState.Number;
            told = false;
        }

        if (!raging)
        {
            wasRaging = false;
            return;
        }

        if (!wasRaging)
        {
            wasRaging = true;
            nextFall = Time.time + Random.Range(15f, 30f);
        }

        if (Time.time >= nextFall)
        {
            nextFall = Time.time + StormSettings.RagnarokFireSeconds.Value * Random.Range(0.6f, 1.4f);
            StartCoroutine(Fall(player, PickPoint(player.transform.position)));
        }
    }

    // On a wooden piece players have built, more often than not; else the ground.
    private static Vector3 PickPoint(Vector3 origin)
    {
        if (Random.value < OnBuildingChance)
        {
            List<WearNTear> wood = WearNTear.GetAllInstances()
                .Where(piece => piece != null && Burns(piece)
                                && Distance2D(piece.transform.position, origin) is >= Nearest and <= Farthest)
                .ToList();
            if (wood.Count > 0)
            {
                return wood[Random.Range(0, wood.Count)].transform.position;
            }
        }

        Vector2 around = Random.insideUnitCircle.normalized * Random.Range(Nearest, Farthest);
        Vector3 point = origin + new Vector3(around.x, 0f, around.y);
        if (ZoneSystem.instance != null && ZoneSystem.instance.GetSolidHeight(point, out float height))
        {
            point.y = Mathf.Max(height, ZoneSystem.instance.m_waterLevel);
        }

        return point;
    }

    private static float Distance2D(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

    // A built piece of wood that fire takes.
    private static bool Burns(WearNTear piece)
    {
        return piece.m_nview != null && piece.m_nview.IsValid() && piece.m_burnable
            && piece.m_materialType is WearNTear.MaterialType.Wood or WearNTear.MaterialType.HardWood
            && piece.GetComponent<Piece>() is Piece built && built.IsPlacedByPlayer();
    }

    // The streak comes down the sky, then the rock lands.
    private IEnumerator Fall(Player player, Vector3 point)
    {
        if (!told)
        {
            told = true;
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_ragnarok_fire");
        }

        Vector2 slant = Random.insideUnitCircle.normalized * FallSlant;
        Vector3 from = point + new Vector3(slant.x, FallHeight, slant.y);
        StormHazards.PlayAt("sfx_storm_meteor", point, 25f, 200f);
        if (!VisualHelper.IsHeadless)
        {
            yield return DrawStreak(from, point);
        }
        else
        {
            yield return new WaitForSeconds(FallSeconds);
        }

        Land(point);
    }

    private void Land(Vector3 point)
    {
        Player player = Player.m_localPlayer;
        GameObject burst = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab("vfx_RockDestroyed_large") : null;
        if (burst != null)
        {
            Instantiate(burst, point, Quaternion.identity);
        }

        if (player == null)
        {
            return;
        }

        StormVision.Flash(player.InShelter() ? 0.4f : 0.9f);
        if (GameCamera.instance != null)
        {
            GameCamera.instance.AddShake(point, 60f, 2f, false);
        }

        float distance = Vector3.Distance(player.transform.position, point);
        if (distance <= HitRadius && !player.IsDead())
        {
            HitData hit = new()
            {
                m_point = player.GetCenterPoint(),
                m_dir = (player.transform.position - point + Vector3.up).normalized,
                m_pushForce = 60f,
                m_staggerMultiplier = 2f
            };
            float share = PlayerDamage * (1f - distance / (HitRadius * 2f));
            hit.m_damage.m_fire = player.GetMaxHealth() * share * 0.6f;
            hit.m_damage.m_blunt = player.GetMaxHealth() * share * 0.4f;
            player.Damage(hit);
        }

        List<WearNTear> burning = WearNTear.GetAllInstances()
            .Where(piece => piece != null && Burns(piece) && Vector3.Distance(piece.transform.position, point) <= BurnRadius)
            .ToList();
        if (burning.Count > 0 && StormSettings.RagnarokFireDamage.Value > 0f)
        {
            if (Distance2D(player.transform.position, point) <= Farthest + 10f)
            {
                player.Message(MessageHud.MessageType.TopLeft, "$msg_whitehilt_ragnarok_burning");
            }

            StartCoroutine(Burn(burning));
        }
    }

    // The wood burns for a while, losing health with each tick, through the game's own damage so the owner applies it.
    private static IEnumerator Burn(List<WearNTear> pieces)
    {
        List<GameObject> flames = new();
        GameObject fire = VisualHelper.IsHeadless || ZNetScene.instance == null ? null : ZNetScene.instance.GetPrefab("vfx_Burning");
        if (fire != null)
        {
            foreach (WearNTear piece in pieces.Take(6))
            {
                flames.Add(Instantiate(fire, piece.transform.position + Vector3.up * 0.5f, Quaternion.identity, piece.transform));
            }
        }

        float tick = BurnSeconds / BurnTicks;
        float share = StormSettings.RagnarokFireDamage.Value / BurnTicks;
        for (int i = 0; i < BurnTicks; i++)
        {
            yield return new WaitForSeconds(tick);
            foreach (WearNTear piece in pieces)
            {
                if (piece == null || piece.m_nview == null || !piece.m_nview.IsValid())
                {
                    continue;
                }

                HitData hit = new() { m_point = piece.transform.position, m_dir = Vector3.down, m_hitType = HitData.HitType.Burning };
                hit.m_damage.m_fire = piece.m_health * share;
                piece.Damage(hit);
            }
        }

        foreach (GameObject flame in flames.Where(flame => flame != null))
        {
            Destroy(flame);
        }
    }

    // A burning line coming down the sky, its glow lighting the ground below as it nears.
    private static IEnumerator DrawStreak(Vector3 from, Vector3 to)
    {
        if (streakMaterial == null && Shader.Find("Sprites/Default") is Shader shader && shader != null)
        {
            streakMaterial = new Material(shader);
        }

        GameObject holder = new("whitehilt_ragnarok_fire");
        LineRenderer line = streakMaterial != null ? holder.AddComponent<LineRenderer>() : null;
        if (line != null)
        {
            line.material = streakMaterial;
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = 0.4f;
            line.endWidth = 3f;
            line.startColor = new Color(1f, 0.35f, 0.05f, 0f);
            line.endColor = new Color(1f, 0.75f, 0.3f, 1f);
        }

        Light light = holder.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.45f, 0.1f);
        light.range = 50f;
        light.intensity = 4f;

        float start = Time.time;
        while (Time.time - start < FallSeconds)
        {
            float t = (Time.time - start) / FallSeconds;
            Vector3 head = Vector3.Lerp(from, to, t * t);
            holder.transform.position = head;
            if (line != null)
            {
                line.SetPosition(0, Vector3.Lerp(from, head, 0.6f));
                line.SetPosition(1, head);
            }

            yield return null;
        }

        Destroy(holder);
    }
}
