using UnityEngine;

namespace GameJam.Creatures
{
    [DisallowMultipleComponent]
    public sealed class GliptodontePatrol : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private bool _patrolEnabled = true;
        [SerializeField, Min(0f)] private float _travelDistance = 8f;
        [SerializeField, Min(0f)] private float _speed = 1.4f;
        [SerializeField, Min(0f)] private float _turnPause = 0.15f;
        [SerializeField] private bool _startAtLeftEdge = true;
        [SerializeField] private bool _startMovingRight = true;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private bool _flipSpriteToMovement = true;

        [Header("Editor Preview")]
        [SerializeField] private bool _drawTrajectoryGizmo = true;
        [SerializeField] private Color _trajectoryColor = new(1f, 0f, 0f, 0.85f);
        [SerializeField, Min(0f)] private float _trajectoryHeightPadding = 0.2f;

        private float _leftEndpointX;
        private float _rightEndpointX;
        private int _direction;
        private float _pauseTimer;
        private bool _hasInitialized;

        private void Reset()
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void Awake()
        {
            InitializePatrol();
        }

        private void OnEnable()
        {
            if (!_hasInitialized)
                InitializePatrol();
        }

        private void Update()
        {
            if (!_patrolEnabled || _travelDistance <= 0f || _speed <= 0f)
                return;

            if (_pauseTimer > 0f)
            {
                _pauseTimer = Mathf.Max(0f, _pauseTimer - Time.deltaTime);
                return;
            }

            float targetX = _direction > 0 ? _rightEndpointX : _leftEndpointX;
            Vector3 position = transform.position;
            position.x = Mathf.MoveTowards(position.x, targetX, _speed * Time.deltaTime);
            transform.position = position;

            if (Mathf.Abs(position.x - targetX) <= 0.001f)
            {
                _direction *= -1;
                _pauseTimer = _turnPause;
                ApplyFacing();
            }
        }

        private void OnValidate()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (!Application.isPlaying)
                ApplyFacingPreview();
        }

        private void OnDrawGizmos()
        {
            if (!_drawTrajectoryGizmo)
                return;

            Bounds bounds = GetTrajectoryBounds();
            Gizmos.color = _trajectoryColor;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }

        private void InitializePatrol()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            Vector3 startPosition = transform.position;
            if (_startAtLeftEdge)
            {
                _leftEndpointX = startPosition.x;
                _rightEndpointX = startPosition.x + _travelDistance;
            }
            else
            {
                _leftEndpointX = startPosition.x - _travelDistance;
                _rightEndpointX = startPosition.x;
            }

            _direction = _startMovingRight ? 1 : -1;
            _pauseTimer = 0f;
            _hasInitialized = true;

            ApplyFacing();
        }

        private void ApplyFacing()
        {
            if (!_flipSpriteToMovement || _spriteRenderer == null)
                return;

            // The source gliptodonte sheet faces left, so rightward movement needs a horizontal flip.
            _spriteRenderer.flipX = _direction > 0;
        }

        private void ApplyFacingPreview()
        {
            if (!_flipSpriteToMovement || _spriteRenderer == null)
                return;

            _spriteRenderer.flipX = _startMovingRight;
        }

        private Bounds GetTrajectoryBounds()
        {
            Bounds spriteBounds = _spriteRenderer == null
                ? new Bounds(transform.position, Vector3.one)
                : _spriteRenderer.bounds;

            Vector3 center = transform.position;
            if (Application.isPlaying)
            {
                center.x = (_leftEndpointX + _rightEndpointX) * 0.5f;
            }
            else
            {
                float halfDistance = _travelDistance * 0.5f;
                center.x += _startAtLeftEdge ? halfDistance : -halfDistance;
            }

            center.y = spriteBounds.center.y;
            center.z = spriteBounds.center.z;

            Vector3 size = spriteBounds.size;
            size.x += _travelDistance;
            size.y += _trajectoryHeightPadding * 2f;
            size.z = Mathf.Max(size.z, 0.05f);

            return new Bounds(center, size);
        }
    }
}
