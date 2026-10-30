# Plan de Correcciones e Implementaciones (Fase 2) — Computer Learning (Unity 6)

> **Documento de especificación técnica para ejecución secuencial con `/goal`.**
> Este plan abarca la corrección de errores visuales de UI, pulido de la barra de progreso, rediseño completo de la aplicación de logros, erradicación de fugas de objetos al cambiar de escena, optimización de la mascota virtual, eliminación de pasos redundantes en tutoriales, corrección del doble pegado en Notepad, salvaguardas antifallos en niveles de texto y renovación didáctica de los mensajes de feedback.

---

## Reglas Generales de Arquitectura y Contexto

1. **Preservar la arquitectura actual**: Respetar el namespace `ComputerLearning`, componentes modulares de Unity, prefabs y uGUI / Canvas (`RectTransform`).
2. **Input System**: El proyecto utiliza el **New Input System** (`activeInputHandler: 1`). Proteger cualquier referencia a legacy input bajo `#if ENABLE_LEGACY_INPUT_MANAGER` y usar `UnityEngine.InputSystem.Keyboard.current` y `Mouse.current`.
3. **Validación continua**: Tras completar cada sección, validar los scripts modificados mediante `Unity_ValidateScript` y verificar en `Unity_ReadConsole` que la consola de Unity permanece limpia con 0 errores de compilación y 0 excepciones.
4. **Actualización de Documentación**: Al concluir la última sección, actualizar `Documentation/Specs/Diego.md` detallando las decisiones arquitectónicas y técnicas implementadas.

---

## Sección 1: Formato del modal de celebración y eliminación de caracteres incompatibles (Imagen 1)

### Diagnóstico de la causa raíz
1. **Desbordamiento del botón**: En `LevelCelebrationModal.cs`, la tarjeta (`cardRt.sizeDelta = new Vector2(460f, 380f);`) no tiene suficiente altura para alojar el padding superior e inferior (40px), título (30px), subtítulo (22px), medalla heroica (150px), fila de estadísticas (26px), botón de continuar (44px) y todos los espaciados intermedios del `VerticalLayoutGroup`. Como resultado, el botón `BtnContinue` sobresale por la parte inferior de la tarjeta blanca.
2. **Caracteres raros / cuadrados (`□`)**: El font asset `LiberationSans SDF` utilizado por TextMeshPro no incluye el glifo Unicode de estrella negra `\u2605` (`★`). En `LevelCelebrationModal.cs` (líneas 125 y 179–191), el código asigna `"<b>★ LEVEL COMPLETED! ★</b>"` y `"★ GOLD MEDAL ★"`, lo que provoca que Unity genere warnings de fallback y dibuje un cuadrado `□` a ambos lados del texto.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/UI/LevelCelebrationModal.cs`
- `ComputerLearner/Assets/Scripts/UI/MedalVisualUtility.cs` (si contiene caracteres unicode similares)

### Pasos de implementación
1. En **`LevelCelebrationModal.cs`**:
   - Incrementar las dimensiones de la tarjeta: cambiar `cardRt.sizeDelta` a `new Vector2(480f, 440f)`.
   - Ajustar el padding del `VerticalLayoutGroup` a `new RectOffset(24, 24, 22, 22)` y el espaciado a `10f`.
   - Asignar explícitamente `sizeDelta = new Vector2(240f, 46f)` al `RectTransform` del botón `BtnContinue` además del `LayoutElement` para asegurar que el motor de layout lo mantenga centrado y perfectamente delimitado dentro de la tarjeta blanca.
   - Eliminar el caracter `★` de todos los textos. Reemplazar:
     - `title.text = "<b>LEVEL COMPLETED!</b>";`
     - `medalBadgeText.text = "GOLD MEDAL";` (y correspondientes para Silver, Bronze y Completed).
   - Revisar que ningún otro texto del modal o utilidades asociadas use caracteres fuera del rango estándar ASCII que generen sustituciones a `□`.

### Criterios de aceptación
- El modal de celebración contiene el botón de continuar completamente dentro de su fondo blanco con márgenes limpios.
- Desaparecen por completo los caracteres `□` y las advertencias de Unicode en la consola de Unity.

---

## Sección 2: Ajuste de la barra de progreso de niveles (Imagen 2)

### Diagnóstico de la causa raíz
En `LevelRunner.cs` (Sección 6 previa), la barra de progreso discreta se configuró con una altura muy delgada de `8f` y un texto inferior `ProgressLabel` (`progressLabelText`) que muestra valores como `6 / 6`. El usuario solicitó eliminar los números y hacer la barra ligeramente más grande y visible.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/Managers/LevelRunner.cs`

