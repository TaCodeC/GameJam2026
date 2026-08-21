using System;
using System.Collections.Generic;
using DynControls;
using UnityEngine;

namespace GameJam.Input
{
    [AddComponentMenu("Game/Input/Sources/Mobile Input Source")]
    public sealed class MobileInputSource : MonoBehaviour, IGameInputSource
    {
        [Serializable]
        private struct ActionButton
        {
            public GameAction Action;
            public MobileActionButton Button;
        }

        [Header("Joysticks")]
        [SerializeField] private VirtualJoystick _moveJoystick;
        [SerializeField] private VirtualJoystick _lookJoystick;

        [Header("Buttons")]
        [SerializeField] private ActionButton[] _actionButtons = Array.Empty<ActionButton>();

        public event Action<GameAction> ActionPressed;
        public event Action<GameAction> ActionReleased;

        public Vector2 Move => _moveJoystick == null ? Vector2.zero : _moveJoystick.Value;
        public Vector2 Look => _lookJoystick == null ? Vector2.zero : _lookJoystick.Value;

        private readonly Dictionary<MobileActionButton, GameAction> _actionsByButton = new();

        private void OnEnable()
        {
            BuildButtonMap();
        }

        private void OnDisable()
        {
            UnsubscribeFromButtons();
        }

        public bool IsHeld(GameAction action)
        {
            foreach (ActionButton binding in _actionButtons)
            {
                if (binding.Action == action && binding.Button != null && binding.Button.IsHeld)
                    return true;
            }

            return false;
        }

        [ContextMenu("Rebuild Button Map")]
        public void BuildButtonMap()
        {
            UnsubscribeFromButtons();
            _actionsByButton.Clear();

            foreach (ActionButton binding in _actionButtons)
            {
                if (binding.Button == null)
                    continue;

                if (!_actionsByButton.TryAdd(binding.Button, binding.Action))
                {
                    Debug.LogWarning(
                        $"[Input] Button '{binding.Button.name}' has more than one action.",
                        binding.Button);
                    continue;
                }

                binding.Button.Pressed += OnButtonPressed;
                binding.Button.Released += OnButtonReleased;
            }
        }

        private void UnsubscribeFromButtons()
        {
            foreach (MobileActionButton button in _actionsByButton.Keys)
            {
                if (button == null)
                    continue;

                button.Pressed -= OnButtonPressed;
                button.Released -= OnButtonReleased;
            }
        }

        private void OnButtonPressed(MobileActionButton button)
        {
            if (_actionsByButton.TryGetValue(button, out GameAction action))
                ActionPressed?.Invoke(action);
        }

        private void OnButtonReleased(MobileActionButton button)
        {
            if (_actionsByButton.TryGetValue(button, out GameAction action))
                ActionReleased?.Invoke(action);
        }
    }
}
