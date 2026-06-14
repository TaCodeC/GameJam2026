using UnityEngine;

namespace GameJam.Cameras
{
    public sealed class SmoothCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [Min(0.01f)]
        [SerializeField] private float _smoothTime = 0.2f;

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
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref _velocity,
                _smoothTime);
        }
    }
}
