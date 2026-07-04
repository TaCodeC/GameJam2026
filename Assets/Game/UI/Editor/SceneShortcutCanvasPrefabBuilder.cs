using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SceneShortcutCanvasPrefabBuilder
{
    private const string PrefabPath = "Assets/Game/UI/Prefabs/SceneShortcutCanvas.prefab";
    private const string PlatformScenePath = "Assets/Scenes/Platform.unity";

    [MenuItem("Tools/Game UI/Rebuild Scene Shortcut Canvas Prefab")]
    public static void Build()
    {
        EnsurePlatformInBuildSettings();

        GameObject root = new("SceneShortcutCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SetLayerRecursively(root, LayerMask.NameToLayer("UI"));

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800f, 600f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform group = CreateRect("Shortcut Buttons", root.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(18f, 18f), new Vector2(150f, 78f));
        Button menuButton = CreateButton("Menu Button", group, "Menu", new Vector2(0f, 42f), new Vector2(150f, 34f), out TMP_Text menuLabel);
        Button switchButton = CreateButton("Scene Switch Button", group, "Ir a Platformer", new Vector2(0f, 0f), new Vector2(150f, 34f), out TMP_Text switchLabel);

        SceneShortcutCanvas controller = root.AddComponent<SceneShortcutCanvas>();
        SerializedObject serialized = new(controller);
        serialized.FindProperty("_menuButton").objectReferenceValue = menuButton;
        serialized.FindProperty("_switchSceneButton").objectReferenceValue = switchButton;
        serialized.FindProperty("_menuLabel").objectReferenceValue = menuLabel;
        serialized.FindProperty("_switchSceneLabel").objectReferenceValue = switchLabel;
        serialized.FindProperty("_menuSceneName").stringValue = "Menu";
        serialized.FindProperty("_platformSceneName").stringValue = "Platform";
        serialized.FindProperty("_caveSceneName").stringValue = "Cave";
        serialized.FindProperty("_menuButtonText").stringValue = "Menu";
        serialized.FindProperty("_goToPlatformText").stringValue = "Ir a Platformer";
        serialized.FindProperty("_goToCaveText").stringValue = "Ir a Cueva";
        serialized.FindProperty("_createEventSystemIfMissing").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Scene shortcut canvas prefab rebuilt at {PrefabPath}");
    }

    private static Button CreateButton(string name, Transform parent, string text, Vector2 position, Vector2 size, out TMP_Text label)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), position, size);

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.16f, 0.43f, 0.49f, 0.92f);

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.21f, 0.56f, 0.63f, 1f);
        colors.pressedColor = new Color(0.1f, 0.31f, 0.37f, 1f);
        button.colors = colors;

        RectTransform labelRect = CreateRect("Label", rect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-14f, -6f));
        label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = 16f;
        label.enableAutoSizing = true;
        label.fontSizeMin = 10f;
        label.fontSizeMax = 16f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;

        return button;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        GameObject gameObject = new(name, typeof(RectTransform));
        SetLayerRecursively(gameObject, LayerMask.NameToLayer("UI"));
        gameObject.transform.SetParent(parent, false);

        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static void EnsurePlatformInBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(scene => scene.path == PlatformScenePath))
        {
            return;
        }

        scenes.Add(new EditorBuildSettingsScene(PlatformScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void SetLayerRecursively(GameObject gameObject, int layer)
    {
        if (layer >= 0)
        {
            gameObject.layer = layer;
        }

        foreach (Transform child in gameObject.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}
