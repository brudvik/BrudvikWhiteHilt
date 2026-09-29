using BrudvikWhiteHilt.Building.Media;
using System.Collections.Generic;
using UnityEngine;

namespace BrudvikWhiteHilt.Building;

/// <summary>
/// A free-flying camera for building. While it is on, the player stands still, the game camera follows this one and
/// the player's eye is moved to it during placement, so pieces are aimed and placed from the camera.
/// The camera stays within reach of the player or inside a crafting station's build range; it may go below ground.
/// </summary>
public static class BuildCamera
{
    private const float PickupInterval = 0.3f;
    private const float MaxPitch = 89f;
    private const float FlyToDistance = 3f;
    private const float FlyToReach = 150f;
    private const float MinOrbitDistance = 1f;
    private const float EdgeMessageSeconds = 3f;
    private const float SpeedStep = 1.5f;

    private static readonly List<ItemDrop> pickupBuffer = new();

    private static Vector3 position;
    private static float yaw;
    private static float pitch;
    private static GameObject lightObject;
    private static bool lightOn = true;
    private static float pickupTimer;
    private static float savedMaxPlaceDistance;
    private static Vector3 savedEyePosition;
    private static bool eyeMoved;
    private static bool orbiting;
    private static Vector3 orbitPivot;
    private static float orbitDistance;
    private static float nextEdgeMessage;

    /// <summary>True while the build camera is on.</summary>
    public static bool Active { get; private set; }

    /// <summary>True while the camera light is on.</summary>
    public static bool LightOn => lightOn;

    /// <summary>Multiplier on the flying speed, changed with the speed keys.</summary>
    public static float SpeedFactor { get; private set; } = 1f;

    /// <summary>Where the camera is.</summary>
    public static Vector3 Position => position;

    /// <summary>Which way the camera looks.</summary>
    public static Quaternion Rotation => Quaternion.Euler(pitch, yaw, 0f);

    /// <summary>Camera heading in degrees.</summary>
    public static float Yaw => yaw;

    /// <summary>Camera pitch in degrees.</summary>
    public static float Pitch => pitch;

    /// <summary>
    /// Puts the camera at a given spot, e.g. a film point.
    /// </summary>
    /// <param name="at">Camera position.</param>
    /// <param name="newYaw">Heading in degrees.</param>
    /// <param name="newPitch">Pitch in degrees.</param>
    public static void SetPose(Vector3 at, float newYaw, float newPitch)
    {
        position = at;
        yaw = Mathf.Repeat(newYaw, 360f);
        pitch = Mathf.Clamp(Mathf.DeltaAngle(0f, newPitch), -MaxPitch, MaxPitch);
        orbiting = false;
    }

    /// <summary>
    /// Switches the camera on or off.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void Toggle(Player player)
    {
        if (Active)
        {
            Stop(player);
        }
        else
        {
            Start(player);
        }
    }

    /// <summary>
    /// Switches the camera off.
    /// </summary>
    /// <param name="player">The local player, or null if gone.</param>
    public static void Stop(Player player)
    {
        if (!Active)
        {
            return;
        }

        Active = false;
        orbiting = false;
        FilmPlayer.Stop();
        MediaPanel.Close();
        MediaMode.Reset();
        RestoreEye(player);
        if (player != null)
        {
            player.m_maxPlaceDistance = savedMaxPlaceDistance;
        }

        if (lightObject != null)
        {
            Object.Destroy(lightObject);
        }

        lightObject = null;
        BuildGizmos.UpdateRange(System.Array.Empty<(Vector3, float)>(), position);
    }

    /// <summary>
    /// Switches the camera off when the player can no longer build.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void CheckStillAllowed(Player player)
    {
        if (Active && (player == null || !CanUse(player)))
        {
            Stop(player);
        }
    }

    /// <summary>
    /// Switches the camera light on or off.
    /// </summary>
    /// <returns>True if the light is now on.</returns>
    public static bool ToggleLight()
    {
        lightOn = !lightOn;
        UpdateLight();
        return lightOn;
    }

    /// <summary>
    /// Makes the camera faster or slower.
    /// </summary>
    /// <param name="direction">1 faster, -1 slower.</param>
    public static void ChangeSpeed(int direction)
    {
        SpeedFactor = Mathf.Clamp(direction > 0 ? SpeedFactor * SpeedStep : SpeedFactor / SpeedStep, 0.1f, 10f);
    }

