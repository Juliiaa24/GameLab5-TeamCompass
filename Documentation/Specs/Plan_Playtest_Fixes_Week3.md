# Plan de Pulido y Correcciones (Playtest Semana 3) — Computer Learning (Unity 6)

> **Documento de especificación técnica para ejecución secuencial con `/goal`.**
> Este plan aborda exclusivamente los puntos accionables del feedback de esta semana: ralentizar la lectura automática de diálogos (~3x tiempo de lectura manteniendo el skip), mostrar la puntuación máxima posible (`X/Y points`) corrigiendo el cálculo de `maxPossibleScore`, arreglar el solapamiento/recorte de texto de medallas en la carpeta `Levels`, endurecer la detección de Drag & Drop, reemplazar los iconos de Double-Click / Right-Click / Click-&-Hold, y aplicar micro-pulido seguro de feedback sin añadir minijuegos ni rehacer la UI general.

---

## Reglas Generales de Arquitectura, Alcance y Contexto

1. **Alcance estricto (NO sobre-ingeniar)**:
   - **NO añadir nuevos minijuegos.** Todo el esfuerzo debe centrarse en pulir lo existente.
   - **NO rediseñar radicalmente la interfaz ni el estilo general.** La transformación visual hacia una estética más infantil y menos corporativa queda reservada para los **Norwegian Game Awards** (ver Anexo al final).
   - **NO eliminar ni romper el skip de diálogo** (`VirtualMascot.AllowSpaceToSkipDialogue` mediante tecla `Space` o clic en el bocadillo).
2. **Preservar la arquitectura actual**: Respetar el namespace `ComputerLearning`, componentes modulares de Unity, prefabs y uGUI / Canvas (`RectTransform`).
3. **Input System**: El proyecto utiliza el **New Input System** (`activeInputHandler: 1`). Usar `UnityEngine.InputSystem.Keyboard.current` y `UnityEngine.InputSystem.Mouse.current` (protegiendo cualquier uso legacy bajo `#if ENABLE_LEGACY_INPUT_MANAGER`).
4. **Localización intacta**: Mantener sincronizados los 4 recursos de diálogo (`UIDialogs.json`, `EN-UIDialogs.json`, `ES-UIDialogs.json`, `NO-UIDialogs.json`) y la clase `UIDialogs.cs`.
5. **Validación continua**: Tras completar cada sección, validar los scripts modificados con `Unity_ValidateScript` y comprobar en `Unity_ReadConsole` que no hay errores de compilación ni excepciones.
6. **Documentación**: Al concluir todas las secciones, actualizar `Documentation/Specs/Diego.md` con el resumen de cambios realizados.

---

## Sección 1: [REVERTIDA POR FEEDBACK DE PLAYTEST] Velocidad de diálogos (3x tiempo de lectura) manteniendo el skip

> **Nota:** Se implementó el escalado 3x en `VirtualMascot.cs` y posteriormente se revirtió íntegro a petición del usuario al resultar demasiado lento al probarlo.

