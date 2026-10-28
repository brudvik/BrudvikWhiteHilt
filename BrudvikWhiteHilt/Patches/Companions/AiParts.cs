using BrudvikWhiteHilt.Companions;
using BrudvikWhiteHilt.Household;
using System.Runtime.CompilerServices;

namespace BrudvikWhiteHilt.Patches.Companions;

/// <summary>
/// The mod's parts on a creature that its <c>MonsterAI.UpdateAI</c> hooks drive: a dog's or a householder's. They are
/// looked up once per creature and remembered, as the hooks run every frame for every creature once one dog or
/// householder is loaded, and the parts are on the creature from its prefab, so they never change.
/// </summary>
public sealed class AiParts
{
    // Creatures with none of the parts share this one, so a lookup never has to be repeated.
    private static readonly AiParts none = new();
    private static readonly ConditionalWeakTable<MonsterAI, AiParts> byAi = new();
    private static readonly ConditionalWeakTable<MonsterAI, AiParts>.CreateValueCallback find = Find;

    /// <summary>The dog, or null.</summary>
    public DogCompanion Dog { get; private set; }

    /// <summary>The dog's activities, or null.</summary>
    public DogActivities Activities { get; private set; }

    /// <summary>The dog's care, or null.</summary>
    public DogCare Care { get; private set; }

    /// <summary>The dog's rest pose, or null.</summary>
    public RestPose Rest { get; private set; }

    /// <summary>The householder's member part, or null.</summary>
    public HouseholdMember Member { get; private set; }

    /// <summary>The householder's goal steering, or null.</summary>
    public HouseholdProbe Probe { get; private set; }

    /// <summary>
    /// The parts of a creature.
    /// </summary>
    /// <param name="ai">The creature's AI.</param>
    /// <returns>Its parts; all null for a creature without any.</returns>
    public static AiParts Of(MonsterAI ai)
    {
        return byAi.GetValue(ai, find);
    }

    private static AiParts Find(MonsterAI ai)
    {
        AiParts parts = new()
        {
            Dog = ai.GetComponent<DogCompanion>(),
            Activities = ai.GetComponent<DogActivities>(),
            Care = ai.GetComponent<DogCare>(),
            Rest = ai.GetComponent<RestPose>(),
            Member = ai.GetComponent<HouseholdMember>(),
            Probe = ai.GetComponent<HouseholdProbe>(),
        };

        return parts.Dog == null && parts.Activities == null && parts.Care == null && parts.Rest == null && parts.Member == null
            && parts.Probe == null
                ? none
                : parts;
    }
}
