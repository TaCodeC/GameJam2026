using GameJam.Input;
using UnityEngine;

public class Test_PlayerController : MonoBehaviour
{
    [SerializeField] private GameInput _gameInput;
    [SerializeField] private float _moveSpeed = 5f;


    private void OnEnable() {
        _gameInput.ActionPressed += OnActionPressed;

    }

    private void OnDisable() {
        _gameInput.ActionPressed -= OnActionPressed;
    }


    // Update is called once per frame
    void Update()
    {
        // Movimiento de lujo: sumarle numeros a la posicion.
        Vector2 input = _gameInput.Move;
        Vector3 moveDirection = new Vector3(input.x, input.y, 0f);
        transform.position += moveDirection * (_moveSpeed * Time.deltaTime);

    }

    private void OnActionPressed(GameAction action) {
        Debug.Log($"Action pressed: {action}");
    }
}
