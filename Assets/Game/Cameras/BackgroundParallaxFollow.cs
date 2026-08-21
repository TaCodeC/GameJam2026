using UnityEngine;

namespace GameJam.Cameras
{
    [DisallowMultipleComponent]
    public sealed class BackgroundParallaxFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private bool _autoFindPlayer = true;
        [SerializeField, Range(0f, 1f)] private float _followFactor = 0.2f;
        [SerializeField] private bool _followX = true;
        [SerializeField] private bool _followY = true;

        private Vector3 _initialPosition;
        private Vector3 _initialTargetPosition;
        private bool _hasReferencePose;

        private void OnEnable()
        {
            ResolveTarget();
            CaptureReferencePose();
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                ResolveTarget();
                CaptureReferencePose();
            }

            if (_target == null || !_hasReferencePose)
                return;

            Vector3 targetDelta = _target.position - _initialTargetPosition;
            Vector3 nextPosition = _initialPosition;

            if (_followX)
                nextPosition.x += targetDelta.x * _followFactor;

            if (_followY)
                nextPosition.y += targetDelta.y * _followFactor;

            transform.position = nextPosition;
        }

        private void ResolveTarget()
        {
            if (_target != null || !_autoFindPlayer)
                return;

            GameObject player = GameObject.Find("PlatformPlayer");
            if (player == null)
                player = GameObject.Find("Player");

            if (player == null)
            {
                try
                {
                    player = GameObject.FindWithTag("Player");
                }
                catch (UnityException)
                {
                    player = null;
                }
            }

            _target = player != null ? player.transform : null;
        }

        private void CaptureReferencePose()
        {
            if (_target == null)
            {
                _hasReferencePose = false;
                return;
            }

            _initialPosition = transform.position;
            _initialTargetPosition = _target.position;
            _hasReferencePose = true;
        }

        private void OnValidate()
        {
            _followFactor = Mathf.Clamp01(_followFactor);
        }
    }
}
