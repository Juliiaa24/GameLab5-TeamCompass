# Plan de Refactorización y Pulido (Fase 3: Polishing) — Computer Learning (Unity 6)

> **Documento de especificación técnica exhaustiva para ejecución secuencial con `/goal`.**
> Este plan abarca la implementación de control por teclado (Space to Start) en el menú, tarjeta introductoria al Assessment Test, extensión y control de velocidad en los diálogos de la mascota virtual, corrección de posicionamiento dinámico sobre objetivos activos, solución de desbordamiento de objetivos de tareas en WebGL, estabilización del grid de iconos del escritorio, refactorización completa a un sistema de puntuación aditivo (0 a N) con asignación de medallas por puntos y feedback visual flotante en tiempo real (+pts verde / -pts rojo).

---

## Reglas Generales de Arquitectura y Contexto

1. **Rama de Trabajo:** Todos los cambios deben realizarse, verificarse y guardarse exclusivamente en la rama `polishing`.
2. **Arquitectura y Estilo:** Respetar el namespace `ComputerLearning`, componentes modulares de Unity, prefabs existentes y uGUI / Canvas (`RectTransform`).
3. **Input System:** Utilizar la API del **New Input System** (`UnityEngine.InputSystem.Keyboard.current` y `Mouse.current`). Proteger referencias de compatibilidad bajo directivas si fuera necesario.
4. **Localización Bilingüe Obligatoria:** Todos los textos nuevos o modificados de interfaz y diálogos deben declararse en `UIDialogs.cs` y en sus correspondientes archivos JSON: `EN-UIDialogs.json` y `ES-UIDialogs.json` mediante `LocalizationManager`.
5. **Validación Continua:** Tras cada fase, verificar que los scripts compilen limpiamente sin errores ni warnings críticos en la consola de Unity.

---

## Fase 1: Menú Principal — "Space to Start" e Indicador Visual

### Diagnóstico
En la escena `Menu.unity`, el flujo de inicio se realiza exclusivamente haciendo clic con el ratón en el botón "Jugar" (`ChangeToMain()`), mientras que la tecla `Escape` se usa para salir. Para mayor accesibilidad y rapidez de uso, se requiere permitir el inicio mediante la barra espaciadora (`Space`) y proporcionar una indicación visual clara en la pantalla.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/Managers/SceneSystem.cs`
- `ComputerLearner/Assets/Scripts/Data/UIDialogs.cs`
- `ComputerLearner/Assets/Resources/EN-UIDialogs.json`
- `ComputerLearner/Assets/Resources/ES-UIDialogs.json`

### Especificación Técnica
1. **Detección de Tecla en `SceneSystem.cs`:**
   - En el método `Update()`, dentro del bloque condicional donde la escena activa sea `"Menu"`:
     - Comprobar si `UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame`.
     - Si se presiona `Space`, invocar directamente `ChangeToMain()`.
     - No vincular ninguna otra tecla (`Enter` no debe activar el inicio, solo `Space`).
2. **Textos Localizados en `UIDialogs.cs` y JSONs:**
   - Añadir la propiedad `menu_press_space_to_start`:
     - ES: `"Presiona ESPACIO o haz clic en Jugar para comenzar"`
     - EN: `"Press SPACE or click Play to start"`
3. **Indicador Visual en Tiempo de Ejecución:**
   - En `SceneSystem.EnsureMenuButtonsBound()`:
     - Localizar el botón de "Jugar" (o el contenedor principal de botones del menú).
     - Añadir o actualizar un elemento de texto (`TextMeshProUGUI` o `Text`) posicionado justo debajo del botón "Jugar", con estilo sutil, color blanco o crema suave, tamaño de fuente legible (~16-18pt) y `raycastTarget = false`.
     - El texto debe actualizarse ante cambios de idioma mediante `LocalizationManager.OnLanguageChanged`.

### Criterios de Aceptación
- Pulsar la barra espaciadora en el Menú inicia la partida inmediatamente (cargando Assessment si no se completó, o Desktop1 / VirtualDesktop).
- El texto explicativo aparece visible y limpio bajo el botón de jugar en inglés y español.
- Ninguna otra tecla inicia el juego.

---

## Fase 2: Introducción al Assessment Test (Modal Centrado y Control por Espacio)

### Diagnóstico
Al entrar en la escena `AssessmentScene.unity`, `InitialTestManager.StartAssessment()` arranca de inmediato con el primer ejercicio (`mouse_move`) y un temporizador regresivo de 15 segundos sin avisar al jugador de qué trata la prueba, provocando desconcierto en niños y primeros usuarios.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/Managers/InitialTestManager.cs`
- `ComputerLearner/Assets/Scripts/Data/UIDialogs.cs`
- `ComputerLearner/Assets/Resources/EN-UIDialogs.json`
- `ComputerLearner/Assets/Resources/ES-UIDialogs.json`

