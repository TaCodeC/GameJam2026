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
        [SerializeField] private bool _rotateVisualWithMovement = true;
        [SerializeField] private bool _keepVisualUprightWhenFacingLeft = true;
        [SerializeField, Min(0f)] private float _rotationDegreesPerSecond = 540f;
        [SerializeField] private bool _flipSpriteWithHorizontalMovement;

        [Header("Tuning")]
        [SerializeField, Min(0f)] private float _movementDeadZone = 0.01f;

        private Animator _animator;
        private Vector2 _lastFacing = Vector2.right;

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
                _lastFacing = movement.normalized;
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

        private void RotateVisualTowards(Vector2 direction)
        {
            if (!_rotateVisualWithMovement || _visualRoot == null)
                return;

            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);

            if (_rotationDegreesPerSecond <= 0f)
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
                _spriteRenderer.flipY = currentAngle > 90f || currentAngle < -90f;
            }
        }
    }
}