### Diagnóstico de la causa raíz
1. En `ComputerLearner/Assets/Scripts/DesktopSystem/VirtualMascot.cs` (líneas 415–437), `WaitForDialogue(float duration)` espera exactamente el número fijo de segundos pasado por parámetro (`2.0f`–`5.0f` s desde `DesktopTour.cs`, `AdvancedDesktopTour.cs`, `FirstDesktopTutorial.cs`, `VirtualDesktopIntro.cs`, `VirtualDesktopGuidedLevels.cs`, `AdaptiveSessionStarter.cs` y `LevelRunner.AutoStartLevelAfterDelay`), sin tener en cuenta la longitud del texto ni dar margen para leerlo varias veces.
2. En `VirtualMascot.ShowContextualFeedback(...)` (línea 561), las duraciones de feedback contextual están capadas a `Mathf.Min(duration, 2.0f)` (o `1.8f` por defecto), lo cual desaparece demasiado rápido para lectores jóvenes.
3. En `VirtualMascot.Update()` (líneas 330–349), `AllowSpaceToSkipDialogue` permite saltar con `Space` o clic en el bocadillo, pero no tiene un pequeño cooldown tras mostrar un nuevo texto, lo que puede provocar saltos dobles accidentales.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/DesktopSystem/VirtualMascot.cs`
- Revisión de llamadas en `ComputerLearner/Assets/Scripts/Managers/LevelRunner.cs` (si `AutoStartLevelAfterDelay` necesita ajustarse o delegar completamente en `VirtualMascot.WaitForDialogue`).

### Pasos de implementación
1. En **`VirtualMascot.cs`**:
   - Almacenar `lastShownMessage` (y `lastMessageShownUnscaledTime`) cada vez que `ShowMessage(...)` / `ApplyTextAndRebuildBubble(...)` actualiza el texto del globo.
   - Crear un helper `CalculateReadingDuration(string message, float minSeconds, float maxSeconds, float readMultiplier = 3f)`:
     - Contar palabras (y/o caracteres, p. ej. velocidad infantil conservadora de ~2.0–2.2 palabras/segundo más `1.0f` s base de orientación visual) y multiplicar por `readMultiplier` (`3.0f`) para que el jugador pueda leer el texto **tres veces** antes de avanzar automáticamente.
     - En `WaitForDialogue(float duration)`: usar `float effectiveDuration = Mathf.Max(duration * 3f, CalculateReadingDuration(lastShownMessage, 6.0f, 24.0f, 3.0f));` (asegurando que incluso frases cortas permanezcan al menos ~6s y frases largas hasta ~24s, salvo que el jugador pulse `Space` o haga clic en el globo para saltar).
   - En `ShowContextualFeedback(...)`: eliminar el cap restrictivo `Mathf.Min(duration, 2.0f)`. Calcular la duración usando un mínimo cómodo basado en la longitud del mensaje (p. ej. `Mathf.Max(duration > 0f ? duration : 4.5f, CalculateReadingDuration(message, 4.5f, 12.0f, 2.5f))` en tareas, y mantener `>= 6.0f` s en celebración), recordando que el feedback contextual nunca bloquea los clics (`blocksRaycasts = false`) y se reemplaza limpiamente si llega un nuevo evento.
   - En `Update()` dentro del bloque `AllowSpaceToSkipDialogue`: añadir un pequeño cooldown de `0.25f` s desde `lastMessageShownUnscaledTime` para evitar saltar dos diálogos consecutivos con una sola pulsación rápida.

### Criterios de aceptación
- Todos los diálogos secuenciales (`DesktopTour`, `AdvancedDesktopTour`, `FirstDesktopTutorial`, `VirtualDesktopIntro`, `VirtualDesktopGuidedLevels`, `LevelRunner`) permanecen visibles el tiempo suficiente para leerse 3 veces antes de auto-avanzar.
- Saltar el diálogo (clic en el globo o `Space`) sigue funcionando de forma inmediata (pasado el micro-cooldown de `0.25s`).
- Los mensajes de error/consejo contextuales (`ShowContextualFeedback`) son legibles sin desaparecer a los 1.8 segundos.

---

## Sección 2: Mostrar la puntuación máxima posible (`300/400 points`) y corregir `maxPossibleScore`

### Diagnóstico de la causa raíz
1. **Visualización incompleta:** En `ComputerLearner/Assets/Scripts/UI/LevelCelebrationModal.cs` (líneas 240–249), la fila `PointsText` usa `ui.celebration_points_format` (`"Score: <b>{0} PTS</b>"` en `UIDialogs.cs` y en los 4 JSON de `Resources/`), pasando únicamente `pointsVal` sin el máximo posible.
2. **Bug oculto en `LevelRunner.AwardHitPoints`:** En `ComputerLearner/Assets/Scripts/Managers/LevelRunner.cs` (líneas 57–63), `AwardHitPoints(int exactPoints = 100)` suma `pts` tanto a `currentLevelScore` como a `maxPossibleScore`. Cuando una tarea de precisión (`ClickTargetTask`, `DoubleClickTask`, `RightClickTask`, `HoverTarget`) llama a `Complete` / `RecordIntermediateResult` con `45` o `75` puntos (por dar en el anillo exterior o medio según `BaseTask.CalculateRatioAndSizeScore`), `maxPossibleScore` suma `45` en vez del máximo real de ese objetivo (`100 + sizeBonus`). Esto rompe el denominador (`180/180` en lugar de `180/400`) y distorsiona el ratio de medalla en `ProgressData.CalculateMedalFromPoints`.
3. **Niveles guiados (`VirtualDesktopGuidedLevels.cs`):** En las líneas 1660–1703, se calcula `finalScore = Mathf.Max(100, baseScore + speedBonus - mistakePenalty)` (donde `baseScore = 600` y el máximo teórico sin errores y máximo bonus es `baseScore + Mathf.RoundToInt(parTime * 15f)`), pero `LevelCelebrationModal.Show(...)` no recibe ningún `maxPoints`.
4. **Historial en `ScoresApp.cs` y `LevelScoreRecord.cs`:** Conviene guardar también `bestMaxPoints` en `LevelScoreRecord` para que la app de puntuaciones/medallas pueda mostrar `bestPts/maxPts` cuando esté disponible.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/Managers/LevelRunner.cs`
- `ComputerLearner/Assets/Scripts/Tasks/BaseTask.cs`
- `ComputerLearner/Assets/Scripts/Tasks/ClickTargetTask.cs`, `DoubleClickTask.cs`, `RightClickTask.cs`, `HoverTarget.cs`, `HoverTargetTask.cs` (verificar cómo pasan puntos y máximo)
- `ComputerLearner/Assets/Scripts/DesktopSystem/VirtualDesktopGuidedLevels.cs`
- `ComputerLearner/Assets/Scripts/UI/LevelCelebrationModal.cs`
- `ComputerLearner/Assets/Scripts/Data/UIDialogs.cs`
- `ComputerLearner/Assets/Resources/UIDialogs.json`, `EN-UIDialogs.json`, `ES-UIDialogs.json`, `NO-UIDialogs.json`
- `ComputerLearner/Assets/Scripts/Data/LevelScoreRecord.cs`, `ComputerLearner/Assets/Scripts/Managers/ProgressData.cs`, `ComputerLearner/Assets/Scripts/DesktopSystem/ScoresApp.cs` (opcional/recomendado si se muestra en historial)

