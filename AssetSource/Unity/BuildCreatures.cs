using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Turns every animated creature in Assets/Creatures (FBX + textures from AssetSource/Tools/export_creature.py) into a
/// prefab "&lt;name&gt;_visual": the model under an empty root, with an Animator whose controller uses the parameter names
/// Valheim's Character, MonsterAI and Attack drive, and Standard materials carrying the textures the mod moves onto
/// vanilla creature materials at runtime. Called by <see cref="BuildForagingBundle.Build"/>.
/// </summary>
public static class BuildCreatures
{
    private const string Folder = "Assets/Creatures";

    private static readonly string[] floatParameters = { "forward_speed", "sideway_speed", "turn_speed", "statef", "tilt", "anim_speed" };
    private static readonly string[] boolParameters =
    {
        "inWater", "onGround", "encumbered", "flying", "sleeping", "falling", "blocking", "dead", "slipping", "skating", "skategliding"
    };
    private static readonly string[] triggerParameters = { "stagger", "attack_abort", "detach", "wakeup" };

    [Serializable]
    private class ClipSpec
    {
        public string name;
        public string take;
        public bool loop;
    }

    [Serializable]
    private class AttackSpec
    {
        public string trigger;
        public string clip;
        public float hit = 0.5f;
    }

    [Serializable]
    private class ControllerSpec
    {
        public string idle;
        public string move;
        public float move_speed = 1f;
        public string run;
        public float run_speed = 2f;
        public string stagger;
        public AttackSpec[] attacks = new AttackSpec[0];
        public bool always_animate;
    }

    [Serializable]
    private class CreatureSpec
    {
        public ClipSpec[] clips = new ClipSpec[0];
        public string front_bone;
        public ControllerSpec controller = new();
    }

    [Serializable]
    private class MaterialSpec
    {
        public string name;
        public string albedo;
        public string normal;
        public float[] colour;
        public float[] emission;
    }

    [Serializable]
    private class MaterialList
    {
        public MaterialSpec[] materials = new MaterialSpec[0];
    }

    /// <summary>
    /// Builds the creature prefabs.
    /// </summary>
    /// <returns>Asset paths of the prefabs, for the bundle. Their models, clips, controllers and textures come along as dependencies.</returns>
    public static string[] Prepare()
    {
        if (!Directory.Exists(Folder))
        {
            return new string[0];
        }

        List<string> prefabs = new();
        foreach (string fbx in Directory.GetFiles(Folder, "*.fbx").Select(path => path.Replace('\\', '/')).OrderBy(path => path))
        {
            string name = Path.GetFileNameWithoutExtension(fbx);
            CreatureSpec spec = JsonUtility.FromJson<CreatureSpec>(File.ReadAllText($"{Folder}/{name}.creature.json"));
            MaterialList materials = JsonUtility.FromJson<MaterialList>(File.ReadAllText($"{Folder}/{name}.materials.json"));

            ConfigureTextures(materials);
            ConfigureModel(fbx, spec);
            Dictionary<string, AnimationClip> clips = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToDictionary(clip => clip.name);
            AnimatorController controller = BuildController(name, spec, clips);
            Material[] created = BuildMaterials(name, materials);
            prefabs.Add(BuildPrefab(name, fbx, spec, clips, controller, created));
        }

        return prefabs.ToArray();
    }

    private static void ConfigureTextures(MaterialList materials)
    {
        foreach (MaterialSpec material in materials.materials)
        {
            ConfigureTexture(material.albedo, false);
            ConfigureTexture(material.normal, true);
        }
    }

