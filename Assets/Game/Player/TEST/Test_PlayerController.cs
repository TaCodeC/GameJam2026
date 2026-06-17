using GameJam.Input;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Test_PlayerController : MonoBehaviour
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
        _moveInput = _gameInput != null ? _gameInput.Move : Vector2.zero;
    }

    private void FixedUpdate()
    {
        Vector2 nextPosition = _rigidbody.position + _moveInput * (_moveSpeed * Time.fixedDeltaTime);
        _rigidbody.MovePosition(nextPosition);
    }

    private void OnActionPressed(GameAction action)
    {
        Debug.Log($"Action pressed: {action}");
    }
}
