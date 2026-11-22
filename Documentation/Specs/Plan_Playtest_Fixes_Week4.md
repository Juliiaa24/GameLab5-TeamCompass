# Plan de Correcciones y Mejoras (Playtest Semana 4) — Computer Learning (Unity 6)

> **Documento de especificación técnica para ejecución secuencial con `/goal`.**
> Este documento aborda los 11 puntos del feedback de playtest de esta semana (6 mejoras de jugabilidad/sistema de archivos y 5 correcciones de bugs de la mascota Pointy y la Papelera de Reciclaje).

---

## Reglas Generales de Arquitectura y Contexto

1. **Preservar la arquitectura actual**: Respetar el namespace `ComputerLearning`, componentes modulares de Unity, prefabs (`GenericLevelWindow.prefab`, `BaseWindow.prefab`, `InputModalDialog.prefab`) y uGUI / Canvas (`RectTransform`).
2. **Input System**: El proyecto utiliza el **New Input System** (`activeInputHandler: 1`). Usar `UnityEngine.InputSystem.Keyboard.current` y `UnityEngine.InputSystem.Mouse.current` (manteniendo `#if ENABLE_LEGACY_INPUT_MANAGER` únicamente como fallback).
3. **Localización sincronizada**: Mantener sincronizados los recursos JSON (`UIDialogs.json`, `EN-UIDialogs.json`, `ES-UIDialogs.json`, `NO-UIDialogs.json`, `*MascotFeedbackDialogs.json`) y sus clases C# (`UIDialogs.cs`, `MascotFeedbackDialogs`).
4. **Validación continua**: Validar que `Assembly-CSharp.csproj` compile con **0 errores** y verificar mediante `Unity_ValidateScript` / `Unity_ReadConsole` (si el servidor MCP está activo) o compilación limpia que no existan errores ni excepciones.
5. **Documentación**: Al concluir todas las secciones, actualizar `Documentation/Specs/Diego.md` añadiendo una nueva sección numerada con el resumen técnico de los cambios implementados.

---

## Sección 1: Tres modos de movimiento de objetivos (Estacionario, Circular y Rebote DVD) con escalado de puntos

