using BrudvikWhiteHilt.Navigation;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Navigation;

/// <summary>
/// Shows one gem on the worn Pathfinder's Amulet per twenty levels of the wearer's Exploration skill.
/// The gems are the direct children gem_1 to gem_5.
/// </summary>
public class AmuletGems : MonoBehaviour
{
    /// <summary>
    /// Number of gems.
    /// </summary>
    public const int Count = 5;

    private const int LevelsPerGem = 20;
    private const float RefreshInterval = 2f;

    private GameObject[] gems;
    private Player wearer;
    private int shown = -1;
    private float refreshTimer;

    /// <summary>
    /// Name of a gem.
    /// </summary>
    /// <param name="index">Gem, from 1 to <see cref="Count"/>.</param>
    /// <returns>The child name.</returns>
    public static string GemName(int index)
    {
        return $"gem_{index}";
    }

    private void Awake()
    {
        gems = Enumerable.Range(1, Count).Select(i => transform.Find(GemName(i))?.gameObject).ToArray();
        wearer = GetComponentInParent<Player>();
    }

    // Now and then shows one more gem on the amulet for every few levels of the wearer's exploration skill, as a
    // visible sign of it.
    private void Update()
    {
        refreshTimer -= Time.deltaTime;
        if (refreshTimer > 0f)
        {
            return;
        }

        refreshTimer = RefreshInterval;
        int count = Mathf.Clamp(ExplorationSkill.GetLevel(wearer) / LevelsPerGem, 0, Count);
        if (count == shown)
        {
            return;
        }

        shown = count;
        for (int i = 0; i < gems.Length; i++)
        {
            if (gems[i] != null)
            {
                gems[i].SetActive(i < count);
            }
        }
    }
}
