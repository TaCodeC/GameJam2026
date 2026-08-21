using UnityEngine;

namespace GameJam.Gameplay.Chat
{
    [DisallowMultipleComponent]
    public sealed class AluxeSmoothFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private bool _followOnStart = false;
        [SerializeField, Min(0f)] private float _behindDistance = 3.2f;
        [SerializeField] private float _verticalLift = 0.9f;
        [SerializeField, Min(0.1f)] private float _maxSpeed = 4.1f;
        [SerializeField, Min(0.1f)] private float _acceleration = 2.35f;
        [SerializeField, Min(0.01f)] private float _slowdownDistance = 1.5f;
        [SerializeField, Min(0f)] private float _movementDeadZone = 0.05f;
        [SerializeField] private bool _snapOnBegin = false;
        [SerializeField] private bool _preserveZ = true;
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private bool _lockVisualRotation = true;
        [SerializeField] private Vector3 _lockedVisualLocalEulerAngles = Vector3.zero;

        private Vector3 _velocity;
        private Vector3 _lastTargetPosition;
        private Rigidbody2D _targetRigidbody;
        private Vector2 _lastTargetMoveDirection = Vector2.right;
        private bool _isFollowing;
        private bool _hasLastTargetPosition;

        private void Awake()
        {
            ResolveVisualRoot();
            LockVisualRotation();
        }

        private void Start()
        {
            if (_followOnStart)
                BeginFollowing(_target);
        }

        private void LateUpdate()
        {
            if (!_isFollowing || _target == null)
                return;

            Vector3 desiredPosition = GetDesiredPosition();
            MoveToward(desiredPosition);

            LockVisualRotation();
            CacheTargetPosition();
        }

        public void BeginFollowing(Transform target)
        {
            if (target != null)
                _target = target;

            if (_target == null)
                _target = FindPlayer();

            _isFollowing = _target != null;
            _velocity = Vector3.zero;
            _targetRigidbody = _target == null ? null : _target.GetComponentInParent<Rigidbody2D>();
            CacheTargetPosition();

            if (_isFollowing && _snapOnBegin)
                transform.position = GetDesiredPosition();

            LockVisualRotation();
        }

        public void StopFollowing()
        {
            _isFollowing = false;
            _velocity = Vector3.zero;
        }

        private Vector3 GetDesiredPosition()
        {
            Vector2 movementDirection = GetTargetMovementDirection();
            Vector2 behindOffset = -movementDirection * _behindDistance + Vector2.up * _verticalLift;
            Vector3 targetPosition = _target.position + new Vector3(behindOffset.x, behindOffset.y, 0f);

            if (_preserveZ)
                targetPosition.z = transform.position.z;

            return targetPosition;
        }

        private void MoveToward(Vector3 desiredPosition)
        {
            Vector3 toDesired = desiredPosition - transform.position;
            float distance = toDesired.magnitude;
            if (distance <= Mathf.Epsilon)
            {
                _velocity = Vector3.zero;
                return;
            }

            float speedScale = Mathf.Clamp01(distance / _slowdownDistance);
            Vector3 desiredVelocity = toDesired / distance * (_maxSpeed * speedScale);
            _velocity = Vector3.MoveTowards(
                _velocity,
                desiredVelocity,
                _acceleration * Time.deltaTime);

            Vector3 movement = _velocity * Time.deltaTime;
            if (movement.sqrMagnitude >= toDesired.sqrMagnitude)
            {
                transform.position = desiredPosition;
                _velocity = Vector3.zero;
                return;
            }

            transform.position += movement;
        }

        private Vector2 GetTargetMovementDirection()
        {
            Vector2 movement = Vector2.zero;

            if (_targetRigidbody != null)
                movement = _targetRigidbody.linearVelocity;
            else if (_hasLastTargetPosition && _target != null)
                movement = (Vector2)(_target.position - _lastTargetPosition) / Mathf.Max(Time.deltaTime, 0.0001f);

            if (movement.sqrMagnitude > _movementDeadZone * _movementDeadZone)
                _lastTargetMoveDirection = movement.normalized;

            return _lastTargetMoveDirection;
        }

        private void CacheTargetPosition()
        {
            if (_target == null)
                return;

            _lastTargetPosition = _target.position;
            _hasLastTargetPosition = true;
        }

        private void ResolveVisualRoot()
        {
            if (_visualRoot == null && transform.childCount > 0)
                _visualRoot = transform.GetChild(0);
        }

        private void LockVisualRotation()
        {
            if (!_lockVisualRotation)
                return;

            ResolveVisualRoot();

            if (_visualRoot != null)
                _visualRoot.localRotation = Quaternion.Euler(_lockedVisualLocalEulerAngles);
        }

        private static Transform FindPlayer()
        {
            GameObject namedPlayer = GameObject.Find("Player");
            if (namedPlayer != null)
                return namedPlayer.transform;

            try
            {
                GameObject taggedPlayer = GameObject.FindWithTag("Player");
                return taggedPlayer != null ? taggedPlayer.transform : null;
            }
            catch (UnityException)
            {
                return null;
            }
        }
    }
}
