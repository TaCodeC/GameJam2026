# DynControls manual

DynControls provides reusable UI controls for pointer, mouse, and touch input.

## Requirements

- A Canvas containing the controls.
- An EventSystem in the scene.
- UI Images with raycast targeting enabled.

## VirtualJoystick

Add `VirtualJoystick` to the Image used as the joystick base. Create a child Image
for the handle and assign its RectTransform.

`Value` is normalized between `-1` and `1`. It returns `Vector2.zero` after the
pointer is released. Use `ValueChanged` when event-driven input is more convenient.

## MobileActionButton

Add `MobileActionButton` to an Image. It reports:

- `Pressed` when the pointer goes down.
- `Released` when the pointer goes up or the control is disabled.
- `IsHeld` while the control remains pressed.
- `Release()` to cancel a held button from code.

The component does not store an action name, so the same button can be reused
wherever press, release, or held state is needed.

## PointerSurface

`PointerSurface` unifies mouse and touch interactions through Unity's EventSystem.
Place it on a raycastable UI element covering the desired interaction area.

It exposes:

- `Position`: current screen-space pointer position.
- `Delta`: latest screen-space movement.
- `IsPressed`: whether the active pointer is held.
- `Pressed`, `Released`, and `PositionChanged` events.

The first active pointer owns the surface until released. This prevents another
finger from unexpectedly taking over a drag.

## MobileControlsManager

Assign the root GameObject containing the mobile controls. The manager shows it
on Android, iOS, and touch-oriented WebGL browsers, and hides it elsewhere.

Enable `Force Enable In Editor` to test the mobile layout without making a build.

For WebGL portals that pass device information through the page URL, the detector
also honors explicit overrides such as `?device=pc`, `?device=mobile`,
`?dyncontrolsMobile=0`, and `?dyncontrolsMobile=1`.

The `Basic Controls` sample demonstrates direct reading. The `Pointer Surface`
sample demonstrates a mouse-and-touch drag interaction.
