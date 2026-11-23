# Especificación Técnica: Renovación Visual de Iconos de Dianas (`RightClickTarget`, `GlassTarget` / `GlassTargetDamaged` y `HoldTarget`)

## 1. Contexto y Análisis de los Nuevos Assets

En la carpeta `Assets/Art/UI/Target Icons/` se han incorporado cuatro texturas pixel-art de `740x740` píxeles para reemplazar el antiguo icono genérico teñido por color (`Target_Icon_01.png`, `376x376`, `guid: fdbcf9652edac484992eb3c9911b9ebd`):

| Archivo | GUID | Dimensiones Lienzo | Bounding Box Círculo | Elementos Adicionales | Estado actual en `.meta` |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `GlassTarget.png` | `53c7c3af77b3e2d48bc33f6b4cd9a73f` | `740x740` | `X=[182..557]`, `Y=[182..557]` (`376x376`) | — | `spriteMode: 1` (Single) |
| `GlassTargetDamaged.png` | `ff99b07f12d75b74daa260be7138e002` | `740x740` | `X=[182..557]`, `Y=[182..557]` (`376x376`) | Grietas de cristal roto | `spriteMode: 2` (`sprites: []`) -> **Corregir a `spriteMode: 1`** |
| `RightClickTarget.png` | `1f681aa4d1a600746a4980e06b2f1a51` | `740x740` | `X=[182..557]`, `Y=[182..557]` (`376x376`) | Icono central de ratón con clic derecho azul | `spriteMode: 2` (`sprites: []`) -> **Corregir a `spriteMode: 1`** |
| `HoldTarget.png` | `46f5cef5f61b5854eb34041547728579` | `740x740` | `X=[182..557]`, `Y=[182..557]` (`376x376`) | Texto `"HOLD"` y barra inferior en `X=[255..482]`, `Y_top=[578..622]` | `spriteMode: 2` (`sprites: []`) -> **Corregir a `spriteMode: 1`** |

### Hallazgos Críticos de Geometría e Importación
1. **Sub-Asset Sprite (`fileID: 21300000`) en Unity 6**:
   - `GlassTargetDamaged.png.meta`, `RightClickTarget.png.meta` y `HoldTarget.png.meta` tienen actualmente `spriteMode: 2` (`Multiple`) con `sprites: []`. Para que Unity genere y enlace el `Sprite` `{fileID: 21300000, guid: <GUID>, type: 3}`, deben actualizarse a `spriteMode: 1` (`Single`) con `spriteID: 5e97eb03825dee720800000000000000` (igual que `GlassTarget.png.meta`).
2. **Proporción Círculo (`376px`) vs Lienzo (`740px`)**:
   - En los 4 nuevos sprites de `740x740`, el círculo de la diana mide `376x376` píxeles y está centrado en `(370, 370)` (proporción `376 / 740 = 0.5081081f`, radio normalizado `188 / 740 = 0.25405405f`).
   - Para que el círculo visible en pantalla conserve exactamente el diámetro lógico `targetSize` configurado en cada `TaskDefinition` (y el `CircleHitbox`, los rebotes en `TaskTargetMovement` y la puntuación de precisión en `BaseTask.CalculatePrecisionAndSizeScore` coincidan al píxel con el borde negro del círculo de `376x376` sin permitir clics en el margen transparente ni en la barra inferior de `HoldTarget`), `CircleHitbox` soportará un `radiusRatio` configurable (`0.25405405f` en los targets de lienzo `740x740`, manteniendo `0.5f` por defecto en `ClickTarget` y `HoverTarget`), y `TaskTargetMovement` escalará el `RectTransform` por `0.5f / radiusRatio` (`740f / 376f = 1.9680851f`) usando el radio visual efectivo para los márgenes de movimiento.
3. **Coordenadas Exactas de la Barra Inferior en `HoldTarget.png` (`740x740`)**:
   - El marco negro exterior de la barra inferior ocupa `X = [255..482]` (`228px` de ancho) y `Y_top_down = [578..622]` (`45px` de alto).
   - La cavidad interior de la barra (dentro del borde negro de `4px` a izquierda/derecha y `12px` arriba/abajo) ocupa exactamente:
     - `X = [259..478]` (`220px` de ancho) -> en coordenadas normalizadas UI (`0..1`): `xMin = 259 / 740 = 0.35f`, `xMax = 479 / 740 = 0.6472973f`.
     - `Y_top_down = [590..610]` (`21px` de alto) -> en coordenadas normalizadas UI de abajo a arriba (`0..1`): `yMin = (740 - 611) / 740 = 129 / 740 = 0.17432432f`, `yMax = (740 - 590) / 740 = 150 / 740 = 0.2027027f`.

---

## 2. Plan de Implementación por Secciones

