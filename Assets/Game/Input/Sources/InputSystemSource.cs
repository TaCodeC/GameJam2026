using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameJam.Input
{
    [AddComponentMenu("Game/Input/Sources/Input System Source")]
    public sealed class InputSystemSource : MonoBehaviour, IGameInputSource
    {
        [SerializeField] private InputActionAsset _actions;
        [SerializeField] private string _actionMapName = "Player";
        [SerializeField] private string _moveActionName = "Move";
        [SerializeField] private string _lookActionName = "Look";

        public event Action<GameAction> ActionPressed;
        public event Action<GameAction> ActionReleased;

        public Vector2 Move => ReadVector2(_moveAction);
        public Vector2 Look => ReadVector2(_lookAction);

        private readonly Dictionary<GameAction, InputAction> _gameActions = new();

        private InputActionMap _actionMap;
        private InputAction _moveAction;
        private InputAction _lookAction;
        private bool _isSubscribed;

        private void Awake()
        {
            ResolveActions();
        }

        private void OnEnable()
        {
            if (_actionMap == null)
                ResolveActions();

            SubscribeToActions();
            _actionMap?.Enable();
        }

        private void OnDisable()
        {
            UnsubscribeFromActions();
            _actionMap?.Disable();
        }

        public bool IsHeld(GameAction action)
        {
            return _gameActions.TryGetValue(action, out InputAction inputAction)
                && inputAction.IsPressed();
        }

        [ContextMenu("Resolve Input Actions")]
        public void ResolveActions()
        {
            bool wasSubscribed = _isSubscribed;
            if (wasSubscribed)
                UnsubscribeFromActions();

            _actionMap?.Disable();
            _gameActions.Clear();
            _actionMap = null;
            _moveAction = null;
            _lookAction = null;

            if (_actions == null)
            {
                Debug.LogWarning("[Input] Assign an InputActionAsset to InputSystemSource.", this);
                return;
            }

            _actionMap = _actions.FindActionMap(_actionMapName, false);
            if (_actionMap == null)
            {
                Debug.LogWarning($"[Input] Action map '{_actionMapName}' was not found.", this);
                return;
            }

            _moveAction = _actionMap.FindAction(_moveActionName, false);
            _lookAction = _actionMap.FindAction(_lookActionName, false);

            // El enum manda; si alguien renombra un action, empieza la diversion.
            foreach (GameAction action in Enum.GetValues(typeof(GameAction)))
            {
                InputAction inputAction = _actionMap.FindAction(action.ToString(), false);
                if (inputAction != null)
                {
                    _gameActions.Add(action, inputAction);
                }
                else
                {
                    Debug.LogWarning(
                        $"[Input] Action '{action}' was not found in map '{_actionMapName}'.",
                        this);
                }
            }

            if (wasSubscribed || isActiveAndEnabled)
            {
                SubscribeToActions();
                _actionMap.Enable();
            }
        }

        private void SubscribeToActions()
        {
            if (_isSubscribed)
                return;

            foreach (KeyValuePair<GameAction, InputAction> entry in _gameActions)
            {
                entry.Value.started += OnActionStarted;
                entry.Value.canceled += OnActionCanceled;
            }

            _isSubscribed = true;
        }

        private void UnsubscribeFromActions()
        {
            if (!_isSubscribed)
                return;

            foreach (KeyValuePair<GameAction, InputAction> entry in _gameActions)
            {
                entry.Value.started -= OnActionStarted;
                entry.Value.canceled -= OnActionCanceled;
            }

            _isSubscribed = false;
        }

        private void OnActionStarted(InputAction.CallbackContext context)
        {
            if (TryFindGameAction(context.action, out GameAction action))
                ActionPressed?.Invoke(action);
        }

        private void OnActionCanceled(InputAction.CallbackContext context)
        {
            if (TryFindGameAction(context.action, out GameAction action))
                ActionReleased?.Invoke(action);
        }

        private bool TryFindGameAction(InputAction inputAction, out GameAction action)
        {
            foreach (KeyValuePair<GameAction, InputAction> entry in _gameActions)
            {
                if (entry.Value == inputAction)
                {
                    action = entry.Key;
                    return true;
                }
            }

            action = default;
            return false;
        }

        private static Vector2 ReadVector2(InputAction action)
        {
            return action == null ? Vector2.zero : action.ReadValue<Vector2>();
        }
    }
}
