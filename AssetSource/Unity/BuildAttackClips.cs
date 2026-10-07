#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Makes the White Hilt Rune Sword's own attack animations: three humanoid clips (a rising overhead strike, a lunging
/// thrust and a leaping whirl) written key pose by key pose below, and preview strips of them played by the game's own
/// player animator. Run with <c>-executeMethod BuildAttackClips.Build</c> (no -nographics, the previews render).
/// </summary>
/// <remarks>
/// The poses are this mod's own work. Valheim's bundles are only read at build time, from the local game: for the
/// player's avatar and controller (to check that the clips play in the dual knives' states the mod swaps them into),
/// for the sword's ready stance the clips start and end in (so the game blends into and out of them cleanly), and for
/// a sword to hold in the previews. Nothing of the game is written into the clips but that stance.
/// In game, Items/Weapons/Styles/RuneSwordMotion.cs swaps these clips in for the dual knives' combo while the Rune
/// Sword is in hand; the seconds per hit in AttackStyles.RuneSecondsPerHit must follow the Chain events here.
/// </remarks>
public static class BuildAttackClips
{
    /// <summary>Folder of the generated clips, which BuildForagingBundle puts in the bundle.</summary>
    public const string OutputFolder = "Assets/Animations";

    private const float FrameRate = 30f;
    private const string DefaultGame = @"C:\Program Files (x86)\Steam\steamapps\common\Valheim";

    // The dual knives' combo, whose states play the clips in game (state speed 1.1).
    private static readonly string[] Triggers = { "dual_knives0", "dual_knives1", "dual_knives2" };
    private static readonly string[] ReplacedClips = { "Knife Attack Combo (1)", "Knife Attack Combo (2)", "Knife Attack Combo (3)" };

    /// <summary>
    /// The generated clips in the project, for the bundle; none until <see cref="Build"/> has run.
    /// </summary>
    /// <returns>Asset paths.</returns>
    public static string[] Prepare()
    {
        return Directory.Exists(OutputFolder)
            ? Directory.GetFiles(OutputFolder, "*.anim").Select(path => path.Replace('\\', '/')).OrderBy(path => path).ToArray()
            : new string[0];
    }

    /// <summary>
    /// Writes the clips to <see cref="OutputFolder"/> and the previews to Preview/attacks.
    /// </summary>
    public static void Build()
    {
        string game = Environment.GetEnvironmentVariable("VALHEIM_INSTALL") ?? DefaultGame;
        string bundles = Path.Combine(game, @"valheim_Data\StreamingAssets\SoftRef\Bundles");
        List<AssetBundle> loaded = Directory.GetFiles(bundles).Select(AssetBundle.LoadFromFile).Where(b => b != null).ToList();
        try
        {
            AssetBundle items = loaded.First(b => b.name == "c4210710");
            GameObject playerPrefab = items.LoadAllAssets<GameObject>().First(g => g.name == "Player");
            GameObject swordPrefab = items.LoadAllAssets<GameObject>().First(g => g.name == "SwordNiedhogg");

            GameObject player = UnityEngine.Object.Instantiate(playerPrefab);
            Animator animator = player.GetComponentsInChildren<Animator>(true).First();
            AnimationClip[] vanilla = animator.runtimeAnimatorController.animationClips;
            HumanPose stance = Sample(animator, vanilla.First(c => c.name == "Attack1"), 0f);

            Directory.CreateDirectory(OutputFolder);
            AnimationClip[] clips = RuneSword.Cuts.Select(cut => Write(cut, stance)).ToArray();
            AssetDatabase.SaveAssets();

            Preview(player, animator, swordPrefab, clips);
            UnityEngine.Object.DestroyImmediate(player);
        }
        finally
        {
            loaded.ForEach(bundle => bundle.Unload(true));
        }
    }

    private static HumanPose Sample(Animator animator, AnimationClip clip, float time)
    {
        clip.SampleAnimation(animator.gameObject, time);
        HumanPoseHandler handler = new(animator.avatar, animator.transform);
        HumanPose pose = new();
        handler.GetHumanPose(ref pose);
        handler.Dispose();
        return pose;
    }