### Pasos de implementación
1. En **`LevelRunner.cs`**:
   - Eliminar la creación y actualización del GameObject `ProgressLabel` y su componente `Text progressLabelText`.
   - Aumentar la altura del contenedor `progressBarContainer`: cambiar `containerRect.sizeDelta` de `new Vector2(0f, 8f)` a `new Vector2(0f, 14f)`.
   - Ajustar el margen superior a `new Vector2(0f, -14f)` y los anchors horizontales a `anchorMin = new Vector2(0.28f, 1f)` y `anchorMax = new Vector2(0.72f, 1f)`.
   - Dar un acabado visual más pulido al track de fondo y al fill de progreso, manteniendo `raycastTarget = false` en ambas imágenes.
   - Actualizar `RefreshProgressBar()` para que únicamente calcule e interpole suavemente `targetProgress` hacia `displayedProgress` sin manipular textos.

### Criterios de aceptación
- La barra de progreso es un poco más grande (14px de grosor), moderna y visualmente atractiva en la parte superior.
- Ya no se muestran números (ej. `6 / 6`) debajo de la barra.

---

## Sección 3: Rediseño completo y estético de `ScoresApp` (Imagen 3)

### Diagnóstico de la causa raíz
En `ScoresApp.cs`, la interfaz actual presenta:
1. Una barra de herramientas con dos botones de bloque gigantes ("Refresh Records" azul y "Reset Records" rojo) que se estiran de forma desproporcionada.
2. Un gran espacio vacío gris en el centro sin estructura visual adecuada.
3. Filas de tabla comprimidas y con tipografía pequeña que dificultan la navegación y lectura.
4. Falta de navegación por categorías o tarjetas limpias para los 16 niveles.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/DesktopSystem/ScoresApp.cs`

### Pasos de implementación
1. En **`ScoresApp.cs`**:
   - **Eliminar los botones de reinicio y refresco**: Suprimir la barra de herramientas (`Toolbar`) con el botón rojo "Reset Records" y el botón azul "Refresh Records". Los datos ya se refrescan automáticamente mediante `ProgressData.Instance.OnProgressChanged`.
   - **Cabecera destacada estilo tarjeta**:
     - Diseñar un encabezado superior elegante con tarjeta de fondo blanco/marfil suave (`#F8F9FA`) y bordes limpios.
     - Mostrar un panel resumen con contadores visuales: medallas de Oro, Plata y Bronce obtenidas acompañadas de sus iconos o insignias con colores vivos, porcentaje de precisión media y progreso total de niveles (ej. `11/16 Completados`).
   - **Área de lista moderna y navegable**:
     - Configurar un `ScrollRect` con scrollbar vertical visible y agradable al ratón.
     - Diseñar cada fila de nivel como una "tarjeta de nivel" individual con separación de 8px, fondo blanco suave, efecto hover sutil y esquinas redondeadas simuladas.
     - Cada fila debe incluir:
       - Nombre del nivel en negrita y legible (ej. `Level 1: Move`, `Level 10: Context Menu`).
       - Insignia o icono de medalla de alta calidad con el nombre de la medalla (`Gold`, `Silver`, `Bronze` o `No Medal`).
       - Métricas alineadas claramente: Mejor Precisión (`100%`), Mejor Tiempo (`00:08s`) y Número de Intentos.
       - Estado de completado con badge de color verde suave (`Completed`) o gris (`Locked/Unplayed`).
   - Asegurar que la ventana sea 100% responsiva tanto en tamaño estándar como maximizada.

### Criterios de aceptación
- Desaparecen los botones toscos de reinicio.
- La ventana de puntuaciones tiene un diseño limpio, navegable, con tipografía nítida y badges de medallas atractivos.

---

## Sección 4: Corrección de error de Unity al cambiar de escena ("Some objects were not cleaned up") (Imagen 4)

