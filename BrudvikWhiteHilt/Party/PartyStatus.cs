using System;
using System.Globalization;

namespace BrudvikWhiteHilt.Party;

/// <summary>
/// What a player shares about themselves with the others for the party list: health, stamina and eitr with their
/// maximums, whether they are dead, and a few status effects. Small enough to send twice a second.
/// </summary>
public sealed class PartyStatus
{
    /// <summary>Most status effects sent per player.</summary>
    public const int MaxEffects = 4;

    private const byte Version = 1;

    /// <summary>The player's name.</summary>
    public string Name = string.Empty;

    /// <summary>Current health.</summary>
    public float Health;

    /// <summary>Maximum health.</summary>
    public float MaxHealth;

    /// <summary>Current stamina.</summary>
    public float Stamina;

    /// <summary>Maximum stamina.</summary>
    public float MaxStamina;

    /// <summary>Current eitr.</summary>
    public float Eitr;

    /// <summary>Maximum eitr; 0 when the player has none.</summary>
    public float MaxEitr;

    /// <summary>Whether the player is dead.</summary>
    public bool Dead;

    /// <summary>Name hashes of the status effects shown, worst first.</summary>
    public int[] Effects = Array.Empty<int>();

    /// <summary>
    /// Writes the status to a package.
    /// </summary>
    /// <param name="package">The package to write to.</param>
    public void Write(ZPackage package)
    {
        package.Write(Version);
        package.Write(Name ?? string.Empty);
        package.Write(Health);
        package.Write(MaxHealth);
        package.Write(Stamina);
        package.Write(MaxStamina);
        package.Write(Eitr);
        package.Write(MaxEitr);
        package.Write(Dead);
        int count = Math.Min(Effects?.Length ?? 0, MaxEffects);
        package.Write((byte)count);
        for (int i = 0; i < count; i++)
        {
            package.Write(Effects[i]);
        }
    }

    /// <summary>
    /// Reads a status written by <see cref="Write"/>.
    /// </summary>
    /// <param name="package">The package to read from.</param>
    /// <returns>The status, or null when it is from a newer version or broken.</returns>
    public static PartyStatus Read(ZPackage package)
    {
        try
        {
            if (package.ReadByte() != Version)
            {
                return null;
            }

            PartyStatus status = new()
            {
                Name = package.ReadString(),
                Health = package.ReadSingle(),
                MaxHealth = package.ReadSingle(),
                Stamina = package.ReadSingle(),
                MaxStamina = package.ReadSingle(),
                Eitr = package.ReadSingle(),
                MaxEitr = package.ReadSingle(),
                Dead = package.ReadBool()
            };
            int count = Math.Min((int)package.ReadByte(), MaxEffects);
            status.Effects = new int[count];
            for (int i = 0; i < count; i++)
            {
                status.Effects[i] = package.ReadInt();
            }

            return status;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Whether this status is worth sending after the last one sent: a whole point of health, stamina or eitr, a new
    /// maximum, death or a change in the status effects.
    /// </summary>
    /// <param name="sent">The status sent last, or null.</param>
    /// <returns>True when it should be sent.</returns>
    public bool DiffersFrom(PartyStatus sent)
    {
        if (sent == null || sent.Dead != Dead || sent.Name != Name)
        {
            return true;
        }

        if (Math.Abs(sent.Health - Health) >= 1f || Math.Abs(sent.Stamina - Stamina) >= 1f || Math.Abs(sent.Eitr - Eitr) >= 1f
            || Math.Abs(sent.MaxHealth - MaxHealth) >= 0.5f || Math.Abs(sent.MaxStamina - MaxStamina) >= 0.5f
            || Math.Abs(sent.MaxEitr - MaxEitr) >= 0.5f)
        {
            return true;
        }

        int[] before = sent.Effects ?? Array.Empty<int>();
        int[] now = Effects ?? Array.Empty<int>();
        if (before.Length != now.Length)
        {
            return true;
        }

        for (int i = 0; i < now.Length; i++)
        {
            if (before[i] != now[i])
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// An amount as the party list shows it, "184/250". The current value is rounded up, so a player with a sliver of
    /// health left never shows 0.
    /// </summary>
    /// <param name="value">The current value.</param>
    /// <param name="max">The maximum.</param>
    /// <returns>The text.</returns>
    public static string Amount(float value, float max)
    {
        int now = Math.Max(0, (int)Math.Ceiling(value - 0.01f));
        return now.ToString(CultureInfo.InvariantCulture) + "/" + ((int)Math.Round(max)).ToString(CultureInfo.InvariantCulture);
    }
}
