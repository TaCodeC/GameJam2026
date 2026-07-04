# Beta: descubrimiento de mapa

El sistema usa una imagen como mascara del nivel:

- Blanco: zona transitable.
- Negro: zona bloqueada.
- `MapDefinition`: relaciona la mascara completa con un area del mundo.
- `MapDiscoverySystem`: registra donde esta el jugador, que se descubrio y por donde paso.
- `MapDebugHud`: genera los dos mapas de debug solicitados.
- `MapDiscoveryView`: aplica el shader de descubrimiento a un `Renderer` o `RawImage`.

La camara del jugador puede mostrar una zona muy pequena. Esto no cambia el tamano del mapa ni su relacion con el mundo.

## Resultado del HUD de debug

Al iniciar Play Mode:

- **Arriba a la izquierda:** mapa completo, con un punto rojo en la posicion real del jugador y sus coordenadas.
- **Arriba a la derecha:** mapa inicialmente oculto que se va descubriendo alrededor del jugador.
- Debajo del mapa derecho aparecen los porcentajes descubierto y recorrido.

El HUD se crea automaticamente durante la ejecucion. No hace falta crear un `Canvas` ni `RawImage` manualmente.

## Implementacion paso a paso

### 1. Configurar el mapa completo

Usa `Example/PlaceholderMapDefinition.asset` para probar o crea otro desde:

`Create > Game Jam > Map > Map Definition`

Configura:

- `Traversable Mask`: imagen blanco/negro del nivel.
- `World Plane`: usa `XY` para la escena `Cave`, porque el juego actual se mueve en X/Y con camara ortografica.
- `World Size`: tamano completo que representa la imagen dentro del mundo.
- `Flip World X / Y`: invierte la deteccion horizontal o vertical sin girar los mapas mostrados.
- `Discovery Resolution`: resolucion interna del historial descubierto.

El placeholder de `Cave` usa `World Size = 192.844 x 106.74`. Si el objeto `MapDiscovery` esta centrado en X/Y `0,0`, representa:

- X desde `-96.422` hasta `96.422`.
- Y desde `-53.37` hasta `53.37`.

Al cambiar la imagen, ajusta `World Size` para que coincida con el nuevo nivel. La logica no depende de la resolucion original de la imagen.

### 2. Crear el sistema de descubrimiento

1. Crea un GameObject vacio llamado `MapDiscovery`.
2. Colocalo en el centro real del area representada por la imagen.
3. Agrega `MapDiscoverySystem`.
4. Asigna:
   - `Definition`: el `MapDefinition` del nivel.
   - `Tracked Transform`: el jugador. Si se deja vacio, el sistema intentara encontrar un GameObject llamado `Player`.
   - `Reveal Radius`: radio que se descubre alrededor del jugador.
   - `Visited Radius`: ancho del recorrido real registrado.
   - `Stamp Spacing`: separacion maxima entre puntos pintados.

Valores iniciales recomendados para el placeholder:

```text
Reveal Radius: 11
Visited Radius: 0.5
Stamp Spacing: 0.5
```

Con un viewport aproximado de `1/10` del mapa, la camara muestra alrededor de `19.2844 x 10.674` unidades. Un `Reveal Radius` de aproximadamente `11` cubre casi toda esa vista. Usa un radio menor si solamente quieres descubrir la zona cercana al jugador.

En la escena `Cave`, `MapDiscovery` no agrega rotacion extra. Deja `Flip World X` y `Flip World Y` apagados para no duplicar la orientacion de la imagen.

### 3. Activar los dos mapas de debug

Agrega `MapDebugHud` al mismo GameObject `MapDiscovery`.

Configura:

- `Discovery`: puede dejarse vacio si esta en el mismo objeto que `MapDiscoverySystem`.
- `Player`: puede dejarse vacio; usara el `Tracked Transform` del sistema.
- `Real Map Texture Override`: imagen visual para el debug de posicion real; en `Cave` usa `Example/MapBeta_1.PNG`.
- `Map Panel Size`: tamano visual de cada panel en pantalla.
- `Preserve Map Aspect Ratio`: mantenlo activo para no deformar mapas nuevos.
- `Screen Margin`: separacion respecto a las esquinas.
- `Player Marker Diameter`: tamano del punto rojo.

El punto rojo se coloca convirtiendo la posicion real del jugador a coordenadas dentro de la imagen. Si el jugador sale de los limites definidos por `World Size`, el punto desaparece del mapa.

Si el jugador esta fuera de esos limites, `MapDiscoverySystem` tambien muestra una advertencia en la consola indicando la posicion, el centro y el tamano configurados.

El punto rojo, la transitabilidad y la textura descubierta usan exactamente la misma conversion mundo-a-UV.

En `Cave`, el plano se llama `Map Plane`, esta centrado en X/Y `0,0` y es hijo de `MapDiscovery`.

