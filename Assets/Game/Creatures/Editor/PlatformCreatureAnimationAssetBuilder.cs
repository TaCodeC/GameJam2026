using System;
using System.IO;
using System.Linq;
using GameJam.Creatures;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

namespace GameJam.Creatures.Editor
{
    public static class PlatformCreatureAnimationAssetBuilder
    {
        private const string ArtRoot = "Assets/Game/Art/Creatures/Platform";
        private const string InfoCardsFolder = "Assets/Game/Art/Creatures/InfoCards";
        private const string UiButtonFolder = "Assets/Game/Art/UI/BotonesGenerales";
        private const string CreatureRoot = "Assets/Game/Creatures";
        private const string AnimationFolder = CreatureRoot + "/Animations";
        private const string AtlasFolder = CreatureRoot + "/Atlases";
        private const string PrefabFolder = CreatureRoot + "/Prefabs";
        private const string AtlasPath = AtlasFolder + "/PlatformCreatures.spriteatlas";
        private const string DefaultAluxeInfoCardPath = InfoCardsFolder + "/Perezoso_Ficha.jpg";
        private const string PreviousButtonPath = UiButtonFolder + "/BotonRegresar_Izquierda.png";
        private const string PreviousButtonHighlightedPath = UiButtonFolder + "/BotonRegresar_Izquierda_OnHover.png";
        private const string NextButtonPath = UiButtonFolder + "/BotonRegresar.png";
        private const string NextButtonHighlightedPath = UiButtonFolder + "/BotonRegresar_OnHover.png";
        private const string CloseButtonPath = UiButtonFolder + "/BotonSalir.png";
        private const string CloseButtonHighlightedPath = UiButtonFolder + "/BotonSalir_OnHover.png";
        private const string VisualRootName = "Visual";
        private const float PixelsPerUnit = 100f;
        private const int MaxTextureSize = 1024;
        private const int MaxInfoCardTextureSize = 2048;
        private const int MaxUiButtonTextureSize = 1024;

        private static readonly CreatureDefinition[] Definitions =
        {
            new("Aluxe", "Aluxe", "Aluxe_Idle", "Idle", 8f),
            new(
                "Perezoso",
                "Perezoso",
                "Perezoso_Walk",
                "Walk",
                8f,
                new[] { InfoCardsFolder + "/Perezoso_Ficha.jpg", InfoCardsFolder + "/Perezoso_Dato.jpg" },
                hasPatrol: true,
                patrolDistance: 6f,
                patrolSpeed: 0.85f),
            new(
                "Gliptodonte",
                "Gliptodonte",
                "Gliptodonte_Walk",
                "Walk",
                10f,
                new[] { InfoCardsFolder + "/Gliptodonte_Ficha.jpg", InfoCardsFolder + "/Gliptodonte_Dato.jpg" },
                hasPatrol: true,
                patrolDistance: 8f,
                patrolSpeed: 1.2f),
            new(
                "DientesDeSable",
                "DientesDeSable",
                "DientesDeSable_Run",
                "Run",
                12f,
                new[] { InfoCardsFolder + "/DientesDeSable_Ficha.jpg", InfoCardsFolder + "/DientesDeSable_Dato.jpg" },
                hasPatrol: true,
                patrolDistance: 9f,
                patrolSpeed: 2.1f),
            new(
                "LeonAmericano",
                "LeonAmericano",
                "LeonAmericano_Run",
                "Run",
                12f,
                new[] { InfoCardsFolder + "/LeonAmericano_Ficha.jpg", InfoCardsFolder + "/LeonAmericano_Dato.jpg" },
                hasPatrol: true,
                patrolDistance: 9f,
                patrolSpeed: 2.35f),
            new(
                "Gonfoterio",
                "Gonfoterio",
                "Gonfoterio_Walk",
                "Walk",
                8f,
                new[] { InfoCardsFolder + "/Gonfoterio_Ficha.jpg", InfoCardsFolder + "/Gonfoterio_Dato.jpg" },
                hasPatrol: true,
                patrolDistance: 7f,
                patrolSpeed: 0.9f)
        };

