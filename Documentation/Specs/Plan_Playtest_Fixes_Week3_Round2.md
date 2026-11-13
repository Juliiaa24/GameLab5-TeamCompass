# Plan de Correcciones y Pulido (Playtest Semana 3 - Ronda 2) — Computer Learning (Unity 6)

> **Documento de especificación técnica para ejecución secuencial con `/goal`.**
> Este plan resuelve los 7 puntos de feedback de la segunda ronda de playtest: detección lenta al arrastrar la ventana en Tutorial 2 (`AdvancedDesktopTour`), desajuste entre el diálogo de Nivel 10 (`.txt`) y el menú contextual (`+ New Folder`), bug de `3600` puntos máximos en Nivel 1 (`MoveMouseTask` + `BaseTask`), transparencia/coherencia en la evaluación de niveles guiados 10–16, errata `"Click abd drag"`, desaparición de la mascota al cerrar y reabrir Paint/Notepad en mitad de un nivel guiado, y ampliación del nivel de Paint (`level16`) para incluir tamaños de pincel y colores.

---

## Reglas Generales de Arquitectura, Alcance y Contexto

1. **Alcance estricto**:
   - **NO añadir nuevos minijuegos ni rediseñar la UI general** (el aspecto visual completo queda para Norwegian Game Awards).
   - **NO volver a ralentizar 3x los diálogos de `VirtualMascot`** (ya se revirtió a petición del usuario). Mantener intacto el skip con `Space` / clic.
   - **NO reemplazar los iconos** de Double-Click, Right-Click ni Click-&-Hold.
2. **Preservar la arquitectura actual**: Respetar el namespace `ComputerLearning`, prefabs (`BaseWindow.prefab`, `PaintContent.prefab`) y eventos desacoplados.
3. **Input System**: El proyecto usa el **New Input System** (`activeInputHandler: 1`). Cualquier lectura directa debe usar `UnityEngine.InputSystem` (con `#if ENABLE_LEGACY_INPUT_MANAGER` solo como fallback existente).
4. **Localización sincronizada**: Mantener sincronizados los 4 recursos de diálogos guiados (`GuidedLevelsDialogs.json`, `EN-GuidedLevelsDialogs.json`, `ES-GuidedLevelsDialogs.json`, `NO-GuidedLevelsDialogs.json`) y `VirtualDesktopGuidedLevels.cs` (`GuidedLevelsDialogs`), así como `UIDialogs.cs` y sus 4 JSON si se añaden claves de UI.
5. **Validación continua**: Validar cada archivo modificado con `Unity_ValidateScript` y verificar `Unity_ReadConsole` (0 errores de compilación, 0 excepciones).
6. **Documentación**: Al completar las secciones, actualizar `Documentation/Specs/Diego.md` con el resumen técnico.

---

## Sección 1: Detección lenta al arrastrar la ventana en Tutorial 2 (`AdvancedDesktopTour`)

### Diagnóstico de la causa raíz
1. **Umbral en coordenadas mundo/pantalla sin compensar escala (`AdvancedDesktopTour.cs`, líneas 275–278):**
   - `startWinPos` captura `windowRect.position` y exige `Vector3.Distance(startWinPos, windowRect.position) > 50f`. Como `VirtualDesktop.unity` usa `CanvasScaler` con `ScaleWithScreenSize` (`1920x1080`), en resoluciones o ventanas del Editor más pequeñas (`scaleFactor < 1`, p. ej. 1280×720) mover 50 unidades de `position` requiere arrastrar **75+ píxeles lógicos de canvas**. Movimientos cortos o moderados no cruzan el umbral y el jugador cree que la mascota no lo detecta hasta varios segundos después cuando vuelve a moverla más lejos (a menudo tras saltar el recordatorio a los `8.0s`).
