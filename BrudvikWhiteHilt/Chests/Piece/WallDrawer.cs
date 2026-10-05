using BrudvikWhiteHilt.Helpers;
using BrudvikWhiteHilt.Pieces.Ships.Skidbladnir;
using System;
using System.Linq;
using UnityEngine;

namespace BrudvikWhiteHilt.Chests.Piece;

/// <summary>Identifies a compact chest variant that must attach to a player-built wall.</summary>
public sealed class WallDrawer : MonoBehaviour
{
	// Depth from the wall to the handle tip; the model's front face sits at 0.87 of it.
	private const float Depth = 0.22f;
	private const float FrontFace = Depth * 0.87f;
	private const int IconSize = 128;
	private const int CategorySize = 88;
	private const int ModelSize = 96;

	// Every drawer has the same model, so it is rendered once.
	private static Sprite drawerRender;

	/// <summary>Builds a build-menu icon that tells a drawer from its chest: the category icon with the drawer model in front.</summary>
	/// <param name="prefab">A configured drawer prefab.</param>
	/// <param name="category">The chest's icon, or null when it is not available.</param>
	/// <returns>The icon, or null on a server or when the model could not be rendered.</returns>
	public static Sprite ComposeIcon(GameObject prefab, Sprite category)
	{
		if (VisualHelper.IsHeadless) return null;
		if (drawerRender == null)
		{
			// The front sprite and glow belong to one category; keep them out of the shared render.
			SpriteRenderer[] sprites = prefab.GetComponentsInChildren<SpriteRenderer>(true).Where(sprite => sprite.enabled).ToArray();
			Light[] lights = prefab.GetComponentsInChildren<Light>(true).Where(light => light.enabled).ToArray();
			foreach (SpriteRenderer sprite in sprites) sprite.enabled = false;
			foreach (Light light in lights) light.enabled = false;
			try { drawerRender = VisualHelper.RenderIcon(prefab); }
			finally
			{
				foreach (SpriteRenderer sprite in sprites) sprite.enabled = true;
				foreach (Light light in lights) light.enabled = true;
			}
		}
		if (drawerRender == null || category == null) return drawerRender;
		var pixels = new Color32[IconSize * IconSize];
		Blend(pixels, VisualHelper.ReadPixels(category.texture, CategorySize, CategorySize, category.textureRect), CategorySize, 0, IconSize - CategorySize);
		Blend(pixels, VisualHelper.ReadPixels(drawerRender.texture, ModelSize, ModelSize, drawerRender.textureRect), ModelSize, IconSize - ModelSize, 0);
		Texture2D texture = VisualHelper.CreateTexture(prefab.name + "_icon", IconSize, IconSize, pixels);
		return Sprite.Create(texture, new Rect(0, 0, IconSize, IconSize), new Vector2(0.5f, 0.5f));
	}

	// Alpha-over of a square layer at (left, bottom); rows run bottom to top.
	private static void Blend(Color32[] target, Color32[] layer, int size, int left, int bottom)
	{
		for (int y = 0; y < size; y++)
			for (int x = 0; x < size; x++)
			{
				Color top = layer[y * size + x];
				int index = (bottom + y) * IconSize + left + x;
				Color under = target[index];
				float alpha = top.a + under.a * (1f - top.a);
				if (alpha <= 0f) continue;
				Color mixed = (top * top.a + under * under.a * (1f - top.a)) / alpha;
				mixed.a = alpha;
				target[index] = mixed;
			}
	}

	/// <summary>Turns the placement ghost square to the wall behind it and moves its back flat against it.</summary>
	public void SnapToWall()
	{
		Vector3 origin = transform.TransformPoint(new Vector3(0, 0.15f, Depth / 2f));
		foreach (RaycastHit hit in Physics.RaycastAll(origin, -transform.forward, 0.6f,
			LayerMask.GetMask("piece", "vehicle"), QueryTriggerInteraction.Ignore).OrderBy(hit => hit.distance))
		{
			if (hit.collider.GetComponentInParent<WallDrawer>() != null) continue;
			if (Mathf.Abs(hit.normal.y) > 0.15f) return;
			Vector3 normal = new Vector3(hit.normal.x, 0f, hit.normal.z).normalized;
			if (Vector3.Dot(normal, transform.forward) < 0.9f) return;
			transform.rotation = Quaternion.LookRotation(normal, Vector3.up);
			transform.position -= normal * Vector3.Dot(transform.position - hit.point, normal);
			return;
		}
	}

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
		box.center = new Vector3(0, 0.15f, FrontFace / 2f);
		box.size = new Vector3(0.7f, 0.3f, FrontFace);
		global::Piece piece = prefab.GetComponent<global::Piece>();
		piece.m_notOnFloor = true;
		piece.m_notOnTiltingSurface = false;
		Transform icon = prefab.transform.Find("Icon");
		if (icon != null)
		{
			icon.localPosition = new Vector3(0.18f, 0.20f, FrontFace + 0.015f);
			Sprite sprite = icon.GetComponent<SpriteRenderer>()?.sprite;
			icon.localScale = Vector3.one * (sprite != null && sprite.bounds.size.x > 0 ? 0.14f / sprite.bounds.size.x : 0.15f);
		}
		if (VisualHelper.IsHeadless) return;
		try
		{
			Mesh mesh = ForagingAssets.LoadMesh("walldrawer");
			GameObject model = VisualHelper.CreateModel(visual.transform, mesh, ForagingAssets.LoadTexture("walldrawer_albedo"),
				template, Vector3.zero, Quaternion.identity, 1f);
			Vector3 scale = new(0.7f / mesh.bounds.size.x, 0.3f / mesh.bounds.size.y, Depth / mesh.bounds.size.z);
			model.transform.localScale = scale;
			model.transform.localPosition = Vector3.Scale(new Vector3(-mesh.bounds.center.x, -mesh.bounds.min.y, -mesh.bounds.min.z), scale);
			// Iron bands, a pine front and a plank case, as the other chests and workbenches.
			PaletteSurfaces.Apply(model, new[] { (0.167f, PaletteSurfaces.Iron), (0.5f, PaletteSurfaces.Top), (0.833f, PaletteSurfaces.Frame) }, scale);
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