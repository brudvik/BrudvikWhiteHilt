using UnityEngine;

namespace BrudvikWhiteHilt.TradingPosts;

/// <summary>
/// The invisible marker at a trading post's origin, turned the way the post faces. It carries the dome on every
/// machine that loads it; the trader and the post's other behaviour come here in later phases.
/// </summary>
public class TradingPostSite : MonoBehaviour
{
    private ZNetView view;

    /// <summary>
    /// Gets the post's id, the same on all its pieces.
    /// </summary>
    public string PostId => view != null && view.IsValid() ? view.GetZDO().GetString(TradingPostKeys.PostId) : null;

    private void Awake()
    {
        view = GetComponent<ZNetView>();
        if (view == null || !view.IsValid())
        {
            return;
        }

        TradingPostLayout layout = TradingPostLayout.Load();
        TradingPostDome.Attach(transform, TradingPostLayout.ToVector(layout.dome.centre), layout.dome.radius);
    }
}