### Diagnóstico de la causa raíz
1. En `ClickTarget.cs` (líneas 162–174), `TaskTargetMovement.cs` (líneas 29–43) y `HoverTarget.cs` (líneas 199–201), cuando `config.isMoving == false`, el código activa `isSwayingOnly = true`, haciendo que **todos los objetivos supuestamente estáticos se muevan en círculos/elipses** (`Mathf.Sin(swayTimer) * 32f, Mathf.Cos(swayTimer * 0.85f) * 22f`). Incluso `ClickHoldTask.cs` (línea 25) llama a `mover.SetMoving(false)`, lo cual no desactiva `isSwayingOnly`. Como resultado, **no existe ningún objetivo estacionario** en todo el juego.
2. En `L_Level2.asset` (`Hover`) y `L_Level3.asset` (`Click`), todas las tareas asignadas tienen `isMoving: 0` (`difficulty` 1, 2 y 3), por lo que el 100% de los objetivos de los Niveles 2 y 3 se mueven únicamente con el movimiento circular pequeño y ninguno usa el rebote tipo logo de DVD.
3. En `ClickTarget.cs`, `TaskTargetMovement.cs` y `HoverTarget.cs`, cuando `isMoving == true`, la dirección inicial se elige con `Random.insideUnitCircle.normalized`, lo que a veces genera ángulos casi horizontales o verticales en vez del movimiento diagonal clásico de protector de pantalla de DVD.
4. En `BaseTask.CalculateRatioAndSizeScore` (líneas 96–126), los puntos por precisión y tamaño (`100` centro, `75` medio, `45` exterior + `sizeBonus` hasta `+50`) **no distinguen entre objetivos estacionarios, circulares o de rebote DVD**.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/Data/TaskConfig.cs`
- `ComputerLearner/Assets/Scripts/Tasks/BaseTask.cs`
- `ComputerLearner/Assets/Scripts/Tasks/ClickTarget.cs`
- `ComputerLearner/Assets/Scripts/Tasks/ClickTargetTask.cs`
- `ComputerLearner/Assets/Scripts/Tasks/TaskTargetMovement.cs`
- `ComputerLearner/Assets/Scripts/Tasks/HoverTarget.cs`
- `ComputerLearner/Assets/Scripts/Tasks/HoverTargetTask.cs`
- `ComputerLearner/Assets/Scripts/Tasks/DoubleClickTask.cs`
- `ComputerLearner/Assets/Scripts/Tasks/RightClickTask.cs`
- `ComputerLearner/Assets/Scripts/Tasks/ClickHoldTask.cs`

### Especificación Técnica
1. **Definir el enum `TargetMovementType` en `TaskConfig.cs`:**
   ```csharp
   public enum TargetMovementType
   {
       Stationary = 0, // Fijo en su posición (menor puntuación base)
       Circular   = 1, // Movimiento circular/elíptico pequeño alrededor de su punto base (+25 pts)
       DvdBounce  = 2  // Movimiento lineal diagonal rebotando en los bordes como el logo de DVD (+50 pts)
   }
   ```
2. **Resolución coherente del modo de movimiento (`BaseTask.ResolveTargetMovementType`):**
   - Implementar en `BaseTask.cs` un método estático `ResolveTargetMovementType(TaskDefinition def, int targetIndex = 0, int totalTargets = 1)`:
     - Si `def == null || def.config == null`: devolver `TargetMovementType.Stationary`.
     - Si `def.config.isMoving` es `true` o `def.difficulty >= 3` (en tareas de un solo objetivo como `Hover`, `DoubleClick`, `RightClick`):
       - `difficulty == 1`: `TargetMovementType.Stationary`
       - `difficulty == 2`: `TargetMovementType.Circular`
       - `difficulty >= 3` (o `def.config.isMoving == true`): `TargetMovementType.DvdBounce`
     - Para `ClickTargetTask` cuando una misma tarea spawnea múltiples objetivos (`totalTargets > 1`, ej. `difficulty == 2` con 2 objetivos o `difficulty == 3` con 3 objetivos):
       - `difficulty == 1`: `Stationary`
       - `difficulty == 2`: `Circular`
       - `difficulty == 3`: combinar o asignar `DvdBounce` (o cuando `totalTargets == 3`, spawnear 1 `Stationary`, 1 `Circular` y 1 `DvdBounce` — o todos `DvdBounce` en `d3` ya que `d1` es `Stationary` y `d2` es `Circular`), garantizando que en el Nivel 3 el jugador experimente claramente los tres tipos de objetivos.
     - Para `ClickHoldTask` (`Level 7`):
       - `difficulty == 1`: `Stationary`
       - `difficulty == 2` y `3`: `Circular` (con radio suave `18f x 14f` y velocidad angular moderada para que el niño pueda mantener el cursor dentro sin frustración)
       - `difficulty >= 4`: `DvdBounce` con velocidad suave (`45f` u/s) adecuada para mantener pulsado mientras sigue al objetivo.
3. **Movimiento diagonal auténtico tipo Logo de DVD (`GetRandomDvdDirection`):**
   - En `BaseTask.cs` (o helper compartido usado por `ClickTarget`, `TaskTargetMovement` y `HoverTarget`), crear `GetRandomDvdDirection()`:
     - Elegir un ángulo entre `28°` y `62°` (`float rad = Random.Range(28f, 62f) * Mathf.Deg2Rad;`) con signos aleatorios `(Random.value < 0.5f ? -1f : 1f)` en X e Y:
       `new Vector2(Mathf.Cos(rad) * signX, Mathf.Sin(rad) * signY).normalized;`
     - Así el objetivo siempre se desplaza en diagonal constante y rebota limpiamente en las 4 paredes de la ventana igual que el salvapantallas de DVD.
   - Asegurar una velocidad por defecto fluida para `DvdBounce` cuando `cfg.moveSpeed <= 0f` (ej. `115f` en `ClickTarget`, `85f` en `HoverTarget`, `95f` en `DoubleClickTask`/`RightClickTask`, `45f` en `ClickHoldTask`).
4. **Escalado de puntos según el tipo de movimiento (`Stationary < Circular < DvdBounce`):**
   - En `BaseTask.cs`, añadir:
     ```csharp
     public static int GetMovementBonusPoints(TargetMovementType movementType)
     {
         switch (movementType)
         {
             case TargetMovementType.Circular:
                 return 25;
             case TargetMovementType.DvdBounce:
                 return 50;
             case TargetMovementType.Stationary:
             default:
                 return 0;
         }
     }
     ```
   - Actualizar `CalculatePrecisionAndSizeScore`, `CalculateRatioAndSizeScore`, `CalculateMaxPossiblePrecisionScore` y `GetStandardizedTargetPoints` con un parámetro opcional `TargetMovementType movementType = TargetMovementType.Stationary`:
     - Sumar `GetMovementBonusPoints(movementType)` tanto a la puntuación obtenida como al máximo posible de ese objetivo (`CalculateMaxPossiblePrecisionScore(targetRect, movementType)`).
     - De esta forma:
       - Un objetivo **Estacionario** da la menor cantidad de puntos (`100` base en centro + bono de tamaño).
       - Un objetivo **Circular** da más puntos (`+25` pts extra).
       - Un objetivo **Rebote DVD** da la mayor cantidad de puntos (`+50` pts extra).
       - El denominador `maxPossibleScore` suma el máximo exacto de cada objetivo spawneado, manteniendo el formato `X/Y points` 100% exacto.

### Criterios de aceptación
- En los niveles de objetivos (Niveles 2, 3, 4, 5, 6, 7 y 9) aparecen objetivos completamente estacionarios, objetivos con movimiento circular pequeño y objetivos con movimiento diagonal tipo logo de DVD rebotando en los bordes.
- Los textos flotantes de puntuación (`+X`) y el marcador reflejan que los objetivos estacionarios otorgan menos puntos, los circulares otorgan más (`+25`) y los de rebote DVD otorgan el máximo (`+50`).

---

## Sección 2: Puntuación de Doble Clic combinando ambos clics y penalización leve por tardar entre clics

### Diagnóstico de la causa raíz
1. **Solo se evalúa el segundo clic:** En `ComputerLearner/Assets/Scripts/Tasks/DoubleClickTask.cs` (líneas 131–165), cuando el jugador hace el primer clic (rama `else`, línea 154), solo se guarda `lastClickTime = now;` sin evaluar ni almacenar la precisión espacial del primer clic (`eventData.position`). Al llegar el segundo clic, `CalculatePrecisionAndSizeScore` calcula el 100% de los puntos únicamente con la posición del segundo clic.
2. **Bug potencial con `eventData.clickCount >= 2`:** En la línea 136, la condición `if (eventData.clickCount >= 2 || (lastClickTime > 0f && timeSinceLastClick <= DOUBLE_CLICK_THRESHOLD))` puede entrar como segundo clic aunque el primer clic hubiera sido invalidado por salir del objetivo (`OnPointerExit` puso `lastClickTime = -10f`), ya que `EventSystem` mantiene `clickCount >= 2`.
3. **Sin penalización por segundo clic ligeramente tardío:** Actualmente, si el segundo clic ocurre en `0.64s` (justo antes de `DOUBLE_CLICK_THRESHOLD = 0.65f`), el jugador recibe exactamente los mismos puntos que si hace un doble clic inmediato (`<= 0.28s`). El feedback pide restar una pequeña cantidad de puntos si el jugador tarda un poco más de la cuenta entre el primer y el segundo clic (manteniendo la penalización fuerte existente si tarda demasiado y caduca el tiempo de doble clic).

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/Tasks/DoubleClickTask.cs`

