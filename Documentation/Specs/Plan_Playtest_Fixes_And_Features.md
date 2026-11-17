# Plan de Correcciones y Mejoras de Playtesting (Post-Pull `main`) — Computer Learning (Unity 6)

> **Documento de especificación técnica exhaustiva para ejecución secuencial con `/goal`.**
> Este documento ha sido auditado y adaptado a los últimos cambios integrados en `main` (`SessionManager`, `TeacherModeGate`, `StudentConfiguration`, `RetroUITheme`, `TaskbarClock` y la UI serializada de `Menu.unity` / `AssessmentScene.unity`). Abarca las 9 fases de corrección y pulido derivadas del playtesting.

---

## Reglas Generales de Arquitectura y Contexto

1. **Rama de Trabajo:** Ejecutar sobre la rama `main` actual con todos los últimos commits integrados.
2. **Arquitectura y Estilo:** Respetar el namespace `ComputerLearning`, la integración con `SessionManager`, `RetroUITheme` y `TaskbarClock`, y las referencias serializadas en escena.
3. **Persistencia y Ciclo de Vida:** Asegurar que ninguna variable estática en memoria RAM (`static`) conserve datos entre diferentes estudiantes tras invocar `SceneSystem.ResetProgress()` o `SessionManager.ApplyConfiguration(..., resetExistingProgress: true)`.
4. **Input System:** Utilizar la API del **New Input System** (`UnityEngine.InputSystem.Keyboard.current` y `Mouse.current`).
5. **Localización Trilingüe:** Todos los textos nuevos o modificados deben declararse en `UIDialogs.cs` y en `UIDialogs.json`, `EN-UIDialogs.json`, `ES-UIDialogs.json` y `NO-UIDialogs.json` (sin romper las nuevas claves de Teacher Mode, Learning Paths y LAN añadidas en `main`).
6. **Validación Continua:** Comprobar que `Assembly-CSharp.csproj` compile con 0 errores, que todos los archivos JSON sean válidos y registrar la nueva sección en `Documentation/Specs/Diego.md`.

---

## Fase 1: Corrección Integral de Persistencia en `ResetProgress`, `SessionManager` y `DesktopManager`

