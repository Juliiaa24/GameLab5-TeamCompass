# Plan de Implementación: Tutorial Guiado de Pointy para el Teacher Mode y Classroom Dashboard — Computer Learning (Unity 6)

> **Documento de especificación técnica para ejecución secuencial con `/goal`.**
> Este documento recoge el análisis exhaustivo de toda la arquitectura actual del **Teacher Mode** y especifica la implementación paso a paso de un tutorial interactivo guiado por **Pointy (`VirtualMascot`)** que se ejecuta automáticamente la primera vez que se desbloquea el Teacher Mode (y la primera vez que se abre el Classroom Dashboard), con opción de repetición mediante botón de ayuda (`?`), soporte de `VirtualMascot` en `Menu.unity` y traducciones completas en **Inglés (`EN` + base)**, **Español (`ES`)** y **Noruego (`NO`)**.

---

## 1. Análisis Completo de lo Implementado en el Teacher Mode

Actualmente el **Teacher Mode** está distribuido en 7 módulos principales dentro de `Menu.unity` y managers persistentes (`DontDestroyOnLoad`):

### 1.1. Acceso Discreto, Seguridad de Build y Modal de PIN (`TeacherModeGate.cs` y `TeacherModeConfig.cs`)
- **Botón discreto en esquina (`Btn-TeacherAccess`):** Ubicado en la esquina inferior derecha del menú principal (`(-34, 28)`, `204 × 56` px). Muestra el icono de candado cerrado/abierto (`iconLock` / `iconUnlock`) y el texto localizado (`"Teacher"` / `"Teacher (Unlocked)"`).
- **Configuración (`Resources/TeacherModeConfig.asset`):**
  - `allowTeacherMode = true`: Permite ocultar y desactivar por completo el modo profesor en builds exclusivos para alumnos.
  - `requireLongPress` y `longPressDuration = 1.5f`: Soporte para pulsación mantenida (`TeacherHoldTrigger` + barra de relleno `HoldProgressFill`).
  - `defaultPin = "1234"`: PIN por defecto de 4 dígitos, sobreescribible en tiempo de ejecución mediante `PlayerPrefs` (`"TeacherMode_CustomPin_v1"`).
- **Bloqueo automático en modo Cliente LAN (`IsConnectedAsStudent`):** Si el ordenador está conectado como alumno a la clase de un profesor (`NetworkSessionClient.ExistingInstance.IsConnected`), el botón de Teacher Mode se oculta y cualquier ventana de profesor se cierra automáticamente.
- **Modal de PIN (`TeacherPinModal` -> `PinWindow`):**
  - Soporta teclado numérico en pantalla (`0–9`, `Clear`, `Del`) y teclado físico (`New Input System`: dígitos principales, numpad, `Backspace`, `Enter`, `Escape`).
  - Muestra 4 indicadores circulares (`PinDot_0..3`) y validación automática al introducir el 4.º dígito (`TryUnlockWithPin`).

### 1.2. Ventana Principal de Control del Profesor (`TeacherModeOverlay` -> `TeacherControlWindow`, `1260 × 760` px)
Estructurada en dos columnas y un pie de ventana:
- **Columna Izquierda (`LeftColumn_LearningPath` — `LearningPathSelectorWidget.cs` + Reinicio):**
  1. **Selector de Ruta de Aprendizaje (`Btn-Path0`, `Btn-Path1`, `Btn-Path2` -> `SessionManager.SetLearningPath`):**
     - **Beginner (`BeginnerFoundations`):** Flujo guiado completo desde `Desktop1` (Niveles 1–5) hacia `VirtualDesktop` (`DesktopTour` -> Niveles 6–9 -> `AdvancedDesktopTour` -> Niveles 10–16).
     - **Intermediate (`Intermediate`):** Omite `Desktop1`; entra directamente en `VirtualDesktop` ejecutando los tutoriales de ventanas (`DesktopTour` + `AdvancedDesktopTour`) y desbloquea el escritorio para completar los Niveles 1–16 progresivamente.
     - **Advanced (`Advanced`):** Entra directamente en `VirtualDesktop` sin tutoriales iniciales y con todas las aplicaciones disponibles, manteniendo la progresión de los Niveles 1–16.
  2. **Interruptor Independiente de Evaluación Inicial (`Btn-ToggleAssessment` -> `SessionManager.SetRunAssessmentOverride`):**
     - Activa o desactiva `Initial Assessment: ON / OFF` (`runAssessment`). Cuando está en `OFF`, precarga en `SkillManager` el perfil de habilidades base de la ruta (`SkillProfilePreset`) y salta `AssessmentScene`.
  3. **Resumen de Flujo (`SummaryFooterText`):** Muestra en vivo el resumen localizado de la combinación seleccionada (`StudentConfiguration.GetPathShortSummary`).
  4. **Reinicio para el Siguiente Alumno (`Btn-ResetProgress` -> `SceneSystem.ResetProgress()`):**
     - Exporta automáticamente los datos del alumno actual si realizó actividad, limpia todo el progreso (archivos virtuales, fondo de pantalla, reloj, habilidades y medallas) y avanza automáticamente el identificador al siguiente alumno (`S01` -> `S02`) conservando la ruta y el ajuste de evaluación elegidos por el profesor.
