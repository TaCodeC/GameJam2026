using GameJam.Player.Cave;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameJam.UI.Editor
{
    public static class CaveStageTransitionUITool
    {
        private const string CaveScenePath = "Assets/Scenes/Cave.unity";
        private const string RootName = "Cave Stage Transition Canvas";
        private const string ButtonName = "Linterna Stage Button";
        private const string FadeName = "Black Fade";

        [MenuItem("Tools/Game UI/Create Cave Stage Transition Canvas")]
        public static void CreateForActiveScene()
        {
            CreateInOpenScene();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        public static void CreateForCaveScene()
        {
            Scene scene = EditorSceneManager.OpenScene(CaveScenePath);
            CreateInOpenScene();
            EditorSceneManager.SaveScene(scene);
        }

        private static void CreateInOpenScene()
        {
            GameObject root = FindRootObject(RootName);
            if (root == null)
            {
                root = new GameObject(RootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                Undo.RegisterCreatedObjectUndo(root, "Create Cave stage transition canvas");
            }

            SetLayerRecursively(root, LayerMask.NameToLayer("UI"));
            ConfigureCanvas(root);

            Button transitionButton = CreateOrConfigureButton(root.transform);
            CanvasGroup fadeGroup = CreateOrConfigureFade(root.transform);
            fadeGroup.transform.SetAsLastSibling();

            EnsureEventSystem();
            WireTransition(transitionButton, fadeGroup);

            Selection.activeGameObject = root;
            Debug.Log("[CaveStageTransitionUITool] Cave stage transition UI is ready.");
        }

        private static void ConfigureCanvas(GameObject root)
        {
            Canvas canvas = GetOrAdd<Canvas>(root);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 6000;

            CanvasScaler scaler = GetOrAdd<CanvasScaler>(root);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GetOrAdd<GraphicRaycaster>(root);
        }

        private static Button CreateOrConfigureButton(Transform root)
        {
            RectTransform rect = CreateOrFindRect(ButtonName, root);
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-28f, 28f);
            rect.sizeDelta = new Vector2(178f, 52f);

            Image image = GetOrAdd<Image>(rect.gameObject);
            image.color = new Color(0.06f, 0.08f, 0.11f, 0.86f);
            image.raycastTarget = true;

            Button button = GetOrAdd<Button>(rect.gameObject);
            button.targetGraphic = image;

            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.12f, 0.16f, 0.22f, 0.95f);
            colors.pressedColor = new Color(0.02f, 0.03f, 0.05f, 1f);
            colors.disabledColor = new Color(0.06f, 0.08f, 0.11f, 0.35f);
            button.colors = colors;

            RectTransform labelRect = CreateOrFindRect("Label", rect);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = new Vector2(-14f, -6f);

            TMP_Text label = GetOrAdd<TextMeshProUGUI>(labelRect.gameObject);
            label.text = "Linterna";
            label.fontSize = 24f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 12f;
            label.fontSizeMax = 24f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;

            return button;
        }

        private static CanvasGroup CreateOrConfigureFade(Transform root)
        {
            RectTransform rect = CreateOrFindRect(FadeName, root);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            Image image = GetOrAdd<Image>(rect.gameObject);
            image.color = Color.black;
            image.raycastTarget = true;

            CanvasGroup group = GetOrAdd<CanvasGroup>(rect.gameObject);
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            return group;
        }

        private static RectTransform CreateOrFindRect(string name, Transform parent)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
                return existing.GetComponent<RectTransform>();

            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            Undo.RegisterCreatedObjectUndo(gameObject, $"Create {name}");
            gameObject.transform.SetParent(parent, false);
            SetLayerRecursively(gameObject, LayerMask.NameToLayer("UI"));
            return gameObject.GetComponent<RectTransform>();
        }

        private static void WireTransition(Button button, CanvasGroup fadeGroup)
        {
            CavePlayerStageTransition transition = FindFirst<CavePlayerStageTransition>();
            if (transition == null)
            {
                Debug.LogWarning("[CaveStageTransitionUITool] No CavePlayerStageTransition found in the open scene.");
                return;
            }

            SerializedObject serialized = new SerializedObject(transition);
            serialized.FindProperty("_transitionButton").objectReferenceValue = button;
            serialized.FindProperty("_fadeCanvasGroup").objectReferenceValue = fadeGroup;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(transition);
        }

        private static void EnsureEventSystem()
        {
            if (FindFirst<EventSystem>() != null)
                return;

            GameObject eventSystem = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        private static GameObject FindRootObject(string name)
        {
            GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (GameObject root in roots)
            {
                if (root.name == name)
                    return root;
            }

            return null;
        }

        private static T FindFirst<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
#else
            return Object.FindObjectOfType<T>(true);
#endif
        }

        private static T GetOrAdd<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component != null)
                return component;

            return Undo.AddComponent<T>(gameObject);
        }

        private static void SetLayerRecursively(GameObject gameObject, int layer)
        {
            if (layer >= 0)
                gameObject.layer = layer;

            foreach (Transform child in gameObject.transform)
                SetLayerRecursively(child.gameObject, layer);
        }
    }
}
