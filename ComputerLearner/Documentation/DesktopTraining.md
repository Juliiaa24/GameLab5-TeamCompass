# Escritorio y entrenamiento de click

La integraciÃ³n usa las clases del proyecto y las decisiones de Erasmus: Game-Lab: Canvas/uGUI, Input System, una ventana por icono y contenido cargado mediante Window.SetContent.

## Flujo de ventanas

DraggableIcon.OpenApplication â†’ WindowManager.OpenWindow â†’ Window â†’ Taskbar.

- DraggableIcon conserva la referencia a su instancia. Abrir el mismo icono restaura esa ventana.
- Window conserva maximizaciÃ³n, geometrÃ­a y visibilidad. WindowManager mantiene la lista y la ventana activa.
- Taskbar observa WindowsChanged y cada botÃ³n apunta a la Window real.
- Click en una entrada activa: minimizar. En una entrada minimizada: restaurar y enfocar. En una entrada de fondo: enfocar.
- Cerrar o destruir una Window elimina su entrada. El foco pasa a la ventana visible superior.
- El atajo de InputManager sigue pudiendo crear varias instancias; la taskbar representa cada una.
- WindowsArea excluye los 64 puntos del Canvas ocupados por la taskbar. Los botones tienen desplazamiento horizontal si no caben.
- WindowFocusRelay transmite el foco desde controles que consumen eventos de uGUI; el ejercicio no necesita conocer Window.

## Flujo de aprendizaje

TrainingLevel (deriva de Level) contiene TargetTask (deriva de Task).

Al abrir la aplicaciÃ³n, TrainingLevel pide a LevelManager iniciar su nivel. Este usa TaskManager para comenzar las Tasks configuradas. ClickTargetExercise presenta targets y cada ClickTarget acepta un click izquierdo. TargetTask acumula progreso y emite un Ãºnico resultado al completar el objetivo. TaskManager entrega ese resultado a SkillManager y despuÃ©s avisa a LevelManager, que comprueba las Tasks del Level.

La configuraciÃ³n inicial contiene LEVEL1, una TargetTask de cinco targets y la habilidad CLICK. El nÃºmero cinco es un valor de Inspector, no una condiciÃ³n del cÃ³digo. No habÃ­a reglas de desbloqueo ni niveles posteriores configurados: LevelManager registra el nivel como completado y emite LevelCompleted; no inventa un siguiente nivel.

Minimizar mantiene el progreso. Restart / Play again inicia un nuevo intento. Cerrar y volver a abrir crea un ejercicio nuevo. Cada intento completado suma un resultado a ClickSkill. Skills y niveles completados permanecen durante la sesiÃ³n y los cambios de escena; no se ha aÃ±adido guardado en disco.

## Escenas y prefabs

Las escenas MainScene, DiegoScene y JuliaScene incluyen:
- El Canvas original y sus iconos originales.
- WindowsArea y WindowManager.
- Taskbar.
- ClickTrainingIcon.
- ProgressionManagers, con los tres managers existentes y ClickSkill.

Prefabs nuevos:
- Assets/Prefabs/Desktop/Taskbar.prefab
- Assets/Prefabs/Desktop/TaskbarWindowButton.prefab
- Assets/Prefabs/Desktop/ClickTrainingIcon.prefab
- Assets/Prefabs/Windows/ClickTrainingWindow.prefab
- Assets/Prefabs/Training/ClickTargetExercise.prefab
- Assets/Prefabs/Training/ProgressionManagers.prefab

BaseWindow y WindowTest1/2/3 conservan sus scripts y referencias; WindowContents queda bajo la cabecera y los botones de minimizar estÃ¡n conectados.

## ParÃ¡metros editables

Abrir Assets/Prefabs/Training/ClickTargetExercise.prefab:

- RaÃ­z > TargetTask > Required Targets: cantidad de clicks necesarios (mÃ­nimo 1).
- RaÃ­z > TargetTask > Skills: habilidades que reciben el resultado.
- RaÃ­z > TrainingLevel > Tasks: Tasks requeridas por ese nivel.
- RaÃ­z > TrainingLevel > Level ID / Start On Open: identidad y comienzo automÃ¡tico.
- RaÃ­z > ClickTargetExercise > Target Padding / Next Target Delay: margen y breve bloqueo entre targets para evitar contar dos veces un click.