### Especificación Técnica
1. **Definición de Textos Didácticos en `UIDialogs.cs`:**
   - Añadir campos de texto en `UIDialogs.cs`, `EN-UIDialogs.json` y `ES-UIDialogs.json`:
     - `assessment_intro_title`:
       - ES: `"¡Bienvenido al Test de Habilidades!"`
       - EN: `"Welcome to the Skills Test!"`
     - `assessment_intro_description`:
       - ES: `"Antes de empezar, haremos unas pequeñas pruebas para ver cómo manejas el ratón (mover, hacer clic, arrastrar...). No te preocupes por equivocarte ni tengas prisa, ¡tómate todo el tiempo que necesites!"`
       - EN: `"Before starting, we will do a few quick tests to see how you use the mouse (moving, clicking, dragging...). Don't worry about making mistakes or rushing, take all the time you need!"`
     - `assessment_intro_prompt`:
       - ES: `"Presiona la barra ESPACIO para comenzar"`
       - EN: `"Press SPACE to start"`
2. **Creación de la Tarjeta Modal en `InitialTestManager.cs`:**
   - Declarar una bandera de estado `private bool isShowingIntro = false;` y un contenedor `private GameObject introModalObject;`.
   - Modificar `StartAssessment()`:
     - En lugar de llamar a `SpawnCurrentTask()` de forma inmediata, llamar a `ShowAssessmentIntroModal()`.
     - `ShowAssessmentIntroModal()`:
       - Instanciar una tarjeta modal centrada sobre el canvas del Assessment con fondo semitransparente oscuro detrás (backdrop) que bloquee la interacción con el fondo.
       - La tarjeta debe tener diseño Light Mode (blanco `#FFFFFF`, esquinas redondeadas, sombra suave, ancho ~560px, altura ~360px).
       - Insertar:
         - Icono o cabecera amigable.
         - Título destacado (`assessment_intro_title`, tamaño ~24pt, negrita, color `#1E293B`).
         - Párrafo descriptivo (`assessment_intro_description`, tamaño ~16pt, interlineado cómodo, color `#475569`).
         - Píldora o botón visual inferior con el aviso (`assessment_intro_prompt`, color destacado `#2563EB`, texto blanco).
       - Establecer `isShowingIntro = true;`.
3. **Control Exclusivo por Barra Espaciadora:**
   - En el `Update()` de `InitialTestManager.cs`:
     - Si `isShowingIntro == true`:
       - Verificar si `Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame`.
       - Si se pulsa `Space`:
         - Destruir o desactivar `introModalObject`.
         - Establecer `isShowingIntro = false;`.
         - Arrancar el flujo normal llamando a `SpawnCurrentTask()`.
       - Si no se pulsa `Space`, el temporizador de la tarea permanece detenido (`currentTaskTimer = 0f`).

### Criterios de Aceptación
- La escena del Assessment muestra una tarjeta explicativa atractiva antes de cualquier tarea.
- El temporizador del primer ejercicio no corre mientras la tarjeta está visible.
- Solo se puede comenzar presionando la barra `Espacio` (o clic en el botón de comenzar).
- El texto es claro y se muestra en el idioma activo seleccionado.

---

## Fase 3: Mascota Virtual — Duración de Diálogos y Seguimiento de Objetivos