2. **Posicionamiento del bocadillo y botones en `WindowTop` (`AdvancedDesktopTour.cs`, líneas 251 y 267–268):**
   - En la línea 267 se llama `VirtualMascot.Show(dialogs.step4_moveWindow, windowRect, new Vector2(0, 200))`. En una `BaseWindow` (`640x450`), `offset = (0, 200)` sitúa a la mascota/bocadillo justo encima de `WindowTop` (que está a `y = +225` respecto al centro de la ventana), tapando visualmente la barra que el jugador debe arrastrar hasta que a los `8s` el recordatorio cambia el objetivo a `dragBarRect`.
   - `TutorialBlocker.SetAllowedTarget(dragBarRect)` permite clic en toda `WindowTop`, incluidos sus hijos `Close`, `Maximize` y `Minimize`; además no escucha directamente el evento de arrastre de `WindowDrag.cs`.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/DesktopSystem/WindowDrag.cs`
- `ComputerLearner/Assets/Scripts/DesktopSystem/AdvancedDesktopTour.cs`

### Pasos de implementación
1. En **`WindowDrag.cs`**:
   - Añadir eventos ligeros (o callback de instancia/estático), p. ej. `public static event Action<Window> OnWindowDragMoved;` (invocado dentro de `OnDrag` cuando `window.localPosition` haya cambiado respecto a la posición de inicio de arrastre).
2. En **`AdvancedDesktopTour.cs`** (Paso 4: `Move Window`):
   - Anclar `VirtualMascot.Show` directamente a `dragBarRect` con un offset claro que **no tape `WindowTop`** (p. ej. `new Vector2(0, 110)` o lateral, validando con el tamaño de la barra), en lugar de `windowRect` con `(0, 200)`.
   - Excluir `targetWindow.CloseButton`, `targetWindow.MaximizeButton` y `targetWindow.MinimizeButton` mediante `tutorialBlocker?.AddExcludedTarget(...)` durante el paso de mover ventana para evitar cierres o maximizados accidentales al agarrar la barra.
   - Capturar `Vector2 startAnchoredPos = windowRect.anchoredPosition;` (coordenadas de canvas independientes de la resolución) y detectar el movimiento **inmediatamente** cuando `Vector2.Distance(startAnchoredPos, windowRect.anchoredPosition) > 25f` **o** cuando `WindowDrag.OnWindowDragMoved` dispare para `targetWindow` habiéndose desplazado al menos `18f` unidades locales.
   - Desuscribirse del evento `OnWindowDragMoved` tanto al completar el paso como en `StopTour()` / `OnDestroy()`.

### Criterios de aceptación
- Arrastrar la ventana `Practice Note` desde la barra superior en Tutorial 2 (`AdvancedDesktopTour` / `Tutorial2.unity`) es detectado de forma inmediata con un desplazamiento normal (`~25 px` de canvas), a cualquier resolución de pantalla.
- La mascota no tapa la barra de título durante el paso y no se pueden pulsar por accidente los botones `Close`/`Maximize`/`Minimize` mientras se pide arrastrar la ventana.

---

## Sección 2: Menú contextual del Nivel 10 pide crear `.txt` pero solo permite crear carpeta

### Diagnóstico de la causa raíz
1. En `VirtualDesktopGuidedLevels.cs` (línea 393), `RunLevel10ContextMenu` activa `DesktopContextMenuTrigger.RestrictToFolderCreationOnly = true;`.
2. En `DesktopContextMenuTrigger.cs` (líneas 71–74), cuando `RestrictToFolderCreationOnly` es `true`, solo se añade **una** opción al menú contextual: `"+ New Folder"`.
3. Sin embargo, **`EN-GuidedLevelsDialogs.json`** (líneas 4 y 6) dice explícitamente `"Click on '+ New Text Document (.txt)'!"` (ni siquiera menciona carpeta), y los demás JSON (`GuidedLevelsDialogs.json`, `ES-GuidedLevelsDialogs.json`, `NO-GuidedLevelsDialogs.json` y `VirtualDesktopGuidedLevels.cs` líneas 20 y 22) dicen `'+ New Text Document (.txt)' or '+ New Folder'`. El jugador busca `.txt` siguiendo las instrucciones de la mascota y solo encuentra `+ New Folder`.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/DesktopSystem/DesktopContextMenuTrigger.cs`
- `ComputerLearner/Assets/Scripts/DesktopSystem/VirtualDesktopGuidedLevels.cs`
- `ComputerLearner/Assets/Resources/EN-GuidedLevelsDialogs.json`
- (Verificar coherencia en `GuidedLevelsDialogs.json`, `ES-GuidedLevelsDialogs.json`, `NO-GuidedLevelsDialogs.json`)