### Diagnóstico de la causa raíz
En el registro de Unity (`11:08:58`), el error:
`Some objects were not cleaned up when closing the scene. (Did you spawn new GameObjects from OnDestroy?) The following scene GameObjects were found: ProgressData`
ocurre porque `DesktopManager.OnDestroy()` ejecuta:
```csharp
if (ProgressData.Instance != null)
{
    ProgressData.Instance.OnLevelCompleted -= HandleLevelCompletedForAdvancedTour;
}
```
En Unity, cuando una escena se descarga, los GameObjects se destruyen secuencialmente. Si `ProgressData` ya fue destruido, el getter `ProgressData.Instance`:
```csharp
if (_instance == null)
{
    GameObject go = new GameObject("ProgressData");
    _instance = go.AddComponent<ProgressData>();
}
```
**instancia un nuevo GameObject dentro de `OnDestroy()` mientras la escena se está cerrando**, lo que Unity detecta inmediatamente como una fuga de escena ilegal.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/Managers/ProgressData.cs`
- `ComputerLearner/Assets/Scripts/DesktopSystem/DesktopManager.cs`
- `ComputerLearner/Assets/Scripts/DesktopSystem/ScoresApp.cs`
- Revisión general de singletons en `ComputerLearner/Assets/Scripts/`

### Pasos de implementación
1. En **`ProgressData.cs`**:
   - Añadir una propiedad pública estática `public static bool HasInstance => _instance != null;`.
   - Añadir un flag de cierre de aplicación o escena: `private static bool isShuttingDown = false;` activado en `OnApplicationQuit()`.
   - Modificar el getter `ProgressData.Instance`: si `_instance == null` y la aplicación está cerrando o no se encuentra una instancia existente, **no instanciar automáticamente** un nuevo GameObject a ciegas si se consulta desde código de limpieza.
2. En **`DesktopManager.cs`**:
   - En `OnDestroy()`, cambiar `if (ProgressData.Instance != null)` por `if (ProgressData.HasInstance)`.
3. En **`ScoresApp.cs`** y demás scripts que se desuscriben en `OnDestroy()`:
   - Utilizar `if (ProgressData.HasInstance)` para evitar invocar el getter creador en momentos de destrucción.

### Criterios de aceptación
- Al pasar de `Desktop1` a `VirtualDesktop`, al volver al menú principal o al reiniciar el juego, la consola de Unity reporta 0 warnings de "Some objects were not cleaned up".

---

## Sección 5: Ocultar la mascota tras dar feedback en VirtualDesktop durante niveles

### Diagnóstico de la causa raíz
En `VirtualMascot.cs`:
```csharp
public void Hide()
{
    if (DesktopManager.Instance != null && DesktopManager.Instance.EnableMascotCompanion)
    {
        InitAsDesktopCompanion();
        return;
    }
    if (isDesktopCompanion) return;
    gameObject.SetActive(false);
    targetRect = null;
}
```
Cuando un nivel se juega en `VirtualDesktop`, `DesktopManager.Instance.EnableMascotCompanion` es `true`. Cuando el feedback temporal del nivel termina (`AutoHideFeedback`), se invoca `Hide()`, pero en vez de desactivar el GameObject, `Hide()` ejecuta `InitAsDesktopCompanion()`. Esto teletransporta la mascota a la esquina superior derecha (`companionCornerOffset`) en pleno nivel, lanzando el mensaje de bienvenida y provocando movimientos bruscos y superposición sobre el nivel activo.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/DesktopSystem/VirtualMascot.cs`

### Pasos de implementación
1. En **`VirtualMascot.cs`**:
   - Añadir un helper estático que detecte si hay un nivel en curso:
     ```csharp
     public static bool IsAnyLevelActive()
     {
         if (VirtualDesktopGuidedLevels.IsLevelRunning) return true;
         LevelRunner runner = FindAnyObjectByType<LevelRunner>();
         if (runner != null && runner.currentLevel != null && !runner.IsCompleted) return true;
         return false;
     }
     ```
   - Modificar `Hide()`:
     - Si `IsAnyLevelActive()` es verdadero, o si el feedback provino de una acción dentro de un nivel/ventana, **desactivar inmediatamente la mascota** (`gameObject.SetActive(false)`) y resetear `targetRect = null`.
     - Nunca restaurar el modo compañero (`InitAsDesktopCompanion()`) mientras haya un nivel en ejecución.
   - En `ShowTemporaryFeedback` y `AutoHideFeedback`, garantizar que al expirar el temporizador la mascota se oculte limpiamente por completo.

