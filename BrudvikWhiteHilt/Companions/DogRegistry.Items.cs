using BrudvikWhiteHilt.Helpers;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Companions;

public static partial class DogRegistry
{
    /// <summary>
    /// True for items whose recipe stays unknown until the player's first dog.
    /// </summary>
    /// <param name="prefabName">Prefab name of the crafted item.</param>
    /// <returns>Whether the item belongs to the dog.</returns>
    public static bool IsDogItem(string prefabName)
    {
        return prefabName != null && dogItems.Contains(prefabName);
    }

    /// <summary>
    /// True when the item is the dog whistle.
    /// </summary>
    /// <param name="item">An inventory item.</param>
    /// <returns>Whether it is the whistle.</returns>
    public static bool IsWhistle(ItemDrop.ItemData item)
    {
        return item?.m_dropPrefab != null && item.m_dropPrefab.name == WhistlePrefabName;
    }

    /// <summary>
    /// Which collar an item is.
    /// </summary>
    /// <param name="item">An inventory item.</param>
    /// <returns>1 for leather, 2 for iron, 0 when it is no collar.</returns>
    public static int GetCollar(ItemDrop.ItemData item)
    {
        string prefabName = item?.m_dropPrefab != null ? item.m_dropPrefab.name : null;
        return prefabName == null ? 0 : Math.Max(0, Array.IndexOf(collarPrefabs, prefabName));
    }

    /// <summary>
    /// Prefab name of a collar.
    /// </summary>
    /// <param name="collar">1 for leather, 2 for iron.</param>
    /// <returns>The prefab name, or null.</returns>
    public static string GetCollarPrefab(int collar)
    {
        return collar > 0 && collar < collarPrefabs.Length ? collarPrefabs[collar] : null;
    }

    /// <summary>
    /// Armour a collar gives the dog.
    /// </summary>
    /// <param name="collar">1 for leather, 2 for iron.</param>
    /// <returns>The armour.</returns>
    public static float GetCollarArmor(int collar)
    {
        return collar > 0 && collar < collarArmor.Length ? collarArmor[collar] : 0f;
    }

    /// <summary>
    /// The model of a collar: a ring in the XZ plane.
    /// </summary>
    /// <param name="collar">1 for leather, 2 for iron.</param>
    /// <param name="mesh">The ring.</param>
    /// <param name="texture">Its texture.</param>
    public static void GetCollarModel(int collar, out Mesh mesh, out Texture2D texture)
    {
        if (collar == 1)
        {
            mesh = ForagingAssets.LoadMesh("dogcollarleather");
            texture = ForagingAssets.LoadTexture("dogcollarleather_albedo");
            return;
        }

        mesh = ForagingAssets.LoadMesh("dogcollariron");

        // The spiked collar has no texture of its own, only a white base colour.
        if (ironCollarTexture == null)
        {
            ironCollarTexture = VisualHelper.RecolorTexture(ForagingAssets.LoadTexture("dogcollariron_albedo"), _ => new Color32(62, 62, 68, 255));
        }

        texture = ironCollarTexture;
    }

    /// <summary>
    /// Drops a stack of an item on the ground.
    /// </summary>
    /// <param name="prefabName">Prefab name of the item.</param>
    /// <param name="amount">How many.</param>
    /// <param name="position">Where.</param>
    public static void DropItem(string prefabName, int amount, Vector3 position)
    {
        ItemDrop prefab = PrefabManager.Instance.GetPrefab(prefabName)?.GetComponent<ItemDrop>();
        if (prefab == null)
        {
            return;
        }

        ItemDrop.ItemData data = prefab.m_itemData.Clone();
        data.m_dropPrefab = prefab.gameObject;
        data.m_stack = amount;
        ItemDrop.DropItem(data, amount, position + Vector3.up * 0.5f, Quaternion.identity);
    }

    /// <summary>
    /// Puts a stick in a dog's mouth: the vanilla club's mesh, across the jaw.
    /// </summary>
    /// <param name="jaw">The dog's jaw bone.</param>
    /// <returns>The stick, or null.</returns>
    public static GameObject CreateStickModel(Transform jaw)
    {
        MeshFilter source = PrefabManager.Instance.GetPrefab("Club")?.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(filter => filter.sharedMesh != null);
        MeshRenderer renderer = source != null ? source.GetComponent<MeshRenderer>() : null;
        if (renderer == null)
        {
            return null;
        }

        // The club lies along its z, like the jaw's z runs from cheek to cheek; the tongue sits at (-0.07, -0.02, 0).
        GameObject stick = new("whitehilt_stick");
        stick.transform.SetParent(jaw, false);
        stick.transform.localPosition = new Vector3(-0.07f, -0.01f, 0f);
        stick.transform.localScale = Vector3.one * 0.58f;
        stick.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
        stick.AddComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
        return stick;
    }

    /// <summary>
    /// True once the player has had a dog, which makes the dog's pieces known.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>Whether the player knows about dogs.</returns>
    public static bool IsDogKnown(Player player)
    {
        if (player.HaveUniqueKey(KnownKey))
        {
            return true;
        }

        if (!player.HaveUniqueKey(BuyKey))
        {
            return false;
        }

        // The buy key goes when the dog does; this one stays.
        player.AddUniqueKey(KnownKey);
        return true;
    }

    /// <summary>
    /// Writes a dog's name and age into an item (remains or gravestone).
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="dogName">The dog's name.</param>
    /// <param name="days">Days the dog lived.</param>
    public static void SetInscription(ItemDrop.ItemData item, string dogName, int days)
    {
        item.m_customData[NameData] = dogName;
        item.m_customData[DaysData] = days.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Copies the dog's name and age from one item to another.
    /// </summary>
    /// <param name="from">The item with the inscription.</param>
    /// <param name="to">The item to write it into.</param>
    public static void CopyInscription(ItemDrop.ItemData from, ItemDrop.ItemData to)
    {
        if (from.m_customData.TryGetValue(NameData, out string dogName))
        {
            to.m_customData[NameData] = dogName;
            to.m_customData[DaysData] = from.m_customData.TryGetValue(DaysData, out string days) ? days : "0";
        }
    }

    /// <summary>
    /// True when the item carries a dog's name.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>Whether it is inscribed.</returns>
    public static bool HasInscription(ItemDrop.ItemData item)
    {
        return item != null && item.m_customData.ContainsKey(NameData);
    }

    /// <summary>
    /// The dog's name and age on two lines, in the local player's language, or null.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The inscription.</returns>
    public static string GetInscription(ItemDrop.ItemData item)
    {
        if (!HasInscription(item))
        {
            return null;
        }

        item.m_customData.TryGetValue(DaysData, out string text);
        int.TryParse(text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int days);
        string age = days == 1
            ? Localization.instance.Localize(Translations.Token("whitehilt_dog_day"))
            : Localization.instance.Localize(Translations.Token("whitehilt_dog_days"), days.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return item.m_customData[NameData] + "\n" + age;
    }
}
