using BrudvikWhiteHilt.Helpers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Mastery;

/// <summary>
/// Woodcutting and Pickaxes milestones on the machine that owns the tree, log or rock: it watches what a hit drops,
/// then adds extra wood, ore and finds.
/// </summary>
public static class Gathering
{
    /// <summary>ZDO key on a log from an old growth tree.</summary>
    public const string OldGrowthKey = "whitehilt_oldgrowth";

    /// <summary>ZDO key on a rock: 1 normal, 2 rich, 0 not yet decided.</summary>
    public const string RichKey = "whitehilt_rich";

    /// <summary>Marks a hit from a falling tree, so it does not knock down more trees in turn.</summary>
    public const short DominoMarker = -77;

    private const float ReplantDistance = 3f;
    private const string ReplantRpc = "WhiteHilt_Replant";

    private static readonly (string Prefab, int Min, int Max, float Weight)[] miningFinds =
    {
        ("Amber", 1, 1, 40f), ("Flint", 2, 3, 30f), ("Coins", 5, 15, 20f), ("AmberPearl", 1, 1, 10f)
    };

    private static readonly (string Prefab, int Min, int Max, float Weight)[] nestFinds =
    {
        ("Feathers", 1, 3, 60f), ("Resin", 1, 2, 30f), ("Honey", 1, 1, 10f)
    };

    private static Dictionary<string, string> saplingByTree;

    /// <summary>The current context, while a hit on something that drops is handled.</summary>
    public static Context Current { get; private set; }

    /// <summary>
    /// Registers the English text. Call from the plugin's Awake.
    /// </summary>
    public static void RegisterTranslations()
    {
        Translations.AddEnglish("msg_whitehilt_oldgrowth", "Old growth!");
        Translations.AddEnglish("msg_whitehilt_richvein", "A rich vein!");
        Translations.AddEnglish("msg_whitehilt_nest", "A bird's nest!");
        Translations.AddEnglish("msg_whitehilt_replanted", "Replanted");
    }

    /// <summary>
    /// Starts watching the drops of a hit.
    /// </summary>
    /// <param name="hit">The hit.</param>
    /// <param name="skill">The gathering skill: WoodCutting or Pickaxes.</param>
    /// <returns>The context, or null when the attacker is not a player.</returns>
    public static Context Begin(HitData hit, Skills.SkillType skill)
    {
        Player attacker = hit?.GetAttacker() as Player;
        if (attacker == null)
        {
            return null;
        }

        Current = new Context(attacker, skill, SkillLevels.Get(attacker, skill), hit);
        return Current;
    }

    /// <summary>
    /// Stops watching.
    /// </summary>
    public static void End()
    {
        Current = null;
    }

    /// <summary>
    /// Forgets which sapling grows into which tree, e.g. when a new ZNetScene starts.
    /// </summary>
    public static void ClearCache()
    {
        saplingByTree = null;
    }

    /// <summary>
    /// Counts an item dropped while a context is active.
    /// </summary>
    /// <param name="item">The new item drop.</param>
    public static void Observe(ItemDrop item)
    {
        Current?.Drops.Add(item);
    }

    /// <summary>
    /// Adds what a broken log or stump gives on top: clean splits and old growth.
    /// </summary>
    /// <param name="context">The finished context.</param>
    /// <param name="oldGrowth">Whether the wood is old growth.</param>
    public static void FinishWood(Context context, bool oldGrowth)
    {
        foreach (ItemDrop drop in context.Drops.Where(drop => drop != null).ToList())
        {
            int extra = (Random.value < Perks.CleanSplit(context.Level) ? 1 : 0) + (oldGrowth ? drop.m_itemData.m_stack : 0);
            Spawn(drop.gameObject, extra, drop.transform.position);
        }
    }

    /// <summary>
    /// Adds what broken rock gives on top: extra ore, rich veins and finds.
    /// </summary>
    /// <param name="context">The finished context.</param>
    /// <param name="rich">Whether the rock is a rich vein.</param>
    /// <param name="position">Where the rock broke.</param>
    public static void FinishRock(Context context, bool rich, Vector3 position)
    {
        if (context.Drops.Count == 0)
        {
            return;
        }

        foreach (ItemDrop drop in context.Drops.Where(drop => drop != null && OreEcho.IsOre(Utils.GetPrefabName(drop.gameObject))).ToList())
        {
            int extra = (Random.value < Perks.ExtraOre(context.Level) ? 1 : 0) + (rich ? drop.m_itemData.m_stack : 0);
            Spawn(drop.gameObject, extra, drop.transform.position);
        }

        if (Random.value < Perks.MiningFind(context.Level))
        {
            SpawnFind(miningFinds, position);
        }
    }

