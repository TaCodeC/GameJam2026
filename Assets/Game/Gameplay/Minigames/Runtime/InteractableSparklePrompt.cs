#pragma warning disable 0649

using UnityEngine;

namespace GameJam.Gameplay.Minigames
{
    public sealed class InteractableSparklePrompt : MonoBehaviour
    {
        [SerializeField] private MinigameObjectState _objectState;
        [SerializeField] private string _minigameId = "measurement";
        [SerializeField] private Renderer _promptRenderer;
        [SerializeField] private bool _hideWhenCompleted = true;
        [SerializeField] private bool _showWhenNoState = true;
        [SerializeField] private bool _billboardToCamera = true;
        [SerializeField] private Camera _camera;
        [SerializeField] private bool _applyLocalOffset = true;
        [SerializeField] private Vector3 _localOffset = new(0f, 0.4f, 0f);
        [SerializeField, Min(0f)] private float _bobAmplitude = 0.04f;
        [SerializeField, Min(0f)] private float _bobSpeed = 2.2f;

        private Vector3 _initialLocalPosition;
        private bool _hasInitialLocalPosition;

        private void Awake()
        {
            CacheReferences();
            CacheInitialPosition();
        }

        private void OnEnable()
        {
            CacheReferences();
            CacheInitialPosition();
            RefreshVisibility();
        }

        private void LateUpdate()
        {
            RefreshVisibility();

            if (_promptRenderer != null && !_promptRenderer.enabled)
            {
                return;
            }

            ApplyMotion();
            FaceCamera();
        }

        private void OnValidate()
        {
            CacheReferences();
            _bobAmplitude = Mathf.Max(0f, _bobAmplitude);
            _bobSpeed = Mathf.Max(0f, _bobSpeed);
        }

        public void SetObjectState(MinigameObjectState objectState)
        {
            _objectState = objectState;
            RefreshVisibility();
        }

        public void SetMinigameId(string minigameId)
        {
            _minigameId = minigameId;
            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            if (_promptRenderer == null)
            {
                return;
            }

            _promptRenderer.enabled = ShouldShowPrompt();
        }

        private bool ShouldShowPrompt()
        {
            if (!_hideWhenCompleted)
            {
                return true;
            }

            if (_objectState == null)
            {
                return _showWhenNoState;
            }

            return !_objectState.IsCompleted(_minigameId);
        }

        private void ApplyMotion()
        {
            if (!_applyLocalOffset || !_hasInitialLocalPosition)
            {
                return;
            }

            float bob = _bobAmplitude > 0f && _bobSpeed > 0f
                ? Mathf.Sin(Time.time * _bobSpeed) * _bobAmplitude
                : 0f;

            transform.localPosition = _initialLocalPosition + _localOffset + Vector3.up * bob;
        }

        private void FaceCamera()
        {
            if (!_billboardToCamera)
            {
                return;
            }

            Camera targetCamera = _camera != null ? _camera : Camera.main;
            if (targetCamera == null)
            {
                return;
            }

            Vector3 direction = transform.position - targetCamera.transform.position;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(direction, targetCamera.transform.up);
        }

        private void CacheReferences()
        {
            if (_promptRenderer == null)
            {
                _promptRenderer = GetComponent<Renderer>();
            }

            if (_objectState == null)
            {
                _objectState = GetComponentInParent<MinigameObjectState>();
            }
        }

        private void CacheInitialPosition()
        {
            if (_hasInitialLocalPosition)
            {
                return;
            }

            _initialLocalPosition = transform.localPosition;
            _hasInitialLocalPosition = true;
        }
    }
}
