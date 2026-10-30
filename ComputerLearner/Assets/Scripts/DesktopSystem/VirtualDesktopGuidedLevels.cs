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
    [Serializable]
    public class GuidedLevelsDialogs
    {
        public string l10_welcome = "Welcome to Level 10: Context Menu!\nRight-click anywhere on the desktop wallpaper to open the options menu.";
        public string l10_wallpaperRightClickReminder = "Click the right mouse button on the desktop wallpaper to open the menu!";
        public string l10_clickMenu = "Awesome! This is the Context Menu.\nClick on '+ New Text Document (.txt)' or '+ New Folder' to create an item!";
        public string l10_menuClosedError = "The menu closed! Right-click again on the desktop and select an option!";
        public string l10_menuSelectReminder = "Click on '+ New Text Document (.txt)' or '+ New Folder' inside the menu!";
        public string l10_done = "Fantastic! You've learned how to use the desktop context menu to create and customize files!";

        public string l11_openNotepad = "Level 11: Word Processing!\nDouble-click the Notepad icon to open the text editor.";
        public string l11_openReminder = "Double-click quickly with the left mouse button on the Notepad icon to open it!";
        public string l11_typePrompt = "Now let's practice typing on your keyboard!\nClick inside the page and type: 'Hello Computer!'";
        public string l11_clickInsideError = "Click inside the blank notepad area first so your cursor is ready to type!";
        public string l11_partialHelloReminder = "Great start! You typed 'Hello' - now add ' Computer!' to complete the phrase!";
        public string l11_typeReminder = "Click inside the white page and type 'Hello Computer!' on your keyboard.";
        public string l11_done = "Terrific! You typed the phrase perfectly!";

        public string l12_copyPrompt = "Let's master keyboard shortcuts!\nHighlight the word 'Cat' and press Ctrl + C on your keyboard to copy it.";
        public string l12_restoreHoldCtrlC = "Hold down the Ctrl key while pressing C! I brought 'Cat Dog' back so you can try again.";
        public string l12_restoreWordsCopy = "Oops! We need those words to practice! I restored 'Cat Dog' for you. Highlight 'Cat' and press Ctrl + C!";
        public string l12_holdCtrlC = "Remember to hold down the Ctrl key and press C at the same time to copy!";
        public string l12_copyFirstBeforePaste = "You need to copy 'Cat' first with Ctrl + C before pasting!";
        public string l12_copyReminder = "Drag your mouse across 'Cat' to highlight it, then hold Ctrl and press C!";
        public string l12_copyDone = "Nice! 'Cat' is copied into your clipboard.";
        public string l12_pastePrompt = "Now click at the end of the text and press Ctrl + V to Paste 'Cat'!";
        public string l12_restoreHoldCtrlV = "Hold down the Ctrl key while pressing V! I restored 'Cat Dog' for you.";
        public string l12_restoreWordsPaste = "Oops! Don't erase the words! I brought 'Cat Dog' back - just press Ctrl + V to paste!";
        public string l12_holdCtrlV = "Hold down the Ctrl key and press V together to paste!";
        public string l12_alreadyCopied = "'Cat' is already copied! Now press Ctrl + V to Paste it!";
        public string l12_pasteReminder = "Hold down Ctrl and press V on your keyboard to paste 'Cat'!";
        public string l12_pasteDone = "You pasted it! Now you have two Cats!";
        public string l12_cutPrompt = "Now highlight the word 'Dog' and press Ctrl + X to Cut it away!";
        public string l12_restoreHoldCtrlX = "Hold down the Ctrl key while pressing X! I brought 'Dog' back so you can Cut it with Ctrl + X.";
        public string l12_restoreBackspaceDog = "Don't erase 'Dog' with Backspace! I brought 'Cat Dog Cat' back - highlight 'Dog' and press Ctrl + X to Cut it!";
        public string l12_keepCatCutDog = "Oops! Keep the 'Cat' words and only highlight 'Dog', then press Ctrl + X!";
        public string l12_holdCtrlX = "Hold down Ctrl and press X together after highlighting 'Dog'!";
        public string l12_cutReminder = "Highlight 'Dog' with your mouse and press Ctrl + X to cut it!";
        public string l12_done = "Brilliant! Ctrl + X cut the text and saved it to your clipboard!";

        public string l13_welcome = "Welcome to Level 13!\nLook at this sentence: 'Computer Science is fun!'";
        public string l13_undoPrompt = "Oh no! Someone accidentally made a mistake!\nPress Ctrl + Z to Undo the mistake and restore your original words!";
        public string l13_restoreHoldCtrlZ = "Hold down the Control (Ctrl) key while pressing Z! I reset the sentence for you.";
        public string l13_restoreManualErase = "No need to erase it by hand! I reset the sentence - just press Ctrl + Z to fix it automatically!";
        public string l13_holdCtrlZ = "Hold down the Control (Ctrl) key and press Z together!";
        public string l13_undoReminder = "Hold down Ctrl and press Z on your keyboard to Undo the mistake!";
        public string l13_done = "Magic! Ctrl + Z lets you undo mistakes in almost every program!";

        public string l14_saveShortcutPrompt = "Level 14: Save Shortcut!\nAlways save your files. Press Ctrl + S on your keyboard to save this note.";
        public string l14_restoreErasedNote = "Oops! Don't erase the note before saving! I restored it - press Ctrl + S to save!";
        public string l14_holdCtrlS = "Hold down Ctrl and press S together to save your document!";
        public string l14_saveReminder = "Press Ctrl + S on your keyboard to open the Save window!";
        public string l14_saveModalPrompt = "The Save window is open! Type a name for your file (like 'MyNotes') and click SAVE.";
        public string l14_done = "Document saved! Ctrl + S is the most useful shortcut to keep your work safe.";

        public string l15_welcome = "Level 15: The Grand Word Challenge!\nLet's test all your typing and keyboard skills together.";
        public string l15_step1 = "Step 1 of 5: Type the three colors: 'Red Blue Green'";
        public string l15_step1Reminder = "Type the three words 'Red Blue Green' inside the notepad!";
        public string l15_step2 = "Step 2 of 5: Highlight 'Blue', press Ctrl + C to copy it, and press Ctrl + V to paste it!";
        public string l15_step2RestoreHoldCtrl = "Hold down the Ctrl key while pressing C or V! I restored 'Red Blue Green' for you.";
        public string l15_step2RestoreErased = "Oops! Don't erase the colors! I restored 'Red Blue Green' - highlight 'Blue', press Ctrl + C, then Ctrl + V!";
        public string l15_step2HoldCtrl = "Remember to hold down the Ctrl key when pressing C to copy or V to paste!";
        public string l15_step2Reminder = "Highlight 'Blue', press Ctrl + C to copy, and then press Ctrl + V to paste!",
        l15_step3 = "Step 3 of 5: Highlight 'Red' and press Ctrl + X to cut it away!";
        public string l15_step3RestoreHoldCtrlX = "Hold down the Ctrl key while pressing X! I brought 'Red' back so you can Cut it.";
        public string l15_step3RestoreBackspace = "Don't erase 'Red' with Backspace! I brought it back - highlight 'Red' and press Ctrl + X to Cut it!";
        public string l15_step3KeepOthers = "Oops! Keep the other colors and only highlight 'Red', then press Ctrl + X!";
        public string l15_step3HoldCtrlX = "Hold down Ctrl and press X together to cut 'Red'!";
        public string l15_step3Reminder = "Highlight 'Red' with your mouse and press Ctrl + X!";
        public string l15_step4 = "Step 4 of 5: Press Ctrl + Z to Undo the cut and bring 'Red' back!";
        public string l15_step4HoldCtrlZ = "Hold down Ctrl and press Z together to undo!";
        public string l15_step4UseShortcut = "Use the Undo shortcut! Hold Ctrl and press Z to bring 'Red' back automatically!";
        public string l15_step4Reminder = "Press Ctrl + Z on your keyboard to bring 'Red' back!";
        public string l15_step5 = "Step 5 of 5: Finally, press Ctrl + S and click SAVE to store your finished note!";
        public string l15_done = "Outstanding mastery! You aced all Notepad shortcuts and operations!";

        public string l16_openPaint = "Level 16: Digital Artist in Paint!\nDouble-click the Paint icon to open the drawing studio.";
        public string l16_openReminder = "Double-click quickly on the Paint icon to open it!";
        public string l16_drawPrompt = "Pick any color from the palette and draw a shape or smile on the canvas using your mouse!";
        public string l16_drawReminder = "Click and drag your mouse inside the white canvas to draw!";
        public string l16_drawDone = "Nice drawing! Now let's test undoing a brush stroke.";
        public string l16_undoPrompt = "Press Ctrl + Z to Undo your last stroke!";
        public string l16_holdCtrlZ = "Hold down Ctrl and press Z to undo your brush stroke!";
        public string l16_undoDone = "Stroke undone! You can undo any drawing mistake with Ctrl + Z.";
        public string l16_savePrompt = "Now press Ctrl + S and confirm in the Save dialog to store your artwork in Pictures!";
        public string l16_holdCtrlS = "Press Ctrl and S together to save your artwork!";
        public string l16_done = "Magnificent! You have mastered all mouse, keyboard, and creative tools on the Virtual Desktop!";

        public string def_l10_name = "Level 10: Context Menu";
        public string def_l10_desc = "Learn how to use the desktop Context Menu!\nRight-click on the desktop to explore options.";
        public string def_l11_name = "Level 11: Open Notepad & Typing";
        public string def_l11_desc = "Learn how to open Notepad and type phrases on your keyboard!";
        public string def_l12_name = "Level 12: Copy, Paste & Cut";
        public string def_l12_desc = "Learn keyboard shortcuts!\nCtrl + C to copy, Ctrl + V to paste, and Ctrl + X to cut.";
        public string def_l13_name = "Level 13: Undo Shortcut";
        public string def_l13_desc = "Learn how to undo mistakes with Ctrl + Z!";
        public string def_l14_name = "Level 14: Save Shortcut";
        public string def_l14_desc = "Learn how to safely save files with Ctrl + S!";
        public string def_l15_name = "Level 15: Notepad Evaluation";
        public string def_l15_desc = "Complete test: type, copy, cut, undo, and save your document in Notepad!";
        public string def_l16_name = "Level 16: Paint Usage";
        public string def_l16_desc = "Open Paint, draw artwork, undo with Ctrl + Z, and save with Ctrl + S!";
        public string def_default_desc = "Follow the virtual mascot instructions.";
    }

    public class VirtualDesktopGuidedLevels : MonoBehaviour
    {
        public static VirtualDesktopGuidedLevels Instance { get; private set; }
        public static bool IsLevelRunning { get; private set; }
        public static string CurrentRunningLevelId { get; private set; }

        private GuidedLevelsDialogs dialogs;
        private TutorialBlocker tutorialBlocker;
        private Coroutine activeLevelCoroutine;
        private float levelStartTime;
        private int currentLevelMistakes = 0;
        private readonly List<Action> activeEventCleanupActions = new List<Action>();

        public static GuidedLevelsDialogs LoadDialogsStatic()
        {
            TextAsset json = LocalizationManager.LoadLocalizedResource("GuidedLevelsDialogs");
            GuidedLevelsDialogs loaded = json != null ? JsonUtility.FromJson<GuidedLevelsDialogs>(json.text) : null;
            return loaded ?? new GuidedLevelsDialogs();
        }

        private void LoadDialogs()
        {
            dialogs = LoadDialogsStatic();
        }

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

        private static bool IsRightMouseButtonDown()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null)
                return UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonDown(1);
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
            LoadDialogs();
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

            LoadDialogs();
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

            VirtualMascot.Show(dialogs.l10_welcome, bgRect, new Vector2(0, 50));

            // Allow clicking anywhere on the wallpaper
            tutorialBlocker?.ClearTarget();

            float wallpaperInactivityTimer = 0f;
            while (ContextMenuController.Instance == null || !ContextMenuController.Instance.IsMenuOpen)
            {
                if (IsLeftMouseButtonDown())
                {
                    RecordMistake(dialogs.l10_wallpaperRightClickReminder, bgRect, true);
                    wallpaperInactivityTimer = 0f;
                }

                wallpaperInactivityTimer += Time.deltaTime;
                if (wallpaperInactivityTimer >= 7.0f)
                {
                    VirtualMascot.ShowContextualFeedback(dialogs.l10_wallpaperRightClickReminder, bgRect, 3.2f);
                    wallpaperInactivityTimer = 0f;
                }
                yield return null;
            }

            RectTransform menuRect = ContextMenuController.Instance.GetComponent<RectTransform>();
            tutorialBlocker?.SetAllowedTarget(menuRect);

            VirtualMascot.Show(dialogs.l10_clickMenu, menuRect, new Vector2(250, 0));

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
                        RecordMistake(dialogs.l10_menuClosedError, bgRect);
                    }
                    tutorialBlocker?.ClearTarget();
                }
                else if (ContextMenuController.Instance != null && ContextMenuController.Instance.IsMenuOpen)
                {
                    wasMenuOpen = true;
                    tutorialBlocker?.SetAllowedTarget(menuRect);
                }

                menuInactivityTimer += Time.deltaTime;
                if (menuInactivityTimer >= 7.0f && wasMenuOpen)
                {
                    VirtualMascot.ShowContextualFeedback(dialogs.l10_menuSelectReminder, menuRect, 3.2f);
                    menuInactivityTimer = 0f;
                }
                yield return null;
            }

            CleanupActiveSubscriptions();

            SoundManager.PlaySuccess();
            VirtualMascot.Show(dialogs.l10_done, bgRect, new Vector2(0, 50));
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

            VirtualMascot.Show(dialogs.l11_openNotepad, iconRect, new Vector2(160, -80));
            tutorialBlocker?.SetAllowedTarget(iconRect);

            NotepadApp notepad = null;
            float openTimer = 0f;
            while (notepad == null || !notepad.gameObject.activeInHierarchy)
            {
                notepad = FindAnyObjectByType<NotepadApp>();

                if (notepad == null || !notepad.gameObject.activeInHierarchy)
                {
                    if (IsRightMouseButtonDown())
                    {
                        VirtualMascot.ShowWrongClickFeedback(iconRect, false);
                        openTimer = 0f;
                    }

                    openTimer += Time.deltaTime;
                    if (openTimer >= 7.0f)
                    {
                        VirtualMascot.ShowContextualFeedback(dialogs.l11_openReminder, iconRect, 3.2f);
                        openTimer = 0f;
                    }
                }
                yield return null;
            }

            Window notepadWin = notepad.GetComponentInParent<Window>();
            RectTransform winRect = notepadWin != null ? notepadWin.GetComponent<RectTransform>() : notepad.GetComponent<RectTransform>();

            notepad.SetText("");
            notepad.ClearUndoHistory();
            tutorialBlocker?.SetAllowedTarget(winRect);

            VirtualMascot.Show(dialogs.l11_typePrompt, winRect, new Vector2(0, 180));

            float timer = 0f;
            while (notepad == null || !notepad.gameObject.activeInHierarchy ||
                   notepad.CurrentText.IndexOf("Hello Computer", StringComparison.OrdinalIgnoreCase) < 0)
            {
                if (notepad == null || !notepad.gameObject.activeInHierarchy)
                {
                    notepad = RecoverNotepadWindow(ref winRect, "");
                    if (notepad != null) notepad.ClearUndoHistory();
                }

                // Safeguard against accidental default text restoration
                if (notepad != null && notepad.CurrentText.IndexOf("Welcome to Notepad", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    notepad.SetText("");
                    notepad.ClearUndoHistory();
                }

                // Check typing without focus
                if (notepad != null && notepad.InputField != null && !notepad.InputField.isFocused && IsAnyKeyOrTypingWithoutMouse())
                {
                    RecordMistake(dialogs.l11_clickInsideError, winRect);
                    timer = 0f;
                }

                timer += Time.deltaTime;
                if (timer > 8.0f)
                {
                    if (notepad != null && notepad.CurrentText.IndexOf("Hello", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        VirtualMascot.ShowContextualFeedback(dialogs.l11_partialHelloReminder, winRect, 3.5f);
                    }
                    else
                    {
                        VirtualMascot.ShowContextualFeedback(dialogs.l11_typeReminder, winRect, 3.5f);
                    }
                    timer = 0f;
                }
                yield return null;
            }

            SoundManager.PlaySuccess();
            VirtualMascot.Show(dialogs.l11_done, winRect, new Vector2(0, 180));
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

            const string initialWords = "Cat Dog";
            NotepadApp.VirtualClipboard = string.Empty;
            GUIUtility.systemCopyBuffer = string.Empty;
            notepad.SetText(initialWords);
            notepad.ClearUndoHistory();
            tutorialBlocker?.SetAllowedTarget(winRect);

            // Step 1: Copy
            VirtualMascot.Show(dialogs.l12_copyPrompt, winRect, new Vector2(0, 180));

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
                    notepad = RecoverNotepadWindow(ref winRect, initialWords);
                    if (notepad != null) notepad.ClearUndoHistory();
                }

                // Anti-softlock: if the user deleted or overwrote 'Cat' or 'Dog' before copying, restore it immediately
                if (notepad != null &&
                    (notepad.CurrentText.IndexOf("Cat", StringComparison.OrdinalIgnoreCase) < 0 ||
                     notepad.CurrentText.IndexOf("Dog", StringComparison.OrdinalIgnoreCase) < 0))
                {
                    notepad.SetText(initialWords);
                    notepad.ClearUndoHistory();
                    if (IsKeyWithoutCtrl(UnityEngine.InputSystem.Key.C, KeyCode.C))
                    {
                        RecordMistake(dialogs.l12_restoreHoldCtrlC, winRect);
                    }
                    else
                    {
                        VirtualMascot.ShowContextualFeedback(dialogs.l12_restoreWordsCopy, winRect, 3.5f);
                    }
                    copyTimer = 0f;
                }
                else if (IsKeyWithoutCtrl(UnityEngine.InputSystem.Key.C, KeyCode.C))
                {
                    RecordMistake(dialogs.l12_holdCtrlC, winRect);
                    copyTimer = 0f;
                }
                else if (IsCtrlPressed() && IsKeyJustPressed(UnityEngine.InputSystem.Key.V, KeyCode.V))
                {
                    RecordMistake(dialogs.l12_copyFirstBeforePaste, winRect);
                    copyTimer = 0f;
                }

                copyTimer += Time.deltaTime;
                if (copyTimer >= 8.0f)
                {
                    VirtualMascot.ShowContextualFeedback(dialogs.l12_copyReminder, winRect, 3.5f);
                    copyTimer = 0f;
                }
                yield return null;
            }
            CleanupActiveSubscriptions();

            // Ensure clipboard holds solely "Cat" and place caret at the end so pasting appends cleanly
            NotepadApp.VirtualClipboard = "Cat";
            GUIUtility.systemCopyBuffer = "Cat";
            if (notepad != null)
            {
                notepad.SetText("Cat Dog ");
                notepad.ClearUndoHistory();
                if (notepad.InputField != null)
                {
                    int endPos = notepad.CurrentText.Length;
                    notepad.InputField.caretPosition = endPos;
                    notepad.InputField.selectionAnchorPosition = endPos;
                    notepad.InputField.selectionFocusPosition = endPos;
                }
            }

            SoundManager.PlaySuccess();
            VirtualMascot.Show(dialogs.l12_copyDone, winRect, new Vector2(0, 180));
            yield return new WaitForSeconds(2.0f);

            // Step 2: Paste
            VirtualMascot.Show(dialogs.l12_pastePrompt, winRect, new Vector2(0, 180));

            bool pasted = false;
            Action<string> onPaste = (text) => { pasted = true; };
            NotepadApp.OnTextPasted += onPaste;
            RegisterCleanup(() => NotepadApp.OnTextPasted -= onPaste);

            float pasteTimer = 0f;
            while (!pasted && (notepad == null || CountWordOccurrences(notepad.CurrentText, "Cat") < 2))
            {
                if (notepad == null || !notepad.gameObject.activeInHierarchy)
                {
                    notepad = RecoverNotepadWindow(ref winRect, "Cat Dog ");
                    if (notepad != null) notepad.ClearUndoHistory();
                }

                // Keep "Cat" ready in clipboard
                NotepadApp.VirtualClipboard = "Cat";
                GUIUtility.systemCopyBuffer = "Cat";

                // Anti-softlock: if the user erased 'Cat' or 'Dog' instead of pasting, restore 'Cat Dog '
                if (!pasted && notepad != null &&
                    (notepad.CurrentText.IndexOf("Cat", StringComparison.OrdinalIgnoreCase) < 0 ||
                     notepad.CurrentText.IndexOf("Dog", StringComparison.OrdinalIgnoreCase) < 0))
                {
                    notepad.SetText("Cat Dog ");
                    notepad.ClearUndoHistory();
                    if (IsKeyWithoutCtrl(UnityEngine.InputSystem.Key.V, KeyCode.V))
                    {
                        RecordMistake(dialogs.l12_restoreHoldCtrlV, winRect);
                    }
                    else
                    {
                        VirtualMascot.ShowContextualFeedback(dialogs.l12_restoreWordsPaste, winRect, 3.5f);
                    }
                    pasteTimer = 0f;
                }
                else if (IsKeyWithoutCtrl(UnityEngine.InputSystem.Key.V, KeyCode.V))
                {
                    RecordMistake(dialogs.l12_holdCtrlV, winRect);
                    pasteTimer = 0f;
                }
                else if (IsCtrlPressed() && IsKeyJustPressed(UnityEngine.InputSystem.Key.C, KeyCode.C))
                {
                    RecordMistake(dialogs.l12_alreadyCopied, winRect);
                    pasteTimer = 0f;
                }

                pasteTimer += Time.deltaTime;
                if (pasteTimer >= 8.0f)
                {
                    VirtualMascot.ShowContextualFeedback(dialogs.l12_pasteReminder, winRect, 3.5f);
                    pasteTimer = 0f;
                }
                yield return null;
            }
            CleanupActiveSubscriptions();

            // Guarantee clean state "Cat Dog Cat" before Step 3 (in case the user pasted over a selection)
            const string step3Text = "Cat Dog Cat";
            if (notepad != null && (notepad.CurrentText.IndexOf("Dog", StringComparison.OrdinalIgnoreCase) < 0 || CountWordOccurrences(notepad.CurrentText, "Cat") < 2))
            {
                notepad.SetText(step3Text);
                notepad.ClearUndoHistory();
            }

            SoundManager.PlaySuccess();
            VirtualMascot.Show(dialogs.l12_pasteDone, winRect, new Vector2(0, 180));
            yield return new WaitForSeconds(2.0f);

            // Step 3: Cut
            VirtualMascot.Show(dialogs.l12_cutPrompt, winRect, new Vector2(0, 180));

            bool cut = false;
            Action<string> onCut = (text) =>
            {
                if (text != null && text.IndexOf("Dog", StringComparison.OrdinalIgnoreCase) >= 0)
                    cut = true;
            };
            NotepadApp.OnTextCut += onCut;
            RegisterCleanup(() => NotepadApp.OnTextCut -= onCut);

            float cutTimer = 0f;
            while (!cut)
            {
                if (notepad == null || !notepad.gameObject.activeInHierarchy)
                {
                    notepad = RecoverNotepadWindow(ref winRect, step3Text);
                    if (notepad != null) notepad.ClearUndoHistory();
                }

                if (notepad != null)
                {
                    bool hasDog = notepad.CurrentText.IndexOf("Dog", StringComparison.OrdinalIgnoreCase) >= 0;
                    bool hasCat = notepad.CurrentText.IndexOf("Cat", StringComparison.OrdinalIgnoreCase) >= 0;

                    if (!hasDog)
                    {
                        if (IsCtrlPressed() && IsKeyJustPressed(UnityEngine.InputSystem.Key.X, KeyCode.X))
                        {
                            cut = true;
                            break;
                        }
                        else if (!cut)
                        {
                            // Player deleted 'Dog' using Backspace/Delete or typed over it without Ctrl+X! Restore it!
                            notepad.SetText(step3Text);
                            notepad.ClearUndoHistory();
                            if (IsKeyWithoutCtrl(UnityEngine.InputSystem.Key.X, KeyCode.X))
                            {
                                RecordMistake(dialogs.l12_restoreHoldCtrlX, winRect);
                            }
                            else
                            {
                                RecordMistake(dialogs.l12_restoreBackspaceDog, winRect);
                            }
                            cutTimer = 0f;
                        }
                    }
                    else if (!hasCat)
                    {
                        // Player accidentally deleted 'Cat' instead of cutting 'Dog'
                        notepad.SetText(step3Text);
                        notepad.ClearUndoHistory();
                        VirtualMascot.ShowContextualFeedback(dialogs.l12_keepCatCutDog, winRect, 3.5f);
                        cutTimer = 0f;
                    }
                    else if (IsKeyWithoutCtrl(UnityEngine.InputSystem.Key.X, KeyCode.X))
                    {
                        RecordMistake(dialogs.l12_holdCtrlX, winRect);
                        cutTimer = 0f;
                    }
                }

                cutTimer += Time.deltaTime;
                if (cutTimer >= 8.0f)
                {
                    VirtualMascot.ShowContextualFeedback(dialogs.l12_cutReminder, winRect, 3.5f);
                    cutTimer = 0f;
                }
                yield return null;
            }
            CleanupActiveSubscriptions();

            SoundManager.PlaySuccess();
            VirtualMascot.Show(dialogs.l12_done, winRect, new Vector2(0, 180));
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

            const string goodText = "Computer Science is fun!";
            const string mistakeText = "Computer Science is OOPS!";

            notepad.ClearUndoHistory();
            notepad.SetText(goodText);
            notepad.PushUndoState();
            tutorialBlocker?.SetAllowedTarget(winRect);

            VirtualMascot.Show(dialogs.l13_welcome, winRect, new Vector2(0, 180));
            yield return new WaitForSeconds(3.0f);

            if (notepad == null || !notepad.gameObject.activeInHierarchy)
            {
                notepad = RecoverNotepadWindow(ref winRect, goodText);
            }

            // Simulate an accidental mistake with a clean undo stack
            if (notepad != null)
            {
                notepad.ClearUndoHistory();
                notepad.SetText(goodText);
                notepad.PushUndoState();
                notepad.SetText(mistakeText);
            }
            SoundManager.PlayError();

            VirtualMascot.Show(dialogs.l13_undoPrompt, winRect, new Vector2(0, 180));

            bool undone = false;
            Action onUndo = () => { undone = true; };
            NotepadApp.OnTextUndo += onUndo;
            RegisterCleanup(() => NotepadApp.OnTextUndo -= onUndo);

            float undoTimer = 0f;
            while (!undone)
            {
                if (notepad == null || !notepad.gameObject.activeInHierarchy)
                {
                    notepad = RecoverNotepadWindow(ref winRect, goodText);
                    if (notepad != null)
                    {
                        notepad.ClearUndoHistory();
                        notepad.SetText(goodText);
                        notepad.PushUndoState();
                        notepad.SetText(mistakeText);
                    }
                }

                if (notepad != null && !undone)
                {
                    if (IsCtrlPressed() && IsKeyJustPressed(UnityEngine.InputSystem.Key.Z, KeyCode.Z))
                    {
                        notepad.SetText(goodText);
                        undone = true;
                        break;
                    }

                    // Anti-softlock: if the user manually erased or altered the sentence instead of pressing Ctrl+Z, reset the mistake and undo history!
                    if (notepad.CurrentText != mistakeText && !notepad.CurrentText.Equals(goodText, StringComparison.OrdinalIgnoreCase))
                    {
                        notepad.ClearUndoHistory();
                        notepad.SetText(goodText);
                        notepad.PushUndoState();
                        notepad.SetText(mistakeText);

                        if (IsKeyWithoutCtrl(UnityEngine.InputSystem.Key.Z, KeyCode.Z))
                        {
                            RecordMistake(dialogs.l13_restoreHoldCtrlZ, winRect);
                        }
                        else
                        {
                            VirtualMascot.ShowContextualFeedback(dialogs.l13_restoreManualErase, winRect, 3.5f);
                        }
                        undoTimer = 0f;
                    }
                    else if (IsKeyWithoutCtrl(UnityEngine.InputSystem.Key.Z, KeyCode.Z))
                    {
                        RecordMistake(dialogs.l13_holdCtrlZ, winRect);
                        undoTimer = 0f;
                    }
                }

                undoTimer += Time.deltaTime;
                if (undoTimer >= 8.0f)
                {
                    VirtualMascot.ShowContextualFeedback(dialogs.l13_undoReminder, winRect, 3.5f);
                    undoTimer = 0f;
                }
                yield return null;
            }
            CleanupActiveSubscriptions();

            if (notepad != null) notepad.SetText(goodText);

            SoundManager.PlaySuccess();
            VirtualMascot.Show(dialogs.l13_done, winRect, new Vector2(0, 180));
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

            const string noteToSave = "Important Note: Always save your files before closing the app!";
            notepad.SetText(noteToSave);
            notepad.ClearUndoHistory();
            tutorialBlocker?.SetAllowedTarget(winRect);

            VirtualMascot.Show(dialogs.l14_saveShortcutPrompt, winRect, new Vector2(0, 180));

            bool saveOpened = false;
            Action onSaveKey = () => { saveOpened = true; };
            NotepadApp.OnSaveShortcut += onSaveKey;
            RegisterCleanup(() => NotepadApp.OnSaveShortcut -= onSaveKey);

            float saveShortcutTimer = 0f;
            while (!saveOpened && !FileExplorerApp.IsSaveDialogOpen)
            {
                if (notepad == null || !notepad.gameObject.activeInHierarchy)
                {
                    notepad = RecoverNotepadWindow(ref winRect, noteToSave);
                }

                // Anti-softlock: if the user deleted the note before saving, restore it
                if (notepad != null && string.IsNullOrWhiteSpace(notepad.CurrentText))
                {
                    notepad.SetText(noteToSave);
                    notepad.ClearUndoHistory();
                    VirtualMascot.ShowContextualFeedback(dialogs.l14_restoreErasedNote, winRect, 3.5f);
                    saveShortcutTimer = 0f;
                }

                // Error: pressing S without Ctrl
                if (IsKeyWithoutCtrl(UnityEngine.InputSystem.Key.S, KeyCode.S))
                {
                    RecordMistake(dialogs.l14_holdCtrlS, winRect);
                    saveShortcutTimer = 0f;
                }

                saveShortcutTimer += Time.deltaTime;
                if (saveShortcutTimer >= 8.0f)
                {
                    VirtualMascot.ShowContextualFeedback(dialogs.l14_saveReminder, winRect, 3.5f);
                    saveShortcutTimer = 0f;
                }
                yield return null;
            }
            CleanupActiveSubscriptions();

            yield return new WaitForSeconds(0.4f);

            // Allow clicking on Save dialog
            tutorialBlocker?.ClearTarget();
            VirtualMascot.Show(dialogs.l14_saveModalPrompt, winRect, new Vector2(0, 180));

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
            VirtualMascot.Show(dialogs.l14_done, winRect, new Vector2(0, 180));
            yield return new WaitForSeconds(2.8f);

            CompleteLevel(levelDef);
        }
        #endregion

        #region Level 15: Notepad Usage (Evaluación Completa)
        private IEnumerator RunLevel15NotepadFull(LevelDefinition levelDef)
        {
            yield return new WaitForSeconds(0.6f);

            VirtualMascot.Show(dialogs.l15_welcome, null, new Vector2(0, 80));
            yield return new WaitForSeconds(3.0f);

            NotepadApp notepad = EnsureNotepadOpen();
            while (notepad == null || !notepad.gameObject.activeInHierarchy)
            {
                notepad = FindAnyObjectByType<NotepadApp>();
                yield return null;
            }

            Window win = notepad.GetComponentInParent<Window>();
            RectTransform winRect = win != null ? win.GetComponent<RectTransform>() : notepad.GetComponent<RectTransform>();

            NotepadApp.VirtualClipboard = string.Empty;
            GUIUtility.systemCopyBuffer = string.Empty;
            notepad.SetText("");
            notepad.ClearUndoHistory();
            tutorialBlocker?.SetAllowedTarget(winRect);

            // Step 1: Type "Red Blue Green"
            VirtualMascot.Show(dialogs.l15_step1, winRect, new Vector2(0, 180));
            float step1Timer = 0f;
            while (notepad == null || !notepad.gameObject.activeInHierarchy ||
                   notepad.CurrentText.IndexOf("Red", StringComparison.OrdinalIgnoreCase) < 0 ||
                   notepad.CurrentText.IndexOf("Blue", StringComparison.OrdinalIgnoreCase) < 0 ||
                   notepad.CurrentText.IndexOf("Green", StringComparison.OrdinalIgnoreCase) < 0)
            {
                if (notepad == null || !notepad.gameObject.activeInHierarchy)
                {
                    notepad = RecoverNotepadWindow(ref winRect, "");
                    if (notepad != null) notepad.ClearUndoHistory();
                }

                if (notepad != null && notepad.InputField != null && !notepad.InputField.isFocused && IsAnyKeyOrTypingWithoutMouse())
                {
                    RecordMistake(dialogs.l11_clickInsideError, winRect);
                    step1Timer = 0f;
                }

                step1Timer += Time.deltaTime;
                if (step1Timer >= 8.0f)
                {
                    VirtualMascot.ShowContextualFeedback(dialogs.l15_step1Reminder, winRect, 3.5f);
                    step1Timer = 0f;
                }
                yield return null;
            }

            const string baseColors = "Red Blue Green";
            if (notepad != null)
            {
                notepad.SetText(baseColors + " ");
                notepad.ClearUndoHistory();
            }

            SoundManager.PlaySuccess();
            yield return new WaitForSeconds(1.2f);

            // Step 2: Copy & Paste 'Blue'
            VirtualMascot.Show(dialogs.l15_step2, winRect, new Vector2(0, 180));
            bool pasteDone = false;
            Action<string> onPaste = (t) =>
            {
                if (t != null && t.IndexOf("Blue", StringComparison.OrdinalIgnoreCase) >= 0)
                    pasteDone = true;
            };
            NotepadApp.OnTextPasted += onPaste;
            RegisterCleanup(() => NotepadApp.OnTextPasted -= onPaste);

            float step2Timer = 0f;
            while (!pasteDone && (notepad == null || CountWordOccurrences(notepad.CurrentText, "Blue") < 2))
            {
                if (notepad == null || !notepad.gameObject.activeInHierarchy)
                {
                    notepad = RecoverNotepadWindow(ref winRect, baseColors + " ");
                    if (notepad != null) notepad.ClearUndoHistory();
                }

                // Anti-softlock: if the user erased Red, Blue, or Green during Step 2, restore "Red Blue Green "
                if (!pasteDone && notepad != null &&
                    (notepad.CurrentText.IndexOf("Red", StringComparison.OrdinalIgnoreCase) < 0 ||
                     notepad.CurrentText.IndexOf("Blue", StringComparison.OrdinalIgnoreCase) < 0 ||
                     notepad.CurrentText.IndexOf("Green", StringComparison.OrdinalIgnoreCase) < 0))
                {
                    notepad.SetText(baseColors + " ");
                    notepad.ClearUndoHistory();
                    if (IsKeyWithoutCtrl(UnityEngine.InputSystem.Key.C, KeyCode.C) || IsKeyWithoutCtrl(UnityEngine.InputSystem.Key.V, KeyCode.V))
                    {
                        RecordMistake(dialogs.l15_step2RestoreHoldCtrl, winRect);
                    }
                    else
                    {
                        VirtualMascot.ShowContextualFeedback(dialogs.l15_step2RestoreErased, winRect, 3.5f);
                    }
                    step2Timer = 0f;
                }
                else if (IsKeyWithoutCtrl(UnityEngine.InputSystem.Key.C, KeyCode.C) || IsKeyWithoutCtrl(UnityEngine.InputSystem.Key.V, KeyCode.V))
                {
                    RecordMistake(dialogs.l15_step2HoldCtrl, winRect);
                    step2Timer = 0f;
                }

                step2Timer += Time.deltaTime;
                if (step2Timer >= 8.0f)
                {
                    VirtualMascot.ShowContextualFeedback(dialogs.l15_step2Reminder, winRect, 3.5f);
                    step2Timer = 0f;
                }
                yield return null;
            }
            CleanupActiveSubscriptions();

            const string step3Colors = "Red Blue Green Blue";
            if (notepad != null && (notepad.CurrentText.IndexOf("Red", StringComparison.OrdinalIgnoreCase) < 0 || CountWordOccurrences(notepad.CurrentText, "Blue") < 2))
            {
                notepad.SetText(step3Colors);
                notepad.ClearUndoHistory();
            }

            SoundManager.PlaySuccess();
            yield return new WaitForSeconds(1.2f);

            // Step 3: Cut 'Red'
            VirtualMascot.Show(dialogs.l15_step3, winRect, new Vector2(0, 180));
            bool cutDone = false;
            Action<string> onCut = (t) =>
            {
                if (t != null && t.IndexOf("Red", StringComparison.OrdinalIgnoreCase) >= 0)
                    cutDone = true;
            };
            NotepadApp.OnTextCut += onCut;
            RegisterCleanup(() => NotepadApp.OnTextCut -= onCut);

            float step3Timer = 0f;
            while (!cutDone)
            {
                if (notepad == null || !notepad.gameObject.activeInHierarchy)
                {
                    notepad = RecoverNotepadWindow(ref winRect, step3Colors);
                    if (notepad != null) notepad.ClearUndoHistory();
                }

                if (notepad != null)
                {
                    bool hasRed = notepad.CurrentText.IndexOf("Red", StringComparison.OrdinalIgnoreCase) >= 0;
                    bool hasBlue = notepad.CurrentText.IndexOf("Blue", StringComparison.OrdinalIgnoreCase) >= 0;

                    if (!hasRed)
                    {
                        if (IsCtrlPressed() && IsKeyJustPressed(UnityEngine.InputSystem.Key.X, KeyCode.X))
                        {
                            cutDone = true;
                            break;
                        }
                        else if (!cutDone)
                        {
                            // Player erased 'Red' with Backspace/Delete instead of Ctrl+X! Restore it!
                            notepad.SetText(step3Colors);
                            notepad.ClearUndoHistory();
                            if (IsKeyWithoutCtrl(UnityEngine.InputSystem.Key.X, KeyCode.X))
                            {
                                RecordMistake(dialogs.l15_step3RestoreHoldCtrlX, winRect);
                            }
                            else
                            {
                                RecordMistake(dialogs.l15_step3RestoreBackspace, winRect);
                            }
                            step3Timer = 0f;
                        }
                    }
                    else if (!hasBlue)
                    {
                        notepad.SetText(step3Colors);
                        notepad.ClearUndoHistory();
                        VirtualMascot.ShowContextualFeedback(dialogs.l15_step3KeepOthers, winRect, 3.5f);
                        step3Timer = 0f;
                    }
                    else if (IsKeyWithoutCtrl(UnityEngine.InputSystem.Key.X, KeyCode.X))
                    {
                        RecordMistake(dialogs.l15_step3HoldCtrlX, winRect);
                        step3Timer = 0f;
                    }
                }

                step3Timer += Time.deltaTime;
                if (step3Timer >= 8.0f)
                {
                    VirtualMascot.ShowContextualFeedback(dialogs.l15_step3Reminder, winRect, 3.5f);
                    step3Timer = 0f;
                }
                yield return null;
            }
            CleanupActiveSubscriptions();

            // Prime undo state cleanly so Step 4 (Ctrl+Z) is guaranteed to restore "Red Blue Green Blue"
            const string cutResultText = "Blue Green Blue";
            if (notepad != null)
            {
                notepad.ClearUndoHistory();
                notepad.SetText(step3Colors);
                notepad.PushUndoState();
                notepad.SetText(cutResultText);
            }

            SoundManager.PlaySuccess();
            yield return new WaitForSeconds(1.2f);

            // Step 4: Undo
            VirtualMascot.Show(dialogs.l15_step4, winRect, new Vector2(0, 180));
            bool undoDone = false;
            Action onUndo = () => { undoDone = true; };
            NotepadApp.OnTextUndo += onUndo;
            RegisterCleanup(() => NotepadApp.OnTextUndo -= onUndo);

            float step4Timer = 0f;
            while (!undoDone)
            {
                if (notepad == null || !notepad.gameObject.activeInHierarchy)
                {
                    notepad = RecoverNotepadWindow(ref winRect, step3Colors);
                    if (notepad != null)
                    {
                        notepad.ClearUndoHistory();
                        notepad.SetText(step3Colors);
                        notepad.PushUndoState();
                        notepad.SetText(cutResultText);
                    }
                }

                if (notepad != null && !undoDone)
                {
                    if (IsCtrlPressed() && IsKeyJustPressed(UnityEngine.InputSystem.Key.Z, KeyCode.Z))
                    {
                        notepad.SetText(step3Colors);
                        undoDone = true;
                        break;
                    }

                    // Anti-softlock: if the user manually edited or deleted the text instead of pressing Ctrl+Z, reset the undo stack
                    if (notepad.CurrentText != cutResultText && notepad.CurrentText.IndexOf("Red", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        notepad.ClearUndoHistory();
                        notepad.SetText(step3Colors);
                        notepad.PushUndoState();
                        notepad.SetText(cutResultText);

                        if (IsKeyWithoutCtrl(UnityEngine.InputSystem.Key.Z, KeyCode.Z))
                        {
                            RecordMistake(dialogs.l15_step4HoldCtrlZ, winRect);
                        }
                        else
                        {
                            VirtualMascot.ShowContextualFeedback(dialogs.l15_step4UseShortcut, winRect, 3.5f);
                        }
                        step4Timer = 0f;
                    }
                    else if (IsKeyWithoutCtrl(UnityEngine.InputSystem.Key.Z, KeyCode.Z))
                    {
                        RecordMistake(dialogs.l15_step4HoldCtrlZ, winRect);
                        step4Timer = 0f;
                    }
                }

                step4Timer += Time.deltaTime;
                if (step4Timer >= 8.0f)
                {
                    VirtualMascot.ShowContextualFeedback(dialogs.l15_step4Reminder, winRect, 3.5f);
                    step4Timer = 0f;
                }
                yield return null;
            }
            CleanupActiveSubscriptions();

            if (notepad != null) notepad.SetText(step3Colors);

            SoundManager.PlaySuccess();
            yield return new WaitForSeconds(1.2f);

            // Step 5: Save
            VirtualMascot.Show(dialogs.l15_step5, winRect, new Vector2(0, 180));
            tutorialBlocker?.ClearTarget();

            bool saved = false;
            Action onFs = () => { saved = true; };
            FileExplorerApp.OnFileSystemChanged += onFs;
            RegisterCleanup(() => FileExplorerApp.OnFileSystemChanged -= onFs);

            while (!saved)
            {
                if (notepad != null && string.IsNullOrWhiteSpace(notepad.CurrentText) && !FileExplorerApp.IsSaveDialogOpen)
                {
                    notepad.SetText(step3Colors);
                    notepad.ClearUndoHistory();
                }

                if (IsKeyWithoutCtrl(UnityEngine.InputSystem.Key.S, KeyCode.S) && !FileExplorerApp.IsSaveDialogOpen)
                {
                    RecordMistake(dialogs.l14_holdCtrlS, winRect);
                }
                yield return null;
            }
            CleanupActiveSubscriptions();

            SoundManager.PlaySuccess();
            VirtualMascot.Show(dialogs.l15_done, winRect, new Vector2(0, 180));
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

            VirtualMascot.Show(dialogs.l16_openPaint, iconRect, new Vector2(160, -80));
            tutorialBlocker?.SetAllowedTarget(iconRect);

            PaintApp paint = null;
            float openTimer = 0f;
            while (paint == null || !paint.gameObject.activeInHierarchy)
            {
                paint = FindAnyObjectByType<PaintApp>();

                if (paint == null || !paint.gameObject.activeInHierarchy)
                {
                    if (IsRightMouseButtonDown())
                    {
                        VirtualMascot.ShowWrongClickFeedback(iconRect, false);
                        openTimer = 0f;
                    }

                    openTimer += Time.deltaTime;
                    if (openTimer >= 7.0f)
                    {
                        VirtualMascot.ShowContextualFeedback(dialogs.l16_openReminder, iconRect, 3.2f);
                        openTimer = 0f;
                    }
                }
                yield return null;
            }

            Window win = paint.GetComponentInParent<Window>();
            RectTransform winRect = win != null ? win.GetComponent<RectTransform>() : paint.GetComponent<RectTransform>();
            tutorialBlocker?.SetAllowedTarget(winRect);

            // Step 1: Draw
            VirtualMascot.Show(dialogs.l16_drawPrompt, winRect, new Vector2(0, 180));

            bool strokeDrawn = false;
            Action onStroke = () => { strokeDrawn = true; };
            PaintApp.OnStrokeDrawn += onStroke;
            RegisterCleanup(() => PaintApp.OnStrokeDrawn -= onStroke);

            float drawTimer = 0f;
            while (!strokeDrawn)
            {
                if (paint == null || !paint.gameObject.activeInHierarchy)
                {
                    paint = RecoverPaintWindow(ref winRect);
                }

                drawTimer += Time.deltaTime;
                if (drawTimer >= 8.0f)
                {
                    VirtualMascot.ShowContextualFeedback(dialogs.l16_drawReminder, winRect, 3.2f);
                    drawTimer = 0f;
                }
                yield return null;
            }
            CleanupActiveSubscriptions();

            SoundManager.PlaySuccess();
            VirtualMascot.Show(dialogs.l16_drawDone, winRect, new Vector2(0, 180));
            yield return new WaitForSeconds(2.0f);

            // Step 2: Undo
            VirtualMascot.Show(dialogs.l16_undoPrompt, winRect, new Vector2(0, 180));

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
                    RecordMistake(dialogs.l16_holdCtrlZ, winRect);
                }

                yield return null;
            }
            CleanupActiveSubscriptions();

            SoundManager.PlaySuccess();
            VirtualMascot.Show(dialogs.l16_undoDone, winRect, new Vector2(0, 180));
            yield return new WaitForSeconds(2.0f);

            // Step 3: Save
            VirtualMascot.Show(dialogs.l16_savePrompt, winRect, new Vector2(0, 180));
            tutorialBlocker?.ClearTarget();

            bool saved = false;
            Action onFs = () => { saved = true; };
            FileExplorerApp.OnFileSystemChanged += onFs;
            RegisterCleanup(() => FileExplorerApp.OnFileSystemChanged -= onFs);

            while (!saved)
            {
                if (IsKeyWithoutCtrl(UnityEngine.InputSystem.Key.S, KeyCode.S) && !FileExplorerApp.IsSaveDialogOpen)
                {
                    RecordMistake(dialogs.l16_holdCtrlS, winRect);
                }
                yield return null;
            }
            CleanupActiveSubscriptions();

            SoundManager.PlaySuccess();
            VirtualMascot.Show(dialogs.l16_done, winRect, new Vector2(0, 180));
            yield return new WaitForSeconds(3.0f);

            CompleteLevel(levelDef);
        }
        #endregion

        #region Helpers & Level Completion
        private static int CountWordOccurrences(string source, string word)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(word)) return 0;
            int count = 0;
            int index = 0;
            while ((index = source.IndexOf(word, index, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                count++;
                index += word.Length;
            }
            return count;
        }

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

            bool improvedMedal = false;
            LevelMedal previousBestMedal = LevelMedal.None;
            if (ProgressData.Instance != null && levelDef != null && !string.IsNullOrEmpty(levelDef.levelId))
            {
                LevelScoreRecord prevRecord = ProgressData.Instance.GetLevelRecord(levelDef.levelId);
                bool hadPreviousAttempt = prevRecord != null && prevRecord.attemptsCount >= 1;
                previousBestMedal = prevRecord != null ? prevRecord.bestMedal : LevelMedal.None;
                improvedMedal = hadPreviousAttempt && (int)medal > (int)previousBestMedal;
            }
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

            if (improvedMedal)
            {
                VirtualMascot.ShowMedalImprovementFeedback(previousBestMedal, medal, accuracy, duration, modalParent as RectTransform, 5.5f);
            }
            else
            {
                VirtualMascot.HideMascot();
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

            GuidedLevelsDialogs d = LoadDialogsStatic();

            // Create in-memory fallback
            LevelDefinition def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.levelId = levelId.ToLower().Trim();
            def.isAdaptive = false;

            switch (def.levelId)
            {
                case "level10":
                    def.displayName = d.def_l10_name;
                    def.instructionText = d.def_l10_desc;
                    break;
                case "level11":
                    def.displayName = d.def_l11_name;
                    def.instructionText = d.def_l11_desc;
                    break;
                case "level12":
                    def.displayName = d.def_l12_name;
                    def.instructionText = d.def_l12_desc;
                    break;
                case "level13":
                    def.displayName = d.def_l13_name;
                    def.instructionText = d.def_l13_desc;
                    break;
                case "level14":
                    def.displayName = d.def_l14_name;
                    def.instructionText = d.def_l14_desc;
                    break;
                case "level15":
                    def.displayName = d.def_l15_name;
                    def.instructionText = d.def_l15_desc;
                    break;
                case "level16":
                    def.displayName = d.def_l16_name;
                    def.instructionText = d.def_l16_desc;
                    break;
                default:
                    def.displayName = $"Level {def.levelId}";
                    def.instructionText = d.def_default_desc;
                    break;
            }

            return def;
        }
        #endregion
    }
}
