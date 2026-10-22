using BrudvikWhiteHilt.Helpers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Household;

/// <summary>
/// Who a member of the household is and what they are told: their name, role and look, the gear they wear (saved in
/// their network data, since the game saves no inventory but a player's), and following a player, waiting or going
/// home. Pressing the use key on them makes them follow or wait, with the alternative key they go home, and an item
/// used on them is handed over and put on. The state lives in the network data; the owner acts on it.
/// </summary>
public class HouseholdMember : MonoBehaviour
{
    // How far a followed player may be for a member loaded again to take up following.
    private const float FollowRestoreRange = 60f;

    // How long a line stays over the head, and how high.
    private const float LineSeconds = 4f;
    private const float LineHeight = 2.1f;

    // How near a bed must stand to where the member's bed was given them to be theirs.
    private const float BedMatch = 0.3f;

    private Householder human;
    private MonsterAI ai;
    private HouseholdProbe walker;
    private bool loading;

    /// <summary>The member's name, or the general word for a householder before they have one.</summary>
    public string MemberName
    {
        get
        {
            string name = Zdo?.GetString(HouseholdKeys.Name) ?? string.Empty;
            return string.IsNullOrEmpty(name) ? Localization.instance.Localize(Translations.Token(HouseholdBody.NameKey)) : name;
        }
    }

    /// <summary>The member's role.</summary>
    public HouseholdRole Role => (HouseholdRole)(Zdo?.GetInt(HouseholdKeys.Role) ?? 0);

    /// <summary>Whether the member is a woman.</summary>
    public bool Female => Zdo?.GetBool(HouseholdKeys.Female) ?? false;

    /// <summary>Where the member lives.</summary>
    public Vector3 Home => Zdo?.GetVec3(HouseholdKeys.Home, transform.position) ?? transform.position;

    /// <summary>The player the member follows, 0 for nobody.</summary>
    public long FollowsPlayerId => Zdo?.GetLong(HouseholdKeys.Follows) ?? 0L;

    /// <summary>The number of the High Seat whose farm the member belongs to, 0 for none.</summary>
    public long FarmId => Zdo?.GetLong(HouseholdKeys.Farm) ?? 0L;

    /// <summary>Whether the member has a bed of their own.</summary>
    public bool HasBed => Zdo != null && Zdo.GetVec3(HouseholdKeys.BedAt, Vector3.zero) != Vector3.zero;

    /// <summary>
    /// The member's own bed, if it is loaded and still stands where it stood.
    /// </summary>
    /// <returns>The bed, or null.</returns>
    public Bed OwnBed()
    {
        return HasBed ? FindObjectsByType<Bed>(FindObjectsSortMode.None).FirstOrDefault(Sleeps) : null;
    }

    /// <summary>
    /// Whether this is the member's own bed: one standing where theirs was given them.
    /// </summary>
    /// <param name="bed">The bed.</param>
    /// <returns>True when it is.</returns>
    public bool Sleeps(Bed bed)
    {
        return bed != null && HasBed && Vector3.Distance(bed.transform.position, Zdo.GetVec3(HouseholdKeys.BedAt, Vector3.zero)) < BedMatch;
    }

    /// <summary>The gear a player handed over and can take back.</summary>
    public int GivenCount => human.GetInventory().GetAllItems().Count(Given);

    private ZDO Zdo => human != null && human.View != null && human.View.IsValid() ? human.View.GetZDO() : null;

