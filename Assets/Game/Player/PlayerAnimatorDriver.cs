using GameJam.Input;
using UnityEngine;

namespace GameJam.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class PlayerAnimatorDriver : MonoBehaviour
    {
        public static readonly int SpeedHash = Animator.StringToHash("Speed");
        public static readonly int MoveXHash = Animator.StringToHash("MoveX");
        public static readonly int MoveYHash = Animator.StringToHash("MoveY");
        public static readonly int FacingXHash = Animator.StringToHash("FacingX");
        public static readonly int FacingYHash = Animator.StringToHash("FacingY");
        public static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

        [Header("Input")]
        [SerializeField] private GameInput _gameInput;
        [SerializeField] private bool _autoFindGameInput = true;

        [Header("Visuals")]
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Vector2 _initialFacing = Vector2.right;
        [SerializeField] private bool _rotateVisualWithMovement = true;
        [SerializeField] private bool _keepVisualUprightWhenFacingLeft = true;
        [SerializeField, Range(0f, 45f)] private float _uprightFlipToleranceDegrees = 18f;
        [SerializeField, Min(0f)] private float _rotationDegreesPerSecond = 540f;
        [SerializeField] private bool _flipSpriteWithHorizontalMovement;

        [Header("Tuning")]
        [SerializeField, Min(0f)] private float _movementDeadZone = 0.01f;

        private Animator _animator;
        private Vector2 _lastFacing = Vector2.right;
        private bool _visualFlipped;

        private void Reset()
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            _visualRoot = _spriteRenderer == null ? transform : _spriteRenderer.transform;
        }

        private void Awake()
        {
            _animator = GetComponent<Animator>();

            if (_spriteRenderer == null)
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (_visualRoot == null && _spriteRenderer != null)
                _visualRoot = _spriteRenderer.transform;

            if (_gameInput == null && _autoFindGameInput)
                _gameInput = FindFirstObjectByType<GameInput>();

            if (_spriteRenderer != null)
                _visualFlipped = _spriteRenderer.flipY;

            ApplyInitialFacing();
        }

        private void Update()
        {
            if (_gameInput != null)
                SetMovement(_gameInput.Move);
        }

        public void SetMovement(Vector2 movement)
        {
            if (_animator == null)
                return;

            float speed = Mathf.Clamp01(movement.magnitude);
            bool isMoving = speed > _movementDeadZone;

            if (isMoving)
            {
                _lastFacing = NormalizeFacing(movement);
                RotateVisualTowards(_lastFacing);

                if (!_rotateVisualWithMovement
                    && _flipSpriteWithHorizontalMovement
                    && _spriteRenderer != null
                    && Mathf.Abs(movement.x) > _movementDeadZone)
                {
                    _spriteRenderer.flipX = movement.x < 0f;
                }
            }

            _animator.SetFloat(SpeedHash, speed);
            _animator.SetFloat(MoveXHash, movement.x);
            _animator.SetFloat(MoveYHash, movement.y);
            _animator.SetFloat(FacingXHash, _lastFacing.x);
            _animator.SetFloat(FacingYHash, _lastFacing.y);
            _animator.SetBool(IsMovingHash, isMoving);
        }

        private void ApplyInitialFacing()
        {
            _lastFacing = NormalizeFacing(_initialFacing);
            RotateVisualTowards(_lastFacing, true);

            if (!_rotateVisualWithMovement
                && _flipSpriteWithHorizontalMovement
                && _spriteRenderer != null
                && Mathf.Abs(_lastFacing.x) > _movementDeadZone)
            {
                _spriteRenderer.flipX = _lastFacing.x < 0f;
            }

            _animator.SetFloat(SpeedHash, 0f);
            _animator.SetFloat(MoveXHash, 0f);
            _animator.SetFloat(MoveYHash, 0f);
            _animator.SetFloat(FacingXHash, _lastFacing.x);
            _animator.SetFloat(FacingYHash, _lastFacing.y);
            _animator.SetBool(IsMovingHash, false);
        }

        private void RotateVisualTowards(Vector2 direction, bool instant = false)
        {
            if (!_rotateVisualWithMovement || _visualRoot == null)
                return;

            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);

            if (instant || _rotationDegreesPerSecond <= 0f)
            {
                _visualRoot.localRotation = targetRotation;
            }
            else
            {
                _visualRoot.localRotation = Quaternion.RotateTowards(
                    _visualRoot.localRotation,
                    targetRotation,
                    _rotationDegreesPerSecond * Time.deltaTime);
            }

            if (_keepVisualUprightWhenFacingLeft && _spriteRenderer != null)
            {
                float currentAngle = Mathf.DeltaAngle(0f, _visualRoot.localEulerAngles.z);
                UpdateVisualFlip(currentAngle);
            }
        }

        private void UpdateVisualFlip(float currentAngle)
        {
            bool shouldFlip = ShouldFlipForAngle(currentAngle, _visualFlipped);

            if (shouldFlip != _visualFlipped)
                ApplyVisualFlip(shouldFlip);
        }

        private bool ShouldFlipForAngle(float currentAngle, bool referenceFlipped)
        {
            float absAngle = Mathf.Abs(currentAngle);
            float tolerance = Mathf.Clamp(_uprightFlipToleranceDegrees, 0f, 45f);
            float threshold = referenceFlipped ? 90f - tolerance : 90f + tolerance;
            return absAngle > threshold;
        }

        private void ApplyVisualFlip(bool flipped)
        {
            _visualFlipped = flipped;

            if (_spriteRenderer != null)
                _spriteRenderer.flipY = flipped;
        }

        private static Vector2 NormalizeFacing(Vector2 facing)
        {
            return facing.sqrMagnitude > 0.0001f ? facing.normalized : Vector2.right;
        }
    }
}
