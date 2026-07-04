using System;
using System.Collections.Generic;
using GameJam.Gameplay.PlatformObstacles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameJam.Gameplay.PlatformObstacles.Editor
{
    [CustomEditor(typeof(PlatformStaticTileSpawner))]
    public sealed class PlatformStaticTileSpawnerEditor : UnityEditor.Editor
    {
        private const string PrefabFolder = "Assets/Game/Gameplay/PlatformObstacles/Prefabs";
        private const string PrefabPath = PrefabFolder + "/PlatformStaticTileSpawner.prefab";
        private const string Tiles1Path = "Assets/Game/Art/Platform/Tiles1.png";
        private const string Tiles2Path = "Assets/Game/Art/Platform/Tiles2.png";
        private const string Vegetacion1Path = "Assets/Game/Art/Platform/Vegetation/Vegetacion1.png";
        private const string Vegetacion3Path = "Assets/Game/Art/Platform/Vegetation/Vegetacion3.png";
        private const string Vegetacion4Path = "Assets/Game/Art/Platform/Vegetation/Vegetacion4.png";
        private const string Vegetacion5Path = "Assets/Game/Art/Platform/Vegetation/Vegetacion5.png";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate Static Tiles", GUILayout.Height(32f)))
                {
                    foreach (UnityEngine.Object targetObject in targets)
                        StaticTileSpawnerBaker.Generate((PlatformStaticTileSpawner)targetObject);
                }

                if (GUILayout.Button("Clear Generated", GUILayout.Height(32f)))
                {
                    foreach (UnityEngine.Object targetObject in targets)
                        StaticTileSpawnerBaker.Clear((PlatformStaticTileSpawner)targetObject);
                }
            }

            if (GUILayout.Button("Rebake Generated Colliders", GUILayout.Height(28f)))
            {
                foreach (UnityEngine.Object targetObject in targets)
                    StaticTileSpawnerBaker.RebakeColliders((PlatformStaticTileSpawner)targetObject);
            }

            EditorGUILayout.HelpBox(
                "Generate crea GameObjects reales como hijos del spawner. Rebake Colliders conserva posiciones y rehace solo los colliders. No hay spawn en runtime; guarda la escena o el prefab despues.",
                MessageType.Info);
        }

        [MenuItem("GameJam/Platform Obstacles/Create Default Static Tile Spawner Prefab")]
        public static void CreateDefaultSpawnerPrefab()
        {
            EnsureFolder("Assets/Game/Gameplay", "PlatformObstacles");
            EnsureFolder("Assets/Game/Gameplay/PlatformObstacles", "Prefabs");

            GameObject root = new GameObject("PlatformStaticTileSpawner");
            PlatformStaticTileSpawner spawner = root.AddComponent<PlatformStaticTileSpawner>();

            PlatformStaticTileSpawnZone smallRocks = CreateZone(root.transform, "Zona - Piedras Chicas", new Vector3(-9f, 0f, 0f), new Vector2(8f, 2.25f));
            PlatformStaticTileSpawnZone bigRocks = CreateZone(root.transform, "Zona - Piedras Grandes", new Vector3(0f, 0f, 0f), new Vector2(9f, 2.6f));
            PlatformStaticTileSpawnZone wood = CreateZone(root.transform, "Zona - Troncos", new Vector3(9.5f, 0f, 0f), new Vector2(8f, 2.2f));
            PlatformStaticTileSpawnZone trees = CreateZone(root.transform, "Zona - Arboles Secos", new Vector3(0f, 4f, 0f), new Vector2(12f, 3.2f));
            PlatformStaticTileSpawnZone vegetation = CreateZone(root.transform, "Zona - Vegetacion", new Vector3(9.5f, 4f, 0f), new Vector2(12f, 3.4f));

            spawner.EditorConfigure(
                new[]
                {
                    new PlatformStaticTileSpawner.SpawnGroup(
                        "Piedras Chicas",
                        "Obstacles",
                        smallRocks,
                        LoadSprites(Tiles1Path, "Tiles1_0", "Tiles1_5", "Tiles1_6"),
                        10,
                        0.9f,
                        new Vector2(0.85f, 1.15f),
                        new Vector2(-5f, 5f),
                        Vector2.zero,
                        new Vector2(-0.03f, 0.03f),
                        2,
                        GeneratedTileColliderMode.SpritePhysicsShape),
                    new PlatformStaticTileSpawner.SpawnGroup(
                        "Piedras Grandes",
                        "Obstacles",
                        bigRocks,
                        LoadSprites(Tiles1Path, "Tiles1_1", "Tiles1_2", "Tiles1_3", Tiles2Path, "Tiles2_1"),
                        5,
                        2.8f,
                        new Vector2(0.85f, 1.1f),
                        new Vector2(-3f, 3f),
                        Vector2.zero,
                        new Vector2(-0.03f, 0.03f),
                        2,
                        GeneratedTileColliderMode.SpritePhysicsShape),
                    new PlatformStaticTileSpawner.SpawnGroup(
                        "Troncos",
                        "Obstacles",
                        wood,
                        LoadSprites(Tiles2Path, "Tiles2_2"),
                        4,
                        2.8f,
                        new Vector2(0.9f, 1.1f),
                        new Vector2(-4f, 4f),
                        Vector2.zero,
                        new Vector2(-0.03f, 0.03f),
                        3,
                        GeneratedTileColliderMode.SpritePhysicsShape),
                    new PlatformStaticTileSpawner.SpawnGroup(
                        "Arboles Secos",
                        "Obstacles",
                        trees,
                        LoadSprites(Tiles2Path, "Tiles2_4", Tiles1Path, "Tiles1_4"),
                        4,
                        3.2f,
                        new Vector2(0.85f, 1.1f),
                        new Vector2(-2f, 2f),
                        Vector2.zero,
                        new Vector2(-0.03f, 0.03f),
                        1,
                        GeneratedTileColliderMode.SpritePhysicsShape),
                    new PlatformStaticTileSpawner.SpawnGroup(
                        "Vegetacion",
                        "Vegetation No Colliders",
                        vegetation,
                        LoadSprites(
                            Vegetacion1Path, "Vegetacion1",
                            Vegetacion3Path, "Vegetacion3",
                            Vegetacion4Path, "Vegetacion4",
                            Vegetacion5Path, "Vegetacion5"),
                        12,
                        1.6f,
                        new Vector2(0.22f, 0.48f),
                        new Vector2(-3f, 3f),
                        Vector2.zero,
                        new Vector2(-0.08f, 0.08f),
                        0,
                        GeneratedTileColliderMode.None)
                });

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Platform static tile spawner prefab created at {PrefabPath}");
        }

        private static PlatformStaticTileSpawnZone CreateZone(Transform parent, string label, Vector3 localPosition, Vector2 size)
        {
            GameObject zoneObject = new GameObject(label);
            zoneObject.transform.SetParent(parent, false);
            zoneObject.transform.localPosition = localPosition;

            PlatformStaticTileSpawnZone zone = zoneObject.AddComponent<PlatformStaticTileSpawnZone>();
            zone.EditorConfigure(
                label.Replace("Zona - ", string.Empty),
                size,
                new Color(1f, 0f, 0f, 0.14f),
                new Color(1f, 0f, 0f, 0.9f));
            return zone;
        }

        private static Sprite[] LoadSprites(params string[] pathAndSpriteNames)
        {
            List<Sprite> sprites = new List<Sprite>();
            string currentPath = null;

            foreach (string value in pathAndSpriteNames)
            {
                if (value.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    currentPath = value;
                    continue;
                }

                Sprite sprite = LoadSprite(currentPath, value);
                if (sprite != null)
                    sprites.Add(sprite);
            }

            return sprites.ToArray();
        }

        private static Sprite LoadSprite(string assetPath, string spriteName)
        {
            if (string.IsNullOrEmpty(assetPath))
                return null;

            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (UnityEngine.Object asset in assets)
            {
                Sprite sprite = asset as Sprite;
                if (sprite != null && sprite.name == spriteName)
                    return sprite;
            }

            Sprite mainSprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (mainSprite != null && (mainSprite.name == spriteName || string.IsNullOrEmpty(spriteName)))
                return mainSprite;

            Debug.LogWarning($"Sprite {spriteName} was not found in {assetPath}.");
            return null;
        }

        private static void EnsureFolder(string parent, string folderName)
        {
            string path = $"{parent}/{folderName}";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, folderName);
        }
    }

    internal static class StaticTileSpawnerBaker
    {
        public static void Generate(PlatformStaticTileSpawner spawner)
        {
            if (spawner == null)
                return;

            Clear(spawner);

            Transform generatedRoot = CreateGeneratedRoot(spawner);
            int seed = spawner.UseRandomSeed ? Environment.TickCount : spawner.Seed;
            System.Random random = new System.Random(seed);
            Dictionary<string, Transform> generatedParents = new Dictionary<string, Transform>();

            foreach (PlatformStaticTileSpawner.SpawnGroup group in spawner.Groups)
            {
                if (group == null || !group.Enabled || group.Zone == null || group.Count <= 0)
                    continue;

                Transform generatedParent = GetOrCreateGeneratedParent(generatedRoot, group.GeneratedParentName, generatedParents);
                Transform groupRoot = CreateGroupRoot(generatedParent, group.Label);
                List<Vector3> acceptedPositions = new List<Vector3>();

                for (int i = 0; i < group.Count; i++)
                {
                    Sprite sprite = group.GetRandomSprite(random);
                    if (sprite == null)
                        continue;

                    Vector3 position = FindPosition(group, random, acceptedPositions);
                    acceptedPositions.Add(position);

                    GameObject tile = CreateTileObject(group, sprite, position, i, random, spawner.MarkGeneratedObjectsStatic);
                    Undo.SetTransformParent(tile.transform, groupRoot, "Parent static tile");
                }
            }

            MarkDirty(spawner);
        }

        public static void Clear(PlatformStaticTileSpawner spawner)
        {
            if (spawner == null)
                return;

            Transform root = spawner.transform.Find(spawner.GeneratedRootName);
            if (root == null)
                return;

            Undo.DestroyObjectImmediate(root.gameObject);
            MarkDirty(spawner);
        }

        public static void RebakeColliders(PlatformStaticTileSpawner spawner)
        {
            if (spawner == null)
                return;

            Transform generatedRoot = spawner.transform.Find(spawner.GeneratedRootName);
            if (generatedRoot == null)
                return;

            Dictionary<string, PlatformStaticTileSpawner.SpawnGroup> groupsByLabel =
                new Dictionary<string, PlatformStaticTileSpawner.SpawnGroup>();
            foreach (PlatformStaticTileSpawner.SpawnGroup group in spawner.Groups)
            {
                if (group != null && !groupsByLabel.ContainsKey(group.Label))
                    groupsByLabel.Add(group.Label, group);
            }

            for (int i = 0; i < generatedRoot.childCount; i++)
            {
                Transform child = generatedRoot.GetChild(i);
                RebakeCollidersRecursive(child, groupsByLabel);
            }

            MarkDirty(spawner);
        }

        private static Transform CreateGeneratedRoot(PlatformStaticTileSpawner spawner)
        {
            GameObject root = new GameObject(spawner.GeneratedRootName);
            Undo.RegisterCreatedObjectUndo(root, "Create generated static tiles root");
            Undo.SetTransformParent(root.transform, spawner.transform, "Parent generated static tiles root");
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            return root.transform;
        }

        private static Transform GetOrCreateGeneratedParent(
            Transform generatedRoot,
            string parentName,
            Dictionary<string, Transform> generatedParents)
        {
            string key = string.IsNullOrWhiteSpace(parentName) ? "Obstacles" : parentName;
            if (generatedParents.TryGetValue(key, out Transform existingParent))
                return existingParent;

            Transform foundParent = generatedRoot.Find(key);
            if (foundParent != null)
            {
                generatedParents.Add(key, foundParent);
                return foundParent;
            }

            Transform createdParent = CreateGroupRoot(generatedRoot, key);
            generatedParents.Add(key, createdParent);
            return createdParent;
        }

        private static Transform CreateGroupRoot(Transform parent, string label)
        {
            GameObject root = new GameObject(label);
            Undo.RegisterCreatedObjectUndo(root, "Create static tile group");
            Undo.SetTransformParent(root.transform, parent, "Parent static tile group");
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            return root.transform;
        }

        private static GameObject CreateTileObject(
            PlatformStaticTileSpawner.SpawnGroup group,
            Sprite sprite,
            Vector3 position,
            int index,
            System.Random random,
            bool markStatic)
        {
            GameObject tile = new GameObject($"{group.Label}_{index + 1:00}_{sprite.name}");
            Undo.RegisterCreatedObjectUndo(tile, "Create static tile");

            if (markStatic)
                GameObjectUtility.SetStaticEditorFlags(tile, StaticEditorFlags.BatchingStatic);

            float scale = NextRange(random, group.UniformScaleRange.x, group.UniformScaleRange.y);
            float rotation = NextRange(random, group.ZRotationRange.x, group.ZRotationRange.y);
            tile.transform.position = position;
            tile.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            tile.transform.localScale = Vector3.one * scale;

            SpriteRenderer renderer = Undo.AddComponent<SpriteRenderer>(tile);
            renderer.sprite = sprite;
            renderer.sortingOrder = group.SortingOrder;

            AddBakedCollider(tile, sprite, group.ColliderMode);

            return tile;
        }

        private static void RebakeCollidersRecursive(
            Transform current,
            Dictionary<string, PlatformStaticTileSpawner.SpawnGroup> groupsByLabel)
        {
            if (groupsByLabel.TryGetValue(current.name, out PlatformStaticTileSpawner.SpawnGroup group))
            {
                RebakeColliders(current, group);
                return;
            }

            for (int i = 0; i < current.childCount; i++)
                RebakeCollidersRecursive(current.GetChild(i), groupsByLabel);
        }

        private static void RebakeColliders(Transform groupRoot, PlatformStaticTileSpawner.SpawnGroup group)
        {
            for (int i = 0; i < groupRoot.childCount; i++)
            {
                Transform tile = groupRoot.GetChild(i);
                SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
                if (renderer == null || renderer.sprite == null)
                    continue;

                RemoveExistingColliders(tile.gameObject);
                AddBakedCollider(tile.gameObject, renderer.sprite, group.ColliderMode);
            }
        }

        private static void RemoveExistingColliders(GameObject tile)
        {
            Collider2D[] colliders = tile.GetComponents<Collider2D>();
            foreach (Collider2D collider in colliders)
                Undo.DestroyObjectImmediate(collider);
        }

        private static void AddBakedCollider(GameObject tile, Sprite sprite, GeneratedTileColliderMode colliderMode)
        {
            if (tile == null || sprite == null || colliderMode == GeneratedTileColliderMode.None)
                return;

            if (colliderMode == GeneratedTileColliderMode.SpritePhysicsShape && TryAddSpriteShapeCollider(tile, sprite))
                return;

            AddBoxCollider(tile, sprite);
        }

        private static bool TryAddSpriteShapeCollider(GameObject tile, Sprite sprite)
        {
            int shapeCount = sprite.GetPhysicsShapeCount();
            if (shapeCount <= 0)
                return false;

            PolygonCollider2D collider = Undo.AddComponent<PolygonCollider2D>(tile);
            List<Vector2> path = new List<Vector2>();
            int validPathCount = 0;
            collider.pathCount = shapeCount;

            for (int shapeIndex = 0; shapeIndex < shapeCount; shapeIndex++)
            {
                path.Clear();
                sprite.GetPhysicsShape(shapeIndex, path);
                if (path.Count < 3)
                    continue;

                collider.SetPath(validPathCount, path);
                validPathCount++;
            }

            if (validPathCount == 0)
            {
                Undo.DestroyObjectImmediate(collider);
                return false;
            }

            collider.pathCount = validPathCount;
            return true;
        }

        private static void AddBoxCollider(GameObject tile, Sprite sprite)
        {
            BoxCollider2D collider = Undo.AddComponent<BoxCollider2D>(tile);
            collider.size = sprite.bounds.size;
            collider.offset = sprite.bounds.center;
        }

        private static Vector3 FindPosition(
            PlatformStaticTileSpawner.SpawnGroup group,
            System.Random random,
            List<Vector3> acceptedPositions)
        {
            Vector3 position = group.Zone.GetRandomWorldPoint(random);

            for (int attempt = 0; attempt < group.MaxAttemptsPerItem; attempt++)
            {
                position = group.Zone.GetRandomWorldPoint(random);
                position.y += NextRange(random, group.YOffsetRange.x, group.YOffsetRange.y);
                position.z += NextRange(random, group.ZOffsetRange.x, group.ZOffsetRange.y);

                if (IsFarEnough(position, acceptedPositions, group.MinSpacing))
                    return position;
            }

            return position;
        }

        private static bool IsFarEnough(Vector3 position, List<Vector3> acceptedPositions, float minSpacing)
        {
            if (minSpacing <= 0f)
                return true;

            float minSqrDistance = minSpacing * minSpacing;
            foreach (Vector3 acceptedPosition in acceptedPositions)
            {
                if ((acceptedPosition - position).sqrMagnitude < minSqrDistance)
                    return false;
            }

            return true;
        }

        private static void MarkDirty(PlatformStaticTileSpawner spawner)
        {
            EditorUtility.SetDirty(spawner);
            PrefabUtility.RecordPrefabInstancePropertyModifications(spawner);

            Scene scene = spawner.gameObject.scene;
            if (scene.IsValid())
                EditorSceneManager.MarkSceneDirty(scene);
        }

        private static float NextRange(System.Random random, float min, float max)
        {
            if (max < min)
            {
                float swap = min;
                min = max;
                max = swap;
            }

            return Mathf.Lerp(min, max, (float)random.NextDouble());
        }
    }
}
