using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Production;

/// <summary>
/// The kinds of station that get production timers.
/// </summary>
public enum ProductionKind
{
    /// <summary>Smelters, kilns, spinning wheels, windmills and the eitr refinery.</summary>
    Smelter,

    /// <summary>Fermenters.</summary>
    Fermenter,

    /// <summary>Cooking stations and ovens.</summary>
    Cooking,

    /// <summary>Beehives.</summary>
    Beehive,

    /// <summary>Sap collectors.</summary>
    SapCollector,

    /// <summary>Fires, braziers and torches.</summary>
    Fire,

    /// <summary>Eggs that hatch.</summary>
    Egg,

    /// <summary>Tame animals that breed.</summary>
    Animal
}

/// <summary>
/// What a station is doing.
/// </summary>
public enum ProductionState
{
    /// <summary>Nothing to do.</summary>
    Idle,

    /// <summary>Working; <see cref="ProductionStatus.Seconds"/> tells how long.</summary>
    Working,

    /// <summary>Something is waiting to be taken, or it is full.</summary>
    Ready,

    /// <summary>Has work but cannot do it; <see cref="ProductionStatus.Reason"/> tells why.</summary>
    Stopped
}

/// <summary>
/// A snapshot of one station, read from its ZDO. Texts hold $ tokens and are localized when shown.
/// </summary>
public class ProductionStatus
{
    /// <summary>The station.</summary>
    public Component Source;

    /// <summary>The kind of station.</summary>
    public ProductionKind Kind;

    /// <summary>The station's name.</summary>
    public string Name;

    /// <summary>What it is doing.</summary>
    public ProductionState State;

    /// <summary>Seconds until the work is all done, or -1.</summary>
    public double Seconds = -1.0;

    /// <summary>Why it has stopped, or null.</summary>
    public string Reason;

    /// <summary>Short text for the floating label and the overview.</summary>
    public string Summary;

    /// <summary>Lines added to the hover text.</summary>
    public readonly List<string> Lines = new();

    /// <summary>True if it needs the player soon: fuel running low, food about to burn.</summary>
    public bool Attention;

    /// <summary>A message to send once when it first appears, like food about to burn, or null.</summary>
    public string Warning;

    /// <summary>
    /// Clears the snapshot for reuse.
    /// </summary>
    /// <param name="source">The station.</param>
    /// <param name="kind">The kind of station.</param>
    /// <param name="name">The station's name.</param>
    public void Reset(Component source, ProductionKind kind, string name)
    {
        Source = source;
        Kind = kind;
        Name = name;
        State = ProductionState.Idle;
        Seconds = -1.0;
        Reason = null;
        Summary = null;
        Lines.Clear();
        Attention = false;
        Warning = null;
    }
}