- **Columna Derecha (`RightColumn_ClassroomAndTools` — `PlaytestStudentWidget.cs`, `ClassroomNetworkWidget.cs`, `TeacherModeGate.cs`):**
  1. **Sección `Student & Progress Tools` (`PlaytestStudentWidget.cs` y `PlaytestDataManager.cs`):**
     - Muestra el `Student ID` activo (`TeacherStudentIdValue`, ej. `S01`).
     - **`Btn-ExportPlaytest` (`Export Data`):** Exporta manualmente los informes del alumno actual (`.txt`, `.html`, `.csv`, `.json` y `All_Students_Summary.csv`) a `Desktop/ComputerLearner_PlaytestData`.
     - **`Btn-AutoDownload` (`Auto Download: ON / OFF`):** Activa/desactiva el guardado automático de telemetría de playtesting.
  2. **Sección `Classroom & Dashboard` (`ClassroomNetworkWidget.cs` y `TeacherModeGate.cs`):**
     - **`Input-ClassName`:** Define el nombre personalizado del aula (`TeacherCustomClassName`, ej. `"Room 3B"`) que verán los alumnos al buscar clases en red.
     - **`Btn-OpenTeacherDashboard` (`Open Teacher Dashboard`):** Abre el panel completo de gestión de aula en red (`TeacherDashboardModal`) e inicia el servidor LAN (`NetworkSessionHost.StartHost()`).
     - **`TeacherHostStatusText`:** Muestra si el servidor de clase está apagado o activo con el número de alumnos conectados e `IP:Puerto`.
  3. **Sección `Teacher PIN` (`TeacherModeGate.cs`):**
     - **`Input-NewPin` + `Btn-SavePin`:** Permite cambiar el código PIN de 4 dígitos y guardarlo en `PlayerPrefs`.
- **Pie de Ventana (`Btn-LockTeacherMode` — `"Lock & Student Mode"`):**
  - Cierra las ventanas de profesor, vuelve a bloquear el Teacher Mode (`IsTeacherUnlocked = false`) y devuelve la pantalla al menú infantil del alumno.

### 1.3. Panel del Aula en Red (`TeacherDashboardModal` -> `DashboardWindow`, `1740 × 940` px — `TeacherDashboardController.cs` + `NetworkSessionHost.cs`)
- **Barra Superior (`WindowTitleBar`):**
  - Muestra el estado del servidor LAN (`IP {0}:{1} - {2} students` o `Server: Offline`).
  - **`Btn-ClearOffline`:** Limpia de la lista los alumnos desconectados (`RemoveDisconnectedStudents()`).
  - **`Btn-ToggleServer`:** Inicia o detiene el servidor LAN (`Start Class Server` / `Stop Server`).
  - **`Btn-CloseDashboard` (`[X]`):** Cierra el dashboard.
- **Barra de Control Masivo (`BatchControlsBar`):**
  - Permite elegir la configuración por defecto para toda la clase (`Class Default`: `Beginner` / `Intermediate` / `Advanced` + `Assess: ON / OFF`).
  - **`Btn-ApplyAll` (`Apply to All`):** Envía por red la configuración seleccionada a todos los ordenadores de alumnos conectados.
  - **`Btn-LaunchAll` (`Launch All`):** Inicia remotamente la partida en todos los ordenadores de los alumnos conectados al mismo tiempo.
- **Barra de Nombre de Clase y Permisos (`PermissionsAndNameBar`):**
  - **`Input-ClassName`:** Edición en vivo del nombre de la clase.
  - **`Btn-LockStudentNames` (`Lock Student Names: ON / OFF`):** Bloquea o desbloquea remotamente el campo de nombre en el menú de todos los alumnos conectados.
  - **`Btn-LockLeaveClass` (`Lock Leave Class: ON / OFF`):** Oculta/bloquea el botón `"Leave Class"` en los clientes para evitar que los niños salgan de la sesión accidentalmente.