### Pasos de implementación
1. En **`DesktopContextMenuTrigger.cs`**:
   - Permitir que durante la restricción guiada del Nivel 10 (p. ej. manteniendo `RestrictToFolderCreationOnly` por compatibilidad o añadiendo/renombrando a modo guiado de creación directa en Level 10) el menú muestre **ambas opciones directas de primer nivel** (sin submenú anidado que dificulte `TutorialBlocker`):
     - `"+ New Text Document (.txt)"` -> `CreateTextDocumentOnDesktop`
     - `"+ New Folder"` -> `CreateFolderOnDesktop`
   - Nota: comprobar en `CreateTextDocumentOnDesktop()` (líneas 198+) que al igual que `CreateFolderOnDesktop()` (línea 190: `if (!VirtualDesktopGuidedLevels.IsLevelRunning)`), si existe `PromptRenameDesktopIcon()` no bloquee la finalización del Nivel 10 cuando `VirtualDesktopGuidedLevels.IsLevelRunning` es `true`.
2. En **`EN-GuidedLevelsDialogs.json`** (claves `l10_clickMenu` y `l10_menuSelectReminder`):
   - Actualizarlas para mencionar ambas opciones exactamente como en `GuidedLevelsDialogs.json` (`'+ New Text Document (.txt)' or '+ New Folder'`), quedando todos los idiomas (`EN`, `ES`, `NO`, base) 100% alineados con las etiquetas reales del menú.
3. En **`VirtualDesktopGuidedLevels.cs` (`RunLevel10ContextMenu`)**:
   - Confirmar que tanto `ContextMenuController.OnMenuItemClicked` como `FileExplorerApp.OnFileSystemChanged` completan el nivel limpiamente independientemente de si el alumno elige `.txt` o carpeta, y que `CleanupActiveSubscriptions` desactiva la restricción en todo escenario.

### Criterios de aceptación
- Al hacer clic derecho en el escritorio durante el Nivel 10 aparecen `+ New Text Document (.txt)` y `+ New Folder`.
- Los textos de `EN`, `ES`, `NO` y base coinciden exactamente con las opciones visibles del menú.
- Crear cualquiera de los dos elementos completa el Nivel 10 sin errores ni modales de renombrado bloqueados por `TutorialBlocker`.

---

## Sección 3: Corrección del límite de `3600` puntos máximos en el Nivel 1 (`MoveMouseTask` + `BaseTask`)

### Diagnóstico de la causa raíz
1. En `ComputerLearner/Assets/Scripts/Tasks/MoveMouseTask.cs`:
   - `spotsToSpawn = 24` (línea 12).
   - Al limpiar cada mancha (`OnSpotCleaned`, línea 174), llama `TriggerHitScoreFeedback(spot.transform, 15)`. Es decir, el nivel otorga **15 puntos fijos por mancha** (`24 × 15 = 360` puntos máximos reales).
