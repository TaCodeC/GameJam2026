using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameJam.Cameras;
using GameJam.Gameplay.PlatformObjectives;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameJam.Gameplay.PlatformObstacles.Editor
{
    public static class PlatformContentUpdateTool
    {
        private const string PlatformScenePath = "Assets/Scenes/Platform.unity";
        private const string UpdatedFondosFolder = "Assets/Game/Art/Platform/New/Fondos";
        private const string UpdatedObjectsFolder = "Assets/Game/Art/Platform/New/Objetos";
        private const string ObjectivePrefabFolder = "Assets/Game/Gameplay/PlatformObjectives/Prefabs";
        private const string UpdatedTiles2Path = UpdatedFondosFolder + "/Tiles2 nuevos.png";

        private readonly struct SpriteSliceDefinition
        {
            public SpriteSliceDefinition(string name, RectInt topLeftRect)
            {
                Name = name;
                TopLeftRect = topLeftRect;
            }

            public string Name { get; }
            public RectInt TopLeftRect { get; }
        }

        private readonly struct CollectibleDefinition
        {
            public CollectibleDefinition(
                string prefabName,
                string itemId,
                PlatformObjectiveItemType itemType,
                string texturePath,
                string spriteName)
            {
                PrefabName = prefabName;
                ItemId = itemId;
                ItemType = itemType;
                TexturePath = texturePath;
                SpriteName = spriteName;
            }

            public string PrefabName { get; }
            public string ItemId { get; }
            public PlatformObjectiveItemType ItemType { get; }
            public string TexturePath { get; }
            public string SpriteName { get; }
            public string PrefabPath => $"{ObjectivePrefabFolder}/{PrefabName}.prefab";
        }

        private static readonly CollectibleDefinition[] Collectibles =
        {
            new("Rama1Collectible", "Rama1", PlatformObjectiveItemType.Branch, UpdatedObjectsFolder + "/Rama1 copy.png", "Objective_Rama_1"),
            new("Rama2Collectible", "Rama2", PlatformObjectiveItemType.Branch, UpdatedObjectsFolder + "/Rama2 copy.png", "Objective_Rama_2"),
            new("Palma1Collectible", "Palma1", PlatformObjectiveItemType.PalmLeaf, UpdatedObjectsFolder + "/Palma1 copy.png", "Objective_Palma_1"),
            new("Palma2Collectible", "Palma2", PlatformObjectiveItemType.PalmLeaf, UpdatedObjectsFolder + "/Palma2 copy.png", "Objective_Palma_2"),
            new("Piedra1Collectible", "Piedra1", PlatformObjectiveItemType.Rock, UpdatedObjectsFolder + "/Piedra1 copy.png", "Objective_Piedra_1"),
            new("Piedra2Collectible", "Piedra2", PlatformObjectiveItemType.Rock, UpdatedObjectsFolder + "/Piedra2 copy.png", "Objective_Piedra_2"),
            new("Piedra3Collectible", "Piedra3", PlatformObjectiveItemType.Rock, UpdatedObjectsFolder + "/Piedra3 copy.png", "Objective_Piedra_3"),
            new("Piedra4Collectible", "Piedra4", PlatformObjectiveItemType.Rock, UpdatedObjectsFolder + "/Piedra4 copy.png", "Objective_Piedra_4"),
            new("Piedra5Collectible", "Piedra5", PlatformObjectiveItemType.Rock, UpdatedObjectsFolder + "/Piedra5 copy.png", "Objective_Piedra_5"),
            new("MontonDeTierraCollectible", "MontonDeTierra", PlatformObjectiveItemType.Soil, UpdatedObjectsFolder + "/Montón de tierra copy.png", "Objective_Tierra_1")
        };

        [MenuItem("GameJam/Platform/Apply Updated Tiles, Parallax and Objectives")]
        public static void ApplyAll()
        {
            PlatformStaticTileSpawnerEditor.CreateDefaultSpawnerPrefab();
            BuildObjectivePrefabs();
            UpdatePlatformScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PlatformContentUpdateTool] Updated tiles, no-collider backgrounds, parallax and objectives are ready.");
        }

        public static void ConfigureUpdatedPlatformArt()
        {
            ConfigureAtlas(
                UpdatedTiles2Path,
                new[]
                {
                    Slice("Tiles2Nuevos_Wall_01", 0, 29, 675, 553),
                    Slice("Tiles2Nuevos_Wall_02", 42, 614, 704, 466),
                    Slice("Tiles2Nuevos_Log_01", 755, 149, 795, 142),
                    Slice("Tiles2Nuevos_Log_02", 746, 396, 887, 142),
                    Slice("Tiles2Nuevos_Log_03", 837, 614, 705, 172),
                    Slice("Tiles2Nuevos_Log_04", 845, 895, 705, 164),
                    Slice("Tiles2Nuevos_Tree_01", 1722, 0, 634, 631),
                    Slice("Tiles2Nuevos_Wall_03", 1730, 712, 508, 336),
                    Slice("Tiles2Nuevos_Tree_02", 2462, 0, 588, 726),
                    Slice("Tiles2Nuevos_Tree_03", 3122, 29, 588, 734)
                });

            ConfigureTightSprite(UpdatedFondosFolder + "/Charco.png", "Platform_Charco");
            ConfigureTightSprite(UpdatedFondosFolder + "/Piedra.png", "Platform_Piedra");
            ConfigureTightSprite(UpdatedFondosFolder + "/Vegetación 1.png", "Platform_Vegetacion_1");
            ConfigureTightSprite(UpdatedFondosFolder + "/Vegetación 2.png", "Platform_Vegetacion_2");
            ConfigureTightSprite(UpdatedFondosFolder + "/Vegetación 3.png", "Platform_Vegetacion_3");
            ConfigureTightSprite(UpdatedFondosFolder + "/Vegetación 4.png", "Platform_Vegetacion_4");
            ConfigureTightSprite(UpdatedFondosFolder + "/Vegetación 5.png", "Platform_Vegetacion_5");
            ConfigureTightSprite(UpdatedFondosFolder + "/fondo arbol.png", "Platform_Fondo_Arbol_1");
            ConfigureTightSprite(UpdatedFondosFolder + "/fondo arbol2.png", "Platform_Fondo_Arbol_2");
            ConfigureTightSprite(UpdatedFondosFolder + "/fondo roca.png", "Platform_Fondo_Roca_1");
            ConfigureTightSprite(UpdatedFondosFolder + "/fondo roca 2.png", "Platform_Fondo_Roca_2");
            ConfigureTightSprite(UpdatedFondosFolder + "/fondo vegetacion 3.png", "Platform_Fondo_Vegetacion_1");

            foreach (CollectibleDefinition collectible in Collectibles)
                ConfigureTightSprite(collectible.TexturePath, collectible.SpriteName);

            ConfigureTightSprite(UpdatedObjectsFolder + "/CasitaAlux.PNG", "Objective_Casita_Alux");
        }

        private static SpriteSliceDefinition Slice(string name, int x, int y, int width, int height)
        {
            return new SpriteSliceDefinition(name, new RectInt(x, y, width, height));
        }

        private static void ConfigureAtlas(string assetPath, IReadOnlyList<SpriteSliceDefinition> slices)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[PlatformContentUpdateTool] Texture not found: {assetPath}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Compressed;
            SetPlatformMaxSize(importer, "DefaultTexturePlatform", 4096);
            SetPlatformMaxSize(importer, "Standalone", 4096);
            SetPlatformMaxSize(importer, "Android", 2048);
            SetPlatformMaxSize(importer, "iPhone", 2048);
            SetPlatformMaxSize(importer, "WebGL", 2048);

            importer.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
            if (sourceWidth <= 0 || sourceHeight <= 0)
                return;

            SpriteRect[] spriteRects = new SpriteRect[slices.Count];
            for (int i = 0; i < slices.Count; i++)
            {
                SpriteSliceDefinition slice = slices[i];
                RectInt source = slice.TopLeftRect;
                spriteRects[i] = new SpriteRect
                {
                    name = slice.Name,
                    spriteID = GUID.Generate(),
                    rect = new Rect(source.x, sourceHeight - source.yMax, source.width, source.height),
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                };
            }

            ApplySpriteRects(importer, spriteRects);
        }

        private static void ConfigureTightSprite(string assetPath, string spriteName)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[PlatformContentUpdateTool] Texture not found: {assetPath}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture == null)
                return;

            Rect tightRect = FindAlphaBounds(texture, 8, 2);
            importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            importer.spriteImportMode = SpriteImportMode.Multiple;

            SpriteRect spriteRect = new SpriteRect
            {
                name = spriteName,
                spriteID = GUID.Generate(),
                rect = tightRect,
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f)
            };

            ApplySpriteRects(importer, new[] { spriteRect });
            importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        private static Rect FindAlphaBounds(Texture2D texture, byte alphaThreshold, int padding)
        {
            Color32[] pixels = texture.GetPixels32();
            int minX = texture.width;
            int minY = texture.height;
            int maxX = -1;
            int maxY = -1;

            for (int y = 0; y < texture.height; y++)
            {
                int row = y * texture.width;
                for (int x = 0; x < texture.width; x++)
                {
                    if (pixels[row + x].a <= alphaThreshold)
                        continue;

                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                }
            }

            if (maxX < minX || maxY < minY)
                return new Rect(0f, 0f, texture.width, texture.height);

            minX = Mathf.Max(0, minX - padding);
            minY = Mathf.Max(0, minY - padding);
            maxX = Mathf.Min(texture.width - 1, maxX + padding);
            maxY = Mathf.Min(texture.height - 1, maxY + padding);
            return new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        private static void ApplySpriteRects(TextureImporter importer, SpriteRect[] spriteRects)
        {
            SpriteDataProviderFactories factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            Dictionary<string, GUID> existingIds = provider.GetSpriteRects()
                .GroupBy(rect => rect.name)
                .ToDictionary(group => group.Key, group => group.First().spriteID);
            foreach (SpriteRect spriteRect in spriteRects)
            {
                if (existingIds.TryGetValue(spriteRect.name, out GUID existingId))
                    spriteRect.spriteID = existingId;
            }

            provider.SetSpriteRects(spriteRects);

            ISpriteNameFileIdDataProvider nameProvider = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            nameProvider.SetNameFileIdPairs(spriteRects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
        }

        private static void SetPlatformMaxSize(TextureImporter importer, string platformName, int maxSize)
        {
            TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platformName);
            settings.name = platformName;
            settings.maxTextureSize = maxSize;
            settings.textureCompression = TextureImporterCompression.Compressed;
            importer.SetPlatformTextureSettings(settings);
        }

        private static void BuildObjectivePrefabs()
        {
            EnsureFolder(ObjectivePrefabFolder);
            foreach (CollectibleDefinition collectible in Collectibles)
                BuildCollectiblePrefab(collectible);

            UpdateHousePrefab();
        }

        private static void BuildCollectiblePrefab(CollectibleDefinition definition)
        {
            Sprite sprite = LoadSprite(definition.TexturePath, definition.SpriteName);
            if (sprite == null)
                return;

            bool prefabExists = AssetDatabase.LoadAssetAtPath<GameObject>(definition.PrefabPath) != null;
            GameObject root = prefabExists
                ? PrefabUtility.LoadPrefabContents(definition.PrefabPath)
                : new GameObject(definition.PrefabName);

            try
            {
                root.name = definition.PrefabName;
                if (!prefabExists)
                    root.transform.localScale = Vector3.one * 0.16f;

                SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
                if (renderer == null)
                    renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = 4;

                BoxCollider2D trigger = root.GetComponent<BoxCollider2D>();
                if (trigger == null)
                    trigger = root.AddComponent<BoxCollider2D>();
                trigger.isTrigger = true;
                if (!prefabExists)
                {
                    trigger.offset = sprite.bounds.center;
                    trigger.size = new Vector2(
                        Mathf.Max(0.5f, sprite.bounds.size.x * 0.85f),
                        Mathf.Max(0.5f, sprite.bounds.size.y * 0.85f));
                }

                PlatformObjectiveCollectible collectible = root.GetComponent<PlatformObjectiveCollectible>();
                if (collectible == null)
                    collectible = root.AddComponent<PlatformObjectiveCollectible>();
                SerializedObject serializedCollectible = new SerializedObject(collectible);
                serializedCollectible.FindProperty("_itemId").stringValue = definition.ItemId;
                serializedCollectible.FindProperty("_itemType").enumValueIndex = (int)definition.ItemType;
                serializedCollectible.FindProperty("_disableOnCollected").boolValue = true;
                serializedCollectible.FindProperty("_visualRoot").objectReferenceValue = root;
                serializedCollectible.FindProperty("_playerTag").stringValue = "Player";
                serializedCollectible.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, definition.PrefabPath);
            }
            finally
            {
                if (prefabExists)
                    PrefabUtility.UnloadPrefabContents(root);
                else
                    UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void UpdateHousePrefab()
        {
            string prefabPath = ObjectivePrefabFolder + "/CasitaAluxGate.prefab";
            if (!File.Exists(prefabPath))
                return;

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
                Sprite houseSprite = LoadSprite(UpdatedObjectsFolder + "/CasitaAlux.PNG", "Objective_Casita_Alux");
                if (renderer != null && houseSprite != null)
                    renderer.sprite = houseSprite;

                PlatformAluxHouseGate gate = root.GetComponent<PlatformAluxHouseGate>();
                if (gate != null)
                {
                    SerializedObject serializedGate = new SerializedObject(gate);
                    serializedGate.FindProperty("_requiredBranches").intValue = 2;
                    serializedGate.FindProperty("_requiredPalmLeaves").intValue = 2;
                    serializedGate.FindProperty("_requiredRocks").intValue = 5;
                    serializedGate.FindProperty("_requiredSoil").intValue = 1;
                    serializedGate.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Sprite LoadSprite(string assetPath, string spriteName)
        {
            return AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<Sprite>()
                .FirstOrDefault(sprite => sprite.name == spriteName);
        }

        private static void UpdatePlatformScene()
        {
            Scene scene = EditorSceneManager.OpenScene(PlatformScenePath, OpenSceneMode.Single);
            ConfigureParallaxObjects(scene);
            EnsureObjectiveInstances(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureParallaxObjects(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform current in root.GetComponentsInChildren<Transform>(true))
                {
                    if (!current.name.StartsWith("BG_Parallax", StringComparison.Ordinal))
                        continue;

                    BackgroundParallaxFollow parallax = current.GetComponent<BackgroundParallaxFollow>()
                        ?? current.gameObject.AddComponent<BackgroundParallaxFollow>();
                    SerializedObject serializedParallax = new SerializedObject(parallax);
                    serializedParallax.FindProperty("_autoFindPlayer").boolValue = true;
                    serializedParallax.FindProperty("_followFactor").floatValue = 0.2f;
                    serializedParallax.FindProperty("_followX").boolValue = true;
                    serializedParallax.FindProperty("_followY").boolValue = true;
                    serializedParallax.ApplyModifiedPropertiesWithoutUndo();

                    foreach (Collider collider in current.GetComponents<Collider>())
                        UnityEngine.Object.DestroyImmediate(collider);

                    foreach (Collider2D collider in current.GetComponents<Collider2D>())
                        UnityEngine.Object.DestroyImmediate(collider);
                }
            }
        }

        private static void EnsureObjectiveInstances(Scene scene)
        {
            EnsureObjectiveInstance(scene, "Rama2Collectible", "Rama1Collectible", new Vector3(2.2f, 0f, 0f));
            EnsureObjectiveInstance(scene, "Palma2Collectible", "Palma1Collectible", new Vector3(2.2f, 0f, 0f));
            EnsureObjectiveInstance(scene, "Piedra2Collectible", "Piedra1Collectible", new Vector3(2f, 0f, 0f));
            EnsureObjectiveInstance(scene, "Piedra3Collectible", "Piedra1Collectible", new Vector3(4f, 0f, 0f));
            EnsureObjectiveInstance(scene, "Piedra5Collectible", "Piedra4Collectible", new Vector3(2f, 0f, 0f));
        }

        private static void EnsureObjectiveInstance(Scene scene, string prefabName, string anchorName, Vector3 offset)
        {
            if (FindInScene(scene, prefabName) != null)
                return;

            GameObject anchor = FindInScene(scene, anchorName);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ObjectivePrefabFolder}/{prefabName}.prefab");
            if (anchor == null || prefab == null)
            {
                Debug.LogWarning($"[PlatformContentUpdateTool] Could not place {prefabName}; anchor or prefab is missing.");
                return;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            instance.name = prefabName;
            instance.transform.position = anchor.transform.position + offset;
            instance.transform.rotation = anchor.transform.rotation;
        }

        private static GameObject FindInScene(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform current in root.GetComponentsInChildren<Transform>(true))
                {
                    if (current.name == objectName)
                        return current.gameObject;
                }
            }

            return null;
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] segments = folderPath.Split('/');
            string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = $"{current}/{segments[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segments[i]);
                current = next;
            }
        }
    }
}
