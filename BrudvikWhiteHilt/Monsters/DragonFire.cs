using UnityEngine;

namespace BrudvikWhiteHilt.Monsters;

/// <summary>
/// Marks a Desert Dragon (and the Black Dragon cloned from it), whose fire only burns buildings when
/// <see cref="MonsterSettings.DragonBurnsBuildings"/> is on.
/// </summary>
public class DragonFire : MonoBehaviour
{
	private int patchesThisBreath;

	/// <summary>Resets the patch budget when a new breath begins.</summary>
	public void BeginBreath()
	{
		patchesThisBreath = 0;
	}

	/// <summary>Leaves bounded ground fire at a dry terrain impact.</summary>
	/// <param name="projectile">The breath projectile, with its final scaled damage.</param>
	/// <param name="point">The terrain impact point.</param>
	public void Ignite(Projectile projectile, Vector3 point)
	{
		if (!MonsterSettings.DragonGroundFire.Value || point.y <= ZoneSystem.instance.m_waterLevel)
		{
			return;
		}

		Character source = projectile.m_owner;
		float baseDamage = projectile.m_weapon?.m_shared.m_damages.m_fire ?? 0f;
		float damage = DragonGroundFire.ScaledDamage(MonsterSettings.DragonGroundDamage.Value, projectile.m_damage.m_fire, baseDamage);
		if (damage <= 0f)
		{
			return;
		}

		int active = 0;
		foreach (DragonGroundFire patch in DragonGroundFire.Instances)
		{
			if (!patch.IsActive || patch.Source != source.GetZDOID())
			{
				continue;
			}
			active++;
			if ((patch.transform.position - point).sqrMagnitude < patch.Radius * patch.Radius)
			{
				patch.Refresh(damage);
				return;
			}
		}

		if (patchesThisBreath >= MonsterSettings.DragonGroundPerBreath.Value || active >= MonsterSettings.DragonGroundMax.Value)
		{
			return;
		}

		GameObject prefab = ZNetScene.instance.GetPrefab(DragonGroundFire.PrefabName);
		if (prefab != null)
		{
			DragonGroundFire patch = Instantiate(prefab, point, Quaternion.identity).GetComponent<DragonGroundFire>();
			patch.Initialize(source, damage);
			patchesThisBreath++;
		}
	}
}
