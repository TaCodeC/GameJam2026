# Toon Underwater Effect

Efecto global barato para cueva submarina 2D toon. El camino principal es URP fullscreen con un pass de material; el overlay es la version modo supervivencia para WebGL/movil.

## Archivos

- `Shaders/ToonUnderwaterFullscreen.shader`: efecto fullscreen URP.
- `Scripts/UnderwaterRendererFeature.cs`: Renderer Feature + Scriptable Render Pass.
- `Scripts/UnderwaterEffectController.cs`: manda parametros y posicion de lampara al material.
- `Shaders/ToonUnderwaterOverlay.shader`: alternativa para Canvas/Quad fullscreen transparente.

## Instalacion URP

1. Crea un material: `Assets > Create > Material`.
2. Ponle el shader `Jaramillo/Underwater/Toon Cave Fullscreen`.
3. Asigna una textura chica de ruido/normal a `Distortion Texture`. Recomendado: 128x128 o 256x256, `Wrap Mode: Repeat`.
4. Asigna una textura chica a `Caustics Texture` si quieres caustics. Si no, apaga `Enable Caustics` en el controller.
5. Abre `Assets/Settings/PC_Renderer.asset`.
6. En `Add Renderer Feature`, agrega `UnderwaterRendererFeature`.
7. Arrastra el material fullscreen al campo `Underwater Material`.
8. Deja `Injection Point` en `Before Rendering Post Processing`.
9. Repite lo mismo en `Assets/Settings/Mobile_Renderer.asset` si quieres el efecto tambien en movil/WebGL.
10. Agrega `UnderwaterEffectController` a la camara principal o a un GameObject en la escena.
11. En `Effect Material`, asigna el mismo material.
12. En `Target Camera`, asigna la camara que renderiza el juego.
13. En `Light Transform`, arrastra el Transform de la lampara del jugador/buzo.

## Activar por escena

- Deja `UnderwaterRendererFeature` agregado en el Renderer asset.
- En escenas con agua, pon un `UnderwaterEffectController` activo y asignale el material.
- En escenas sin agua, no pongas el controller o desactiva su GameObject.
- La feature trae `Require Active Controller` encendido: si no encuentra controller activo, no ejecuta el pass. Es el switch de "aqui no hay cenote, gracias".

## Valores recomendados

- `Tint Intensity`: `0.35`
- `Darkness`: `0.25`
- `Vertical Gradient Strength`: `0.3`
- `Vignette Strength`: `0.35`
- `Distortion Strength`: `0.008` movil/WebGL, `0.015` PC
- `Distortion Speed`: `0.05`
- `Caustics Intensity`: `0.08`
- `Light Intensity`: `0.5`
- `Light Radius`: `0.35`

## Quality Mode

- `Low`: reduce distorsion/caustics y usa menos detalle en shader.
- `Medium`: recomendado general.
- `High`: mas detalle en ondas/caustics, todavia sin blur ni volumetria.

Para WebGL/movil, usa `Low`, baja `Distortion Strength` a `0.008`, y apaga `Enable Caustics` si el frame-time se pone dramatico.

## Alternativa overlay barata

1. Crea otro material con shader `Jaramillo/Underwater/Toon Cave Overlay`.
2. Crea un `Canvas` fullscreen o un `Quad` frente a la camara.
3. Si usas Canvas, agrega una `Image` estirada a toda la pantalla y asigna el material overlay.
4. Usa el mismo `UnderwaterEffectController`, pero asigna el material overlay en `Effect Material`.
5. Esta version no distorsiona la escena porque no lee el framebuffer. A cambio es baratisima y suficiente para builds moviles agresivos.

## Notas de rendimiento

- No usa luces reales de Unity, sombras, volumetria, blur ni camaras extra.
- La lampara es una mascara radial en viewport con distancia 2D.
- El fullscreen mueve escena, tinte y caustics con la misma textura de distorsion; asi el color no queda como mica pegada a la camara.
- El overlay no puede deformar lo que ya se renderizo, pero si mueve el tinte y las caustics con el ruido.
- Las burbujas/particulas van aparte, no dentro del shader.
- Si la lampara sale de pantalla, la luz cae sola por distancia.
