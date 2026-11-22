# Plan de Detalles y Mejoras (Playtest Semana 4 - Ronda 2: Attention to Detail) — Computer Learning (Unity 6)

> **Documento de especificación técnica para ejecución secuencial con `/goal`.**
> Este documento aborda los 6 puntos de "Attention to detail" del feedback de esta semana: presentación de Pointy por su nombre, implementación de los iconos reales de Paint (`Paint_App_Icon_01`) y Notepad (`Notepad_App_Icon_01`), duplicación de archivos/carpetas con `Ctrl + D`, deshacer la creación de archivos/carpetas con `Ctrl + Z`, y reloj interactivo configurable en la barra de tareas (`TaskbarClock`).

---

## Reglas Generales de Arquitectura y Contexto

1. **Preservar la arquitectura actual**: Respetar el namespace `ComputerLearning`, componentes modulares de Unity, prefabs y uGUI / Canvas (`RectTransform`).
2. **Input System**: El proyecto utiliza el **New Input System** (`activeInputHandler: 1`). Usar `UnityEngine.InputSystem.Keyboard.current` y `UnityEngine.InputSystem.Mouse.current` (manteniendo `#if ENABLE_LEGACY_INPUT_MANAGER` únicamente como fallback).
3. **Localización sincronizada**: Mantener sincronizados los archivos JSON de recursos (`*TutorialDialogs.json`, `*DesktopTourDialogs.json`, `*MascotFeedbackDialogs.json`, `*UIDialogs.json`) y sus clases C# correspondientes.
4. **Validación continua**: Comprobar que `Assembly-CSharp.csproj` compile con **0 errores** y que todos los archivos JSON modificados sean sintácticamente válidos.
5. **Documentación**: Al concluir todas las secciones, actualizar `Documentation/Specs/Diego.md` con una nueva sección numerada detallando los cambios implementados.

---

## Sección 1: Pointy se presenta por su nombre en los diálogos de bienvenida

