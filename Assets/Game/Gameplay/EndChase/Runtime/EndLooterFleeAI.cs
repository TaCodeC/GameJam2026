using GameJam.Gameplay.Map;
using GameJam.Player;
using UnityEngine;

namespace GameJam.Gameplay.EndChase
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class EndLooterFleeAI : MonoBehaviour
    {
        private static readonly float[] CandidateAngles =
        {
            0f, 18f, -18f, 38f, -38f, 62f, -62f, 92f, -92f, 128f, -128f, 180f
        };

        [SerializeField] private Transform _player;
        [SerializeField] private MapDiscoverySystem _map;
        [SerializeField, Min(0.1f)] private float _moveSpeed = 3.75f;
        [SerializeField, Min(0.1f)] private float _lookAheadDistance = 3.2f;
        [SerializeField, Min(0.01f)] private float _repathInterval = 0.2f;
        [SerializeField, Min(0.01f)] private float _mapSampleSpacing = 0.22f;
        [SerializeField] private LayerMask _obstacleMask = ~0;

        private readonly RaycastHit2D[] _castHits = new RaycastHit2D[8];
        private Rigidbody2D _rigidbody;
        private Animator _animator;
        private Transform _visualRoot;
        private SpriteRenderer _spriteRenderer;
        private Vector2 _currentDirection = Vector2.right;
        private float _nextRepathTime;
        private bool _visualFlipped;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _animator = GetComponent<Animator>();
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            _visualRoot = _spriteRenderer != null ? _spriteRenderer.transform : transform;

            _rigidbody.bodyType = RigidbodyType2D.Dynamic;
            _rigidbody.gravityScale = 0f;
            _rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
            _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            if (_spriteRenderer != null)
                _visualFlipped = _spriteRenderer.flipY;
        }

        private void OnDisable()
        {
            if (_rigidbody != null)
                _rigidbody.linearVelocity = Vector2.zero;
        }

        private void FixedUpdate()
        {
            if (_player == null)
            {
                _rigidbody.linearVelocity = Vector2.zero;
                UpdateAnimation(Vector2.zero);
                return;
            }

            Vector2 current = _rigidbody.position;
            Vector2 awayFromPlayer = current - (Vector2)_player.position;
            if (awayFromPlayer.sqrMagnitude <= 0.0001f)
                awayFromPlayer = _currentDirection.sqrMagnitude > 0.0001f ? _currentDirection : Vector2.right;

            Vector2 preferred = awayFromPlayer.normalized;
            bool shouldRepath = Time.time >= _nextRepathTime || !CanMove(_currentDirection, _lookAheadDistance);
            if (shouldRepath)
            {
                _currentDirection = ChooseEscapeDirection(preferred);
                _nextRepathTime = Time.time + _repathInterval;
            }

            _rigidbody.linearVelocity = _currentDirection * _moveSpeed;
            UpdateAnimation(_currentDirection);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TurnAwayFromCollision(collision);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            TurnAwayFromCollision(collision);
        }

        public void Configure(Transform player, MapDiscoverySystem map, float moveSpeed)
        {
            _player = player;
            _map = map;
            _moveSpeed = Mathf.Max(0.1f, moveSpeed);
        }

        private void TurnAwayFromCollision(Collision2D collision)
        {
            if (collision == null || collision.collider == null || IsPlayerCollider(collision.collider))
                return;

            Vector2 normal = Vector2.zero;
            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint2D contact = collision.GetContact(i);
                normal += contact.normal;
            }

            if (normal.sqrMagnitude <= 0.0001f)
                normal = -_currentDirection;

            _currentDirection = ChooseEscapeDirection(normal.normalized);
            _nextRepathTime = 0f;
        }

        private Vector2 ChooseEscapeDirection(Vector2 preferred)
        {
            if (preferred.sqrMagnitude <= 0.0001f)
                preferred = _currentDirection.sqrMagnitude > 0.0001f ? _currentDirection : Vector2.right;

            preferred.Normalize();
            Vector2 bestDirection = Vector2.zero;
            float bestScore = float.NegativeInfinity;
            Vector2 currentPosition = _rigidbody.position;
            Vector2 playerPosition = _player != null ? (Vector2)_player.position : currentPosition - preferred;

            for (int i = 0; i < CandidateAngles.Length; i++)
            {
                Vector2 candidate = Rotate(preferred, CandidateAngles[i]);
                if (!CanMove(candidate, _lookAheadDistance))
                    continue;

                Vector2 projected = currentPosition + candidate * _lookAheadDistance;
                float distanceScore = Vector2.Distance(projected, playerPosition);
                float awayScore = Vector2.Dot(candidate, preferred) * 2.25f;
                float continuityScore = Vector2.Dot(candidate, _currentDirection) * 0.85f;
                float score = distanceScore + awayScore + continuityScore;

                if (score <= bestScore)
                    continue;

                bestScore = score;
                bestDirection = candidate;
            }

            if (bestDirection.sqrMagnitude > 0.0001f)
                return bestDirection.normalized;

            Vector2 reverse = -_currentDirection;
            if (reverse.sqrMagnitude > 0.0001f && CanMove(reverse.normalized, _lookAheadDistance * 0.55f))
                return reverse.normalized;

            return Vector2.zero;
        }

        private bool CanMove(Vector2 direction, float distance)
        {
            if (direction.sqrMagnitude <= 0.0001f)
                return false;

            direction.Normalize();
            Vector3 current = transform.position;
            Vector3 target = current + new Vector3(direction.x, direction.y, 0f) * distance;

            if (_map != null && _map.IsInitialized)
            {
                if (!_map.IsWalkable(target) || !_map.CanTraverseSegment(current, target, _mapSampleSpacing))
                    return false;
            }

            ContactFilter2D filter = new ContactFilter2D
            {
                useTriggers = false,
                useLayerMask = true,
                layerMask = _obstacleMask
            };

            int hitCount = _rigidbody.Cast(direction, filter, _castHits, distance);
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hitCollider = _castHits[i].collider;
                if (hitCollider == null || hitCollider.transform.IsChildOf(transform) || IsPlayerCollider(hitCollider))
                    continue;

                return false;
            }

            return true;
        }

        private bool IsPlayerCollider(Collider2D collider)
        {
            if (collider == null || _player == null)
                return false;

            return collider.transform == _player || collider.transform.IsChildOf(_player);
        }

        private void UpdateAnimation(Vector2 movement)
        {
            if (_animator != null)
            {
                float speed = Mathf.Clamp01(movement.magnitude);
                _animator.SetFloat(PlayerAnimatorDriver.SpeedHash, speed);
                _animator.SetFloat(PlayerAnimatorDriver.MoveXHash, movement.x);
                _animator.SetFloat(PlayerAnimatorDriver.MoveYHash, movement.y);
                _animator.SetFloat(PlayerAnimatorDriver.FacingXHash, movement.x);
                _animator.SetFloat(PlayerAnimatorDriver.FacingYHash, movement.y);
                _animator.SetBool(PlayerAnimatorDriver.IsMovingHash, speed > 0.01f);
            }

            RotateVisualTowards(movement);
        }

        private void RotateVisualTowards(Vector2 direction)
        {
            if (_visualRoot == null || direction.sqrMagnitude <= 0.0001f)
                return;

            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _visualRoot.localRotation = Quaternion.RotateTowards(
                _visualRoot.localRotation,
                Quaternion.Euler(0f, 0f, targetAngle),
                360f * Time.deltaTime);

            if (_spriteRenderer == null)
                return;

            float currentAngle = Mathf.DeltaAngle(0f, _visualRoot.localEulerAngles.z);
            bool shouldFlip = Mathf.Abs(currentAngle) > (_visualFlipped ? 72f : 108f);
            if (shouldFlip == _visualFlipped)
                return;

            _visualFlipped = shouldFlip;
            _spriteRenderer.flipY = shouldFlip;
        }

        private static Vector2 Rotate(Vector2 vector, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            return new Vector2(
                vector.x * cos - vector.y * sin,
                vector.x * sin + vector.y * cos);
        }
    }
}