### Diagnóstico
1. **Velocidad de texto excesiva:** Los mensajes temporales de la mascota duran entre 3.0s y 3.5s (`AutoHideFeedback`), tiempo insuficiente para que un niño en edad de aprendizaje lea el mensaje tres veces en voz alta.
2. **Solapamiento / Error de Target:** En `VirtualMascot.ShowMessage()`, cuando se pasa un `targetRect`, el parámetro `offset` es completamente ignorado y la mascota se sitúa exactamente en el centro del objetivo, tapando el elemento interactivo. Además, al dar feedback de fallos, el target pasado suele ser el contenedor raíz en vez del objetivo específico activo.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/DesktopSystem/VirtualMascot.cs`
- `ComputerLearner/Assets/Scripts/Tasks/BaseTask.cs`
- Tareas individuales (`ClickTargetTask.cs`, etc.) donde se invoque `VirtualMascot.ShowContextualFeedback`.

### Especificación Técnica
1. **Tiempo Extendido y Descarte Manual (Híbrido):**
   - En `VirtualMascot.cs`, actualizar `ShowContextualFeedback(string message, RectTransform target, float duration = 8.5f)`:
     - La duración por defecto se amplía de 3.5s a un rango de **8.5s a 10.0s** según la longitud de la cadena: `Mathf.Clamp(4.5f + (message.Split(' ').Length * 0.45f), 8.5f, 10.5f)`.
     - Implementar descarte anticipado en `Update()`: Si el bocadillo de diálogo está activo (`speechBubble.activeSelf`) y el usuario pulsa `Space` o hace clic sobre el bocadillo (`IPointerClickHandler`), descartar inmediatamente el mensaje y cancelar la corrutina `hideBubbleCoroutine`.
2. **Posicionamiento Seguro al lado del Target Activo:**
   - En `VirtualMascot.ShowMessage()` y en su ciclo `Update()`:
     - Dejar de forzar la posición de la mascota directamente en el centro `basePos = (corners[0] + corners[2]) / 2f;`.
     - Aplicar un desplazamiento lateral seguro:
       - Si el objetivo está en la mitad derecha de la pantalla, situar la mascota a la izquierda del objetivo (`basePos.x - targetWidth * 0.5f - 140f`).
       - Si está en la mitad izquierda, situarla a la derecha (`basePos.x + targetWidth * 0.5f + 140f`).
       - Mantener la altura alineada con la parte superior del objetivo (`basePos.y`).
     - Asegurar que la mascota y el speech bubble tengan `raycastTarget = false` durante tutoriales y tareas para imposibilitar el bloqueo de clics.
3. **Vinculación con el Target Activo Actual:**
   - En tareas de clics múltiples o dinámicas (`ClickTargetTask.cs`, etc.), asegurar que cuando la mascota dé un feedback de error o recordatorio, el `target` pasado a `VirtualMascot.ShowContextualFeedback` sea el `ClickTarget` específico que el jugador debe pulsar a continuación, y no el `spawnArea` completo.

### Criterios de Aceptación
- Los bocadillos de la mascota permanecen legibles durante ~9 segundos.
- El jugador puede cerrar el bocadillo al instante pulsando `Espacio` o haciendo clic sobre él.
- La mascota se posiciona lateralmente al objetivo sin tapar jamás el área donde el niño debe hacer clic o arrastrar.

---

## Fase 4: WebGL, Delimitación de Targets en Ventanas y Estabilidad de IconGrid

### Diagnóstico
1. **Targets fuera de ventana:** En tareas de ratón dentro de ventanas, los cálculos de posiciones aleatorias en `ClickTargetTask`, `HoverTargetTask` y `TaskTargetMovement` no consideran correctamente el padding de seguridad respecto a las barras de herramientas o bordes del contenedor, provocando que los círculos/cuadrados se generen parcialmente fuera de la ventana o detrás de otros elementos.
2. **Iconos desorganizados en WebGL:** En `IconGrid.cs`, al redimensionar la ventana o cambiar de escala en WebGL, el callback `OnRectTransformDimensionsChange()` recalcula la posición de los iconos a partir de coordenadas globales desfasadas (`InverseTransformPoint`), causando que los iconos salten arbitrariamente a otras casillas.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/Tasks/ClickTargetTask.cs`
- `ComputerLearner/Assets/Scripts/Tasks/HoverTargetTask.cs`
- `ComputerLearner/Assets/Scripts/Tasks/ClickHoldTask.cs`
- `ComputerLearner/Assets/Scripts/Tasks/DoubleClickTask.cs`
- `ComputerLearner/Assets/Scripts/Tasks/TaskTargetMovement.cs`
- `ComputerLearner/Assets/Scripts/DesktopSystem/IconGrid.cs`

### Especificación Técnica
1. **Margen de Seguridad Interior en Tareas de Ventana:**
   - En todas las tareas generadoras de targets interactivos:
     - Obtener el rectángulo local del contenedor: `Rect area = spawnArea.rect;`.
     - Si `area.width <= 0` o `area.height <= 0`, forzar actualización de layout inmediata con `LayoutRebuilder.ForceRebuildLayoutImmediate(spawnArea)`.
     - Definir un padding de borde estricto: `float padding = (cfg.targetSize * 0.5f) + 20f;`.
     - Delimitar los rangos válidos:
       ```csharp
       float minX = area.xMin + padding;
       float maxX = area.xMax - padding;
       float minY = area.yMin + padding;
       float maxY = area.yMax - padding;
       if (maxX <= minX) { minX = area.center.x; maxX = area.center.x; }
       if (maxY <= minY) { minY = area.center.y; maxY = area.center.y; }
       ```
     - En `TaskTargetMovement.cs`, aplicar esta misma delimitación en el rebote contra bordes para que los targets en movimiento no atraviesen los límites visibles de la ventana.