2. En `ComputerLearner/Assets/Scripts/Tasks/BaseTask.cs` (`TriggerHitScoreFeedback`, alrededor de las líneas 204–221):
   - Se introdujo la rama:
     ```csharp
     else if (overrideFixedPoints > 0 && anchor is RectTransform anchorRt)
         maxPts = Mathf.Max(fixedPts, CalculateMaxPossiblePrecisionScore(anchorRt));
     ```
   - Como cada mancha de `MoveMouseTask` tiene un `RectTransform` de `40x40` px (`diameter < 130`), `CalculateMaxPossiblePrecisionScore(anchorRt)` devuelve `100 + 50 = 150` como máximo posible por mancha en lugar de `15`!
   - Resultado: **`24 × 150 = 3600`** puntos máximos posibles, cuando el jugador solo puede ganar `360` puntos (`360/3600`, lo que además hunde la medalla a Bronce por el cálculo `ProgressData.CalculateMedalFromPoints`).
3. Por el contrario, `ClickTargetTask.cs` es la tarea que realmente usa puntuación variable por anillos de precisión (`CalculatePrecisionScore`), por lo que debe pasar su `overrideMaxPoints` explícitamente en lugar de que `BaseTask` infiera precisión automáticamente a cualquier `overrideFixedPoints > 0` con `RectTransform`.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/Tasks/BaseTask.cs`
- `ComputerLearner/Assets/Scripts/Tasks/ClickTargetTask.cs`
- (Revisar llamadas a `TriggerHitScoreFeedback` en `DoubleClickTask.cs`, `RightClickTask.cs`, `ClickHoldTask.cs`, `HoverTargetTask.cs`, `DragDropTask.cs`)

### Pasos de implementación
1. En **`BaseTask.cs`** (`TriggerHitScoreFeedback`):
   - Cambiar la lógica de `maxPts` para que **solo** use `CalculateMaxPossiblePrecisionScore` cuando se solicite explícitamente mediante `overrideMaxPoints > 0`:
     ```csharp
     int fixedPts = overrideFixedPoints > 0 ? overrideFixedPoints : GetStandardizedTargetPoints();
     int maxPts = overrideMaxPoints > 0 ? Mathf.Max(fixedPts, overrideMaxPoints) : fixedPts;
     ```
   - De este modo, cualquier llamada de puntuación fija como `MoveMouseTask.TriggerHitScoreFeedback(spot.transform, 15)` registra `pts = 15` y `maxPts = 15` (`24 × 15 = 360` total exacto).
2. En **`ClickTargetTask.cs`**:
   - En la llamada a `TriggerHitScoreFeedback(targetRect, earnedPts, ...)` al acertar un objetivo, pasar como tercer argumento (`overrideMaxPoints`) `CalculateMaxPossiblePrecisionScore(targetRect)` (o el máximo correspondiente del target golpeado).
3. **Auditoría rápida de todas las demás `BaseTask`:**
   - Verificar que `DoubleClickTask`, `RightClickTask`, `ClickHoldTask`, `HoverTargetTask` y `DragDropTask` también pasan su `maxPossibleScore` exacto y que sus puntos ganables coinciden 1:1 con `maxPossibleScore` (por ejemplo, si alguna usa `CalculatePrecisionScore`, pasar también `CalculateMaxPossiblePrecisionScore`).

### Criterios de aceptación
- Al completar el Nivel 1 limpiando las 24 manchas sin errores, la pantalla final muestra `360/360 points` (y medalla de Oro), nunca `.../3600`.
- En `ClickTargetTask` (y demás niveles 2–9), acertar fuera del centro sigue mostrando una fracción realista (`X / maxReal`), y acertar en el centro perfecto alcanza el 100% del máximo de ese objetivo.

---

## Sección 4: Claridad y coherencia en la evaluación de los niveles guiados (Niveles 10–16)

### Diagnóstico de la causa raíz
1. **Reglas ocultas e incompatibles respecto a Niveles 1–9:**
   - Niveles 1–9 (`LevelRunner.cs`) evalúan medalla a partir del porcentaje de puntos ganados frente al máximo (`CalculateMedalFromPoints`), mostrando puntos en vivo (`+100`, `+150`, `-20`) en cada acción.
   - Niveles 10–16 (`VirtualDesktopGuidedLevels.cs`, líneas 1565–1668) calculan la medalla **únicamente al final** según **tiempo total** (`durationSeconds`) y **errores** (`mistakesCount`) con umbrales fijos por nivel en `CalculateGuidedLevelMedal`, sin mostrarlos ni explicarlos al alumno.
2. **Denominador `maxPossibleScore` inalcanzable (`VirtualDesktopGuidedLevels.cs`, líneas 1660–1666):**
   - `int maxSpeedBonus = Mathf.RoundToInt(parTime * 15f);` asume `duration = 0.0f` segundos (`600 + 20*15 = 900` en L10, `600 + 50*15 = 1350` en L16), lo cual es físicamente imposible incluso en una partida perfecta instantánea. Un jugador con medalla de Oro ve p. ej. `750/900` sin entender qué puntos faltaron.
3. **Falta de explicación visible al finalizar (`LevelCelebrationModal.cs`):**
   - El modal muestra `Points`, `Accuracy`, `Time` y `Mistakes`, pero no aclara cómo influyen tiempo y errores en los niveles guiados ni qué umbral hacía falta para Oro/Plata.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/DesktopSystem/VirtualDesktopGuidedLevels.cs`
