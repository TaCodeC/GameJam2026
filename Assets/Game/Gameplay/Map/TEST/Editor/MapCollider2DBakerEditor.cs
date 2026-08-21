using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameJam.Gameplay.Map.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MapCollider2DBaker))]
    public sealed class MapCollider2DBakerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            DrawStats((MapCollider2DBaker)target);

            EditorGUILayout.Space();
            if (GUILayout.Button("Bake Colliders"))
            {
                BakeSelectedTargets();
            }

            if (GUILayout.Button("Clear Generated Colliders"))
            {
                ClearSelectedTargets();
            }
        }

        private void DrawStats(MapCollider2DBaker baker)
        {
            MapColliderBakeStats stats = baker.LastBakeStats;
            if (!stats.HasData)
            {
                EditorGUILayout.HelpBox("No collider bake data yet.", MessageType.None);
                return;
            }

            EditorGUILayout.HelpBox(
                $"Last bake: {stats.ColliderCount} colliders, {stats.SimplifiedPathPointCount} collider points. " +
                $"Source {stats.SourceWidth}x{stats.SourceHeight}, sampled {stats.SampledWidth}x{stats.SampledHeight}.",
                MessageType.Info);
        }

        private void BakeSelectedTargets()
        {
            for (int i = 0; i < targets.Length; i++)
            {
                MapCollider2DBaker baker = (MapCollider2DBaker)targets[i];
                Undo.RegisterFullObjectHierarchyUndo(baker.gameObject, "Bake Map Colliders");
                baker.BakeColliders();
                MarkDirty(baker);
            }
        }

        private void ClearSelectedTargets()
        {
            for (int i = 0; i < targets.Length; i++)
            {
                MapCollider2DBaker baker = (MapCollider2DBaker)targets[i];
                Undo.RegisterFullObjectHierarchyUndo(baker.gameObject, "Clear Map Colliders");
                baker.ClearGeneratedColliders();
                MarkDirty(baker);
            }
        }

        private static void MarkDirty(MapCollider2DBaker baker)
        {
            EditorUtility.SetDirty(baker);
            EditorUtility.SetDirty(baker.gameObject);

            if (!Application.isPlaying && baker.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(baker.gameObject.scene);
            }
        }
    }
}
