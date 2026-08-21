using UnityEngine;

namespace GameJam.Gameplay.Map
{
    [DisallowMultipleComponent]
    public sealed class MapAttentionMarker : MonoBehaviour
    {
        [SerializeField] private bool _visibleOnMap = true;
        [SerializeField] private Color _color = new Color(1f, 0.82f, 0.16f, 1f);
        [SerializeField, Min(1f)] private float _diameter = 16f;

        public bool VisibleOnMap => _visibleOnMap;
        public Color Color => _color;
        public float Diameter => _diameter;

        public void SetVisible(bool visible)
        {
            _visibleOnMap = visible;
        }

        public void SetColor(Color color)
        {
            _color = color;
        }

        public void SetDiameter(float diameter)
        {
            _diameter = Mathf.Max(1f, diameter);
        }
    }
}