- `ComputerLearner/Assets/Scripts/UI/LevelCelebrationModal.cs`
- `ComputerLearner/Assets/Scripts/Data/UIDialogs.cs` + `UIDialogs.json`, `EN-UIDialogs.json`, `ES-UIDialogs.json`, `NO-UIDialogs.json`

### Pasos de implementación
1. En **`VirtualDesktopGuidedLevels.cs`**:
   - Ajustar `CompleteLevel` para que `maxPossibleScore` sea **alcanzable** al cumplir el tiempo objetivo de Oro sin errores:
     - Definir `GetGoldTimeForLevel(string cleanId)` con los mismos umbrales exactos de `CalculateGuidedLevelMedal` (`12s` L10, `20s` L11, `25s` L12, `18s` L13, `20s` L14, `35s` L15, ajustar L16 según Sección 7).
     - Hacer que si `duration <= goldTime && mistakes == 0`, se obtenga el bonus completo de velocidad (o calcular `speedBonus` desde una referencia alcanzable como `Mathf.RoundToInt(Mathf.Clamp01((parTime * 1.4f - duration) / Mathf.Max(1f, parTime * 1.4f - goldTime)) * maxSpeedBonus)`), de manera que lograr Oro sin errores otorgue el **100%** (`finalScore == maxPossibleScore`, p. ej. `1000/1000` o `900/900`) y no un máximo fantasma para `t = 0s`.
   - Dar feedback visual de puntos durante el nivel o al registrar error (opcionalmente un pequeño texto/nota en `VirtualMascot` si comete fallo: recordar que cada fallo resta `-50` pts y cuenta para la medalla).
2. En **`LevelCelebrationModal.cs`** y **`UIDialogs` (4 idiomas + `.cs`)**:
   - Añadir una línea breve y amigable para niveles guiados (`level10`–`level16`) debajo de `StatsText` (sin recargar la tarjeta ni cambiar su diseño general) indicando la regla de evaluación de forma transparente y localizada, p. ej.:
     - Oro / Plata alcanzado vs criterio: `"Gold: <= {0}s & 0 mistakes | Each mistake: -50 pts"` (en `EN`, `ES`, `NO`, base), mostrando el tiempo objetivo real de ese nivel (`GetGoldTimeForLevel`).

### Criterios de aceptación
- Al lograr medalla de Oro con `0` errores dentro del tiempo objetivo en un nivel guiado (10–16), es posible alcanzar la puntuación máxima mostrada (`X/X points`).
- El modal de resultados deja claro al jugador por qué obtuvo su medalla/puntuación (criterio de tiempo para Oro y penalización por errores).

---

## Sección 5: Errata `"Click abd drag"` y revisión ortográfica de diálogos

