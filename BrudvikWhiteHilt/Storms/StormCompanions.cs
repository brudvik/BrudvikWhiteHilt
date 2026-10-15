using BrudvikWhiteHilt.Companions;
using UnityEngine;

namespace BrudvikWhiteHilt.Storms;

/// <summary>
/// The player's dogs feel the storm: they whine and look toward home when it is coming, and whine now and then while
/// they are out in it.
/// </summary>
public static class StormCompanions
{
    private const float Range = 60f;
    private const float WhineSeconds = 40f;

    private static float nextWhine;

    /// <summary>
    /// The warning: every dog of the player's close by whines, and the player is told.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Warn(Player player)
    {
        DogCompanion first = null;
        foreach (DogCompanion dog in Dogs(player))
        {
            first ??= dog;
            dog.PlaySound("dogwhine");
        }

        if (first != null)
        {
            player.Message(MessageHud.MessageType.TopLeft, string.Format(Localization.instance.Localize("$msg_whitehilt_storm_dog"), first.DogName));
        }

        nextWhine = Time.time + WhineSeconds;
    }

    /// <summary>
    /// While the storm rages, a dog out in it whines now and then. Called from the storm's tick.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Tick(Player player)
    {
        if (!StormClient.RagingHere || Time.time < nextWhine)
        {
            return;
        }

        nextWhine = Time.time + WhineSeconds * Random.Range(0.75f, 1.25f);
        foreach (DogCompanion dog in Dogs(player))
        {
            Cover.GetCoverForPoint(dog.transform.position + Vector3.up, out _, out bool underRoof);
            if (!underRoof)
            {
                dog.PlaySound("dogwhine");
                return;
            }
        }
    }

    private static System.Collections.Generic.IEnumerable<DogCompanion> Dogs(Player player)
    {
        long id = player.GetPlayerID();
        foreach (DogCompanion dog in DogCompanion.All)
        {
            if (dog != null && dog.OwnerId == id && Vector3.Distance(dog.transform.position, player.transform.position) <= Range)
            {
                yield return dog;
            }
        }
    }
}
