# Cinematic comic system

## Side smoke fade

- Shader: `Shaders/HorizontalSmokeSideFade.shader`
- Material: `Materials/HorizontalSmokeSideFade.mat`
- Canvas prefab: `Assets/Resources/Cinematics/Prefabs/CinematicSideSmokeFadeCanvas.prefab`

Assign the material to a fullscreen UI `Image`, or drag the canvas prefab into a scene. The image alpha or a parent `CanvasGroup` can be animated normally.

## Comic cinematics

Create a comic cinematic asset from:

`Create > Game Jam > Cinematics > Comic Cinematic`

Set `Default Page` to the full comic image. Each shot uses a normalized rect over that page:

- `x = 0` is left, `x = 1` is right.
- `y = 0` is bottom, `y = 1` is top.
- `width` and `height` define the panel area.

The player scales and moves the page so the focused rect fills the screen, then interpolates to the next shot.

Useful runtime entry points:

- `ComicCinematicPlayer.Instance.Play(asset)`
- `ComicCinematicPlayer.Instance.PlayResource("AssetName")` for assets under `Resources/Cinematics/Comic`
- `ComicCinematicTrigger` for scene objects and UnityEvents

Prefab with the comic cinematic player:

`Prefabs/ComicCinematicPlayer.prefab`