Abrir Assets/Prefabs/Windows/ClickTrainingWindow.prefab:

- Window > Content Prefab: contenido del ejercicio, consumido por SetContent.
- Window > Window Title / Window Icon: presentaciÃ³n en la taskbar.
- Los ResizeHandle mantienen un mÃ­nimo adecuado para el ejercicio.

## Correcciones previas necesarias

- WindowManager, TaskManager, SkillManager y LevelManager eran esqueletos sin gestiÃ³n funcional.
- Click.cs declaraba TargetTask: se renombrÃ³ el archivo a TargetTask.cs conservando su GUID para que Unity pueda adjuntarlo.
- Maximizar guardaba offsets despuÃ©s de modificar anchors: ahora se guarda toda la geometrÃ­a antes del cambio.
- WindowDrag sumaba pÃ­xeles de pantalla a coordenadas del Canvas: ahora convierte al espacio del padre, como ResizeHandle.
- Los prefabs WindowTest1/2/3 tenÃ­an un botÃ³n de minimizar visual sin su componente.
- Los controles hijos pueden consumir los eventos de foco; WindowFocusRelay los transmite.
- IconGrid no reconstruÃ­a su distribuciÃ³n al cambiar el Ã¡rea y producÃ­a logs de depuraciÃ³n: ahora adapta la cuadrÃ­cula y coloca los iconos sin esos logs.
- InputManager podÃ­a consultar referencias nulas y conservar un Canvas destruido al cambiar de escena; ahora protege el atajo y permanece en su escena.

## ComprobaciÃ³n manual

1. Abrir Assets/Scenes/MainScene.unity y entrar en Play.
2. Doble click en ClickTrainingIcon: debe abrir Click Training y su entrada en la taskbar.
3. Abrir otro icono y alternar sus entradas; minimizar, restaurar y cerrar desde ambas ventanas.
4. Maximizar, minimizar la ventana maximizada y restaurarla: debe seguir maximizada. Restaurar tamaÃ±o y verificar su posiciÃ³n anterior.
5. Arrastrar y redimensionar en 16:9, 4:3 y 21:9.
6. Completar los targets: el texto muestra el nivel completado y el resultado de la habilidad CLICK.
7. Pulsar Play again; minimizar a mitad del intento, restaurar y terminarlo.
8. Cerrar y reabrir el ejercicio; verificar un intento nuevo sin una entrada duplicada.

## Archivos de código

Modificados: Window, WindowManager, WindowDrag, ResizeHandle, Close, Minimize, Maximize, DraggableIcon, IconGrid, InputManager, Task, TaskManager, Skill, SkillManager, Level y LevelManager.

Nuevos: WindowFocusRelay, Taskbar, TaskbarWindowButton, ClickTarget, ClickTargetExercise, ClickSkill y TrainingLevel. TargetTask continúa la clase que antes estaba en Click.cs; su archivo y su meta se renombraron conservando el GUID.

## Validación realizada

Unity 6000.5.9f1 compiló el proyecto y ejecutó 44 comprobaciones de Play Mode en una copia aislada con los mismos scripts, prefabs y escenas entregados. Los clicks pasaron por InputSystem y EventSystem. Se verificaron cambios reales de Game View a 1280×720, 1280×1024 y 2560×1080, el arrastre y redimensionado, las ventanas y taskbar, el ejercicio de cinco targets y su repetición con dos, los resultados de Skill y Level y la persistencia sin duplicar managers al cambiar entre las tres escenas.

- Informe: Validation/DesktopIntegrationValidation.json.
- Capturas: Validation/Desktop.png y Validation/ClickTraining.png. La segunda muestra la repetición parametrizada de dos targets; el prefab conserva cinco por defecto.
- El informe separa una excepción interna del indexador UnityEditor.Search observada en el editor de pruebas. No procedía del código del juego; los 44 controles pasaron sin errores de ejecución del juego.
- La comprobación reproducible está en Assets/Tests/Editor/DesktopIntegrationValidation.cs. Se ejecuta en una copia aislada con Unity en batch mode y el método ComputerLearning.EditorTools.DesktopIntegrationValidation.Run; no se ejecuta automáticamente en el proyecto de trabajo. Sus resultados se escriben en Temp/DesktopIntegrationValidation.

No queda configuración de Inspector pendiente. Si Unity tenía una de las escenas abierta durante la actualización, recárgala desde su archivo antes de probar.
