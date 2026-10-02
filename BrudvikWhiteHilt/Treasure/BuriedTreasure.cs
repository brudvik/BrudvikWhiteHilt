using BrudvikWhiteHilt.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Treasure;

/// <summary>
/// The heap of dug earth over a treasure, with a cairn beside it. A few pickaxe blows dig the chest up. A player
/// carrying its map is told when the ground nearby looks dug, and sees dust rise from it when close.
/// </summary>
public class BuriedTreasure : MonoBehaviour, IDestructible, Hoverable
{
    /// <summary>ZDO key of the treasure id, shared with its map.</summary>
    public const string IdKey = "whitehilt_treasure_id";

    /// <summary>ZDO key of the player id of the buyer.</summary>
    public const string BuyerKey = "whitehilt_treasure_buyer";

    /// <summary>ZDO key of the middle of the map, as a vector with y = 0.</summary>
    public const string CentreKey = "whitehilt_treasure_centre";

    /// <summary>ZDO key of the width of the map.</summary>
    public const string SizeKey = "whitehilt_treasure_size";

    private const string DugKey = "whitehilt_treasure_dug";
    private const string SettledKey = "whitehilt_treasure_settled";
    private const string DigRpc = "WhiteHiltTreasureDig";

    // Zone objects (trees, rocks) are still being created for a moment after the mound appears.
    private const float SettleDelay = 3f;
    private const float SettleClearance = 1.4f;
    private const float SettleSearch = 10f;
    private const float CheckInterval = 1f;
    private const float DustInterval = 2.5f;

    private static readonly HashSet<string> warned = new();
    private static readonly int obstacleMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "vehicle");

    /// <summary>Effects of a pickaxe blow, copied from the vanilla mud pile.</summary>
    public EffectList m_hitEffect = new();

    private ZNetView nview;
    private float nextCheck;
    private float nextDust;
    private bool carried;

    /// <inheritdoc />
    public DestructibleType GetDestructibleType()
    {
        return DestructibleType.Default;
    }

    /// <inheritdoc />
    public void Damage(HitData hit)
    {
        if (nview == null || !nview.IsValid())
        {
            return;
        }

        if (hit.m_damage.m_pickaxe <= 0f)
        {
            DamageText.instance.ShowText(DamageText.TextType.Immune, hit.m_point, 0f);
            return;
        }

        m_hitEffect.Create(hit.m_point, Quaternion.identity, transform);
        nview.InvokeRPC(DigRpc, hit.m_point);
    }

    /// <inheritdoc />
    public string GetHoverText()
    {
        int dug = nview != null && nview.IsValid() ? nview.GetZDO().GetInt(DugKey) : 0;
        string text = Localization.instance.Localize("$whitehilt_treasure_mound\n$whitehilt_treasure_mound_hint");
        return dug > 0 ? $"{text} ({dug}/{TreasureSettings.DigHits.Value})" : text;
    }

    /// <inheritdoc />
    public string GetHoverName()
    {
        return Localization.instance.Localize("$whitehilt_treasure_mound");
    }

    /// <inheritdoc />
    public float GetHoverOffset()
    {
        return 0f;
    }

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        if (nview == null || nview.GetZDO() == null)
        {
            enabled = false;
            return;
        }

        nview.Register<Vector3>(DigRpc, RPC_Dig);
        nextCheck = Time.time + Random.Range(0f, CheckInterval);
    }

    private void Start()
    {
        if (nview != null && nview.IsValid() && nview.IsOwner() && !nview.GetZDO().GetBool(SettledKey))
        {
            Invoke(nameof(Settle), SettleDelay);
        }
    }

    private void Update()
    {
        if (Time.time < nextCheck || nview == null || !nview.IsValid())
        {
            return;
        }

        nextCheck = Time.time + CheckInterval;
        Player player = Player.m_localPlayer;
        if (player == null || TreasureSettings.HintLevel.Value == TreasureHintLevel.Hard)
        {
            return;
        }

        string id = nview.GetZDO().GetString(IdKey);
        float distance = Utils.DistanceXZ(player.transform.position, transform.position);
        float warm = TreasureSettings.WarmDistance.Value;
        float dust = TreasureSettings.DustDistance.Value;
        carried = distance <= Mathf.Max(warm, dust) && TreasureMapItem.Carries(player, id);
        if (!carried)
        {
            return;
        }

        if (distance <= warm && warned.Add(id))
        {
            player.Message(MessageHud.MessageType.Center, Translations.Word("msg_whitehilt_treasure_warm"));
        }

        if (distance <= dust && Time.time >= nextDust)
        {
            nextDust = Time.time + DustInterval;
            TreasureRegistry.DustEffect?.Create(transform.position + Vector3.up * 0.2f, Quaternion.identity);
        }
    }

    // Owner only: the mound moves onto the real ground, clear of trees and rocks that grew where the server put it.
    private void Settle()
    {
        if (nview == null || !nview.IsValid() || !nview.IsOwner() || nview.GetZDO().GetBool(SettledKey))
        {
            return;
        }

        Vector3 best = transform.position;
        if (!IsClear(best))
        {
            bool found = false;
            for (float radius = 2f; radius <= SettleSearch && !found; radius += 2f)
            {
                for (int step = 0; step < 8 && !found; step++)
                {
                    float angle = step * Mathf.PI / 4f;
                    Vector3 point = transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                    if (IsClear(point))
                    {
                        best = point;
                        found = true;
                    }
                }
            }
        }

        if (ZoneSystem.instance.GetGroundHeight(best, out float height))
        {
            best.y = height;
        }

        transform.position = best;
        nview.GetZDO().SetPosition(best);
        nview.GetZDO().Set(SettledKey, true);
    }

    private bool IsClear(Vector3 point)
    {
        foreach (Collider hit in Physics.OverlapSphere(point + Vector3.up * (SettleClearance + 0.2f), SettleClearance, obstacleMask))
        {
            if (!hit.transform.IsChildOf(transform) && hit.GetComponentInParent<Character>() == null)
            {
                return false;
            }
        }

        return true;
    }

    private void RPC_Dig(long sender, Vector3 point)
    {
        if (!nview.IsValid() || !nview.IsOwner())
        {
            return;
        }

        ZDO zdo = nview.GetZDO();
        int dug = zdo.GetInt(DugKey) + 1;
        zdo.Set(DugKey, dug);
        if (dug >= TreasureSettings.DigHits.Value)
        {
            Unearth(zdo.GetString(IdKey));
        }
    }

    private void Unearth(string id)
    {
        GameObject prefab = ZNetScene.instance.GetPrefab(TreasureRegistry.ChestPrefabName);
        if (prefab == null)
        {
            Jotunn.Logger.LogWarning("Treasure: the chest prefab is missing, the treasure stays buried");
            return;
        }

        GameObject chest = Instantiate(prefab, transform.position, transform.rotation);
        Container container = chest.GetComponent<Container>();
        TreasureLoot.Fill(container.GetInventory());
        chest.GetComponent<ZNetView>()?.GetZDO()?.Set(IdKey, id);
        TreasureRegistry.UnearthEffect?.Create(transform.position, Quaternion.identity);
        TreasureService.AnnounceFound(id);
        ZNetScene.instance.Destroy(gameObject);
    }
}
