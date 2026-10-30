/**
 * Author: TeamCompass & Senior Unity Dev
 * Date: 23/10/2026
 * Description: Interactive guided tutorial system for Levels 10 to 16 executed directly
 *              on the Virtual Desktop with the Virtual Mascot, TutorialBlocker, and LevelCelebrationModal.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace ComputerLearning
{
    public class VirtualDesktopGuidedLevels : MonoBehaviour
    {
        public static VirtualDesktopGuidedLevels Instance { get; private set; }
        public static bool IsLevelRunning { get; private set; }
        public static string CurrentRunningLevelId { get; private set; }

        private TutorialBlocker tutorialBlocker;
        private Coroutine activeLevelCoroutine;
        private float levelStartTime;
        private readonly List<Action> activeEventCleanupActions = new List<Action>();

        #region Input Helpers (Supports New Input System & Legacy Fallback)
        private static bool IsLeftMouseButtonDown()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null)
                return UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonDown(0);
#endif
            return false;
        }

        private static bool IsAnyKeyOrTypingWithoutMouse()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            bool anyKey = kb != null && kb.anyKey.wasPressedThisFrame;
            bool mouseHeld = mouse != null && (mouse.leftButton.isPressed || mouse.rightButton.isPressed);
            return anyKey && !mouseHeld;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.anyKeyDown && !Input.GetMouseButton(0) && !Input.GetMouseButton(1);
#else
            return false;
#endif
        }

        private static bool IsCtrlPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null) return kb.ctrlKey.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
#endif
            return false;
        }

        private static bool IsKeyJustPressed(UnityEngine.InputSystem.Key newKey, KeyCode oldKey)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null) return kb[newKey].wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(oldKey);
#endif
            return false;
        }

        private static bool IsKeyWithoutCtrl(UnityEngine.InputSystem.Key newKey, KeyCode oldKey)
        {
            return IsKeyJustPressed(newKey, oldKey) && !IsCtrlPressed();
        }
        #endregion

        public static bool IsGuidedDesktopLevel(string levelId)
        {
            if (string.IsNullOrEmpty(levelId)) return false;
            string clean = levelId.ToLower().Trim();
            return clean == "level10" || clean == "level11" || clean == "level12" ||
                   clean == "level13" || clean == "level14" || clean == "level15" ||
                   clean == "level16";
        }

        public static void StartGuidedLevel(LevelDefinition levelDef)
        {
            if (levelDef == null) return;

            if (Instance == null)
            {
                DesktopManager desktopMgr = DesktopManager.Instance != null ? DesktopManager.Instance : FindAnyObjectByType<DesktopManager>();
                if (desktopMgr != null)
                {
                    Instance = desktopMgr.gameObject.AddComponent<VirtualDesktopGuidedLevels>();
                }
                else
                {
                    GameObject go = new GameObject("VirtualDesktopGuidedLevels");
                    Instance = go.AddComponent<VirtualDesktopGuidedLevels>();
                }
            }

            Instance.BeginLevel(levelDef);
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDisable()
        {
            CleanupActiveSubscriptions();
        }

        private void OnDestroy()
        {
            CleanupActiveSubscriptions();
            if (Instance == this) Instance = null;
            IsLevelRunning = false;
            CurrentRunningLevelId = null;
            if (tutorialBlocker != null)
            {
                Destroy(tutorialBlocker.gameObject);
                tutorialBlocker = null;
            }
        }

        private void RegisterCleanup(Action cleanupAction)
        {
            if (cleanupAction != null)
            {
                activeEventCleanupActions.Add(cleanupAction);
            }
        }

        private void CleanupActiveSubscriptions()
        {
            for (int i = activeEventCleanupActions.Count - 1; i >= 0; i--)
            {
                try
                {
                    activeEventCleanupActions[i]?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[VirtualDesktopGuidedLevels] Event cleanup warning: {e.Message}");
                }
            }
            activeEventCleanupActions.Clear();
        }

        private void SetupTutorialBlocker()
        {
            if (TutorialBlocker.Instance != null)
            {
                tutorialBlocker = TutorialBlocker.Instance;
                tutorialBlocker.gameObject.SetActive(true);
                return;
            }

            GameObject blockerObj = new GameObject("TutorialBlocker");
            tutorialBlocker = blockerObj.AddComponent<TutorialBlocker>();
        }

        private int currentLevelMistakes = 0;

        public void RecordMistake(string message, RectTransform target = null, bool isRightClickExpected = false)
        {
            currentLevelMistakes++;
            FeedbackManager.PlayError(true);
            if (isRightClickExpected)
            {
                VirtualMascot.ShowWrongClickFeedback(target, true);
            }
            else
            {
                VirtualMascot.ShowContextualFeedback(message, target, 3.2f);
            }
        }

        public void BeginLevel(LevelDefinition levelDef)
        {
            if (levelDef == null) return;

            CleanupActiveSubscriptions();
            if (activeLevelCoroutine != null)
            {
                StopCoroutine(activeLevelCoroutine);
                activeLevelCoroutine = null;
            }

            IsLevelRunning = true;
            CurrentRunningLevelId = levelDef.levelId;
            levelStartTime = Time.time;
            currentLevelMistakes = 0;

            // 1. Close all currently open windows to start with a clean desktop
            if (WindowManager.Instance != null)
            {
                WindowManager.Instance.CloseAllWindows();
            }

            SetupTutorialBlocker();
            tutorialBlocker?.SetAllowedTarget(null);

            string cleanId = levelDef.levelId.ToLower().Trim();
            switch (cleanId)
            {
                case "level10":
                    activeLevelCoroutine = StartCoroutine(RunLevel10ContextMenu(levelDef));
                    break;
                case "level11":
                    activeLevelCoroutine = StartCoroutine(RunLevel11NotepadTyping(levelDef));
                    break;
                case "level12":
                    activeLevelCoroutine = StartCoroutine(RunLevel12CopyPasteCut(levelDef));
                    break;
                case "level13":
                    activeLevelCoroutine = StartCoroutine(RunLevel13UndoShortcut(levelDef));
                    break;
                case "level14":
                    activeLevelCoroutine = StartCoroutine(RunLevel14SaveShortcut(levelDef));
                    break;
                case "level15":
                    activeLevelCoroutine = StartCoroutine(RunLevel15NotepadFull(levelDef));
                    break;
                case "level16":
                    activeLevelCoroutine = StartCoroutine(RunLevel16PaintUsage(levelDef));
                    break;
                default:
                    Debug.LogWarning($"[VirtualDesktopGuidedLevels] Unrecognized guided level: {levelDef.levelId}");
                    CompleteLevel(levelDef);
                    break;
            }
        }

        #region Level 10: Context Menu Level
        private IEnumerator RunLevel10ContextMenu(LevelDefinition levelDef)
        {
            yield return new WaitForSeconds(0.6f);

            RectTransform bgRect = DesktopManager.Instance != null && DesktopManager.Instance.DesktopIconContainer != null
                ? DesktopManager.Instance.DesktopIconContainer
                : null;

            VirtualMascot.Show("Welcome to Level 10: Context Menu!\nRight-click anywhere on the desktop wallpaper to open the options menu.", bgRect, new Vector2(0, 50));

            // Allow clicking anywhere on the wallpaper
            tutorialBlocker?.ClearTarget();

            float wallpaperInactivityTimer = 0f;
            while (ContextMenuController.Instance == null || !ContextMenuController.Instance.IsMenuOpen)
            {
                if (IsLeftMouseButtonDown())
                {
                    RecordMistake("Right-click anywhere on the desktop to open the options menu!", bgRect, true);
                    wallpaperInactivityTimer = 0f;
                }

                wallpaperInactivityTimer += Time.deltaTime;
                if (wallpaperInactivityTimer >= 7.0f)
                {
                    VirtualMascot.ShowContextualFeedback("Click the right mouse button on the desktop wallpaper to open the menu!", bgRect, 3.2f);
                    wallpaperInactivityTimer = 0f;
                }
                yield return null;
            }

            RectTransform menuRect = ContextMenuController.Instance.GetComponent<RectTransform>();
            tutorialBlocker?.SetAllowedTarget(menuRect);

            VirtualMascot.Show("Awesome! This is the Context Menu.\nClick on '+ New Text Document (.txt)' or '+ New Folder' to create an item!", menuRect, new Vector2(250, 0));

            bool actionConfirmed = false;
            Action<string> onMenuClick = (item) => { actionConfirmed = true; };
            Action onFsChange = () => { actionConfirmed = true; };

            ContextMenuController.OnMenuItemClicked += onMenuClick;
            FileExplorerApp.OnFileSystemChanged += onFsChange;
            RegisterCleanup(() =>
            {
                ContextMenuController.OnMenuItemClicked -= onMenuClick;
                FileExplorerApp.OnFileSystemChanged -= onFsChange;
            });

            bool wasMenuOpen = true;
            float menuInactivityTimer = 0f;
            while (!actionConfirmed)
            {
                if (ContextMenuController.Instance != null && !ContextMenuController.Instance.IsMenuOpen && !actionConfirmed)
                {
                    if (wasMenuOpen)
                    {
                        wasMenuOpen = false;
                        RecordMistake("The menu closed! Right-click again on the desktop and select an option!", bgRect);
                    }
                    tutorialBlocker?.ClearTarget();
                }
                else if (ContextMenuController.Instance != null && ContextMenuController.Instance.IsMenuOpen)
                {
                    wasMenuOpen = true;
                    tutorialBlocker?.SetAllowedTarget(menuRect);
                }

                menuInactivityTimer += Time.deltaTime;
                if (menuInactivityTimer >= 7.0f)
                {
                    VirtualMascot.ShowContextualFeedback("Click on '+ New Text Document (.txt)' or '+ New Folder' to continue!", menuRect, 3.2f);
                    menuInactivityTimer = 0f;
                }
                yield return null;
            }

            CleanupActiveSubscriptions();

            SoundManager.PlaySuccess();
            VirtualMascot.Show("Fantastic! You've learned how to use the desktop context menu to create and customize files!", bgRect, new Vector2(0, 50));
            yield return new WaitForSeconds(2.8f);

            CompleteLevel(levelDef);
        }
        #endregion

        #region Level 11: Open Notepad & Basic Typing
        private IEnumerator RunLevel11NotepadTyping(LevelDefinition levelDef)
        {
            yield return new WaitForSeconds(0.6f);

            DraggableIcon notepadIcon = DesktopManager.Instance?.GetIconByName("Notepad");
            RectTransform iconRect = notepadIcon != null ? notepadIcon.GetComponent<RectTransform>() : null;

            VirtualMascot.Show("Level 11: Word Processing!\nDouble-click the Notepad icon to open the text editor.", iconRect, new Vector2(160, -80));
            tutorialBlocker?.SetAllowedTarget(iconRect);

            float openTimer = 0f;
            NotepadApp notepad = null;
            while (notepad == null || !notepad.gameObject.activeInHierarchy)
            {
                notepad = FindAnyObjectByType<NotepadApp>();

                if (notepad == null || !notepad.gameObject.activeInHierarchy)
                {
                    openTimer += Time.deltaTime;
                    if (openTimer >= 7.0f)
                    {
                        VirtualMascot.ShowContextualFeedback("Double-click quickly on the Notepad icon to open it!", iconRect, 3.2f);
                        openTimer = 0f;
                    }
                }
                yield return null;
            }

            Window notepadWin = notepad.GetComponentInParent<Window>();
            RectTransform winRect = notepadWin != null ? notepadWin.GetComponent<RectTransform>() : notepad.GetComponent<RectTransform>();

            notepad.SetText("");
            tutorialBlocker?.SetAllowedTarget(winRect);

            VirtualMascot.Show("Now let's practice typing on your keyboard!\nType: 'Hello Computer!' in the notepad.", winRect, new Vector2(0, 180));

            float timer = 0f;
            while (notepad == null || !notepad.gameObject.activeInHierarchy ||
                   notepad.CurrentText.IndexOf("Hello Computer", StringComparison.OrdinalIgnoreCase) < 0)
            {
                if (notepad == null || !notepad.gameObject.activeInHierarchy)
                {
                    notepad = RecoverNotepadWindow(ref winRect);
                }

                // Check typing without focus
                if (notepad != null && notepad.InputField != null && !notepad.InputField.isFocused && IsAnyKeyOrTypingWithoutMouse())
                {
                    RecordMistake("Click inside the blank notepad area first to start typing!", winRect);
                }

                timer += Time.deltaTime;
                if (timer > 8.0f)
                {
                    VirtualMascot.Show("Remember: Type 'Hello Computer!' into the notepad.", winRect, new Vector2(0, 180));
                    timer = 0f;
                }
                yield return null;
            }

            SoundManager.PlaySuccess();
            VirtualMascot.Show("Terrific! You typed the phrase perfectly!", winRect, new Vector2(0, 180));
            yield return new WaitForSeconds(2.5f);

            CompleteLevel(levelDef);
        }
        #endregion

        #region Level 12: Copy, Paste & Cut
        private IEnumerator RunLevel12CopyPasteCut(LevelDefinition levelDef)
        {
            yield return new WaitForSeconds(0.6f);

            NotepadApp notepad = EnsureNotepadOpen();
            while (notepad == null || !notepad.gameObject.activeInHierarchy)
            {
                notepad = FindAnyObjectByType<NotepadApp>();
                yield return null;
            }

            Window win = notepad.GetComponentInParent<Window>();
            RectTransform winRect = win != null ? win.GetComponent<RectTransform>() : notepad.GetComponent<RectTransform>();

            notepad.SetText("Cat Dog");
            tutorialBlocker?.SetAllowedTarget(winRect);

            // Step 1: Copy
            VirtualMascot.Show("Let's master keyboard shortcuts!\nHighlight the word 'Cat' and press Ctrl + C on your keyboard to copy it.", winRect, new Vector2(0, 180));

            bool copied = false;
            Action<string> onCopy = (text) =>
            {
                if (text != null && text.IndexOf("Cat", StringComparison.OrdinalIgnoreCase) >= 0)
                    copied = true;
            };
            NotepadApp.OnTextCopied += onCopy;
            RegisterCleanup(() => NotepadApp.OnTextCopied -= onCopy);

            float copyTimer = 0f;
            while (!copied && (NotepadApp.VirtualClipboard == null || NotepadApp.VirtualClipboard.IndexOf("Cat", StringComparison.OrdinalIgnoreCase) < 0))
            {
                if (notepad == null || !notepad.gameObject.activeInHierarchy)
                {
                    notepad = RecoverNotepadWindow(ref winRect, "Cat Dog");
                }

                // Error: user pressed Ctrl+V before copying anything
                if (IsCtrlPressed() && IsKeyJustPressed(UnityEngine.InputSystem.Key.V, KeyCode.V))
                {
                    RecordMistake("You need to copy the text first with Ctrl + C before pasting!", winRect);
                }

                copyTimer += Time.deltaTime;
                if (copyTimer >= 8.0f)
                {
                    VirtualMascot.ShowContextualFeedback("Highlight 'Cat' and press Ctrl + C to copy it!", winRect, 3.2f);
                    copyTimer = 0f;
                }
                yield return null;
            }
            CleanupActiveSubscriptions();

            SoundManager.PlaySuccess();
            VirtualMascot.Show("Nice! 'Cat' is copied into your clipboard.", winRect, new Vector2(0, 180));
            yield return new WaitForSeconds(2.0f);

            // Step 2: Paste
            VirtualMascot.Show("Now click at the end of the text and press Ctrl + V to Paste it!", winRect, new Vector2(0, 180));

            bool pasted = false;
            Action<string> onPaste = (text) => { pasted = true; };
            NotepadApp.OnTextPasted += onPaste;
            RegisterCleanup(() => NotepadApp.OnTextPasted -= onPaste);

            float pasteTimer = 0f;
            while (!pasted && (notepad == null || notepad.CurrentText.Split(new[] { "Cat", "cat" }, StringSplitOptions.None).Length <= 2))
            {
                if (notepad == null || !notepad.gameObject.activeInHierarchy)
                {
                    notepad = RecoverNotepadWindow(ref winRect, "Cat Dog");
                }

                // Error: user pressed Ctrl+C again instead of Ctrl+V
                if (IsCtrlPressed() && IsKeyJustPressed(UnityEngine.InputSystem.Key.C, KeyCode.C))
                {
                    RecordMistake("Press Ctrl + V to Paste your copied word!", winRect);
                }

                pasteTimer += Time.deltaTime;
                if (pasteTimer >= 8.0f)
                {
                    VirtualMascot.ShowContextualFeedback("Press Ctrl + V on your keyboard to paste the word!", winRect, 3.2f);
                    pasteTimer = 0f;
                }
                yield return null;
            }
            CleanupActiveSubscriptions();

            SoundManager.PlaySuccess();
            VirtualMascot.Show("You pasted it! Now you have two Cats!", winRect, new Vector2(0, 180));
            yield return new WaitForSeconds(2.0f);

            // Step 3: Cut
            VirtualMascot.Show("Now highlight the word 'Dog' and press Ctrl + X to Cut it away!", winRect, new Vector2(0, 180));

            bool cut = false;
            Action<string> onCut = (text) =>
            {
                if (text != null && text.IndexOf("Dog", StringComparison.OrdinalIgnoreCase) >= 0)
                    cut = true;
            };
            NotepadApp.OnTextCut += onCut;
            RegisterCleanup(() => NotepadApp.OnTextCut -= onCut);

            float cutTimer = 0f;
            while (!cut && (notepad == null || notepad.CurrentText.Contains("Dog")))
            {
                if (notepad == null || !notepad.gameObject.activeInHierarchy)
                {
                    notepad = RecoverNotepadWindow(ref winRect, "Cat Dog Cat");
                }

                cutTimer += Time.deltaTime;
                if (cutTimer >= 8.0f)
                {
                    VirtualMascot.ShowContextualFeedback("Highlight 'Dog' and press Ctrl + X to cut it!", winRect, 3.2f);
                    cutTimer = 0f;
                }
                yield return null;
            }
            CleanupActiveSubscriptions();

            SoundManager.PlaySuccess();
            VirtualMascot.Show("Brilliant! Ctrl + X cut the text and saved it to your clipboard!", winRect, new Vector2(0, 180));
            yield return new WaitForSeconds(2.5f);

            CompleteLevel(levelDef);
        }
        #endregion

        #region Level 13: Undo Shortcut
        private IEnumerator RunLevel13UndoShortcut(LevelDefinition levelDef)
        {
            yield return new WaitForSeconds(0.6f);

            NotepadApp notepad = EnsureNotepadOpen();
            while (notepad == null || !notepad.gameObject.activeInHierarchy)
            {
                notepad = FindAnyObjectByType<NotepadApp>();
                yield return null;
            }

            Window win = notepad.GetComponentInParent<Window>();
            RectTransform winRect = win != null ? win.GetComponent<RectTransform>() : notepad.GetComponent<RectTransform>();

            notepad.SetText("Computer Science is fun!");
            notepad.PushUndoState();
            tutorialBlocker?.SetAllowedTarget(winRect);

            VirtualMascot.Show("Welcome to Level 13!\nLook at this sentence: 'Computer Science is fun!'", winRect, new Vector2(0, 180));
            yield return new WaitForSeconds(3.0f);

            if (notepad == null || !notepad.gameObject.activeInHierarchy)
            {
                notepad = RecoverNotepadWindow(ref winRect, "Computer Science is fun!");
                if (notepad != null) notepad.PushUndoState();
            }

            // Simulate an accidental mistake
            if (notepad != null) notepad.SetText("Computer Science is OOPS!");
            SoundManager.PlayError();

            VirtualMascot.Show("Oh no! Someone accidentally made a mistake!\nPress Ctrl + Z to Undo the mistake and restore your original words!", winRect, new Vector2(0, 180));

            bool undone = false;
            Action onUndo = () => { undone = true; };
            NotepadApp.OnTextUndo += onUndo;
            RegisterCleanup(() => NotepadApp.OnTextUndo -= onUndo);

            float undoTimer = 0f;
            while (!undone && (notepad == null || !notepad.CurrentText.Contains("fun")))
            {
                if (notepad == null || !notepad.gameObject.activeInHierarchy)
                {
                    notepad = RecoverNotepadWindow(ref winRect, "Computer Science is fun!");
                    if (notepad != null)
                    {
                        notepad.PushUndoState();
                        notepad.SetText("Computer Science is OOPS!");
                    }
                }

                // Error: pressing Z without Ctrl
                if (IsKeyWithoutCtrl(UnityEngine.InputSystem.Key.Z, KeyCode.Z))
                {
                    RecordMistake("Hold down the Control (Ctrl) key and press Z!", winRect);
                }

                undoTimer += Time.deltaTime;
                if (undoTimer >= 8.0f)
                {
                    VirtualMascot.ShowContextualFeedback("Press Ctrl + Z to Undo the mistake!", winRect, 3.2f);
                    undoTimer = 0f;
                }
                yield return null;
            }
            CleanupActiveSubscriptions();

            SoundManager.PlaySuccess();
            VirtualMascot.Show("Magic! Ctrl + Z lets you undo mistakes in almost every program!", winRect, new Vector2(0, 180));
            yield return new WaitForSeconds(2.8f);

            CompleteLevel(levelDef);
        }
        #endregion

        #region Level 14: Save Shortcut
        private IEnumerator RunLevel14SaveShortcut(LevelDefinition levelDef)
        {
            yield return new WaitForSeconds(0.6f);

            NotepadApp notepad = EnsureNotepadOpen();
            while (notepad == null || !notepad.gameObject.activeInHierarchy)
            {
                notepad = FindAnyObjectByType<NotepadApp>();
                yield return null;
            }

            Window win = notepad.GetComponentInParent<Window>();
            RectTransform winRect = win != null ? win.GetComponent<RectTransform>() : notepad.GetComponent<RectTransform>();

            notepad.SetText("Important Note: Always save your files before closing the app!");
            tutorialBlocker?.SetAllowedTarget(winRect);

            VirtualMascot.Show("Level 14: Save Shortcut!\nAlways save your files. Press Ctrl + S on your keyboard to save this note.", winRect, new Vector2(0, 180));

            bool saveOpened = false;
            Action onSaveKey = () => { saveOpened = true; };
            NotepadApp.OnSaveShortcut += onSaveKey;
            RegisterCleanup(() => NotepadApp.OnSaveShortcut -= onSaveKey);

            float saveShortcutTimer = 0f;
            while (!saveOpened && !FileExplorerApp.IsSaveDialogOpen)
            {
                if (notepad == null || !notepad.gameObject.activeInHierarchy)
                {
                    notepad = RecoverNotepadWindow(ref winRect, "Important Note: Always save your files before closing the app!");
                }

                // Error: pressing S without Ctrl
                if (IsKeyWithoutCtrl(UnityEngine.InputSystem.Key.S, KeyCode.S))
                {
                    RecordMistake("Press Ctrl and S together to save your document!", winRect);
                }

                saveShortcutTimer += Time.deltaTime;
                if (saveShortcutTimer >= 8.0f)
                {
                    VirtualMascot.ShowContextualFeedback("Press Ctrl + S on your keyboard to open the save window!", winRect, 3.2f);
                    saveShortcutTimer = 0f;
                }
                yield return null;
            }
            CleanupActiveSubscriptions();

            yield return new WaitForSeconds(0.4f);

            // Allow clicking on Save dialog
            tutorialBlocker?.ClearTarget();
            VirtualMascot.Show("The Save window is open! Type a name for your file (like 'MyNotes') and click SAVE.", winRect, new Vector2(0, 180));

            bool fileSaved = false;
            Action onFsChange = () => { fileSaved = true; };
            FileExplorerApp.OnFileSystemChanged += onFsChange;
            RegisterCleanup(() => FileExplorerApp.OnFileSystemChanged -= onFsChange);

            while (!fileSaved)
            {
                yield return null;
            }
            CleanupActiveSubscriptions();

            SoundManager.PlaySuccess();
            VirtualMascot.Show("Document saved! Ctrl + S is the most useful shortcut to keep your work safe.", winRect, new Vector2(0, 180));
            yield return new WaitForSeconds(2.8f);

            CompleteLevel(levelDef);
        }
        #endregion

        #region Level 15: Notepad Usage (Evaluación Completa)
        private IEnumerator RunLevel15NotepadFull(LevelDefinition levelDef)
        {
            yield return new WaitForSeconds(0.6f);

            VirtualMascot.Show("Level 15: The Grand Word Challenge!\nLet's test all your typing and keyboard skills together.", null, new Vector2(0, 80));
            yield return new WaitForSeconds(3.0f);

            NotepadApp notepad = EnsureNotepadOpen();
            while (notepad == null || !notepad.gameObject.activeInHierarchy)
            {
                notepad = FindAnyObjectByType<NotepadApp>();
                yield return null;
            }

            Window win = notepad.GetComponentInParent<Window>();
            RectTransform winRect = win != null ? win.GetComponent<RectTransform>() : notepad.GetComponent<RectTransform>();

            notepad.SetText("");
            tutorialBlocker?.SetAllowedTarget(winRect);

            // Step 1: Type
            VirtualMascot.Show("Step 1 of 5: Type the words: 'Red Blue Green'", winRect, new Vector2(0, 180));
            while (notepad == null || !notepad.gameObject.activeInHierarchy ||
                   notepad.CurrentText.IndexOf("Red", StringComparison.OrdinalIgnoreCase) < 0 ||
                   notepad.CurrentText.IndexOf("Blue", StringComparison.OrdinalIgnoreCase) < 0)
            {
                if (notepad == null || !notepad.gameObject.activeInHierarchy)
                {
                    notepad = RecoverNotepadWindow(ref winRect);
                }
                yield return null;
            }
            SoundManager.PlaySuccess();
            yield return new WaitForSeconds(1.2f);

            // Step 2: Copy & Paste
            VirtualMascot.Show("Step 2 of 5: Highlight 'Blue', press Ctrl + C, and press Ctrl + V to duplicate it!", winRect, new Vector2(0, 180));
            bool pasteDone = false;
            Action<string> onPaste = (t) => { pasteDone = true; };
            NotepadApp.OnTextPasted += onPaste;
            RegisterCleanup(() => NotepadApp.OnTextPasted -= onPaste);

            while (!pasteDone && (notepad == null || notepad.CurrentText.Split(new[] { "Blue", "blue" }, StringSplitOptions.None).Length <= 2))
            {
                if (notepad == null || !notepad.gameObject.activeInHierarchy)
                {
                    notepad = RecoverNotepadWindow(ref winRect, "Red Blue Green");
                }
                yield return null;
            }
            CleanupActiveSubscriptions();
            SoundManager.PlaySuccess();
            yield return new WaitForSeconds(1.2f);

            // Step 3: Cut
            VirtualMascot.Show("Step 3 of 5: Highlight 'Red' and press Ctrl + X to cut it away!", winRect, new Vector2(0, 180));
            bool cutDone = false;
            Action<string> onCut = (t) => { cutDone = true; };
            NotepadApp.OnTextCut += onCut;
            RegisterCleanup(() => NotepadApp.OnTextCut -= onCut);

            while (!cutDone && (notepad == null || notepad.CurrentText.Contains("Red")))
            {
                if (notepad == null || !notepad.gameObject.activeInHierarchy)
                {
                    notepad = RecoverNotepadWindow(ref winRect, "Red Blue Green Blue");
                }
                yield return null;
            }
            CleanupActiveSubscriptions();
            SoundManager.PlaySuccess();
            yield return new WaitForSeconds(1.2f);

            // Step 4: Undo
            VirtualMascot.Show("Step 4 of 5: Press Ctrl + Z to Undo the cut and bring 'Red' back!", winRect, new Vector2(0, 180));
            bool undoDone = false;
            Action onUndo = () => { undoDone = true; };
            NotepadApp.OnTextUndo += onUndo;
            RegisterCleanup(() => NotepadApp.OnTextUndo -= onUndo);

            while (!undoDone && (notepad == null || !notepad.CurrentText.Contains("Red")))
            {
                if (notepad == null || !notepad.gameObject.activeInHierarchy)
                {
                    notepad = RecoverNotepadWindow(ref winRect, "Red Blue Green Blue");
                    if (notepad != null)
                    {
                        notepad.PushUndoState();
                        notepad.SetText("Blue Green Blue");
                    }
                }

                // Error: pressing Z without Ctrl
                if (IsKeyWithoutCtrl(UnityEngine.InputSystem.Key.Z, KeyCode.Z))
                {
                    RecordMistake("Hold down Ctrl and press Z to undo!", winRect);
                }

                yield return null;
            }
            CleanupActiveSubscriptions();
            SoundManager.PlaySuccess();
            yield return new WaitForSeconds(1.2f);

            // Step 5: Save
            VirtualMascot.Show("Step 5 of 5: Finally, press Ctrl + S and click SAVE to store your finished note!", winRect, new Vector2(0, 180));
            tutorialBlocker?.ClearTarget();

            bool saved = false;
            Action onFs = () => { saved = true; };
            FileExplorerApp.OnFileSystemChanged += onFs;
            RegisterCleanup(() => FileExplorerApp.OnFileSystemChanged -= onFs);

            while (!saved)
            {
                if (IsKeyWithoutCtrl(UnityEngine.InputSystem.Key.S, KeyCode.S))
                {
                    RecordMistake("Press Ctrl and S together to save your document!", winRect);
                }
                yield return null;
            }
            CleanupActiveSubscriptions();

            SoundManager.PlaySuccess();
            VirtualMascot.Show("Outstanding mastery! You aced all Notepad shortcuts and operations!", winRect, new Vector2(0, 180));
            yield return new WaitForSeconds(2.8f);

            CompleteLevel(levelDef);
        }
        #endregion

        #region Level 16: Paint Usage
        private IEnumerator RunLevel16PaintUsage(LevelDefinition levelDef)
        {
            yield return new WaitForSeconds(0.6f);

            DraggableIcon paintIcon = DesktopManager.Instance?.GetIconByName("Paint");
            RectTransform iconRect = paintIcon != null ? paintIcon.GetComponent<RectTransform>() : null;

            VirtualMascot.Show("Level 16: Digital Artist in Paint!\nDouble-click the Paint icon to open the drawing studio.", iconRect, new Vector2(160, -80));
            tutorialBlocker?.SetAllowedTarget(iconRect);

            PaintApp paint = null;
            while (paint == null || !paint.gameObject.activeInHierarchy)
            {
                paint = FindAnyObjectByType<PaintApp>();
                yield return null;
            }

            Window win = paint.GetComponentInParent<Window>();
            RectTransform winRect = win != null ? win.GetComponent<RectTransform>() : paint.GetComponent<RectTransform>();
            tutorialBlocker?.SetAllowedTarget(winRect);

            // Step 1: Draw
            VirtualMascot.Show("Pick any color from the palette and draw a shape or smile on the canvas using your mouse!", winRect, new Vector2(0, 180));

            bool strokeDrawn = false;
            Action onStroke = () => { strokeDrawn = true; };
            PaintApp.OnStrokeDrawn += onStroke;
            RegisterCleanup(() => PaintApp.OnStrokeDrawn -= onStroke);

            while (!strokeDrawn)
            {
                if (paint == null || !paint.gameObject.activeInHierarchy)
                {
                    paint = RecoverPaintWindow(ref winRect);
                }
                yield return null;
            }
            CleanupActiveSubscriptions();

            SoundManager.PlaySuccess();
            VirtualMascot.Show("Nice drawing! Now let's test undoing a brush stroke.", winRect, new Vector2(0, 180));
            yield return new WaitForSeconds(2.0f);

            // Step 2: Undo
            VirtualMascot.Show("Press Ctrl + Z to Undo your last stroke!", winRect, new Vector2(0, 180));

            bool undone = false;
            Action onUndo = () => { undone = true; };
            PaintApp.OnPaintUndo += onUndo;
            RegisterCleanup(() => PaintApp.OnPaintUndo -= onUndo);

            while (!undone)
            {
                if (paint == null || !paint.gameObject.activeInHierarchy)
                {
                    paint = RecoverPaintWindow(ref winRect);
                }

                if (IsKeyWithoutCtrl(UnityEngine.InputSystem.Key.Z, KeyCode.Z))
                {
                    RecordMistake("Hold down Ctrl and press Z to undo your brush stroke!", winRect);
                }

                yield return null;
            }
            CleanupActiveSubscriptions();

            SoundManager.PlaySuccess();
            VirtualMascot.Show("Stroke undone! You can undo any drawing mistake with Ctrl + Z.", winRect, new Vector2(0, 180));
            yield return new WaitForSeconds(2.0f);

            // Step 3: Save
            VirtualMascot.Show("Now press Ctrl + S and confirm in the Save dialog to store your artwork in Pictures!", winRect, new Vector2(0, 180));
            tutorialBlocker?.ClearTarget();

            bool saved = false;
            Action onFs = () => { saved = true; };
            FileExplorerApp.OnFileSystemChanged += onFs;
            RegisterCleanup(() => FileExplorerApp.OnFileSystemChanged -= onFs);

            while (!saved)
            {
                if (IsKeyWithoutCtrl(UnityEngine.InputSystem.Key.S, KeyCode.S))
                {
                    RecordMistake("Press Ctrl and S together to save your artwork!", winRect);
                }
                yield return null;
            }
            CleanupActiveSubscriptions();

            SoundManager.PlaySuccess();
            VirtualMascot.Show("Magnificent! You have mastered all mouse, keyboard, and creative tools on the Virtual Desktop!", winRect, new Vector2(0, 180));
            yield return new WaitForSeconds(3.0f);

            CompleteLevel(levelDef);
        }
        #endregion

        #region Helpers & Level Completion
        private NotepadApp RecoverNotepadWindow(ref RectTransform winRect, string restoreText = null)
        {
            NotepadApp notepad = EnsureNotepadOpen();
            if (notepad != null)
            {
                Window w = notepad.GetComponentInParent<Window>();
                winRect = w != null ? w.GetComponent<RectTransform>() : notepad.GetComponent<RectTransform>();
                if (restoreText != null)
                {
                    notepad.SetText(restoreText);
                }
                tutorialBlocker?.SetAllowedTarget(winRect);
            }
            return notepad;
        }

        private PaintApp RecoverPaintWindow(ref RectTransform winRect)
        {
            PaintApp paint = FindAnyObjectByType<PaintApp>();
            if (paint == null || !paint.gameObject.activeInHierarchy)
            {
                if (StartMenuController.Instance != null)
                {
                    StartMenuController.Instance.OpenPaint();
                }
                else
                {
                    DraggableIcon icon = DesktopManager.Instance?.GetIconByName("Paint");
                    if (icon != null)
                    {
                        icon.OnPointerClick(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
                        {
                            button = UnityEngine.EventSystems.PointerEventData.InputButton.Left,
                            clickCount = 2
                        });
                    }
                }
                paint = FindAnyObjectByType<PaintApp>();
            }

            if (paint != null)
            {
                Window w = paint.GetComponentInParent<Window>();
                winRect = w != null ? w.GetComponent<RectTransform>() : paint.GetComponent<RectTransform>();
                tutorialBlocker?.SetAllowedTarget(winRect);
            }
            return paint;
        }

        private NotepadApp EnsureNotepadOpen()
        {
            NotepadApp notepad = FindAnyObjectByType<NotepadApp>();
            if (notepad != null && notepad.gameObject.activeInHierarchy)
            {
                Window w = notepad.GetComponentInParent<Window>();
                if (w != null)
                {
                    w.Restore();
                    w.BringToFront();
                }
                return notepad;
            }

            if (StartMenuController.Instance != null)
            {
                StartMenuController.Instance.OpenNotepad();
            }
            else
            {
                DraggableIcon icon = DesktopManager.Instance?.GetIconByName("Notepad");
                if (icon != null)
                {
                    icon.OnPointerClick(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
                    {
                        button = UnityEngine.EventSystems.PointerEventData.InputButton.Left,
                        clickCount = 2
                    });
                }
            }

            return FindAnyObjectByType<NotepadApp>();
        }

        private void CompleteLevel(LevelDefinition levelDef)
        {
            CleanupActiveSubscriptions();

            if (tutorialBlocker != null)
            {
                tutorialBlocker.ClearTarget();
                Destroy(tutorialBlocker.gameObject);
                tutorialBlocker = null;
            }

            float duration = Mathf.Max(2.5f, Time.time - levelStartTime);
            int mistakes = currentLevelMistakes;
            int totalActions = Mathf.Max(1, 5 + mistakes);
            float accuracy = Mathf.Clamp(((float)5 / totalActions) * 100f, 0f, 100f);
            LevelMedal medal = ProgressData.CalculateMedal(accuracy, mistakes);

            if (ProgressData.Instance != null && levelDef != null && !string.IsNullOrEmpty(levelDef.levelId))
            {
                ProgressData.Instance.RecordLevelAttempt(levelDef.levelId, medal, accuracy, duration, mistakes);
                ProgressData.Instance.MarkLevelCompleted(levelDef.levelId);
            }

            IsLevelRunning = false;
            CurrentRunningLevelId = null;
            activeLevelCoroutine = null;

            // Spawn celebration modal on desktop Canvas
            Transform modalParent = null;
            if (DesktopManager.Instance != null && DesktopManager.Instance.DesktopIconContainer != null)
            {
                modalParent = DesktopManager.Instance.DesktopIconContainer.parent;
            }
            if (modalParent == null)
            {
                Canvas canvas = FindAnyObjectByType<Canvas>();
                if (canvas != null) modalParent = canvas.transform;
            }

            LevelCelebrationModal.Show(modalParent, levelDef, medal, accuracy, duration, mistakes, () =>
            {
                VirtualMascot.HideMascot();
            });
        }

        public static LevelDefinition GetOrCreateLevelDefinition(string levelId)
        {
            if (string.IsNullOrEmpty(levelId)) return null;

            // Try loading from Resources
            LevelDefinition loaded = Resources.Load<LevelDefinition>($"Levels/L_{char.ToUpper(levelId[0])}{levelId.Substring(1)}");
            if (loaded == null)
            {
                loaded = Resources.Load<LevelDefinition>($"Levels/{levelId}");
            }
            if (loaded != null) return loaded;

            // Create in-memory fallback
            LevelDefinition def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.levelId = levelId.ToLower().Trim();
            def.isAdaptive = false;

            switch (def.levelId)
            {
                case "level10":
                    def.displayName = "Level 10: Context Menu";
                    def.instructionText = "Learn how to use the desktop Context Menu!\nRight-click on the desktop to explore options.";
                    break;
                case "level11":
                    def.displayName = "Level 11: Open Notepad & Typing";
                    def.instructionText = "Learn how to open Notepad and type phrases on your keyboard!";
                    break;
                case "level12":
                    def.displayName = "Level 12: Copy, Paste & Cut";
                    def.instructionText = "Learn keyboard shortcuts!\nCtrl + C to copy, Ctrl + V to paste, and Ctrl + X to cut.";
                    break;
                case "level13":
                    def.displayName = "Level 13: Undo Shortcut";
                    def.instructionText = "Learn how to undo mistakes with Ctrl + Z!";
                    break;
                case "level14":
                    def.displayName = "Level 14: Save Shortcut";
                    def.instructionText = "Learn how to safely save files with Ctrl + S!";
                    break;
                case "level15":
                    def.displayName = "Level 15: Notepad Evaluation";
                    def.instructionText = "Complete test: type, copy, cut, undo, and save your document in Notepad!";
                    break;
                case "level16":
                    def.displayName = "Level 16: Paint Usage";
                    def.instructionText = "Open Paint, draw artwork, undo with Ctrl + Z, and save with Ctrl + S!";
                    break;
                default:
                    def.displayName = $"Level {def.levelId}";
                    def.instructionText = "Follow the virtual mascot instructions.";
                    break;
            }

            return def;
        }
        #endregion
    }
}
