using UnityEditor;
using UnityEngine;

namespace GameJam.Creatures.Editor
{
    public static class PlatformNaiaPrefabBuilder
    {
        private const string ArtFolder = "Assets/Game/Art/Platform/Naia";
        private const string PrefabFolder = "Assets/Game/Creatures/Prefabs";
        private const string PrefabPath = PrefabFolder + "/Naia.prefab";

        [MenuItem("GameJam/Creatures/Rebuild Naia Prefab")]
        public static void Build()
        {
            EnsureFolder("Assets/Game", "Creatures");
            EnsureFolder("Assets/Game/Creatures", "Prefabs");

            Sprite eyesOpen = ConfigureAndLoadSprite(ArtFolder + "/01.png");
            Sprite eyesHalfClosed = ConfigureAndLoadSprite(ArtFolder + "/02.png");
            Sprite eyesClosed = ConfigureAndLoadSprite(ArtFolder + "/03.png");
            if (eyesOpen == null || eyesHalfClosed == null || eyesClosed == null)
            {
                Debug.LogError("[PlatformNaiaPrefabBuilder] Faltan uno o más sprites de Naia.");
                return;
            }

            GameObject root = new GameObject("Naia");
            root.transform.localScale = Vector3.one * 0.55f;

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, 5.35f, 0f);

            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = eyesOpen;
            renderer.sortingOrder = 6;

            NaiaBlinkAnimator blinkAnimator = root.AddComponent<NaiaBlinkAnimator>();
            SerializedObject serializedBlink = new SerializedObject(blinkAnimator);
            serializedBlink.FindProperty("_spriteRenderer").objectReferenceValue = renderer;
            serializedBlink.FindProperty("_eyesOpenSprite").objectReferenceValue = eyesOpen;
            serializedBlink.FindProperty("_eyesHalfClosedSprite").objectReferenceValue = eyesHalfClosed;
            serializedBlink.FindProperty("_eyesClosedSprite").objectReferenceValue = eyesClosed;
            serializedBlink.FindProperty("_blinkIntervalRange").vector2Value = new Vector2(3.5f, 7f);
            serializedBlink.FindProperty("_transitionFrameDuration").floatValue = 0.07f;
            serializedBlink.FindProperty("_closedFrameDuration").floatValue = 0.1f;
            serializedBlink.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[PlatformNaiaPrefabBuilder] Naia prefab created at {PrefabPath}.");
        }

        private static Sprite ConfigureAndLoadSprite(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                return null;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static void EnsureFolder(string parent, string folderName)
        {
            string path = $"{parent}/{folderName}";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