        [MenuItem("GameJam/Creatures/Rebuild Platform Creature Animation Assets")]
        public static void Build()
        {
            EnsureFolder("Assets/Game", "Creatures");
            EnsureFolder(CreatureRoot, "Animations");
            EnsureFolder(CreatureRoot, "Atlases");
            EnsureFolder(CreatureRoot, "Prefabs");

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            foreach (CreatureDefinition definition in Definitions)
                ConfigureTextureImports(definition);

            ConfigureInfoCardImports();
            ConfigureUiButtonImports();

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            foreach (CreatureDefinition definition in Definitions)
                BuildCreature(definition);

            PlatformNaiaPrefabBuilder.Build();
            BuildSpriteAtlas();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[PlatformCreatureAnimationAssetBuilder] Platform creature animation assets rebuilt.");
        }

        private static void ConfigureTextureImports(CreatureDefinition definition)
        {
            foreach (string path in Directory.GetFiles(definition.ArtFolder, "*.png").OrderBy(path => path, StringComparer.Ordinal))
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = PixelsPerUnit;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.maxTextureSize = MaxTextureSize;

                TextureImporterSettings textureSettings = new();
                importer.ReadTextureSettings(textureSettings);
                textureSettings.spriteAlignment = (int)SpriteAlignment.Custom;
                textureSettings.spritePivot = new Vector2(0.5f, 0f);
                importer.SetTextureSettings(textureSettings);

                ConfigurePlatform(importer, "DefaultTexturePlatform", MaxTextureSize);
                ConfigurePlatform(importer, "WebGL", MaxTextureSize);
                ConfigurePlatform(importer, "iPhone", MaxTextureSize);
                ConfigurePlatform(importer, "Android", MaxTextureSize);

                importer.SaveAndReimport();
            }
        }

        private static void ConfigureInfoCardImports()
        {
            if (!AssetDatabase.IsValidFolder(InfoCardsFolder))
                return;

            foreach (string path in Directory.GetFiles(InfoCardsFolder)
                         .Where(path => path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                         .OrderBy(path => path, StringComparer.Ordinal))
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.maxTextureSize = MaxInfoCardTextureSize;

                ConfigurePlatform(importer, "DefaultTexturePlatform", MaxInfoCardTextureSize);
                ConfigurePlatform(importer, "WebGL", MaxInfoCardTextureSize);
                ConfigurePlatform(importer, "iPhone", MaxInfoCardTextureSize);
                ConfigurePlatform(importer, "Android", MaxInfoCardTextureSize);

                importer.SaveAndReimport();
            }
        }

