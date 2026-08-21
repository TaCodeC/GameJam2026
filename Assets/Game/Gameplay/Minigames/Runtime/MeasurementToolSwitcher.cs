#pragma warning disable 0649

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GameJam.Gameplay.Minigames
{
    public enum MeasurementToolType
    {
        LinearTape,
        Angle,
        Circumference,
        Custom
    }

    public sealed class MeasurementToolSwitcher : MonoBehaviour
    {
        [Serializable]
        public sealed class ToolBinding
        {
            [SerializeField] private MeasurementToolType _toolType = MeasurementToolType.LinearTape;
            [SerializeField] private string _id = "tool";
            [SerializeField] private GameObject _root;
            [SerializeField] private Button _button;
            [SerializeField] private MeasurementToolBase _tool;

            public MeasurementToolType ToolType => _toolType;
            public string Id => _id;
            public GameObject Root => _root;
            public Button Button => _button;

            public MeasurementToolBase GetTool()
            {
                if (_tool != null)
                {
                    return _tool;
                }

                return _root != null ? _root.GetComponentInChildren<MeasurementToolBase>(true) : null;
            }
        }

        [SerializeField] private ToolBinding[] _tools = Array.Empty<ToolBinding>();
        [SerializeField] private MeasurementToolType _defaultTool = MeasurementToolType.LinearTape;
        [SerializeField] private bool _selectDefaultOnEnable = true;
        [SerializeField] private bool _hideInactiveTools = true;
        [SerializeField] private Color _normalButtonColor = new(0.16f, 0.43f, 0.49f, 1f);
        [SerializeField] private Color _selectedButtonColor = new(0.34f, 0.67f, 0.58f, 1f);
        [SerializeField] private UnityEvent<MeasurementToolType> _toolChanged = new();

        private readonly List<UnityAction> _buttonActions = new();
        private int _currentToolIndex = -1;
        private string _currentUnit = "cm";
        private bool _buttonsBound;

        public MeasurementToolType CurrentToolType => IsValidIndex(_currentToolIndex)
            ? _tools[_currentToolIndex].ToolType
            : _defaultTool;

        public MeasurementToolBase CurrentTool => IsValidIndex(_currentToolIndex)
            ? _tools[_currentToolIndex].GetTool()
            : null;

        private void OnEnable()
        {
            BindButtons();

            if (_currentToolIndex >= 0)
            {
                SelectToolByIndex(_currentToolIndex);
            }
            else if (_selectDefaultOnEnable)
            {
                SelectTool(_defaultTool);
            }
        }

        private void OnDisable()
        {
            UnbindButtons();
        }

        public void SelectLinearTape()
        {
            SelectTool(MeasurementToolType.LinearTape);
        }

        public void SelectAngle()
        {
            SelectTool(MeasurementToolType.Angle);
        }

        public void SelectCircumference()
        {
            SelectTool(MeasurementToolType.Circumference);
        }

        public void SelectTool(MeasurementToolType toolType)
        {
            for (int i = 0; i < _tools.Length; i++)
            {
                if (_tools[i].ToolType == toolType)
                {
                    SelectToolByIndex(i);
                    return;
                }
            }

            Debug.LogWarning($"[Measurement] No measurement tool of type '{toolType}' is configured.", this);
        }

        public void SelectToolByIndex(int index)
        {
            if (!IsValidIndex(index))
            {
                Debug.LogWarning($"[Measurement] Invalid tool index {index}.", this);
                return;
            }

            for (int i = 0; i < _tools.Length; i++)
            {
                bool isSelected = i == index;
                ToolBinding binding = _tools[i];

                if (binding.Root != null && _hideInactiveTools)
                {
                    binding.Root.SetActive(isSelected);
                }

                ApplyButtonVisual(binding.Button, isSelected);
            }

            _currentToolIndex = index;
            ApplyUnit(_tools[index], _currentUnit);
            _toolChanged.Invoke(_tools[index].ToolType);
        }

        public void SetCurrentUnit(string unit)
        {
            if (!string.IsNullOrWhiteSpace(unit))
            {
                _currentUnit = unit;
            }

            if (IsValidIndex(_currentToolIndex))
            {
                ApplyUnit(_tools[_currentToolIndex], _currentUnit);
            }
        }

        private void BindButtons()
        {
            if (_buttonsBound)
            {
                return;
            }

            _buttonActions.Clear();

            for (int i = 0; i < _tools.Length; i++)
            {
                ToolBinding binding = _tools[i];
                if (binding.Button == null)
                {
                    _buttonActions.Add(null);
                    continue;
                }

                int toolIndex = i;
                UnityAction action = () => SelectToolByIndex(toolIndex);
                binding.Button.onClick.AddListener(action);
                _buttonActions.Add(action);
            }

            _buttonsBound = true;
        }

        private void UnbindButtons()
        {
            if (!_buttonsBound)
            {
                return;
            }

            for (int i = 0; i < _tools.Length && i < _buttonActions.Count; i++)
            {
                if (_tools[i].Button != null && _buttonActions[i] != null)
                {
                    _tools[i].Button.onClick.RemoveListener(_buttonActions[i]);
                }
            }

            _buttonActions.Clear();
            _buttonsBound = false;
        }

        private void ApplyUnit(ToolBinding binding, string unit)
        {
            MeasurementToolBase tool = binding.GetTool();
            if (tool == null)
            {
                return;
            }

            tool.SetUnit(unit);
            tool.Refresh();
        }

        private void ApplyButtonVisual(Button button, bool selected)
        {
            if (button == null || button.targetGraphic == null)
            {
                return;
            }

            button.targetGraphic.color = selected ? _selectedButtonColor : _normalButtonColor;
        }

        private bool IsValidIndex(int index)
        {
            return index >= 0 && index < _tools.Length;
        }
    }
}
