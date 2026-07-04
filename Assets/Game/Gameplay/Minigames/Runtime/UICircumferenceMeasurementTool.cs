#pragma warning disable 0649

using TMPro;
using UnityEngine;

namespace GameJam.Gameplay.Minigames
{
    public sealed class UICircumferenceMeasurementTool : MeasurementToolBase
    {
        [SerializeField] private RectTransform _edgeAHandle;
        [SerializeField] private RectTransform _edgeBHandle;
        [SerializeField] private RectTransform _diameterLine;
        [SerializeField] private DropRadiusPreviewGraphic _circlePreview;
        [SerializeField] private TMP_Text _readout;
        [SerializeField] private float _pixelsPerUnit = 20f;
        [SerializeField] private string _unit = "cm";
        [SerializeField] private int _decimalPlaces = 1;
        [SerializeField] private bool _refreshEveryFrame;

        public float Diameter
        {
            get
            {
                if (_edgeAHandle == null || _edgeBHandle == null || _pixelsPerUnit <= 0f)
                {
                    return 0f;
                }

                return Vector2.Distance(_edgeAHandle.anchoredPosition, _edgeBHandle.anchoredPosition) / _pixelsPerUnit;
            }
        }

        public override float CurrentValue => Diameter * Mathf.PI;

        private void Awake()
        {
            ConfigureHandle(_edgeAHandle);
            ConfigureHandle(_edgeBHandle);
            Refresh();
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void LateUpdate()
        {
            if (_refreshEveryFrame)
            {
                Refresh();
            }
        }

        private void OnValidate()
        {
            _pixelsPerUnit = Mathf.Max(0.01f, _pixelsPerUnit);
            _decimalPlaces = Mathf.Max(0, _decimalPlaces);
        }

        public override void SetUnit(string unit)
        {
            if (!string.IsNullOrWhiteSpace(unit))
            {
                _unit = unit;
                Refresh();
            }
        }

        public override void Refresh()
        {
            if (_edgeAHandle == null || _edgeBHandle == null)
            {
                return;
            }

            UpdateDiameterLine();
            UpdateCirclePreview();
            UpdateReadout();
        }

        private void ConfigureHandle(RectTransform handle)
        {
            if (handle == null)
            {
                return;
            }

            MeasurementHandleDrag drag = handle.GetComponent<MeasurementHandleDrag>();
            if (drag == null)
            {
                drag = handle.gameObject.AddComponent<MeasurementHandleDrag>();
            }

            drag.Configure(Refresh);
        }

        private void UpdateDiameterLine()
        {
            if (_diameterLine == null)
            {
                return;
            }

            Vector2 start = _edgeAHandle.anchoredPosition;
            Vector2 end = _edgeBHandle.anchoredPosition;
            Vector2 delta = end - start;
            float distance = delta.magnitude;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

            _diameterLine.anchoredPosition = start + delta * 0.5f;
            _diameterLine.sizeDelta = new Vector2(distance, Mathf.Max(6f, _diameterLine.sizeDelta.y));
            _diameterLine.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void UpdateCirclePreview()
        {
            if (_circlePreview == null)
            {
                return;
            }

            Vector2 start = _edgeAHandle.anchoredPosition;
            Vector2 end = _edgeBHandle.anchoredPosition;
            float diameter = Vector2.Distance(start, end);

            RectTransform circleTransform = _circlePreview.RectTransform;
            _circlePreview.gameObject.SetActive(true);
            circleTransform.anchoredPosition = start + (end - start) * 0.5f;
            circleTransform.localRotation = Quaternion.identity;
            circleTransform.sizeDelta = Vector2.one * diameter;
            _circlePreview.Refresh();
        }

        private void UpdateReadout()
        {
            if (_readout == null)
            {
                return;
            }

            Vector2 start = _edgeAHandle.anchoredPosition;
            Vector2 end = _edgeBHandle.anchoredPosition;
            _readout.rectTransform.anchoredPosition = start + (end - start) * 0.5f + new Vector2(0f, 62f);
            _readout.text = $"Diam: {Diameter.ToString($"F{_decimalPlaces}")} {_unit} | Circ: {CurrentValue.ToString($"F{_decimalPlaces}")} {_unit}";
        }
    }
}