### Pasos de implementación
1. En **`BaseTask.cs`**:
   - Añadir un helper estático `CalculateMaxPossiblePrecisionScore(RectTransform targetRect)` que devuelva `CalculateRatioAndSizeScore(targetRect, 0f, out _)` (es decir, `100 + sizeBonus`, la puntuación máxima posible de ese target).
   - Extender `TriggerHitScoreFeedback(Transform targetTransform = null, int overrideFixedPoints = -1, int overrideMaxPoints = -1)`, `RecordIntermediateResult(..., int overrideMaxPoints = -1)` y `Complete(..., int overrideMaxPoints = -1)` de forma retrocompatible (parámetro opcional `-1`).
   - Cuando `overrideFixedPoints > 0` pero `overrideMaxPoints <= 0`, si la tarea usa `CalculatePrecisionAndSizeScore`, pasar explícitamente el máximo (`CalculateMaxPossiblePrecisionScore`) desde `ClickTargetTask`, `DoubleClickTask`, `RightClickTask` y `HoverTarget`/`HoverTargetTask`. Para tareas con puntaje fijo (`GetStandardizedTargetPoints()`), el máximo por hit es `GetStandardizedTargetPoints()`.
   - Además, si una tarea termina con `Complete(false)` (fallo o timeout), asegurarse de que `maxPossibleScore` en `LevelRunner` no quede infravalorado (sumando el máximo que valía esa tarea/subtareas pendientes si procede).
2. En **`LevelRunner.cs`**:
   - Actualizar `AwardHitPoints(int earnedPoints = 100, int maxTargetPoints = -1)`:
     - `int earned = Mathf.Max(1, earnedPoints);`
     - `int maxPts = maxTargetPoints > 0 ? Mathf.Max(earned, maxTargetPoints) : earned;`
     - `currentLevelScore += earned;`
     - `maxPossibleScore += maxPts;`
   - Pasar `maxPossibleScore` a `LevelCelebrationModal.Show(...)`.
3. En **`VirtualDesktopGuidedLevels.cs`**:
   - Calcular `int maxPossibleScore = baseScore + Mathf.RoundToInt(Mathf.Max(0f, parTime) * 15f);` y pasarlo como `maxPoints` a `LevelCelebrationModal.Show(...)` (y a `RecordLevelAttempt` si se añade el campo).
