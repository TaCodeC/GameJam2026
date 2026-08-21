using DynControls;
using UnityEngine;

public sealed class BasicControlsReader : MonoBehaviour
{
    [SerializeField] private VirtualJoystick _movement;
    [SerializeField] private MobileActionButton _action;

    private void OnEnable()
    {
        if (_action != null)
            _action.Pressed += OnActionPressed;
    }

    private void OnDisable()
    {
        if (_action != null)
            _action.Pressed -= OnActionPressed;
    }

    private void Update()
    {
        if (_movement != null && _movement.Value != Vector2.zero)
            Debug.Log($"Joystick: {_movement.Value}");
    }

    private void OnActionPressed(MobileActionButton button)
    {
        Debug.Log($"Pressed: {button.name}");
    }
}