### Diagnóstico de la causa raíz
1. Actualmente la mascota virtual ("Pointy") nunca menciona su nombre en ninguno de los diálogos iniciales ni en el modo mascota de escritorio:
   - En `TutorialDialogs.cs` y `Resources/*TutorialDialogs.json`:
     - `first_welcome`: `"Welcome to the computer! Let's learn how to use it."`
     - `virtual_welcome`: `"Welcome to your full Virtual Desktop! Here you can explore and practice freely."`
   - En `Resources/*DesktopTourDialogs.json`:
     - `step1_welcome`: `"Hi! Welcome to your computer!"`
   - En `VirtualMascot.cs` (`MascotFeedbackDialogs`) y `Resources/*MascotFeedbackDialogs.json`:
     - `companionWelcome`: `"Hello! I'm your desktop pet! Click on me whenever you want tips."`
     - `companionMessages[0]`: `"Hello! I'm your desktop pet! Feel free to explore at your own pace."`

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/Data/TutorialDialogs.cs`
- `ComputerLearner/Assets/Resources/TutorialDialogs.json`, `EN-TutorialDialogs.json`, `ES-TutorialDialogs.json`
- `ComputerLearner/Assets/Resources/DesktopTourDialogs.json`, `EN-DesktopTourDialogs.json`, `ES-DesktopTourDialogs.json`, `NO-DesktopTourDialogs.json`
- `ComputerLearner/Assets/Scripts/DesktopSystem/VirtualMascot.cs`
- `ComputerLearner/Assets/Resources/MascotFeedbackDialogs.json`, `EN-MascotFeedbackDialogs.json`, `ES-MascotFeedbackDialogs.json`

### Especificación Técnica
1. **En `TutorialDialogs.cs` y `*TutorialDialogs.json` (`EN`, `ES`, base):**
   - `first_welcome`:
     - EN / Base: `"Hi! I'm Pointy! Welcome to the computer! Let's learn how to use it."`
     - ES: `"¡Hola, soy Pointy! ¡Bienvenido al ordenador! Vamos a aprender a usarlo."`
   - `virtual_welcome`:
     - EN / Base: `"Hi, I'm Pointy! Welcome to your full Virtual Desktop! Here you can explore and practice freely."`
     - ES: `"¡Hola, soy Pointy! ¡Bienvenido a tu Escritorio Virtual completo! Aquí puedes explorar y practicar libremente."`
2. **En `*DesktopTourDialogs.json` (`EN`, `ES`, `NO`, base):**
   - `step1_welcome`:
     - EN / Base: `"Hi! I'm Pointy! Welcome to your computer!"`
     - ES: `"¡Hola, soy Pointy! ¡Bienvenido a tu ordenador!"`
     - NO: `"Hei! Jeg heter Pointy! Velkommen til datamaskinen din!"`
3. **En `VirtualMascot.cs` y `*MascotFeedbackDialogs.json` (`EN`, `ES`, base):**
   - `companionWelcome`:
     - EN / Base: `"Hello! I'm Pointy, your desktop pet! Click on me whenever you want tips."`
     - ES: `"¡Hola! ¡Soy Pointy, tu mascota de escritorio! Haz clic en mí cuando quieras consejos."`
   - `companionMessages[0]`:
     - EN / Base: `"Hello! I'm Pointy, your desktop pet! Feel free to explore at your own pace."`
     - ES: `"¡Hola! ¡Soy Pointy, tu mascota de escritorio! Explora a tu propio ritmo."`

### Criterios de aceptación
- Tanto al iniciar el juego en la ruta Beginner (`Desktop1`), Intermediate (`DesktopTour`) o Free Play / Virtual Desktop (`VirtualDesktopIntro` y mascota en la esquina), Pointy se presenta claramente por su nombre (**Pointy**).

---

## Sección 2: Implementar los iconos oficiales de Paint (`Paint_App_Icon_01`) y Notepad (`Notepad_App_Icon_01`)

### Diagnóstico de la causa raíz
1. En `ComputerLearner/Assets/Art/UI/App icons/` existen los sprites dedicados para las aplicaciones Paint y Notepad:
   - `Paint_App_Icon_01.png` (`guid: e9fc926c64fcf274599cf7503484609c`)
   - `Notepad_App_Icon_01.png` (`guid: b81c831d7ad7db04b9efe247f0f68194`)
2. Sin embargo, ninguno de los dos GUIDs está referenciado en el proyecto:
   - En `VirtualDesktop.unity` (líneas 4623–4633), `initialDesktopIcons.Array.data[3]` (`Notepad`) tiene asignado `Text_File_Icon_W01_Scaled.png` (`guid: 1ee91fb1395357449bc80a3bb4fa13ac`), e `initialDesktopIcons.Array.data[5]` (`Paint`) tiene asignado `Image_File_Icon_01_Scaled.png` (`guid: 1a2cf4b2658908e40b13cd6baccd71c4`).
   - En el Menú de Inicio (`StartMenu` en `VirtualDesktop.unity`, línea 2750 y `BtnPaint`), los botones `btnNotepad` y `BtnPaint` tampoco usan `Notepad_App_Icon_01` ni `Paint_App_Icon_01`.
   - En `DesktopManager.SpawnIcon()` (líneas 1215–1233) y `StartMenuController.cs` (`OpenNotepad`, `OpenPaint`), no existen getters de fallback para cargar `Paint_App_Icon_01` y `Notepad_App_Icon_01`.

### Archivos a modificar
- `ComputerLearner/Assets/Resources/Paint_App_Icon_01.png` y `Notepad_App_Icon_01.png` (copiar desde `Assets/Art/UI/App icons/` a `Assets/Resources/` igual que `RecyclingBin_Icon_01.png` para carga garantizada en runtime/builds)
- `ComputerLearner/Assets/Scenes/VirtualDesktop.unity`
- `ComputerLearner/Assets/Scripts/DesktopSystem/FileExplorerApp.cs`
- `ComputerLearner/Assets/Scripts/DesktopSystem/DesktopManager.cs`
- `ComputerLearner/Assets/Scripts/DesktopSystem/StartMenuController.cs`

### Especificación Técnica
1. **Disponibilidad en `Resources` y helpers en `FileExplorerApp.cs`:**
   - Copiar `Paint_App_Icon_01.png` y `Notepad_App_Icon_01.png` de `Assets/Art/UI/App icons/` a `Assets/Resources/`.
   - En `FileExplorerApp.cs`, añadir métodos estáticos cacheados:
     - `public static Sprite GetPaintAppSprite()`: carga desde `AssetDatabase` (`"Assets/Art/UI/App icons/Paint_App_Icon_01.png"`) en Editor y `Resources.Load<Sprite>("Paint_App_Icon_01")` en runtime.
     - `public static Sprite GetNotepadAppSprite()`: carga desde `AssetDatabase` (`"Assets/Art/UI/App icons/Notepad_App_Icon_01.png"`) en Editor y `Resources.Load<Sprite>("Notepad_App_Icon_01")` en runtime.
2. **En `DesktopManager.cs` (`SpawnIcon` y `RegisterSceneIconsIntoDesktopFolder`):**
   - Cuando `iconData.iconName.Equals("Paint", StringComparison.OrdinalIgnoreCase)`, asignar `spriteToUse = FileExplorerApp.GetPaintAppSprite() ?? iconData.iconSprite;`.
   - Cuando `iconData.iconName.Equals("Notepad", StringComparison.OrdinalIgnoreCase)`, asignar `spriteToUse = FileExplorerApp.GetNotepadAppSprite() ?? iconData.iconSprite;`.
   - (Manteniendo `FileExplorerApp.GetTextFileSprite()` y `GetImageFileSprite()` para los documentos `.txt` y `.png` creados por el usuario).
3. **En `StartMenuController.cs`:**
   - En `Awake()` / `Start()`, actualizar el icono hijo (`Icon`) de `btnNotepad` con `FileExplorerApp.GetNotepadAppSprite()` y el de `btnPaint` con `FileExplorerApp.GetPaintAppSprite()`.
   - En `OpenNotepad()`, `OpenNotepadWithFile(...)`, `OpenPaint()` y `OpenPaintWithFile(...)`, pasar `FileExplorerApp.GetNotepadAppSprite()` y `FileExplorerApp.GetPaintAppSprite()` como `appIcon` a `OpenAppWindow(...)` para que la barra de título de la ventana y el botón en la barra de tareas (`TaskbarWindowButton`) también muestren los nuevos iconos de aplicación.
4. **En `VirtualDesktop.unity`:**
   - Actualizar `initialDesktopIcons.Array.data[3].iconSprite` (`Notepad`) a `{fileID: 21300000, guid: b81c831d7ad7db04b9efe247f0f68194, type: 3}`.
   - Actualizar `initialDesktopIcons.Array.data[5].iconSprite` (`Paint`) a `{fileID: 21300000, guid: e9fc926c64fcf274599cf7503484609c, type: 3}`.

### Criterios de aceptación
- El acceso directo de **Paint** en el Escritorio, en el Menú de Inicio, en la barra de título de su ventana y en la barra de tareas muestra `Paint_App_Icon_01`.
- El acceso directo de **Notepad** en el Escritorio, en el Menú de Inicio, en la barra de título de su ventana y en la barra de tareas muestra `Notepad_App_Icon_01`.
- Los archivos `.txt` y `.png` de usuario siguen mostrando sus iconos de documento correspondientes.

---

## Sección 3: Duplicar carpetas y archivos con `Ctrl + D` y Deshacer creación con `Ctrl + Z`

### Diagnóstico de la causa raíz
1. **Falta atajo `Ctrl + D` para duplicar:**
   - Actualmente `FileExplorerApp.Update()` (líneas 734–775) y `DesktopManager.Update()` (líneas 354–410) solo escuchan `Delete`, `Ctrl + C`, `Ctrl + X` y `Ctrl + V`. Si el jugador selecciona un archivo o carpeta y pulsa `Ctrl + D`, no ocurre nada.
2. **Falta atajo `Ctrl + Z` para deshacer la creación de carpetas o archivos:**
   - No existe una pila de historial de creación (`createdItemsUndoStack`) en `FileExplorerApp`. Cuando el jugador crea una carpeta o archivo nuevo en el Escritorio o en el Explorador de Archivos (o lo duplica/pega) y pulsa `Ctrl + Z`, no se deshace la creación.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/DesktopSystem/FileExplorerApp.cs`