    /// <summary>
    /// Flies the camera to just in front of what it aims at, as far as the allowed area reaches.
    /// Switches the camera on first if needed.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void FlyTo(Player player)
    {
        if (!Active)
        {
            Start(player);
            if (!Active)
            {
                return;
            }
        }

        Vector3 direction = Rotation * Vector3.forward;
        if (!Physics.Raycast(position, direction, out RaycastHit hit, FlyToReach, player.m_placeRayMask))
        {
            return;
        }

        Vector3 target = hit.point - direction * Mathf.Min(FlyToDistance, hit.distance);
        if (IsAllowed(player, target))
        {
            position = target;
            return;
        }

        // Go as far along the way as the allowed area lets us.
        float allowed = 0f;
        float blocked = 1f;
        for (int i = 0; i < 16; i++)
        {
            float middle = (allowed + blocked) / 2f;
            if (IsAllowed(player, Vector3.Lerp(position, target, middle)))
            {
                allowed = middle;
            }
            else
            {
                blocked = middle;
            }
        }

        position = Vector3.Lerp(position, target, allowed);
        OnEdge(player);
    }

    /// <summary>
    /// Flies the camera and places the game camera on it. Called in place of vanilla's camera update.
    /// </summary>
    /// <param name="gameCamera">The game camera.</param>
    /// <param name="dt">Unscaled frame time.</param>
    public static void UpdateCamera(GameCamera gameCamera, float dt)
    {
        Player player = Player.m_localPlayer;
        float fov = MediaMode.CurrentFov(gameCamera.m_fov);
        if (FilmPlayer.Playing)
        {
            if (FilmPlayer.Update(dt, out Vector3 filmPosition, out Quaternion filmRotation, out float filmFov))
            {
                Vector3 euler = filmRotation.eulerAngles;
                SetPose(filmPosition, euler.y, euler.x);
                fov = filmFov;
            }
        }
        else if (player != null)
        {
            bool input = CanTakeInput();
            bool orbitHeld = input && !BuildToolbar.CursorMode && Input.GetKey(BuildToolSettings.OrbitKey.Value)
                && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl);
            if (orbitHeld && !orbiting)
            {
                BeginOrbit(player);
            }

            orbiting = orbitHeld;
            if (orbiting)
            {
                Orbit(player, dt);
            }
            else if (input)
            {
                Look();
                Fly(player, dt);
            }

            AutoPickup(player, dt);
        }

        if (player != null)
        {
            BuildGizmos.UpdateRange(MediaMode.HideUi ? System.Array.Empty<(Vector3, float)>() : AllowedAreas(player), position);
        }

