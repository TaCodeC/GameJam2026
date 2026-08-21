using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GameJam.Gameplay.Minigames.Editor
{
    public static class BonePlacementDragDropPrefabGenerator
    {
        private const string PopupSourcePath = "Assets/Game/Gameplay/Minigames/Prefabs/MinigamePopupCanvas.prefab";
        private const string PrefabFolder = "Assets/Game/Gameplay/Minigames/BoneMeasurements/Prefabs";
        private const string ArtFolder = "Assets/Game/Gameplay/Minigames/BoneMeasurements/Art";
        private const string BoxSpritePath = ArtFolder + "/Boxes/CurvedBonesBox.png";
        private const string RibSpritePath = ArtFolder + "/Bones/PrimeraCostilla.png";
        private const string SacrumSpritePath = ArtFolder + "/Bones/Sacro.png";
        private const string StraightBoxSpritePath = "Assets/Game/Correciones_Final/CAJA HUESOS RECTOS (20260709044356).png";
        private const string HumerusSpritePath = ArtFolder + "/Bones/Humero.png";
        private const string FemurSpritePath = ArtFolder + "/Bones/Femur.png";

        [MenuItem("Game Jam/Minigames/Generate All Updated Bone Minigames")]
        public static void GenerateAllUpdatedBoneMinigames()
        {
            UpdateExistingMeasurementPrefabs();
            Generate();
            GenerateHumero();
        }

        private static void UpdateExistingMeasurementPrefabs()
        {
            UpdateMeasurementPrefabAtPath(PopupSourcePath);

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder });
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                if (!Path.GetFileName(path).StartsWith("MinigamePopupCanvas_", StringComparison.Ordinal))
                {
                    continue;
                }

                UpdateMeasurementPrefabAtPath(path);
            }
        }

        private static void UpdateMeasurementPrefabAtPath(string path)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                RemoveChildIfPresent(root.transform, "Angle Tool Button");
                RemoveChildIfPresent(root.transform, "Angle Measurement Tool");

                MeasurementToolSwitcher switcher = root.GetComponentInChildren<MeasurementToolSwitcher>(true);
                if (switcher != null)
                {
                    RemoveAngleEntries(new SerializedObject(switcher), "_tools", "_toolType");
                }

                MeasurementMinigame minigame = root.GetComponentInChildren<MeasurementMinigame>(true);
                if (minigame != null)
                {
                    SerializedObject minigameSerialized = new SerializedObject(minigame);
                    RemoveAngleEntries(minigameSerialized, "_questions", "_toolType");

                    if (Path.GetFileNameWithoutExtension(path) == "MinigamePopupCanvas_Clavicula")
                    {
                        ConfigureClavicleLength(minigameSerialized);
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [MenuItem("Game Jam/Minigames/Generate Costilla and Sacro Drag Drop Prefabs")]
        public static void Generate()
        {
            ConfigureSpriteImporter(BoxSpritePath);

            Sprite boxSprite = RequireSprite(BoxSpritePath);
            Sprite ribSprite = RequireSprite(RibSpritePath);
            Sprite sacrumSprite = RequireSprite(SacrumSpritePath);

            MinigamePopupCanvas ribPopup = CreatePopup(
                "PrimeraCostilla",
                "primera_costilla",
                "Primera costilla",
                ribSprite,
                new Vector2(245f, 130f),
                boxSprite,
                ribSprite,
                sacrumSprite,
                true);

            MinigamePopupCanvas sacrumPopup = CreatePopup(
                "Sacro",
                "sacro",
                "Sacro",
                sacrumSprite,
                new Vector2(130f, 185f),
                boxSprite,
                ribSprite,
                sacrumSprite,
                false);

            ConfigureSpecialMinigame("PrimeraCostilla", "primera_costilla", ribPopup);
            ConfigureSpecialMinigame("Sacro", "sacro", sacrumPopup);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Minigames] Generated editable Costilla and Sacro drag-and-drop popup prefabs.");
        }

        [MenuItem("Game Jam/Minigames/Generate Humero Drag Drop Prefab")]
        public static void GenerateHumero()
        {
            ConfigureSpriteImporter(StraightBoxSpritePath);

            Sprite boxSprite = RequireSprite(StraightBoxSpritePath);
            Sprite humerusSprite = RequireSprite(HumerusSpritePath);
            Sprite femurSprite = RequireSprite(FemurSpritePath);
            MinigamePopupCanvas popup = CreateHumeroPopup(boxSprite, humerusSprite, femurSprite);

            ConfigureSpecialMinigame("Humero", "humero", popup);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Minigames] Generated editable Humero drag-and-drop popup prefab.");
        }

        private static MinigamePopupCanvas CreateHumeroPopup(Sprite boxSprite, Sprite humerusSprite, Sprite femurSprite)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(PopupSourcePath);
            if (source == null)
            {
                throw new InvalidOperationException($"Popup source prefab not found at {PopupSourcePath}.");
            }

            GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(source);
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            root.name = "MinigamePopupCanvas_Humero";

            Transform testButtons = FindChild(root.transform, "Test Buttons");
            if (testButtons != null)
            {
                testButtons.gameObject.SetActive(false);
            }

            Transform measurementPanel = FindChild(root.transform, "Measurement Panel");
            if (measurementPanel != null)
            {
                UnityEngine.Object.DestroyImmediate(measurementPanel.gameObject);
            }

            Transform dragPanelTransform = FindChild(root.transform, "DragDrop Panel");
            if (dragPanelTransform == null)
            {
                UnityEngine.Object.DestroyImmediate(root);
                throw new InvalidOperationException("DragDrop Panel was not found in the popup source prefab.");
            }

            ClearChildren(dragPanelTransform);
            RectTransform dragPanel = (RectTransform)dragPanelTransform;
            DragDropMinigame dragDrop = dragPanel.GetComponent<DragDropMinigame>();
            BonePlacementDropState placementState = dragPanel.gameObject.AddComponent<BonePlacementDropState>();

            TMP_Text title = CreateText(
                "Title",
                dragPanel,
                "Guarda: Humero",
                new Vector2(0f, 247f),
                new Vector2(760f, 42f),
                32);
            title.fontStyle = FontStyles.Bold;

            CreateText(
                "Prompt",
                dragPanel,
                "Arrastra el humero hasta su silueta dentro de la caja.",
                new Vector2(0f, 211f),
                new Vector2(760f, 34f),
                20);

            Image box = CreateImage("Caja de huesos rectos", dragPanel, boxSprite, new Vector2(0f, -22f), new Vector2(820f, 524f));
            box.raycastTarget = false;

            RectTransform placedRoot = CreateRect("Huesos ya colocados", dragPanel, Vector2.zero, Vector2.zero);
            Stretch(placedRoot);
            Image placedHumerus = CreateImage("Humero colocado (Superior)", placedRoot, humerusSprite, new Vector2(155f, 60f), new Vector2(110f, 360f));
            Image placedFemur = CreateImage("Femur colocado (Inferior)", placedRoot, femurSprite, new Vector2(-35f, -102f), new Vector2(110f, 420f));
            placedHumerus.rectTransform.localEulerAngles = new Vector3(0f, 0f, 90f);
            placedFemur.rectTransform.localEulerAngles = new Vector3(0f, 0f, -90f);
            placedHumerus.raycastTarget = false;
            placedFemur.raycastTarget = false;
            placedHumerus.gameObject.SetActive(false);
            placedFemur.gameObject.SetActive(false);

            RectTransform targetsRoot = CreateRect("Zonas de entrega (EDITAR AQUI)", dragPanel, Vector2.zero, Vector2.zero);
            Stretch(targetsRoot);
            RectTransform humerusTarget = CreateRect("TARGET Humero (Superior)", targetsRoot, new Vector2(155f, 60f), new Vector2(365f, 125f));
            CreateRect("REFERENCIA Femur (Inferior)", targetsRoot, new Vector2(-35f, -102f), new Vector2(470f, 135f));

            RectTransform draggableRoot = CreateRect("Hueso arrastrable (EDITAR AQUI)", dragPanel, Vector2.zero, Vector2.zero);
            Stretch(draggableRoot);
            Image draggableImage = CreateImage("Humero", draggableRoot, humerusSprite, new Vector2(-190f, 155f), new Vector2(75f, 285f));
            draggableImage.rectTransform.localEulerAngles = new Vector3(0f, 0f, 90f);
            draggableImage.raycastTarget = true;
            draggableImage.gameObject.AddComponent<CanvasGroup>();
            DragDropItem draggable = draggableImage.gameObject.AddComponent<DragDropItem>();

            SerializedObject dragDropSerialized = new SerializedObject(dragDrop);
            dragDropSerialized.FindProperty("_dragPlane").objectReferenceValue = dragPanel;
            dragDropSerialized.FindProperty("_showDropRadiusPreview").boolValue = true;
            dragDropSerialized.FindProperty("_previewOnlyInEditor").boolValue = true;
            SerializedProperty pairs = dragDropSerialized.FindProperty("_pairs");
            pairs.arraySize = 1;
            ConfigurePair(pairs.GetArrayElementAtIndex(0), "humero", draggable, humerusTarget);
            dragDropSerialized.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject placementSerialized = new SerializedObject(placementState);
            SerializedProperty visuals = placementSerialized.FindProperty("_placedBoneVisuals");
            visuals.arraySize = 2;
            ConfigurePlacedVisual(visuals.GetArrayElementAtIndex(0), "humero", placedHumerus.gameObject);
            ConfigurePlacedVisual(visuals.GetArrayElementAtIndex(1), "femur", placedFemur.gameObject);
            placementSerialized.ApplyModifiedPropertiesWithoutUndo();

            MinigamePopupCanvas popup = root.GetComponent<MinigamePopupCanvas>();
            SerializedObject popupSerialized = new SerializedObject(popup);
            SerializedProperty minigames = popupSerialized.FindProperty("_minigames");
            minigames.arraySize = 1;
            SerializedProperty entry = minigames.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("_id").stringValue = "drag_drop";
            entry.FindPropertyRelative("_root").objectReferenceValue = dragPanel.gameObject;
            entry.FindPropertyRelative("_testButton").objectReferenceValue = null;
            entry.FindPropertyRelative("_hideOtherPanels").boolValue = true;
            popupSerialized.ApplyModifiedPropertiesWithoutUndo();

            string path = $"{PrefabFolder}/MinigamePopupCanvas_Humero.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab.GetComponent<MinigamePopupCanvas>();
        }

        private static MinigamePopupCanvas CreatePopup(
            string assetName,
            string boneId,
            string displayName,
            Sprite draggableSprite,
            Vector2 draggableSize,
            Sprite boxSprite,
            Sprite ribSprite,
            Sprite sacrumSprite,
            bool useLeftTarget)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(PopupSourcePath);
            if (source == null)
            {
                throw new InvalidOperationException($"Popup source prefab not found at {PopupSourcePath}.");
            }

            GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(source);
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            root.name = $"MinigamePopupCanvas_{assetName}";

            Transform testButtons = FindChild(root.transform, "Test Buttons");
            if (testButtons != null)
            {
                testButtons.gameObject.SetActive(false);
            }

            Transform measurementPanel = FindChild(root.transform, "Measurement Panel");
            if (measurementPanel != null)
            {
                UnityEngine.Object.DestroyImmediate(measurementPanel.gameObject);
            }

            Transform dragPanelTransform = FindChild(root.transform, "DragDrop Panel");
            if (dragPanelTransform == null)
            {
                UnityEngine.Object.DestroyImmediate(root);
                throw new InvalidOperationException("DragDrop Panel was not found in the popup source prefab.");
            }

            ClearChildren(dragPanelTransform);
            RectTransform dragPanel = (RectTransform)dragPanelTransform;
            DragDropMinigame dragDrop = dragPanel.GetComponent<DragDropMinigame>();
            BonePlacementDropState placementState = dragPanel.gameObject.AddComponent<BonePlacementDropState>();

            TMP_Text title = CreateText(
                "Title",
                dragPanel,
                $"Guarda: {displayName}",
                new Vector2(0f, 247f),
                new Vector2(760f, 42f),
                32);
            title.fontStyle = FontStyles.Bold;

            CreateText(
                "Prompt",
                dragPanel,
                "Arrastra el hueso hasta su silueta dentro de la caja.",
                new Vector2(0f, 211f),
                new Vector2(760f, 34f),
                20);

            Image box = CreateImage("Caja de huesos arqueados", dragPanel, boxSprite, new Vector2(0f, -22f), new Vector2(820f, 524f));
            box.raycastTarget = false;

            RectTransform placedRoot = CreateRect("Huesos ya colocados", dragPanel, Vector2.zero, Vector2.zero);
            Stretch(placedRoot);
            Image placedRib = CreateImage("Costilla colocada (Izquierda)", placedRoot, ribSprite, new Vector2(-185f, -92f), new Vector2(300f, 210f));
            Image placedSacrum = CreateImage("Sacro colocado (Derecha)", placedRoot, sacrumSprite, new Vector2(185f, -92f), new Vector2(180f, 235f));
            placedRib.raycastTarget = false;
            placedSacrum.raycastTarget = false;
            placedRib.gameObject.SetActive(false);
            placedSacrum.gameObject.SetActive(false);

            RectTransform targetsRoot = CreateRect("Zonas de entrega (EDITAR AQUI)", dragPanel, Vector2.zero, Vector2.zero);
            Stretch(targetsRoot);
            RectTransform ribTarget = CreateRect("TARGET Costilla (Izquierda)", targetsRoot, new Vector2(-185f, -92f), new Vector2(300f, 210f));
            RectTransform sacrumTarget = CreateRect("TARGET Sacro (Derecha)", targetsRoot, new Vector2(185f, -92f), new Vector2(180f, 235f));

            RectTransform draggableRoot = CreateRect("Hueso arrastrable (EDITAR AQUI)", dragPanel, Vector2.zero, Vector2.zero);
            Stretch(draggableRoot);
            Image draggableImage = CreateImage(displayName, draggableRoot, draggableSprite, new Vector2(0f, 112f), draggableSize);
            draggableImage.raycastTarget = true;
            draggableImage.gameObject.AddComponent<CanvasGroup>();
            DragDropItem draggable = draggableImage.gameObject.AddComponent<DragDropItem>();

            SerializedObject dragDropSerialized = new SerializedObject(dragDrop);
            dragDropSerialized.FindProperty("_dragPlane").objectReferenceValue = dragPanel;
            dragDropSerialized.FindProperty("_showDropRadiusPreview").boolValue = true;
            dragDropSerialized.FindProperty("_previewOnlyInEditor").boolValue = true;
            SerializedProperty pairs = dragDropSerialized.FindProperty("_pairs");
            pairs.arraySize = 1;
            ConfigurePair(
                pairs.GetArrayElementAtIndex(0),
                boneId,
                draggable,
                useLeftTarget ? ribTarget : sacrumTarget);
            dragDropSerialized.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject placementSerialized = new SerializedObject(placementState);
            SerializedProperty visuals = placementSerialized.FindProperty("_placedBoneVisuals");
            visuals.arraySize = 2;
            ConfigurePlacedVisual(visuals.GetArrayElementAtIndex(0), "primera_costilla", placedRib.gameObject);
            ConfigurePlacedVisual(visuals.GetArrayElementAtIndex(1), "sacro", placedSacrum.gameObject);
            placementSerialized.ApplyModifiedPropertiesWithoutUndo();

            MinigamePopupCanvas popup = root.GetComponent<MinigamePopupCanvas>();
            SerializedObject popupSerialized = new SerializedObject(popup);
            SerializedProperty minigames = popupSerialized.FindProperty("_minigames");
            minigames.arraySize = 1;
            SerializedProperty entry = minigames.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("_id").stringValue = "drag_drop";
            entry.FindPropertyRelative("_root").objectReferenceValue = dragPanel.gameObject;
            entry.FindPropertyRelative("_testButton").objectReferenceValue = null;
            entry.FindPropertyRelative("_hideOtherPanels").boolValue = true;
            popupSerialized.ApplyModifiedPropertiesWithoutUndo();

            string path = $"{PrefabFolder}/MinigamePopupCanvas_{assetName}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab.GetComponent<MinigamePopupCanvas>();
        }

        private static void ConfigureSpecialMinigame(string assetName, string objectId, MinigamePopupCanvas popupPrefab)
        {
            string path = $"{PrefabFolder}/SpecialMinigame_{assetName}.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                MinigameObjectState state = root.GetComponent<MinigameObjectState>();
                SerializedObject stateSerialized = new SerializedObject(state);
                stateSerialized.FindProperty("_objectId").stringValue = objectId;
                stateSerialized.ApplyModifiedPropertiesWithoutUndo();

                MinigameInteractableObject interactable = root.GetComponent<MinigameInteractableObject>();
                SerializedObject interactableSerialized = new SerializedObject(interactable);
                interactableSerialized.FindProperty("_popupCanvas").objectReferenceValue = null;
                interactableSerialized.FindProperty("_popupCanvasPrefab").objectReferenceValue = popupPrefab;
                interactableSerialized.FindProperty("_minigameId").stringValue = "drag_drop";
                interactableSerialized.ApplyModifiedPropertiesWithoutUndo();

                InteractableSparklePrompt[] prompts = root.GetComponentsInChildren<InteractableSparklePrompt>(true);
                for (int i = 0; i < prompts.Length; i++)
                {
                    SerializedObject promptSerialized = new SerializedObject(prompts[i]);
                    promptSerialized.FindProperty("_objectState").objectReferenceValue = state;
                    promptSerialized.FindProperty("_minigameId").stringValue = "drag_drop";
                    promptSerialized.ApplyModifiedPropertiesWithoutUndo();
                }

                BoneMeasurementMinigameDefinition definition = root.GetComponent<BoneMeasurementMinigameDefinition>();
                if (definition != null)
                {
                    UnityEngine.Object.DestroyImmediate(definition, true);
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigurePair(SerializedProperty pair, string id, DragDropItem draggable, RectTransform target)
        {
            pair.FindPropertyRelative("_id").stringValue = id;
            pair.FindPropertyRelative("_draggable").objectReferenceValue = draggable;
            pair.FindPropertyRelative("_target").objectReferenceValue = target;
            pair.FindPropertyRelative("_dropRadius").floatValue = 88f;
            pair.FindPropertyRelative("_snapOnCorrectDrop").boolValue = true;
            pair.FindPropertyRelative("_lockOnCorrectDrop").boolValue = true;
        }

        private static void RemoveAngleEntries(SerializedObject serialized, string arrayPropertyName, string toolTypePropertyName)
        {
            SerializedProperty entries = serialized.FindProperty(arrayPropertyName);
            for (int i = entries.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty toolType = entries.GetArrayElementAtIndex(i).FindPropertyRelative(toolTypePropertyName);
                if (toolType != null && toolType.enumValueIndex == (int)MeasurementToolType.Angle)
                {
                    entries.DeleteArrayElementAtIndex(i);
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureClavicleLength(SerializedObject minigameSerialized)
        {
            SerializedProperty questions = minigameSerialized.FindProperty("_questions");
            questions.arraySize = 1;
            SerializedProperty question = questions.GetArrayElementAtIndex(0);
            question.FindPropertyRelative("_id").stringValue = "clavicula";
            question.FindPropertyRelative("_measurementKind").enumValueIndex = (int)MeasurementKind.Length;
            question.FindPropertyRelative("_toolType").enumValueIndex = (int)MeasurementToolType.LinearTape;
            question.FindPropertyRelative("_prompt").stringValue = "Mide la longitud visible de la clavicula sin moverla.";
            question.FindPropertyRelative("_answerKind").enumValueIndex = (int)MeasurementAnswerKind.Number;
            question.FindPropertyRelative("_expectedNumber").floatValue = 28f;
            question.FindPropertyRelative("_numberTolerance").floatValue = 1f;
            question.FindPropertyRelative("_unit").stringValue = "cm";
            minigameSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigurePlacedVisual(SerializedProperty entry, string boneId, GameObject visual)
        {
            entry.FindPropertyRelative("_boneId").stringValue = boneId;
            entry.FindPropertyRelative("_visual").objectReferenceValue = visual;
        }

        private static Sprite RequireSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            return sprite != null ? sprite : throw new InvalidOperationException($"Sprite not found at {path}.");
        }

        private static void ConfigureSpriteImporter(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Texture importer not found for {path}.");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size)
        {
            RectTransform rect = CreateRect(name, parent, position, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
            return image;
        }

        private static TMP_Text CreateText(string name, Transform parent, string value, Vector2 position, Vector2 size, float fontSize)
        {
            RectTransform rect = CreateRect(name, parent, position, size);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.layer = LayerMask.NameToLayer("UI");
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }

        private static void RemoveChildIfPresent(Transform root, string name)
        {
            Transform child = FindChild(root, name);
            if (child != null)
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            if (string.Equals(root.name, name, StringComparison.Ordinal))
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChild(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
