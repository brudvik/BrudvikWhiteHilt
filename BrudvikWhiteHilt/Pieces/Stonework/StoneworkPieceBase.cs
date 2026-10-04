using BrudvikWhiteHilt.Pieces.Defenses;
using Jotunn.Configs;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Pieces.Stonework;

/// <summary>
/// A stone piece from the layout, built near the Stonecutter: it is stone in the building rules, does not wear in the rain
/// and sounds like a stone wall when struck or broken.
/// </summary>
public abstract class StoneworkPieceBase : DefensePieceBase
{
    /// <summary>
    /// Constructor for the StoneworkPieceBase class.
    /// </summary>
    /// <param name="instance">The piece manager.</param>
    protected StoneworkPieceBase(PieceManager instance) : base(instance)
    {
    }

    /// <inheritdoc/>
    protected override string BuildStation => CraftingStations.Stonecutter;

    /// <summary>
    /// Makes the piece stone, then lets the subclass change it further.
    /// </summary>
    /// <param name="prefab">The piece prefab.</param>
    /// <param name="data">The piece in the layout.</param>
    /// <param name="groups">Moving groups by name.</param>
    protected sealed override void CustomizePrefab(GameObject prefab, DefensePieceData data, IDictionary<string, Transform> groups)
    {
        WearNTear wear = prefab.GetComponent<WearNTear>();
        WearNTear stone = PrefabManager.Instance.GetPrefab("stone_wall_1x1")?.GetComponent<WearNTear>();
        wear.m_materialType = WearNTear.MaterialType.Stone;
        wear.m_noRoofWear = false;
        if (stone != null)
        {
            wear.m_hitEffect = stone.m_hitEffect;
            wear.m_destroyedEffect = stone.m_destroyedEffect;
        }

        CustomizeStone(prefab);
    }

    /// <summary>
    /// Changes the prefab after it is made stone.
    /// </summary>
    /// <param name="prefab">The piece prefab.</param>
    protected virtual void CustomizeStone(GameObject prefab)
    {
    }
}
