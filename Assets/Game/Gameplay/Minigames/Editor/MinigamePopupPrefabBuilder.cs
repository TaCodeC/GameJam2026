using System.IO;
using GameJam.Gameplay.Minigames;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GameJam.Gameplay.Minigames.Editor
{
    public static class MinigamePopupPrefabBuilder
    {
        private const string PrefabFolder = "Assets/Game/Gameplay/Minigames/Prefabs";
        private const string PrefabPath = PrefabFolder + "/MinigamePopupCanvas.prefab";

        [MenuItem("Game Jam/Minigames/Create Popup Canvas Prefab")]
        public static void CreatePopupCanvasPrefab()
        {
            Directory.CreateDirectory(PrefabFolder);

            GameObject root = CreateRootCanvas();
            MinigamePopupCanvas popupCanvas = root.GetComponent<MinigamePopupCanvas>();

            RectTransform testButtons = CreatePanel("Test Buttons", root.transform, AnchorPreset.TopRight, new Vector2(-24f, -24f), new Vector2(360f, 52f));
            Button dragButton = CreateButton("Open Drag Drop", testButtons, "Probar drag", new Vector2(-90f, 0f), new Vector2(168f, 44f));
            Button measurementButton = CreateButton("Open Measurement", testButtons, "Probar medicion", new Vector2(-270f, 0f), new Vector2(168f, 44f));
            testButtons.gameObject.SetActive(false);

            RectTransform popupRoot = CreatePanel("Popup Root", root.transform, AnchorPreset.Stretch, Vector2.zero, Vector2.zero);
            CanvasGroup popupGroup = popupRoot.gameObject.AddComponent<CanvasGroup>();
            Image dim = popupRoot.gameObject.AddComponent<Image>();
            dim.color = new Color(0.02f, 0.04f, 0.07f, 0.74f);

            RectTransform window = CreatePanel("Window", popupRoot, AnchorPreset.Center, Vector2.zero, new Vector2(980f, 620f));
            Image windowImage = window.gameObject.AddComponent<Image>();
            windowImage.color = new Color(0.08f, 0.13f, 0.17f, 0.96f);

            Button closeButton = CreateButton("Close Button", window, "X", new Vector2(458f, 276f), new Vector2(42f, 42f));

            RectTransform dragPanel = CreateDragDropPanel(window);
            RectTransform measurementPanel = CreateMeasurementPanel(window);
            measurementPanel.gameObject.SetActive(false);

            ConfigurePopupCanvas(popupCanvas, popupRoot.gameObject, popupGroup, closeButton, dragButton, measurementButton, dragPanel.gameObject, measurementPanel.gameObject);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Minigames] Popup prefab created at {PrefabPath}.");
        }

        private static GameObject CreateRootCanvas()
        {
            GameObject root = new GameObject("MinigamePopupCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(MinigamePopupCanvas));
            SetLayerRecursively(root, LayerMask.NameToLayer("UI"));

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform rectTransform = root.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;

            return root;
        }

        private static RectTransform CreateDragDropPanel(RectTransform parent)
        {
            RectTransform panel = CreatePanel("DragDrop Panel", parent, AnchorPreset.Stretch, Vector2.zero, new Vector2(-80f, -92f));
            DragDropMinigame minigame = panel.gameObject.AddComponent<DragDropMinigame>();

            CreateText("Title", panel, "Registro de hallazgos", new Vector2(0f, 244f), new Vector2(820f, 52f), 34, TextAlignmentOptions.Center);
            CreateText("Prompt", panel, "Arrastra cada pieza al recuadro que le corresponde.", new Vector2(0f, 198f), new Vector2(820f, 42f), 22, TextAlignmentOptions.Center);

            RectTransform targetsGroup = CreatePanel("Targets", panel, AnchorPreset.Center, new Vector2(0f, 28f), new Vector2(760f, 230f));
            RectTransform targetA = CreateDropTarget("Target Hueso", targetsGroup, "Hueso largo", new Vector2(-210f, 0f));
            RectTransform targetB = CreateDropTarget("Target Vasija", targetsGroup, "Fragmento ceramico", new Vector2(210f, 0f));

            RectTransform itemsGroup = CreatePanel("Draggable Items", panel, AnchorPreset.BottomCenter, new Vector2(0f, 48f), new Vector2(760f, 130f));
            DragDropItem itemA = CreateDraggableItem("Item Hueso", itemsGroup, "Hueso", new Vector2(-210f, 0f), new Color(0.83f, 0.78f, 0.62f, 1f));
            DragDropItem itemB = CreateDraggableItem("Item Vasija", itemsGroup, "Ceramica", new Vector2(210f, 0f), new Color(0.62f, 0.79f, 0.86f, 1f));

            SerializedObject serialized = new SerializedObject(minigame);
            serialized.FindProperty("_dragPlane").objectReferenceValue = panel;
            serialized.FindProperty("_showDropRadiusPreview").boolValue = true;
            serialized.FindProperty("_previewOnlyInEditor").boolValue = true;
            SerializedProperty pairs = serialized.FindProperty("_pairs");
            pairs.arraySize = 2;
            ConfigureDragPair(pairs.GetArrayElementAtIndex(0), "hueso_largo", itemA, targetA);
            ConfigureDragPair(pairs.GetArrayElementAtIndex(1), "fragmento_ceramico", itemB, targetB);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return panel;
        }

        private static RectTransform CreateMeasurementPanel(RectTransform parent)
        {
            RectTransform panel = CreatePanel("Measurement Panel", parent, AnchorPreset.Stretch, Vector2.zero, new Vector2(-80f, -92f));
            MeasurementMinigame minigame = panel.gameObject.AddComponent<MeasurementMinigame>();
            MeasurementToolSwitcher toolSwitcher = panel.gameObject.AddComponent<MeasurementToolSwitcher>();

            RectTransform measurementPage = CreatePanel("Measurement Page", panel, AnchorPreset.Stretch, Vector2.zero, Vector2.zero);
            RectTransform notebookPage = CreatePanel("Field Notebook Page", panel, AnchorPreset.Stretch, Vector2.zero, Vector2.zero);
            notebookPage.gameObject.SetActive(false);

            TMP_Text prompt = CreateText("Prompt", measurementPage, "Selecciona una herramienta, mide el hueso in situ y registra el resultado en la libreta.", new Vector2(0f, 230f), new Vector2(860f, 52f), 24, TextAlignmentOptions.Center);
            CreateText("Title", measurementPage, "Medicion in situ", new Vector2(0f, 276f), new Vector2(860f, 52f), 34, TextAlignmentOptions.Center);

            RectTransform toolButtons = CreatePanel("Measurement Tool Buttons", measurementPage, AnchorPreset.Center, new Vector2(0f, 176f), new Vector2(620f, 48f));
            Button tapeButton = CreateButton("Tape Tool Button", toolButtons, "Cinta", new Vector2(-100f, 0f), new Vector2(170f, 42f));
            Button circumferenceButton = CreateButton("Circumference Tool Button", toolButtons, "Circunf.", new Vector2(100f, 0f), new Vector2(170f, 42f));

            RectTransform fossil = CreatePanel("Measured Fossil Placeholder", measurementPage, AnchorPreset.Center, new Vector2(0f, 48f), new Vector2(420f, 68f));
            Image fossilImage = fossil.gameObject.AddComponent<Image>();
            fossilImage.color = new Color(0.74f, 0.70f, 0.56f, 1f);
            CreateText("Fossil Label", fossil, "hueso fosil", Vector2.zero, new Vector2(260f, 38f), 20, TextAlignmentOptions.Center);

            RectTransform toolWorkspace = CreatePanel("Measurement Tools", measurementPage, AnchorPreset.Center, new Vector2(0f, 18f), new Vector2(760f, 230f));
            UIMeasurementTape tape = CreateMeasurementTape(toolWorkspace);
            UICircumferenceMeasurementTool circumferenceTool = CreateCircumferenceMeasurementTool(toolWorkspace);
            circumferenceTool.gameObject.SetActive(false);

            Button openNotebook = CreateButton("Open Notebook Button", measurementPage, "Libreta", new Vector2(332f, -226f), new Vector2(170f, 52f));

            CreateText("Notebook Title", notebookPage, "Libreta de campo", new Vector2(0f, 270f), new Vector2(820f, 52f), 34, TextAlignmentOptions.Center);
            TMP_Text notebookPrompt = CreateText("Notebook Prompt", notebookPage, "Registra la medicion tomada.", new Vector2(0f, 210f), new Vector2(780f, 64f), 24, TextAlignmentOptions.Center);
            RectTransform notebookPaper = CreatePanel("Notebook Paper", notebookPage, AnchorPreset.Center, new Vector2(0f, 10f), new Vector2(660f, 320f));
            Image notebookImage = notebookPaper.gameObject.AddComponent<Image>();
            notebookImage.color = new Color(0.88f, 0.84f, 0.69f, 1f);
            TMP_Text fieldLabel = CreateText("Field Label", notebookPaper, "Resultado", new Vector2(-160f, 72f), new Vector2(220f, 38f), 22, TextAlignmentOptions.Left);
            fieldLabel.color = new Color(0.08f, 0.11f, 0.12f, 1f);
            TMP_InputField input = CreateInputField("Answer Input", notebookPaper, new Vector2(-40f, 20f), new Vector2(300f, 56f));
            TMP_Text unit = CreateText("Unit Label", notebookPaper, "cm", new Vector2(150f, 20f), new Vector2(100f, 42f), 22, TextAlignmentOptions.Left);
            unit.color = new Color(0.08f, 0.11f, 0.12f, 1f);
            Button submit = CreateButton("Submit Button", notebookPaper, "Validar", new Vector2(0f, -78f), new Vector2(170f, 52f));
            Button returnToMeasurement = CreateButton("Return To Measurement Button", notebookPage, "Volver a medir", new Vector2(-300f, -226f), new Vector2(210f, 52f));

            ConfigureMeasurementToolSwitcher(toolSwitcher, tape, circumferenceTool, tapeButton, circumferenceButton);

            SerializedObject serialized = new SerializedObject(minigame);
            serialized.FindProperty("_measurementPageRoot").objectReferenceValue = measurementPage.gameObject;
            serialized.FindProperty("_answerPageRoot").objectReferenceValue = notebookPage.gameObject;
            serialized.FindProperty("_sharedPromptLabel").objectReferenceValue = prompt;
            serialized.FindProperty("_sharedAnswerPromptLabel").objectReferenceValue = notebookPrompt;
            serialized.FindProperty("_sharedUnitLabel").objectReferenceValue = unit;
            serialized.FindProperty("_sharedAnswerInput").objectReferenceValue = input;
            serialized.FindProperty("_toolSwitcher").objectReferenceValue = toolSwitcher;
            serialized.FindProperty("_sharedMeasurementTool").objectReferenceValue = tape;
            serialized.FindProperty("_openNotebookButton").objectReferenceValue = openNotebook;
            serialized.FindProperty("_returnToMeasurementButton").objectReferenceValue = returnToMeasurement;
            serialized.FindProperty("_submitButton").objectReferenceValue = submit;

            SerializedProperty questions = serialized.FindProperty("_questions");
            questions.arraySize = 2;
            ConfigureMeasurementQuestion(
                questions.GetArrayElementAtIndex(0),
                "longitud_hueso",
                MeasurementKind.Length,
                MeasurementToolType.LinearTape,
                "Mide la longitud visible del hueso sin moverlo.",
                12.5f,
                0.5f,
                "cm");
            ConfigureMeasurementQuestion(
                questions.GetArrayElementAtIndex(1),
                "circunferencia_concrecion",
                MeasurementKind.Circumference,
                MeasurementToolType.Circumference,
                "Estima la circunferencia de una forma redondeada usando su radio.",
                24f,
                1f,
                "cm");
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return panel;
        }

        private static UIMeasurementTape CreateMeasurementTape(RectTransform parent)
        {
            RectTransform tool = CreatePanel("Measurement Tape", parent, AnchorPreset.Center, new Vector2(0f, 12f), new Vector2(520f, 160f));
            UIMeasurementTape tape = tool.gameObject.AddComponent<UIMeasurementTape>();

            RectTransform tapeBody = CreatePanel("Tape Body", tool, AnchorPreset.Center, Vector2.zero, new Vector2(250f, 12f));
            Image tapeImage = tapeBody.gameObject.AddComponent<Image>();
            tapeImage.color = new Color(0.93f, 0.71f, 0.23f, 1f);

            RectTransform startHandle = CreateHandle("Start Handle", tool, new Vector2(-125f, 0f));
            RectTransform endHandle = CreateHandle("End Handle", tool, new Vector2(125f, 0f));
            TMP_Text readout = CreateText("Readout", tool, "12.5 cm", new Vector2(0f, 36f), new Vector2(180f, 40f), 22, TextAlignmentOptions.Center);

            SerializedObject serialized = new SerializedObject(tape);
            serialized.FindProperty("_startHandle").objectReferenceValue = startHandle;
            serialized.FindProperty("_endHandle").objectReferenceValue = endHandle;
            serialized.FindProperty("_tapeBody").objectReferenceValue = tapeBody;
            serialized.FindProperty("_readout").objectReferenceValue = readout;
            serialized.FindProperty("_pixelsPerUnit").floatValue = 20f;
            serialized.FindProperty("_unit").stringValue = "cm";
            serialized.FindProperty("_decimalPlaces").intValue = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return tape;
        }

        private static UIAngleMeasurementTool CreateAngleMeasurementTool(RectTransform parent)
        {
            RectTransform tool = CreatePanel("Angle Measurement Tool", parent, AnchorPreset.Center, Vector2.zero, new Vector2(640f, 190f));
            UIAngleMeasurementTool angleTool = tool.gameObject.AddComponent<UIAngleMeasurementTool>();

            RectTransform firstArm = CreateToolLine("First Angle Arm", tool, new Color(0.55f, 0.86f, 0.94f, 1f));
            RectTransform secondArm = CreateToolLine("Second Angle Arm", tool, new Color(0.98f, 0.78f, 0.35f, 1f));
            RectTransform vertexHandle = CreateHandle("Vertex Handle", tool, new Vector2(0f, -16f));
            RectTransform firstHandle = CreateHandle("First Arm Handle", tool, new Vector2(-140f, -16f));
            RectTransform secondHandle = CreateHandle("Second Arm Handle", tool, new Vector2(112f, 86f));
            TMP_Text readout = CreateText("Angle Readout", tool, "35 grados", new Vector2(0f, 64f), new Vector2(190f, 40f), 22, TextAlignmentOptions.Center);

            SerializedObject serialized = new SerializedObject(angleTool);
            serialized.FindProperty("_vertexHandle").objectReferenceValue = vertexHandle;
            serialized.FindProperty("_firstArmHandle").objectReferenceValue = firstHandle;
            serialized.FindProperty("_secondArmHandle").objectReferenceValue = secondHandle;
            serialized.FindProperty("_firstArm").objectReferenceValue = firstArm;
            serialized.FindProperty("_secondArm").objectReferenceValue = secondArm;
            serialized.FindProperty("_readout").objectReferenceValue = readout;
            serialized.FindProperty("_unit").stringValue = "grados";
            serialized.FindProperty("_decimalPlaces").intValue = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return angleTool;
        }

        private static UICircumferenceMeasurementTool CreateCircumferenceMeasurementTool(RectTransform parent)
        {
            RectTransform tool = CreatePanel("Circumference Measurement Tool", parent, AnchorPreset.Center, Vector2.zero, new Vector2(640f, 190f));
            UICircumferenceMeasurementTool circumferenceTool = tool.gameObject.AddComponent<UICircumferenceMeasurementTool>();

            RectTransform circle = CreatePanel("Circumference Preview", tool, AnchorPreset.Center, Vector2.zero, new Vector2(250f, 250f));
            DropRadiusPreviewGraphic circlePreview = circle.gameObject.AddComponent<DropRadiusPreviewGraphic>();
            circlePreview.raycastTarget = false;
            circlePreview.SetStyle(new Color(0.04f, 0.95f, 1f, 0.16f), new Color(0.04f, 0.95f, 1f, 1f), 6f, 96);

            RectTransform diameterLine = CreateToolLine("Diameter Line", tool, new Color(0.93f, 0.71f, 0.23f, 1f));
            RectTransform edgeA = CreateHandle("Diameter Edge A", tool, new Vector2(-125f, 0f));
            RectTransform edgeB = CreateHandle("Diameter Edge B", tool, new Vector2(125f, 0f));
            edgeA.GetComponent<Image>().color = Color.red;
            TMP_Text readout = CreateText("Circumference Readout", tool, "Circ: 39.3 cm", new Vector2(0f, 78f), new Vector2(390f, 42f), 20, TextAlignmentOptions.Center);

            SerializedObject serialized = new SerializedObject(circumferenceTool);
            serialized.FindProperty("_edgeAHandle").objectReferenceValue = edgeA;
            serialized.FindProperty("_edgeBHandle").objectReferenceValue = edgeB;
            serialized.FindProperty("_diameterLine").objectReferenceValue = diameterLine;
            serialized.FindProperty("_circlePreview").objectReferenceValue = circlePreview;
            serialized.FindProperty("_readout").objectReferenceValue = readout;
            serialized.FindProperty("_pixelsPerUnit").floatValue = 20f;
            serialized.FindProperty("_unit").stringValue = "cm";
            serialized.FindProperty("_decimalPlaces").intValue = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return circumferenceTool;
        }

        private static RectTransform CreateToolLine(string name, RectTransform parent, Color color)
        {
            RectTransform line = CreatePanel(name, parent, AnchorPreset.Center, Vector2.zero, new Vector2(120f, 10f));
            Image image = line.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return line;
        }

        private static RectTransform CreateDropTarget(string name, RectTransform parent, string label, Vector2 position)
        {
            RectTransform target = CreatePanel(name, parent, AnchorPreset.Center, position, new Vector2(260f, 146f));
            Image image = target.gameObject.AddComponent<Image>();
            image.color = new Color(0.1f, 0.22f, 0.25f, 0.78f);
            CreateText("Label", target, label, new Vector2(0f, -72f), new Vector2(240f, 34f), 18, TextAlignmentOptions.Center);
            return target;
        }

        private static DragDropItem CreateDraggableItem(string name, RectTransform parent, string label, Vector2 position, Color color)
        {
            RectTransform item = CreatePanel(name, parent, AnchorPreset.Center, position, new Vector2(188f, 66f));
            Image image = item.gameObject.AddComponent<Image>();
            image.color = color;
            item.gameObject.AddComponent<CanvasGroup>();
            DragDropItem dragDropItem = item.gameObject.AddComponent<DragDropItem>();
            CreateText("Label", item, label, Vector2.zero, new Vector2(170f, 38f), 22, TextAlignmentOptions.Center);
            return dragDropItem;
        }

        private static RectTransform CreateHandle(string name, RectTransform parent, Vector2 position)
        {
            RectTransform handle = CreatePanel(name, parent, AnchorPreset.Center, position, new Vector2(34f, 34f));
            Image image = handle.gameObject.AddComponent<Image>();
            image.color = new Color(0.98f, 0.95f, 0.72f, 1f);
            return handle;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size)
        {
            RectTransform rect = CreatePanel(name, parent, AnchorPreset.Center, position, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.16f, 0.43f, 0.49f, 1f);
            Button button = rect.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.21f, 0.56f, 0.63f, 1f);
            colors.pressedColor = new Color(0.1f, 0.31f, 0.37f, 1f);
            button.colors = colors;
            TMP_Text text = CreateText("Label", rect, label, Vector2.zero, size - new Vector2(18f, 8f), 20, TextAlignmentOptions.Center);
            text.raycastTarget = false;
            return button;
        }

        private static TMP_InputField CreateInputField(string name, Transform parent, Vector2 position, Vector2 size)
        {
            RectTransform rect = CreatePanel(name, parent, AnchorPreset.Center, position, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.92f, 0.96f, 0.94f, 1f);

            TMP_InputField input = rect.gameObject.AddComponent<TMP_InputField>();
            TMP_Text text = CreateText("Text", rect, "", Vector2.zero, size - new Vector2(24f, 8f), 22, TextAlignmentOptions.Left);
            text.color = new Color(0.05f, 0.08f, 0.09f, 1f);
            TMP_Text placeholder = CreateText("Placeholder", rect, "0.0", Vector2.zero, size - new Vector2(24f, 8f), 22, TextAlignmentOptions.Left);
            placeholder.color = new Color(0.36f, 0.44f, 0.45f, 0.72f);
            input.textComponent = text;
            input.placeholder = placeholder;
            return input;
        }

        private static TMP_Text CreateText(string name, Transform parent, string value, Vector2 position, Vector2 size, int fontSize, TextAlignmentOptions alignment)
        {
            RectTransform rect = CreatePanel(name, parent, AnchorPreset.Center, position, size);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private static RectTransform CreatePanel(string name, Transform parent, AnchorPreset preset, Vector2 position, Vector2 size)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform));
            SetLayerRecursively(panel, LayerMask.NameToLayer("UI"));
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.GetComponent<RectTransform>();
            ApplyAnchorPreset(rect, preset);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static void ConfigurePopupCanvas(
            MinigamePopupCanvas popupCanvas,
            GameObject popupRoot,
            CanvasGroup popupGroup,
            Button closeButton,
            Button dragButton,
            Button measurementButton,
            GameObject dragPanel,
            GameObject measurementPanel)
        {
            SerializedObject serialized = new SerializedObject(popupCanvas);
            serialized.FindProperty("_popupRoot").objectReferenceValue = popupRoot;
            serialized.FindProperty("_popupGroup").objectReferenceValue = popupGroup;
            serialized.FindProperty("_closeButton").objectReferenceValue = closeButton;
            serialized.FindProperty("_hideOnAwake").boolValue = true;

            SerializedProperty minigames = serialized.FindProperty("_minigames");
            minigames.arraySize = 2;
            ConfigurePopupEntry(minigames.GetArrayElementAtIndex(0), "drag_drop", dragPanel, dragButton);
            ConfigurePopupEntry(minigames.GetArrayElementAtIndex(1), "measurement", measurementPanel, measurementButton);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigurePopupEntry(SerializedProperty property, string id, GameObject root, Button button)
        {
            property.FindPropertyRelative("_id").stringValue = id;
            property.FindPropertyRelative("_root").objectReferenceValue = root;
            property.FindPropertyRelative("_testButton").objectReferenceValue = button;
            property.FindPropertyRelative("_hideOtherPanels").boolValue = true;
        }

        private static void ConfigureDragPair(SerializedProperty property, string id, DragDropItem item, RectTransform target)
        {
            property.FindPropertyRelative("_id").stringValue = id;
            property.FindPropertyRelative("_draggable").objectReferenceValue = item;
            property.FindPropertyRelative("_target").objectReferenceValue = target;
            property.FindPropertyRelative("_dropRadius").floatValue = 72f;
            property.FindPropertyRelative("_snapOnCorrectDrop").boolValue = true;
            property.FindPropertyRelative("_lockOnCorrectDrop").boolValue = true;
        }

        private static void ConfigureMeasurementToolSwitcher(
            MeasurementToolSwitcher switcher,
            UIMeasurementTape tape,
            UICircumferenceMeasurementTool circumferenceTool,
            Button tapeButton,
            Button circumferenceButton)
        {
            SerializedObject serialized = new SerializedObject(switcher);
            serialized.FindProperty("_defaultTool").enumValueIndex = (int)MeasurementToolType.LinearTape;

            SerializedProperty tools = serialized.FindProperty("_tools");
            tools.arraySize = 2;
            ConfigureMeasurementToolBinding(tools.GetArrayElementAtIndex(0), MeasurementToolType.LinearTape, "cinta", tape.gameObject, tapeButton, tape);
            ConfigureMeasurementToolBinding(tools.GetArrayElementAtIndex(1), MeasurementToolType.Circumference, "circunferencia", circumferenceTool.gameObject, circumferenceButton, circumferenceTool);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureMeasurementToolBinding(
            SerializedProperty property,
            MeasurementToolType toolType,
            string id,
            GameObject root,
            Button button,
            MeasurementToolBase tool)
        {
            property.FindPropertyRelative("_toolType").enumValueIndex = (int)toolType;
            property.FindPropertyRelative("_id").stringValue = id;
            property.FindPropertyRelative("_root").objectReferenceValue = root;
            property.FindPropertyRelative("_button").objectReferenceValue = button;
            property.FindPropertyRelative("_tool").objectReferenceValue = tool;
        }

        private static void ConfigureMeasurementQuestion(
            SerializedProperty property,
            string id,
            MeasurementKind kind,
            MeasurementToolType toolType,
            string prompt,
            float expectedNumber,
            float tolerance,
            string unit)
        {
            property.FindPropertyRelative("_id").stringValue = id;
            property.FindPropertyRelative("_measurementKind").enumValueIndex = (int)kind;
            property.FindPropertyRelative("_toolType").enumValueIndex = (int)toolType;
            property.FindPropertyRelative("_prompt").stringValue = prompt;
            property.FindPropertyRelative("_answerKind").enumValueIndex = (int)MeasurementAnswerKind.Number;
            property.FindPropertyRelative("_expectedNumber").floatValue = expectedNumber;
            property.FindPropertyRelative("_numberTolerance").floatValue = tolerance;
            property.FindPropertyRelative("_unit").stringValue = unit;
        }

        private static void ApplyAnchorPreset(RectTransform rect, AnchorPreset preset)
        {
            switch (preset)
            {
                case AnchorPreset.Stretch:
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                    break;
                case AnchorPreset.TopRight:
                    rect.anchorMin = Vector2.one;
                    rect.anchorMax = Vector2.one;
                    rect.pivot = Vector2.one;
                    break;
                case AnchorPreset.BottomCenter:
                    rect.anchorMin = new Vector2(0.5f, 0f);
                    rect.anchorMax = new Vector2(0.5f, 0f);
                    rect.pivot = new Vector2(0.5f, 0f);
                    break;
                default:
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    break;
            }
        }

        private static void SetLayerRecursively(GameObject gameObject, int layer)
        {
            if (layer < 0)
            {
                layer = 5;
            }

            gameObject.layer = layer;
            foreach (Transform child in gameObject.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private enum AnchorPreset
        {
            Center,
            TopRight,
            BottomCenter,
            Stretch
        }
    }
}
