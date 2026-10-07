using BepInEx.Configuration;
using BrudvikWhiteHilt.Progression;

namespace BrudvikWhiteHilt.Items.Weapons.Styles;

/// <summary>
/// How White Hilt melee weapons swing, chosen by the server for everyone.
/// </summary>
public enum AttackVarietyMode
{
    /// <summary>Each combo draws a style: the weapon's signature with <see cref="AttackVarietySettings.SignatureShare"/>, else another that suits its grip.</summary>
    Varied,

    /// <summary>Every combo uses the weapon's signature style.</summary>
    Signature,

    /// <summary>The attacks of the vanilla weapon the White Hilt weapon is made from.</summary>
    Vanilla
}

/// <summary>
/// A player's own choice of <see cref="AttackVarietyMode"/>, or the server's.
/// </summary>
public enum AttackVarietyChoice
{
    /// <summary>Follow the server's <see cref="AttackVarietySettings.Mode"/>.</summary>
    Server,

    /// <summary>As <see cref="AttackVarietyMode.Varied"/>.</summary>
    Varied,

    /// <summary>As <see cref="AttackVarietyMode.Signature"/>.</summary>
    Signature,

    /// <summary>As <see cref="AttackVarietyMode.Vanilla"/>.</summary>
    Vanilla
}

/// <summary>
/// Config of the White Hilt weapons' attack styles, section "Gear.AttackStyles". The server decides the default and
/// the balance; each player may choose differently for their own swings, which every other player sees as they are.
/// </summary>
public static class AttackVarietySettings
{
    private const string Section = "Gear.AttackStyles";

    /// <summary>How the weapons swing for players who follow the server.</summary>
    public static ConfigEntry<AttackVarietyMode> Mode { get; private set; }

    /// <summary>Chance that a combo uses the weapon's signature style when styles vary.</summary>
    public static ConfigEntry<float> SignatureShare { get; private set; }

    /// <summary>How far damage and stamina per hit are evened out for faster and slower styles.</summary>
    public static ConfigEntry<float> EvenOutTempo { get; private set; }

    /// <summary>The player's own choice.</summary>
    public static ConfigEntry<AttackVarietyChoice> MyChoice { get; private set; }

    /// <summary>
    /// The mode the local player's swings follow: their own choice, or the server's.
    /// </summary>
    public static AttackVarietyMode Effective => MyChoice.Value switch
    {
        AttackVarietyChoice.Varied => AttackVarietyMode.Varied,
        AttackVarietyChoice.Signature => AttackVarietyMode.Signature,
        AttackVarietyChoice.Vanilla => AttackVarietyMode.Vanilla,
        _ => Mode.Value
    };

    /// <summary>
    /// Binds the config entries. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        Mode = WhiteHiltConfig.BindAdminOnly(Section, "Mode", AttackVarietyMode.Varied,
            "How White Hilt melee weapons swing for players who follow the server. Varied: each combo draws one of the " +
            "styles that suit the weapon. Signature: always the weapon's own style. Vanilla: as the vanilla weapon it is made from.");
        SignatureShare = WhiteHiltConfig.BindAdminOnly(Section, "SignatureShare", 0.5f,
            "Chance that a combo uses the weapon's own style when styles vary (0.5 = every other combo); the rest is shared " +
            "equally by its other styles.", new AcceptableValueRange<float>(0f, 1f));
        EvenOutTempo = WhiteHiltConfig.BindAdminOnly(Section, "EvenOutTempo", 1f,
            "How far damage and stamina per hit follow a style's tempo, so the weapon deals as much damage and costs as " +
            "much stamina per second in every style (1 = fully, 0 = the same per hit).", new AcceptableValueRange<float>(0f, 1f));
        MyChoice = WhiteHiltConfig.BindLocal(Section, "MyChoice", AttackVarietyChoice.Server,
            "How your own White Hilt weapons swing: as the server says, or Varied, Signature or Vanilla for you alone.");
    }
}
