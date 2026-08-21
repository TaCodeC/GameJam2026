# Minigame popup system

Prefab generado: `Assets/Game/Gameplay/Minigames/Prefabs/MinigamePopupCanvas.prefab`

Menu para crearlo o regenerarlo: `Game Jam > Minigames > Create Popup Canvas Prefab`

## Drag and drop

Usa `DragDropMinigame` y llena el arreglo `Pairs`.

Cada entrada tiene:
- `Id`: nombre para logs/eventos.
- `Draggable`: el objeto UI que lleva `DragDropItem`.
- `Target`: el `RectTransform` donde debe caer.
- `Drop Radius`: tolerancia extra alrededor del target.
- `Snap On Correct Drop`: centra el objeto sobre el target.
- `Lock On Correct Drop`: deja fijo el objeto correcto.

Si el jugador falla, por ahora hace `Debug.Log` y dispara el evento `Incorrect Drop`.

Para revisar el area de tolerancia mientras configuras, activa `Show Drop Radius Preview`.
El preview crea circulos UI sobre cada target, no bloquea raycasts, y con `Preview Only In Editor` encendido solo aparece dentro del editor.

## Medicion

Usa `MeasurementMinigame` y llena el arreglo `Questions`.

Cada pregunta permite configurar:
- Tipo de medicion: longitud, ancho, profundidad, diametro, circunferencia, conteo o custom.
- Herramienta requerida: cinta lineal o circunferencia.
- Tipo de respuesta: numero o texto.
- Respuesta correcta y tolerancia numerica.
- Unidad visible.
- Campo de entrada, prompt y herramienta de medicion compartidos o especificos por pregunta.

El `MeasurementToolSwitcher` permite alternar herramientas desde botones del prefab.
La plantilla incluye:
- `UIMeasurementTape`: cinta lineal para longitud, altura, anchura, profundidad o diametro.
- `UICircumferenceMeasurementTool`: dos manijas de diametro que estiman circunferencia como pi por diametro.

El prefab separa el flujo en dos paginas dentro de `MeasurementMinigame`:
- `Measurement Page`: herramientas, resto a medir y boton `Libreta`.
- `Field Notebook Page`: campo para escribir la respuesta, `Validar` y `Volver a medir`.

Al responder correctamente una pregunta, el sistema avanza a la siguiente y vuelve a la pagina de medicion para seguir tomando datos.

## Estado por objeto

Para que cada resto tenga su propio progreso, agrega `MinigameObjectState` al objeto del mundo, por ejemplo `Hueso 1`.
Ese componente guarda:
- Estado por minijuego: `NotStarted`, `InProgress`, `Completed` o `Failed`.
- Respuestas/intentos por pregunta o par de drag and drop.
- Si la respuesta fue correcta, el valor esperado y el numero de intento.

Agrega tambien `MinigameInteractableObject` si quieres abrir el canvas desde ese objeto.
Configura `Minigame Id` con el mismo id que tenga el panel en `MinigamePopupCanvas`, por ejemplo `measurement` o `drag_drop`.
Cuando se llame `OpenMinigame()`, el canvas abre ese panel y los minijuegos guardan sus respuestas en el `MinigameObjectState` del objeto.

## Cajas de huesos drag and drop

Prefabs editables:
- `BoneMeasurements/Prefabs/MinigamePopupCanvas_PrimeraCostilla.prefab`
- `BoneMeasurements/Prefabs/MinigamePopupCanvas_Sacro.prefab`
- `BoneMeasurements/Prefabs/MinigamePopupCanvas_Humero.prefab`

En cada prefab, ajusta las zonas bajo `Zonas de entrega (EDITAR AQUI)` y el hueso inicial bajo `Hueso arrastrable (EDITAR AQUI)`.
Costilla usa el target izquierdo y Sacro el derecho. `BonePlacementDropState` consulta el progreso compartido y muestra automaticamente el otro hueso si ya fue guardado.
Humero usa la silueta superior de la caja de huesos rectos en `Correciones_Final`; la silueta inferior queda como referencia persistente para el Femur.

## Destello de interactuable

Material listo: `Assets/Game/Gameplay/Minigames/Materials/InteractableStarPrompt.mat`

El material usa el shader `GameJam/Minigames/InteractableStarPrompt`: es un quad transparente con una estrella pulsante en el centro.
Para usarlo, crea un Quad o Sprite hijo del objeto interactuable, asigna ese material y agrega `InteractableSparklePrompt`.
El componente puede hacer billboard hacia camara, aplicar un offset/bob suave y ocultarse cuando el minijuego indicado ya este completado.

Ideas para hacer mas divertidas las mediciones en Hoyo Negro:
- Medir un hueso con cinta y luego comparar contra una tarjeta de escala arqueologica.
- Ajustar dos puntos de una brujula UI para capturar la orientacion de un hallazgo.
- Medir el diametro de una concrecion o fragmento con un calibrador sencillo de dos manijas.
- Estimar profundidad o distancia usando marcas en una linea guia de buceo.
- Registrar turbidez/visibilidad alineando una barra hasta donde deja de verse un marcador.
