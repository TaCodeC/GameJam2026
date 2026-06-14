using UnityEngine;

namespace GameJam.Gameplay.Map
{
    [DefaultExecutionOrder(1000)]
    [DisallowMultipleComponent]
    public sealed class MapWalkabilityConstraint : MonoBehaviour
    {
        [SerializeField] private MapDiscoverySystem _map;
        [SerializeField] private Transform _target;
        [SerializeField, Min(0.01f)] private float _segmentSampleSpacing = 0.1f;

        private Vector3 _lastWalkablePosition;
        private bool _hasWalkablePosition;

        private void Awake()
        {
            if (_target == null)
            {
                _target = transform;
            }
        }

        private void OnEnable()
        {
            _hasWalkablePosition = false;
        }

        private void LateUpdate()
        {
            if (_map == null || !_map.IsInitialized || _target == null)
            {
                return;
            }

            Vector3 currentPosition = _target.position;
            if (!_hasWalkablePosition)
            {
                if (_map.IsWalkable(currentPosition))
                {
                    _lastWalkablePosition = currentPosition;
                    _hasWalkablePosition = true;
                }

                return;
            }

            if (_map.CanTraverseSegment(_lastWalkablePosition, currentPosition, _segmentSampleSpacing))
            {
                _lastWalkablePosition = currentPosition;
            }
            else
            {
                _target.position = _lastWalkablePosition;
            }
        }
    }
}