### Especificación Técnica
1. **Registrar la precisión del primer clic:**
   - Añadir en `DoubleClickTask.cs`:
     - `private bool hasValidFirstClick = false;`
     - `private int firstClickPrecisionScore = 100;`
     - `private bool firstClickWasOuterRing = false;`
   - Resetear `hasValidFirstClick = false;` en `SetupTask`, `OnPointerExit`, `OnBackgroundClicked`, clic derecho y `WaitAndReportSingleClickError`.
   - Cuando se recibe el **primer clic izquierdo** (`!hasValidFirstClick || timeSinceLastClick > DOUBLE_CLICK_THRESHOLD`):
     - `hasValidFirstClick = true;`
     - `lastClickTime = now;`
     - Calcular y guardar inmediatamente la puntuación del primer clic:
       `firstClickPrecisionScore = BaseTask.CalculatePrecisionAndSizeScore(rt, eventData.position, eventData.pressEventCamera, out firstClickWasOuterRing, movementType);`
     - Iniciar `pendingSingleClickErrorCoroutine = StartCoroutine(WaitAndReportSingleClickError());`.
2. **Combinar la precisión de ambos clics en el segundo clic (`hasValidFirstClick && timeSinceLastClick <= DOUBLE_CLICK_THRESHOLD`):**
   - Calcular la puntuación del segundo clic:
     `int secondClickPrecisionScore = BaseTask.CalculatePrecisionAndSizeScore(rt, eventData.position, eventData.pressEventCamera, out bool secondClickWasOuterRing, movementType);`
   - Combinar ambos clics promediando sus puntuaciones (redondeado a múltiplos de 5 para mantener números limpios, o `Mathf.RoundToInt((firstClickPrecisionScore + secondClickPrecisionScore) * 0.5f)`):
     - Si ambos clics son en el centro: `100%` de los puntos máximos del objetivo (`maxScorePoints`).
     - Si uno es en el centro y otro en el anillo medio (o exterior + medio): puntuación intermedia (no recibe puntos máximos).
     - Si uno es en el centro y otro en el anillo exterior: puntuación menor.
     - Si ambos clics son en el anillo exterior: recibe la puntuación mínima de acierto.
   - Si `firstClickWasOuterRing || secondClickWasOuterRing`, mostrar `VirtualMascot.ShowAlmostOutFeedback(rt)`.
3. **Penalización leve por tardar un poco entre el primer y el segundo clic:**
   - Definir:
     - `private const float IMMEDIATE_DOUBLE_CLICK_WINDOW = 0.30f;` (doble clic rápido/inmediato: `<= 0.30s` -> **0 penalización**, puntos completos).
     - `private const float DOUBLE_CLICK_THRESHOLD = 0.65f;` (ventana máxima antes de fallar como clic simple).
   - Si `timeSinceLastClick > IMMEDIATE_DOUBLE_CLICK_WINDOW` (es decir, entre `0.30s` y `0.65s`):
     - Calcular una reducción leve proporcional a la tardanza entre `5` y `25` puntos (redondeada a 5):
       ```csharp
       float lateRatio = Mathf.Clamp01((timeSinceLastClick - IMMEDIATE_DOUBLE_CLICK_WINDOW) / (DOUBLE_CLICK_THRESHOLD - IMMEDIATE_DOUBLE_CLICK_WINDOW));
       int latePenalty = Mathf.Clamp(Mathf.RoundToInt((lateRatio * 25f) / 5f) * 5, 5, 25);
       combinedScore = Mathf.Max(20, combinedScore - latePenalty);
       ```
     - De este modo:
       - Clics inmediatos (`<= 0.30s`) en el centro ambos -> **100% de los puntos**.
       - Clics ligeramente lentos (`0.30s–0.65s`) -> **completa el objetivo pero con una ligera resta de puntos (`-5` a `-25` pts)** respecto a `maxScorePoints`.
       - Tardar demasiado (`> 0.65s`) -> sigue disparando `WaitAndReportSingleClickError()` con `RegisterAttempt()` y `-50` puntos de penalización.

### Criterios de aceptación
- Hacer doble clic con ambos clics en el centro y de forma inmediata (`<= 0.30s`) otorga el 100% de los puntos del objetivo (`X/X`).
- Hacer el primer clic en el borde y el segundo en el centro (o viceversa) otorga menos puntos que dos clics en el centro, y dos clics en el borde otorgan la menor puntuación.
- Tardar entre `0.30s` y `0.65s` entre el primer y segundo clic completa el objetivo pero resta entre `5` y `25` puntos respecto al máximo posible.

---

## Sección 3: Penalización por tardar demasiado / inactividad prolongada y recordatorios periódicos de Pointy

### Diagnóstico de la causa raíz
1. **Bug — La mascota deja de aparecer si tardas demasiado en completar una tarea:**
   - En `BaseTask.cs` (línea 55) y en todas las subclases (`DoubleClickTask.cs` línea 44, `RightClickTask.cs` línea 38, `ClickHoldTask.cs` línea 64, `DragDropTask.cs` línea 145, `ClickTargetTask.cs` línea 44, `MoveMouseTask.cs` línea 50, `HoverTargetTask.cs` línea 58), los recordatorios de inactividad utilizan booleanos de un solo uso (`hasShownInactivityReminder = true`, `hasShownDelayReminder = true`, `hasShownStartReminder = true`, `hasShownBetweenReminder = true`).
   - En `VirtualMascot.ShowContextualFeedback` (línea 692), la duración de visualización está forzada a `Mathf.Min(duration, 2.0f)` (`1.8f` por defecto).
   - Como consecuencia, a los `5.0s` la mascota aparece fugazmente durante `1.8s–2.0s`, se oculta (`Hide()`), deja `hasShownInactivityReminder = true` y **nunca vuelve a aparecer durante el resto de la tarea** aunque el jugador pase minuto y medio sin avanzar.
   - Además, en `HoverTargetTask.cs` (línea 125), `target.OnHoverActive += NotifyUserInteraction;` hace que si un objetivo en movimiento pasa por debajo del cursor quieto del jugador, `lastInteractionTime` se reinicie solo y `HasEverHovered` pase a `true`, impidiendo que salte el recordatorio.