### Criterios de aceptación
- Durante la ejecución de cualquier nivel en `VirtualDesktop`, la mascota aparece solo para dar el feedback contextual de error/guía y, al terminar los segundos de visualización, desaparece por completo sin volar a la esquina ni quedarse en modo compañero.

---

## Sección 6: Eliminar pasos redundantes de minimizar y maximizar en `AdvancedDesktopTour`

### Diagnóstico de la causa raíz
En `AdvancedDesktopTour.cs`, las secciones 6 (*"Maximize and Restore"*, líneas 208–230) y 7 (*"Minimize and Restore from Taskbar"*, líneas 232–260) enseñan a maximizar y minimizar ventanas. Sin embargo, estas mismas acciones ya se enseñan paso a paso en el primer tutorial del escritorio virtual (`DesktopTour.cs`, Pasos 7, 9 y 10). Su presencia en `AdvancedDesktopTour` es redundante y alarga innecesariamente la introducción al Nivel 10.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/DesktopSystem/AdvancedDesktopTour.cs`

### Pasos de implementación
1. En **`AdvancedDesktopTour.cs`**:
   - Eliminar el bloque de la sección 6 (Maximize and Restore).
   - Eliminar el bloque de la sección 7 (Minimize and Restore from Taskbar).
   - Hacer que la secuencia pase fluidamente del paso 5 (*"Resize Window"*) directamente al paso 8 (*"Close Window"* y *"Recycle Bin"*).
   - Ajustar los números y textos de los diálogos de la mascota para que la transición sea natural y coherente.

### Criterios de aceptación
- `AdvancedDesktopTour` no repite los pasos de maximizar ni minimizar, resultando en un tutorial ágil centrado en mover, redimensionar y cerrar/reciclar.

---

## Sección 7: Eliminar el doble pegado (Ctrl+C / Ctrl+V) en Notepad

### Diagnóstico de la causa raíz
En `NotepadApp.cs`, el componente de entrada de texto es un `TMP_InputField`. TextMeshPro incluye soporte nativo integrado para atajos del portapapeles (`Ctrl+C`, `Ctrl+X`, `Ctrl+V`, `Ctrl+A`).
En `NotepadApp.Update()`:
```csharp
else if (isV)
{
    HandlePasteShortcut();
}
```
Cuando el usuario pulsa `Ctrl+V`, `TMP_InputField` inserta nativamente el texto del portapapeles en su ciclo de eventos de teclado. A la vez, `HandlePasteShortcut()` en `Update()` vuelve a insertar manualmente el texto en `inputField.text` mediante `inputField.text.Insert(...)`. Como resultado, la palabra copiada se pega **dos veces consecutivas**.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/DesktopSystem/NotepadApp.cs`

### Pasos de implementación
1. En **`NotepadApp.cs`**:
   - Sincronizar siempre `GUIUtility.systemCopyBuffer` con `VirtualClipboard` en `HandleCopyShortcut()` y `HandleCutShortcut()`.
   - En `HandlePasteShortcut()`:
     - Si `inputField != null && inputField.isFocused`: verificar si `TMP_InputField` ya maneja el evento de teclado nativo o evitar la doble inserción manual en `inputField.text`. Una solución limpia consiste en actualizar la variable de undo, disparar `OnTextPasted?.Invoke(...)`, reproducir el sonido de éxito `FeedbackManager.PlaySuccess()` y mostrar el mensaje en la barra de estado sin volver a concatenar `toPaste` a la fuerza si el texto ya cambió, o consumiendo la pulsación para que solo se realice una vez.
     - Si se pulsa el botón de la interfaz "Paste" (cuando el campo no tiene el foco de teclas), ejecutar la inserción manual en la posición del cursor.

### Criterios de aceptación
- Al copiar una palabra (ej. "Cat") y pulsar `Ctrl + V` en Notepad, la palabra se pega exactamente **una sola vez**.

---

## Sección 8: Salvaguardas antifallos y pulido de niveles de texto (Niveles 11 al 15)

