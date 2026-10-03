using System.Linq;

namespace BrudvikWhiteHilt.Items.Meads.BogBeanBitter;

/// <summary>
/// Ends poison, fire, frost, lightning, tar and smoke when drunk, and keeps them off while it lasts.
/// </summary>
public class CleansingEffect : SE_Stats
{
    private static readonly int[] cleansed =
    {
        SEMan.s_statusEffectPoison, SEMan.s_statusEffectBurning, SEMan.s_statusEffectFrost,
        SEMan.s_statusEffectLightning, SEMan.s_statusEffectTared, SEMan.s_statusEffectSmoked
    };

    /// <inheritdoc/>
    public override void Setup(Character character)
    {
        base.Setup(character);
        Cleanse(m_character);
    }

    /// <inheritdoc/>
    public override void UpdateStatusEffect(float dt)
    {
        base.UpdateStatusEffect(dt);
        Cleanse(m_character);
    }

    /// <summary>
    /// Ends poison, fire, frost, lightning, tar and smoke on a character.
    /// </summary>
    /// <param name="character">The character, or null for nothing.</param>
    public static void Cleanse(Character character)
    {
        SEMan seMan = character?.GetSEMan();
        if (seMan == null)
        {
            return;
        }

        foreach (int hash in cleansed.Where(seMan.HaveStatusEffect))
        {
            seMan.RemoveStatusEffect(hash, quiet: true);
        }
    }
}