### Diagnóstico sobre el nuevo `main`
1. Aunque `SceneSystem.ResetProgress()` ahora llama a `DesktopManager.ResetWallpaperPreferences()`, **nunca limpia `FileExplorerApp.sharedRootFolder` en RAM ni borra `VirtualFileSystemSnapshot_v1`**. Por ello, al reiniciar el progreso, todos los archivos y carpetas de la sesión anterior siguen vivos en memoria.
2. El nuevo `SessionManager.ApplyConfiguration(StudentConfiguration config, bool resetExistingProgress = true)` limpia `PlaytestDataManager`, `SkillManager` y `ProgressData` cuando empieza un nuevo alumno o cambia de ruta, pero **no llama a `DesktopManager.ResetWallpaperPreferences()` ni a `FileExplorerApp.ResetStaticFileSystem()`**.
3. En `DesktopManager.Start()` (líneas 166-167), siguen presentes las asignaciones forzadas:
   ```csharp
   PlayerPrefs.SetInt("InitialAssessmentCompleted", 1);
   PlayerPrefs.SetInt("AutoSequenceCompleted", 1);
   ```
   Si un alumno entra primero a `VirtualDesktop`, estas líneas sobrescriben el estado de `SessionManager`, provocando que al volver al Menú y pulsar `Play`, `SessionManager.ResolveResumeScene()` se salte el Assessment Test y `Desktop1`.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/DesktopSystem/FileExplorerApp.cs`
- `ComputerLearner/Assets/Scripts/DesktopSystem/DesktopManager.cs`
- `ComputerLearner/Assets/Scripts/Managers/SceneSystem.cs`
- `ComputerLearner/Assets/Scripts/Managers/SessionManager.cs`

### Especificación Técnica
1. **En `FileExplorerApp.cs`:**
   - Añadir método público estático:
     ```csharp
     public static void ResetStaticFileSystem()
     {
         sharedRootFolder = null;
         activeExplorers.Clear();
         clipboardItem = null;
         clipboardSourceItem = null;
         IsClipboardCut = false;
         ActiveExplorerFolder = null;
         IsDesktopFocused = true;
         PlayerPrefs.DeleteKey(SNAPSHOT_PREFS_KEY);
         PlayerPrefs.Save();
     }
     ```
2. **En `DesktopManager.cs`:**
   - En `EnsureWallpaperImage()`: Capturar también `defaultWallpaperColor` la primera vez (`hasCapturedDefaultWallpaper`) para restaurar el color original junto con `defaultWallpaperSprite`.
   - En `ResetWallpaperPreferences()`: Si `HasInstance && _instance != null`, invocar `_instance.RestoreSavedWallpaper()`.
   - En `DesktopManager.Start()`: **Eliminar** las dos líneas `PlayerPrefs.SetInt("InitialAssessmentCompleted", 1);` y `PlayerPrefs.SetInt("AutoSequenceCompleted", 1);` para que `SessionManager` sea la única fuente de verdad del progreso de sesión.
3. **En `SceneSystem.ResetProgress()` y `SessionManager.ApplyConfiguration(..., resetExistingProgress: true)`:**
   - Invocar `FileExplorerApp.ResetStaticFileSystem();` y `DesktopManager.ResetWallpaperPreferences();` tanto en `SceneSystem.ResetProgress()` como dentro del bloque `if (resetExistingProgress)` de `SessionManager.ApplyConfiguration()`.

---

## Fase 2: Robustez del Sistema de Archivos Virtual (Prevención de Crashes y Bucles)

### Diagnóstico
En `FileExplorerApp.cs`, `CopySelectedItem()` clona el elemento en una nueva instancia `clipboardItem` sin guardar la referencia al nodo original (`clipboardSourceItem`), por lo que `IsFolderOrDescendant(clipboardItem, targetFolder)` devuelve `false` al pegar una carpeta dentro de sí misma o de sus subcarpetas, permitiendo duplicación exponencial y bloqueos/crashes al serializar el snapshot en `PlayerPrefs`.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/DesktopSystem/FileExplorerApp.cs`
- `ComputerLearner/Assets/Scripts/DesktopSystem/DesktopContextMenuTrigger.cs`

### Especificación Técnica
1. **Detección Estricta de Ciclos y Ancestros:**
   - Añadir `private static ExplorerItemData clipboardSourceItem;` y actualizarlo en `SharedClipboardItem`, `CopySelectedItem()` y `CutItemToClipboard()`.
   - Implementar `IsFolderDescendantOf(ExplorerItemData potentialDescendant, ExplorerItemData ancestor, int depth = 0)`, `GetFolderDepth(ExplorerItemData targetFolder)` y `GetSubtreeMaxDepth(ExplorerItemData node, int currentDepth = 0)`.
   - En `PasteClipboardItem()`, `PasteClipboardIntoFolder()` y `MoveItemToFolder()`: Impedir la operación si la carpeta destino es descendiente de `clipboardItem` o de `clipboardSourceItem`, mostrando el mensaje localizado `UIDialogs.Current.explorer_cycle_error`.
   - En `FindFolderChainRecursive`: Comparar por referencia (`current == target`) en lugar de por nombre para evitar falsos positivos entre carpetas anidadas con el mismo nombre, y priorizar hijos directos en `FindFolderRecursiveStatic`.
2. **Límite de Profundidad y Capacidad Máxima:**
   - Definir `public const int MAX_FOLDER_DEPTH = 8;` y `public const int MAX_ITEMS_PER_FOLDER = 35;`.
   - Comprobar dichos límites en `CreateNewFolder`, `CreateNewTextDocument`, `CreateNewImageDocument`, `PasteClipboardItem`, `PasteClipboardIntoFolder`, `MoveItemToFolder` y en los métodos de creación de `DesktopContextMenuTrigger.cs`.
   - Añadir corte de profundidad máxima en todos los métodos recursivos (`CollectSnapshotEntriesRecursive`, `CloneChildrenRecursive`, `RemoveItemRecursive`, `FindFolderRecursiveStatic`, `FindPathRecursive`, `FindFolderChainRecursive`, `RefreshIconsRecursive`).

