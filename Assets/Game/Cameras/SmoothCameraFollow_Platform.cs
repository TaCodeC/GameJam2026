using UnityEngine;

namespace GameJam.Cameras
{
    public sealed class SmoothCameraFollow : MonoBehaviour
    {
        [Header("Follow")]
        [SerializeField] private Transform _target;
        [Min(0.01f)]
        [SerializeField] private float _smoothTime = 0.2f;

        [Header("Position Constraints")]
        [SerializeField] private bool _constrainX;
        [SerializeField] private float _xMin;
        [SerializeField] private float _xMax;
        [SerializeField] private bool _constrainY;
        [SerializeField] private float _yMin;
        [SerializeField] private float _yMax;

        private Vector3 _offset;
        private Vector3 _velocity;

        private void Start()
        {
            if (_target != null)
                _offset = transform.position - _target.position;
        }

        private void LateUpdate()
        {
            if (_target == null)
                return;

            Vector3 targetPosition = _target.position + _offset;
            Vector3 nextPosition = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref _velocity,
                _smoothTime);
            transform.position = ApplyConstraints(nextPosition);
        }

        private Vector3 ApplyConstraints(Vector3 position)
        {
            if (_constrainX)
                position.x = ClampAxis(position.x, _xMin, _xMax, ref _velocity.x);

            if (_constrainY)
                position.y = ClampAxis(position.y, _yMin, _yMax, ref _velocity.y);

            return position;
        }

        private static float ClampAxis(float value, float limitA, float limitB, ref float velocity)
        {
            float min = Mathf.Min(limitA, limitB);
            float max = Mathf.Max(limitA, limitB);
            float clampedValue = Mathf.Clamp(value, min, max);

            if (!Mathf.Approximately(value, clampedValue))
                velocity = 0f;

            return clampedValue;
        }
    }
}
