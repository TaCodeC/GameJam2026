using System;
using UnityEngine;
using GameJam.Input;

namespace GameJam.Player.Platform
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Platform_PlayerController : MonoBehaviour
    {
        [SerializeField] private GameInput _gameInput;
        [SerializeField] private bool _autoFindGameInput = true;
        [SerializeField] private float _speed = 5f;
        [SerializeField] private float _jumpForce = 7f;
        [SerializeField] private float _gravityScale = 3f;
        [SerializeField] private LayerMask _groundLayers = ~0;
        [SerializeField, Min(0.001f)] private float _groundCheckDistance = 0.08f;
        [SerializeField, Min(0f)] private float _groundCheckLockoutAfterJump = 0.06f;
        [SerializeField, Min(0f)] private float _coyoteTime = 0.12f;
        [SerializeField, Min(0)] private int _airJumpCount = 1;
        [SerializeField] private bool _applyFrictionlessMaterial = true;

        private Rigidbody2D _rigidbody;
        private Collider2D[] _colliders;
        private readonly RaycastHit2D[] _groundHits = new RaycastHit2D[8];
        private ContactFilter2D _groundFilter;
        private PhysicsMaterial2D _frictionlessMaterial;
        private float _horizontalInput;
        private float _groundCheckLockoutTimer;
        private float _coyoteTimer;
        private int _airJumpsRemaining;
        private bool _jumpRequested;
        private bool _isGrounded;

        public event Action Jumped;

        public bool IsGrounded => _isGrounded;
        public Vector2 Velocity => _rigidbody == null ? Vector2.zero : _rigidbody.linearVelocity;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _colliders = GetComponentsInChildren<Collider2D>();

            if (_gameInput == null && _autoFindGameInput)
                _gameInput = FindFirstObjectByType<GameInput>();

            _airJumpsRemaining = _airJumpCount;

            ConfigureRigidbody();
            ConfigureGroundFilter();
            ApplyFrictionlessMaterial();
        }

        private void OnEnable()
        {
            if (_gameInput != null)
                _gameInput.ActionPressed += OnActionPressed;
        }

        private void OnDisable()
        {
            if (_gameInput != null)
                _gameInput.ActionPressed -= OnActionPressed;
        }

        private void Update()
        {
            _horizontalInput = _gameInput == null ? 0f : _gameInput.Move.x;
        }

        private void FixedUpdate()
        {
            _groundCheckLockoutTimer = Mathf.Max(0f, _groundCheckLockoutTimer - Time.fixedDeltaTime);
            _isGrounded = _groundCheckLockoutTimer <= 0f && CheckGrounded();

            if (_isGrounded)
            {
                _coyoteTimer = _coyoteTime;
                _airJumpsRemaining = _airJumpCount;
            }
            else
            {
                _coyoteTimer = Mathf.Max(0f, _coyoteTimer - Time.fixedDeltaTime);
            }

            Vector2 velocity = _rigidbody.linearVelocity;
            velocity.x = _horizontalInput * _speed;

            if (_jumpRequested)
            {
                bool canGroundJump = _coyoteTimer > 0f;
                bool canAirJump = !canGroundJump && _airJumpsRemaining > 0;

                if (canGroundJump || canAirJump)
                {
                    if (canAirJump)
                        _airJumpsRemaining--;

                    velocity.y = _jumpForce;
                    _groundCheckLockoutTimer = _groundCheckLockoutAfterJump;
                    _coyoteTimer = 0f;
                    _isGrounded = false;
                    Jumped?.Invoke();
                }

                _jumpRequested = false;
            }

            _rigidbody.linearVelocity = velocity;
        }

        private void OnActionPressed(GameAction action)
        {
            if (action == GameAction.Jump)
                _jumpRequested = true;
        }

        private void ConfigureRigidbody()
        {
            _rigidbody.bodyType = RigidbodyType2D.Dynamic;
            _rigidbody.gravityScale = _gravityScale;
            _rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
            _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rigidbody.linearDamping = 0f;
            _rigidbody.angularDamping = 0f;
        }

        private void ConfigureGroundFilter()
        {
            _groundFilter = new ContactFilter2D();
            _groundFilter.SetLayerMask(_groundLayers);
            _groundFilter.useLayerMask = true;
            _groundFilter.useTriggers = false;
        }

        private void ApplyFrictionlessMaterial()
        {
            if (!_applyFrictionlessMaterial)
                return;

            _frictionlessMaterial = new PhysicsMaterial2D($"{name} No Friction")
            {
                friction = 0f,
                bounciness = 0f
            };

            foreach (Collider2D bodyCollider in _colliders)
            {
                if (bodyCollider != null && !bodyCollider.isTrigger)
                    bodyCollider.sharedMaterial = _frictionlessMaterial;
            }
        }

        private bool CheckGrounded()
        {
            foreach (Collider2D bodyCollider in _colliders)
            {
                if (bodyCollider == null || !bodyCollider.enabled || bodyCollider.isTrigger)
                    continue;

                int hitCount = bodyCollider.Cast(
                    Vector2.down,
                    _groundFilter,
                    _groundHits,
                    _groundCheckDistance);

                for (int i = 0; i < hitCount; i++)
                {
                    if (_groundHits[i].normal.y > 0.5f)
                        return true;
                }
            }

            return false;
        }

        private void OnDestroy()
        {
            if (_frictionlessMaterial != null)
                Destroy(_frictionlessMaterial);
        }
    }
}
