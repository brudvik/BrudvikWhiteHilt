using BrudvikWhiteHilt.Helpers;
using UnityEngine;

namespace BrudvikWhiteHilt.Companions;

/// <summary>
/// Tricks the dog does when its owner makes an emote near it: sit, lie down, stay, come, give paw and roll over.
/// Stay and come need no teaching; the others take three lessons, each with a Dog Treat from the owner's pack.
/// </summary>
public sealed class DogTricks : MonoBehaviour
{
    /// <summary>
    /// Lessons before a trick is learnt.
    /// </summary>
    public static int LessonsToLearn => DogSettings.LessonsToLearn.Value;

    private const string TrickRpc = "WhiteHilt_DogTrick";
    private const float EmoteRange = 10f;
    private const float HoldSeconds = 20f;
    private static float TrickXp => DogSettings.TrickXp.Value;
    private const float HintSeconds = 600f;

    private static readonly int learnedKey = "whitehilt_dog_tricks".GetStableHashCode();
    private static readonly string[] lessonKeys = { "sit", "down", "stay", "come", "paw", "roll" };

    private static float nextHint;

    private ZNetView nview;
    private DogCompanion dog;
    private DogCare care;

    /// <summary>
    /// The tricks.
    /// </summary>
    public enum Trick
    {
        /// <summary>Sits.</summary>
        Sit = 0,

        /// <summary>Lies down.</summary>
        Down = 1,

        /// <summary>Stays where it is instead of following.</summary>
        Stay = 2,

        /// <summary>Comes and follows.</summary>
        Come = 3,

        /// <summary>Gives its paw.</summary>
        Paw = 4,

        /// <summary>Rolls over.</summary>
        Roll = 5
    }

    private ZDO Zdo => nview != null && nview.IsValid() ? nview.GetZDO() : null;

    /// <summary>
    /// Handles an emote the local player just made: the nearby own dog does the trick, or learns it with a treat.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="emote">The emote's name, e.g. "sit".</param>
    public static void OnEmote(Player player, string emote)
    {
        if (!TryGetTrick(emote, out Trick trick))
        {
            return;
        }

        DogCompanion dog = DogCompanion.FindOwnedBy(player.GetPlayerID());
        if (dog == null || Vector3.Distance(dog.transform.position, player.transform.position) > EmoteRange
            || !dog.TryGetComponent(out DogTricks tricks))
        {
            return;
        }

        // A puppy cannot follow, so it has nothing to stay or come for.
        if (dog.IsPuppy && (trick == Trick.Stay || trick == Trick.Come))
        {
            return;
        }

        if (trick == Trick.Come)
        {
            dog.Call(player);
            return;
        }

        string trickName = Localization.instance.Localize(Translations.Token("whitehilt_dog_trick_" + lessonKeys[(int)trick]));
        if (tricks.Knows(trick))
        {
            tricks.Do(trick, lesson: false);
            return;
        }

        Inventory inventory = player.GetInventory();
        ItemDrop.ItemData treat = inventory.GetItem(DogRegistry.TreatPrefabName, isPrefabName: true);
        if (treat == null)
        {
            // Players sit and rest for other reasons too, so the hint comes now and then only.
            if (Time.time >= nextHint)
            {
                nextHint = Time.time + HintSeconds;
                player.Message(MessageHud.MessageType.Center,
                    Localization.instance.Localize(Translations.Token("whitehilt_dog_trick_unknown"), dog.DogName, trickName));
            }

            return;
        }

        inventory.RemoveOneItem(treat);
        int lessons = Mathf.Min(LessonsToLearn, tricks.Lessons(trick) + 1);
        tricks.Do(trick, lesson: true);
        string message = lessons >= LessonsToLearn
            ? Localization.instance.Localize(Translations.Token("whitehilt_dog_trick_learned"), dog.DogName, trickName)
            : Localization.instance.Localize(Translations.Token("whitehilt_dog_trick_lesson"), dog.DogName, trickName, lessons.ToString());
        player.Message(MessageHud.MessageType.Center, message);
    }

    /// <summary>
    /// True when the dog knows a trick.
    /// </summary>
    /// <param name="trick">The trick.</param>
    /// <returns>Whether it is learnt.</returns>
    public bool Knows(Trick trick)
    {
        return trick == Trick.Stay || trick == Trick.Come || (Zdo != null && (Zdo.GetInt(learnedKey) & (1 << (int)trick)) != 0);
    }

    /// <summary>
    /// Lessons the dog has had in a trick.
    /// </summary>
    /// <param name="trick">The trick.</param>
    /// <returns>The number of lessons.</returns>
    public int Lessons(Trick trick)
    {
        return Zdo?.GetInt(LessonKey(trick)) ?? 0;
    }

    /// <summary>
    /// Asks the dog's owner to do a trick, as a lesson or because it is known.
    /// </summary>
    /// <param name="trick">The trick.</param>
    /// <param name="lesson">True when a treat was given for it.</param>
    public void Do(Trick trick, bool lesson)
    {
        if (Zdo != null)
        {
            nview.InvokeRPC(TrickRpc, (int)trick, lesson);
        }
    }

    private static bool TryGetTrick(string emote, out Trick trick)
    {
        switch (emote)
        {
            case "sit":
                trick = Trick.Sit;
                return true;
            case "rest":
                trick = Trick.Down;
                return true;
            case "point":
                trick = Trick.Stay;
                return true;
            case "comehere":
                trick = Trick.Come;
                return true;
            case "bow":
            case "kneel":
                trick = Trick.Paw;
                return true;
            case "dance":
                trick = Trick.Roll;
                return true;
            default:
                trick = Trick.Sit;
                return false;
        }
    }

    private static int LessonKey(Trick trick)
    {
        return ("whitehilt_dog_lesson_" + lessonKeys[(int)trick]).GetStableHashCode();
    }

    private void Awake()
    {
        nview = GetComponent<ZNetView>();
        dog = GetComponent<DogCompanion>();
        care = GetComponent<DogCare>();
        if (nview != null && nview.GetZDO() != null)
        {
            nview.Register<int, bool>(TrickRpc, RPC_Trick);
        }
    }

    private void RPC_Trick(long sender, int trickIndex, bool lesson)
    {
        if (!nview.IsOwner())
        {
            return;
        }

        Trick trick = (Trick)trickIndex;
        if (lesson)
        {
            int lessons = Lessons(trick) + 1;
            Zdo.Set(LessonKey(trick), lessons);
            if (lessons >= LessonsToLearn)
            {
                Zdo.Set(learnedKey, Zdo.GetInt(learnedKey) | (1 << trickIndex));
            }

            care.Give(DogCare.CareItem.Treat);
        }
        else
        {
            dog.AddXp(TrickXp);
            dog.Happy();
        }

        switch (trick)
        {
            case Trick.Sit:
                care.HoldPose(RestPose.Pose.Sit, HoldSeconds);
                break;
            case Trick.Down:
                dog.Stay();
                care.HoldPose(RestPose.Pose.Lie, HoldSeconds);
                break;
            case Trick.Stay:
                dog.Stay();
                care.HoldPose(RestPose.Pose.Sit, HoldSeconds);
                break;
            case Trick.Paw:
                care.Request(DogAction.Paw);
                break;
            case Trick.Roll:
                care.Request(DogAction.Roll);
                break;
        }
    }
}