4. En **`LevelCelebrationModal.cs`**, **`UIDialogs.cs`** y **`Resources/*UIDialogs.json`**:
   - Añadir parámetro opcional `int maxPoints = -1` en `LevelCelebrationModal.Show(...)` e `Initialize(...)`. Si `maxPoints <= 0` y `LevelRunner.ActiveRunner != null && LevelRunner.ActiveRunner.MaxPossibleScore > 0`, tomar `LevelRunner.ActiveRunner.MaxPossibleScore`.
   - Actualizar `UIDialogs`:
     - `EN-UIDialogs.json` y `UIDialogs.json`: `"celebration_points_format": "You got <b>{0}/{1}</b> points"` (o `"Score: <b>{0}/{1}</b> points"` — usar exactamente el estilo pedido: `"You got <b>{0}/{1}</b> points"`).
     - `ES-UIDialogs.json`: `"celebration_points_format": "Conseguiste <b>{0}/{1}</b> puntos"`
     - `NO-UIDialogs.json`: `"celebration_points_format": "Du fikk <b>{0}/{1}</b> poeng"`
   - En `LevelCelebrationModal.BuildVisuals()`, si `maxPointsVal > 0`, formatear `string.Format(ptsFormat, pointsVal, maxPointsVal)` (protegiendo con `try/catch` o comprobando `{1}` por si alguna traducción antigua tuviera solo `{0}`, y con fallback `"Score: <b>{0} PTS</b>"` si `maxPointsVal <= 0`).
5. *(Recomendado)* En **`LevelScoreRecord.cs`**, **`ProgressData.cs`** y **`ScoresApp.cs`**:
   - Guardar `bestMaxPoints` junto a `bestPoints` y mostrar `{bestPts}/{maxPts} PTS` en `ScoresApp.cs` cuando `bestMaxPoints > 0`.

### Criterios de aceptación
- Al finalizar cualquier nivel estándar (1–9) o guiado (10–16), el modal de celebración muestra tanto los puntos obtenidos como el máximo posible (ej. `"You got 300/400 points"`).
- Si el jugador golpea los bordes de los objetivos (45 o 75 puntos en vez de 100+), `maxPossibleScore` sigue reflejando el máximo real alcanzable (ej. `245/400`), no `245/245`.

---

## Sección 3: Reparar el texto de medallas en la carpeta `Levels` del escritorio

### Diagnóstico de la causa raíz
1. En `ComputerLearner/Assets/Scripts/DesktopSystem/FileExplorerApp.cs` (`CreateItemUI`, líneas 2163–2181), el nombre del nivel y su etiqueta de medalla se asignan dentro del mismo componente `TextMeshProUGUI Label`:
   `label.text = $"{item.name}\n<size=11><color={hex}><b>[{medalName}]</b></color></size>";`
2. En `ComputerLearner/Assets/Prefabs/Windows/ExplorerItemPrefab.prefab`:
   - El root mide `110 x 110`.
   - `Icon` mide `64 x 64` en la parte superior (`anchoredPosition: (0, -8)`), dejando solo `~38px` en la parte inferior.
   - `Label` tiene altura fija `m_SizeDelta: {x: -6, y: 32}`, `m_fontSize: 13.5` y `m_overflowMode: 1` (`Ellipsis`).
3. En `LevelsFolderContent.prefab` (línea 249) y `FileExplorerContent.prefab` (línea 1570), el `GridLayoutGroup` usa `m_CellSize: {x: 105, y: 110}`.
4. Cuando `item.name` ocupa 2 líneas (ej. `"Level 5: Double Click"` o `"Level 7: Click & Hold"`), consume los ~32px de `Label` y la 3.ª línea (`[Gold]` / `[Silver]` / `[Bronze]`) queda cortada, reemplazada por `...` o desplazada fuera de la celda.

### Archivos a modificar
- `ComputerLearner/Assets/Prefabs/Windows/ExplorerItemPrefab.prefab`
- `ComputerLearner/Assets/Prefabs/Windows/LevelsFolderContent.prefab`
- `ComputerLearner/Assets/Prefabs/Windows/FileExplorerContent.prefab`
- `ComputerLearner/Assets/Prefabs/Windows/MinigamesFolderContent.prefab` (si comparte el mismo `GridLayoutGroup`)
- `ComputerLearner/Assets/Scripts/DesktopSystem/FileExplorerApp.cs`