    /// <summary>
    /// Decides once per rock whether it is a rich vein, when a player with the milestone first breaks some of it.
    /// </summary>
    /// <param name="nview">The rock's network view, owned here.</param>
    /// <param name="context">The context.</param>
    /// <returns>True if the rock is rich.</returns>
    public static bool IsRich(ZNetView nview, Context context)
    {
        ZDO zdo = nview != null && nview.IsValid() ? nview.GetZDO() : null;
        if (zdo == null)
        {
            return false;
        }

        int state = zdo.GetInt(RichKey);
        if (state == 0 && Perks.RichVeins.ReachedAt(context.Level))
        {
            state = Random.value < MasterySettings.RichVeinChance.Value ? 2 : 1;
            zdo.Set(RichKey, state);
            if (state == 2)
            {
                context.Attacker.Message(MessageHud.MessageType.Center, "$msg_whitehilt_richvein");
            }
        }

        return state == 2;
    }

    /// <summary>
    /// Rolls the tree milestones when a tree is about to fall: old growth and the fall direction.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="hit">The hit.</param>
    public static void PrepareTree(Context context, HitData hit)
    {
        context.OldGrowth = Perks.OldGrowth.ReachedAt(context.Level) && Random.value < MasterySettings.OldGrowthChance.Value;
        context.Domino = Perks.DominoFelling.ReachedAt(context.Level) && hit.m_itemLevel != DominoMarker;
        Vector3 forward = context.Attacker.transform.forward;
        forward.y = 0f;
        context.FallDirection = Perks.AimedFall.ReachedAt(context.Level) && forward.sqrMagnitude > 0.01f ? forward.normalized : hit.m_dir;
    }

    /// <summary>
    /// What a felled tree gives on top: a bird's nest, the old growth message and replanting.
    /// </summary>
    /// <param name="context">The finished context.</param>
    /// <param name="tree">The felled tree.</param>
    public static void FinishTree(Context context, TreeBase tree)
    {
        Vector3 position = tree.transform.position;
        if (context.OldGrowth)
        {
            context.Attacker.Message(MessageHud.MessageType.Center, "$msg_whitehilt_oldgrowth");
        }

        if (Random.value < Perks.BirdNest(context.Level))
        {
            SpawnFind(nestFinds, position + Vector3.up);
            context.Attacker.Message(MessageHud.MessageType.TopLeft, "$msg_whitehilt_nest");
        }

        if (Perks.Replanting.ReachedAt(context.Level) && SaplingFor(Utils.GetPrefabName(tree.gameObject)) is string sapling)
        {
            Vector3 away = -context.FallDirection;
            away.y = 0f;
            Vector3 spot = position + (away.sqrMagnitude > 0.01f ? away.normalized : Vector3.forward) * ReplantDistance;
            context.Attacker.m_nview.InvokeRPC(ReplantRpc, spot, sapling);
        }
    }

    /// <summary>
    /// Lets the player plant the sapling the machine that owned a felled tree asks for.
    /// </summary>
    /// <param name="player">A player that just woke up.</param>
    public static void RegisterRpc(Player player)
    {
        if (player.m_nview != null)
        {
            player.m_nview.Register<Vector3, string>(ReplantRpc, (_, spot, sapling) => Replant(player, spot, sapling));
        }
    }

