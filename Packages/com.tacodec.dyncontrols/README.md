# DynControls

DynControls is a small collection of reusable on-screen controls for Unity.

The package provides UI components that report pointer interaction through
properties, C# events, and UnityEvents.

## Included controls

- `VirtualJoystick`: normalized two-dimensional input.
- `MobileActionButton`: press, release, and held state.
- `PointerSurface`: shared pointer position and press state for mouse and touch.
- `MobileControlsManager`: shows or hides a mobile-controls root.
- `MobileInputDetector`: detects native mobile builds, touch-oriented WebGL browsers,
  and explicit WebGL URL overrides such as `?device=pc`.

## Quick start

1. Create a Canvas and make sure the scene has an EventSystem.
2. Add an Image for a joystick base and place a smaller Image inside it as the handle.
3. Add `VirtualJoystick` to the base and assign the handle.
4. Add `MobileActionButton` to any Image that should behave as a button.
5. Read the controls directly, use their C# events, or connect their UnityEvents.

```csharp
using DynControls;
using UnityEngine;

public class ControlsReader : MonoBehaviour
{
    [SerializeField] private VirtualJoystick _movement;
    [SerializeField] private MobileActionButton _action;

    private void OnEnable()
    {
        _action.Pressed += OnActionPressed;
    }

    private void OnDisable()
    {
        _action.Pressed -= OnActionPressed;
    }

    private void Update()
    {
        Vector2 direction = _movement.Value;
        Debug.Log($"Joystick: {direction}");
    }

    private void OnActionPressed(MobileActionButton button)
    {
        Debug.Log("Action pressed");
    }
}
```

More detail is available in `Documentation~/DynControls.md`. Importable examples
are available from the Samples section in Package Manager.