---

## Fase 3: Capas de Renderizado en Arrastre (Fix Nota vs Papelera de Reciclaje)

### Diagnóstico
Al arrastrar la nota de práctica (`Practice Note`) o cualquier icono del escritorio sobre la ventana abierta de la Papelera de Reciclaje, el icono queda por detrás de la ventana porque el contenedor de iconos se renderiza por debajo del contenedor de ventanas.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/DesktopSystem/DraggableIcon.cs`
- `ComputerLearner/Assets/Scripts/DesktopSystem/ExplorerItemPointerHandler.cs`

### Especificación Técnica
1. En `DraggableIcon.cs`:
   - Implementar `ElevateForDrag()` que añada/active un componente `Canvas` con `overrideSorting = true` y `sortingOrder = 32000` (sin `GraphicRaycaster` para que los raycasts de `IDropHandler` y `RaycastAll` atraviesen limpiamente el icono arrastrado).
   - Llamar a `ElevateForDrag()` en `OnBeginDrag` y a `RestoreAfterDrag()` en `OnEndDrag`, `OnPointerUp` y `ReturnToInitialPosition`.
   - En `DeleteDesktopIconToRecycleBin()`, llamar también a `RecycleBinIcon.NotifyIconRecycled(this)` para que soltar la nota dentro de la ventana abierta de la Papelera complete el paso correspondiente del tutorial.
2. En `ExplorerItemPointerHandler.cs`:
   - Añadir `Canvas` con `overrideSorting = true` y `sortingOrder = 32000` al `dragGhost`.

---

## Fase 4: Periodo de Gracia en el Assessment Test y Clarificación en Nivel 7

### Diagnóstico
1. En `InitialTestManager.cs` (que ahora soporta tanto referencias serializadas `serializedTimerBarFill` como UI generada), `currentTaskTimer` comienza a contar desde el frame 0 al aparecer cada prueba, penalizando a los niños mientras leen el enunciado.
2. En el Nivel 7 (`Click & Hold`), tras haber aprendido el clic derecho en el Nivel 6, los textos no indican explícitamente que debe usarse el botón **IZQUIERDO**.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/Managers/InitialTestManager.cs`
- `ComputerLearner/Assets/Scripts/Data/UIDialogs.cs`
- `ComputerLearner/Assets/Scripts/Data/TutorialDialogs.cs`
- `ComputerLearner/Assets/Resources/UIDialogs.json`, `EN-UIDialogs.json`, `ES-UIDialogs.json`, `NO-UIDialogs.json`
- `ComputerLearner/Assets/Resources/TutorialDialogs.json`, `EN-TutorialDialogs.json`, `ES-TutorialDialogs.json`
- `ComputerLearner/Assets/Resources/AssessmentInstructions.json`, `NO-AssessmentInstructions.json`

### Especificación Técnica
1. **Periodo de Gracia en `InitialTestManager.cs`:**
   - Añadir `private float readingGracePeriod = 2.5f;`, `private float currentGraceTimer = 0f;` y `private bool isWaitingGracePeriod = false;`.
   - En `SpawnCurrentTask()`: Inicializar `currentGraceTimer = readingGracePeriod; isWaitingGracePeriod = true;` y mantener `timerBarFill.localScale = Vector3.one`.
   - En `Update()`: Mientras `isWaitingGracePeriod` sea `true`, descontar `currentGraceTimer` manteniendo `currentTaskTimer = 0f` y la barra al 100% antes de iniciar la cuenta atrás real.
2. **Clarificación del Botón Izquierdo en Nivel 7:**
   - Actualizar `level_instruction_7` en `UIDialogs.cs` y en los 4 JSONs (`UIDialogs.json`, `EN`, `ES`, `NO`), `intro_click_hold` en `TutorialDialogs` (`EN`, `ES`, base) y `click_hold` en `AssessmentInstructions.json` / `NO-AssessmentInstructions.json` para indicar explícitamente el **botón IZQUIERDO** del ratón.

---

## Fase 5: Cursor Visual de Redimensionamiento (`ResizeHandle`)

