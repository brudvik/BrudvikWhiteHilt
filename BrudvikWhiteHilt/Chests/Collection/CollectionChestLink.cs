using UnityEngine;

namespace BrudvikWhiteHilt.Chests.Collection;

/// <summary>Transfers a closed receiving chest with its authoritative inventory before collection begins.</summary>
internal sealed class CollectionChestLink : MonoBehaviour
{
    private Container container;
    private long sequence;
    private bool pending;
    private long revision = -1;
    private float requestedAt;

    private void Awake()
    {
        container = GetComponent<Container>();
        var view = container.m_nview;
        if (view == null || !view.IsValid()) return;
        view.Register<long, long>("WhiteHilt_CollectionRequest", Request);
        view.Register<long, ZPackage>("WhiteHilt_CollectionResponse", Response);
    }

    internal bool Ready(long playerId)
    {
        var view = container.m_nview;
        if (view == null || !view.IsValid() || container.IsInUse() || view.GetZDO().GetInt(ZDOVars.s_inUse) != 0) return false;
        if (pending)
        {
            if (revision >= 0 && view.IsOwner() && view.GetZDO().DataRevision >= revision)
            {
                container.Load();
                pending = false;
                revision = -1;
            }
            else
            {
                if (!view.IsOwner() && Time.time - requestedAt >= CollectionSettings.OwnershipRetry.Value)
                {
                    sequence++;
                    requestedAt = Time.time;
                    revision = -1;
                    view.InvokeRPC("WhiteHilt_CollectionRequest", playerId, sequence);
                }
                return false;
            }
        }
        if (view.IsOwner()) return true;
        pending = true;
        sequence++;
        requestedAt = Time.time;
        view.InvokeRPC("WhiteHilt_CollectionRequest", playerId, sequence);
        return false;
    }

    private void Request(long sender, long playerId, long request)
    {
        var view = container.m_nview;
        if (!view.IsOwner()) return;
        var data = new ZPackage();
        bool granted = !container.IsInUse() && container.CheckAccess(playerId);
        data.Write(granted);
        if (granted)
        {
            container.Load();
            container.Save();
            data.Write((long)view.GetZDO().DataRevision);
            ZDOMan.instance.ForceSendZDO(sender, view.GetZDO().m_uid);
            view.GetZDO().SetOwner(sender);
        }
        view.InvokeRPC(sender, "WhiteHilt_CollectionResponse", request, data);
    }

    private void Response(long sender, long request, ZPackage data)
    {
        if (!pending || request != sequence) return;
        if (data.ReadBool()) revision = data.ReadLong();
        else pending = false;
    }
}