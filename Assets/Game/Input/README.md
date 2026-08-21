# Entrada del juego

Este sistema separa los controles fisicos de las acciones del juego.

Un script de gameplay no necesita saber si el jugador uso `W`, un gamepad o un
joystick tactil. Solo recibe intenciones como `Move`, `Jump` o `Attack`.

## Recorrido de una tecla

Cuando el jugador presiona `W`, ocurre lo siguiente:

1. El asset `InputSystem_Actions.inputactions` revisa el Action Map activo.
2. El Action Map decide que significa `W` en ese nivel.
3. `InputSystemSource` traduce el resultado al contrato `IGameInput`.
4. `GameInput` combina ese resultado con otras fuentes, como los controles tactiles.
5. El controlador del personaje usa la accion sin preguntar que tecla la produjo.

Por ejemplo, si el mapa activo relaciona `W` con la parte superior de `Move`,
el gameplay recibe:

```csharp
Controls.Move == new Vector2(0f, 1f);
```

Si el mapa activo relaciona `W` con `Jump`, el gameplay recibe el evento:

```csharp
GameAction.Jump
```

## Controles distintos segun el nivel

Para cambiar el significado de las teclas entre niveles, crea un Action Map para
cada tipo de gameplay dentro de `InputSystem_Actions.inputactions`.

Una configuracion posible seria:

```text
Swimming
|- Move
|  |- Up: W
|  |- Down: S
|  |- Left: A
|  `- Right: D
`- Attack

Platformer
|- Move
|  |- Left: A
|  `- Right: D
|- Jump
|  `- W
`- Attack
```

En el nivel de natacion, `W` produce `Move = (0, 1)`.

En el nivel plataformero, `W` produce `GameAction.Jump`. No produce movimiento
vertical porque ya no pertenece a la accion `Move` de ese mapa.

Si cada nivel es una escena distinta, agrega un `InputSystemSource` en cada escena
y configura su campo `Action Map Name`:

```text
Escena de natacion    -> Swimming
Escena plataformera   -> Platformer
```

Los nombres de las acciones deben seguir siendo los del contrato, como `Move`,
`Jump` y `Attack`. Lo que cambia entre mapas son sus teclas y bindings.

## Piezas del sistema

- `IGameInput`: contrato que consumen los scripts de gameplay.
- `GameInput`: combina todas las fuentes de entrada y las presenta como una sola.
- `InputSystemSource`: adapta teclado, mouse y gamepad desde Unity Input System.
- `MobileInputSource`: adapta los joysticks y botones tactiles de `DynControls`.
- `IGameInputSource`: contrato para agregar nuevas fuentes, como IA o replays.

## Configuracion basica

1. Crea un GameObject llamado `GameInput`.
2. Agrega los componentes `GameInput`, `InputSystemSource` y `MobileInputSource`.
3. Asigna `Assets/InputSystem_Actions.inputactions` a `InputSystemSource`.
4. Escribe el Action Map que necesita la escena en `Action Map Name`.
5. Deja vacia la lista de fuentes de `GameInput` para que encuentre las fuentes cercanas.
6. En `MobileInputSource`, asigna los joysticks y relaciona cada boton con un `GameAction`.

## Ejemplo breve de uso

Este controlador funciona con cualquier Action Map que exponga `Move` y `Jump`.
Cada nivel decide que teclas producen esas acciones.

```csharp
using GameJam.Input;
using UnityEngine;

public class SimplePlayerController : MonoBehaviour
{
    [SerializeField] private GameInput _gameInput;
    [SerializeField] private float _speed = 5f;

    private IGameInput Controls => _gameInput;

    private void OnEnable()
    {
        Controls.ActionPressed += OnActionPressed;
    }

    private void OnDisable()
    {
        Controls.ActionPressed -= OnActionPressed;
    }

    private void Update()
    {
        Vector2 direction = Controls.Move;
        transform.position += (Vector3)direction * (_speed * Time.deltaTime);
    }

    private void OnActionPressed(GameAction action)
    {
        if (action == GameAction.Jump)
            Debug.Log("Saltar");
    }
}
```

El controlador no contiene condiciones por nivel ni revisa teclas directamente.
La escena selecciona el Action Map y el mapa decide que significa cada control.