### 4. Mostrar el descubrimiento en el plano real

Agrega `MapDiscoveryView` al `Map Plane`.

Configura:

- `Discovery`: el `MapDiscoverySystem` de `MapDiscovery`.
- `Target Renderer`: el `MeshRenderer` del `Map Plane`.
- `Discovery Shader`: `Shaders/MapDiscovery.shader`.
- `Map Texture Override`: la imagen visual del mapa, por ejemplo `Example/MapBeta_1.PNG`.

Si `Map Texture Override` queda vacio, el shader revelara la mascara blanco/negro. Esto es lo que usan los previews de debug; el plano real puede usar arte distinto sin cambiar la deteccion.

## Rotar o invertir la imagen

Si los mapas mostrados se ven bien, pero la deteccion ocurre en el lado opuesto, no rotes los mapas:

- Activa `Flip World X` si izquierda y derecha estan invertidas.
- Activa `Flip World Y` si arriba y abajo estan invertidos.

El placeholder de `Cave` no usa flips porque la mascara, el plano visible y los previews ya comparten la misma orientacion.

La rotacion de `MapDiscovery` se usa solamente cuando el area logica completa tambien esta rotada en el mundo. El plano visible debe tener esa misma orientacion para permanecer alineado.

No rotes solamente el plano visible: hacerlo cambiaria lo que se ve, pero no la orientacion de la mascara utilizada para calcular el punto rojo y las zonas transitables.

`MapDebugHud` mantiene ambos previews en la orientacion original de la mascara. No hace falta girar manualmente ningun elemento del HUD.

### 5. Generar colliders desde la mascara

Para WebGL/movil, usa colliders horneados en Editor:

1. Agrega `MapCollider2DBaker` al mismo GameObject del mapa, normalmente `MapDiscovery`.
2. Asigna `Discovery` o `Definition`. Si lo agregas accidentalmente a un hijo visual como `Map Plane`, el baker buscara un `MapDiscoverySystem` en los padres y horneara bajo ese objeto logico.
3. Ajusta:
   - `Source Sample Step`: usa `1` para maxima precision.
   - `Simplification Tolerance`: empieza con `0.2` para el placeholder.
   - `Edge Radius`: dejalo en `0`; valores grandes hacen que los colliders se vean como tubos gruesos.
4. Pulsa `Bake Colliders`.

Tambien puedes rebakear desde batchmode con:

```text
Unity -batchmode -quit -projectPath <proyecto> -executeMethod GameJam.Gameplay.Map.Editor.MapCollider2DCommandLineBaker.BakeScene -mapBakeScene Assets/Scenes/Cave.unity
```

El baker crea un hijo llamado `Generated Map Colliders` con `EdgeCollider2D` estaticos bajo el transform logico del mapa, no bajo el plano visual escalado/rotado. El costo de leer la textura, sacar contornos y simplificar puntos queda en Editor; en runtime solamente se cargan los colliders resultantes.

El flujo interno es:

```text
mascara blanco/negro
-> borde entre negro y blanco
-> loops cerrados
-> simplificacion Ramer-Douglas-Peucker
-> EdgeCollider2D
```

`Collider2D` vive en el plano `XY`, asi que este baker solamente acepta definitions configuradas con `World Plane = XY`.

### 6. Bloquear zonas negras por mascara, opcional

Agrega `MapWalkabilityConstraint` al jugador:

- `Map`: referencia al objeto `MapDiscovery`.
- `Target`: jugador.
- `Segment Sample Spacing`: comienza con `0.1`.

Esto evita que el jugador atraviese zonas negras de la mascara.
Es util como fallback o para movimiento directo por `Transform`, pero para fisica 2D real conviene usar `MapCollider2DBaker`.

## Viewport y descubrimiento

El viewport y el descubrimiento son independientes:

- Cambiar zoom o tamano de la camara no cambia `World Size`.
- Cambiar la imagen del mapa requiere actualizar el `MapDefinition`.
- Cambiar cuanto se descubre solamente requiere ajustar `Reveal Radius`.
- Los paneles de debug siempre muestran el mapa completo, aunque el jugador vea una parte pequena del nivel.

## API util

```csharp
bool puedeEstarAqui = discovery.IsWalkable(worldPosition);
bool yaSeRevelo = discovery.IsDiscovered(worldPosition);
bool elJugadorPasoAqui = discovery.HasBeenVisited(worldPosition);
bool puedeCruzar = discovery.CanTraverseSegment(origen, destino);

float porcentajeRevelado = discovery.DiscoveredFraction;
float porcentajeVisitado = discovery.VisitedFraction;
```

No hace falta activar `Read/Write` en las texturas. Hay pruebas EditMode en `Tests/Editor` para comprobar el mapeo mundo/textura, el descubrimiento y el bloqueo de trayectos.