    private static void ConfigureTexture(string name, bool normal)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath($"{Folder}/{name}.png");
        importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = !normal;
        importer.mipmapEnabled = true;
        importer.maxTextureSize = 1024;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.isReadable = false;
        importer.SaveAndReimport();
    }

    private static void ConfigureModel(string path, CreatureSpec spec)
    {
        ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(path);
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.importBlendShapes = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.optimizeGameObjects = false;
        importer.meshCompression = ModelImporterMeshCompression.Off;
        // Readable so the mod can bake a pose into a static mesh (trophies, items).
        importer.isReadable = true;
        importer.clipAnimations = new ModelImporterClipAnimation[0];
        importer.SaveAndReimport();

        ModelImporterClipAnimation[] takes = importer.defaultClipAnimations;
        List<ModelImporterClipAnimation> clips = new();
        foreach (ClipSpec clipSpec in spec.clips)
        {
            ModelImporterClipAnimation take = takes.FirstOrDefault(each => each.takeName == clipSpec.name || each.takeName.EndsWith("|" + clipSpec.name, StringComparison.Ordinal))
                ?? throw new InvalidOperationException($"{path}: no take '{clipSpec.name}' among {string.Join(", ", takes.Select(each => each.takeName))}");
            take.name = clipSpec.name;
            take.loopTime = clipSpec.loop;
            take.lockRootRotation = true;
            take.lockRootHeightY = true;
            take.lockRootPositionXZ = true;
            clips.Add(take);
        }

        // Valheim's CharacterAnimEvent turns this event into the attack's hit.
        foreach (ModelImporterClipAnimation clip in clips)
        {
            clip.events = (spec.controller.attacks ?? new AttackSpec[0])
                .Where(attack => attack.clip == clip.name)
                .Select(attack => new AnimationEvent { functionName = "OnAttackTrigger", time = attack.hit })
                .ToArray();
        }

        importer.clipAnimations = clips.ToArray();
        importer.SaveAndReimport();
    }

    private static AnimatorController BuildController(string name, CreatureSpec spec, Dictionary<string, AnimationClip> clips)
    {
        string path = $"{Folder}/{name}.controller";
        AssetDatabase.DeleteAsset(path);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        foreach (string parameter in floatParameters)
        {
            controller.AddParameter(parameter, AnimatorControllerParameterType.Float);
        }

        foreach (string parameter in boolParameters)
        {
            controller.AddParameter(parameter, AnimatorControllerParameterType.Bool);
        }

        controller.AddParameter("statei", AnimatorControllerParameterType.Int);
        AttackSpec[] attacks = spec.controller.attacks ?? new AttackSpec[0];
        foreach (string trigger in triggerParameters.Concat(attacks.Select(attack => attack.trigger)).Distinct())
        {
            controller.AddParameter(trigger, AnimatorControllerParameterType.Trigger);
        }

        AnimationClip idle = Clip(clips, spec.controller.idle, name);
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState movement;
        if (!string.IsNullOrEmpty(spec.controller.move))
        {
            movement = controller.CreateBlendTreeInController("Movement", out BlendTree tree, 0);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = "forward_speed";
            tree.useAutomaticThresholds = false;
            tree.AddChild(idle, 0f);
            tree.AddChild(Clip(clips, spec.controller.move, name), spec.controller.move_speed);
            if (!string.IsNullOrEmpty(spec.controller.run))
            {
                tree.AddChild(Clip(clips, spec.controller.run, name), spec.controller.run_speed);
            }
        }
        else
        {
            movement = machine.AddState("Movement");
            movement.motion = idle;
        }

        machine.defaultState = movement;
        HashSet<string> used = new() { spec.controller.idle, spec.controller.move, spec.controller.run };
        if (!string.IsNullOrEmpty(spec.controller.stagger))
        {
            // Character.Stagger sets this trigger when the stagger bar fills.
            AnimatorState stagger = machine.AddState("stagger");
            stagger.motion = Clip(clips, spec.controller.stagger, name);
            AnimatorStateTransition into = machine.AddAnyStateTransition(stagger);
            into.AddCondition(AnimatorConditionMode.If, 0f, "stagger");
            into.hasExitTime = false;
            into.duration = 0.1f;
            into.canTransitionToSelf = false;
            AnimatorStateTransition back = stagger.AddTransition(movement);
            back.hasExitTime = true;
            back.exitTime = 0.9f;
            back.duration = 0.2f;
            used.Add(spec.controller.stagger);
        }

        foreach (AttackSpec attack in attacks)
        {
            AnimatorState state = machine.AddState(attack.trigger);
            state.motion = Clip(clips, attack.clip, name);
            // Humanoid.InAttack looks for this tag.
            state.tag = "attack";
            AnimatorStateTransition into = machine.AddAnyStateTransition(state);
            into.AddCondition(AnimatorConditionMode.If, 0f, attack.trigger);
            into.hasExitTime = false;
            into.duration = 0.15f;
            into.canTransitionToSelf = false;
            AnimatorStateTransition back = state.AddTransition(movement);
            back.hasExitTime = true;
            back.exitTime = 0.92f;
            back.duration = 0.25f;
            used.Add(attack.clip);
        }

        // Clips without a transition (e.g. die) are played by the mod with Animator.Play(name).
        foreach (AnimationClip clip in clips.Values.Where(clip => !used.Contains(clip.name)))
        {
            machine.AddState(clip.name).motion = clip;
        }

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static AnimationClip Clip(Dictionary<string, AnimationClip> clips, string clip, string creature)
    {
        return clips.TryGetValue(clip ?? string.Empty, out AnimationClip found)
            ? found
            : throw new InvalidOperationException($"{creature}: no clip '{clip}' among {string.Join(", ", clips.Keys)}");
    }

    private static Material[] BuildMaterials(string name, MaterialList list)
    {
        Shader shader = Shader.Find("Standard");
        List<Material> materials = new();
        foreach (MaterialSpec spec in list.materials)
        {
            Material material = new(shader) { name = $"{name}_{spec.name}" };
            if (!string.IsNullOrEmpty(spec.albedo))
            {
                material.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>($"{Folder}/{spec.albedo}.png"));
            }

            if (!string.IsNullOrEmpty(spec.normal))
            {
                material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{Folder}/{spec.normal}.png"));
                material.EnableKeyword("_NORMALMAP");
            }

            // Blender gives linear colours; Material.color is gamma space.
            if (spec.colour != null && spec.colour.Length >= 3)
            {
                material.color = new Color(spec.colour[0], spec.colour[1], spec.colour[2], spec.colour.Length > 3 ? spec.colour[3] : 1f).gamma;
            }

            if (spec.emission != null && spec.emission.Length >= 3)
            {
                material.SetColor("_EmissionColor", new Color(spec.emission[0], spec.emission[1], spec.emission[2]));
                material.EnableKeyword("_EMISSION");
            }

            string path = $"{Folder}/{name}_{spec.name}.mat";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(material, path);
            materials.Add(material);
        }

        return materials.ToArray();
    }

    private static string BuildPrefab(string name, string fbx, CreatureSpec spec, Dictionary<string, AnimationClip> clips, AnimatorController controller, Material[] materials)
    {
        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbx));
        PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        GameObject root = new($"{name}_visual");
        model.transform.SetParent(root.transform, false);

        // The rest pose of a downloaded rig can be anything; start from the first frame of the idle clip instead.
        clips[spec.controller.idle].SampleAnimation(model, 0f);
        foreach (AnimationClip clip in clips.Values)
        {
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
            int bound = bindings.Count(binding => AnimationUtility.GetAnimatedObject(model, binding) != null);
            Debug.Log($"[WhiteHilt] Creature '{name}': clip {clip.name} binds {bound} of {bindings.Length} curves" +
                (bound < bindings.Length ? $", e.g. unbound '{bindings.First(binding => AnimationUtility.GetAnimatedObject(model, binding) == null).path}'" : string.Empty));
        }

        Animator animator = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.avatar = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<Avatar>().FirstOrDefault();
        animator.applyRootMotion = false;
        // Hits come from animation events, so an attacker must animate even when its owner cannot see it.
        animator.cullingMode = spec.controller.always_animate ? AnimatorCullingMode.AlwaysAnimate : AnimatorCullingMode.CullUpdateTransforms;

        SkinnedMeshRenderer[] renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (SkinnedMeshRenderer renderer in renderers)
        {
            int count = renderer.sharedMesh.subMeshCount;
            renderer.sharedMaterials = Enumerable.Range(0, count).Select(i => materials[Mathf.Min(i, materials.Length - 1)]).ToArray();
            renderer.updateWhenOffscreen = spec.controller.always_animate;
            if (count != materials.Length)
            {
                Debug.LogWarning($"[WhiteHilt] Creature '{name}': {count} submeshes but {materials.Length} materials");
            }
        }

        if (!string.IsNullOrEmpty(spec.front_bone))
        {
            Transform front = model.GetComponentsInChildren<Transform>(true).FirstOrDefault(each => each.name == spec.front_bone)
                ?? throw new InvalidOperationException($"{name}: no bone '{spec.front_bone}'");
            if (root.transform.InverseTransformPoint(front.position).z < 0f)
            {
                model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * model.transform.localRotation;
            }

            Debug.Log($"[WhiteHilt] Creature '{name}': front bone {spec.front_bone} at {root.transform.InverseTransformPoint(front.position):F2}");
        }

        Bounds bounds = MeasureBounds(root, renderers);
        string path = $"{Folder}/{name}_visual.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        Debug.Log($"[WhiteHilt] Creature '{name}': {renderers.Length} renderers, clips {string.Join(", ", clips.Keys)}, " +
            $"idle bounds centre {bounds.center:F2} size {bounds.size:F2}");
        return path;
    }

    private static Bounds MeasureBounds(GameObject root, SkinnedMeshRenderer[] renderers)
    {
        Bounds bounds = new();
        bool first = true;
        foreach (SkinnedMeshRenderer renderer in renderers)
        {
            Mesh baked = new();
            renderer.BakeMesh(baked, true);
            foreach (Vector3 vertex in baked.vertices)
            {
                Vector3 point = root.transform.InverseTransformPoint(renderer.transform.TransformPoint(vertex));
                if (first)
                {
                    bounds = new Bounds(point, Vector3.zero);
                    first = false;
                }
                else
                {
                    bounds.Encapsulate(point);
                }
            }

            UnityEngine.Object.DestroyImmediate(baked);
        }

        return bounds;
    }
}
