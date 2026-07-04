using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameJam.Gameplay.Minigames.Editor
{
    public static class BoneMeasurementMinigamePrefabGenerator
    {
        private const string SourceRoot = "/Users/tacodec/Downloads/Game Jam 2026-3/HUESOS MINIJUEGO";
        private const string ScenePath = "Assets/Scenes/Cave_ShaderTesst.unity";
        private const string PopupPrefabPath = "Assets/Game/Gameplay/Minigames/Prefabs/MinigamePopupCanvas.prefab";
        private const string OutputRoot = "Assets/Game/Gameplay/Minigames/BoneMeasurements";
        private const string ArtRoot = OutputRoot + "/Art";
        private const string PrefabFolder = OutputRoot + "/Prefabs";
        private const string CanvasBasePrefabPath = PrefabFolder + "/MinigamePopupCanvas_BoneMeasurement_Base.prefab";
        private const string SpecialMinigameBasePrefabPath = PrefabFolder + "/SpecialMinigame_BoneMeasurement_Base.prefab";

        [MenuItem("Game Jam/Minigames/Generate Bone Measurement Prefabs")]
        public static void Generate()
        {
            if (!Directory.Exists(SourceRoot))
            {
                throw new DirectoryNotFoundException($"Bone minigame source folder not found: {SourceRoot}");
            }

            Directory.CreateDirectory(ProjectPath(ArtRoot + "/Backgrounds"));
            Directory.CreateDirectory(ProjectPath(ArtRoot + "/Bones"));
            Directory.CreateDirectory(ProjectPath(ArtRoot + "/Notebooks"));
            Directory.CreateDirectory(ProjectPath(PrefabFolder));

            Sprite background = CopySprite(
                "Fondo rocoso (20260702090728).png",
                ArtRoot + "/Backgrounds/RockBackground.png");

            BoneRecord[] records = CreateRecords();
            for (int i = 0; i < records.Length; i++)
            {
                BoneRecord record = records[i];
                record.BackgroundSprite = background;
                record.BoneSprite = CopySprite(record.BoneSourcePath, $"{ArtRoot}/Bones/{record.AssetName}.png");
                record.NotebookSprite = CopySprite(record.NotebookSourcePath, $"{ArtRoot}/Notebooks/{record.AssetName}_Notebook.png");
            }

            ConfigurePopupPrefab();

            GameObject canvasBasePrefab = CreateCanvasBasePrefab(background);
            for (int i = 0; i < records.Length; i++)
            {
                CreateCanvasVariantPrefab(canvasBasePrefab, records[i]);
            }

            GameObject basePrefab = CreateBasePrefabFromScene(canvasBasePrefab.GetComponent<MinigamePopupCanvas>());
            for (int i = 0; i < records.Length; i++)
            {
                GameObject popupCanvasAsset = AssetDatabase.LoadAssetAtPath<GameObject>(GetCanvasVariantPath(records[i]));
                MinigamePopupCanvas popupCanvasPrefab = popupCanvasAsset != null
                    ? popupCanvasAsset.GetComponent<MinigamePopupCanvas>()
                    : null;
                CreateVariantPrefab(basePrefab, records[i], popupCanvasPrefab);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Minigames] Generated {records.Length} bone measurement popup variants and triggers in {PrefabFolder}.");
        }

        private static BoneRecord[] CreateRecords()
        {
            return new[]
            {
                new BoneRecord(
                    "femur",
                    "Femur",
                    "HUESOS RECTOS/femur render (20260701025620).png",
                    "UI LIBRETA LVL1/femur.png",
                    MeasurementKind.Length,
                    MeasurementToolType.LinearTape,
                    "Mide la longitud visible del femur sin moverlo.",
                    48f,
                    1f,
                    "cm"),
                new BoneRecord(
                    "humero",
                    "Humero",
                    "HUESOS RECTOS/umero render (20260701025648).png",
                    "UI LIBRETA LVL1/humero.png",
                    MeasurementKind.Length,
                    MeasurementToolType.LinearTape,
                    "Mide la longitud visible del humero sin moverlo.",
                    32f,
                    1f,
                    "cm"),
                new BoneRecord(
                    "perone",
                    "Perone",
                    "HUESOS RECTOS/Perone  render(20260701081104).png",
                    "UI LIBRETA LVL1/perone.png",
                    MeasurementKind.Length,
                    MeasurementToolType.LinearTape,
                    "Mide la longitud visible del perone sin moverlo.",
                    35f,
                    1f,
                    "cm"),
                new BoneRecord(
                    "clavicula",
                    "Clavicula",
                    "HUESOS ARQUEADOS/clavicula (20260702023006).png",
                    "UI LIBRETA LVL1/clavicula.png",
                    MeasurementKind.Angle,
                    MeasurementToolType.Angle,
                    "Mide el angulo principal de la curvatura de la clavicula.",
                    28f,
                    3f,
                    "grados"),
                new BoneRecord(
                    "primera_costilla",
                    "Primera costilla",
                    "HUESOS ARQUEADOS/1era costilla (20260702022950).png",
                    "UI LIBRETA LVL1/costilla.png",
                    MeasurementKind.Angle,
                    MeasurementToolType.Angle,
                    "Mide el angulo principal de la curvatura de la primera costilla.",
                    42f,
                    3f,
                    "grados"),
                new BoneRecord(
                    "sacro",
                    "Sacro",
                    "HUESOS ARQUEADOS/hueso sacro (20260702120719).png",
                    "UI LIBRETA LVL1/sacro.png",
                    MeasurementKind.Angle,
                    MeasurementToolType.Angle,
                    "Mide el angulo principal de la curvatura del sacro.",
                    18f,
                    3f,
                    "grados"),
                new BoneRecord(
                    "craneo",
                    "Craneo",
                    "HUESOS CIRCULARES/craneo (20260702055539).png",
                    "UI LIBRETA LVL1/craneo.png",
                    MeasurementKind.Circumference,
                    MeasurementToolType.Circumference,
                    "Estima la circunferencia visible del craneo usando su diametro.",
                    55f,
                    2f,
                    "cm"),
                new BoneRecord(
                    "rotula",
                    "Rotula",
                    "HUESOS CIRCULARES/rotula de rodilla (20260702055728).png",
                    "UI LIBRETA LVL1/rotula.png",
                    MeasurementKind.Circumference,
                    MeasurementToolType.Circumference,
                    "Estima la circunferencia visible de la rotula usando su diametro.",
                    12f,
                    1f,
                    "cm"),
                new BoneRecord(
                    "vertebra_lumbar",
                    "Vertebra lumbar",
                    "HUESOS CIRCULARES/vertebra lumbar (20260702055707).png",
                    "UI LIBRETA LVL1/vertebra lumbar.png",
                    MeasurementKind.Circumference,
                    MeasurementToolType.Circumference,
                    "Estima la circunferencia visible de la vertebra lumbar usando su diametro.",
                    18f,
                    1f,
                    "cm")
            };
        }

        private static void ConfigurePopupPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PopupPrefabPath);

            try
            {
                MeasurementMinigame minigame = root.GetComponentInChildren<MeasurementMinigame>(true);
                if (minigame == null)
                {
                    throw new InvalidOperationException($"MeasurementMinigame not found in {PopupPrefabPath}");
                }

                Transform measurementPage = FindChild(minigame.transform, "Measurement Page");
                Transform notebookPage = FindChild(minigame.transform, "Field Notebook Page");
                if (measurementPage == null || notebookPage == null)
                {
                    throw new InvalidOperationException("Measurement pages not found in popup prefab.");
                }

                Image backgroundImage = EnsureImageChild(measurementPage, "Rock Background");
                ConfigureStretch(backgroundImage.rectTransform);
                backgroundImage.raycastTarget = false;
                backgroundImage.preserveAspect = false;
                backgroundImage.transform.SetAsFirstSibling();

                Transform placeholder = FindChild(measurementPage, "Measured Fossil Placeholder");
                Image boneImage = placeholder != null
                    ? EnsureImage(placeholder.gameObject)
                    : EnsureImageChild(measurementPage, "Measured Bone Image");
                boneImage.gameObject.name = "Measured Bone Image";
                ConfigureCentered(boneImage.rectTransform, new Vector2(0f, -10f), new Vector2(460f, 330f));
                boneImage.color = Color.white;
                boneImage.raycastTarget = false;
                boneImage.preserveAspect = true;

                TMP_Text fossilLabel = FindChild(boneImage.transform, "Fossil Label")?.GetComponent<TMP_Text>();
                if (fossilLabel != null)
                {
                    fossilLabel.gameObject.SetActive(false);
                }

                Transform notebookPaper = FindChild(notebookPage, "Notebook Paper");
                Image notebookImage = notebookPaper != null
                    ? EnsureImage(notebookPaper.gameObject)
                    : EnsureImageChild(notebookPage, "Notebook Paper");
                ConfigureCentered(notebookImage.rectTransform, new Vector2(0f, 0f), new Vector2(780f, 430f));
                notebookImage.color = Color.white;
                notebookImage.raycastTarget = false;
                notebookImage.preserveAspect = true;

                TMP_Text title = FindChild(measurementPage, "Title")?.GetComponent<TMP_Text>();
                TMP_Text notebookTitle = FindChild(notebookPage, "Notebook Title")?.GetComponent<TMP_Text>();

                SerializedObject serialized = new SerializedObject(minigame);
                SetObject(serialized, "_measurementBackgroundImage", backgroundImage);
                SetObject(serialized, "_measuredBoneImage", boneImage);
                SetObject(serialized, "_notebookImage", notebookImage);
                SetObject(serialized, "_titleLabel", title);
                SetObject(serialized, "_notebookTitleLabel", notebookTitle);
                SetBool(serialized, "_useBoundObjectDefinition", false);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PopupPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static GameObject CreateCanvasBasePrefab(Sprite background)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(PopupPrefabPath);
            if (source == null)
            {
                throw new InvalidOperationException($"Popup prefab not found at {PopupPrefabPath}");
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.name = "MinigamePopupCanvas_BoneMeasurement_Base";
            ConfigureMeasurementCanvas(
                instance,
                new BoneRecord(
                    "bone_measurement",
                    "Hueso",
                    string.Empty,
                    string.Empty,
                    MeasurementKind.Length,
                    MeasurementToolType.LinearTape,
                    "Mide el hueso y registra el resultado en la libreta.",
                    10f,
                    0.5f,
                    "cm")
                {
                    BackgroundSprite = background
                });

            PrefabUtility.SaveAsPrefabAsset(instance, CanvasBasePrefabPath);
            UnityEngine.Object.DestroyImmediate(instance);

            return AssetDatabase.LoadAssetAtPath<GameObject>(CanvasBasePrefabPath);
        }

        private static void CreateCanvasVariantPrefab(GameObject canvasBasePrefab, BoneRecord record)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(canvasBasePrefab);
            instance.name = $"MinigamePopupCanvas_{record.AssetName}";
            ConfigureMeasurementCanvas(instance, record);

            PrefabUtility.SaveAsPrefabAsset(instance, GetCanvasVariantPath(record));
            UnityEngine.Object.DestroyImmediate(instance);
        }

        private static void ConfigureMeasurementCanvas(GameObject root, BoneRecord record)
        {
            MeasurementMinigame minigame = root.GetComponentInChildren<MeasurementMinigame>(true);
            if (minigame == null)
            {
                throw new InvalidOperationException($"MeasurementMinigame not found in {root.name}");
            }

            Transform measurementPage = FindChild(minigame.transform, "Measurement Page");
            Transform notebookPage = FindChild(minigame.transform, "Field Notebook Page");
            if (measurementPage == null || notebookPage == null)
            {
                throw new InvalidOperationException($"Measurement pages not found in {root.name}.");
            }

            Image backgroundImage = EnsureImageChild(measurementPage, "Rock Background");
            ConfigureStretch(backgroundImage.rectTransform);
            backgroundImage.sprite = record.BackgroundSprite;
            backgroundImage.color = Color.white;
            backgroundImage.raycastTarget = false;
            backgroundImage.preserveAspect = false;
            backgroundImage.transform.SetAsFirstSibling();

            Transform placeholder = FindChild(measurementPage, "Measured Bone Image")
                ?? FindChild(measurementPage, "Measured Fossil Placeholder");
            Image boneImage = placeholder != null
                ? EnsureImage(placeholder.gameObject)
                : EnsureImageChild(measurementPage, "Measured Bone Image");
            boneImage.gameObject.name = "Measured Bone Image";
            ConfigureCentered(boneImage.rectTransform, new Vector2(0f, -10f), new Vector2(460f, 330f));
            boneImage.sprite = record.BoneSprite;
            boneImage.color = Color.white;
            boneImage.raycastTarget = false;
            boneImage.preserveAspect = true;
            boneImage.enabled = record.BoneSprite != null;

            TMP_Text fossilLabel = FindChild(boneImage.transform, "Fossil Label")?.GetComponent<TMP_Text>();
            if (fossilLabel != null)
            {
                fossilLabel.gameObject.SetActive(false);
            }

            Transform notebookPaper = FindChild(notebookPage, "Notebook Paper");
            Image notebookImage = notebookPaper != null
                ? EnsureImage(notebookPaper.gameObject)
                : EnsureImageChild(notebookPage, "Notebook Paper");
            ConfigureCentered(notebookImage.rectTransform, new Vector2(0f, 0f), new Vector2(780f, 430f));
            notebookImage.sprite = record.NotebookSprite;
            notebookImage.color = Color.white;
            notebookImage.raycastTarget = false;
            notebookImage.preserveAspect = true;

            TMP_Text title = FindChild(measurementPage, "Title")?.GetComponent<TMP_Text>();
            if (title != null)
            {
                title.text = $"Medicion: {record.DisplayName}";
            }

            TMP_Text notebookTitle = FindChild(notebookPage, "Notebook Title")?.GetComponent<TMP_Text>();
            if (notebookTitle != null)
            {
                notebookTitle.text = $"Libreta: {record.DisplayName}";
            }

            SerializedObject serialized = new SerializedObject(minigame);
            SetObject(serialized, "_measurementBackgroundImage", backgroundImage);
            SetObject(serialized, "_measuredBoneImage", boneImage);
            SetObject(serialized, "_notebookImage", notebookImage);
            SetObject(serialized, "_titleLabel", title);
            SetObject(serialized, "_notebookTitleLabel", notebookTitle);
            SetBool(serialized, "_useBoundObjectDefinition", false);

            SerializedProperty questions = RequireProperty(serialized, "_questions");
            questions.arraySize = 1;
            ConfigureMeasurementQuestion(questions.GetArrayElementAtIndex(0), record);

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureMeasurementQuestion(SerializedProperty property, BoneRecord record)
        {
            property.FindPropertyRelative("_id").stringValue = record.RecordId;
            property.FindPropertyRelative("_measurementKind").enumValueIndex = (int)record.MeasurementKind;
            property.FindPropertyRelative("_toolType").enumValueIndex = (int)record.ToolType;
            property.FindPropertyRelative("_customMeasurementLabel").stringValue = string.Empty;
            property.FindPropertyRelative("_prompt").stringValue = record.Prompt;
            property.FindPropertyRelative("_answerKind").enumValueIndex = (int)MeasurementAnswerKind.Number;
            property.FindPropertyRelative("_expectedNumber").floatValue = record.ExpectedNumber;
            property.FindPropertyRelative("_numberTolerance").floatValue = record.Tolerance;
            property.FindPropertyRelative("_expectedText").stringValue = string.Empty;
            property.FindPropertyRelative("_caseSensitive").boolValue = false;
            property.FindPropertyRelative("_unit").stringValue = record.Unit;
            property.FindPropertyRelative("_promptLabel").objectReferenceValue = null;
            property.FindPropertyRelative("_unitLabel").objectReferenceValue = null;
            property.FindPropertyRelative("_answerInput").objectReferenceValue = null;
            property.FindPropertyRelative("_measurementTool").objectReferenceValue = null;
        }

        private static GameObject CreateBasePrefabFromScene(MinigamePopupCanvas popupCanvasPrefab)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject source = FindRootOrChild(scene, "SpecialMinigame");
            if (source == null)
            {
                throw new InvalidOperationException($"SpecialMinigame object not found in {ScenePath}");
            }

            GameObject copy = UnityEngine.Object.Instantiate(source);
            copy.name = "SpecialMinigame_BoneMeasurement_Base";
            copy.transform.SetParent(null);
            ConfigureInteractable(copy, "bone_measurement_base", popupCanvasPrefab);

            PrefabUtility.SaveAsPrefabAsset(copy, SpecialMinigameBasePrefabPath);
            UnityEngine.Object.DestroyImmediate(copy);

            return AssetDatabase.LoadAssetAtPath<GameObject>(SpecialMinigameBasePrefabPath);
        }

        private static void CreateVariantPrefab(GameObject basePrefab, BoneRecord record, MinigamePopupCanvas popupCanvasPrefab)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            instance.name = $"SpecialMinigame_{record.AssetName}";
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            ConfigureInteractable(instance, record.RecordId, popupCanvasPrefab);

            string path = $"{PrefabFolder}/SpecialMinigame_{record.AssetName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(instance, path);
            UnityEngine.Object.DestroyImmediate(instance);
        }

        private static void ConfigureInteractable(GameObject root, string objectId, MinigamePopupCanvas popupCanvasPrefab)
        {
            MinigameObjectState state = root.GetComponent<MinigameObjectState>();
            if (state == null)
            {
                state = root.AddComponent<MinigameObjectState>();
            }

            SerializedObject stateObject = new SerializedObject(state);
            SetString(stateObject, "_objectId", objectId);
            stateObject.ApplyModifiedPropertiesWithoutUndo();

            MinigameInteractableObject interactable = root.GetComponent<MinigameInteractableObject>();
            if (interactable == null)
            {
                interactable = root.AddComponent<MinigameInteractableObject>();
            }

            SerializedObject interactableObject = new SerializedObject(interactable);
            SetObject(interactableObject, "_popupCanvas", null);
            SetObject(interactableObject, "_popupCanvasPrefab", popupCanvasPrefab);
            SetString(interactableObject, "_minigameId", "measurement");
            SetBool(interactableObject, "_openOnPlayerTouch", true);
            SetBool(interactableObject, "_openOnPointerDown", true);
            SetFloat(interactableObject, "_openCooldown", 0.35f);
            interactableObject.ApplyModifiedPropertiesWithoutUndo();

            CircleCollider2D touchCollider = root.GetComponent<CircleCollider2D>();
            if (touchCollider == null)
            {
                touchCollider = root.AddComponent<CircleCollider2D>();
            }

            touchCollider.isTrigger = true;
            touchCollider.offset = new Vector2(0f, -3.08f);
            touchCollider.radius = 2.35f;

            InteractableSparklePrompt[] prompts = root.GetComponentsInChildren<InteractableSparklePrompt>(true);
            for (int i = 0; i < prompts.Length; i++)
            {
                SerializedObject promptObject = new SerializedObject(prompts[i]);
                SetObject(promptObject, "_objectState", state);
                SetString(promptObject, "_minigameId", "measurement");
                SetObject(promptObject, "_camera", null);
                promptObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void ConfigureDefinition(GameObject root, BoneRecord record)
        {
            BoneMeasurementMinigameDefinition definition = root.GetComponent<BoneMeasurementMinigameDefinition>();
            if (definition == null)
            {
                definition = root.AddComponent<BoneMeasurementMinigameDefinition>();
            }

            SerializedObject serialized = new SerializedObject(definition);
            SetString(serialized, "_recordId", record.RecordId);
            SetString(serialized, "_displayName", record.DisplayName);
            SetString(serialized, "_measurementTitle", $"Medicion: {record.DisplayName}");
            SetString(serialized, "_notebookTitle", $"Libreta: {record.DisplayName}");
            SetEnum(serialized, "_measurementKind", (int)record.MeasurementKind);
            SetEnum(serialized, "_toolType", (int)record.ToolType);
            SetString(serialized, "_customMeasurementLabel", string.Empty);
            SetString(serialized, "_prompt", record.Prompt);
            SetEnum(serialized, "_answerKind", (int)MeasurementAnswerKind.Number);
            SetFloat(serialized, "_expectedNumber", record.ExpectedNumber);
            SetFloat(serialized, "_numberTolerance", record.Tolerance);
            SetString(serialized, "_expectedText", string.Empty);
            SetBool(serialized, "_caseSensitive", false);
            SetString(serialized, "_unit", record.Unit);
            SetObject(serialized, "_measurementBackgroundSprite", record.BackgroundSprite);
            SetObject(serialized, "_measuredBoneSprite", record.BoneSprite);
            SetObject(serialized, "_notebookSprite", record.NotebookSprite);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Sprite CopySprite(string sourceRelativePath, string destinationAssetPath)
        {
            string source = Path.Combine(SourceRoot, sourceRelativePath);
            if (!File.Exists(source))
            {
                throw new FileNotFoundException($"Source sprite not found: {source}", source);
            }

            string destination = ProjectPath(destinationAssetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            File.Copy(source, destination, true);
            AssetDatabase.ImportAsset(destinationAssetPath, ImportAssetOptions.ForceUpdate);
            ConfigureSpriteImporter(destinationAssetPath);
            return AssetDatabase.LoadAssetAtPath<Sprite>(destinationAssetPath);
        }

        private static void ConfigureSpriteImporter(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static Image EnsureImageChild(Transform parent, string name)
        {
            Transform existing = FindChild(parent, name);
            GameObject gameObject = existing != null
                ? existing.gameObject
                : new GameObject(name, typeof(RectTransform));

            if (existing == null)
            {
                gameObject.transform.SetParent(parent, false);
                gameObject.layer = parent.gameObject.layer;
            }

            return EnsureImage(gameObject);
        }

        private static Image EnsureImage(GameObject gameObject)
        {
            if (gameObject.GetComponent<CanvasRenderer>() == null)
            {
                gameObject.AddComponent<CanvasRenderer>();
            }

            Image image = gameObject.GetComponent<Image>();
            return image != null ? image : gameObject.AddComponent<Image>();
        }

        private static void ConfigureStretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void ConfigureCentered(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
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

        private static GameObject FindRootOrChild(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (string.Equals(roots[i].name, name, StringComparison.Ordinal))
                {
                    return roots[i];
                }

                Transform found = FindChild(roots[i].transform, name);
                if (found != null)
                {
                    return found.gameObject;
                }
            }

            return null;
        }

        private static void SetObject(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = RequireProperty(serialized, propertyName);
            property.objectReferenceValue = value;
        }

        private static void SetString(SerializedObject serialized, string propertyName, string value)
        {
            SerializedProperty property = RequireProperty(serialized, propertyName);
            property.stringValue = value ?? string.Empty;
        }

        private static void SetFloat(SerializedObject serialized, string propertyName, float value)
        {
            SerializedProperty property = RequireProperty(serialized, propertyName);
            property.floatValue = value;
        }

        private static void SetBool(SerializedObject serialized, string propertyName, bool value)
        {
            SerializedProperty property = RequireProperty(serialized, propertyName);
            property.boolValue = value;
        }

        private static void SetEnum(SerializedObject serialized, string propertyName, int value)
        {
            SerializedProperty property = RequireProperty(serialized, propertyName);
            property.enumValueIndex = value;
        }

        private static SerializedProperty RequireProperty(SerializedObject serialized, string propertyName)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Serialized property '{propertyName}' not found on {serialized.targetObject}.");
            }

            return property;
        }

        private static string ProjectPath(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
        }

        private static string GetCanvasVariantPath(BoneRecord record)
        {
            return $"{PrefabFolder}/MinigamePopupCanvas_{record.AssetName}.prefab";
        }

        private sealed class BoneRecord
        {
            public BoneRecord(
                string recordId,
                string displayName,
                string boneSourcePath,
                string notebookSourcePath,
                MeasurementKind measurementKind,
                MeasurementToolType toolType,
                string prompt,
                float expectedNumber,
                float tolerance,
                string unit)
            {
                RecordId = recordId;
                DisplayName = displayName;
                BoneSourcePath = boneSourcePath;
                NotebookSourcePath = notebookSourcePath;
                MeasurementKind = measurementKind;
                ToolType = toolType;
                Prompt = prompt;
                ExpectedNumber = expectedNumber;
                Tolerance = tolerance;
                Unit = unit;
                AssetName = ToAssetName(recordId);
            }

            public string RecordId { get; }
            public string DisplayName { get; }
            public string BoneSourcePath { get; }
            public string NotebookSourcePath { get; }
            public MeasurementKind MeasurementKind { get; }
            public MeasurementToolType ToolType { get; }
            public string Prompt { get; }
            public float ExpectedNumber { get; }
            public float Tolerance { get; }
            public string Unit { get; }
            public string AssetName { get; }
            public Sprite BackgroundSprite { get; set; }
            public Sprite BoneSprite { get; set; }
            public Sprite NotebookSprite { get; set; }

            private static string ToAssetName(string value)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return "Bone";
                }

                string[] parts = value.Split('_');
                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i].Length == 0)
                    {
                        continue;
                    }

                    parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1);
                }

                return string.Join(string.Empty, parts);
            }
        }
    }
}