- `ComputerLearner/Assets/Scripts/DesktopSystem/DesktopManager.cs`
- `ComputerLearner/Assets/Scripts/DesktopSystem/DraggableIcon.cs`
- `ComputerLearner/Assets/Scripts/DesktopSystem/DesktopContextMenuTrigger.cs`

### Especificación Técnica
1. **Duplicación directa con `Ctrl + D` (`DuplicateItemsInFolder`, `DuplicateSelectedItems`, `DuplicateSelectedDesktopIcons`):**
   - En `FileExplorerApp.cs`:
     - Implementar `public static List<ExplorerItemData> DuplicateItemsInFolder(IEnumerable<ExplorerItemData> sourceItems, ExplorerItemData targetFolder, out string errorMessage)`:
       - Validar que `targetFolder` no sea nulo ni de solo lectura (`!IsReadOnlySystemFolderStatic(targetFolder)`).
       - Para cada elemento no protegido (`!IsItemProtectedStatic(srcItem)`):
         - Comprobar capacidad (`if (IsFolderAtCapacity(targetFolder))` -> mostrar aviso `UIDialogs.Current.explorer_folder_limit_reached`, `FeedbackManager.PlayError(true)` y `VirtualMascot.ShowContextualFeedback(...)`).
         - Generar nombre único con `GetUniqueItemNameStatic(targetFolder, nameWithoutExt, ext)` (respetando el límite de longitud `MAX_BASE_NAME_LENGTH`).
         - Clonar el elemento (incluyendo `textContent`, `imageContent` y subárbol `CloneChildrenRecursive` si es carpeta) **sin sobrescribir el portapapeles de `Ctrl + C`**.
         - Añadir el duplicado a `targetFolder.children`, registrarlo en la pila de deshacer (`RegisterCreatedItemsForUndo`) y llamar a `NotifyFileSystemChanged()`.
     - Añadir método de instancia `public void DuplicateSelectedItems()` en `FileExplorerApp.cs` que duplique `selectedItems` en `currentFolder`, seleccione los nuevos duplicados y reproduzca `FeedbackManager.PlaySuccess()`.
     - En `FileExplorerApp.Update()`: detectar `bool isD = kb != null && kb.dKey.wasPressedThisFrame;` con `isCtrl` y llamar a `DuplicateSelectedItems()`.
   - En `DraggableIcon.cs` y `DesktopManager.cs`:
     - Añadir `public static void DuplicateSelectedDesktopIcons()` en `DraggableIcon.cs` que recopile los `LinkedExplorerItem` de los iconos seleccionados no protegidos en el escritorio y llame a `FileExplorerApp.DuplicateItemsInFolder(items, FileExplorerApp.GetDesktopFolder(), out _)`.
     - En `DesktopManager.Update()`: detectar `isCtrl && isD` cuando el escritorio tiene el foco y hay iconos seleccionados, e invocar `DraggableIcon.DuplicateSelectedDesktopIcons()`.
