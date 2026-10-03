using BrudvikWhiteHilt.Helpers;
using UnityEngine;

namespace BrudvikWhiteHilt.Companions;

/// <summary>
/// On a dog house: using it while your grown dog follows you makes it the dog's new home, wherever it stands.
/// </summary>
public sealed class DogHouseHome : MonoBehaviour, Hoverable, Interactable
{
    private const float SameHouse = 0.5f;

    private Piece piece;

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
        string text = GetHoverName();
        Player player = Player.m_localPlayer;
        DogCompanion dog = player != null ? DogCompanion.FindOwnedBy(player.GetPlayerID()) : null;
        if (dog != null && LivesHere(dog))
        {
            text += "\n" + Localization.instance.Localize(Translations.Token("whitehilt_dog_lives_here"), dog.DogName);
        }
        else if (dog != null)
        {
            text += "\n[<color=yellow><b>$KEY_Use</b></color>] " + Localization.instance.Localize(Translations.Token("whitehilt_dog_make_home"), dog.DogName);
        }

        return Localization.instance.Localize(text);
    }

    /// <inheritdoc/>
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold || user is not Player player || !PrivateArea.CheckAccess(transform.position))
        {
            return false;
        }

        long playerId = player.GetPlayerID();
        DogCompanion dog = DogCompanion.FindOwnedBy(playerId);
        if (dog == null || LivesHere(dog))
        {
            return false;
        }

        if (dog.IsPuppy || dog.FollowsPlayerId != playerId
            || Vector3.Distance(dog.transform.position, transform.position) > DogCompanion.HomeRadius)
        {
            player.Message(MessageHud.MessageType.Center, Localization.instance.Localize(Translations.Token("whitehilt_dog_move_follow"), dog.DogName));
            return true;
        }

        dog.SetHome(transform.position);
        dog.Happy();
        player.Message(MessageHud.MessageType.Center, Localization.instance.Localize(Translations.Token("whitehilt_dog_new_home"), dog.DogName));
        return true;
    }

    /// <inheritdoc/>
    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        return false;
    }

    private void Awake()
    {
        piece = GetComponent<Piece>();
    }

    private bool LivesHere(DogCompanion dog)
    {
        return Vector3.Distance(dog.Home, transform.position) <= SameHouse;
    }
}
