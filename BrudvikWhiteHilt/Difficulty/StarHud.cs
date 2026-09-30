using UnityEngine;

namespace BrudvikWhiteHilt.Difficulty;

/// <summary>
/// Shows 3 to 5 stars over a creature. The game's HUD only has rows for 1 and 2 stars, so the 2 star row gets more stars.
/// </summary>
public static class StarHud
{
    private const string ExtraStarName = "whitehilt_star";

    // The vanilla 2 star row has its stars at x -8 and 8.
    private const float FirstStarX = -8f;
    private const float StarSpacing = 16f;

    /// <summary>
    /// Updates the star row of one HUD after the game's own update.
    /// </summary>
    /// <param name="level3">The HUD's 2 star row.</param>
    /// <param name="level">The creature's level.</param>
    public static void Apply(RectTransform level3, int level)
    {
        if (level3 == null)
        {
            return;
        }

        int extra = Mathf.Max(0, level - 3);
        if (extra > 0)
        {
            level3.gameObject.SetActive(true);
        }

        RectTransform template = null;
        int found = 0;
        foreach (Transform child in level3)
        {
            if (child.name == ExtraStarName)
            {
                child.gameObject.SetActive(found < extra);
                found++;
            }
            else if (template == null)
            {
                template = child as RectTransform;
            }
        }

        for (int i = found; i < extra && template != null; i++)
        {
            RectTransform star = Object.Instantiate(template, level3);
            star.name = ExtraStarName;
            star.anchoredPosition = new Vector2(FirstStarX + StarSpacing * (i + 2), template.anchoredPosition.y);
            star.gameObject.SetActive(true);
        }
    }
}
