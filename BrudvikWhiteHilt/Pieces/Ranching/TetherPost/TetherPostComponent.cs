using BrudvikWhiteHilt.Ranching;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Ranching.TetherPost;

/// <summary>
/// Tethers the tame animals within <see cref="AnimalCare.TetherRange"/> to the post, or sets them free.
/// </summary>
public class TetherPostComponent : MonoBehaviour, Hoverable, Interactable
{
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
        int tethered = FindTameAnimals().Count(animal => AnimalCare.IsTethered(animal) && IsTetheredHere(animal));
        return Localization.instance.Localize(
            $"{GetHoverName()}\n" +
            "[<color=yellow><b>$KEY_Use</b></color>] $whitehilt_tether_animals\n" +
            "[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] $whitehilt_tether_release\n" +
            $"$whitehilt_tether_count: {tethered}");
    }

    /// <inheritdoc/>
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold)
        {
            return false;
        }

        List<Tameable> animals = FindTameAnimals().ToList();
        if (animals.Count == 0)
        {
            user.Message(MessageHud.MessageType.Center, "$msg_whitehilt_tether_none");
            return true;
        }

        foreach (Tameable animal in animals)
        {
            AnimalCare.Tether(animal, transform.position, !alt);
        }

        user.Message(MessageHud.MessageType.Center, $"{animals.Count} {(alt ? "$msg_whitehilt_released" : "$msg_whitehilt_tethered")}");
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

    private IEnumerable<Tameable> FindTameAnimals()
    {
        return Character.GetAllCharacters()
            .Where(character => !character.IsPlayer() && character.IsTamed()
                && Vector3.Distance(character.transform.position, transform.position) <= AnimalCare.TetherRange)
            .Select(character => character.GetComponent<Tameable>())
            .Where(tameable => tameable != null);
    }

    private bool IsTetheredHere(Tameable animal)
    {
        ZDO zdo = animal.m_nview.GetZDO();
        return Vector3.Distance(zdo.GetVec3(ZDOVars.s_patrolPoint, Vector3.zero), transform.position) < 0.5f;
    }
}
