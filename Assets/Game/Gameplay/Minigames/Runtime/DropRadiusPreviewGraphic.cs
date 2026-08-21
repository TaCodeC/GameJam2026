#pragma warning disable 0649

using UnityEngine;
using UnityEngine.UI;

namespace GameJam.Gameplay.Minigames
{
    public sealed class DropRadiusPreviewGraphic : Graphic
    {
        [SerializeField] private Color _fillColor = new(0.04f, 0.95f, 1f, 0.16f);
        [SerializeField] private Color _outlineColor = new(0.04f, 0.95f, 1f, 1f);
        [SerializeField, Min(0f)] private float _outlineThickness = 6f;
        [SerializeField, Range(12, 128)] private int _segments = 96;

        public RectTransform RectTransform => rectTransform;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        public void SetStyle(Color fillColor, Color outlineColor, float outlineThickness, int segments)
        {
            _fillColor = fillColor;
            _outlineColor = outlineColor;
            _outlineThickness = Mathf.Max(0f, outlineThickness);
            _segments = Mathf.Clamp(segments, 12, 128);
            Refresh();
        }

        public void Refresh()
        {
            SetVerticesDirty();
            SetMaterialDirty();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();

            Rect rect = GetPixelAdjustedRect();
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
            if (radius <= 0f)
            {
                return;
            }

            Vector2 center = rect.center;
            int segmentCount = Mathf.Clamp(_segments, 12, 128);

            AddFill(vertexHelper, center, radius, segmentCount);
            AddOutline(vertexHelper, center, radius, segmentCount);
        }

        private void AddFill(VertexHelper vertexHelper, Vector2 center, float radius, int segmentCount)
        {
            if (_fillColor.a <= 0f)
            {
                return;
            }

            int centerIndex = vertexHelper.currentVertCount;
            vertexHelper.AddVert(center, _fillColor, Vector2.zero);

            for (int i = 0; i <= segmentCount; i++)
            {
                float angle = Mathf.PI * 2f * i / segmentCount;
                Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                vertexHelper.AddVert(point, _fillColor, Vector2.zero);
            }

            for (int i = 1; i <= segmentCount; i++)
            {
                vertexHelper.AddTriangle(centerIndex, centerIndex + i, centerIndex + i + 1);
            }
        }

        private void AddOutline(VertexHelper vertexHelper, Vector2 center, float radius, int segmentCount)
        {
            if (_outlineColor.a <= 0f || _outlineThickness <= 0f)
            {
                return;
            }

            float innerRadius = Mathf.Max(0f, radius - _outlineThickness);

            for (int i = 0; i < segmentCount; i++)
            {
                float startAngle = Mathf.PI * 2f * i / segmentCount;
                float endAngle = Mathf.PI * 2f * (i + 1) / segmentCount;
                Vector2 startDirection = new(Mathf.Cos(startAngle), Mathf.Sin(startAngle));
                Vector2 endDirection = new(Mathf.Cos(endAngle), Mathf.Sin(endAngle));

                int startIndex = vertexHelper.currentVertCount;
                vertexHelper.AddVert(center + startDirection * innerRadius, _outlineColor, Vector2.zero);
                vertexHelper.AddVert(center + startDirection * radius, _outlineColor, Vector2.zero);
                vertexHelper.AddVert(center + endDirection * radius, _outlineColor, Vector2.zero);
                vertexHelper.AddVert(center + endDirection * innerRadius, _outlineColor, Vector2.zero);

                vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
                vertexHelper.AddTriangle(startIndex, startIndex + 2, startIndex + 3);
            }
        }
    }
}
