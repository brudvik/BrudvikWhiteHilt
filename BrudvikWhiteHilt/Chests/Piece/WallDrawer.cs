using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Ships.Skidbladnir;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Chests.Piece;

/// <summary>Identifies a compact chest variant that must attach to a player-built wall.</summary>
public sealed class WallDrawer : MonoBehaviour
{
	/// <summary>Fits the drawer's collision, visuals and category icon without changing its inventory.</summary>
	/// <param name="prefab">Cloned category chest.</param>
	public static void Configure(GameObject prefab)
	{
		prefab.AddComponent<WallDrawer>();
		Renderer template = prefab.GetComponentsInChildren<MeshRenderer>(true).First();
		foreach (MeshRenderer renderer in prefab.GetComponentsInChildren<MeshRenderer>(true)) renderer.enabled = false;
		foreach (LODGroup group in prefab.GetComponentsInChildren<LODGroup>(true)) UnityEngine.Object.DestroyImmediate(group);
		foreach (Collider collider in prefab.GetComponentsInChildren<Collider>(true))
			if (!collider.isTrigger) UnityEngine.Object.DestroyImmediate(collider);
		Container container = prefab.GetComponent<Container>();
		container.m_open = null;
		container.m_closed = null;
		GameObject visual = new("DrawerVisual") { layer = LayerMask.NameToLayer("piece") };
		visual.transform.SetParent(prefab.transform, false);
		WearNTear wear = prefab.GetComponent<WearNTear>();
		wear.m_new = visual;
		wear.m_worn = visual;
		wear.m_broken = visual;
		wear.m_fragmentRoots = null;
		BoxCollider box = visual.AddComponent<BoxCollider>();
		box.center = new Vector3(0, 0.15f, 0.145f);
		box.size = new Vector3(0.7f, 0.3f, 0.29f);
		global::Piece piece = prefab.GetComponent<global::Piece>();
		piece.m_notOnFloor = true;
		piece.m_notOnTiltingSurface = false;
		Transform icon = prefab.transform.Find("Icon");
		if (icon != null)
		{
			icon.localPosition = new Vector3(0.18f, 0.20f, 0.301f);
			Sprite sprite = icon.GetComponent<SpriteRenderer>()?.sprite;
			icon.localScale = Vector3.one * (sprite != null && sprite.bounds.size.x > 0 ? 0.14f / sprite.bounds.size.x : 0.15f);
		}
		if (VisualHelper.IsHeadless) return;
		try
		{
			Mesh mesh = ForagingAssets.LoadMesh("walldrawer");
			GameObject model = VisualHelper.CreateModel(visual.transform, mesh, ForagingAssets.LoadTexture("walldrawer_albedo"),
				template, Vector3.zero, Quaternion.identity, 1f);
			Vector3 scale = new(0.7f / mesh.bounds.size.x, 0.3f / mesh.bounds.size.y, 0.3f / mesh.bounds.size.z);
			model.transform.localScale = scale;
			model.transform.localPosition = Vector3.Scale(new Vector3(-mesh.bounds.center.x, -mesh.bounds.min.y, -mesh.bounds.min.z), scale);
		}
		catch (Exception exception)
		{
			Jotunn.Logger.LogError($"Wall drawer model failed: {exception}");
			throw;
		}
	}

	/// <summary>Checks for a player-built wooden or stone wall directly behind the drawer.</summary>
	/// <returns>True only with an upright, suitable supporting wall.</returns>
	public bool HasWall()
	{
		Vector3 origin = transform.TransformPoint(new Vector3(0, 0.15f, 0.05f));
		foreach (RaycastHit hit in Physics.RaycastAll(origin, -transform.forward, 0.25f,
			LayerMask.GetMask("piece", "vehicle"), QueryTriggerInteraction.Ignore).OrderBy(hit => hit.distance))
		{
			if (hit.collider.GetComponentInParent<WallDrawer>() != null) continue;
			if (Mathf.Abs(hit.normal.y) > 0.15f || Vector3.Dot(hit.normal, transform.forward) < 0.9f) return false;
			global::Piece support = hit.collider.GetComponentInParent<global::Piece>();
			if (support == null || !support.IsPlacedByPlayer()) return false;
			if (support.GetComponent<SkidbladnirShip>() != null)
				return hit.collider.name.IndexOf("wall", StringComparison.OrdinalIgnoreCase) >= 0;
			WearNTear wear = support.GetComponent<WearNTear>();
			return global::Utils.GetPrefabName(support.gameObject).IndexOf("wall", StringComparison.OrdinalIgnoreCase) >= 0
				&& wear != null && (wear.m_materialType == WearNTear.MaterialType.Wood
					|| wear.m_materialType == WearNTear.MaterialType.HardWood || wear.m_materialType == WearNTear.MaterialType.Stone);
		}
		return false;
	}
}