### Diagnóstico de la causa raíz
En `VirtualDesktopGuidedLevels.cs`:
1. **Pérdida de texto esencial**: En el Nivel 12 (Copy, Paste & Cut), si el usuario borra accidentalmente el texto ("Cat Dog") antes de copiar "Cat" o de cortar "Dog", el bucle `while (!copied)` o `while (!cut)` queda esperando que el usuario copie una palabra que ya no existe en pantalla. El jugador queda atrapado indefinidamente.
2. **Nivel 13 (Undo)**: Si el jugador borra el texto con Backspace en lugar de pulsar `Ctrl + Z`, no puede deshacer y queda bloqueado.
3. **Nivel 15 (Evaluación de Notepad)**: Mismo riesgo al alternar escritura, corte, pegado y deshacer.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/DesktopSystem/VirtualDesktopGuidedLevels.cs`

### Pasos de implementación
1. En **`VirtualDesktopGuidedLevels.cs`**:
   - **Salvaguarda de recuperación en Nivel 12**:
     - Durante el paso de copiar: si `string.IsNullOrWhiteSpace(notepad.CurrentText)` o si `!notepad.CurrentText.Contains("Cat")`, restaurar automáticamente `notepad.SetText("Cat Dog");` y mostrar a la mascota: *"¡Vaya! Necesitamos ese texto. ¡Te lo he vuelto a colocar! Selecciona 'Cat' y pulsa Ctrl + C."*.
     - Durante el paso de cortar: si el usuario borra todo el texto sin usar `Ctrl + X`, restaurar `"Cat Dog Cat"` y guiarle amablemente.
   - **Salvaguarda en Nivel 13 (Undo)**:
     - Si el texto queda completamente vacío o si el historial de deshacer se corrompe por tecleo errático, reinyectar el estado de error previsto (`"Computer Science is OOPS!"` con estado anterior en el stack de undo) para que `Ctrl + Z` siempre tenga efecto.
   - **Salvaguarda en Nivel 15**:
     - Añadir verificaciones similares en cada uno de los 5 pasos de la evaluación para asegurar que cualquier acción destructiva sea reconducida didácticamente sin frustración.

### Criterios de aceptación
- Es imposible quedarse atascado en los niveles 11 a 15, incluso si el jugador borra todo el texto, pulsa teclas inesperadas o cierra la ventana.

---

## Sección 9: Feedback enriquecido y contextual en niveles de ratón (Click and Hold y tareas básicas)

### Diagnóstico de la causa raíz
En `ClickHoldTask.cs`, `RightClickTask.cs`, `DoubleClickTask.cs` y otras tareas, varios mensajes son genéricos o poco didácticos (ej. `"Press and hold the left mouse button on the circle!"` o mensajes técnicos sin variedad). No se diferencian situaciones como soltar cuando faltaba un 5% para completar la barra, hacer clicks rápidos sucesivos ("tapping") en lugar de mantener pulsado, o pulsar el botón secundario.

### Archivos a modificar
- `ComputerLearner/Assets/Scripts/Tasks/ClickHoldTask.cs`
- `ComputerLearner/Assets/Scripts/Tasks/RightClickTask.cs`
- `ComputerLearner/Assets/Scripts/Tasks/DoubleClickTask.cs`
- `ComputerLearner/Assets/Scripts/Tasks/HoverTargetTask.cs`

### Pasos de implementación
1. En **`ClickHoldTask.cs`**:
   - Detectar clicks rápidos repetidos (tapping): si el usuario hace varios clicks con `holdTime < 0.25f`, la mascota debe animar: *"¡No lo sueltes tan rápido! Mantén el botón izquierdo apretado sin levantarlo."*.
   - Detectar casi-acierto: si el usuario suelta cuando `currentHoldTime >= requiredHoldTime * 0.75f`: *"¡Caaaasi! ¡Aguanta solo un segundito más hasta que se llene el círculo!"*.
   - Detectar salida del área: *"¡Ups! No saques el ratón del círculo mientras mantienes presionado."*.
   - Detectar click derecho: *"¡Botón equivocado! Usa el botón IZQUIERDO del ratón para mantener."*.
2. En **`RightClickTask.cs`** y **`DoubleClickTask.cs`**:
   - Pulir los mensajes de inactividad y error con el mismo tono alegre, comprensivo y motivador de los niveles 1 al 5.

### Criterios de aceptación
- Las tareas de habilidad de ratón ofrecen feedback dinámico, específico según la acción del jugador, con un tono pedagógico y divertido.

---

## Sección 10: Verificación integral, compilación limpia y actualización de `Diego.md`

### Pasos de implementación
1. Validar todos los scripts tocados con `Unity_ValidateScript`.
2. Ejecutar `Unity_ReadConsole` comprobando 0 errores de compilación y 0 excepciones.
3. Actualizar `Documentation/Specs/Diego.md` documentando la Fase 2 completa.

---
