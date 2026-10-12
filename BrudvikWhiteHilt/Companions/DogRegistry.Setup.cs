using BrudvikWhiteHilt.Helpers;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Companions;

public static partial class DogRegistry
{
    // Registers the dog as a clone of the wolf: a name of its own, a ragdoll that keeps its size, no drops (it leaves
    // its own remains) and no breeding.
    private static void AddDog()
    {
        CreatureManager.OnVanillaCreaturesAvailable -= AddDog;
        try
        {
            string name = Translations.Token("whitehilt_dog");
            CustomCreature creature = new(DogPrefabName, "Wolf", new CreatureConfig { Name = name });
            GameObject prefab = creature.Prefab;
            Character character = prefab.GetComponent<Character>();
            character.m_name = name;

            // The dog is smaller than a wolf and grows, so its ragdoll must take its size.
            foreach (EffectList.EffectData effect in character.m_deathEffects.m_effectPrefabs)
            {
                effect.m_inheritParentScale = true;
            }

            // The remains are spawned by the dog itself, so they carry its name; no pelts or meat.
            CharacterDrop drops = prefab.GetComponent<CharacterDrop>();
            if (drops != null)
            {
                drops.m_drops.Clear();
            }

            Procreation procreation = prefab.GetComponent<Procreation>();
            if (procreation != null)
            {
                // Vanilla's m_pregnancyChance is the chance to skip a breeding check: 1 always skips.
                procreation.m_pregnancyChance = 1f;
            }

            prefab.AddComponent<RestPose>();
            prefab.AddComponent<DogCompanion>();
            prefab.AddComponent<DogActivities>();
            prefab.AddComponent<DogCare>();
            prefab.AddComponent<DogTricks>();
            prefab.AddComponent<DogExpression>();
            if (!VisualHelper.IsHeadless)
            {
                CreateSoundEffects(prefab);
            }

            CreatureManager.Instance.AddCreature(creature);
            Jotunn.Logger.LogInfo("Dog added!");
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError("Dog failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    // Registers the dog's items: a puppy per coat colour, remains, the gravestone, the whistle, the collars and the
    // coat, and the cuddle and memories effects.
    private static void AddItems()
    {
        PrefabManager.OnVanillaPrefabsAvailable -= AddItems;
        foreach (CoatColor color in colors)
        {
            AddItem(color.PrefabName, "TrophyWolf", 5f, new ItemConfig(), item => ShapePuppy(item, color.Recolor));
        }

        AddItem(RemainsPrefabName, "BoneFragments", 2f, new ItemConfig(), null);
        AddItem(GravestonePrefabName, "Stone", 20f, new ItemConfig
        {
            CraftingStation = CraftingStations.Stonecutter,
            Requirements = new RequirementConfig[]
            {
                new() { Item = RemainsPrefabName, Amount = 1 },
                new() { Item = "Stone", Amount = 10 }
            }
        }, ShapeGravestone);

        AddItem(WhistlePrefabName, "BoneFragments", 0.3f, new ItemConfig
        {
            CraftingStation = CraftingStations.Workbench,
            Requirements = new RequirementConfig[]
            {
                new() { Item = "BoneFragments", Amount = 4 },
                new() { Item = "LeatherScraps", Amount = 2 }
            }
        }, item => VisualHelper.ReplaceMesh(item.ItemPrefab, ForagingAssets.LoadMesh("dogwhistle"), ForagingAssets.LoadTexture("dogwhistle_albedo"), size: 0.3f));
        AddItem(LeatherCollarPrefabName, "LeatherScraps", 1f, new ItemConfig
        {
            CraftingStation = CraftingStations.Workbench,
            Requirements = new RequirementConfig[]
            {
                new() { Item = "LeatherScraps", Amount = 6 },
                new() { Item = "Bronze", Amount = 1 }
            }
        }, item => ShapeCollar(item, 1));
        AddItem(IronCollarPrefabName, "Chain", 2f, new ItemConfig
        {
            CraftingStation = CraftingStations.Forge,
            Requirements = new RequirementConfig[]
            {
                new() { Item = "Iron", Amount = 3 },
                new() { Item = "Chain", Amount = 1 },
                new() { Item = "LeatherScraps", Amount = 2 }
            }
        }, item => ShapeCollar(item, 2));
        AddItem(DogFoodPrefabName, "TurnipStew", 1f, new ItemConfig
        {
            CraftingStation = Pieces.Cooking.StonePot.StonePot.PrefabName,
            Amount = 2,
            Requirements = new RequirementConfig[]
            {
                new() { Item = "RawMeat", Amount = 2 },
                new() { Item = "BoneFragments", Amount = 3 },
                new() { Item = "Mushroom", Amount = 2 }
            }
        }, item => VisualHelper.Tint(item.ItemPrefab, new Color(0.85f, 0.7f, 0.5f)), maxStack: 10);
        AddItem(StickPrefabName, "Club", 0.5f, new ItemConfig
        {
            Requirements = new RequirementConfig[] { new() { Item = "Wood", Amount = 1 } }
        }, null);
        AddItem(TreatPrefabName, "CookedMeat", 0.1f, new ItemConfig
        {
            CraftingStation = Pieces.Cooking.StonePot.StonePot.PrefabName,
            Amount = 6,
            Requirements = new RequirementConfig[]
            {
                new() { Item = "RawMeat", Amount = 1 },
                new() { Item = "BoneFragments", Amount = 1 }
            }
        }, item => VisualHelper.Tint(item.ItemPrefab, new Color(0.6f, 0.4f, 0.25f)), maxStack: 50);
        AddItem(BandagePrefabName, "LeatherScraps", 0.2f, new ItemConfig
        {
            CraftingStation = CraftingStations.Workbench,
            Amount = 2,
            Requirements = new RequirementConfig[]
            {
                new() { Item = "LeatherScraps", Amount = 2 },
                new() { Item = "Resin", Amount = 1 },
                new() { Item = "Dandelion", Amount = 1 }
            }
        }, item => VisualHelper.Tint(item.ItemPrefab, new Color(0.95f, 0.9f, 0.8f)), maxStack: 10);
        AddItem(CoatPrefabName, "DeerHide", 1f, new ItemConfig
        {
            CraftingStation = CraftingStations.Workbench,
            MinStationLevel = 2,
            Requirements = new RequirementConfig[]
            {
                new() { Item = "DeerHide", Amount = 2 },
                new() { Item = "LeatherScraps", Amount = 4 },
                new() { Item = "TrollHide", Amount = 1 }
            }
        }, item => VisualHelper.Tint(item.ItemPrefab, new Color(0.55f, 0.35f, 0.2f)));
        AddCuddleEffect();
        AddMemoriesEffect();
    }

    private static void AddMemoriesEffect()
    {
        memoriesEffect = ScriptableObject.CreateInstance<SE_Stats>();
        memoriesEffect.name = MemoriesEffectName;
        memoriesEffect.m_name = Translations.Token("whitehilt_dog_memories");
        memoriesEffect.m_tooltip = Translations.Token("whitehilt_dog_memories_tooltip");
        memoriesEffect.m_ttl = MemoriesMinutes * 60f;
        memoriesEffect.m_staminaRegenMultiplier = 1.15f;
        memoriesEffect.m_icon = PrefabManager.Instance.GetPrefab(GravestonePrefabName)?.GetComponent<ItemDrop>()?.m_itemData.GetIcon();
        ItemManager.Instance.AddStatusEffect(new CustomStatusEffect(memoriesEffect, fixReference: false));
    }

    private static void AddCuddleEffect()
    {
        cuddleEffect = ScriptableObject.CreateInstance<SE_Stats>();
        cuddleEffect.name = CuddleEffectName;
        cuddleEffect.m_name = Translations.Token("whitehilt_dog_cuddle");
        cuddleEffect.m_tooltip = Translations.Token("whitehilt_dog_cuddle_tooltip");
        cuddleEffect.m_ttl = DogSettings.CuddleMinutes.Value * 60f;
        cuddleEffect.m_healthRegenMultiplier = 1.1f;
        cuddleEffect.m_staminaRegenMultiplier = 1.1f;
        cuddleEffect.m_icon = PrefabManager.Instance.GetPrefab(colors[0].PrefabName)?.GetComponent<ItemDrop>()?.m_itemData.GetIcon();
        ItemManager.Instance.AddStatusEffect(new CustomStatusEffect(cuddleEffect, fixReference: false));
    }

    // Runs for every ZNetScene, so it must be idempotent.
    private static void AddDogFoodToDiet()
    {
        MonsterAI ai = PrefabManager.Instance.GetPrefab(DogPrefabName)?.GetComponent<MonsterAI>();
        ItemDrop food = PrefabManager.Instance.GetPrefab(DogFoodPrefabName)?.GetComponent<ItemDrop>();
        if (ai != null && food != null && !ai.m_consumeItems.Contains(food))
        {
            ai.m_consumeItems.Add(food);
        }
    }

    // Registers one dog item as a clone of a vanilla item with its own name, weight and look. Food values are cleared,
    // as dog food is not for people.
    private static void AddItem(string prefabName, string copyFrom, float weight, ItemConfig config, Action<CustomItem> applyVisual, int maxStack = 1)
    {
        try
        {
            config.Name = Translations.Token(Translations.ItemKey(prefabName));
            config.Description = Translations.Token(Translations.ItemKey(prefabName) + "_description");
            CustomItem item = new(prefabName, copyFrom, config);

            ItemDrop.ItemData.SharedData shared = item.ItemDrop.m_itemData.m_shared;
            shared.m_weight = weight;
            shared.m_maxStackSize = maxStack;
            shared.m_itemType = ItemDrop.ItemData.ItemType.Misc;
            shared.m_value = 0;
            shared.m_teleportable = true;

            // Dog food is not for people.
            shared.m_food = 0f;
            shared.m_foodStamina = 0f;
            shared.m_foodEitr = 0f;
            shared.m_foodRegen = 0f;
            shared.m_consumeStatusEffect = null;
            if (applyVisual != null && !VisualHelper.IsHeadless)
            {
                try
                {
                    applyVisual(item);
                    Sprite icon = VisualHelper.RenderIcon(item.ItemPrefab);
                    if (icon != null)
                    {
                        shared.m_icons = new[] { icon };
                    }
                }
                catch (Exception ex)
                {
                    Jotunn.Logger.LogWarning($"{prefabName}: no custom look: {ex.Message}");
                }
            }

            ItemManager.Instance.AddItem(item);
        }
        catch (Exception ex)
        {
            Jotunn.Logger.LogError($"{prefabName} failed to load!");
            Jotunn.Logger.LogError(ex);
        }
    }

    private static void ShapeCollar(CustomItem item, int collar)
    {
        GetCollarModel(collar, out Mesh mesh, out Texture2D texture);
        VisualHelper.ReplaceMesh(item.ItemPrefab, mesh, texture, size: 0.25f);
    }

    // Copies of the wolf's alert sound effect with the dog's sounds in them, so they play through the game's sound settings.
    private static void CreateSoundEffects(GameObject dog)
    {
        GameObject source = dog.GetComponent<MonsterAI>().m_alertedEffects.m_effectPrefabs
            .Select(effect => effect.m_prefab)
            .FirstOrDefault(prefab => prefab != null && prefab.GetComponentInChildren<ZSFX>(true) != null);
        if (source == null)
        {
            Jotunn.Logger.LogWarning("Dog: the wolf has no alert sound to copy, the dog makes no sounds of its own");
            return;
        }

        foreach (string name in soundNames)
        {
            AudioClip clip;
            try
            {
                clip = ForagingAssets.LoadAudio(name);
            }
            catch (InvalidOperationException)
            {
                continue;
            }

            GameObject effect = PrefabManager.Instance.CreateClonedPrefab("sfx_whitehilt_" + name, source);
            ZSFX sfx = effect.GetComponentInChildren<ZSFX>(true);
            sfx.m_audioClips = new[] { clip };
            sfx.m_minPitch = 0.92f;
            sfx.m_maxPitch = 1.08f;
            if (name == "dogsnore")
            {
                sfx.m_minVol = 0.5f;
                sfx.m_maxVol = 0.5f;
            }

            sounds[name] = effect;
            soundLengths[name] = clip.length;
        }
    }

    private static void ShapeGravestone(CustomItem item)
    {
        VisualHelper.ReplaceMesh(item.ItemPrefab, ForagingAssets.LoadMesh("valkyriestone"), ForagingAssets.LoadTexture("valkyriestone_albedo"));
    }

    // Runs for every ZNetScene, so it must be idempotent.
    private static void AddToTrader()
    {
        Trader trader = PrefabManager.Instance.GetPrefab(TraderPrefabName)?.GetComponent<Trader>();
        if (trader == null)
        {
            Jotunn.Logger.LogWarning($"Dog: trader {TraderPrefabName} not found, puppies cannot be bought");
            return;
        }

        UpdateTrader(trader);
    }

    // Bog Witches already in the world hold their own copy of the list, so they are updated too.
    private static void UpdateTraders()
    {
        AddToTrader();
        foreach (Trader trader in UnityEngine.Object.FindObjectsByType<Trader>(FindObjectsSortMode.None))
        {
            if (Utils.GetPrefabName(trader.gameObject) == TraderPrefabName)
            {
                UpdateTrader(trader);
            }
        }
    }

    // Adds the puppies to the trader's goods at the configured price, or takes them away when dogs are switched off.
    private static void UpdateTrader(Trader trader)
    {
        foreach (CoatColor color in colors)
        {
            ItemDrop puppy = PrefabManager.Instance.GetPrefab(color.PrefabName)?.GetComponent<ItemDrop>();
            if (puppy == null)
            {
                continue;
            }

            Trader.TradeItem entry = trader.m_items.Find(candidate => candidate.m_prefab == puppy);
            if (!DogSettings.Enabled.Value)
            {
                if (entry != null)
                {
                    trader.m_items.Remove(entry);
                }

                continue;
            }

            if (entry == null)
            {
                // StoreGui.BuySelectedItem plays these after every purchase without a null check.
                entry = new Trader.TradeItem { m_prefab = puppy, m_stack = 1, m_buyKey = BuyKey, m_buyPlayerEffects = new EffectList() };
                trader.m_items.Add(entry);
            }

            entry.m_price = DogSettings.Price.Value;
        }
    }
}
