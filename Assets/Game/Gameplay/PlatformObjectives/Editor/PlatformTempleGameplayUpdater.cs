using System;
using System.Linq;
using GameJam.Creatures;
using GameJam.Creatures.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameJam.Gameplay.PlatformObjectives.Editor
{
    public static class PlatformTempleGameplayUpdater
    {
        private const string PlatformScenePath = "Assets/Scenes/Platform.unity";
        private const string HousePrefabPath = "Assets/Game/Gameplay/PlatformObjectives/Prefabs/CasitaAluxGate.prefab";
        private const string GonfoterioName = "Gonfoterio";

        [MenuItem("GameJam/Platform/Apply Naia, Gonfoterio and Temple Updates")]
        public static void ApplyAll()
        {
            PlatformNaiaPrefabBuilder.Build();
            ConfigureHousePrefab();

            Scene scene = EditorSceneManager.OpenScene(PlatformScenePath, OpenSceneMode.Single);
            ConfigureSceneGonfoterio(scene);
            ConfigureSceneNaiaVisibility(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PlatformTempleGameplayUpdater] Naia, Gonfoterio and temple progression updated.");
        }

        private static void ConfigureHousePrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(HousePrefabPath);
            try
            {
                PlatformAluxHouseGate gate = root.GetComponent<PlatformAluxHouseGate>();
                SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
                if (gate == null || renderer == null)
                    throw new InvalidOperationException("CasitaAluxGate needs PlatformAluxHouseGate and SpriteRenderer.");

                SerializedObject serializedGate = new SerializedObject(gate);
                serializedGate.FindProperty("_completeMessage").stringValue = "Listo! mi amiga Naia me ayudó a construir";
                serializedGate.FindProperty("_completeMessageDuration").floatValue = 3f;
                serializedGate.FindProperty("_showCompleteMessageNearAluxe").boolValue = true;
                serializedGate.FindProperty("_waitForCompleteMessageBeforeTransition").boolValue = true;
                serializedGate.FindProperty("_templeRenderer").objectReferenceValue = renderer;
                serializedGate.FindProperty("_autoFindNaia").boolValue = true;
                serializedGate.FindProperty("_fadeToCinematicDuration").floatValue = 3f;
                serializedGate.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, HousePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureSceneGonfoterio(Scene scene)
        {
            GameObject gonfoterio = FindInScene(scene, GonfoterioName);
            if (gonfoterio == null)
                throw new InvalidOperationException("Gonfoterio was not found in Platform scene.");

            AnimalInfoInteractable info = gonfoterio.GetComponent<AnimalInfoInteractable>();
            SpriteRenderer visual = gonfoterio.GetComponentInChildren<SpriteRenderer>(true);
            BoxCollider2D infoCollider = visual != null ? visual.GetComponent<BoxCollider2D>() : null;
            if (infoCollider == null)
            {
                infoCollider = gonfoterio.GetComponentsInChildren<BoxCollider2D>(true)
                    .Where(collider => collider.transform != gonfoterio.transform)
                    .OrderByDescending(ColliderArea)
                    .FirstOrDefault();
            }

            BoxCollider2D hazardCollider = gonfoterio.GetComponents<BoxCollider2D>()
                .Where(collider => collider.enabled)
                .OrderBy(ColliderArea)
                .FirstOrDefault();

            if (infoCollider == null || hazardCollider == null)
                throw new InvalidOperationException("The scene Gonfoterio needs an info collider on Visual and a hazard collider on its root.");

            hazardCollider.isTrigger = true;
            infoCollider.isTrigger = true;

            if (info != null)
            {
                SerializedObject serializedInfo = new SerializedObject(info);
                serializedInfo.FindProperty("_clickTarget").objectReferenceValue = infoCollider;
                serializedInfo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void ConfigureSceneNaiaVisibility(Scene scene)
        {
            NaiaBlinkAnimator naia = FindNaiaInScene(scene);
            PlatformAluxHouseGate gate = FindComponentInScene<PlatformAluxHouseGate>(scene);
            if (naia == null || gate == null)
                throw new InvalidOperationException("Platform scene needs both Naia and CasitaAluxGate.");

            SerializedObject serializedGate = new SerializedObject(gate);
            serializedGate.FindProperty("_naiaRoot").objectReferenceValue = naia.gameObject;
            serializedGate.FindProperty("_autoFindNaia").boolValue = true;
            serializedGate.ApplyModifiedPropertiesWithoutUndo();

            naia.gameObject.SetActive(false);
        }

        private static NaiaBlinkAnimator FindNaiaInScene(Scene scene)
        {
            NaiaBlinkAnimator fallback = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (NaiaBlinkAnimator naia in root.GetComponentsInChildren<NaiaBlinkAnimator>(true))
                {
                    fallback ??= naia;
                    if (naia.gameObject.name == "Naia")
                        return naia;
                }
            }

            return fallback;
        }

        private static float ColliderArea(BoxCollider2D collider)
        {
            return collider.size.x * collider.size.y;
        }

        private static T FindComponentInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                    return component;
            }

            return null;
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
    }
}