### Sección 1: Corrección de `.meta` de los Sprites y Ajuste de Hitbox/Escala para Lienzos `740x740`
1. **`Assets/Art/UI/Target Icons/GlassTargetDamaged.png.meta`**, **`HoldTarget.png.meta`** y **`RightClickTarget.png.meta`**:
   - Cambiar `spriteMode: 2` a `spriteMode: 1` y establecer `spriteID: 5e97eb03825dee720800000000000000` para que el `Sprite` `{fileID: 21300000, guid: ..., type: 3}` sea válido en prefabs y escenas.
2. **`Assets/Scripts/Tasks/CircleHitbox.cs`**:
   - Añadir `[SerializeField] private float radiusRatio = 0.5f;` y la propiedad pública `public float RadiusRatio => radiusRatio > 0.01f ? radiusRatio : 0.5f;`.
   - En `IsRaycastLocationValid`, comprobar `return normalizedLocal.sqrMagnitude <= (r * r);` donde `r = RadiusRatio`.
3. **`Assets/Scripts/Tasks/BaseTask.cs` y `Assets/Scripts/Tasks/TaskTargetMovement.cs`**:
   - En `BaseTask.CalculatePrecisionAndSizeScore` y `CalculateRatioAndSizeScore`: si el `targetRect` tiene un componente `CircleHitbox`, calcular el radio efectivo de la diana como `Mathf.Max(targetRect.rect.width, targetRect.rect.height) * hitbox.RadiusRatio` y el diámetro efectivo (`effectiveRadius * 2f`) para el cálculo de `ratio` (centro/anillo medio/anillo exterior) y `sizeBonus`.
   - En `TaskTargetMovement.Setup`: si el objeto tiene un `CircleHitbox` con `RadiusRatio < 0.5f`, escalar `rectTransform.sizeDelta` por `0.5f / hitbox.RadiusRatio` (`size * (740f / 376f)`) para que el círculo visible tenga exactamente diámetro `size`, y usar el radio visual efectivo (`rectTransform.rect.width * hitbox.RadiusRatio`, más el margen inferior adicional de la barra cuando sea `ClickHoldTask`) en `RandomizePosition()` y `Update()` para que los rebotes DVD y el movimiento circular lleguen limpiamente hasta los bordes del área de juego sin recortar la barra inferior.

### Sección 2: Right Click Target (`RightClickTask` y `Task_RightClick.prefab`)
1. **`Assets/Prefabs/Tasks/Task_RightClick.prefab`**:
   - En el componente `Image` (`fileID: 5190953794425844330`) del objeto `Target`:
     - Sustituir `m_Sprite` por `{fileID: 21300000, guid: 1f681aa4d1a600746a4980e06b2f1a51, type: 3}` (`RightClickTarget.png`).
     - Cambiar `m_Color` a `{r: 1, g: 1, b: 1, a: 1}` para eliminar el antiguo tinte naranja y mostrar los colores reales del sprite.
   - En el componente `CircleHitbox` (`fileID: 8618452825108688356`):
     - Configurar `radiusRatio: 0.25405407` (`188 / 740`).

### Sección 3: Double Click Target con Cristal Dañado y Desaparición (`DoubleClickTask.cs` y `Task_DoubleClick.prefab`)
1. **`Assets/Scripts/Tasks/DoubleClickTask.cs`**:
   - Añadir campos serializados:
     ```csharp
     [SerializeField] private UnityEngine.UI.Image targetImage;
     [SerializeField] private Sprite intactGlassSprite;
     [SerializeField] private Sprite damagedGlassSprite;
     ```
   - En `SetupTask(TaskDefinition definition)`:
     - Resolver `if (targetImage == null) targetImage = GetComponent<UnityEngine.UI.Image>();`.
     - Asegurar que `targetImage.enabled = true`, `targetImage.color = Color.white` y, si `intactGlassSprite != null`, asignar `targetImage.sprite = intactGlassSprite`.
   - **Al registrar el primer clic izquierdo (`hasValidFirstClick = true`)**:
     - Cambiar el sprite de `targetImage` a `damagedGlassSprite` (`GlassTargetDamaged.png`) para que la diana de cristal se agriete visualmente tras el primer impacto.
   - **Si el primer clic se invalida antes del segundo clic** (por expirar el tiempo `DOUBLE_CLICK_THRESHOLD` en `WaitAndReportSingleClickError()`, salir de la diana en `OnPointerExit`, hacer clic fuera en `OnBackgroundClicked` o pulsar clic derecho):
     - Restaurar `if (targetImage != null && intactGlassSprite != null) targetImage.sprite = intactGlassSprite;` para indicar que el intento de doble clic se ha reiniciado.
   - **Al registrar el segundo clic izquierdo dentro de `DOUBLE_CLICK_THRESHOLD`**:
     - Ocultar inmediatamente la imagen de la diana (`if (targetImage != null) targetImage.enabled = false;`) antes de llamar a `Complete(true, transform, combinedScore, maxScorePoints)` para que la diana desaparezca al romperse tras el segundo clic.