- **Lista Izquierda de Alumnos Conectados (`StudentsListCard` -> `StudentsScroll`):**
  - Genera una tarjeta interactiva (`StudentRowTemplate`) por cada alumno conectado con: indicador Online/Offline, ID del alumno (`S01..S99`) con botones `[-]` y `[+]` para reasignar su ID remotamente, escena actual y niveles completados (`X/16`), botones individuales de ruta (`Beginner`/`Intermediate`/`Advanced`), botón individual `Assess: ON/OFF`, botón **`Start`** remoto y botón **`Reset`** remoto.
- **Inspector Derecho de Telemetría en Vivo (`InspectorCard` -> `InspectorContent`):**
  - Al seleccionar a un alumno de la lista, muestra en tiempo real su progreso: Avatar, Nombre/Dispositivo, IP/Escena/Ruta, **3 tarjetas KPI** (`Levels Completed X/16`, `Medals Earned`, `Total Points / Average %`) y **9 barras de progreso de habilidades en vivo** (`mouse_move`, `hover`, `click`, `double_click`, `right_click`, `click_hold`, `drag_drop`, `typing`, `shortcuts`).

---

## 2. Reglas Generales de Arquitectura y Contexto

1. **Preservar la arquitectura actual**: Respetar el namespace `ComputerLearning`, componentes modulares de Unity, prefabs y uGUI / Canvas (`RectTransform`).
2. **Input System**: El proyecto utiliza el **New Input System** (`activeInputHandler: 1`). Usar `UnityEngine.InputSystem.Keyboard.current` y `UnityEngine.InputSystem.Mouse.current`.
3. **Localización Trilingüe Completa (`EN`, `ES`, `NO` + base):** Todos los diálogos del tutorial de Pointy para el Teacher Mode y etiquetas de botones deben declararse en `UIDialogs.cs` y mantenerse 100% sincronizados en `UIDialogs.json`, `EN-UIDialogs.json`, `ES-UIDialogs.json` y `NO-UIDialogs.json`.
4. **Persistencia ante `ResetProgress()`:** El flag de completado del tutorial del profesor (`"TeacherMode_PointyTutorialCompleted_v1"` y `"TeacherDashboard_PointyTutorialCompleted_v1"`) y el PIN personalizado (`"TeacherMode_CustomPin_v1"`) **deben preservarse** en `SceneSystem.ResetProgress()` cuando el profesor reinicia el progreso para pasar de un alumno al siguiente (`S01 -> S02`).
5. **Validación continua y Documentación**: Verificar que `Assembly-CSharp.csproj` compile con **0 errores**, que todos los archivos JSON sean sintácticamente válidos y actualizar `Documentation/Specs/Diego.md` con una nueva sección numerada al finalizar.

---

## Sección 1: Soporte de Pointy (`VirtualMascot`) en `Menu.unity` y Posicionamiento con Offset en Tutoriales de UI

### Diagnóstico de la causa raíz
1. `VirtualMascotPrefab.prefab` (`Assets/Prefabs/Desktop/VirtualMascotPrefab.prefab`) solo se instancia actualmente a través de `DesktopManager.SpawnVirtualMascot()` en las escenas de escritorio (`Desktop1`, `VirtualDesktop`, etc.). En `Menu.unity` no existe `DesktopManager` ni una instancia de `VirtualMascot`, y el prefab tampoco está dentro de `Assets/Resources/`, por lo que llamar a `VirtualMascot.Show(...)` en `Menu.unity` imprime un `Debug.LogWarning` y no muestra a Pointy.
2. En `VirtualMascot.ShowMessage(string message, RectTransform target, Vector2? offset = null)` (líneas 918–929), `currentTargetOffset` solo aplica `offset` cuando el target es `LevelCelebrationModal` o `LevelIntroPanel`, forzando `Vector2.zero` en el resto de casos. Además, cuando `TutorialBlocker` está activo (`Canvas.sortingOrder = 30000`), el modal de Teacher Mode está debajo del `TutorialBlocker`, por lo que `VirtualMascot` necesita renderizarse por encima de los modales del menú para que Pointy y su bocadillo se vean siempre nítidos y sin quedar tapados.

### Archivos a modificar
- `ComputerLearner/Assets/Resources/VirtualMascotPrefab.prefab` (copiar desde `ComputerLearner/Assets/Prefabs/Desktop/VirtualMascotPrefab.prefab` a `Assets/Resources/`)
- `ComputerLearner/Assets/Scripts/DesktopSystem/VirtualMascot.cs`

### Especificación Técnica
1. **Disponibilidad de `VirtualMascotPrefab` en `Assets/Resources/`:**
   - Copiar `ComputerLearner/Assets/Prefabs/Desktop/VirtualMascotPrefab.prefab` a `ComputerLearner/Assets/Resources/VirtualMascotPrefab.prefab` para que pueda cargarse dinámicamente en `Menu.unity` (tanto en Editor como en builds Standalone/WebGL).
