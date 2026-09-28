using BrudvikWhiteHilt.Items.Runes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Portals.RuneRack;

/// <summary>
/// Holds the runes hung on a rune post. The runes are a bit mask in the post's ZDO, so every player sees the same state.
/// </summary>
public class RuneRackComponent : MonoBehaviour, Hoverable, Interactable
{
    /// <summary>
    /// Child that holds one ring per rune, named <c>rune_0</c> to <c>rune_5</c>.
    /// </summary>
    public const string RingsName = "RuneRings";

    /// <summary>
    /// Child with the particles and light shown while every rune hangs on the post.
    /// </summary>
    public const string FullSetEffectName = "FullSetEffect";

    private const string ZdoKey = "whitehilt_runes";
    private const string AddRuneRpc = "WhiteHiltAddRune";
    private const string TakeRuneRpc = "WhiteHiltTakeRune";

    private static readonly List<RuneRackComponent> instances = new();

    private ZNetView nview;
    private Piece piece;
    private GameObject[] rings;
    private EffectFade fullSetEffect;
    private int shownMask = -1;

    /// <summary>
    /// Every loaded rune post.
    /// </summary>
    public static IReadOnlyList<RuneRackComponent> Instances => instances;

    /// <summary>
    /// Bit mask of the runes on the post.
    /// </summary>
    public int Mask => nview != null && nview.IsValid() ? nview.GetZDO().GetInt(ZdoKey) : 0;

    /// <inheritdoc/>
    public string GetHoverName()
    {
        return piece != null ? piece.m_name : string.Empty;
    }

    /// <inheritdoc/>
    public float GetHoverOffset()
    {
        return 0f;
    }

    /// <inheritdoc/>
    public string GetHoverText()
    {
        if (!nview.IsValid())
        {
            return string.Empty;
        }

        int mask = Mask;
        string text = $"{GetHoverName()}\n";
        if (mask != 0)
        {
            text += "[<color=yellow><b>$KEY_Use</b></color>] $whitehilt_runerack_take\n";
        }

        text += "[<color=yellow><b>1-8</b></color>] $whitehilt_runerack_hang\n";
        text += mask == 0 ? "$whitehilt_runerack_empty" : $"$whitehilt_runerack_runes: {RuneNames(mask)}";
        if (mask == WhiteHiltRuneBase.FullMask)
        {
            text += "\n<color=orange>$whitehilt_runerack_everything</color>";
        }

        return Localization.instance.Localize(text);
    }

    /// <inheritdoc/>
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        int mask = Mask;
        if (hold || mask == 0 || !PrivateArea.CheckAccess(transform.position))
        {
            return false;
        }

        // Take the last rune that was hung, so repeated presses empty the post from the right.
        int index = Enumerable.Range(0, WhiteHiltRuneBase.Count).Last(i => (mask & (1 << i)) != 0);
        nview.InvokeRPC(TakeRuneRpc, index);
        return true;
    }

    /// <inheritdoc/>
    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        WhiteHiltRuneBase rune = WhiteHiltRuneBase.FromItem(item);
        if (rune == null)
        {
            return false;
        }

        if (!PrivateArea.CheckAccess(transform.position))
        {
            return true;
        }

        int mask = Mask;
        int bit = 1 << rune.Index;
        if ((mask & bit) != 0)
        {
            user.Message(MessageHud.MessageType.Center, "$msg_whitehilt_rune_already");
            return true;
        }

        user.GetInventory().RemoveOneItem(item);
        nview.InvokeRPC(AddRuneRpc, rune.Index);
        user.Message(MessageHud.MessageType.Center, (mask | bit) == WhiteHiltRuneBase.FullMask
            ? "$msg_whitehilt_runes_complete"
            : Localization.instance.Localize($"$msg_whitehilt_rune_hung: {rune.NameToken}"));
        return true;
    }

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        piece = GetComponent<Piece>();
        Transform ringRoot = transform.Find(RingsName);
        rings = Enumerable.Range(0, WhiteHiltRuneBase.Count).Select(i => ringRoot?.Find($"rune_{i}")?.gameObject).ToArray();
        fullSetEffect = transform.Find(FullSetEffectName)?.GetComponent<EffectFade>();

        if (nview == null || nview.GetZDO() == null)
        {
            return;
        }

        nview.Register<int>(AddRuneRpc, RPC_AddRune);
        nview.Register<int>(TakeRuneRpc, RPC_TakeRune);
        WearNTear wearNTear = GetComponent<WearNTear>();
        if (wearNTear != null)
        {
            wearNTear.m_onDestroyed += DropAllRunes;
        }

        instances.Add(this);
        UpdateVisuals();
    }

    private void OnDestroy()
    {
        instances.Remove(this);
    }

    private void Update()
    {
        if (nview.IsValid() && Mask != shownMask)
        {
            UpdateVisuals();
        }
    }

    private void RPC_AddRune(long sender, int index)
    {
        if (!nview.IsOwner())
        {
            return;
        }

        int mask = Mask;
        int bit = 1 << index;
        if ((mask & bit) != 0)
        {
            // Two players hung the same rune at once: give the second one back.
            DropRune(index);
            return;
        }

        nview.GetZDO().Set(ZdoKey, mask | bit);
    }

    private void RPC_TakeRune(long sender, int index)
    {
        if (!nview.IsOwner())
        {
            return;
        }

        int mask = Mask;
        int bit = 1 << index;
        if ((mask & bit) == 0)
        {
            return;
        }

        nview.GetZDO().Set(ZdoKey, mask & ~bit);
        DropRune(index);
    }

    private void DropAllRunes()
    {
        int mask = Mask;
        for (int i = 0; i < WhiteHiltRuneBase.Count; i++)
        {
            if ((mask & (1 << i)) != 0)
            {
                DropRune(i);
            }
        }

        nview.GetZDO().Set(ZdoKey, 0);
    }

    private void DropRune(int index)
    {
        GameObject prefab = ZNetScene.instance.GetPrefab(WhiteHiltRuneBase.Get(index)?.PrefabName ?? string.Empty);
        if (prefab != null)
        {
            Vector3 position = transform.position + transform.forward * 0.6f + Vector3.up * 1.2f;
            Instantiate(prefab, position, Quaternion.identity);
        }
    }

    private void UpdateVisuals()
    {
        int mask = Mask;
        shownMask = mask;
        for (int i = 0; i < rings.Length; i++)
        {
            rings[i]?.SetActive((mask & (1 << i)) != 0);
        }

        fullSetEffect?.SetActive(mask == WhiteHiltRuneBase.FullMask);
    }

    private static string RuneNames(int mask)
    {
        return string.Join(", ", Enumerable.Range(0, WhiteHiltRuneBase.Count)
            .Where(i => (mask & (1 << i)) != 0)
            .Select(i => WhiteHiltRuneBase.Get(i)?.NameToken)
            .Where(name => name != null));
    }
}