### Pasos de implementación
1. En **`ExplorerItemPrefab.prefab`** (y respaldado por código en `FileExplorerApp.CreateItemUI` por seguridad):
   - Aumentar el tamaño vertical del ítem (ej. `116 x 136`) y actualizar `m_CellSize` en el `GridLayoutGroup` de `LevelsFolderContent.prefab`, `FileExplorerContent.prefab` y `MinigamesFolderContent.prefab` a `{x: 116, y: 136}` (o `{x: 112, y: 134}`) para dar espacio vertical cómodo sin solapamientos entre filas.
   - Reestructurar la zona inferior de `ExplorerItemPrefab` con **dos textos separados** (o un `VerticalLayoutGroup` limpio):
     - `Icon` (`58x58` o `60x60` arriba).
     - `Label` (nombre del ítem): `fontSize: 12.5`–`13`, altura fija reservada para **2 líneas** (`~34px`), alineado `Top` / `Center`, con truncamiento limpio solo si excediera 2 líneas (`maxVisibleLines = 2` o altura exacta + `Ellipsis`), sin contener el texto de la medalla.
     - `MedalLabel` (nuevo hijo `TextMeshProUGUI` debajo de `Label`, altura `~18px`, `fontSize: 11`, negrita, centrado, `raycastTarget = false`): dedicado exclusivamente a `[{medalName}]` o `[Locked]`.
2. En **`FileExplorerApp.CreateItemUI`**:
   - Buscar `MedalLabel` (y si un prefab antiguo no lo tuviera, crearlo dinámicamente debajo de `Label` asegurando la disposición correcta).
   - Asignar `label.text = item.name;` siempre limpio (sin `\n[{medalName}]`).
   - Si `!isUnlocked`, mostrar `[Locked]` en `MedalLabel` (en gris/rojo suave). Si `levelMedal != LevelMedal.None`, mostrar `<b>[{medalName}]</b>` con su color en `MedalLabel` y activar el objeto; si no tiene medalla ni está bloqueado, ocultar o vaciar `MedalLabel`.

### Criterios de aceptación
- En la carpeta `Levels` del escritorio, tanto los niveles con nombre de 1 línea como los de 2 líneas muestran el nombre completo y el texto de medalla (`[Gold]`, `[Silver]`, `[Bronze]`) perfectamente alineados debajo, sin taparse, cortarse ni empujarse fuera de la celda.

---

## Sección 4: Hacer más exigentes ("punishing") los niveles de Drag & Drop

### Diagnóstico de la causa raíz
1. En `ComputerLearner/Assets/Scripts/Tasks/DragDropTask.cs` (líneas 32–40):
   - `dropSize = targetSize + 50f` hace la zona de destino excesivamente grande respecto a la pieza (`dragSize = targetSize`).
   - `requiredDistance = Mathf.Max(20f, definition.config.targetSize * 0.5f)` permite aceptar la pieza cuando su centro está a medio diámetro de distancia (apenas rozando el borde de la zona de destino).
