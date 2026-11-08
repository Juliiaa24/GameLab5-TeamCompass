# Technical Specification: WebGL Build Adaptation & Compatibility (`webBuild`)

## 1. Context & Architectural Overview
* **Engine & Target Platform:** Unity 6 (`6000.5.9f1`) — Universal Render Pipeline (URP) targeting **WebGL (`UNITY_WEBGL`)**.
* **Branch:** `webBuild` (exclusively).
* **Core Problem:** Several core systems in `ComputerLearner` rely on desktop-only OS assumptions (`System.IO.File` writing to the Windows Desktop folder, `Application.dataPath` file I/O, `#if UNITY_EDITOR AssetDatabase.LoadAssetAtPath` fallbacks for prefabs/sprites missing from `Assets/Resources/`, native OS clipboard `GUIUtility.systemCopyBuffer`, and browser-hijacked keyboard shortcuts like `Ctrl+S` and right-click context menus).
* **Goal:** Adapt the entire project on the `webBuild` branch so that every scene (`Menu`, `AssessmentScene`, `Desktop1`, `VirtualDesktop`, `StatsScene`), every guided level (`1` to `16`), all statistics/playtest downloads, all dynamic windows/icons/medals, and the `16:9` aspect ratio enforcement work seamlessly in any modern browser (Chrome, Edge, Firefox, Safari) and on any static web host (itch.io, GitHub Pages, school servers).

---

## 2. Diagnostic Summary of WebGL Issues Found

| # | Area / Subsystem | Root Cause in `webBuild` | Impact in WebGL Build |
|---|---|---|---|
| **1** | **Playtest & Statistics Export** (`PlaytestDataManager.cs`, `PlaytestStudentWidget.cs`) | Uses `Environment.GetFolderPath(SpecialFolder.DesktopDirectory)` and `System.IO.File.WriteAllText` to write 5 files (`1_...txt`, `2_...html`, `3_...csv`, `4_...json`, `All_Students_Summary.csv`). | In WebGL, `File.WriteAllText` writes only to Emscripten's in-memory virtual filesystem (`IDBFS`). **No file is ever downloaded by the browser.** Furthermore, triggering 5 separate browser downloads simultaneously gets blocked by browser popup/download blockers. |
| **2** | **`Application.dataPath` File System I/O** (`InitialTestManager.cs`, `PlaytestDataManager.cs`, `StatsSceneController.cs`, `MedalVisualUtility.cs`) | `InitialTestManager.SaveReportToJson` calls `Directory.CreateDirectory(Path.Combine(Application.dataPath, "Assessment"))` and `File.WriteAllText` unconditionally. `PlaytestDataManager` and `StatsSceneController` check `Directory.Exists(Path.Combine(Application.dataPath, "Assessment"))`. | In WebGL, `Application.dataPath` is an HTTP URL (`https://.../Build`), not a local disk path. File I/O on `Application.dataPath` fails or throws exceptions in WebGL. |
| **3** | **IndexedDB Persistence (`IDBFS` & `PlayerPrefs`)** (`ProgressData.cs`, `PlaytestDataManager.cs`, `SceneSystem.cs`) | Relies on `OnApplicationQuit()` to auto-export and flush data, and writes to `Application.persistentDataPath` without calling Emscripten's `FS.syncfs(false, ...)`. | Browser tabs closed by the user rarely fire `OnApplicationQuit()` reliably, and `IDBFS` changes in `Application.persistentDataPath` are lost on page reload unless explicitly synced to IndexedDB. |
| **4** | **Medals Missing in WebGL** (`MedalVisualUtility.cs`, `Assets/Resources/Medals/*.png.meta`) | `GoldMedalNoRibbon.png`, `SilverMedalNoRibbon.png`, and `BronzeMedalNoRibbon.png` in `Assets/Resources/Medals/` have `textureType: 0` (`Default` Texture2D) instead of `textureType: 8` (`Sprite`). In the Editor it only worked because of a fallback reading `Application.dataPath/Art/UI/Medals/*.png` from disk! | `Resources.Load<Sprite>("Medals/...")` returns `null` in WebGL, and the disk fallback fails because `Application.dataPath` is a URL. All medals degrade to plain procedural circles. |
| **5** | **Missing Prefabs & Sprites in `Assets/Resources/`** (`StartMenuController.cs`, `FileExplorerApp.cs`, `DesktopManager.cs`, `DesktopContextMenuTrigger.cs`, `MinigamesHubApp.cs`) | Scripts call `Resources.Load<GameObject>("ImageViewerContent")`, `Resources.Load<GameObject>("BaseWindow")`, `Resources.Load<GameObject>("FileExplorerContent")`, and `Resources.Load<Sprite>("Text_File_Icon_T01_Scaled")` etc., relying on `#if UNITY_EDITOR AssetDatabase.LoadAssetAtPath(...)` when `Resources.Load` returns `null`. | In a WebGL build, `#if UNITY_EDITOR` is stripped out and `Resources.Load` returns `null`. Opening `ImageViewerContent` (double-clicking images in Explorer) fails completely, and fallback icons/windows fail to load. |
| **6** | **Browser Shortcut Hijacking (`Ctrl+S`, Right-Click, Mac `Cmd`)** (`NotepadApp.cs`, `VirtualDesktopGuidedLevels.cs`, `RightClickTask.cs`) | Levels 14 & 16 require pressing `Ctrl + S` in Notepad; several tasks require right-clicking (`ContextMenuController`, `RightClickTask`). | Pressing `Ctrl + S` in a browser opens the browser's native *"Save Webpage As..."* dialog and steals focus from the WebGL canvas. Right-clicking can open the browser's context menu. Mac users pressing `Cmd + C/V/X/Z/S` are ignored. |
| **7** | **Clipboard Restrictions in WebGL** (`NotepadApp.cs`) | `NotepadApp` reads/writes `GUIUtility.systemCopyBuffer` during `Ctrl+C`, `Ctrl+X`, `Ctrl+V`. | Browser security blocks synchronous access to the OS clipboard from WebAssembly (`GUIUtility.systemCopyBuffer`), causing copy/cut/paste to fail if `VirtualClipboard` is not prioritized. |
| **8** | **16:9 Aspect Ratio & WebGL Canvas / Project Settings** (`ProjectSettings.asset`, `AspectRatioController.cs`, `VirtualMascot.cs`, `BuildFinalScene.unity`) | `webBuild` does not yet have `AspectRatioController.cs` from `main`, `defaultScreenWidthWeb: 960` / `defaultScreenHeightWeb: 600` is 16:10, and `webGLDecompressionFallback` is `0`. | Browser windows/iframes with non-16:9 aspect ratios distort or clip UI elements, and compressed WebGL builds fail to load on hosts like itch.io or GitHub Pages without decompression fallback. |
| **9** | **`Application.Quit()` & Escape Key in Browser** (`SceneSystem.cs`) | Pressing `Escape` in `Menu` or clicking the Exit button calls `Application.Quit()`. | In WebGL, `Application.Quit()` cannot close the browser tab and can leave the WebGL canvas unresponsive or confuse the user. |