    /// <summary>
    /// Registers the texts. Call from the plugin's Awake.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglish("whitehilt_hh_farmhand", "Farmhand");
        Translations.AddEnglish("whitehilt_hh_huskarl", "Huskarl");
        Translations.AddEnglish("whitehilt_hh_following", "Follows you");
        Translations.AddEnglish("whitehilt_hh_following_other", "Follows someone else");
        Translations.AddEnglish("whitehilt_hh_waiting", "Waits here");
        Translations.AddEnglish("whitehilt_hh_follow", "Follow me");
        Translations.AddEnglish("whitehilt_hh_wait", "Wait here");
        Translations.AddEnglish("whitehilt_hh_go_home", "Go home");
        Translations.AddEnglish("whitehilt_hh_give", "Use an item to hand it over");
        Translations.AddEnglish("whitehilt_hh_takes", "$1 puts on the $2.");
        Translations.AddEnglish("whitehilt_hh_no_use", "$1 has no use for that.");
        Translations.AddEnglish("whitehilt_hh_say_follow", "Lead on.");
        Translations.AddEnglish("whitehilt_hh_say_wait", "I'll wait here.");
        Translations.AddEnglish("whitehilt_hh_say_home", "Back to the farm, then.");
        Translations.AddEnglish("whitehilt_hh_say_thanks", "My thanks. I'll put it to good use.");
        Translations.AddEnglish("whitehilt_hh_say_bed", "A bed of my own. That's more than I had.");
        Translations.AddEnglish("whitehilt_hh_say_return", "Here you are, as it was lent.");
    }

    /// <summary>
    /// Makes a new member: a name and a look from the seed, their role, their home, and the clothes or gear they come
    /// with. Run by the owner, right after the body is put in the world.
    /// </summary>
    /// <param name="role">Farmhand or huskarl.</param>
    /// <param name="female">Whether a woman.</param>
    /// <param name="seed">The seed of the name and the look.</param>
    /// <param name="home">Where they live.</param>
    public void InitializeNew(HouseholdRole role, bool female, int seed, Vector3 home)
    {
        ZDO zdo = Zdo;
        if (zdo == null || !human.View.IsOwner())
        {
            return;
        }

        string[] taken = Householder.All.Select(other => other.GetComponent<HouseholdMember>())
            .Where(member => member != null && member != this).Select(member => member.MemberName).ToArray();
        zdo.Set(HouseholdKeys.Name, HouseholdNames.Pick(seed, female, taken));
        zdo.Set(HouseholdKeys.Female, female);
        zdo.Set(HouseholdKeys.Role, (int)role);
        zdo.Set(HouseholdKeys.Home, home);
        HouseholdLooks.WriteBody(zdo, seed, female);
        string[] gear = role == HouseholdRole.Huskarl ? HouseholdLooks.HuskarlGear : HouseholdLooks.FarmhandClothes(seed);
        foreach (string name in gear)
        {
            ItemDrop.ItemData item = human.GetInventory().AddItem(name, 1, 1, 0, 0L, string.Empty, false);
            if (item != null)
            {
                human.EquipItem(item, false);
            }
        }

        SaveGear();
        ai.SetPatrolPoint(home);
    }

    /// <summary>
    /// The hover text: name, role, what they are doing and the keys.
    /// </summary>
    /// <returns>The localized text.</returns>
    public string HoverText()
    {
        long me = Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerID() : 0L;
        string state = FollowsPlayerId == 0L ? "whitehilt_hh_waiting"
            : FollowsPlayerId == me ? "whitehilt_hh_following" : "whitehilt_hh_following_other";
        string role = Role == HouseholdRole.Huskarl ? "whitehilt_hh_huskarl" : "whitehilt_hh_farmhand";
        string text = $"{MemberName}\n<color=#a0a0a0>{Translations.Token(role)} · {Translations.Token(state)}</color>"
                      + $"\n[<color=yellow><b>$KEY_Use</b></color>] {Translations.Token(FollowsPlayerId == me ? "whitehilt_hh_wait" : "whitehilt_hh_follow")}"
                      + $"\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] {Translations.Token("whitehilt_hh_go_home")}"
                      + $"\n<color=#a0a0a0>{Translations.Token("whitehilt_hh_give")}</color>";
        return Localization.instance.Localize(text);
    }

    /// <summary>
    /// The use key: follow the player, or wait if following them already; with the alternative key, go home.
    /// </summary>
    /// <param name="user">Who pressed it.</param>
    /// <param name="hold">Whether the key is held.</param>
    /// <param name="alt">Whether the alternative key is down.</param>
    /// <returns>True when it was handled.</returns>
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold || user is not Player player || !TakeCharge())
        {
            return false;
        }

        if (alt)
        {
            GoHome();
        }
        else if (FollowsPlayerId == player.GetPlayerID())
        {
            Wait();
        }
        else
        {
            Follow(player);
        }

        return true;
    }

    /// <summary>
    /// An item used on the member: weapons, shields, armour and clothes are handed over and put on, and what they
    /// wore in that place goes back to the player.
    /// </summary>
    /// <param name="user">Who used it.</param>
    /// <param name="item">The item.</param>
    /// <returns>True when it was handled.</returns>
    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        if (user is not Player player || item == null)
        {
            return false;
        }

        if (!Wearable(item))
        {
            player.Message(MessageHud.MessageType.Center, Localization.instance.Localize(Translations.Token("whitehilt_hh_no_use"), MemberName));
            return true;
        }

        if (!TakeCharge())
        {
            return false;
        }

        if (player.IsItemEquiped(item))
        {
            player.UnequipItem(item);
        }

        ItemDrop.ItemData given = item.Clone();
        given.m_customData[HouseholdKeys.Given] = "1";
        player.GetInventory().RemoveItem(item);
        Inventory gear = human.GetInventory();
        gear.AddItem(given);
        human.EquipItem(given, false);
        // What they wore in that place is unequipped by the game but stays in their bag. What a player lent them goes
        // back to the player; their own clothes and gear they keep, to put on again when the lent things are taken back.
        foreach (ItemDrop.ItemData old in gear.GetAllItems().Where(other => other != given && !human.IsItemEquiped(other) && Given(other)).ToList())
        {
            gear.RemoveItem(old);
            GiveTo(player, old);
        }

        SaveGear();
        player.Message(MessageHud.MessageType.Center, Localization.instance.Localize(Translations.Token("whitehilt_hh_takes"), MemberName,
            Localization.instance.Localize(given.m_shared.m_name)));
        Say("whitehilt_hh_say_thanks");
        return true;
    }

    /// <summary>
    /// Gives back to a player everything players handed over, and puts the member's own clothes and gear on again.
    /// </summary>
    /// <param name="player">Who takes it.</param>
    /// <returns>How many things were given back.</returns>
    public int TakeBackGear(Player player)
    {
        if (player == null || !TakeCharge())
        {
            return 0;
        }

        Inventory gear = human.GetInventory();
        List<ItemDrop.ItemData> lent = gear.GetAllItems().Where(Given).ToList();
        foreach (ItemDrop.ItemData item in lent)
        {
            human.UnequipItem(item, false);
            gear.RemoveItem(item);
            GiveTo(player, item);
        }

        foreach (ItemDrop.ItemData own in gear.GetAllItems().Where(item => !human.IsItemEquiped(item)).ToList())
        {
            human.EquipItem(own, false);
        }

        SaveGear();
        if (lent.Count > 0)
        {
            Say("whitehilt_hh_say_return");
        }

        return lent.Count;
    }

    /// <summary>
    /// Makes the member one of a High Seat's household: the farm is theirs, and home is before the seat.
    /// </summary>
    /// <param name="seat">The High Seat.</param>
    public void JoinFarm(HighSeat seat)
    {
        if (seat == null || !seat.IsBuilt || !TakeCharge())
        {
            return;
        }

        Zdo.Set(HouseholdKeys.Farm, seat.Id);
        Zdo.Set(HouseholdKeys.Home, seat.HomePoint);
        if (FollowsPlayerId == 0L)
        {
            ai.SetPatrolPoint(seat.HomePoint);
        }
    }

    /// <summary>
    /// Gives the member a bed of their own, or takes it away with null.
    /// </summary>
    /// <param name="bed">The bed, or null.</param>
    public void SetBed(Bed bed)
    {
        if (!TakeCharge())
        {
            return;
        }

        if (bed != null)
        {
            Zdo.Set(HouseholdKeys.BedAt, bed.transform.position);
        }
        else
        {
            Zdo.RemoveVec3(HouseholdKeys.BedAt);
        }

        if (bed != null)
        {
            Say("whitehilt_hh_say_bed");
        }
    }

    /// <summary>
    /// Follows a player.
    /// </summary>
    /// <param name="player">The player.</param>
    public void Follow(Player player)
    {
        walker.Stop();
        human.View.GetZDO().Set(HouseholdKeys.Follows, player.GetPlayerID());
        ai.ResetPatrolPoint();
        ai.SetFollowTarget(player.gameObject);
        Say("whitehilt_hh_say_follow");
    }

    /// <summary>
    /// Stops following and keeps to where they stand.
    /// </summary>
    public void Wait()
    {
        walker.Stop();
        human.View.GetZDO().Set(HouseholdKeys.Follows, 0L);
        ai.SetFollowTarget(null);
        ai.SetPatrolPoint();
        Say("whitehilt_hh_say_wait");
    }

    /// <summary>
    /// Stops following, walks home and keeps to it.
    /// </summary>
    public void GoHome()
    {
        // Home is before the High Seat, wherever it stands now; without a farm, where they were first set down.
        HighSeat seat = HighSeat.Find(FarmId);
        if (seat != null)
        {
            human.View.GetZDO().Set(HouseholdKeys.Home, seat.HomePoint);
        }

        human.View.GetZDO().Set(HouseholdKeys.Follows, 0L);
        ai.SetFollowTarget(null);
        ai.SetPatrolPoint(Home);
        walker.WalkTo(Home, false);
        Say("whitehilt_hh_say_home");
    }

    /// <summary>
    /// Takes up following again after being loaded, once the followed player is near. Called by the owner from the
    /// creature's AI update.
    /// </summary>
    public void UpdateAI()
    {
        long followed = FollowsPlayerId;
        if (followed == 0L || ai.GetFollowTarget() != null)
        {
            return;
        }

        Player player = Player.GetAllPlayers().FirstOrDefault(other => other.GetPlayerID() == followed);
        if (player != null && Vector3.Distance(player.transform.position, transform.position) <= FollowRestoreRange)
        {
            ai.SetFollowTarget(player.gameObject);
        }
    }

    /// <summary>
    /// Says a line over the head, seen on this machine.
    /// </summary>
    /// <param name="key">The line's translation key.</param>
    public void Say(string key)
    {
        if (Chat.instance != null)
        {
            Chat.instance.SetNpcText(gameObject, Vector3.up * LineHeight, 20f, LineSeconds, string.Empty, Translations.Token(key), false);
        }
    }

    private void Awake()
    {
        human = GetComponent<Householder>();
        ai = GetComponent<MonsterAI>();
        walker = GetComponent<HouseholdProbe>();
    }

    // After every Awake, so the body's network view is set up: puts the saved gear back on, and saves it from then on
    // whenever the bag changes.
    private void Start()
    {
        LoadGear();
        human.GetInventory().m_onChanged += SaveGear;
    }

    // Only the owner moves a member and changes their gear. Whoever talks to them takes charge, as a player who uses a
    // cart or a ship takes it over, and reads their gear from the network data, which the last owner kept up to date.
    private bool TakeCharge()
    {
        if (Zdo == null)
        {
            return false;
        }

        if (!human.View.IsOwner())
        {
            human.View.ClaimOwnership();
            LoadGear();
        }

        return true;
    }

    // Reads the gear from the network data and puts on what was worn. The bag is filled again from scratch, so it
    // never holds an older copy of the gear.
    private void LoadGear()
    {
        byte[] data = Zdo?.GetByteArray(HouseholdKeys.Gear);
        if (data == null || data.Length == 0)
        {
            return;
        }

        loading = true;
        try
        {
            human.UnequipAllItems();
            Inventory gear = human.GetInventory();
            gear.Load(new ZPackage(data));
            foreach (ItemDrop.ItemData item in gear.GetAllItems().Where(item => item.m_equipped).ToList())
            {
                item.m_equipped = false;
                human.EquipItem(item, false);
            }
        }
        finally
        {
            loading = false;
        }
    }

    // Writes the gear into the network data, where the next owner and the next load find it. Only the owner writes.
    private void SaveGear()
    {
        if (loading || Zdo == null || !human.View.IsOwner())
        {
            return;
        }

        ZPackage package = new();
        human.GetInventory().Save(package);
        Zdo.Set(HouseholdKeys.Gear, package.GetArray());
    }

    // Whether a player handed the item over, so it can be taken back.
    private static bool Given(ItemDrop.ItemData item)
    {
        return item.m_customData.ContainsKey(HouseholdKeys.Given);
    }

    // Into the player's bag, unmarked, or on the ground at their feet when it is full.
    private static void GiveTo(Player player, ItemDrop.ItemData item)
    {
        item.m_customData.Remove(HouseholdKeys.Given);
        if (!player.GetInventory().AddItem(item))
        {
            ItemDrop.DropItem(item, item.m_stack, player.transform.position + Vector3.up, Quaternion.identity);
        }
    }

    // What a member can put on. Bows are left out: the creature AI shoots only with arrows in its bag.
    private static bool Wearable(ItemDrop.ItemData item)
    {
        return item.m_shared.m_itemType is ItemDrop.ItemData.ItemType.OneHandedWeapon
            or ItemDrop.ItemData.ItemType.TwoHandedWeapon or ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft
            or ItemDrop.ItemData.ItemType.Shield
            or ItemDrop.ItemData.ItemType.Helmet or ItemDrop.ItemData.ItemType.Chest
            or ItemDrop.ItemData.ItemType.Legs or ItemDrop.ItemData.ItemType.Shoulder;
    }
}