2. En `ComputerLearner/Assets/Scripts/Tasks/DraggablePiece.cs` (líneas 109–110):
   - `float distance = Vector2.Distance(dragRect.position, dropTarget.position);` compara una distancia en **espacio de mundo/pantalla** (`Transform.position`, escalado por el `CanvasScaler`) contra `requiredDistanceToSnap`, que estaba expresado en **unidades locales del RectTransform**.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/Tasks/DragDropTask.cs`
- `ComputerLearner/Assets/Scripts/Tasks/DraggablePiece.cs`

### Pasos de implementación
1. En **`DraggablePiece.cs`**:
   - Calcular la distancia en **espacio local consistente** (sin verse afectada por la resolución o `Canvas.scaleFactor`), por ejemplo transformando el centro de `dragRect` al espacio local de `dropTarget` (con `dropTarget.InverseTransformPoint(dragRect.TransformPoint(dragRect.rect.center))`) o al espacio local de su padre común.
   - Guardar `requiredDistanceToSnap` en unidades locales estrictas y validar `localDist <= requiredDistanceToSnap`.
   - Añadir feedback visual en tiempo real dentro de `OnDrag(...)`: comprobar si la pieza está dentro del umbral estricto (`IsWithinValidDropZone()`). Cuando el jugador está en el centro válido, resaltar la zona de destino (p. ej. cambio sutil de color/escala del `dropTarget`); si solo roza el borde, mantener un estado visual neutro o de advertencia para comunicar al jugador que aún debe centrarla más antes de soltar.
2. En **`DragDropTask.cs`**:
   - Reducir el exceso de tamaño del `dropTarget`: usar `float dropSize = dragSize + 16f;` (en vez de `+ 50f`) o mantener un margen visual moderado (`+ 20f`) pero con una distancia de aceptación **estricta**:
     `float requiredDistance = Mathf.Max(14f, dragSize * 0.25f);` (aproximadamente el 25% del tamaño de la pieza, en lugar del 50%).
   - De este modo, si el usuario suelta la pieza tocando apenas el borde de la zona de destino, `OnEndDrag` la rechaza, la devuelve a su posición inicial, dispara `OnPieceMissed` (`HandlePieceMissed` registra intento fallido, resta puntos de penalización y muestra el mensaje de la mascota pidiendo llevarla bien al centro).

### Criterios de aceptación
- Arrastrar una pieza y soltarla apenas tocando el borde de la zona de destino **falla**, devuelve la pieza al origen y aplica el feedback/penalización correspondiente.
- Para completar la tarea es necesario llevar la pieza claramente hacia el interior/centro de la zona de destino.

---

## Sección 5: [OMITIDA POR INDICACIÓN DEL USUARIO] Reemplazar los iconos de Double-Click, Right-Click y Click-&-Hold

> **Nota:** El usuario indicó explícitamente que el reemplazo de iconos no debía aplicarse en esta iteración. Se mantienen los sprites actuales sin cambios.

### Diagnóstico de la causa raíz
1. En `ComputerLearner/Assets/Prefabs/Tasks/Task_DoubleClick.prefab`, `Task_RightClick.prefab` y `Task_ClickHold.prefab`, el sprite principal (`m_Sprite`) sigue apuntando al genérico `Target_Icon_01.png` (`guid: fdbcf9652edac484992eb3c9911b9ebd`).
2. En `ComputerLearner/Assets/Scripts/DesktopSystem/FileExplorerApp.cs` (`PopulateLevelsFolder`, líneas 1830–1848) y `LevelsFolderContent.prefab`, todos los niveles de ratón reciben el mismo `targetGameIcon` (`Target_Minigame_Icon01_Scaled.png`).
3. En `ComputerLearner/Assets/Art/UI/Mouse Icons/` existen sprites (`PC_Mouse_RClick_Icon_01.png`, `PC_Mouse_HoldClick_Icon_01.png`, `PC_Mouse_LClick_Icon_01.png`, etc.) que actualmente no están referenciados en ningún prefab ni escena, y no existe un icono dedicado claro para **Double-Click**.

### Archivos a modificar / crear
- `ComputerLearner/Assets/Art/UI/Mouse Icons/` (crear/reemplazar sprites PNG limpios y de alto contraste para `DoubleClick`, `RightClick` y `ClickHold`)
- `ComputerLearner/Assets/Prefabs/Tasks/Task_DoubleClick.prefab`
- `ComputerLearner/Assets/Prefabs/Tasks/Task_RightClick.prefab`
- `ComputerLearner/Assets/Prefabs/Tasks/Task_ClickHold.prefab`
- `ComputerLearner/Assets/Scripts/DesktopSystem/FileExplorerApp.cs` (y `LevelDefinition` / `VirtualDesktopSetup.cs` si procede para iconos diferenciados en la carpeta `Levels` y `MinigamesHub`)

### Pasos de implementación
1. **Crear/actualizar los 3 sprites PNG en `ComputerLearner/Assets/Art/UI/Mouse Icons/`** (por ejemplo mediante un script de Python con Pillow o script temporal de Unity Editor que dibuje gráficos vectoriales/nítidos 256x256 con canal alfa transparente, importados como `Sprite (2D and UI)`):
   - **`Icon_DoubleClick.png`** (nuevo): Silueta clara y amigable de ratón con el botón izquierdo resaltado en azul/dorado y doble onda/destello + insignia clara `"x2"`.
   - **`Icon_RightClick.png`** (reemplaza el uso anterior / mejora `PC_Mouse_RClick_Icon_01.png`): Silueta clara de ratón con el botón **derecho** fuertemente resaltado (naranja/coral vivo) y onda de clic en el lado derecho.
   - **`Icon_ClickHold.png`** (reemplaza el uso anterior / mejora `PC_Mouse_HoldClick_Icon_01.png`): Silueta clara de ratón con el botón izquierdo pulsado en verde/morado + indicador visual de mantener presionado (flecha hacia abajo / anillo de carga).
2. **Asignar los nuevos iconos en los prefabs de tareas**:
   - Actualizar `Task_DoubleClick.prefab`, `Task_RightClick.prefab` y `Task_ClickHold.prefab` para que usen sus respectivos sprites nuevos (respetando la legibilidad del `FillImage` radial en `Task_ClickHold.prefab` y manteniendo `CircleHitbox` funcional).
3. **Asignar los nuevos iconos en la carpeta `Levels` (`FileExplorerApp.cs`)**:
   - En `FileExplorerApp.cs`, añadir campos serializados (con carga fallback desde `Resources` o `Art` / `AssetDatabase` en Editor) para asociar los iconos específicos a los niveles correspondientes (`level5` -> Double Click, `level6` -> Right Click, `level7` -> Click & Hold), de modo que al abrir la carpeta `Levels` en el escritorio cada uno muestre su nuevo icono distintivo en vez del genérico.
   - Actualizar también `MinigamesHubApp` / `MinigamesHubContent.prefab` si muestra tarjetas de `Double Click`, `Right Click` o `Click & Hold` (respetando los prefabs existentes).

### Criterios de aceptación
- Double-click, right-click y click-&-hold tienen iconos nuevos, claros y distintos entre sí tanto en sus niveles/carpeta `Levels` como en los prefabs de tarea correspondientes.
- No hay sprites rotos (`Missing Sprite`) ni regresiones en el relleno radial de `ClickHoldTask`.

---

## Sección 6: Micro-pulido rápido de feedback (sin cambiar la UI ni añadir minijuegos)

### Pasos de implementación
1. **Respuesta visual sutil y amigable al acertar/fallar** (sin rediseñar pantallas ni añadir sistemas pesados):
   - En `DragDropTask.cs` / `DraggablePiece.cs`, dar un pulso breve de escala al `dropTarget` cuando encaja correctamente una pieza y un pequeño shake horizontal si falla al soltarla en el borde.
   - En `ClickHoldTask.cs`, asegurarse de que el `fillImage` cambia gradualmente de color hacia verde brillante conforme se acerca al 100% para reforzar la sensación de progreso al mantener pulsado.
   - Revisar que `FeedbackManager.PlaySuccess()` / `PlayError(true)` y `FloatingScoreText` se disparen de forma consistente en los flujos tocados.

---

## Sección 7: Verificación integral, compilación limpia y actualización de `Diego.md`

### Pasos de implementación
1. Validar todos los scripts C# modificados con `Unity_ValidateScript`.
2. Comprobar `Unity_ReadConsole` verificando **0 errores de compilación** y **0 excepciones** en runtime/editor.
3. Verificar en escena/prefabs que todos los GUIDs de los nuevos sprites y campos de `ExplorerItemPrefab` / `LevelsFolderContent` están correctamente enlazados.
4. Actualizar `Documentation/Specs/Diego.md` documentando los cambios de esta iteración y añadir las recomendaciones para los **Norwegian Game Awards** en una sección separada (sin implementarlas ahora).

---

## Anexo (SOLO REFERENCIA FUTURA — NO IMPLEMENTAR AHORA): Ideas para los Norwegian Game Awards

*El feedback de esta semana señaló que la interfaz se siente como una herramienta corporativa y que podríamos pensar formas más divertidas de dar feedback (y añadir nuevos minijuegos) para los Norwegian Game Awards:*
1. **Rediseño Kid-Friendly del escritorio y ventanas:** Sustituir los grises/azules planos tipo oficina por una paleta cálida y juguetona, esquinas redondeadas, botones táctiles con sombra/volumen 3D sutil, cursores personalizables y fondos temáticos desbloqueables.
2. **Feedback divertido y expresivo:** Reacciones animadas de la mascota según rachas (gafas de sol, saltos, confeti extra en Gold), barra de combo/racha durante los niveles, mini-sonidos musicales ascendentes por cada acierto consecutivo y sellos animados tipo "pegatina" al completar un nivel.
3. **Nuevos minijuegos para NGA:** Minijuegos temáticos en la carpeta de juegos (p. ej. atrapar virus/bichos con doble clic, ordenar archivos en carpetas de colores con drag & drop bajo tiempo, pintar/calcar figuras con click & hold) usando exactamente las mecánicas ya validadas.
