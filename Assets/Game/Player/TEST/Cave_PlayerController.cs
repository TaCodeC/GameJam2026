using GameJam.Input;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[DisallowMultipleComponent]
public class Cave_PlayerController : MonoBehaviour
{
    [SerializeField] private GameInput _gameInput;
    [SerializeField] private float _moveSpeed = 5f;

    private Rigidbody2D _rigidbody;
    private Vector2 _moveInput;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        if (_rigidbody == null)
        {
            _rigidbody = gameObject.AddComponent<Rigidbody2D>();
        }

        _rigidbody.bodyType = RigidbodyType2D.Dynamic;
        _rigidbody.gravityScale = 0f;
        _rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
        _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
        _rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rigidbody.linearDamping = 0f;
        _rigidbody.angularDamping = 0f;
    }

    private void OnEnable()
    {
        if (_gameInput != null)
        {
            _gameInput.ActionPressed += OnActionPressed;
        }
    }

    private void OnDisable()
    {
        if (_gameInput != null)
        {
            _gameInput.ActionPressed -= OnActionPressed;
        }
    }

    private void Update()
    {
        _moveInput = _gameInput == null ? Vector2.zero : Vector2.ClampMagnitude(_gameInput.Move, 1f);
    }

    private void FixedUpdate()
    {
        _rigidbody.linearVelocity = _moveInput * _moveSpeed;
    }

    private void OnActionPressed(GameAction action)
    {
        Debug.Log($"Action pressed: {action}");
    }
}
