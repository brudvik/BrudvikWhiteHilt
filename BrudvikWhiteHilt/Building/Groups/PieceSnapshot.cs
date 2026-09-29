using UnityEngine;

namespace BrudvikWhiteHilt.Building.Groups;

/// <summary>
/// A piece as data: its prefab, where it stands and its text (sign text or portal name), so it can be placed again.
/// </summary>
public sealed class PieceSnapshot
{
    /// <summary>Prefab name.</summary>
    public string Prefab;

    /// <summary>Position, in the world or relative to a blueprint's anchor.</summary>
    public Vector3 Position;

    /// <summary>Rotation, in the world or relative to a blueprint's anchor.</summary>
    public Quaternion Rotation = Quaternion.identity;

    /// <summary>Sign text or portal name, or null.</summary>
    public string Text;

    /// <summary>
    /// Takes a snapshot of a placed piece.
    /// </summary>
    /// <param name="piece">The piece.</param>
    /// <returns>The snapshot.</returns>
    public static PieceSnapshot Of(Piece piece)
    {
        string text = piece.GetComponentInChildren<TextReceiver>()?.GetText();
        return new PieceSnapshot
        {
            Prefab = Utils.GetPrefabName(piece.gameObject),
            Position = piece.transform.position,
            Rotation = piece.transform.rotation,
            Text = string.IsNullOrEmpty(text) ? null : text
        };
    }

    /// <summary>
    /// Writes the text back onto a newly placed piece.
    /// </summary>
    /// <param name="placed">The new piece.</param>
    public void ApplyText(Piece placed)
    {
        if (!string.IsNullOrEmpty(Text) && placed != null)
        {
            placed.GetComponentInChildren<TextReceiver>()?.SetText(Text);
        }
    }
}