2. **Auto-instanciación segura en `VirtualMascot.cs` (`EnsureInstanceOnCanvas`):**
   - Añadir un método público estático `public static VirtualMascot EnsureInstanceOnCanvas(Transform referenceTransform = null)`:
     - Si `Instance == null`, buscar primero con `Object.FindFirstObjectByType<VirtualMascot>(FindObjectsInactive.Include)`.
     - Si sigue siendo `null`:
       - Resolver el `Canvas` raíz adecuado (`referenceTransform != null ? referenceTransform.GetComponentInParent<Canvas>() : null`, o `Object.FindFirstObjectByType<Canvas>()`).
       - Cargar `GameObject prefab = Resources.Load<GameObject>("VirtualMascotPrefab");` (con fallback en Editor a `AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Desktop/VirtualMascotPrefab.prefab")`).
       - Instanciar `prefab` como hijo directo del `Canvas` raíz (`mascotInstance.name = "VirtualMascot"`), inicializar `Instance = mascotInstance.GetComponent<VirtualMascot>()`, asegurarse de que `isDesktopCompanion = false` y devolver `Instance`.
   - Llamar a `EnsureInstanceOnCanvas(target)` al inicio de `VirtualMascot.Show(...)` y `VirtualMascot.ShowContextualFeedback(...)` cuando `Instance == null`.
3. **Respeto de `offset` explícito en `VirtualMascot.ShowMessage(...)`:**
   - En `VirtualMascot.ShowMessage(...)`, en la rama `else` (cuando no es `isCelebrationTarget` ni `isIntroTarget`), cambiar:
     `currentTargetOffset = offset ?? Vector2.zero;`
     (Como todas las llamadas que no desean desplazamiento pasan `null` o `Vector2.zero`, esto mantiene el comportamiento exacto en todos los niveles y permite que los tutoriales pasen un `offset` preciso cuando apuntan a tarjetas grandes del Teacher Mode).
4. **Visibilidad sobre modales en `Menu.unity`:**
   - En `VirtualMascot.ShowMessage(...)`, además de `transform.SetAsLastSibling()`, asegurar que si el `VirtualMascot` está en un `Canvas` con modales activos, siempre sea el último hermano del `Canvas` raíz (justo por encima de `TeacherModeOverlay` y `TeacherDashboardModal`).

### Criterios de aceptación
- Llamar a `VirtualMascot.Show(mensaje, targetRect, offset)` dentro de `Menu.unity` instancia automáticamente a Pointy sobre el `Canvas` principal si no existía, lo posiciona apuntando limpiamente al `targetRect` con su `offset` y muestra su bocadillo perfectamente encuadrado en pantalla.

---

## Sección 2: Traducciones Completas del Tutorial de Pointy para Teacher Mode (`EN`, `ES`, `NO` y base)