        private static void ConfigureUiButtonImports()
        {
            if (!AssetDatabase.IsValidFolder(UiButtonFolder))
                return;

            foreach (string path in Directory.GetFiles(UiButtonFolder, "*.png").OrderBy(path => path, StringComparer.Ordinal))
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.maxTextureSize = MaxUiButtonTextureSize;

                ConfigurePlatform(importer, "DefaultTexturePlatform", MaxUiButtonTextureSize);
                ConfigurePlatform(importer, "WebGL", MaxUiButtonTextureSize);
                ConfigurePlatform(importer, "iPhone", MaxUiButtonTextureSize);
                ConfigurePlatform(importer, "Android", MaxUiButtonTextureSize);

                importer.SaveAndReimport();
            }
        }

        private static void ConfigurePlatform(TextureImporter importer, string platformName, int maxTextureSize)
        {
            TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platformName);
            settings.name = platformName;
            settings.overridden = platformName != "DefaultTexturePlatform";
            settings.maxTextureSize = maxTextureSize;
            settings.textureCompression = TextureImporterCompression.Compressed;
            settings.compressionQuality = 50;
            settings.crunchedCompression = false;
            importer.SetPlatformTextureSettings(settings);
        }

        private static void BuildCreature(CreatureDefinition definition)
        {
            Sprite[] sprites = LoadSprites(definition);
            if (sprites.Length == 0)
            {
                Debug.LogError($"[PlatformCreatureAnimationAssetBuilder] No sprites found for {definition.Name} in {definition.ArtFolder}.");
                return;
            }

            AnimationClip clip = BuildSpriteClip(definition.ClipPath, sprites, definition.FrameRate);
            AnimatorController controller = BuildController(definition, clip);

            BuildPrefab(definition, sprites[0], controller);
        }

        private static Sprite[] LoadSprites(CreatureDefinition definition)
        {
            return AssetDatabase.FindAssets("t:Sprite", new[] { definition.ArtFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => Path.GetFileNameWithoutExtension(path).StartsWith(definition.FramePrefix, StringComparison.Ordinal))
                .OrderBy(path => ExtractTrailingNumber(Path.GetFileNameWithoutExtension(path)))
                .Select(AssetDatabase.LoadAssetAtPath<Sprite>)
                .Where(sprite => sprite != null)
                .ToArray();
        }

        private static int ExtractTrailingNumber(string value)
        {
            int underscore = value.LastIndexOf('_');
            return underscore >= 0 && int.TryParse(value[(underscore + 1)..], out int number)
                ? number
                : 0;
        }

        private static AnimationClip BuildSpriteClip(string clipPath, Sprite[] sprites, float frameRate)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, clipPath);
            }

            clip.frameRate = frameRate;

            foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                if (binding.type == typeof(SpriteRenderer) && binding.propertyName == "m_Sprite")
                    AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
            }

            EditorCurveBinding spriteBinding = new()
            {
                type = typeof(SpriteRenderer),
                path = VisualRootName,
                propertyName = "m_Sprite"
            };

            ObjectReferenceKeyframe[] frames = sprites
                .Select((sprite, index) => new ObjectReferenceKeyframe
                {
                    time = index / frameRate,
                    value = sprite
                })
                .ToArray();

            AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, frames);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimatorController BuildController(CreatureDefinition definition, AnimationClip clip)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(definition.ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(definition.ControllerPath);

            foreach (AnimatorControllerParameter parameter in controller.parameters.ToArray())
                controller.RemoveParameter(parameter);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState childState in stateMachine.states.ToArray())
                stateMachine.RemoveState(childState.state);

            foreach (ChildAnimatorStateMachine childStateMachine in stateMachine.stateMachines.ToArray())
                stateMachine.RemoveStateMachine(childStateMachine.stateMachine);

            AnimatorState state = stateMachine.AddState(definition.StateName, new Vector3(260f, 80f, 0f));
            state.motion = clip;
            stateMachine.defaultState = state;

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void BuildPrefab(CreatureDefinition definition, Sprite initialSprite, RuntimeAnimatorController controller)
        {
            GameObject creature = new(definition.Name);

            GameObject visual = new(VisualRootName);
            visual.transform.SetParent(creature.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            SpriteRenderer spriteRenderer = visual.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = initialSprite;

            Animator animator = creature.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            if (definition.Name == "Aluxe")
                ConfigureAluxeInteraction(creature, spriteRenderer);

            if (definition.InfoCardPaths.Length > 0)
                ConfigureAnimalInfo(creature, spriteRenderer, definition);

            if (definition.HasPatrol)
                ConfigureCreaturePatrol(creature, spriteRenderer, definition);

            PrefabUtility.SaveAsPrefabAsset(creature, definition.PrefabPath);
            Object.DestroyImmediate(creature);
        }

        private static void ConfigureAluxeInteraction(GameObject aluxe, SpriteRenderer spriteRenderer)
        {
            CircleCollider2D clickTarget = aluxe.AddComponent<CircleCollider2D>();
            clickTarget.isTrigger = true;
            clickTarget.offset = new Vector2(0f, 1.9f);
            clickTarget.radius = 1.9f;

            AluxeInfoInteractable interactable = aluxe.AddComponent<AluxeInfoInteractable>();
            Sprite defaultInfoCard = AssetDatabase.LoadAssetAtPath<Sprite>(DefaultAluxeInfoCardPath);

            SerializedObject serialized = new(interactable);
            serialized.FindProperty("_infoCardSprite").objectReferenceValue = defaultInfoCard;
            serialized.FindProperty("_nearbyMessage").stringValue = string.Empty;
            serialized.FindProperty("_interactionDistance").floatValue = 3f;
            serialized.FindProperty("_closeInfoWhenLeavingRange").boolValue = true;
            serialized.FindProperty("_showNearbyBubble").boolValue = false;
            serialized.FindProperty("_pauseTimeWhileShowingInfo").boolValue = true;
            serialized.FindProperty("_lockPlayerMovementWhileShowingInfo").boolValue = true;
            serialized.FindProperty("_clickTarget").objectReferenceValue = clickTarget;
            serialized.FindProperty("_spriteRenderer").objectReferenceValue = spriteRenderer;
            serialized.FindProperty("_closeButtonSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(CloseButtonPath);
            serialized.FindProperty("_closeButtonHighlightedSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(CloseButtonHighlightedPath);
            serialized.FindProperty("_closeButtonSize").vector2Value = new Vector2(96f, 96f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureAnimalInfo(GameObject creature, SpriteRenderer spriteRenderer, CreatureDefinition definition)
        {
            BoxCollider2D clickTarget = creature.AddComponent<BoxCollider2D>();
            clickTarget.isTrigger = true;

            if (spriteRenderer.sprite != null)
            {
                Bounds bounds = spriteRenderer.sprite.bounds;
                clickTarget.offset = bounds.center;
                clickTarget.size = new Vector2(bounds.size.x + 0.35f, bounds.size.y + 0.35f);
            }

            AnimalInfoInteractable interactable = creature.AddComponent<AnimalInfoInteractable>();
            SerializedObject serialized = new(interactable);

            SerializedProperty cards = serialized.FindProperty("_infoCards");
            cards.arraySize = definition.InfoCardPaths.Length;
            for (int i = 0; i < definition.InfoCardPaths.Length; i++)
            {
                Sprite card = AssetDatabase.LoadAssetAtPath<Sprite>(definition.InfoCardPaths[i]);
                cards.GetArrayElementAtIndex(i).objectReferenceValue = card;
            }

            serialized.FindProperty("_firstCardIndex").intValue = 0;
            serialized.FindProperty("_interactionDistance").floatValue = definition.InteractionDistance;
            serialized.FindProperty("_openAutomaticallyOnce").boolValue = true;
            serialized.FindProperty("_closeInfoWhenLeavingRange").boolValue = false;
            serialized.FindProperty("_pauseTimeWhileShowingInfo").boolValue = true;
            serialized.FindProperty("_lockPlayerMovementWhileShowingInfo").boolValue = true;
            serialized.FindProperty("_clickTarget").objectReferenceValue = clickTarget;
            serialized.FindProperty("_spriteRenderer").objectReferenceValue = spriteRenderer;
            serialized.FindProperty("_previousButtonSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(PreviousButtonPath);
            serialized.FindProperty("_previousButtonHighlightedSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(PreviousButtonHighlightedPath);
            serialized.FindProperty("_nextButtonSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(NextButtonPath);
            serialized.FindProperty("_nextButtonHighlightedSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(NextButtonHighlightedPath);
            serialized.FindProperty("_closeButtonSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(CloseButtonPath);
            serialized.FindProperty("_closeButtonHighlightedSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(CloseButtonHighlightedPath);
            serialized.FindProperty("_navigationButtonSize").vector2Value = new Vector2(132f, 132f);
            serialized.FindProperty("_closeButtonSize").vector2Value = new Vector2(96f, 96f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureCreaturePatrol(GameObject creature, SpriteRenderer spriteRenderer, CreatureDefinition definition)
        {
            CreaturePatrol patrol = creature.AddComponent<CreaturePatrol>();

            SerializedObject serialized = new(patrol);
            serialized.FindProperty("_patrolEnabled").boolValue = true;
            serialized.FindProperty("_travelDistance").floatValue = definition.PatrolDistance;
            serialized.FindProperty("_speed").floatValue = definition.PatrolSpeed;
            serialized.FindProperty("_turnPause").floatValue = 0.15f;
            serialized.FindProperty("_startAtLeftEdge").boolValue = true;
            serialized.FindProperty("_startMovingRight").boolValue = true;
            serialized.FindProperty("_spriteRenderer").objectReferenceValue = spriteRenderer;
            serialized.FindProperty("_flipSpriteToMovement").boolValue = true;
            serialized.FindProperty("_spriteFacesRight").boolValue = definition.SpriteFacesRight;
            serialized.FindProperty("_drawTrajectoryGizmo").boolValue = true;
            serialized.FindProperty("_trajectoryColor").colorValue = new Color(1f, 0f, 0f, 0.85f);
            serialized.FindProperty("_trajectoryHeightPadding").floatValue = 0.2f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildSpriteAtlas()
        {
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AtlasPath);
            if (atlas == null)
            {
                atlas = new SpriteAtlas();
                AssetDatabase.CreateAsset(atlas, AtlasPath);
            }

            SpriteAtlasPackingSettings packingSettings = atlas.GetPackingSettings();
            packingSettings.enableRotation = false;
            packingSettings.enableTightPacking = true;
            packingSettings.padding = 4;
            atlas.SetPackingSettings(packingSettings);

            SpriteAtlasTextureSettings textureSettings = atlas.GetTextureSettings();
            textureSettings.generateMipMaps = false;
            textureSettings.sRGB = true;
            textureSettings.filterMode = FilterMode.Bilinear;
            atlas.SetTextureSettings(textureSettings);

            TextureImporterPlatformSettings platformSettings = atlas.GetPlatformSettings("DefaultTexturePlatform");
            platformSettings.maxTextureSize = 4096;
            platformSettings.textureCompression = TextureImporterCompression.Compressed;
            platformSettings.compressionQuality = 50;
            atlas.SetPlatformSettings(platformSettings);

            Object[] existingPackables = SpriteAtlasExtensions.GetPackables(atlas);
            if (existingPackables.Length > 0)
                SpriteAtlasExtensions.Remove(atlas, existingPackables);

            string[] artFolders = Definitions
                .Select(definition => definition.ArtFolder)
                .Distinct()
                .ToArray();

            Object[] textures = AssetDatabase.FindAssets("t:Texture2D", artFolders)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => Definitions.Any(definition =>
                    path.StartsWith(definition.ArtFolder + "/", StringComparison.Ordinal)
                    && Path.GetFileNameWithoutExtension(path).StartsWith(definition.FramePrefix, StringComparison.Ordinal)))
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<Texture2D>)
                .Where(texture => texture != null)
                .Cast<Object>()
                .ToArray();

            SpriteAtlasExtensions.Add(atlas, textures);
            EditorUtility.SetDirty(atlas);
        }

        private static void EnsureFolder(string parentFolder, string childFolder)
        {
            string path = $"{parentFolder}/{childFolder}";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parentFolder, childFolder);
        }

        private readonly struct CreatureDefinition
        {
            public CreatureDefinition(
                string name,
                string artFolderName,
                string framePrefix,
                string stateName,
                float frameRate,
                string[] infoCardPaths = null,
                bool hasPatrol = false,
                float patrolDistance = 0f,
                float patrolSpeed = 0f,
                bool spriteFacesRight = true,
                float interactionDistance = 3f)
            {
                Name = name;
                ArtFolder = $"{ArtRoot}/{artFolderName}";
                FramePrefix = framePrefix;
                StateName = stateName;
                FrameRate = frameRate;
                InfoCardPaths = infoCardPaths ?? Array.Empty<string>();
                HasPatrol = hasPatrol;
                PatrolDistance = patrolDistance;
                PatrolSpeed = patrolSpeed;
                SpriteFacesRight = spriteFacesRight;
                InteractionDistance = interactionDistance;
                ClipPath = $"{AnimationFolder}/{framePrefix}.anim";
                ControllerPath = $"{AnimationFolder}/{name}.controller";
                PrefabPath = $"{PrefabFolder}/{name}.prefab";
            }

            public string Name { get; }
            public string ArtFolder { get; }
            public string FramePrefix { get; }
            public string StateName { get; }
            public float FrameRate { get; }
            public string[] InfoCardPaths { get; }
            public bool HasPatrol { get; }
            public float PatrolDistance { get; }
            public float PatrolSpeed { get; }
            public bool SpriteFacesRight { get; }
            public float InteractionDistance { get; }
            public string ClipPath { get; }
            public string ControllerPath { get; }
            public string PrefabPath { get; }
        }
    }
}
