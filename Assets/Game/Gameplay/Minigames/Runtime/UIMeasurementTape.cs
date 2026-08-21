#pragma warning disable 0649

using TMPro;
using UnityEngine;

namespace GameJam.Gameplay.Minigames
{
    public sealed class UIMeasurementTape : MeasurementToolBase
    {
        [SerializeField] private RectTransform _startHandle;
        [SerializeField] private RectTransform _endHandle;
        [SerializeField] private RectTransform _tapeBody;
        [SerializeField] private TMP_Text _readout;
        [SerializeField] private float _pixelsPerUnit = 20f;
        [SerializeField] private string _unit = "cm";
        [SerializeField] private int _decimalPlaces = 1;
        [SerializeField] private bool _refreshEveryFrame;

        public override float CurrentValue
        {
            get
            {
                if (_startHandle == null || _endHandle == null || _pixelsPerUnit <= 0f)
                {
                    return 0f;
                }

                return Vector2.Distance(_startHandle.anchoredPosition, _endHandle.anchoredPosition) / _pixelsPerUnit;
            }
        }

        private void Awake()
        {
            ConfigureHandle(_startHandle);
            ConfigureHandle(_endHandle);
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
            if (_startHandle == null || _endHandle == null)
            {
                return;
            }

            UpdateTapeBody();
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

            drag.Configure(this);
        }

        private void UpdateTapeBody()
        {
            if (_tapeBody == null)
            {
                return;
            }

            Vector2 start = _startHandle.anchoredPosition;
            Vector2 end = _endHandle.anchoredPosition;
            Vector2 delta = end - start;
            float distance = delta.magnitude;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

            _tapeBody.anchoredPosition = start + delta * 0.5f;
            _tapeBody.sizeDelta = new Vector2(distance, Mathf.Max(6f, _tapeBody.sizeDelta.y));
            _tapeBody.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void UpdateReadout()
        {
            if (_readout == null)
            {
                return;
            }

            Vector2 start = _startHandle.anchoredPosition;
            Vector2 end = _endHandle.anchoredPosition;
            _readout.rectTransform.anchoredPosition = start + (end - start) * 0.5f + new Vector2(0f, 28f);
            _readout.text = $"{CurrentValue.ToString($"F{_decimalPlaces}")} {_unit}";
        }
    }
}