2. **Fijación Lógica de Cuadrícula en `IconGrid.cs`:**
   - Introducir una estructura o diccionario interno en `IconGrid.cs` que asocie cada `DraggableIcon` a una coordenada de cuadrícula lógica inmutable: `Dictionary<DraggableIcon, Vector2Int> iconSlots = new Dictionary<DraggableIcon, Vector2Int>();`.
   - Cuando un icono se registra o el usuario lo suelta en una celda libre, guardar de forma persistente su slot `(columna, fila)`.
   - En `OnRectTransformDimensionsChange()` y `EnsureGrid()`:
     - No recalcular las casillas basándose en la posición física de los iconos.
     - Reposicionar cada icono directamente en las coordenadas que le corresponden a su slot `(columna, fila)` registrado utilizando las nuevas dimensiones de celda:
       ```csharp
       Vector2 localPos = new Vector2(
           rect.rect.xMin + startX + slot.x * stepX + stepX * 0.5f,
           rect.rect.yMax - startY - slot.y * stepY - stepY * 0.5f
       );
       icon.transform.position = rect.TransformPoint(localPos);
       ```
   - De este modo, si la resolución del navegador cambia, los iconos se recolocan armónicamente en su celda sin intercambiarse ni agruparse erráticamente.

### Criterios de Aceptación
- Ningún target se genera fuera de los márgenes visibles de la ventana ni rebota más allá de los bordes.
- Al redimensionar la ventana en WebGL o cambiar de aspect ratio, los iconos del escritorio permanecen en sus casillas asignadas sin reorganizarse de forma aleatoria.

---

## Fase 5: Sistema de Puntuación Acumulativo (0 a N) con Medallas

### Diagnóstico
Actualmente la puntuación de nivel se basa en restar penalizaciones a partir de un 100% de precisión. El feedback solicita transformar esto en un sistema aditivo positivo tradicional: se comienza en **0 puntos**, cada acierto suma puntos y bonus, y los errores aplican penalizaciones moderadas sin bajar de 0. Las medallas de nivel (Bronce, Plata, Oro) se calculan en base a los puntos conseguidos sobre el total posible.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/Managers/ProgressData.cs`
- `ComputerLearner/Assets/Scripts/Managers/LevelRunner.cs`
- `ComputerLearner/Assets/Scripts/Data/LevelScoreRecord.cs`
- `ComputerLearner/Assets/Scripts/Data/DifficultyConfig.cs`

### Especificación Técnica
1. **Modelo de Puntuación Aditiva en `LevelRunner.cs`:**
   - Declarar variables de control de sesión:
     - `private int currentLevelScore = 0;`
     - `private int maxPossibleScore = 0;`
   - Al iniciar un nivel:
     - `currentLevelScore = 0;`
     - Calcular `maxPossibleScore = totalTasks * 150;` (100 puntos base + hasta 50 puntos de bonus por tarea).
   - **Al acertar una tarea / subtarea:**
     - Puntos base por acierto: `+100 pts`.
     - Bonus por tiempo rápido: Si se completa en menos del 40% del tiempo límite, otorgar hasta `+50 pts` adicionales escalonados.
     - Sumar a `currentLevelScore`: `currentLevelScore += (basePoints + speedBonus);`.
   - **Al cometer un fallo o error:**
     - Penalización: `-25 pts`.
     - Clamping para evitar puntos negativos: `currentLevelScore = Mathf.Max(0, currentLevelScore - 25);`.
2. **Determinación de Medallas por Puntos en `ProgressData.cs`:**
   - Actualizar el cálculo de medallas `CalculateMedalFromPoints(int points, int maxPoints)`:
     - **Oro (Gold):** `puntos >= maxPoints * 0.85f` (rendimiento excelente con pocos o ningún fallo).
     - **Plata (Silver):** `puntos >= maxPoints * 0.65f`.
     - **Bronce (Bronze):** `puntos >= maxPoints * 0.40f`.
     - **Sin medalla (None):** `puntos < maxPoints * 0.40f`.
   - Almacenar la puntuación numérica obtenida en `LevelScoreRecord.bestScore` (ahora representando los puntos enteros obtenidos) y conservar el registro de precisión y tiempo para los playtests y `SkillManager`.

### Criterios de Aceptación
- La sesión de cada nivel arranca en 0 puntos.
- Cada acierto incrementa los puntos y cada fallo descuenta puntos de forma balanceada.
- Las medallas de Bronce, Plata y Oro se desbloquean en función del porcentaje de puntos acumulados.

---

## Fase 6: Sistema de Feedback Visual Flotante (+Pts Verde / -Pts Rojo)

### Diagnóstico
Durante la resolución de ejercicios, no existe ningún estímulo visual inmediato en el punto de interacción que informe al jugador del impacto de sus aciertos o fallos.

### Archivos a crear y modificar
- **Crear:** `ComputerLearner/Assets/Scripts/UI/FloatingScoreText.cs`
- **Modificar:** `ComputerLearner/Assets/Scripts/Tasks/BaseTask.cs`
- **Modificar:** Tareas interactivas (`ClickTargetTask.cs`, `HoverTargetTask.cs`, etc.)
- **Modificar:** `ComputerLearner/Assets/Scripts/UI/LevelCelebrationModal.cs`
- **Modificar:** `ComputerLearner/Assets/Scripts/DesktopSystem/ScoresApp.cs`

### Especificación Técnica
1. **Creación del Componente `FloatingScoreText`:**
   - Crear el script `FloatingScoreText.cs` en `Assets/Scripts/UI/`:
     - Función estática: `public static void Spawn(RectTransform parentCanvas, Vector2 worldPosition, int pointDelta, string customText = null)`.
     - Instanciar un `TextMeshProUGUI` dinámico en la posición indicada.
     - **Configuración Visual:**
       - Si `pointDelta > 0`: Color verde esmeralda brillante (`#22C55E`), texto `+{pointDelta}` (ej: `+100`, `+150`).
       - Si `pointDelta < 0`: Color rojo coral (`#EF4444`), texto `-{Mathf.Abs(pointDelta)}` (ej: `-25`).
       - Fuente en negrita, tamaño ~24-28pt con sombra o contorno sutil para máxima legibilidad sobre cualquier fondo.
       - `raycastTarget = false`.
     - **Animación (Corrutina):**
       - Duración: `0.85f` segundos en tiempo no escalado (`unscaledDeltaTime`).
       - Flotar verticalmente hacia arriba: elevar la posición en `+45f` píxeles mediante curva suave (`Mathf.SmoothStep`).
       - Desvanecimiento: mantener opacidad al 100% durante los primeros `0.35f` segundos y luego interpolar el canal alpha de `1f` a `0f`.
       - Destruir el GameObject automáticamente al concluir la animación.