2. **Falta de penalización por tardar demasiado (1.5 minutos sin hacer nada y obtener puntuación máxima):**
   - En los Niveles 1 a 9 (`BaseTask.cs` y `LevelRunner.cs`), el tiempo empleado no reduce los puntos ganados ni aplica penalización por inactividad prolongada. Un jugador puede quedarse quieto 90 segundos, hacer clic en el centro al final y obtener el 100% de los puntos (`maxPossibleScore`) y medalla de Oro.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/Tasks/BaseTask.cs`
- `ComputerLearner/Assets/Scripts/Tasks/MoveMouseTask.cs`
- `ComputerLearner/Assets/Scripts/Tasks/HoverTargetTask.cs`
- `ComputerLearner/Assets/Scripts/Tasks/ClickTargetTask.cs`
- `ComputerLearner/Assets/Scripts/Tasks/DoubleClickTask.cs`
- `ComputerLearner/Assets/Scripts/Tasks/RightClickTask.cs`
- `ComputerLearner/Assets/Scripts/Tasks/ClickHoldTask.cs`
- `ComputerLearner/Assets/Scripts/Tasks/DragDropTask.cs`
- `ComputerLearner/Assets/Scripts/DesktopSystem/VirtualMascot.cs`

### Especificación Técnica
1. **Recordatorios de inactividad periódicos y legibles (`BaseTask.cs` + subclases + `VirtualMascot.cs`):**
   - En `VirtualMascot.ShowContextualFeedback`:
     - Permitir que las llamadas que especifican una duración explícita (ej. `2.8f`–`4.0f` en recordatorios de tarea) respeten hasta `4.2f` segundos (`Mathf.Clamp(duration, 1.8f, 4.2f)` en lugar de `Mathf.Min(duration, 2.0f)`), para que los niños puedan leer el consejo de Pointy sin que desaparezca al instante.
   - En `BaseTask.cs`:
     - Añadir un método virtual `protected virtual void OnInactivityReminderTriggered()` que por defecto invoque `VirtualMascot.ShowInactivityReminder(GetActiveFeedbackTarget());`, y que cada subclase (`MoveMouseTask`, `HoverTargetTask`, `ClickTargetTask`, `DoubleClickTask`, `RightClickTask`, `ClickHoldTask`, `DragDropTask`) sobrescriba para mostrar su mensaje contextual específico (`moveKeepMoving`, `hoverStartReminder`, `clickDelayReminder`, `doubleClickInactivityReminder`, `rightClickReminder`, `holdInactivityReminder`, `dragInactivityReminder`) durante `3.5f` segundos.
     - Reemplazar el booleano único `hasShownInactivityReminder` por un temporizador periódico:
       - Primer recordatorio tras **`6.0f` segundos** sin progreso/interacción.
       - Si el jugador sigue sin completar el objetivo o interactuar, **volver a mostrar a Pointy cada `7.5f` segundos** (`nextInactivityReminderTime = Time.time + 7.5f`), de modo que Pointy siempre reaparezca para ayudar cuando el jugador tarda demasiado en cualquier tarea.
   - En `HoverTargetTask.cs`:
     - No reiniciar el temporizador de inactividad de toda la tarea si el jugador solo rozó el objetivo un instante; reiniciar la inactividad únicamente mientras está manteniendo activamente el hover (`hoverTime > 0.25f`) o al completar, de forma que si un objetivo móvil pasa bajo el cursor quieto el recordatorio y la penalización de tiempo sigan funcionando.
2. **Penalización de puntos por tardar demasiado en tareas (`BaseTask.cs`):**
   - Implementar un sistema de penalización por tiempo justo para niños (con margen de gracia amplio para juego normal, pero que penaliza quedarse inactivo o tardar demasiado):
     - **A) Penalización periódica por inactividad prolongada:**
       - Mantener un contador `inactivityPenaltyTimer` (basado en el tiempo desde el último progreso o interacción válida; en `MoveMouseTask`, `ClickTargetTask` y `DragDropTask` basado en `lastProgressTime` — última mancha limpiada, último objetivo golpeado o última pieza colocada).
       - Tras un periodo de gracia inicial de **`10.0f` segundos** sin realizar progreso en la tarea actual, cada **`7.0f` segundos adicionales** de inactividad/demora se aplica automáticamente una penalización de tiempo de **`-15` puntos** (`LevelRunner.ApplyMistakePenalty(15)` + popup flotante `-15` sobre el objetivo activo) acompañada del recordatorio de Pointy.
     - **B) Reducción por tiempo excesivo al acertar un objetivo (`CalculateTimeDecayAdjustedPoints`):**
       - Registrar `subTaskStartTime` al iniciar la tarea y al completar cada sub-objetivo (`RecordIntermediateResult(true, ...)`).
       - Definir un tiempo par generoso por sub-objetivo (`graceTime = 6.0f` s para clic/doble clic/clic derecho, `7.5f` s para hover/hold/drag-drop).
       - Si el jugador completa el sub-objetivo dentro de `graceTime`, recibe el **100% de sus puntos**.
       - Si tarda más de `graceTime` (`overtime = (Time.time - subTaskStartTime) - graceTime`), descontar `5` puntos por cada `2.5s` de exceso (hasta reducir un máximo del `40%` de los puntos de ese objetivo), manteniendo `maxPts` intacto en `LevelRunner.AwardHitPoints(adjustedPts, maxPts)`.
   - Resultado: un jugador que juega a ritmo normal (completando cada objetivo en `< 6–7s`) obtiene el **100% de los puntos**, pero alguien que se queda sin hacer nada durante minuto y medio pierde puntos progresivamente tanto durante la espera como al resolver tarde, siendo imposible obtener full points.

### Criterios de aceptación
- Si el jugador deja de actuar o tarda demasiado en cualquier tarea (Niveles 1–9), Pointy aparece a los ~6 segundos y vuelve a aparecer periódicamente cada ~7.5 segundos mientras siga atascado.
- Quedarse sin hacer nada durante un tiempo prolongado (ej. 30–90 segundos) reduce visiblemente la puntuación obtenida frente a la máxima posible (`X/Y points`), impidiendo obtener la puntuación máxima.

---

## Sección 4: Límite de longitud para nombres de archivos y carpetas y límite de elementos por carpeta

### Diagnóstico de la causa raíz
1. **Sin límite de caracteres en nombres de carpetas y archivos:**
   - En `InputModalDialog.cs` (`Initialize`, líneas 44–65) y en `FileExplorerApp.cs` (`CreateSaveDialogBottomBar`, líneas 3616–3629), el componente `TMP_InputField` tiene `characterLimit = 0` (ilimitado).
   - Ni `DraggableIcon.PromptRenameDesktopIcon()` (líneas 1465–1500), ni `FileExplorerApp.PromptRenameItem()` (líneas 2098–2140), ni `FileExplorerApp.ConfirmSaveDialog()` (líneas 3665–3703), ni `GetUniqueItemNameStatic()` (que al copiar y pegar repetidamente concatena `"Copy of Copy of Copy of..."`) truncan la longitud del nombre.
2. **Límite de archivos/carpetas dentro de una carpeta incompleto y silencioso:**
   - `FileExplorerApp.MAX_ITEMS_PER_FOLDER` está fijado en `35` (demasiado alto para el tamaño visual de las carpetas) y **no se comprueba** en `SaveOrCreateTextDocument` (línea 1238), `SaveOrCreateImageDocument` (línea 1288) ni `ConfirmSaveDialog` (línea 3665), permitiendo crear infinitos archivos mediante "Save As" desde Notepad o Paint.
   - En el Escritorio (`DesktopContextMenuTrigger.cs`, líneas 210, 272 y 326), al alcanzar el límite solo se escribe un `Debug.LogWarning` en consola sin dar ningún aviso visual ni deshabilitar la opción en el menú contextual.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/DesktopSystem/InputModalDialog.cs`