### Diagnóstico de la causa raíz
1. En `ComputerLearner/Assets/Resources/EN-GuidedLevelsDialogs.json` (línea 17):
   - `"l12_copyPrompt": "Let's master keyboard shortcuts!\nClick abd drag the word 'Cat' and press Ctrl + C on your keyboard to copy it."` contiene la errata `"Click abd drag"` en lugar de `"Click and drag"`.
2. Se buscó `\babd\b` en todo `ComputerLearner/Assets/` (`.cs` y `.json`) y esa es la única ocurrencia de `"abd"`, pero conviene revisar también todas las líneas de `l12_` y `GuidedLevelsDialogs` para uniformar terminología.

### Archivos a modificar
- `ComputerLearner/Assets/Resources/EN-GuidedLevelsDialogs.json`

### Pasos de implementación
1. En **`EN-GuidedLevelsDialogs.json`** (línea 17):
   - Corregir `"Click abd drag"` -> `"Click and drag"`.
2. Revisar las demás claves `l10_`..`l16_` en los 4 JSON (`GuidedLevelsDialogs.json`, `EN-`, `ES-`, `NO-`) por si existe cualquier otra errata menor de tecleo o espacio doble.

### Criterios de aceptación
- `Select-String -Pattern "\babd\b"` sobre `ComputerLearner/Assets/` devuelve 0 coincidencias.
- El texto mostrado en pantalla al iniciar el Nivel 12 en inglés lee correctamente `"Click and drag..."`.

---

## Sección 6: La mascota desaparece si se cierra Paint (o Notepad) en mitad del nivel guiado