### Diagnóstico de la causa raíz
Actualmente `UIDialogs.cs` y los 4 archivos JSON (`UIDialogs.json`, `EN-UIDialogs.json`, `ES-UIDialogs.json`, `NO-UIDialogs.json`) contienen las etiquetas estáticas de los botones del Teacher Mode y del Classroom Dashboard, pero carecen de los diálogos explicativos paso a paso de Pointy para guiar al profesor la primera vez que entra.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/Data/UIDialogs.cs`
- `ComputerLearner/Assets/Resources/UIDialogs.json`
- `ComputerLearner/Assets/Resources/EN-UIDialogs.json`
- `ComputerLearner/Assets/Resources/ES-UIDialogs.json`
- `ComputerLearner/Assets/Resources/NO-UIDialogs.json`

### Especificación Técnica
Añadir en `UIDialogs.cs` y en los 4 archivos JSON (`UIDialogs.json`, `EN-UIDialogs.json`, `ES-UIDialogs.json`, `NO-UIDialogs.json`) las siguientes claves:

1. **Botón para ver/repetir el tutorial (`teacher_btn_tutorial_help`):**
   - **EN / Base:** `"? Tutorial"`
   - **ES:** `"? Tutorial"`
   - **NO:** `"? Veiledning"`

2. **Diálogos del Tutorial de la Ventana Principal (`Teacher Control Window` — 7 pasos):**
   - `teacher_tut_welcome`:
     - **EN / Base:** `"Hi! I'm Pointy! Welcome to the Teacher Control Window. Let me quickly show you how everything works! (Press SPACE or click my speech bubble to continue)."`
     - **ES:** `"¡Hola! ¡Soy Pointy! Bienvenido a la Ventana de Control del Profesor. ¡Déjame enseñarte rápidamente cómo funciona todo! (Pulsa ESPACIO o haz clic en mi bocadillo para avanzar)."`
     - **NO:** `"Hei! Jeg heter Pointy! Velkommen til lærerens kontrollvindu. La meg raskt vise deg hvordan alt fungerer! (Trykk MELLOMROM eller klikk på snakkeboblen min for å fortsette)."`
   - `teacher_tut_paths`:
     - **EN / Base:** `"Here you can choose the Learning Path for this PC: Beginner (full guided onboarding from scratch), Intermediate (window tutorials directly on the Virtual Desktop), or Advanced (direct Virtual Desktop without tutorials)."`
     - **ES:** `"Aquí puedes elegir la Ruta de Aprendizaje para este PC: Principiante (guía completa paso a paso desde cero), Intermedio (tutoriales de ventanas directamente en el Escritorio Virtual) o Avanzado (Escritorio Virtual directo sin tutoriales)."`
     - **NO:** `"Her velger du læringsstien for denne PC-en: Nybegynner (full veiledet oppstart fra bunnen av), Viderekommen (vindusveiledning direkte på det virtuelle skrivebordet) eller Avansert (direkte til skrivebordet uten veiledning)."`
   - `teacher_tut_assessment`:
     - **EN / Base:** `"Use this toggle to turn the Initial Assessment ON or OFF. When ON, the student starts with a quick diagnostic mouse test; when OFF, they skip straight into their assigned path!"`
     - **ES:** `"Usa este botón para activar o desactivar la Evaluación Inicial. En SÍ, el alumno empieza con una prueba diagnóstica rápida de ratón; en NO, pasa directamente a la ruta elegida."`
     - **NO:** `"Bruk denne knappen for å slå startkartleggingen PÅ eller AV. Når den er PÅ, starter eleven med en rask musetest; når den er AV, hopper eleven rett til valgt læringssti!"`
   - `teacher_tut_reset`:
     - **EN / Base:** `"When a student finishes, click 'Reset Progress'. It automatically exports their activity data, clears all files and medals, and advances the Student ID (like S01 to S02) for the next student!"`
     - **ES:** `"Cuando un alumno termine, pulsa «Reiniciar Progreso». Guardará automáticamente sus datos, borrará los archivos y medallas, ¡y avanzará el ID al siguiente alumno (ej. de S01 a S02)!"`
     - **NO:** `"Når en elev er ferdig, klikker du «Nullstill fremgang». Det eksporterer elevens data automatisk, tømmer filer og medaljer, og bytter elev-ID (f.eks. S01 til S02) for neste elev!"`
   - `teacher_tut_export`:
     - **EN / Base:** `"In Student & Progress Tools, you can check the current Student ID, click 'Export Data' anytime to save detailed reports to your Desktop folder, or toggle 'Auto Download'."`
     - **ES:** `"En Alumno y Progreso puedes ver el ID del alumno actual, pulsar «Exportar Datos» en cualquier momento para guardar informes detallados en el Escritorio o activar la descarga automática."`
     - **NO:** `"Under Elev og fremgang ser du aktiv elev-ID, kan klikke «Eksporter data» når som helst for å lagre rapporter på skrivebordet, eller slå på automatisk lagring."`
   - `teacher_tut_classroom`:
     - **EN / Base:** `"Teaching a whole room? Type your Class Name here and click 'Open Teacher Dashboard' to start a local network class where student PCs can join from their menu!"`
     - **ES:** `"¿Das clase a un grupo? Escribe aquí el Nombre de la Clase y pulsa «Abrir Panel del Profesor» para iniciar una sesión en red local a la que los alumnos se unan desde su menú."`
     - **NO:** `"Underviser du en hel klasse? Skriv inn klassenavnet her og klikk «Åpne lærerens kontrollpanel» for å starte en økt på локалnettet der elevene kan bli med fra menyen sin!"`
   - `teacher_tut_pin_and_lock`:
     - **EN / Base:** `"Below, you can change your 4-digit Teacher PIN anytime. When you're ready to hand the computer to a student, click 'Lock & Student Mode' at the bottom!"`
     - **ES:** `"Más abajo puedes cambiar tu PIN de 4 dígitos cuando quieras. Y cuando vayas a dejar el ordenador al alumno, ¡pulsa «Bloquear / Modo alumno» abajo del todo!"`
     - **NO:** `"Nederst kan du når som helst endre din 4-sifrede lærer-PIN. Når PC-en er klar for en elev, klikker du «Lås / Tilbake til elevmodus» helt nederst!"`

3. **Diálogos del Tutorial del Panel de Aula (`Classroom Dashboard` — 4 pasos, primera vez que se abre el Dashboard):**
   - `teacher_tut_dash_server`:
     - **EN / Base:** `"Welcome to the Classroom Dashboard! The LAN Class Server starts automatically so students on the same network can click 'Join Class' on their PCs. You can also stop/start it or clear offline students here."`
     - **ES:** `"¡Bienvenido al Panel del Profesor! El servidor de clase en red local se inicia automáticamente para que los alumnos pulsen «Unirse a clase» en sus PCs. Desde aquí arriba puedes iniciarlo, detenerlo o limpiar desconectados."`
     - **NO:** `"Velkommen til lærerens kontrollpanel! Klasseserveren på lokalnettet starter automatisk slik at elevene kan klikke «Bli med i klasse» på sine PC-er. Her oppe kan du også stoppe/starte den eller fjerne frakoblede elever."`
   - `teacher_tut_dash_batch`:
     - **EN / Base:** `"Use this top bar to set a Class Default learning path and assessment mode. Click 'Apply to All' to configure every connected student at once, or 'Launch All' to start everyone's game simultaneously!"`
     - **ES:** `"Usa esta barra para elegir la ruta y evaluación para toda la clase. Pulsa «Aplicar a todos» para configurar a todos los alumnos a la vez, o «Iniciar todos» para arrancar sus partidas simultáneamente."`
     - **NO:** `"Bruk denne raden til å velge standard læringssti og kartlegging for hele klassen. Klikk «Bruk på alle» for å oppdatere alle tilkoblede elever, eller «Start alle» for å starte spillet hos alle samtidig!"`
   - `teacher_tut_dash_permissions`:
     - **EN / Base:** `"In this yellow bar, you can rename your class live, lock student names so kids can't change their ID, or lock 'Leave Class' so they stay connected during the session."`
     - **ES:** `"En esta barra amarilla puedes cambiar el nombre de la clase en vivo, bloquear el cambio de nombre de los alumnos o bloquear el botón «Salir de clase» para que no se desconecten durante la sesión."`
     - **NO:** `"I dette gule feltet kan du endre klassenavn direkte, låse elevnavn så elevene ikke endrer dem, eller låse «Forlat klasse» slik at de forblir tilkoblet gjennom økta."`
   - `teacher_tut_dash_students_inspector`:
     - **EN / Base:** `"Connected students appear on the left with individual Start, Reset, and Path controls. Click on any student row to inspect their live level progress, medals, and all 9 skill mastery bars on the right!"`
     - **ES:** `"Los alumnos conectados aparecen a la izquierda con controles individuales de Inicio, Reinicio y Ruta. ¡Haz clic en cualquier alumno para inspeccionar a la derecha su progreso en vivo, medallas y sus 9 barras de habilidades!"`
     - **NO:** `"Tilkoblede elever vises til venstre med egne knapper for Start, Nullstill og læringssti. Klikk på en elev for å se sanntidsfremgang, medaljer og alle 9 ferdighetsmålere til høyre!"`

---

## Sección 3: Controlador del Tutorial de Pointy para Teacher Mode (`TeacherModeTutorial.cs`) e Integración con `TeacherModeGate` y `TeacherDashboardController`

### Diagnóstico de la causa raíz
No existe actualmente ningún componente que coordine a `VirtualMascot` y `TutorialBlocker` cuando se abre `TeacherControlWindow` (`TeacherModeGate.OpenTeacherPanel()`) o `DashboardWindow` (`TeacherDashboardController.Open()`).

### Archivos a crear / modificar
- **Crear:** `ComputerLearner/Assets/Scripts/UI/TeacherModeTutorial.cs`
- **Modificar:** `ComputerLearner/Assets/Scripts/UI/TeacherModeGate.cs`
- **Modificar:** `ComputerLearner/Assets/Scripts/UI/TeacherDashboardController.cs`
- **Modificar:** `ComputerLearner/Assets/Scripts/Managers/SceneSystem.cs`

### Especificación Técnica
1. **Crear `TeacherModeTutorial.cs` (`namespace ComputerLearning`):**
   - **Claves de `PlayerPrefs`:**
     - `public const string ControlPanelTourPrefKey = "TeacherMode_PointyTutorialCompleted_v1";`
     - `public const string DashboardTourPrefKey = "TeacherDashboard_PointyTutorialCompleted_v1";`
   - **Estado estático:**
     - `public static TeacherModeTutorial Instance { get; private set; }`
     - `public static bool IsTourRunning { get; private set; }`
   - **Bloqueo de clicks errantes y avance fluido (`TutorialBlocker` + `Space` / Clic):**
     - Durante el tour (`IsTourRunning == true`), activar `TutorialBlocker` (`SetAllowedTarget(null)`) para evitar que el usuario pulse botones por accidente mientras Pointy señala cada sección.
     - En `VirtualMascot.WaitForDialogue(duration)`, el profesor puede pulsar **`Space`** o hacer **clic izquierdo** en cualquier momento para pasar inmediatamente al siguiente consejo de Pointy, o esperar la lectura automática (~6.5s por paso).
     - Si el profesor pulsa **`Escape`** mientras `IsTourRunning == true`, detener inmediatamente el tour (`StopActiveTour(markCompleted: true)`), ocultar a Pointy (`VirtualMascot.HideMascot()`) y liberar `TutorialBlocker` sin cerrar bruscamente la ventana del Teacher Mode.
     - Si la ventana (`TeacherControlWindow` o `DashboardWindow`) se cierra o desactiva externamente, `StopActiveTour()` limpia automáticamente `TutorialBlocker` y oculta a Pointy.
   - **Corrutina 1 — `RunControlPanelTourCoroutine()` (7 pasos):**
     - Se lanza automáticamente al final de `TeacherModeGate.OpenTeacherPanel()` si `PlayerPrefs.GetInt(ControlPanelTourPrefKey, 0) == 0` (o manualmente al pulsar el botón `? Tutorial` con `forceReplay: true`).
     - Espera 1 frame (`yield return null; Canvas.ForceUpdateCanvases();`) para que los `RectTransform` de `TeacherControlWindow` estén activos y posicionados.
     - Recorre los 7 pasos apuntando con `VirtualMascot.Show(text, targetRect, offset)` a las referencias reales de `TeacherControlWindow`:
       1. `teacher_tut_welcome` -> `teacherPanelTitleText` (o cabecera de `TeacherControlWindow`)
       2. `teacher_tut_paths` -> `Btn-Path1` (`LearningPathSelectorWidget` botones de ruta)
       3. `teacher_tut_assessment` -> `Btn-ToggleAssessment` (`assessmentToggleButton`)
       4. `teacher_tut_reset` -> `Btn-ResetProgress` (`resetProgressButton`)
       5. `teacher_tut_export` -> `Btn-ExportPlaytest` (`exportButton` / `SectionStudentToolsLabel`)
       6. `teacher_tut_classroom` -> `Btn-OpenTeacherDashboard` (`openTeacherDashboardButton`)
       7. `teacher_tut_pin_and_lock` -> `Btn-LockTeacherMode` (`lockTeacherModeButton`)
     - Al terminar el paso 7, guarda `PlayerPrefs.SetInt(ControlPanelTourPrefKey, 1); PlayerPrefs.Save();`, limpia `TutorialBlocker` y oculta a Pointy (`VirtualMascot.HideMascot()`).
   - **Corrutina 2 — `RunDashboardTourCoroutine()` (4 pasos):**
     - Se lanza automáticamente al abrir `TeacherDashboardController.Open()` por primera vez si `PlayerPrefs.GetInt(DashboardTourPrefKey, 0) == 0` (o manualmente al pulsar el botón `? Tutorial` en la barra superior del Dashboard).
     - Si el tour de la ventana principal aún estaba corriendo al abrir el Dashboard, lo detiene limpiamente y marca `ControlPanelTourPrefKey = 1`.
     - Recorre los 4 pasos sobre `DashboardWindow`:
       1. `teacher_tut_dash_server` -> `toggleServerButton` / `serverStatusText`
       2. `teacher_tut_dash_batch` -> `applyAllButton` / `BatchControlsBar`
       3. `teacher_tut_dash_permissions` -> `lockStudentNamesButton` / `PermissionsAndNameBar`
       4. `teacher_tut_dash_students_inspector` -> `studentsHeaderText` / `InspectorCard`
     - Al terminar, guarda `PlayerPrefs.SetInt(DashboardTourPrefKey, 1); PlayerPrefs.Save();`, limpia `TutorialBlocker` y oculta a Pointy.
   - **Actualización dinámica de idioma en pleno tutorial:**
     - Suscribirse a `LocalizationManager.OnLanguageChanged` para que, si se cambia el idioma, el mensaje activo de Pointy se actualice al instante.

2. **Botón `? Tutorial` en `TeacherControlWindow` (`TeacherModeGate.cs`) y en `DashboardWindow` (`TeacherDashboardController.cs`):**
   - En `TeacherModeGate.cs` (y en `RetroSceneMaterializer.cs` para consistencia de editor):
     - En `Awake()` / `OpenTeacherPanel()`, asegurar un botón compacto `Btn-TeacherTutorialHelp` (`"? Tutorial"`, `128 × 38` px, estilo `RetroUITheme.BtnSkyBlue`) en la barra de título `WindowTitleBar` de `TeacherControlWindow` a la izquierda del botón de cierre `[X]` (`Btn-CloseTeacherPanel`).
     - Al hacer clic en `Btn-TeacherTutorialHelp`, llamar a `TeacherModeTutorial.StartControlPanelTour(this, forceReplay: true)`.
     - En `CloseTeacherPanel()` y `LockTeacherMode()`, llamar a `TeacherModeTutorial.StopIfRunning()`.
     - En `TeacherModeGate.Update()`, si `Keyboard.current.escapeKey.wasPressedThisFrame` y `TeacherModeTutorial.IsTourRunning` es `true`, dejar que `TeacherModeTutorial` consuma `Escape` para saltar/cerrar solo el tutorial sin cerrar `TeacherControlWindow`.
   - En `TeacherDashboardController.cs`:
     - En `Awake()` / `Open()`, asegurar un botón compacto `Btn-DashboardTutorialHelp` (`"? Tutorial"`, `124 × 38` px, estilo `RetroUITheme.BtnSkyBlue`) en `WindowTitleBar` de `DashboardWindow` a la izquierda de `Btn-ClearOffline` (ajustando el margen derecho de `ServerStatusText` para que no se solapen).
     - Al abrir `Open()`, invocar `TeacherModeTutorial.TryStartDashboardTourOnFirstOpen(this)`.
     - Al hacer clic en `Btn-DashboardTutorialHelp`, invocar `TeacherModeTutorial.StartDashboardTour(this, forceReplay: true)`.
     - En `Close()`, invocar `TeacherModeTutorial.StopIfRunning()`.
     - En `TeacherDashboardController.Update()`, si se pulsa `Escape` mientras `TeacherModeTutorial.IsTourRunning` es `true`, detener el tutorial sin cerrar el Dashboard.

3. **Preservar Flags del Profesor en `SceneSystem.ResetProgress()`:**
   - En `SceneSystem.ResetProgress()`:
     - Antes de `PlayerPrefs.DeleteAll()`, leer:
       - `string customTeacherPin = PlayerPrefs.GetString("TeacherMode_CustomPin_v1", string.Empty);`
       - `int teacherTutDone = PlayerPrefs.GetInt(TeacherModeTutorial.ControlPanelTourPrefKey, 0);`
       - `int dashTutDone = PlayerPrefs.GetInt(TeacherModeTutorial.DashboardTourPrefKey, 0);`
       - `string customClassName = PlayerPrefs.GetString("TeacherCustomClassName", string.Empty);`
       - `int lockNames = PlayerPrefs.GetInt("TeacherLockStudentNames", 0);`
       - `int lockLeave = PlayerPrefs.GetInt("TeacherLockLeaveClass", 0);`
     - Después de `PlayerPrefs.DeleteAll()`, restaurar esos ajustes de profesor para que pulsar `"Reset Progress"` entre un alumno y otro solo reinicie los datos del alumno y no borre ni el PIN del profesor, ni el nombre de la clase, ni vuelva a saltar el tutorial del profesor en cada reinicio.

### Criterios de aceptación
- La primera vez que el profesor introduce el PIN (`1234`) y abre la `Teacher Control Window`, aparece **Pointy** automáticamente y explica paso a paso cada sección del Teacher Mode en el idioma seleccionado (`EN`, `ES` o `NO`).
- La primera vez que el profesor abre el `Classroom Dashboard`, aparece **Pointy** automáticamente y explica en 4 pasos el servidor LAN, los controles en bloque para toda la clase, los permisos y la lista/inspector de telemetría en vivo.
- El profesor puede avanzar entre los pasos de Pointy pulsando `ESPACIO` o haciendo clic en el bocadillo, cancelar el tutorial con `Escape`, o volver a lanzarlo en cualquier momento pulsando el botón **`? Tutorial`** en la barra de título tanto de la ventana de Teacher Mode como del Classroom Dashboard.
- Pulsar `Reset Progress` para preparar el siguiente alumno (`S01 -> S02`) conserva el estado de completado del tutorial del profesor (y su PIN/nombre de clase).

---

## Sección 4: Verificación Integral y Documentación

1. Compilar `ComputerLearner/Assembly-CSharp.csproj` verificando **0 errores de compilación**.
2. Validar la sintaxis de todos los archivos JSON en `ComputerLearner/Assets/Resources/` (`UIDialogs.json`, `EN-UIDialogs.json`, `ES-UIDialogs.json`, `NO-UIDialogs.json`).
3. Actualizar `Documentation/Specs/Diego.md` añadiendo una nueva sección numerada que documente la arquitectura del tutorial de Pointy en el Teacher Mode y Classroom Dashboard.