---

## 3. Phase-by-Phase Implementation Plan

### Phase 1: Native JavaScript Bridge (`WebGLBrowserBridge.jslib`) & C# Helper (`WebGLBridge.cs`)
1. **Create `ComputerLearner/Assets/Plugins/WebGL/WebGLBrowserBridge.jslib`:**
   * Implement `WebGL_DownloadFile(filenamePtr, mimeTypePtr, bytePtr, byteLength)`:
     * Reads the UTF-8 filename and MIME type (`UTF8ToString`), copies the byte buffer from `HEAPU8.subarray(bytePtr, bytePtr + byteLength)`, creates a `new Blob([bytes], { type: mimeType })`, generates an object URL via `URL.createObjectURL(blob)`, creates a temporary `<a download="...">` element, appends it to `document.body`, triggers `.click()`, and revokes the URL after a short timeout.
   * Implement `WebGL_SyncFileSystem()`:
     * Safely calls `if (typeof FS !== 'undefined' && FS.syncfs) { FS.syncfs(false, function(err) { if (err) console.warn('[WebGLBridge] FS.syncfs error:', err); }); }` so files written to `Application.persistentDataPath` persist in IndexedDB across browser reloads.
   * Implement `WebGL_InstallBrowserInterceptors()`:
     * Attaches a `window.addEventListener('keydown', ..., { capture: true })` listener that intercepts browser-hijacked shortcuts (`Ctrl/Cmd + S`, `Ctrl/Cmd + P`, `Ctrl/Cmd + F`, `Ctrl/Cmd + G`, `Ctrl/Cmd + U`) when the WebGL canvas is active and calls `e.preventDefault()` (without calling `e.stopPropagation()`, so Unity's Input System still receives the key press!).
     * Attaches a `window.addEventListener('contextmenu', function(e) { e.preventDefault(); }, { capture: true })` listener on the canvas/document so right-click tasks (`RightClickTask`, `ContextMenuController`, `DesktopContextMenuTrigger`) never trigger the browser's native right-click menu.
     * Attaches `window.addEventListener('beforeunload', ...)` and `document.addEventListener('visibilitychange', ...)` to trigger `FS.syncfs(false, ...)` when the tab is hidden or closing.
2. **Create `ComputerLearner/Assets/Scripts/Utilities/WebGLBridge.cs`:**
   * Provide a clean static C# wrapper inside `namespace ComputerLearning`:
     * `public static bool IsWebGLBuild` (`true` when `#if UNITY_WEBGL && !UNITY_EDITOR`).
     * `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] private static void Initialize()` -> calls `WebGL_InstallBrowserInterceptors()` on WebGL.
     * `public static void DownloadFile(string fileName, string mimeType, byte[] data)` -> invokes `WebGL_DownloadFile` on WebGL.
     * `public static void SyncFileSystem()` -> invokes `WebGL_SyncFileSystem()` on WebGL and `PlayerPrefs.Save()`.

---

### Phase 2: Playtest & Statistics Export in WebGL (`PlaytestDataManager.cs`, `PlaytestStudentWidget.cs`, `UIDialogs.cs`)
1. **In-Memory ZIP Archive Builder (`SimpleZipBuilder` inside `WebGLBridge.cs` or `PlaytestDataManager.cs`):**
   * Browsers block websites that attempt to trigger 5 separate file downloads (`1_...txt`, `2_...html`, `3_...csv`, `4_...json`, `All_Students_Summary.csv`) in a single click.
   * Implement a lightweight, zero-dependency, pure-C# ZIP builder (`CreateZipBytes(Dictionary<string, byte[]> files)`) using standard ZIP Local File Headers (`0x04034b50`), Central Directory Headers (`0x02014b50`), End of Central Directory (`0x06054b50`), and CRC-32 calculation (Store method `0`, which requires zero native compression libraries and works 100% deterministically in IL2CPP/WebAssembly).
2. **Update `PlaytestDataManager.ExportCurrentStudentData(string exportTriggerReason = "manual")`:**
   * Keep writing the 5 files (`1_...txt`, `2_...html`, `3_...csv`, `4_...json`, and updating `All_Students_Summary.csv`) into `Application.persistentDataPath/ComputerLearner_Playtest_Data/{safeId}` so that `All_Students_Summary.csv` accumulates across multiple students (`S01`, `S02`, `S03`...) on the same browser/device, followed by `WebGLBridge.SyncFileSystem()`.
   * On WebGL (`#if UNITY_WEBGL && !UNITY_EDITOR`), determine whether a browser file download should be triggered:
     * Always trigger a browser download when `exportTriggerReason == "manual_button"` or `exportTriggerReason == "auto_before_reset"`.
     * When `exportTriggerReason == "assessment_completed"` or `"all_levels_completed"`, trigger the browser download if `AutoDownloadEnabled` is `true` (avoid triggering unsolicited browser downloads on every intermediate single-level completion `"level_X_completed"` unless requested, or bundle cleanly so the browser never blocks it).
     * Package the 5 files (`{safeId}/1_{safeId}_Readable_Report.txt`, `{safeId}/2_{safeId}_Visual_Report.html`, `{safeId}/3_{safeId}_Excel_Data.csv`, `{safeId}/4_{safeId}_Raw_Data.json`, and `All_Students_Summary.csv`) into a single `ComputerLearner_Playtest_{safeId}.zip` byte array and call `WebGLBridge.DownloadFile($"ComputerLearner_Playtest_{safeId}.zip", "application/zip", zipBytes)`.
3. **Update `PlaytestStudentWidget.OnDownloadClicked()`:**
   * Ensure clicking the Download button in `PlaytestStudentWidget` (available in the Main Menu and Stats Scene) works in WebGL and displays a clear localized toast message confirming the `.zip` download in the browser.
4. **Remove Invalid `Application.dataPath` File I/O in Builds:**
   * In `InitialTestManager.cs` (`SaveReportToJson`, lines 985–1008): Always save the JSON to `PlayerPrefs` (`PlaytestDataManager.CurrentAssessmentJsonKey`) and call `PlayerPrefs.Save()`, and wrap the `Path.Combine(Application.dataPath, "Assessment")` disk write strictly inside `#if UNITY_EDITOR`.
   * In `PlaytestDataManager.cs` (`TryLoadLatestAssessmentReport`, lines 322–336) and `StatsSceneController.cs` (`LoadLatestReport`, lines 371–385): Wrap the `Path.Combine(Application.dataPath, "Assessment")` directory scan inside `#if UNITY_EDITOR` so WebGL builds rely cleanly on `PlayerPrefs` (`CurrentAssessmentJsonKey`) without touching `Application.dataPath`.
   * In `ProgressData.cs`: Call `WebGLBridge.SyncFileSystem()` after `PlayerPrefs.Save()` inside `Save()` so progress is immediately flushed to IndexedDB after every task and level.

---

### Phase 3: Fix Missing Resources & Sprite Import Settings (`Assets/Resources/` & `MedalVisualUtility.cs`)
1. **Fix Medal Sprites in `Assets/Resources/Medals/`:**
   * Update `ComputerLearner/Assets/Resources/Medals/GoldMedalNoRibbon.png.meta`, `SilverMedalNoRibbon.png.meta`, and `BronzeMedalNoRibbon.png.meta`:
     * Change `textureType: 0` -> `textureType: 8` (`Sprite (2D and UI)`) and `spriteMode: 1` (`Single`).
   * In `MedalVisualUtility.cs` (`LoadMedalSpriteFromDisk`):
     * First attempt `Resources.Load<Sprite>("Medals/" + fileNameNoExt)`.
     * Add a second build-safe fallback: `Texture2D tex = Resources.Load<Texture2D>("Medals/" + fileNameNoExt);` and if non-null, create and cache the sprite via `Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f)`.
     * Wrap the `Application.dataPath` file system fallback (`File.ReadAllBytes`) inside `#if UNITY_EDITOR`.
2. **Ensure All Dynamically Loaded Prefabs & Sprites Exist in `Assets/Resources/`:**
   * Copy/place the required prefabs and sprites into `ComputerLearner/Assets/Resources/` (with valid new GUIDs in their `.meta` files so there are zero GUID collisions with `Assets/Prefabs/` or `Assets/Art/`):
     * `Assets/Prefabs/Windows/ImageViewerContent.prefab` -> `Assets/Resources/ImageViewerContent.prefab` (required by `StartMenuController.OpenImageViewer` line 310).
     * `Assets/Prefabs/Windows/BaseWindow.prefab` -> `Assets/Resources/BaseWindow.prefab` (required as fallback by `StartMenuController.cs`, `FileExplorerApp.cs`, `MinigamesHubApp.cs`).
     * `Assets/Prefabs/Windows/FileExplorerContent.prefab` -> `Assets/Resources/FileExplorerContent.prefab` (required as fallback by `FileExplorerApp.cs`, `DesktopContextMenuTrigger.cs`, `DesktopManager.cs`).
     * `Assets/Art/Textures/Icons/Text_File_Icon_T01_Scaled.png` -> `Assets/Resources/Text_File_Icon_T01_Scaled.png` (configured as `textureType: 8`, `spriteMode: 1`).
     * `Assets/Art/Textures/Icons/Image_File_Icon_01_Scaled.png` -> `Assets/Resources/Image_File_Icon_01_Scaled.png` (configured as `textureType: 8`, `spriteMode: 1`).
     * `Assets/Art/Photos/Computer Presentation.png` -> `Assets/Resources/Computer Presentation.png` (configured as `textureType: 8`, `spriteMode: 1` for `FileExplorerApp.FindSpriteByName`).
   * In `FileExplorerApp.cs` (`GetFolderSprite()` line 419):
     * Update `Resources.Load<Sprite>("Folder_Icon01")` to also check `Resources.Load<Sprite>("FolderIcons/Folder_Icon01")` (where `Folder_Icon01.png` actually lives inside `Assets/Resources/FolderIcons/`).

---

### Phase 4: Keyboard Shortcuts (`Ctrl`/`Cmd`) & Clipboard Reliability in WebGL (`NotepadApp.cs` & `VirtualDesktopGuidedLevels.cs`)
1. **Cross-Platform Shortcut Detection (`Ctrl` + Mac `Cmd`):**
   * In `NotepadApp.cs` (`Update()`) and `VirtualDesktopGuidedLevels.cs` (`Update()`):
     * Update modifier detection so `bool ctrlPressed = keyboard.ctrlKey.isPressed || keyboard.leftCommandKey.isPressed || keyboard.rightCommandKey.isPressed;` works on both Windows/Linux/Chromebook (`Ctrl`) and macOS (`Cmd`) in browsers.
2. **WebGL-Safe Clipboard in `NotepadApp.cs`:**
   * In `NotepadApp.HandleCopyShortcut()`, `HandleCutShortcut()`, and `HandlePasteShortcut()`:
     * Always store and read from `VirtualClipboard.CopyText(...)` / `VirtualClipboard.TextContent` as the primary source of truth, wrapping `GUIUtility.systemCopyBuffer` access in a safe helper so that browser clipboard restrictions in WebGL never prevent copying, cutting, or pasting inside `NotepadApp` or failing guided level tasks (`Level 13`, `Level 15`, `Level 16`).

---

### Phase 5: 16:9 Aspect Ratio Enforcement, WebGL Project Settings & Exit Behavior (`AspectRatioController.cs`, `VirtualMascot.cs`, `SceneSystem.cs`, `ProjectSettings.asset`, `BuildFinalScene.unity`)
1. **Port 16:9 Resolution & Aspect Ratio Enforcement to `webBuild`:**
   * Create `ComputerLearner/Assets/Scripts/Utilities/AspectRatioController.cs` (enforcing `16:9` camera viewport rects with black letterbox/pillarbox bars across all cameras, normalizing `CanvasScaler` to `1920x1080`, and switching `ScreenSpaceOverlay` canvases to `ScreenSpaceCamera` when aspect ratio deviates from `16:9` so UI stays strictly inside the `16:9` frame even when the browser window is resized).
   * Update `Managers.cs` to initialize `AspectRatioController.EnsureExists()`.
   * Update `VirtualMascot.cs` (`ClampRectTransformToScreen` and `KeepWithinScreenBounds`) to clamp within `Camera.main.pixelRect` instead of `0..Screen.width/height`.
   * Update `ComputerLearner/Assets/Scenes/BuildFinalScene.unity` `CanvasScaler` `m_ReferenceResolution` from `800x600` to `1920x1080`.
2. **Update `ProjectSettings/ProjectSettings.asset` for WebGL:**
   * Set `defaultScreenWidth: 1920`, `defaultScreenHeight: 1080`.
   * Set `defaultScreenWidthWeb: 1920`, `defaultScreenHeightWeb: 1080` (was `960x600`, which is `16:10`).
   * Set `webGLDecompressionFallback: 1` (enables automatic JS decompression fallback so the Gzip-compressed WebGL build loads on any server without custom HTTP headers).
   * Set `runInBackground: 1`.
3. **Graceful Exit / Escape Behavior in WebGL (`SceneSystem.cs`):**
   * In `SceneSystem.Update()`: On `Escape` while in `Menu`, in `#if UNITY_WEBGL && !UNITY_EDITOR`, toggle `Screen.fullScreen = false` if fullscreen instead of calling `Application.Quit()`.
   * In `SceneSystem.Exit()`: After exporting playtest data (`ExportCurrentStudentData("exit_button")`), in `#if UNITY_WEBGL && !UNITY_EDITOR`, exit fullscreen (`Screen.fullScreen = false`) and show a confirmation toast via `PlaytestStudentWidget.Instance?.ShowToast(...)` so the user knows their playtest data was exported and they can safely close the browser tab.

---

## 4. Verification Criteria
1. **Zero Compilation Warnings/Errors:** Verify via Unity MCP `read_console` that all scripts and `.jslib` plugins compile cleanly in Unity 6 (`6000.5.9f1`) on the `webBuild` branch.
2. **Resource & Asset Verification:** Verify that `Resources.Load<Sprite>("Medals/GoldMedalNoRibbon")`, `Resources.Load<Sprite>("Medals/SilverMedalNoRibbon")`, `Resources.Load<Sprite>("Medals/BronzeMedalNoRibbon")`, `Resources.Load<GameObject>("ImageViewerContent")`, `Resources.Load<GameObject>("BaseWindow")`, `Resources.Load<GameObject>("FileExplorerContent")`, `Resources.Load<Sprite>("Text_File_Icon_T01_Scaled")`, and `Resources.Load<Sprite>("Image_File_Icon_01_Scaled")` all return non-null in runtime without relying on `AssetDatabase`.
3. **ZIP & Export Verification:** Verify that `PlaytestDataManager.ExportCurrentStudentData` generates a valid `.zip` containing all 5 files (`1_...txt`, `2_...html`, `3_...csv`, `4_...json`, `All_Students_Summary.csv`) and triggers `WebGLBridge.DownloadFile` in WebGL.
4. **Git Branch Integrity:** Confirm `git branch --show-current` is `webBuild` and `main` remains untouched.