### Diagnóstico de la causa raíz
1. En `Window.cs` (línea 209), al cerrar cualquier ventana (`CloseWindow()`), se ejecuta `VirtualMascot.HideMascotIfTargeting(transform);`.
2. Durante `RunLevel16PaintUsage` (y también en `RunLevel11NotepadTyping`–`RunLevel15NotepadFull`), `VirtualMascot` está anclado a `winRect` (`win.transform`), por lo que `HideMascotIfTargeting` oculta inmediatamente la mascota (`Instance.Hide()`).
3. Después, en `RunLevel16PaintUsage` (líneas 1394–1397 y 1423–1426 — y falta en el bucle de guardado líneas 1450–1457!), `RecoverPaintWindow(ref winRect)`:
   - Además de no comprobar `w.IsClosed` (espera ~0.2s a que termine `AnimateScale` y desactive el GameObject), **reabre Paint y actualiza `tutorialBlocker`, pero NUNCA vuelve a mostrar `VirtualMascot` con el diálogo del paso activo!**
   - Resultado: Paint se vuelve a abrir, pero la mascota permanece oculta hasta que el jugador completa el paso a ciegas y avanza al siguiente. Lo mismo ocurre en `RecoverNotepadWindow` (`VirtualDesktopGuidedLevels.cs`, línea 1482).

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/DesktopSystem/VirtualDesktopGuidedLevels.cs`

### Pasos de implementación
1. En **`VirtualDesktopGuidedLevels.cs`**:
   - Actualizar `RecoverPaintWindow(ref RectTransform winRect, string activeStepPrompt = null, Vector2? mascotOffset = null)` (y de forma análoga `RecoverNotepadWindow(ref RectTransform winRect, string restoreText = null, string activeStepPrompt = null, Vector2? mascotOffset = null)`):
     - Detectar también si la ventana encontrada tiene `Window w != null && w.IsClosed` para recuperar al instante sin esperar al final de la animación de cierre de `0.2s`.
     - Una vez obtenida la nueva ventana y actualizado `tutorialBlocker?.SetAllowedTarget(winRect)`: si `!string.IsNullOrEmpty(activeStepPrompt)`, llamar a **`VirtualMascot.Show(activeStepPrompt, winRect, mascotOffset ?? new Vector2(0, 180))`** para reanclar y hacer visible de nuevo a la mascota en el paso actual!
   - En `RunLevel16PaintUsage`:
     - Pasar el texto del paso activo (`l16_drawPrompt`, `l16_colorPrompt` / `l16_sizePrompt` de Sección 7, `l16_undoPrompt`) al llamar a `RecoverPaintWindow`.
     - Añadir también comprobación y recuperación en el **paso final de guardado (`Ctrl + S`)**: si `!FileExplorerApp.IsSaveDialogOpen && (paint == null || !paint.gameObject.activeInHierarchy || win == null || win.IsClosed)`, llamar a `RecoverPaintWindow(ref winRect, dialogs.l16_savePrompt)` (y mantener `tutorialBlocker?.ClearTarget()` para permitir interactuar con el diálogo de guardado).
   - Revisar `RunLevel11`–`RunLevel15` para pasar el diálogo del paso actual a `RecoverNotepadWindow` cuando Notepad se cierre en mitad de un paso.

### Criterios de aceptación
- Si el jugador pulsa la `X` para cerrar Paint en cualquiera de los pasos activos del nivel (dibujar, elegir color/tamaño, deshacer o antes de abrir Guardar), Paint se reabre automáticamente y **la mascota reaparece inmediatamente** sobre la nueva ventana mostrando la instrucción del paso actual.
- Ídem si cierra Notepad durante los niveles 11–15.

---

## Sección 7: Ampliar el nivel de Paint (`level16`) para incluir tamaños de pincel y colores

> **Nota sobre numeración:** En el feedback de playtest se menciona Paint como *"nivel 15"*; en el código y assets del proyecto (`L_Level16.asset`, `VirtualDesktopGuidedLevels.cs` línea 1345, `ProgressData.cs`), el nivel de Paint es **`level16` (`Level 16: Paint Usage` -> `RunLevel16PaintUsage`)**, siendo `level15` la evaluación de Notepad (`Level 15: Notepad Evaluation`).

### Diagnóstico de la causa raíz
1. `PaintApp.cs` ya dispone en `PaintContent.prefab` de botones funcionales para tamaño de pincel (`btnSizeS` = radio 2, `btnSizeM` = radio 4, `btnSizeL` = radio 9 en líneas 720–722) y paleta de colores (`swatchButtons` en líneas 724–735).
2. Sin embargo, `PaintApp` solo expone eventos estáticos para trazo (`OnStrokeDrawn`), deshacer (`OnPaintUndo`) y atajo guardar (`OnPaintSaveShortcut`), sin eventos para cambio de color ni de tamaño.
3. `RunLevel16PaintUsage` (`VirtualDesktopGuidedLevels.cs`, líneas 1345–1465) únicamente pide: Abrir Paint -> Dibujar cualquier trazo -> `Ctrl + Z` -> `Ctrl + S`. Aunque el texto `l16_drawPrompt` menciona *"Pick any color from the palette"*, el nivel nunca verifica que el jugador haya elegido un color ni probado a cambiar el tamaño del pincel.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/DesktopSystem/PaintApp.cs`
- `ComputerLearner/Assets/Scripts/DesktopSystem/VirtualDesktopGuidedLevels.cs`
- `ComputerLearner/Assets/Resources/GuidedLevelsDialogs.json`
- `ComputerLearner/Assets/Resources/EN-GuidedLevelsDialogs.json`
- `ComputerLearner/Assets/Resources/ES-GuidedLevelsDialogs.json`
- `ComputerLearner/Assets/Resources/NO-GuidedLevelsDialogs.json`

### Pasos de implementación
1. En **`PaintApp.cs`**:
   - Añadir eventos estáticos públicos junto a los existentes (líneas 29–31):
     - `public static event Action<Color> OnColorSelected;`
     - `public static event Action<int> OnBrushSizeChanged;`
   - Invocar `OnColorSelected?.Invoke(c);` al final de `SelectColor(Color c, int swatchIndex)` (línea 783+).
   - Invocar `OnBrushSizeChanged?.Invoke(radius);` al final de `SelectBrushSize(int radius)` (línea 776+).
