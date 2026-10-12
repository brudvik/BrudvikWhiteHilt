using BrudvikWhiteHilt.Chests;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BrudvikWhiteHilt.Quartermaster;

public partial class QuartermasterPanel
{
    // ---- Watch page ------------------------------------------------------------------------------------------------

    // Shows every stock watch with what the chests hold now; a watch below its threshold is red. The same counts drive
    // the warning on the table's hover text.
    private void RefreshWatches()
    {
        watchRows.ForEach(Destroy);
        watchRows.Clear();
        List<Container> sources = Sources();
        float width = Width - Padding * 2f - 20f;
        foreach (KeyValuePair<string, int> watch in watches)
        {
            QuartermasterStore.CountStock(sources, watch.Key, out int have, out bool unlimited);
            string amount = unlimited ? Localization.instance.Localize("$whitehilt_qm_unlimited") : have.ToString();
            string line = $"<b>{QuartermasterSite.ItemName(watch.Key)}</b>  "
                + string.Format(Localization.instance.Localize("$whitehilt_qm_watch_line"), amount, watch.Value);
            bool isLow = !unlimited && have < watch.Value;
            string prefab = watch.Key;
            watchRows.Add(CreateLine(watchContent, width, isLow ? $"<color=#{ColorUtility.ToHtmlStringRGB(lackingColor)}>{line}</color>" : line,
                () => ChangeWatch(prefab, -StepSize()), () => ChangeWatch(prefab, StepSize()), () => ChangeWatch(prefab, -int.MaxValue / 2)));
        }

        watchHint.text = Localization.instance.Localize(watches.Count == 0 ? "$whitehilt_qm_watch_empty" : "$whitehilt_qm_watch_hint");
    }

    // Raises or lowers a watch's threshold, removing the watch at zero, and saves the watches on the table so every
    // player sees them.
    private void ChangeWatch(string prefab, int change)
    {
        int index = watches.FindIndex(watch => watch.Key == prefab);
        if (index < 0)
        {
            return;
        }

        int threshold = watches[index].Value + change;
        if (threshold <= 0)
        {
            watches.RemoveAt(index);
        }
        else
        {
            watches[index] = new KeyValuePair<string, int>(prefab, threshold);
        }

        stand.SetWatches(watches);
        Refresh();
    }
}
