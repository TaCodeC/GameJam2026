#pragma warning disable 0649

using TMPro;
using UnityEngine;

namespace GameJam.Gameplay.Minigames
{
    public sealed class UIAngleMeasurementTool : MeasurementToolBase
    {
        [SerializeField] private RectTransform _vertexHandle;
        [SerializeField] private RectTransform _firstArmHandle;
        [SerializeField] private RectTransform _secondArmHandle;
        [SerializeField] private RectTransform _firstArm;
        [SerializeField] private RectTransform _secondArm;
        [SerializeField] private TMP_Text _readout;
        [SerializeField] private string _unit = "grados";
        [SerializeField] private int _decimalPlaces;
        [SerializeField] private bool _refreshEveryFrame;

        public override float CurrentValue
        {
            get
            {
                if (_vertexHandle == null || _firstArmHandle == null || _secondArmHandle == null)
                {
                    return 0f;
                }

                Vector2 first = _firstArmHandle.anchoredPosition - _vertexHandle.anchoredPosition;
                Vector2 second = _secondArmHandle.anchoredPosition - _vertexHandle.anchoredPosition;
                if (first.sqrMagnitude <= Mathf.Epsilon || second.sqrMagnitude <= Mathf.Epsilon)
                {
                    return 0f;
                }

                return Vector2.Angle(first, second);
            }
        }

        private void Awake()
        {
            ConfigureHandle(_vertexHandle);
            ConfigureHandle(_firstArmHandle);
            ConfigureHandle(_secondArmHandle);
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
            if (_vertexHandle == null || _firstArmHandle == null || _secondArmHandle == null)
            {
                return;
            }

            UpdateArm(_firstArm, _vertexHandle.anchoredPosition, _firstArmHandle.anchoredPosition);
            UpdateArm(_secondArm, _vertexHandle.anchoredPosition, _secondArmHandle.anchoredPosition);
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

        private static void UpdateArm(RectTransform arm, Vector2 start, Vector2 end)
        {
            if (arm == null)
            {
                return;
            }

            Vector2 delta = end - start;
            float distance = delta.magnitude;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

            arm.anchoredPosition = start + delta * 0.5f;
            arm.sizeDelta = new Vector2(distance, Mathf.Max(6f, arm.sizeDelta.y));
            arm.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void UpdateReadout()
        {
            if (_readout == null)
            {
                return;
            }

            Vector2 first = (_firstArmHandle.anchoredPosition - _vertexHandle.anchoredPosition).normalized;
            Vector2 second = (_secondArmHandle.anchoredPosition - _vertexHandle.anchoredPosition).normalized;
            Vector2 labelDirection = first + second;
            if (labelDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                labelDirection = Vector2.up;
            }

            _readout.rectTransform.anchoredPosition = _vertexHandle.anchoredPosition + labelDirection.normalized * 56f;
            _readout.text = $"{CurrentValue.ToString($"F{_decimalPlaces}")} {_unit}";
        }
    }
}
