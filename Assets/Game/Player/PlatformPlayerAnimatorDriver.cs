using GameJam.Input;
using UnityEngine;

namespace GameJam.Player.Platform
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class PlatformPlayerAnimatorDriver : MonoBehaviour
    {
        public static readonly int SpeedHash = Animator.StringToHash("Speed");
        public static readonly int MoveXHash = Animator.StringToHash("MoveX");
        public static readonly int VerticalVelocityHash = Animator.StringToHash("VerticalVelocity");
        public static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
        public static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int JumpStateHash = Animator.StringToHash("Jump");

        [Header("Input")]
        [SerializeField] private GameInput _gameInput;
        [SerializeField] private bool _autoFindGameInput = true;

        [Header("Physics")]
        [SerializeField] private Platform_PlayerController _platformController;
        [SerializeField] private Rigidbody2D _rigidbody;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private bool _flipSpriteWithHorizontalMovement = true;

        [Header("Tuning")]
        [SerializeField, Min(0f)] private float _movementDeadZone = 0.01f;

        private Animator _animator;
        private bool _isSubscribed;

        private void Reset()
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void Awake()
        {
            _animator = GetComponent<Animator>();

            if (_spriteRenderer == null)
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (_platformController == null)
                _platformController = GetComponent<Platform_PlayerController>();

            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody2D>();

            if (_gameInput == null && _autoFindGameInput)
                _gameInput = FindFirstObjectByType<GameInput>();
        }

        private void OnEnable()
        {
            SubscribeToController();
        }

        private void OnDisable()
        {
            UnsubscribeFromController();
        }

        private void Update()
        {
            SetMovement(_gameInput == null ? Vector2.zero : _gameInput.Move);
            SetPhysicsState();
        }

        public void SetMovement(Vector2 movement)
        {
            if (_animator == null)
                return;

            float horizontalSpeed = Mathf.Abs(movement.x);
            bool isRunning = horizontalSpeed > _movementDeadZone;

            if (isRunning && _flipSpriteWithHorizontalMovement && _spriteRenderer != null)
                _spriteRenderer.flipX = movement.x < 0f;

            _animator.SetFloat(SpeedHash, horizontalSpeed);
            _animator.SetFloat(MoveXHash, movement.x);
            _animator.SetBool(IsRunningHash, isRunning);
        }

        private void SetPhysicsState()
        {
            if (_animator == null)
                return;

            bool isGrounded = _platformController == null || _platformController.IsGrounded;
            float verticalVelocity = _rigidbody == null ? 0f : _rigidbody.linearVelocity.y;

            _animator.SetBool(IsGroundedHash, isGrounded);
            _animator.SetFloat(VerticalVelocityHash, verticalVelocity);
        }

        private void SubscribeToController()
        {
            if (_isSubscribed)
                return;

            if (_platformController == null)
                _platformController = GetComponent<Platform_PlayerController>();

            if (_platformController == null)
                return;

            _platformController.Jumped += OnJumped;
            _isSubscribed = true;
        }

        private void UnsubscribeFromController()
        {
            if (!_isSubscribed || _platformController == null)
                return;

            _platformController.Jumped -= OnJumped;
            _isSubscribed = false;
        }

        private void OnJumped()
        {
            if (_animator == null)
                return;

            _animator.Play(JumpStateHash, 0, 0f);
        }
    }
}