- `ComputerLearner/Assets/Scripts/DesktopSystem/FileExplorerApp.cs`
- `ComputerLearner/Assets/Scripts/DesktopSystem/DraggableIcon.cs`
- `ComputerLearner/Assets/Scripts/DesktopSystem/DesktopContextMenuTrigger.cs`
- `ComputerLearner/Assets/Scripts/Data/UIDialogs.cs` + `UIDialogs.json`, `EN-UIDialogs.json`, `ES-UIDialogs.json`, `NO-UIDialogs.json`

### Especificación Técnica
1. **Límite estricto de longitud de nombres (`MAX_BASE_NAME_LENGTH = 16`, `MAX_ITEM_NAME_LENGTH = 20`):**
   - En `FileExplorerApp.cs`:
     - Definir `public const int MAX_BASE_NAME_LENGTH = 16;` (nombre sin extensión) y `public const int MAX_ITEM_NAME_LENGTH = 20;` (incluyendo `.txt` o `.png`).
     - Crear el método estático `public static string ClampAndFormatItemName(string rawName, string requiredExtension = "")`:
       - Limpiar espacios extremos (`Trim()`).
       - Si tiene `requiredExtension` (ej. `".txt"` o `".png"`) al final, separarla temporalmente para obtener el `baseName`.
       - Recortar `baseName` a `MAX_BASE_NAME_LENGTH` caracteres (`baseName.Substring(0, MAX_BASE_NAME_LENGTH).TrimEnd()`).
       - Si tras recortar queda vacío, usar un fallback seguro (`"New Item"`).
       - Re-añadir `requiredExtension` si corresponde.
     - En `GetUniqueItemNameStatic(ExplorerItemData folder, string baseName, string extension)`:
       - Recortar `baseName` antes de añadir sufijos ` (1)`, ` (2)` o `"Copy of "` para que ni siquiera las copias sucesivas superen `MAX_ITEM_NAME_LENGTH` (reservando espacio para ` (N)` si el nombre base ya está en el límite).
   - En `InputModalDialog.cs`:
     - En `Initialize(...)`, configurar `inputField.characterLimit = FileExplorerApp.MAX_ITEM_NAME_LENGTH;` (20 caracteres).
   - En `FileExplorerApp.CreateSaveDialogBottomBar(...)`:
     - Configurar `saveDialogInputField.characterLimit = MAX_BASE_NAME_LENGTH;`.
   - Aplicar `FileExplorerApp.ClampAndFormatItemName(...)` en:
     - `DraggableIcon.PromptRenameDesktopIcon()`
     - `FileExplorerApp.PromptRenameItem()`
     - `FileExplorerApp.ConfirmSaveDialog()`
     - `FileExplorerApp.SaveOrCreateTextDocument()` y `SaveOrCreateImageDocument()`.
