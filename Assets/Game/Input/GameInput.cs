using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameJam.Input
{
    [AddComponentMenu("Game/Input/Game Input")]
    public sealed class GameInput : MonoBehaviour, IGameInput
    {
        [Tooltip("Components that implement IGameInputSource. Leave empty to find them on this object and its children.")]
        [SerializeField] private MonoBehaviour[] _sourceBehaviours = Array.Empty<MonoBehaviour>();

        public event Action<GameAction> ActionPressed;
        public event Action<GameAction> ActionReleased;

        public Vector2 Move
        {
            get
            {
                Vector2 value = Vector2.zero;

                foreach (IGameInputSource source in _sources)
                    value += source.Move;

                return Vector2.ClampMagnitude(value, 1f);
            }
        }

        public Vector2 Look
        {
            get
            {
                Vector2 value = Vector2.zero;

                foreach (IGameInputSource source in _sources)
                    value += source.Look;

                return value;
            }
        }

        private readonly List<IGameInputSource> _sources = new();
        private bool _isSubscribed;

        private void Awake()
        {
            RebuildSources();
        }

        private void OnEnable()
        {
            SubscribeToSources();
        }

        private void OnDisable()
        {
            UnsubscribeFromSources();
        }

        public bool IsHeld(GameAction action)
        {
            foreach (IGameInputSource source in _sources)
            {
                if (source.IsHeld(action))
                    return true;
            }

            return false;
        }

        [ContextMenu("Rebuild Input Sources")]
        public void RebuildSources()
        {
            bool wasSubscribed = _isSubscribed;
            if (wasSubscribed)
                UnsubscribeFromSources();

            _sources.Clear();

            if (_sourceBehaviours.Length > 0)
            {
                foreach (MonoBehaviour behaviour in _sourceBehaviours)
                    AddSource(behaviour);
            }
            else
            {
                // Porque conectar las fuentes a mano era demasiado sencillo.
                foreach (MonoBehaviour behaviour in GetComponentsInChildren<MonoBehaviour>(true))
                    AddSource(behaviour);
            }

            if (wasSubscribed)
                SubscribeToSources();
        }

        private void AddSource(MonoBehaviour behaviour)
        {
            if (behaviour == null)
                return;

            if (behaviour is not IGameInputSource source)
            {
                if (_sourceBehaviours.Length > 0)
                    Debug.LogWarning($"{behaviour.name} does not implement IGameInputSource.", behaviour);

                return;
            }

            if (!_sources.Contains(source))
                _sources.Add(source);
        }

        private void SubscribeToSources()
        {
            if (_isSubscribed)
                return;

            foreach (IGameInputSource source in _sources)
            {
                source.ActionPressed += OnActionPressed;
                source.ActionReleased += OnActionReleased;
            }

            _isSubscribed = true;
        }

        private void UnsubscribeFromSources()
        {
            if (!_isSubscribed)
                return;

            foreach (IGameInputSource source in _sources)
            {
                source.ActionPressed -= OnActionPressed;
                source.ActionReleased -= OnActionReleased;
            }

            _isSubscribed = false;
        }

        private void OnActionPressed(GameAction action)
        {
            ActionPressed?.Invoke(action);
        }

        private void OnActionReleased(GameAction action)
        {
            ActionReleased?.Invoke(action);
        }
    }
}