2. **Deshacer la creación de carpetas y archivos con `Ctrl + Z` (`RegisterCreatedItemsForUndo`, `UndoLastCreatedItems`):**
   - En `FileExplorerApp.cs`:
     - Añadir `private static readonly Stack<List<ExplorerItemData>> createdItemsUndoStack = new Stack<List<ExplorerItemData>>();` y limpiarla en `ResetStaticFileSystem()`.
     - Añadir `public static void RegisterCreatedItemsForUndo(IEnumerable<ExplorerItemData> createdItems)` (y sobrecarga para un solo `ExplorerItemData`).
     - Invocar `RegisterCreatedItemsForUndo` siempre que se cree un nuevo elemento no protegido en:
       - `FileExplorerApp.CreateNewFolder`, `CreateNewTextDocument`, `CreateNewImageDocument`
       - `DesktopContextMenuTrigger.CreateFolderOnDesktop`, `CreateTextDocumentOnDesktop`, `CreateImageDocumentOnDesktop`
       - `FileExplorerApp.DuplicateItemsInFolder` (`Ctrl + D`)
       - `FileExplorerApp.PasteClipboardBatchIntoFolder` (cuando es copia `!wasCut`)
     - Implementar `public static bool UndoLastCreatedItems()`:
       - Mientras `createdItemsUndoStack.Count > 0`, extraer la última lista de elementos creados.
       - Si el modal de renombrado (`InputModalDialog`) estuviera abierto justo tras crear ese elemento, cerrarlo limpiamente.
       - Para cada `ExplorerItemData` de la entrada:
         - Eliminarlo del árbol del sistema de archivos (`RemoveItemRecursive(GetRootFolder(), item)`).
         - Si existe un `DraggableIcon` en el Escritorio vinculado a ese `ExplorerItemData` (`icon.LinkedExplorerItem == item`), desregistrarlo de `DesktopManager` y destruir su `GameObject`.
       - Si se eliminó al menos un elemento: llamar a `NotifyFileSystemChanged()`, reproducir `FeedbackManager.PlayClick()`, mostrar mensaje de estado (`"Undid creation of ..."`), y devolver `true`.
   - En `FileExplorerApp.Update()` y `DesktopManager.Update()`:
     - Detectar `bool isZ = kb != null && kb.zKey.wasPressedThisFrame;` con `isCtrl`.
     - **Aislamiento de foco:** Solo ejecutar `FileExplorerApp.UndoLastCreatedItems()` cuando la ventana activa **no** sea `NotepadApp` ni `PaintApp` (para no interferir con el `Ctrl + Z` de texto o dibujo ni con los niveles guiados 13, 15 y 16) y no se esté escribiendo en otro `TMP_InputField` ajeno.