2. **Límite de elementos por carpeta (`MAX_ITEMS_PER_FOLDER = 16`):**
   - En `FileExplorerApp.cs`:
     - Ajustar `public const int MAX_ITEMS_PER_FOLDER = 16;` (lo que permite alojar los 16 niveles del sistema en `Levels` y limita cualquier carpeta de usuario, `Desktop`, `Documents` o `Pictures` a un máximo limpio de 16 elementos).
     - Añadir helper `public static bool IsFolderAtCapacity(ExplorerItemData folder)` (`folder != null && folder.children != null && folder.children.Count >= MAX_ITEMS_PER_FOLDER`).
     - En `SaveOrCreateTextDocument`, `SaveOrCreateImageDocument` y `ConfirmSaveDialog`:
       - Si el archivo no es una actualización de un archivo existente con el mismo nombre y `IsFolderAtCapacity(targetFolder)` es `true`, rechazar la creación, reproducir `FeedbackManager.PlayError()`, mostrar el mensaje `UIDialogs.Current.explorer_folder_limit_reached` en el footer del explorador y mediante `VirtualMascot.ShowContextualFeedback(UIDialogs.Current.explorer_folder_limit_reached, ...)`, y devolver `null`.
     - En `CreateNewFolder`, `CreateNewTextDocument` y `CreateNewImageDocument`:
       - Cuando `IsFolderAtCapacity(target)` sea `true`, además de `SetStatusMessage`, reproducir `FeedbackManager.PlayError()` y mostrar `VirtualMascot.ShowContextualFeedback(UIDialogs.Current.explorer_folder_limit_reached, transform as RectTransform, 3.2f)`.
     - En `ShowEmptySpaceContextMenu` (tanto en `FileExplorerApp.cs` como en `DesktopContextMenuTrigger.cs`):
       - Si la carpeta sigue teniendo capacidad (`!IsFolderAtCapacity(folder)`), habilitar las opciones de creación; si ya alcanzó `MAX_ITEMS_PER_FOLDER`, al intentar crear desde cualquier vía (o toolbar) mostrar el aviso claro al jugador.

### Criterios de aceptación
- Ningún archivo ni carpeta puede nombrarse o renombrarse con más de 16 caracteres base (20 con extensión), ni por teclado en los modales de Rename / Save As ni por copias sucesivas (`Copy of...`).
- Ninguna carpeta (incluido Desktop, Documents, Pictures y carpetas creadas por el usuario) permite superar `16` elementos (ni mediante botón `+ New`, ni menú contextual, ni pegar, ni arrastrar, ni `Save As` en Notepad/Paint), informando claramente al usuario cuando la carpeta está llena.

---

## Sección 5: Corrección de los 4 bugs de la mascota Pointy (Tutoriales animados, Flash inicial en esquina y Mascota de escritorio para pistas)

### Diagnóstico de la causa raíz
1. **Bug — "Mascot blocks animated tutorials":**
   - En `GenericLevelWindow.prefab`, `VideoDemoFrame` (`320x184`) está situado en el centro de `IntroCard` (`anchor = (0.5, 0.39)`).
   - En `LevelRunner.ShowInstructionScreen()` (línea 299), durante la secuencia automática (`isAutoSequence`) se llama a `VirtualMascot.Show(livelyMessage, appWindow != null ? appWindow.ContentArea : taskContentArea, new Vector2(0, -80))`.
   - Además, en `VirtualMascot.ShowMessage()` (línea 855):
     `currentTargetOffset = isCelebrationTarget ? (offset ?? GetCelebrationOffsetForTarget(target)) : Vector2.zero;`
     **ignora por completo el parámetro `offset`** para cualquier objetivo que no sea `LevelCelebrationModal`. Como resultado, Pointy y su bocadillo se colocan en el centro exacto de `ContentArea` (o sobre `Btn-IntroStart`), tapando directamente el vídeo del tutorial animado (`VideoDemoFrame`).
2. **Bug — "Pointy appears for a splitsecond in the corner of the desktop when you load it up for the first time":**
   - En `DesktopManager.Awake()` -> `InitializeDesktop()` -> `SpawnVirtualMascot()` (líneas 827–837), cuando `enableMascotCompanion` es `true` (`VirtualDesktop.unity`), se llama inmediatamente a `mascotInstance.SetActive(true)` y `mascot.InitAsDesktopCompanion()` en el frame 0 (`Awake`).
   - Sin embargo, la primera vez que se carga `VirtualDesktop`, `DesktopTour.Start()` (o `VirtualDesktopIntro.Start()`, o `AdvancedDesktopTour.Start()`) espera `yield return new WaitForSeconds(0.5f);` antes de mostrar a Pointy en el centro de la pantalla. Durante ese medio segundo inicial, Pointy aparece en la esquina superior derecha con su bocadillo de mascota de escritorio y luego salta bruscamente al tour.