    // Bakes a cut at 30 frames per second: between two keys every muscle eases in and out, and the body turns about
    // the vertical by a yaw that may pass a full turn, which keyed quaternions could not.
    private static AnimationClip Write(Cut cut, HumanPose stance)
    {
        string[] muscles = HumanTrait.MuscleName;
        AnimationClip clip = new() { name = cut.Name, frameRate = FrameRate };
        int frames = Mathf.RoundToInt(cut.Length * FrameRate);
        AnimationCurve[] muscleCurves = muscles.Select(_ => new AnimationCurve()).ToArray();
        AnimationCurve[] root = Enumerable.Range(0, 7).Select(_ => new AnimationCurve()).ToArray();
        for (int frame = 0; frame <= frames; frame++)
        {
            float time = frame / FrameRate;
            for (int m = 0; m < muscles.Length; m++)
            {
                muscleCurves[m].AddKey(time, cut.Muscle(muscles[m], time, stance.muscles[m]));
            }

            Vector3 offset = cut.Offset(time);
            Quaternion rotation = Quaternion.AngleAxis(cut.Yaw(time), Vector3.up) * stance.bodyRotation;
            Vector3 position = stance.bodyPosition + offset;
            float[] values = { position.x, position.y, position.z, rotation.x, rotation.y, rotation.z, rotation.w };
            for (int i = 0; i < 7; i++)
            {
                root[i].AddKey(time, values[i]);
            }
        }

        for (int m = 0; m < muscles.Length; m++)
        {
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Animator), CurveName(muscles[m])), muscleCurves[m]);
        }

        string[] rootNames = { "RootT.x", "RootT.y", "RootT.z", "RootQ.x", "RootQ.y", "RootQ.z", "RootQ.w" };
        for (int i = 0; i < 7; i++)
        {
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Animator), rootNames[i]), root[i]);
        }

        // The game does not apply root motion, so the turn and the steps stay in the pose and the body comes back.
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        settings.loopBlendOrientation = true;
        settings.loopBlendPositionY = true;
        settings.loopBlendPositionXZ = true;
        settings.keepOriginalOrientation = true;
        settings.keepOriginalPositionY = true;
        settings.keepOriginalPositionXZ = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AnimationUtility.SetAnimationEvents(clip, cut.Events);

        string path = $"{OutputFolder}/{cut.Name}.anim";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip, path);
        Debug.Log($"[WhiteHilt] Clip '{cut.Name}': {cut.Length:0.00} s, humanMotion {clip.humanMotion}, " +
            $"events {string.Join(", ", cut.Events.Select(e => $"{e.functionName}@{e.time:0.00}"))}");
        return clip;
    }

    // Clips name finger muscles "RightHand.Index.1 Stretched" where HumanTrait says "Right Index 1 Stretched".
    private static string CurveName(string muscle)
    {
        foreach (string side in new[] { "Left", "Right" })
        {
            foreach (string finger in new[] { "Thumb", "Index", "Middle", "Ring", "Little" })
            {
                string prefix = $"{side} {finger} ";
                if (muscle.StartsWith(prefix, StringComparison.Ordinal))
                {
                    string rest = muscle.Substring(prefix.Length);
                    return rest == "Spread" ? $"{side}Hand.{finger}.Spread" : $"{side}Hand.{finger}.{rest}";
                }
            }
        }

        return muscle;
    }

    // Plays the clips in the game's controller with the dual knives' clips overridden, as RuneSwordMotion does, next to
    // the vanilla sword combo, and writes one strip of frames per attack.
    private static void Preview(GameObject player, Animator animator, GameObject swordPrefab, AnimationClip[] clips)
    {
        string folder = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Preview", "attacks");
        Directory.CreateDirectory(folder);

        AnimatorOverrideController controller = new(animator.runtimeAnimatorController);
        List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new(controller.overridesCount);
        controller.GetOverrides(overrides);
        for (int i = 0; i < overrides.Count; i++)
        {
            int index = Array.IndexOf(ReplacedClips, overrides[i].Key.name);
            if (index >= 0)
            {
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, clips[index]);
            }
        }

        controller.ApplyOverrides(overrides);
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.fireEvents = false;

        Material body = new(Shader.Find("Standard")) { color = new Color(0.75f, 0.7f, 0.62f) };
        player.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        foreach (LODGroup group in player.GetComponentsInChildren<LODGroup>(true))
        {
            group.enabled = false;
        }

        SkinnedMeshRenderer[] skins = player.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(s => s.gameObject.activeInHierarchy).ToArray();
        foreach (Renderer renderer in player.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = false;
        }

        foreach (SkinnedMeshRenderer skin in skins)
        {
            skin.enabled = true;
            skin.updateWhenOffscreen = true;
            skin.forceMatrixRecalculationPerRender = true;
            skin.sharedMaterials = skin.sharedMaterials.Select(_ => body).ToArray();
            Debug.Log($"[WhiteHilt] Body mesh {skin.sharedMesh.name}: readable {skin.sharedMesh.isReadable}, {skin.sharedMesh.vertexCount} vertices");
        }

        // Skinned meshes drawn by camera.Render in batch mode keep an old pose, so each frame is baked into plain meshes.
        GameObject baked = new("baked");
        List<(SkinnedMeshRenderer Skin, Transform Part, Mesh Mesh)> parts = new();
        foreach (SkinnedMeshRenderer skin in skins)
        {
            GameObject part = new(skin.name);
            part.transform.SetParent(baked.transform, false);
            Mesh mesh = new();
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            part.AddComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials.Select(_ => body).ToArray();
            parts.Add((skin, part.transform, mesh));
        }

        Transform hand = animator.GetBoneTransform(HumanBodyBones.RightHand).Find("RightHand_Attach");
        GameObject sword = HoldSword(hand, swordPrefab);

        GameObject lightObject = new("Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        lightObject.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
        RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.5f);

        GameObject cameraObject = new("Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.16f, 0.18f, 0.22f);
        camera.fieldOfView = 40f;
        const int size = 256;
        RenderTexture target = new(size, size, 24);
        camera.targetTexture = target;

        foreach ((string name, string[] triggers) in new[]
        {
            ("rune", Triggers),
            ("vanilla_sword", new[] { "swing_longsword0", "swing_longsword1", "swing_longsword2" })
        })
        {
            foreach (var view in new[] { ("side", new Vector3(3.2f, 1.3f, 1.6f)), ("top", new Vector3(0.01f, 5.5f, 0.6f)) })
            {
                List<Texture2D> frames = new();
                animator.Rebind();
                animator.SetBool("onGround", true);
                animator.SetInteger("statei", 1);
                animator.Play("Movement", 0);
                animator.Update(0.5f);
                foreach (string trigger in triggers)
                {
                    animator.SetTrigger(trigger);
                    animator.Update(0f);
                    for (int step = 0; step < 10; step++)
                    {
                        animator.Update(0.1f);
                        foreach ((SkinnedMeshRenderer skin, Transform part, Mesh mesh) in parts)
                        {
                            skin.BakeMesh(mesh, true);
                            part.SetPositionAndRotation(skin.transform.position, skin.transform.rotation);
                        }

                        Vector3 focus = new(0f, 0.9f, 0.4f);
                        camera.transform.position = focus + view.Item2;
                        camera.transform.LookAt(focus);
                        camera.Render();
                        RenderTexture.active = target;
                        Texture2D frame = new(size, size, TextureFormat.RGB24, false);
                        frame.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                        frame.Apply();
                        frames.Add(frame);
                    }
                }

                int columns = 10;
                int rows = (frames.Count + columns - 1) / columns;
                Texture2D sheet = new(columns * size, rows * size, TextureFormat.RGB24, false);
                for (int i = 0; i < frames.Count; i++)
                {
                    sheet.SetPixels(i % columns * size, (rows - 1 - i / columns) * size, size, size, frames[i].GetPixels());
                }

                sheet.Apply();
                string file = Path.Combine(folder, $"{name}_{view.Item1}.png");
                File.WriteAllBytes(file, sheet.EncodeToPNG());
                Debug.Log($"[WhiteHilt] Preview {file}: rows = cuts, columns = 0.1 s steps");
            }
        }

        RenderTexture.active = null;
        UnityEngine.Object.DestroyImmediate(cameraObject);
        UnityEngine.Object.DestroyImmediate(lightObject);
        UnityEngine.Object.DestroyImmediate(baked);
        Debug.Log($"[WhiteHilt] Preview drew {parts.Count} skinned parts and {sword.GetComponentsInChildren<Renderer>().Count(r => r.enabled)} sword renderers");
    }

    // Puts a sword in the hand as the game's VisEquipment.AttachItem does: the item's attach object on the hand joint at
    // zero position and rotation. It is the White Hilt Rune Sword's own model when the project has it (in attach space,
    // as VisualHelper.ReplaceWeaponMesh shows it), else the vanilla sword it is cloned from.
    private static GameObject HoldSword(Transform hand, GameObject swordPrefab)
    {
        Mesh runeSword = AssetDatabase.LoadAllAssetsAtPath("Assets/Foraging/whrunesword.obj").OfType<Mesh>().FirstOrDefault();
        GameObject sword;
        if (runeSword != null)
        {
            sword = new GameObject("whrunesword");
            sword.transform.SetParent(hand, false);
            sword.AddComponent<MeshFilter>().sharedMesh = runeSword;
            Material material = new(Shader.Find("Standard"))
            {
                mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Foraging/whrunesword_albedo.png")
            };
            sword.AddComponent<MeshRenderer>().sharedMaterial = material;
        }
        else
        {
            sword = UnityEngine.Object.Instantiate(swordPrefab.transform.Find("attach").gameObject, hand, false);
            Material steel = new(Shader.Find("Standard")) { color = new Color(0.55f, 0.1f, 0.1f) };
            foreach (Renderer renderer in sword.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = renderer.gameObject.activeInHierarchy;
                renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => steel).ToArray();
            }

            foreach (Behaviour behaviour in sword.GetComponentsInChildren<Behaviour>(true).Where(b => b != null))
            {
                behaviour.enabled = false;
            }
        }

        sword.transform.localPosition = Vector3.zero;
        sword.transform.localRotation = Quaternion.identity;
        Debug.Log($"[WhiteHilt] Preview sword: {(runeSword != null ? "White Hilt Rune Sword" : "vanilla SwordNiedhogg")}");
        return sword;
    }

    // One attack: key poses (muscle values, body turn and offset at given times) and its animation events.
    private sealed class Cut
    {
        private readonly List<(float Time, Dictionary<string, float> Muscles, float Yaw, Vector3 Offset)> keys = new();

        public Cut(string name, float length)
        {
            Name = name;
            Length = length;
        }

        public string Name { get; }

        public float Length { get; }

        public AnimationEvent[] Events { get; private set; } = new AnimationEvent[0];

        // A key pose; muscles not named keep the stance. The first and last key should be the stance itself.
        public Cut Key(float time, float yaw, Vector3 offset, params (string Muscle, float Value)[] muscles)
        {
            keys.Add((time, muscles.ToDictionary(m => m.Muscle, m => m.Value), yaw, offset));
            return this;
        }

        // Speed events of the attack before set the animator's speed and it stays, so each cut starts by resetting it.
        public Cut WithEvents(params (string Function, float Time)[] events)
        {
            Events = events.Select(e => new AnimationEvent { functionName = e.Function, time = e.Time })
                .Prepend(new AnimationEvent { functionName = "Speed", time = 0f, floatParameter = 1f })
                .ToArray();
            return this;
        }

        public float Muscle(string muscle, float time, float stance)
        {
            return Ease(time, key => key.Muscles.TryGetValue(muscle, out float value) ? value : stance);
        }

        public float Yaw(float time) => Ease(time, key => key.Yaw);

        public Vector3 Offset(float time)
        {
            return new Vector3(Ease(time, key => key.Offset.x), Ease(time, key => key.Offset.y), Ease(time, key => key.Offset.z));
        }

        private float Ease(float time, Func<(float Time, Dictionary<string, float> Muscles, float Yaw, Vector3 Offset), float> value)
        {
            if (time <= keys[0].Time)
            {
                return value(keys[0]);
            }

            for (int i = 1; i < keys.Count; i++)
            {
                if (time <= keys[i].Time)
                {
                    float t = Mathf.InverseLerp(keys[i - 1].Time, keys[i].Time, time);
                    return Mathf.Lerp(value(keys[i - 1]), value(keys[i]), t * t * (3f - 2f * t));
                }
            }

            return value(keys[keys.Count - 1]);
        }
    }

    // The Rune Sword's three cuts. Muscle names are Unity's (HumanTrait.MuscleName); 0 is the muscle's middle, the
    // signs as the game's sword cuts use them: Arm Down-Up + raises the arm, Forearm Stretch + straightens the elbow.
    private static class RuneSword
    {
        private const string Twist = "Spine Twist Left-Right";
        private const string Bend = "Spine Front-Back";
        private const string ChestBend = "Chest Front-Back";
        private const string Shoulder = "Right Shoulder Down-Up";
        private const string ShoulderFront = "Right Shoulder Front-Back";
        private const string Arm = "Right Arm Down-Up";
        private const string ArmFront = "Right Arm Front-Back";
        private const string ArmTwist = "Right Arm Twist In-Out";
        private const string Elbow = "Right Forearm Stretch";
        private const string ForearmTwist = "Right Forearm Twist In-Out";
        private const string Wrist = "Right Hand Down-Up";
        private const string WristSide = "Right Hand In-Out";
        private const string LeftArm = "Left Arm Down-Up";
        private const string LeftArmFront = "Left Arm Front-Back";
        private const string LeftElbow = "Left Forearm Stretch";
        private const string LeftLeg = "Left Upper Leg Front-Back";
        private const string RightLeg = "Right Upper Leg Front-Back";
        private const string LeftKnee = "Left Lower Leg Stretch";
        private const string RightKnee = "Right Lower Leg Stretch";

        public static readonly Cut[] Cuts =
        {
            // Rising overhead strike: the sword goes up over the head and comes straight down in front.
            new Cut("runesword_cut0", 0.75f)
                .Key(0f, 0f, Vector3.zero)
                .Key(0.26f, 15f, new Vector3(0f, 0.03f, -0.05f),
                    (Twist, 0.5f), (Bend, -0.45f), (Shoulder, 1.4f), (ShoulderFront, 0.3f), (Arm, 1.3f), (ArmFront, 0.6f),
                    (Elbow, 0.1f), (ForearmTwist, 0.6f), (Wrist, 0.7f), (LeftArm, -0.2f), (LeftArmFront, 0.6f))
                .Key(0.42f, -10f, new Vector3(0f, -0.08f, 0.25f),
                    (Twist, -0.2f), (Bend, 0.6f), (ChestBend, 0.4f), (Shoulder, 0.6f), (ShoulderFront, 0.5f), (Arm, -0.1f),
                    (ArmFront, 0.9f), (Elbow, 0.95f), (ForearmTwist, 0.6f), (Wrist, -0.6f), (LeftArm, -0.6f),
                    (LeftLeg, 0.6f), (LeftKnee, 0.2f), (RightLeg, -0.3f))
                .Key(0.55f, -10f, new Vector3(0f, -0.1f, 0.3f),
                    (Twist, -0.2f), (Bend, 0.7f), (ChestBend, 0.4f), (Shoulder, 0.5f), (Arm, -0.35f), (ArmFront, 0.8f),
                    (Elbow, 0.95f), (ForearmTwist, 0.6f), (Wrist, -0.8f), (LeftArm, -0.6f),
                    (LeftLeg, 0.6f), (LeftKnee, 0.2f), (RightLeg, -0.3f))
                .Key(0.75f, 0f, Vector3.zero)
                .WithEvents(("TrailOn", 0.32f), ("OnAttackTrigger", 0.42f), ("TrailOff", 0.5f), ("Chain", 0.6f)),

            // Lunging thrust: the sword is drawn back at the hip and driven straight forward with a long step.
            new Cut("runesword_cut1", 0.7f)
                .Key(0f, 0f, Vector3.zero)
                .Key(0.22f, 30f, new Vector3(0f, -0.04f, -0.1f),
                    (Twist, 0.9f), (Bend, -0.1f), (Shoulder, 0.2f), (ShoulderFront, -0.8f), (Arm, -0.5f), (ArmFront, -0.8f),
                    (Elbow, 0f), (ForearmTwist, -0.4f), (Wrist, 0.4f), (LeftArm, 0.2f), (LeftArmFront, 0.8f), (LeftElbow, 0.8f))
                .Key(0.33f, -15f, new Vector3(0f, -0.12f, 0.45f),
                    (Twist, -0.6f), (Bend, 0.45f), (Shoulder, 0.3f), (ShoulderFront, 0.5f), (Arm, 0.2f), (ArmFront, 0.6f),
                    (Elbow, 1f), (ForearmTwist, 0.2f), (Wrist, 0.1f), (WristSide, 1.6f), (LeftArm, -0.4f), (LeftArmFront, -0.6f), (LeftElbow, 1f),
                    (RightLeg, 0.5f), (RightKnee, 0.2f), (LeftLeg, -0.4f), (LeftKnee, 0.6f))
                .Key(0.48f, -15f, new Vector3(0f, -0.12f, 0.45f),
                    (Twist, -0.6f), (Bend, 0.45f), (Shoulder, 0.3f), (ShoulderFront, 0.5f), (Arm, 0.2f), (ArmFront, 0.6f),
                    (Elbow, 1f), (ForearmTwist, 0.2f), (Wrist, 0.1f), (WristSide, 1.6f), (LeftArm, -0.4f), (LeftArmFront, -0.6f), (LeftElbow, 1f),
                    (RightLeg, 0.5f), (RightKnee, 0.2f), (LeftLeg, -0.4f), (LeftKnee, 0.6f))
                .Key(0.7f, 0f, Vector3.zero)
                .WithEvents(("TrailOn", 0.26f), ("OnAttackTrigger", 0.32f), ("TrailOff", 0.42f), ("Chain", 0.52f)),

            // Leaping whirl: a crouch, a full turn in the air with the sword held out, and a low landing.
            new Cut("runesword_whirl", 1.15f)
                .Key(0f, 0f, Vector3.zero)
                .Key(0.25f, 40f, new Vector3(0f, -0.18f, 0f),
                    (Twist, 1f), (Bend, 0.4f), (Shoulder, 0.4f), (Arm, -0.2f), (ArmFront, -0.4f), (Elbow, 0.6f),
                    (LeftArm, 0.2f), (LeftArmFront, 0.6f), (LeftLeg, 0.6f), (RightLeg, 0.6f), (LeftKnee, -0.6f), (RightKnee, -0.6f))
                .Key(0.42f, -90f, new Vector3(0f, 0.16f, 0.15f),
                    (Twist, -0.3f), (Bend, 0.1f), (Shoulder, 0.6f), (Arm, 0.45f), (ArmFront, 0.2f), (Elbow, 1f),
                    (ForearmTwist, 0.4f), (Wrist, 0f), (LeftArm, 0.5f), (LeftArmFront, -0.3f), (LeftElbow, 1f),
                    (LeftLeg, 0.3f), (RightLeg, 0.2f), (LeftKnee, 0.2f), (RightKnee, 0.4f))
                .Key(0.6f, -250f, new Vector3(0f, 0.2f, 0.3f),
                    (Twist, -0.5f), (Bend, 0.1f), (Shoulder, 0.6f), (Arm, 0.45f), (ArmFront, 0.3f), (Elbow, 1f),
                    (ForearmTwist, 0.4f), (Wrist, -0.2f), (LeftArm, 0.5f), (LeftArmFront, -0.3f), (LeftElbow, 1f),
                    (LeftLeg, 0.3f), (RightLeg, 0.2f), (LeftKnee, 0.2f), (RightKnee, 0.4f))
                .Key(0.78f, -360f, new Vector3(0f, -0.2f, 0.4f),
                    (Twist, -0.9f), (Bend, 0.7f), (ChestBend, 0.3f), (Shoulder, 0.5f), (Arm, -0.3f), (ArmFront, 0.8f),
                    (Elbow, 0.9f), (Wrist, -0.6f), (LeftArm, -0.2f), (LeftArmFront, 0.4f),
                    (LeftLeg, 0.7f), (RightLeg, -0.2f), (LeftKnee, -0.4f), (RightKnee, 0.3f))
                .Key(0.92f, -360f, new Vector3(0f, -0.2f, 0.4f),
                    (Twist, -0.9f), (Bend, 0.7f), (ChestBend, 0.3f), (Shoulder, 0.5f), (Arm, -0.3f), (ArmFront, 0.8f),
                    (Elbow, 0.9f), (Wrist, -0.6f), (LeftArm, -0.2f), (LeftArmFront, 0.4f),
                    (LeftLeg, 0.7f), (RightLeg, -0.2f), (LeftKnee, -0.4f), (RightKnee, 0.3f))
                .Key(1.15f, -360f, Vector3.zero)
                .WithEvents(("TrailOn", 0.4f), ("OnAttackTrigger", 0.62f), ("TrailOff", 0.8f))
        };
    }
}
#endif