### Diagnóstico
`ResizeHandle.cs` no implementa eventos de hover, por lo que el cursor no cambia al situarse sobre los bordes o esquinas de una ventana.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/DesktopSystem/ResizeHandle.cs`

### Especificación Técnica
- Implementar `IPointerEnterHandler`, `IPointerExitHandler` e `IEndDragHandler`.
- Generar cursores procedurales de 32×32 (`TextureFormat.RGBA32`) con relleno blanco y contorno oscuro para las 4 direcciones (`↔` horizontal, `↕` vertical, `↖↘` diagonal NW-SE y `↗↙` diagonal NE-SW) según `direction`.
- No cambiar el cursor si la ventana está maximizada (`controller.IsMaximized`), y restaurar el cursor por defecto en `OnPointerExit` (si `!resizing`), `OnEndDrag`, `OnDisable` y `OnDestroy`.

---

## Fase 6: Hora y Fecha en el Reloj de la Barra de Tareas (`TaskbarClock`)

### Diagnóstico
En los últimos commits de `main` se añadió `TaskbarClock.cs` vinculado al objeto `ClockTray` / `ClockText` dentro de `Taskbar.prefab`, pero actualmente solo formatea `DateTime.Now.ToString("HH:mm")` sin mostrar la fecha.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/DesktopSystem/TaskbarClock.cs`
- `ComputerLearner/Assets/Scripts/DesktopSystem/Taskbar.cs`

### Especificación Técnica
1. **En `TaskbarClock.cs`:**
   - Configurar `timeText` en `Awake()` / `OnEnable()` para admitir dos líneas limpias dentro de `ClockTray` (`timeText.fontSize = 14.5f`, `timeText.lineSpacing = -10f`, `timeText.textWrappingMode = TextWrappingModes.NoWrap`, `timeText.alignment = TextAlignmentOptions.Center`) y ensanchar ligeramente el `RectTransform` de `ClockTray` a `116px` si mide menos de `114px`.
   - Actualizar `UpdateClock()` para mostrar tanto la hora como la fecha actual:
     ```csharp
     DateTime now = DateTime.Now;
     string nowFormatted = $"<b>{now:HH:mm}</b>\n<size=72%>{now:dd/MM/yyyy}</size>";
     ```
2. **En `Taskbar.cs`:**
   - Añadir método auxiliar público `public static TaskbarWindowButton FindButtonForWindow(Window targetWindow)` para que las ventanas puedan localizar su botón correspondiente en la barra de tareas durante la animación de minimizado (Fase 7).

---

## Fase 7: Ralentización y Trayectoria hacia la Barra de Tareas al Minimizar Ventanas

### Diagnóstico
En `Window.cs`, `Minimize()` solo ejecuta `AnimateScale(..., Vector3.zero)` en `0.2s` en el centro de la ventana sin desplazarse hacia la barra de tareas, haciendo difícil para los niños ver a dónde se ha guardado la ventana.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/DesktopSystem/Window.cs`
- `ComputerLearner/Assets/Scripts/DesktopSystem/TaskbarWindowButton.cs`
- `ComputerLearner/Assets/Scripts/DesktopSystem/DesktopTour.cs`

### Especificación Técnica
1. **En `Window.cs`:**
   - Añadir `[SerializeField] private float minimizeAnimationDuration = 0.48f;`, `private Vector3 preMinimizeLocalPosition;` y `private bool hasPreMinimizePosition;`.
   - En `Minimize()`: Guardar `preMinimizeLocalPosition` e iniciar la corrutina `AnimateMinimize()` que interpola durante `0.48s` (`Mathf.SmoothStep`) tanto la escala (`Vector3.one` → `new Vector3(0.08f, 0.05f, 1f)`) como la posición (`windowRectTransform.position` → posición del `TaskbarWindowButton` obtenido con `Taskbar.FindButtonForWindow(this)`).
   - Antes de desactivar el GameObject (`SetActive(false)`), restaurar `windowRectTransform.localPosition = preMinimizeLocalPosition` para que `Restore()` recupere su posición intacta, e invocar `targetBtn?.PulseHighlight()`.
2. **En `TaskbarWindowButton.cs`:**
   - Añadir `public void PulseHighlight()` que ilumine y pulse suavemente el botón en la barra de tareas durante `0.9s` al recibir la ventana minimizada, restaurando sus colores retro al terminar.
3. **En `DesktopTour.cs`:**
   - En el Paso 10 (Minimizar y mirar a la barra de tareas), invocar `targetBtn.PulseHighlight()` y dar `4.5s` en `WaitForDialogue` para que el jugador observe claramente el botón en la barra de tareas.

---

## Fase 8: Failsafe para Archivos del Sistema y Restauración de Archivos por Defecto

### Diagnóstico
Si un jugador elimina los archivos de práctica (`Welcome.txt`, `Mouse_Guide.txt`, `Mascot.png`, `Target_Badge.png`) y vacía la Papelera de Reciclaje, se queda sin archivos de ejemplo para practicar.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/DesktopSystem/FileExplorerApp.cs`
- `ComputerLearner/Assets/Scripts/DesktopSystem/DesktopContextMenuTrigger.cs`
- `ComputerLearner/Assets/Scripts/Data/UIDialogs.cs` y JSONs (`UIDialogs.json`, `EN`, `ES`, `NO`)