    // Plants a sapling of the felled tree where it stood, paid with seeds from the player's inventory.
    private static void Replant(Player player, Vector3 spot, string saplingName)
    {
        GameObject prefab = ZNetScene.instance?.GetPrefab(saplingName);
        Piece piece = prefab != null ? prefab.GetComponent<Piece>() : null;
        Piece.Requirement seed = piece?.m_resources.FirstOrDefault(requirement => requirement.m_resItem != null);
        if (player != Player.m_localPlayer || seed == null)
        {
            return;
        }

        string seedName = seed.m_resItem.m_itemData.m_shared.m_name;
        int amount = Mathf.Max(1, seed.m_amount);
        if (player.GetInventory().CountItems(seedName) < amount)
        {
            return;
        }

        spot.y = ZoneSystem.instance.GetGroundHeight(spot);
        GameObject planted = Object.Instantiate(prefab, spot, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        planted.GetComponent<Piece>()?.SetCreator(player.GetPlayerID(), Splatform.PlatformManager.DistributionPlatform.LocalUser.PlatformUserID);
        player.GetInventory().RemoveItem(seedName, amount);
        player.Message(MessageHud.MessageType.TopLeft, $"$msg_whitehilt_replanted: {seed.m_resItem.m_itemData.m_shared.m_name}", 0, seed.m_resItem.m_itemData.GetIcon());
    }

    // The sapling that grows into a tree, found once from the game's plant pieces.
    private static string SaplingFor(string treePrefab)
    {
        if (saplingByTree == null && ZNetScene.instance != null)
        {
            saplingByTree = new Dictionary<string, string>();
            foreach (GameObject prefab in ZNetScene.instance.m_prefabs.Where(prefab => prefab != null))
            {
                Plant plant = prefab.GetComponent<Plant>();
                if (plant == null || prefab.GetComponent<Piece>() == null)
                {
                    continue;
                }

                foreach (GameObject grown in plant.m_grownPrefabs.Where(grown => grown != null && grown.GetComponent<TreeBase>() != null))
                {
                    if (!saplingByTree.ContainsKey(grown.name))
                    {
                        saplingByTree[grown.name] = prefab.name;
                    }
                }
            }
        }

        return saplingByTree != null && saplingByTree.TryGetValue(treePrefab, out string sapling) ? sapling : null;
    }

    private static void Spawn(GameObject drop, int count, Vector3 position)
    {
        GameObject prefab = count > 0 ? ZNetScene.instance?.GetPrefab(Utils.GetPrefabName(drop)) : null;
        for (int i = 0; prefab != null && i < count; i++)
        {
            Vector3 offset = Random.insideUnitSphere * 0.3f + Vector3.up * (0.3f * i);
            ItemDrop.OnCreateNew(Object.Instantiate(prefab, position + offset, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f)));
        }
    }

    // Drops something found, by weighted chance.
    private static void SpawnFind((string Prefab, int Min, int Max, float Weight)[] table, Vector3 position)
    {
        float roll = Random.value * table.Sum(entry => entry.Weight);
        foreach ((string prefabName, int min, int max, float weight) in table)
        {
            roll -= weight;
            if (roll > 0f)
            {
                continue;
            }

            GameObject prefab = ZNetScene.instance?.GetPrefab(prefabName);
            if (prefab != null)
            {
                GameObject find = Object.Instantiate(prefab, position + Vector3.up * 0.5f, Quaternion.identity);
                find.GetComponent<ItemDrop>()?.SetStack(Random.Range(min, max + 1));
                ItemDrop.OnCreateNew(find);
            }

            return;
        }
    }

    /// <summary>
    /// What is known about a hit while its drops are watched.
    /// </summary>
    public sealed class Context
    {
        /// <summary>
        /// Creates a context.
        /// </summary>
        /// <param name="attacker">The player who hit.</param>
        /// <param name="skill">The gathering skill.</param>
        /// <param name="level">The player's level in it.</param>
        /// <param name="hit">The hit.</param>
        public Context(Player attacker, Skills.SkillType skill, float level, HitData hit)
        {
            Attacker = attacker;
            Skill = skill;
            Level = level;
            FallDirection = hit.m_dir;
        }

        /// <summary>The player who hit.</summary>
        public Player Attacker { get; }

        /// <summary>The gathering skill.</summary>
        public Skills.SkillType Skill { get; }

        /// <summary>The player's level in it.</summary>
        public float Level { get; }

        /// <summary>Items dropped so far.</summary>
        public List<ItemDrop> Drops { get; } = new();

        /// <summary>Whether a falling tree is old growth.</summary>
        public bool OldGrowth { get; set; }

        /// <summary>Whether a falling tree knocks down others.</summary>
        public bool Domino { get; set; }

        /// <summary>Whether the tree fell during this hit.</summary>
        public bool Felled { get; set; }

        /// <summary>The way a tree falls.</summary>
        public Vector3 FallDirection { get; set; }
    }
}