        gameCamera.m_camera.fieldOfView = fov;
        gameCamera.m_skyCamera.fieldOfView = fov;
        gameCamera.transform.SetPositionAndRotation(position, Rotation);
        UpdateLight();
    }

    /// <summary>
    /// Moves the player's eye to the camera before placement, so aiming and reach are measured from it.
    /// </summary>
    /// <param name="player">The local player.</param>
    public static void MoveEye(Player player)
    {
        if (!Active || player.m_eye == null)
        {
            return;
        }

        if (!eyeMoved)
        {
            savedEyePosition = player.m_eye.localPosition;
            eyeMoved = true;
        }

        player.m_lookDir = Rotation * Vector3.forward;
        player.m_maxPlaceDistance = BuildToolSettings.MaxPlaceDistance.Value;
        player.m_eye.SetPositionAndRotation(position, Rotation);
    }

    /// <summary>
    /// Puts the player's eye back after the frame.
    /// </summary>
    /// <param name="player">The local player, or null.</param>
    public static void RestoreEye(Player player)
    {
        if (eyeMoved && player != null && player.m_eye != null)
        {
            player.m_eye.localPosition = savedEyePosition;
        }

        eyeMoved = false;
    }

    private static void Start(Player player)
    {
        if (!BuildToolSettings.CameraEnabled.Value)
        {
            player.Message(MessageHud.MessageType.Center, "$msg_whitehilt_build_camera_disabled");
            return;
        }

        if (!CanUse(player) || GameCamera.instance == null)
        {
            return;
        }

        Transform view = GameCamera.instance.transform;
        position = IsAllowed(player, view.position) ? view.position : player.m_eye.position;
        Vector3 euler = view.rotation.eulerAngles;
        yaw = euler.y;
        pitch = Mathf.Clamp(Mathf.DeltaAngle(0f, euler.x), -MaxPitch, MaxPitch);
        savedMaxPlaceDistance = player.m_maxPlaceDistance;
        pickupTimer = 0f;
        orbiting = false;
        Active = true;
        UpdateLight();
    }

    private static bool CanUse(Player player)
    {
        return player.InPlaceMode() && !player.IsDead() && !player.IsAttached() && !player.IsTeleporting() && !player.InCutscene();
    }

    private static bool CanTakeInput()
    {
        return (Chat.instance == null || !Chat.instance.HasFocus()) && !Console.IsVisible() && !Menu.IsVisible() && !TextInput.IsVisible()
            && !InventoryGui.IsVisible() && !Minimap.IsOpen() && !StoreGui.IsVisible();
    }

    private static void Look()
    {
        bool lookingFromPanel = (MediaPanel.IsOpen || Groups.BlueprintPanel.IsOpen) && Input.GetMouseButton(1);
        if (Hud.IsPieceSelectionVisible() || Hud.InRadial() || (BuildToolbar.CursorMode && !lookingFromPanel))
        {
            return;
        }

        Vector2 delta = ZInput.GetMouseDelta() * PlayerController.m_mouseSens * BuildToolSettings.MouseSensitivity.Value;
        bool invert = PlayerController.m_invertMouse != BuildToolSettings.InvertMouseY.Value;
        yaw = Mathf.Repeat(yaw + delta.x, 360f);
        pitch = Mathf.Clamp(pitch + (invert ? delta.y : -delta.y), -MaxPitch, MaxPitch);
    }

    private static float Speed()
    {
        return BuildToolSettings.MoveSpeed.Value * SpeedFactor * (ZInput.GetButton("Run") ? BuildToolSettings.FastMultiplier.Value : 1f);
    }

    private static void Fly(Player player, float dt)
    {
        Vector3 input = MoveInput();
        if (input == Vector3.zero)
        {
            return;
        }

        Quaternion rotation = Rotation;
        Vector3 move = rotation * Vector3.forward * input.z + rotation * Vector3.right * input.x;
        move = (Vector3.ClampMagnitude(move, 1f) + Vector3.up * input.y) * (Speed() * dt);

        // Slide along the edge of the allowed area instead of stopping dead.
        bool blocked = false;
        foreach (Vector3 step in new[] { move, new Vector3(move.x, 0f, 0f), new Vector3(0f, move.y, 0f), new Vector3(0f, 0f, move.z) })
        {
            if (step == Vector3.zero)
            {
                continue;
            }

            Vector3 next = position + step;
            if (IsAllowed(player, next))
            {
                position = next;
                if (step == move)
                {
                    break;
                }
            }
            else
            {
                blocked = true;
            }
        }

        if (blocked)
        {
            OnEdge(player);
        }
    }

    private static Vector3 MoveInput()
    {
        Vector3 input = Vector3.zero;

        // Ctrl+A/S/D would otherwise also fly the camera when used as shortcuts.
        if (Groups.GroupTools.CtrlComboUsed)
        {
            return input;
        }

        if (ZInput.GetButton("Forward"))
        {
            input.z += 1f;
        }

        if (ZInput.GetButton("Backward"))
        {
            input.z -= 1f;
        }

        if (ZInput.GetButton("Right"))
        {
            input.x += 1f;
        }

        if (ZInput.GetButton("Left"))
        {
            input.x -= 1f;
        }

        if (ZInput.GetButton("Jump"))
        {
            input.y += 1f;
        }

        // Ctrl doubles as the tilt modifier for the mouse wheel; once the wheel is used the camera stops sinking.
        if (ZInput.GetButton("Crouch") && !BuildTools.TiltWheelUsed)
        {
            input.y -= 1f;
        }

        return input;
    }

    private static void BeginOrbit(Player player)
    {
        Vector3 direction = Rotation * Vector3.forward;
        GameObject ghost = player.m_placementGhost;
        if (ghost != null && ghost.activeSelf)
        {
            orbitPivot = ghost.transform.position;
        }
        else if (Physics.Raycast(position, direction, out RaycastHit hit, FlyToReach, player.m_placeRayMask))
        {
            orbitPivot = hit.point;
        }
        else
        {
            orbitPivot = position + direction * 8f;
        }

        orbitDistance = Mathf.Max(MinOrbitDistance, Vector3.Distance(position, orbitPivot));
        Quaternion look = Quaternion.LookRotation(orbitPivot - position);
        Vector3 euler = look.eulerAngles;
        yaw = euler.y;
        pitch = Mathf.Clamp(Mathf.DeltaAngle(0f, euler.x), -MaxPitch, MaxPitch);
    }

    private static void Orbit(Player player, float dt)
    {
        float oldYaw = yaw;
        float oldPitch = pitch;
        float oldDistance = orbitDistance;
        Look();
        orbitDistance = Mathf.Max(MinOrbitDistance, orbitDistance - MoveInput().z * Speed() * dt);

        Vector3 next = orbitPivot - Rotation * Vector3.forward * orbitDistance;
        if (IsAllowed(player, next))
        {
            position = next;
            return;
        }

        yaw = oldYaw;
        pitch = oldPitch;
        orbitDistance = oldDistance;
        OnEdge(player);
    }

    private static void OnEdge(Player player)
    {
        BuildGizmos.ShowRange();
        if (Time.unscaledTime >= nextEdgeMessage)
        {
            nextEdgeMessage = Time.unscaledTime + EdgeMessageSeconds;
            player.Message(MessageHud.MessageType.TopLeft, "$msg_whitehilt_build_camera_edge");
        }
    }

    private static IEnumerable<(Vector3 Centre, float Radius)> AllowedAreas(Player player)
    {
        yield return (player.transform.position + Vector3.up, BuildToolSettings.CameraPlayerRange.Value);
        if (!BuildToolSettings.CameraStationRange.Value)
        {
            yield break;
        }

        foreach (CraftingStation station in CraftingStation.m_allStations)
        {
            if (station != null && station.m_rangeBuild > 0f)
            {
                yield return (station.transform.position, station.GetStationBuildRange());
            }
        }
    }

    private static bool IsAllowed(Player player, Vector3 point)
    {
        foreach ((Vector3 centre, float radius) in AllowedAreas(player))
        {
            if (Vector3.Distance(point, centre) <= radius)
            {
                return true;
            }
        }

        return false;
    }

    private static void AutoPickup(Player player, float dt)
    {
        if (!BuildToolSettings.AutoPickup.Value)
        {
            return;
        }

        pickupTimer -= dt;
        if (pickupTimer > 0f)
        {
            return;
        }

        pickupTimer = PickupInterval;
        float range = BuildToolSettings.AutoPickupRange.Value;
        Inventory inventory = player.GetInventory();
        pickupBuffer.Clear();
        pickupBuffer.AddRange(ItemDrop.s_instances);
        foreach (ItemDrop item in pickupBuffer)
        {
            if (item == null || !item.m_autoPickup || item.IsPiece() || Vector3.Distance(item.transform.position, position) > range
                || item.m_nview == null || !item.m_nview.IsValid() || player.HaveUniqueKey(item.m_itemData.m_shared.m_name))
            {
                continue;
            }

            if (!item.CanPickup())
            {
                item.RequestOwn();
                continue;
            }

            if (item.InTar())
            {
                continue;
            }

            item.Load();
            if (inventory.CanAddItem(item.m_itemData)
                && item.m_itemData.GetWeight() + inventory.GetTotalWeight() <= player.GetMaxCarryWeight())
            {
                player.Pickup(item.gameObject, autoequip: false, autoPickupDelay: false);
            }
        }

        pickupBuffer.Clear();
    }

    private static void UpdateLight()
    {
        bool wanted = Active && lightOn && GameCamera.instance != null;
        if (!wanted)
        {
            if (lightObject != null)
            {
                lightObject.SetActive(false);
            }

            return;
        }

        if (lightObject == null)
        {
            lightObject = new GameObject("WhiteHiltBuildCameraLight");
            lightObject.transform.SetParent(GameCamera.instance.transform, false);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Spot;
            light.spotAngle = 70f;
            light.shadows = LightShadows.None;
        }

        Light spot = lightObject.GetComponent<Light>();
        spot.intensity = BuildToolSettings.LightIntensity.Value;
        spot.range = BuildToolSettings.LightRange.Value;
        lightObject.SetActive(true);
    }
}