2. **`Assets/Prefabs/Tasks/Task_DoubleClick.prefab`**:
   - En el componente `Image` (`fileID: 3980626594689084646`) del objeto `Target`:
     - Asignar `m_Sprite: {fileID: 21300000, guid: 53c7c3af77b3e2d48bc33f6b4cd9a73f, type: 3}` (`GlassTarget.png`) y `m_Color: {r: 1, g: 1, b: 1, a: 1}`.
   - En el componente `DoubleClickTask` (`fileID: 6290488348178071579`):
     - Asignar `targetImage: {fileID: 3980626594689084646}`, `intactGlassSprite: {fileID: 21300000, guid: 53c7c3af77b3e2d48bc33f6b4cd9a73f, type: 3}` y `damagedGlassSprite: {fileID: 21300000, guid: ff99b07f12d75b74daa260be7138e002, type: 3}`.
   - En el componente `CircleHitbox` (`fileID: 1873111713536925065`):
     - Configurar `radiusRatio: 0.25405407` (`188 / 740`).

### Sección 4: Click and Hold con Relleno en la Barra Inferior del Nuevo Icono (`ClickHoldTask.cs` y `Task_ClickHold.prefab`)
1. **`Assets/Prefabs/Tasks/Task_ClickHold.prefab`**:
   - En el componente `Image` (`fileID: 7008192182396012675`) del objeto `Target`:
     - Asignar `m_Sprite: {fileID: 21300000, guid: 46f5cef5f61b5854eb34041547728579, type: 3}` (`HoldTarget.png`) y `m_Color: {r: 1, g: 1, b: 1, a: 1}`.
   - En el componente `CircleHitbox` (`fileID: 7124001335726430981`):
     - Configurar `radiusRatio: 0.25405407` (`188 / 740`).
   - En el hijo `FillImage` (`RectTransform` `fileID: 6192991850878380951`, `Image` `fileID: 8674583940393954986`):
     - Configurar las anclas para que encajen exactamente en el interior hueco de la barra inferior de `HoldTarget.png`:
       - `m_AnchorMin: {x: 0.35, y: 0.17432432}`
       - `m_AnchorMax: {x: 0.6472973, y: 0.2027027}`
       - `m_AnchoredPosition: {x: 0, y: 0}`
       - `m_SizeDelta: {x: 0, y: 0}`
       - `m_Pivot: {x: 0, y: 0.5}`
     - En `Image` (`fileID: 8674583940393954986`), configurar `m_Type: 3` (`Filled`), `m_FillMethod: 0` (`Horizontal`), `m_FillOrigin: 0` (`Left`), `m_FillAmount: 0`, `m_Color: {r: 0.0, g: 0.502, b: 0.251, a: 1}`.
2. **`Assets/Scripts/Tasks/ClickHoldTask.cs`**:
   - En `SetupTask(TaskDefinition definition)`:
     - Garantizar que la cavidad de la barra inferior de `HoldTarget.png` tenga un fondo blanco opaco (`BarBackground` creado o reutilizado automáticamente detrás de `FillImage` con las mismas anclas `(0.35f, 0.17432432f)` a `(0.6472973f, 0.2027027f)` y `raycastTarget = false`) para que la barra vacía se vea blanca dentro de su marco negro.
     - Asignar a `fillImage` un sprite rectangular blanco sólido (`1x1` / `4x4` generado en memoria con `Sprite.Create(Texture2D.whiteTexture, ...)` para no usar el antiguo sprite circular de diana), forzar sus anclas a la cavidad de la barra inferior (`anchorMin = new Vector2(259f / 740f, 129f / 740f)`, `anchorMax = new Vector2(479f / 740f, 150f / 740f)`), `fillImage.type = Image.Type.Filled`, `fillImage.fillMethod = Image.FillMethod.Horizontal`, `fillImage.fillOrigin = (int)Image.OriginHorizontal.Left`, `fillImage.fillAmount = 0f`.
     - Durante el `Update()` mientras `isHolding` es `true`, actualizar `fillImage.fillAmount = progress` (rellenando horizontalmente de izquierda a derecha la barra inferior de `HoldTarget.png` en lugar de rellenar la diana de abajo a arriba).

### Sección 5: Verificación y Documentación
1. Compilar `Assembly-CSharp.csproj` y `Assembly-CSharp-Editor.csproj` verificando **0 errores**.
2. Documentar los cambios en `Documentation/Specs/Diego.md` con una nueva sección numerada.