### Especificación Técnica
1. Añadir `IsProtectedSystemFolderStatic(ExplorerItemData item)` en `FileExplorerApp.cs` para blindar `This PC`, `Desktop`, `Documents`, `Pictures`, `Levels`, `Minigames` y `Recycle Bin` frente a borrado o renombrado.
2. Implementar `RestoreDefaultPracticeFilesStatic()` y `RestoreDefaultFiles()` en `FileExplorerApp.cs` para restaurar desde la Papelera o regenerar los archivos base (`Welcome.txt`, `Mouse_Guide.txt`, `Mascot.png`, `Target_Badge.png`) sin eliminar los archivos creados por el usuario.
3. Exponer la opción localizada `UIDialogs.Current.explorer_restore_defaults` (*"Restaurar Archivos por Defecto"* / *"Restore Default Files"*) en el menú contextual de `FileExplorerApp` y de `DesktopContextMenuTrigger`.

---

## Fase 9: Caja Azul de Selección Múltiple en el Escritorio (Marquee Selection)

### Diagnóstico
Arrastrar con el botón izquierdo sobre el fondo del escritorio debe dibujar el clásico rectángulo azul semitransparente de selección múltiple e iluminar todos los iconos abarcados.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/DesktopSystem/DesktopManager.cs`
- `ComputerLearner/Assets/Scripts/DesktopSystem/DesktopContextMenuTrigger.cs`
- `ComputerLearner/Assets/Scripts/DesktopSystem/DraggableIcon.cs`

### Especificación Técnica
1. **En `DraggableIcon.cs`:**
   - Añadir lista estática `selectedIcons` (`public static IReadOnlyList<DraggableIcon> SelectedIcons => selectedIcons;`), tarjeta de resaltado por instancia (`multiSelectCard`) y método `SetMarqueeSelected(bool selected)` para que múltiples iconos puedan mostrarse seleccionados simultáneamente.
2. **En `DesktopContextMenuTrigger.cs`:**
   - Implementar `IBeginDragHandler`, `IDragHandler` e `IEndDragHandler` para el botón izquierdo del ratón, delegando en `DesktopManager.Instance` (`BeginMarqueeSelection`, `UpdateMarqueeSelection`, `EndMarqueeSelection`) y evitando que `OnPointerClick` limpie la selección al soltar el arrastre.
3. **En `DesktopManager.cs`:**
   - Asegurar que `WallpaperImage` tenga `DesktopContextMenuTrigger` con `raycastTarget = true`.
   - Implementar el rectángulo visual `DesktopMarqueeSelectionBox` (relleno `new Color(0.2f, 0.5f, 1f, 0.22f)` y 4 bordes de `1.5px` `new Color(0.2f, 0.5f, 1f, 0.75f)`), calculando en `UpdateMarqueeSelection` el solapamiento con cada icono activo y permitiendo eliminar varios iconos seleccionados no protegidos al pulsar `Delete`.