2. **Invocación desde las Tareas:**
   - Al acertar un `ClickTarget`, `HoverTarget`, etc.: invocar `FloatingScoreText.Spawn` en la posición global donde estaba el target en el momento del impacto.
   - Al hacer clic fallido fuera del objetivo: invocar `FloatingScoreText.Spawn` con `-25` en el punto de clic o sobre el objetivo que se pretendía alcanzar.
3. **Integración en `LevelCelebrationModal.cs` y `ScoresApp.cs`:**
   - En `LevelCelebrationModal.cs`, mostrar la puntuación total acumulada en grande (ej: `"PUNTUACIÓN: 650 PTS"`) junto a la medalla otorgada y el desglose de aciertos y tiempo.
   - En `ScoresApp.cs`, actualizar las tarjetas de nivel para reflejar la mejor puntuación en puntos alcanzada por el jugador.

### Criterios de Aceptación
- Al acertar un objetivo aparece de inmediato un número verde `+100` que flota hacia arriba y se desvanece suavemente.
- Al fallar aparece un número rojo `-25`.
- La pantalla final de celebración refleja con claridad la puntuación total acumulada.

---

## Verificación Final y Criterios de Calidad

1. **Compilación:** Cero errores de compilación de C# y cero advertencias críticas en Unity.
2. **Control de Versiones:** Todos los cambios deben estar guardados e integrados limpiamente en la rama `polishing`.
3. **Flujo de Usuario Completo:**
   - [ ] En Menú: presionar `Space` arranca el juego.
   - [ ] En Assessment: aparece la tarjeta de bienvenida con texto explicativo y se inicia únicamente al pulsar `Space`.
   - [ ] En Tareas: los targets se mantienen dentro del marco visible de la ventana en todo momento.
   - [ ] Feedback Mascota: diálogos duran ~9s y se pueden descartar con `Space` o clic.
   - [ ] Puntuación: inicia en 0, suma con aciertos, descuenta con fallos, genera feedback flotante verde/rojo y otorga medallas según los puntos obtenidos.
   - [ ] Grid de Escritorio: al cambiar resolución, los iconos mantienen sus celdas sin dispersarse.
