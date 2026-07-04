using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameJam.Gameplay.PlatformObstacles
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class PlatformStaticTileSpawnZone : MonoBehaviour
    {
        [SerializeField] private string _label = "Spawn Zone";
        [SerializeField] private Vector2 _size = new Vector2(8f, 2f);
        [SerializeField] private Color _fillColor = new Color(1f, 0f, 0f, 0.14f);
        [SerializeField] private Color _outlineColor = new Color(1f, 0f, 0f, 0.9f);
        [SerializeField] private bool _drawAlways = true;

        public string Label => _label;
        public Vector2 Size => _size;

        public Vector3 GetRandomWorldPoint(System.Random random)
        {
            float x = NextRange(random, -_size.x * 0.5f, _size.x * 0.5f);
            float y = NextRange(random, -_size.y * 0.5f, _size.y * 0.5f);
            return transform.TransformPoint(new Vector3(x, y, 0f));
        }

#if UNITY_EDITOR
        public void EditorConfigure(string label, Vector2 size, Color fillColor, Color outlineColor)
        {
            _label = label;
            _size = size;
            _fillColor = fillColor;
            _outlineColor = outlineColor;
        }
#endif

        private void OnValidate()
        {
            _size.x = Mathf.Max(0.01f, _size.x);
            _size.y = Mathf.Max(0.01f, _size.y);
        }

        private void OnDrawGizmos()
        {
            if (_drawAlways)
                DrawGizmo();
        }

        private void OnDrawGizmosSelected()
        {
            DrawGizmo();
        }

        private void DrawGizmo()
        {
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color previousColor = Gizmos.color;

            Gizmos.matrix = transform.localToWorldMatrix;
            Vector3 cubeSize = new Vector3(_size.x, _size.y, 0.05f);

            Gizmos.color = _fillColor;
            Gizmos.DrawCube(Vector3.zero, cubeSize);

            Gizmos.color = _outlineColor;
            Gizmos.DrawWireCube(Vector3.zero, cubeSize);

            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;

#if UNITY_EDITOR
            Handles.color = _outlineColor;
            Handles.Label(transform.position + transform.up * (_size.y * 0.55f), _label);
#endif
        }

        private static float NextRange(System.Random random, float min, float max)
        {
            if (random == null)
                return min;

            return Mathf.Lerp(min, max, (float)random.NextDouble());
        }
    }
}
