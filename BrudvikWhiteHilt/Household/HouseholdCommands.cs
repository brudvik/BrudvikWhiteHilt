using Jotunn.Entities;
using Jotunn.Managers;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Household;

/// <summary>
/// Console command whitehilt_household, phase 0 of the household: with cheats on, puts a householder before the player
/// (<c>spawn [female] [huskarl]</c>) and tells the nearest one to walk or run to where the player looks, follow, stay,
/// go home, join the farm of the High Seat they stand on, sit on the nearest chair, lie in their own or the nearest
/// bed, stand up, show what it is doing or go away. It is how the walking body is tried on one machine, on two and on a
/// dedicated server before the household's work is written.
/// </summary>
public static class HouseholdCommands
{
    // How far from the player the nearest householder may be, and a chair or bed from the householder.
    private const float Reach = 30f;
    private const float PieceReach = 20f;

    /// <summary>
    /// Registers the command. Call from the plugin's Awake.
    /// </summary>
    public static void Register()
    {
        CommandManager.Instance.AddConsoleCommand(new HouseholdCommand());
    }

    private static void Spawn(Player player, bool female, bool huskarl)
    {
        GameObject prefab = PrefabManager.Instance.GetPrefab(HouseholdBody.PrefabName);
        if (prefab == null)
        {
            global::Console.instance.Print("The householder prefab is missing; see the log.");
            return;
        }

        Vector3 position = player.transform.position + player.transform.forward * 3f + Vector3.up * 0.2f;
        Quaternion facing = Quaternion.LookRotation(-player.transform.forward);
        GameObject body = Object.Instantiate(prefab, position, facing);
        HouseholdMember member = body.GetComponent<HouseholdMember>();
        member.InitializeNew(huskarl ? HouseholdRole.Huskarl : HouseholdRole.Farmhand, female, Random.Range(int.MinValue, int.MaxValue), position);
        global::Console.instance.Print($"{member.MemberName} the {(huskarl ? "huskarl" : "farmhand")} stands before you.");
        Join(member);
    }

    // Makes a member one of the household of the High Seat whose farm they stand on, if it has room.
    private static void Join(HouseholdMember member)
    {
        HighSeat seat = HighSeat.FarmAt(member.transform.position);
        if (seat == null)
        {
            global::Console.instance.Print("No High Seat's farm reaches here; they belong to no farm.");
            return;
        }

        if (member.FarmId != seat.Id && seat.Members().Count >= HouseholdSettings.MaxPerFarm.Value)
        {
            global::Console.instance.Print("That High Seat's household is full.");
            return;
        }

        member.JoinFarm(seat);
        global::Console.instance.Print($"{member.MemberName} lives on the farm of the High Seat now.");
    }

    private static Householder Nearest(Player player)
    {
        return Householder.All
            .Where(human => human != null && Vector3.Distance(human.transform.position, player.transform.position) <= Reach)
            .OrderBy(human => Vector3.Distance(human.transform.position, player.transform.position))
            .FirstOrDefault();
    }

    private static T NearestPiece<T>(Vector3 from) where T : Component
    {
        return Object.FindObjectsByType<T>(FindObjectsSortMode.None)
            .Where(piece => Vector3.Distance(piece.transform.position, from) <= PieceReach)
            .OrderBy(piece => Vector3.Distance(piece.transform.position, from))
            .FirstOrDefault();
    }

    // Where the player looks: the first thing the camera's ray meets within 60 m, or 10 m ahead of the player.
    private static Vector3 LookPoint(Player player)
    {
        int mask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain");
        Transform eye = GameCamera.instance != null ? GameCamera.instance.transform : player.m_eye;
        return Physics.Raycast(eye.position, eye.forward, out RaycastHit hit, 60f, mask)
            ? hit.point
            : player.transform.position + player.transform.forward * 10f;
    }

    private static string Describe(Householder human)
    {
        MonsterAI ai = human.GetComponent<MonsterAI>();
        HouseholdProbe probe = human.GetComponent<HouseholdProbe>();
        Rigidbody body = human.GetComponent<Rigidbody>();
        Animator animator = human.GetComponentInChildren<Animator>();
        return $"{human.GetHoverName()}: owner {human.View.IsOwner()}, health {human.GetHealth():0}/{human.GetMaxHealth():0}, " +
               $"goal {probe?.Status ?? "none"}, attached {human.IsAttached()}, in bed {human.InBed()}, " +
               $"follows {(ai.GetFollowTarget() != null ? ai.GetFollowTarget().name : "nobody")}, " +
               $"target {(ai.m_targetCreature != null ? ai.m_targetCreature.m_name : "none")}, " +
               $"speed {body.linearVelocity.magnitude:0.0} m/s, animator speed {animator.GetFloat("forward_speed"):0.0}";
    }

    private sealed class HouseholdCommand : ConsoleCommand
    {
        public override string Name => "whitehilt_household";

        public override string Help => "[spawn [female] [huskarl]|go [run]|follow|stay|home|join|sit|sleep|stand|status|remove] " +
                                       "Phase 0 of the household, with cheats on: puts a householder before you and " +
                                       "tells the nearest one to walk or run to where you look, follow you, stay, go home, join " +
                                       "the farm of the High Seat here, sit on the nearest chair, lie in its own or the nearest " +
                                       "bed, stand up, show its state or go.";

        public override void Run(string[] args)
        {
            global::Console console = global::Console.instance;
            Player player = Player.m_localPlayer;
            if (console == null || player == null)
            {
                return;
            }

            if (!console.IsCheatsEnabled())
            {
                console.Print("The household test needs cheats: run devcommands first.");
                return;
            }

            string verb = args.Length > 0 ? args[0] : "status";
            if (verb == "spawn")
            {
                Spawn(player, args.Contains("female"), args.Contains("huskarl"));
                return;
            }

            Householder human = Nearest(player);
            if (human == null)
            {
                console.Print($"No householder within {Reach:0} m. Use spawn first.");
                return;
            }

            // Only the owner moves a body; the test takes it over, so the command works on any machine.
            if (!human.View.IsOwner())
            {
                human.View.ClaimOwnership();
            }

            HouseholdProbe probe = human.GetComponent<HouseholdProbe>();
            switch (verb)
            {
                case "go":
                    probe.WalkTo(LookPoint(player), args.Contains("run"));
                    break;
                case "follow":
                    probe.Follow(player);
                    break;
                case "stay":
                    probe.Follow(null);
                    break;
                case "home":
                    human.Member.GoHome();
                    break;
                case "join":
                    Join(human.Member);
                    break;
                case "sit":
                    Chair chair = NearestPiece<Chair>(human.transform.position);
                    if (chair == null)
                    {
                        console.Print($"No chair within {PieceReach:0} m of the householder.");
                        return;
                    }

                    probe.SitOn(chair);
                    break;
                case "sleep":
                    // Their own bed if they have one, else the nearest.
                    Bed bed = human.Member.OwnBed() ?? NearestPiece<Bed>(human.transform.position);
                    if (bed == null)
                    {
                        console.Print($"No bed within {PieceReach:0} m of the householder.");
                        return;
                    }

                    probe.SleepIn(bed);
                    break;
                case "stand":
                    probe.Stop();
                    break;
                case "remove":
                    ZNetScene.instance.Destroy(human.gameObject);
                    console.Print("The householder is gone.");
                    return;
                case "status":
                    break;
                default:
                    console.Print(Help);
                    return;
            }

            console.Print(Describe(human));
        }
    }
}