2. En **`VirtualDesktopGuidedLevels.cs` (`GuidedLevelsDialogs` y `RunLevel16PaintUsage`)**:
   - Añadir nuevas claves localizadas en `GuidedLevelsDialogs` (con defaults en inglés):
     - `l16_colorPrompt` / `l16_colorReminder` / `l16_colorDone` (elegir un color de la paleta y dibujar un trazo con él).
     - `l16_sizePrompt` / `l16_sizeReminder` / `l16_sizeDone` (cambiar el tamaño del pincel — S, M o L — y dibujar otro trazo).
     - Actualizar `def_l16_desc` para reflejar los nuevos pasos (elegir color, tamaño de pincel, dibujar, deshacer y guardar).
   - Estructurar `RunLevel16PaintUsage` en pasos claros y progresivos (todos protegidos contra cierre con `RecoverPaintWindow`):
     1. **Abrir Paint** (doble clic en icono).
     2. **Elegir color y dibujar:** Esperar a que el alumno seleccione un color (`PaintApp.OnColorSelected`) y realice un trazo (`PaintApp.OnStrokeDrawn`) — o permitir dibujar tras cambiar color; si dibuja sin haber cambiado color aún, recordar amablemente elegir un color de la paleta (sin penalizar como error, solo recordatorio contextual).
     3. **Cambiar tamaño de pincel y dibujar:** Pedir elegir un tamaño de pincel (`S`, `M` o `L` -> `PaintApp.OnBrushSizeChanged`) y dibujar otro trazo (`PaintApp.OnStrokeDrawn`).
     4. **Deshacer con `Ctrl + Z`** (`PaintApp.OnPaintUndo`).
     5. **Guardar dibujo con `Ctrl + S`** (`FileExplorerApp.OnFileSystemChanged`).
   - Ajustar **`CalculateGuidedLevelMedal`**, **`GetParTimeForLevel`**, **`GetGoldTimeForLevel`** (Sección 4) y **`GetExpectedActionsForLevel`** para `"level16"` acorde al flujo ampliado (p. ej. Oro `<= 55s` y 0 errores, Plata `<= 90s` y `<= 2` errores, `parTime = 65f`, `expectedActions = 10`), evitando que el paso extra haga injusta la medalla de Oro.
3. En los 4 JSON (`GuidedLevelsDialogs.json`, `EN-`, `ES-`, `NO-`):
   - Añadir las nuevas claves `l16_colorPrompt`, `l16_colorReminder`, `l16_colorDone`, `l16_sizePrompt`, `l16_sizeReminder`, `l16_sizeDone` y actualizar `def_l16_desc` en inglés, español y noruego.

### Criterios de aceptación
- Durante `Level 16: Paint Usage`, el jugador practica explícitamente: seleccionar un color de la paleta, cambiar el tamaño de pincel (`S`/`M`/`L`), dibujar trazos, deshacer con `Ctrl + Z` y guardar con `Ctrl + S`.
- Cerrar Paint en cualquiera de esos nuevos pasos reabre la ventana y restaura a la mascota con la instrucción del paso correspondiente (Sección 6).
- Los tiempos de medalla Oro/Plata para `level16` son alcanzables con el nuevo número de pasos.

---

## Verificación Final y Cierre

1. Ejecutar `Unity_ValidateScript` sobre todos los scripts `.cs` modificados en las Secciones 1–7.
2. Ejecutar `Unity_ReadConsole` confirmando **0 errores** de compilación / consola.
3. Realizar una prueba rápida de los JSON con Python o PowerShell (`ConvertFrom-Json`) para garantizar sintaxis JSON válida en todos los archivos modificados de `ComputerLearner/Assets/Resources/`.
4. Actualizar **`Documentation/Specs/Diego.md`** añadiendo una nueva sección numerada consecutiva con el registro de todos los cambios realizados.
