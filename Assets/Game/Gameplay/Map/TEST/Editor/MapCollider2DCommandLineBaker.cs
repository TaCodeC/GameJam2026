using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameJam.Gameplay.Map.Editor
{
    public static class MapCollider2DCommandLineBaker
    {
        private const string SceneArgument = "-mapBakeScene";
        private const string DefaultScenePath = "Assets/Scenes/Cave.unity";

        public static void BakeScene()
        {
            string scenePath = GetCommandLineArgument(SceneArgument, DefaultScenePath);
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            MapCollider2DBaker[] bakers = Object.FindObjectsOfType<MapCollider2DBaker>();

            if (bakers.Length == 0)
            {
                throw new MissingComponentException($"No {nameof(MapCollider2DBaker)} was found in {scenePath}.");
            }

            for (int i = 0; i < bakers.Length; i++)
            {
                MapColliderBakeStats stats = bakers[i].BakeColliders();
                EditorUtility.SetDirty(bakers[i]);
                Debug.Log(
                    $"Baked map colliders for {bakers[i].name}: " +
                    $"{stats.ColliderCount} colliders, {stats.SimplifiedPathPointCount} points.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        public static void ValidateScene()
        {
            string scenePath = GetCommandLineArgument(SceneArgument, DefaultScenePath);
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            GameObject mapObject = RequireObject("MapDiscovery");
            GameObject planeObject = RequireObject("Map Plane");

            MapDiscoverySystem discovery = RequireComponent<MapDiscoverySystem>(mapObject);
            Require(discovery.Definition != null, "MapDiscovery needs a definition.");
            Require(discovery.Definition.WorldPlane == MapWorldPlane.XY, "Cave map must use the XY plane.");
            Require(!discovery.Definition.FlipWorldX, "Cave map should not duplicate correction with Flip World X.");
            Require(!discovery.Definition.FlipWorldY, "Cave map should not duplicate correction with Flip World Y.");
            Require(Quaternion.Angle(mapObject.transform.rotation, Quaternion.identity) < 0.01f, "MapDiscovery must not add an extra scene rotation.");

            Renderer planeRenderer = RequireComponent<Renderer>(planeObject);
            Require(Mathf.Abs(planeRenderer.bounds.size.x - discovery.Definition.WorldSize.x) < 0.01f, "Map Plane width does not match map world size.");
            Require(Mathf.Abs(planeRenderer.bounds.size.y - discovery.Definition.WorldSize.y) < 0.01f, "Map Plane height does not match map world size.");
            RequireTexturePath(
                planeRenderer.sharedMaterial.GetTexture("_BaseMap"),
                "Assets/Game/Gameplay/Map/TEST/Example/MapBeta_1.PNG",
                "Map Plane material must use MapBeta_1.PNG.");

            MapDiscoveryView discoveryView = RequireComponent<MapDiscoveryView>(planeObject);
            SerializedObject serializedView = new SerializedObject(discoveryView);
            RequireTexturePath(
                serializedView.FindProperty("_mapTextureOverride").objectReferenceValue as Texture,
                "Assets/Game/Gameplay/Map/TEST/Example/MapBeta_1.PNG",
                "MapDiscoveryView must reveal MapBeta_1.PNG on the real plane.");

            RequireComponent<MapDebugHud>(mapObject);

            discovery.Initialize();
            Require(discovery.TrackedTransform != null, "MapDiscovery must track the Player transform.");
            Require(discovery.TryWorldToUv(discovery.TrackedTransform.position, out _), "Player must be inside the map bounds.");
            Require(discovery.IsWalkable(discovery.TrackedTransform.position), "Player must start on a walkable pixel of the new mask.");

            MapCollider2DBaker baker = RequireComponent<MapCollider2DBaker>(mapObject);
            MapColliderBakeStats stats = baker.LastBakeStats;
            Require(stats.SourceWidth == discovery.Definition.TraversableMask.width, "Bake stats source width is stale.");
            Require(stats.SourceHeight == discovery.Definition.TraversableMask.height, "Bake stats source height is stale.");
            Require(stats.BlockedCellCount == 681342, $"Expected rebaked blocked cell count 681342, got {stats.BlockedCellCount}.");
            Require(stats.ColliderCount == 1, $"Expected one rebaked collider for the new mask, got {stats.ColliderCount}.");
            Require(stats.SimplifiedPathPointCount == 280, $"Expected 280 collider points after simplification, got {stats.SimplifiedPathPointCount}.");

            Debug.Log("Map scene validation passed.");
        }

        private static GameObject RequireObject(string name)
        {
            GameObject target = GameObject.Find(name);
            Require(target != null, $"Scene is missing GameObject '{name}'.");
            return target;
        }

        private static T RequireComponent<T>(GameObject owner) where T : Component
        {
            T component = owner.GetComponent<T>();
            Require(component != null, $"{owner.name} is missing {typeof(T).Name}.");
            return component;
        }

        private static void RequireTexturePath(Texture texture, string expectedPath, string message)
        {
            Require(texture != null, message);
            string actualPath = AssetDatabase.GetAssetPath(texture);
            Require(actualPath == expectedPath, $"{message} Expected {expectedPath}, got {actualPath}.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new System.InvalidOperationException(message);
            }
        }

        private static string GetCommandLineArgument(string name, string fallback)
        {
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }

            return fallback;
        }
    }
}