### Criterios de aceptación
- Seleccionar cualquier carpeta o archivo no protegido (en el Escritorio o en el Explorador de Archivos) y pulsar `Ctrl + D` crea inmediatamente una copia duplicada en la misma ubicación.
- Tras crear una carpeta o archivo (o duplicarlo con `Ctrl + D`), pulsar `Ctrl + Z` deshace su creación y lo elimina inmediatamente tanto del Explorador como del Escritorio.

---

## Sección 4: Permitir cambiar la hora y fecha del reloj de la barra de tareas (`TaskbarClock`)

### Diagnóstico de la causa raíz
1. `TaskbarClock.cs` (en `ClockTray` dentro de `Taskbar.prefab`) solo muestra `DateTime.Now` en modo de solo lectura y no responde a clics ni permite al jugador modificar la hora, los minutos, la fecha o el formato (12h/24h).

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/DesktopSystem/TaskbarClock.cs`
- `ComputerLearner/Assets/Scripts/Managers/SceneSystem.cs` y `SessionManager.cs` (para resetear el offset del reloj al reiniciar progreso/sesión)

### Especificación Técnica
1. **Hacer `TaskbarClock` interactivo (`IPointerClickHandler`):**
   - En `TaskbarClock.Awake()` / `OnEnable()`, asegurar que el `ClockTray` tenga un componente `Image` (transparente o con el estilo del tray) con `raycastTarget = true` para recibir clics.
   - Mantener variables estáticas de ajuste de reloj en `TaskbarClock`:
     - `private static int hourOffset = 0;`
     - `private static int minuteOffset = 0;`
     - `private static int dayOffset = 0;`
     - `private static bool use12HourFormat = false;`
     - `public static DateTime GetCurrentVirtualDateTime() => DateTime.Now.AddDays(dayOffset).AddHours(hourOffset).AddMinutes(minuteOffset);`
     - `public static void ResetClockSettings()` (resetea offsets a `0` y `use12HourFormat = false`, invocado también en `SceneSystem.ResetProgress()` y `SessionManager.ApplyConfiguration(..., resetExistingProgress: true)`).
2. **Popup retro "Date & Time Settings" al hacer clic en el reloj:**
   - Al hacer clic (izquierdo o derecho) en `ClockTray` (siempre que no haya un `TutorialBlocker` bloqueando el reloj), abrir/cerrar un pequeño panel flotante estilo retro OS (`ClockSettingsPopup`, `260 × 210` px) anclado justo encima del reloj en la esquina inferior derecha del Canvas principal:
     - Barra de título azul marino (`#143685`) con texto `"Date & Time"` y botón de cierre `[x]`.
     - Pantalla digital central mostrando la hora y fecha virtual en vivo.
     - Fila **Hour**: botones `[-]` y `[+]` (`hourOffset` `-1` / `+1` h).
     - Fila **Minute**: botones `[-10]`, `[-1]`, `[+1]`, `[+10]` (`minuteOffset`).
     - Fila **Date**: botones `[-]` y `[+]` (`dayOffset` `-1` / `+1` día).
     - Botones inferiores: **`12h / 24h`** (alterna formato 24h `17:30` vs 12h `05:30 PM`) y **`Reset`** (restaura la hora real del sistema).
   - Cerrar el popup automáticamente al pulsar `Escape`, hacer clic en `[x]` o hacer clic fuera del panel y del `ClockTray`.

### Criterios de aceptación
- Al hacer clic sobre el reloj de la esquina inferior derecha en la barra de tareas, se abre el panel `"Date & Time"` que permite adelantar/atrasar horas, minutos y días, cambiar entre formato `24h` y `12h` o restaurar la hora real, actualizando inmediatamente el reloj de la barra de tareas.

---

## Sección 5: Verificación Integral y Documentación

1. Compilar `ComputerLearner/Assembly-CSharp.csproj` verificando **0 errores de compilación**.
2. Validar la sintaxis de todos los archivos JSON en `ComputerLearner/Assets/Resources/`.
3. Actualizar `Documentation/Specs/Diego.md` con una nueva sección numerada documentando las implementaciones de esta ronda.