3. **Bug — "Pointy no longer appears up in the corner in the desktop, to give hints":**
   - En `VirtualMascot.Hide()` (líneas 894–915), cuando se oculta la mascota tras terminar `DesktopTour`, `AdvancedDesktopTour`, `VirtualDesktopIntro`, un nivel guiado o cerrar una ventana de nivel en `VirtualDesktop`, `Hide()` ejecuta `isDesktopCompanion = false; gameObject.SetActive(false);` y **nunca devuelve a Pointy al modo `InitAsDesktopCompanion()` en la esquina superior derecha**.
   - Por ello, una vez que termina cualquier tutorial o nivel en `VirtualDesktop`, Pointy desaparece para siempre de la esquina superior derecha y el jugador ya no puede verla ni hacer clic en ella para recibir pistas.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/DesktopSystem/VirtualMascot.cs`
- `ComputerLearner/Assets/Scripts/DesktopSystem/DesktopManager.cs`
- `ComputerLearner/Assets/Scripts/Managers/LevelRunner.cs`
- `ComputerLearner/Assets/Scripts/DesktopSystem/WindowManager.cs`

### Especificación Técnica
1. **Evitar que Pointy tape los tutoriales animados (`LevelRunner.cs` y `VirtualMascot.cs`):**
   - En `VirtualMascot.cs`:
     - Añadir un método `GetIntroPanelOffsetForTarget(RectTransform target)` similar a `GetCelebrationOffsetForTarget`, o detectar cuando `target` pertenece a `LevelIntroPanel` (`bool isIntroPanelTarget = target != null && (target.name == "VideoDemoFrame" || target.name == "Btn-IntroStart" || target.name == "IntroCard" || target.name == "LevelIntroPanel" || (LevelRunner.ActiveRunner != null && LevelRunner.ActiveRunner.IsShowingIntroPanel));`).
     - Cuando `isIntroPanelTarget` sea `true` (o cuando se pase un `offset` explícito en `LevelIntroPanel`), posicionar a Pointy **al lado derecho (o izquierdo) de `VideoDemoFrame` / `IntroCard`** (p. ej. a `+240px` del centro del vídeo, con el bocadillo abriéndose hacia el exterior lejos del centro de la tarjeta, igual que en `LevelCelebrationModal`), de modo que ni el cuerpo de Pointy ni su bocadillo se solapen jamás con `VideoDemoFrame` ni con `Btn-IntroStart`.
   - En `LevelRunner.ShowInstructionScreen()`:
     - Exponer `public bool IsShowingIntroPanel => introPanel != null && introPanel.activeInHierarchy;`.
     - Tanto en la rama `isAutoSequence` como en la rama con botón `START!`, pasar como `target` el `RectTransform` de `videoDemoFrame` (si está activo) o `introStartButton` con un offset lateral exterior (ej. `new Vector2(250f, 10f)` respecto a `videoDemoFrame`), asegurando que el vídeo central de `320x184` quede 100% despejado.
2. **Eliminar el flash de medio segundo en la esquina al cargar el escritorio por primera vez (`DesktopManager.cs`):**
   - En `DesktopManager.cs`, añadir un helper `public bool ShouldStartOnboardingTourOnLoad()`:
     - Devuelve `true` si `IsVirtualDesktopScene()` y cualquiera de los tours iniciales va a ejecutarse al arrancar (`PlayerPrefs.GetInt("DesktopTourCompleted", 0) == 0`, o `ActiveLearningPath == LearningPath.Intermediate && PlayerPrefs.GetInt("AdvancedDesktopTourCompleted", 0) == 0`, o `PlayerPrefs.GetInt("VirtualDesktopIntroCompleted", 0) == 0`).
   - En `SpawnVirtualMascot()` (llamado desde `Awake()`):
     - Si `ShouldStartOnboardingTourOnLoad()` es `true` (o si estamos en `Desktop1` / `Desktop2`), **NO llamar a `InitAsDesktopCompanion()` en `Awake()`**, sino dejar `mascotInstance.SetActive(false)` para que el tour (`DesktopTour`, `AdvancedDesktopTour` o `VirtualDesktopIntro`) la active directamente en su posición inicial sin ningún parpadeo previo en la esquina.
3. **Restaurar a Pointy en la esquina superior derecha del escritorio para dar pistas (`VirtualMascot.cs`, `DesktopManager.cs`, `WindowManager.cs`):**
   - En `VirtualMascot.cs`, añadir un método estático/instancia `ShouldBeInDesktopCompanionMode()`:
     - Devuelve `true` cuando:
       - `DesktopManager.HasInstance && DesktopManager.Instance != null && DesktopManager.Instance.EnableMascotCompanion`
       - **Y** no hay ningún tour corriendo ni pendiente de iniciar (`!DesktopTour.IsTourRunning`, `!AdvancedDesktopTour.IsTourRunning`, `FindAnyObjectByType<VirtualDesktopIntro>() == null`, `!DesktopManager.Instance.ShouldStartOnboardingTourOnLoad()`)
       - **Y** no hay ningún nivel guiado corriendo (`!VirtualDesktopGuidedLevels.IsLevelRunning`)
       - **Y** no hay ningún `LevelRunner` activo con un nivel en curso o modal de celebración abierto (`(LevelRunner.ActiveRunner == null || !LevelRunner.ActiveRunner.gameObject.activeInHierarchy)` y `FindAnyObjectByType<LevelCelebrationModal>() == null`).
   - Actualizar `VirtualMascot.Hide()`:
     - Si `ShouldBeInDesktopCompanionMode()` es `true`, en lugar de desactivar el GameObject (`gameObject.SetActive(false)`), transicionar limpiamente a **`InitAsDesktopCompanion(showWelcomeBubble: !hasShownInitialCompanionWelcome)`** en la esquina superior derecha.
     - Si `ShouldBeInDesktopCompanionMode()` es `false` (por ejemplo, cuando termina un feedback contextual de 3 segundos en mitad de un nivel `LevelRunner` o nivel guiado), ocultar normalmente (`gameObject.SetActive(false)`).
   - Además, en `WindowManager.OnWindowChanged` / `UnregisterWindow` (o en el callback de cierre de `LevelCelebrationModal` en `LevelRunner.cs` y `VirtualDesktopGuidedLevels.cs`):
     - Cuando se cierra la ventana de un nivel y el jugador regresa al escritorio `VirtualDesktop`, si `VirtualMascot.Instance != null && !VirtualMascot.Instance.IsDesktopCompanion && VirtualMascot.Instance.ShouldBeInDesktopCompanionMode()`, llamar a `VirtualMascot.Instance.InitAsDesktopCompanion(showWelcomeBubble: false)` para que Pointy vuelva siempre a su esquina en el escritorio.
   - En modo `DesktopCompanion` (`UpdateCompanionPosition()`):
     - Mantener el clic sobre Pointy (`OnPointerClick` -> `TriggerCompanionDialogue()`) para pedir pistas en cualquier momento, y añadir un temporizador suave de pistas periódicas en el escritorio (p. ej. si el jugador está en el escritorio sin ventanas de nivel abiertas y sin bocadillo activo durante `25f` segundos, Pointy muestra automáticamente un consejo útil de `companionMessages`).

### Criterios de aceptación
- En la pantalla de instrucciones de cualquier nivel con vídeo (`LevelIntroPanel`), Pointy y su bocadillo se sitúan a un lado y nunca tapan el tutorial animado (`VideoDemoFrame`).
- Al entrar por primera vez al escritorio (`VirtualDesktop` o `Desktop1`), Pointy no aparece durante una fracción de segundo en la esquina superior derecha antes de empezar el tour.
- Una vez completados los tours iniciales y siempre que el jugador esté en el escritorio `VirtualDesktop` fuera de un nivel activo, Pointy permanece en la esquina superior derecha lista para dar pistas al hacerle clic o periódicamente.

---

## Sección 6: Corregir el cambio de tamaño de la Papelera de Reciclaje al vaciarla

### Diagnóstico de la causa raíz
1. Al inspeccionar las dimensiones en píxeles y cajas delimitadoras (bounding box) de los sprites de la papelera:
   - **Papelera llena (`RecyclingBin_Icon_01.png` en `Assets/Art/UI/App icons/` y `Assets/Resources/`):** El lienzo de la textura mide **`654 × 654` px**, y el dibujo de la papelera ocupa **`555 × 611` px** (`BBox: (41, 23)` a `(595, 633)`).
   - **Papelera vacía (`RecyclingBin_Icon_01_Empty.png` en `Assets/Art/UI/App icons/` y `Assets/Resources/`):** El dibujo de la papelera tiene exactamente la misma escala en píxeles (**`554 × 631` px**, `BBox: (162, 113)` a `(715, 743)`), pero fue exportado sobre un lienzo mucho mayor de **`877 × 877` px** con `~115 px` de margen transparente extra alrededor.
2. Cuando `RecycleBinIcon.UpdateVisualState()` cambia `binImage.sprite` de `fullBinSprite` (`654×654`) a `emptyBinSprite` (`877×877`) dentro del mismo `RectTransform` (`105×105` en el escritorio), Unity escala la textura `877×877` entera dentro del rectángulo de `105×105`, haciendo que la papelera vacía se vea un **~25% más pequeña** (`654 / 877 = 0.745`) que la papelera llena.

### Archivos a modificar
- `ComputerLearner/Assets/Art/UI/App icons/RecyclingBin_Icon_01_Empty.png`
- `ComputerLearner/Assets/Resources/RecyclingBin_Icon_01_Empty.png`
- `ComputerLearner/Assets/Scripts/DesktopSystem/RecycleBinIcon.cs`
- `ComputerLearner/Assets/Scripts/DesktopSystem/FileExplorerApp.cs` (para que el icono de `Recycle Bin` dentro de `This PC` también use el sprite lleno/vacío consistente)

### Especificación Técnica
1. **Alinear y recortar `RecyclingBin_Icon_01_Empty.png` a `654 × 654` px:**
   - En `RecyclingBin_Icon_01.png` (`654×654`), el cuerpo de la papelera ocupa `x: 41..595` y su base inferior está en `y = 633`.
   - En `RecyclingBin_Icon_01_Empty.png` (`877×877`), el mismo cuerpo de la papelera ocupa `x: 162..715` y su base inferior está en `y = 743` (un desplazamiento exacto de `dx = 162 - 41 = 121` px y `dy = 743 - 633 = 110` px).
   - Recortar mediante un script de PowerShell (`System.Drawing.Bitmap`) ambos archivos `RecyclingBin_Icon_01_Empty.png` (`Assets/Art/UI/App icons/` y `Assets/Resources/`) usando el rectángulo de origen `(x = 121, y = 110, width = 654, height = 654)`.
   - De este modo, `RecyclingBin_Icon_01_Empty.png` tendrá exactamente **`654 × 654` px** y el cuerpo de la papelera coincidirá píxel a píxel (`x: 41..594`, base `y = 633`) con `RecyclingBin_Icon_01.png`. Al vaciar o llenar la papelera, solo desaparecerán/aparecerán los papeles arrugados de la boca superior sin que el cubo cambie de tamaño ni se desplace un solo píxel.
2. **En `RecycleBinIcon.cs` y `FileExplorerApp.cs`:**
   - Asegurar en `RecycleBinIcon.UpdateVisualState()` que `binImage.preserveAspect = true;` y que al vaciar la papelera tanto el icono del escritorio como el elemento `"Recycle Bin"` en `FileExplorerApp` actualicen su sprite inmediatamente.

### Criterios de aceptación
- `RecyclingBin_Icon_01.png` y `RecyclingBin_Icon_01_Empty.png` tienen exactamente las mismas dimensiones (`654 × 654` px) y la misma alineación del cubo.
- Al vaciar la Papelera de Reciclaje (o al tirar un archivo dentro), el icono cambia entre lleno y vacío manteniendo exactamente el mismo tamaño y posición en pantalla.

---

## Sección 7: Verificación Integral y Documentación

1. Compilar `ComputerLearner/Assembly-CSharp.csproj` verificando **0 errores de compilación**.
2. Validar la sintaxis de todos los archivos JSON en `ComputerLearner/Assets/Resources/`.
3. Actualizar `Documentation/Specs/Diego.md` con una nueva sección numerada documentando todas las soluciones arquitectónicas de esta iteración.
