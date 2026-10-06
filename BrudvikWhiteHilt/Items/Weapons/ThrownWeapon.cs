using Jotunn.Entities;
using Jotunn.Managers;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Items.Weapons;

/// <summary>
/// Lets a thrown White Hilt weapon fly as itself: its throw gets its own copy of the vanilla projectile, which keeps the
/// flight, hits and the drop of the thrown item, and shows the weapon's model instead of the vanilla spear.
/// </summary>
public static class ThrownWeapon
{
    /// <summary>
    /// Gives a throw its own copy of its projectile. Done on servers too, since the projectile is a networked prefab.
    /// </summary>
    /// <param name="weaponName">Prefab name of the weapon; the projectile is named after it.</param>
    /// <param name="throwAttack">The attack that throws the weapon.</param>
    /// <returns>The new projectile, or null if the attack throws nothing.</returns>
    public static GameObject CreateProjectile(string weaponName, Attack throwAttack)
    {
        if (throwAttack?.m_attackProjectile == null)
        {
            Jotunn.Logger.LogWarning($"{weaponName} throws no projectile; it keeps the vanilla throw.");
            return null;
        }

        GameObject projectile = PrefabManager.Instance.CreateClonedPrefab($"{weaponName}_projectile", throwAttack.m_attackProjectile);
        PrefabManager.Instance.AddPrefab(new CustomPrefab(projectile, false));
        throwAttack.m_attackProjectile = projectile;
        return projectile;
    }

    /// <summary>
    /// Shows the weapon's model on its projectile, tip first along the flight, and spins it if asked.
    /// </summary>
    /// <param name="projectile">The projectile from <see cref="CreateProjectile"/>.</param>
    /// <param name="model">The weapon's model under its attach child, in attach space with the tip along +z.</param>
    /// <param name="spin">Turns per second end over end; 0 for none.</param>
    public static void ApplyLook(GameObject projectile, GameObject model, float spin)
    {
        MeshRenderer target = projectile.GetComponentsInChildren<MeshRenderer>(true)
            .FirstOrDefault(renderer => renderer.GetComponent<MeshFilter>()?.sharedMesh != null);
        if (target == null)
        {
            Jotunn.Logger.LogWarning($"{projectile.name} has no mesh to replace; it keeps the vanilla look.");
            return;
        }

        Mesh mesh = model.GetComponent<MeshFilter>().sharedMesh;
        target.GetComponent<MeshFilter>().sharedMesh = mesh;
        target.sharedMaterials = model.GetComponent<MeshRenderer>().sharedMaterials;

        // A projectile flies along its +z, so the tip goes to the front; a spinning one turns about its middle.
        Transform pivot = new GameObject("spin").transform;
        pivot.SetParent(projectile.transform, false);
        pivot.gameObject.layer = target.gameObject.layer;
        target.transform.SetParent(pivot, false);
        target.transform.localRotation = Quaternion.identity;
        target.transform.localScale = Vector3.one;
        if (spin > 0f)
        {
            target.transform.localPosition = -mesh.bounds.center;
            pivot.localPosition = new Vector3(0f, 0f, -mesh.bounds.extents.z);
            pivot.gameObject.AddComponent<ThrownSpinner>().TurnsPerSecond = spin;
        }
        else
        {
            target.transform.localPosition = new Vector3(0f, 0f, -mesh.bounds.max.z);
        }
    }
}

/// <summary>
/// Turns a thrown weapon end over end while it flies.
/// </summary>
public class ThrownSpinner : MonoBehaviour
{
    /// <summary>
    /// Turns per second.
    /// </summary>
    public float TurnsPerSecond = 2f;

    private void Update()
    {
        transform.localRotation *= Quaternion.Euler(TurnsPerSecond * 360f * Time.deltaTime, 0f, 0f);
    }
}
