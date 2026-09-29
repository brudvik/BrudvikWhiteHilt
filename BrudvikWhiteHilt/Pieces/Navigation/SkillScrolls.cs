using BrudvikWhiteHilt.Navigation;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Navigation;

/// <summary>
/// Shows one map scroll per ten levels of the local player's Exploration skill, plus one, on a navigation piece.
/// The scrolls are the direct children scroll_1 to scroll_11.
/// </summary>
public class SkillScrolls : MonoBehaviour
{
    /// <summary>
    /// Number of scroll slots.
    /// </summary>
    public const int Count = 11;

    private const int LevelsPerScroll = 10;
    private const float RefreshInterval = 2f;

    private GameObject[] scrolls;
    private int shown = -1;
    private float refreshTimer;

    /// <summary>
    /// Name of a scroll slot.
    /// </summary>
    /// <param name="index">Slot, from 1 to <see cref="Count"/>.</param>
    /// <returns>The child name.</returns>
    public static string SlotName(int index)
    {
        return $"scroll_{index}";
    }

    private void Awake()
    {
        scrolls = Enumerable.Range(1, Count).Select(i => transform.Find(SlotName(i))?.gameObject).ToArray();
    }

    private void OnEnable()
    {
        refreshTimer = 0f;
    }

    private void Update()
    {
        refreshTimer -= Time.deltaTime;
        if (refreshTimer > 0f)
        {
            return;
        }

        refreshTimer = RefreshInterval;
        int count = Mathf.Clamp(1 + ExplorationSkill.GetLevel(Player.m_localPlayer) / LevelsPerScroll, 1, Count);
        if (count == shown)
        {
            return;
        }

        shown = count;
        for (int i = 0; i < scrolls.Length; i++)
        {
            if (scrolls[i] != null)
            {
                scrolls[i].SetActive(i < count);
            }
        }
    }
}
