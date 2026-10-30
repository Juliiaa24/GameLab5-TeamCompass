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
        public string l10_clickMenu = "Awesome! This is the Context Menu.\nClick on '+ New Text Document (.txt)' or '+ New Folder' to create an item!";
        public string l10_done = "Fantastic! You've learned how to use the desktop context menu to create and customize files!";

        public string l11_openNotepad = "Level 11: Word Processing!\nDouble-click the Notepad icon to open the text editor.";
        public string l11_typePrompt = "Now let's practice typing on your keyboard!\nType: 'Hello Computer!' in the notepad.";
        public string l11_typeReminder = "Remember: Type 'Hello Computer!' into the notepad.";
        public string l11_done = "Terrific! You typed the phrase perfectly!";

        public string l12_copyPrompt = "Let's master keyboard shortcuts!\nHighlight the word 'Cat' and press Ctrl + C on your keyboard to copy it.";
        public string l12_copyDone = "Nice! 'Cat' is copied into your clipboard.";
        public string l12_pastePrompt = "Now click at the end of the text and press Ctrl + V to Paste it!";
        public string l12_pasteDone = "You pasted it! Now you have two Cats!";
        public string l12_cutPrompt = "Now highlight the word 'Dog' and press Ctrl + X to Cut it away!";
        public string l12_done = "Brilliant! Ctrl + X cut the text and saved it to your clipboard!";

        public string l13_welcome = "Welcome to Level 13!\nLook at this sentence: 'Computer Science is fun!'";
        public string l13_undoPrompt = "Oh no! Someone accidentally made a mistake!\nPress Ctrl + Z to Undo the mistake and restore your original words!";
        public string l13_done = "Magic! Ctrl + Z lets you undo mistakes in almost every program!";

        public string l14_saveShortcutPrompt = "Level 14: Save Shortcut!\nAlways save your files. Press Ctrl + S on your keyboard to save this note.";
        public string l14_saveModalPrompt = "The Save window is open! Type a name for your file (like 'MyNotes') and click SAVE.";
        public string l14_done = "Document saved! Ctrl + S is the most useful shortcut to keep your work safe.";

        public string l15_welcome = "Level 15: The Grand Word Challenge!\nLet's test all your typing and keyboard skills together.";
        public string l15_step1 = "Step 1 of 5: Type the words: 'Red Blue Green'";
        public string l15_step2 = "Step 2 of 5: Highlight 'Blue', press Ctrl + C, and press Ctrl + V to duplicate it!";
        public string l15_step3 = "Step 3 of 5: Highlight 'Red' and press Ctrl + X to cut it away!";
        public string l15_step4 = "Step 4 of 5: Press Ctrl + Z to Undo the cut and bring 'Red' back!";
        public string l15_step5 = "Step 5 of 5: Finally, press Ctrl + S and click SAVE to store your finished note!";
        public string l15_done = "Outstanding mastery! You aced all Notepad shortcuts and operations!";

        public string l16_openPaint = "Level 16: Digital Artist in Paint!\nDouble-click the Paint icon to open the drawing studio.";
        public string l16_drawPrompt = "Pick any color from the palette and draw a shape or smile on the canvas using your mouse!";
        public string l16_drawDone = "Nice drawing! Now let's test undoing a brush stroke.";
        public string l16_undoPrompt = "Press Ctrl + Z to Undo your last stroke!";
        public string l16_undoDone = "Stroke undone! You can undo any drawing mistake with Ctrl + Z.";
        public string l16_savePrompt = "Now press Ctrl + S and confirm in the Save dialog to store your artwork in Pictures!";
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

            while (ContextMenuController.Instance == null || !ContextMenuController.Instance.IsMenuOpen)
            {
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

            while (!actionConfirmed)
            {
                if (ContextMenuController.Instance != null && !ContextMenuController.Instance.IsMenuOpen && !actionConfirmed)
                {
                    tutorialBlocker?.ClearTarget();
                }
                else if (ContextMenuController.Instance != null && ContextMenuController.Instance.IsMenuOpen)
                {
                    tutorialBlocker?.SetAllowedTarget(menuRect);
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
            while (notepad == null || !notepad.gameObject.activeInHierarchy)
            {
                notepad = FindAnyObjectByType<NotepadApp>();
                yield return null;
            }

            Window notepadWin = notepad.GetComponentInParent<Window>();
            RectTransform winRect = notepadWin != null ? notepadWin.GetComponent<RectTransform>() : notepad.GetComponent<RectTransform>();

            notepad.SetText("");
            tutorialBlocker?.SetAllowedTarget(winRect);

            VirtualMascot.Show(dialogs.l11_typePrompt, winRect, new Vector2(0, 180));

            float timer = 0f;
            while (notepad == null || !notepad.gameObject.activeInHierarchy ||
                   notepad.CurrentText.IndexOf("Hello Computer", StringComparison.OrdinalIgnoreCase) < 0)
            {
                if (notepad == null || !notepad.gameObject.activeInHierarchy)
                {
                    notepad = RecoverNotepadWindow(ref winRect);
                }

                timer += Time.deltaTime;
                if (timer > 8.0f)
                {
                    VirtualMascot.Show(dialogs.l11_typeReminder, winRect, new Vector2(0, 180));
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

            notepad.SetText("Cat Dog");
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

            while (!copied && (NotepadApp.VirtualClipboard == null || NotepadApp.VirtualClipboard.IndexOf("Cat", StringComparison.OrdinalIgnoreCase) < 0))
            {
                if (notepad == null || !notepad.gameObject.activeInHierarchy)
                {
                    notepad = RecoverNotepadWindow(ref winRect, "Cat Dog");
                }
                yield return null;
            }
            CleanupActiveSubscriptions();

            SoundManager.PlaySuccess();
            VirtualMascot.Show(dialogs.l12_copyDone, winRect, new Vector2(0, 180));
            yield return new WaitForSeconds(2.0f);

            // Step 2: Paste
            VirtualMascot.Show(dialogs.l12_pastePrompt, winRect, new Vector2(0, 180));

            bool pasted = false;
            Action<string> onPaste = (text) => { pasted = true; };
            NotepadApp.OnTextPasted += onPaste;
            RegisterCleanup(() => NotepadApp.OnTextPasted -= onPaste);

            while (!pasted && (notepad == null || notepad.CurrentText.Split(new[] { "Cat", "cat" }, StringSplitOptions.None).Length <= 2))
            {
                if (notepad == null || !notepad.gameObject.activeInHierarchy)
                {
                    notepad = RecoverNotepadWindow(ref winRect, "Cat Dog");
                }
                yield return null;
            }
            CleanupActiveSubscriptions();

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

            while (!cut && (notepad == null || notepad.CurrentText.Contains("Dog")))
            {
                if (notepad == null || !notepad.gameObject.activeInHierarchy)
                {
                    notepad = RecoverNotepadWindow(ref winRect, "Cat Dog Cat");
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

            notepad.SetText("Computer Science is fun!");
            notepad.PushUndoState();
            tutorialBlocker?.SetAllowedTarget(winRect);

            VirtualMascot.Show(dialogs.l13_welcome, winRect, new Vector2(0, 180));
            yield return new WaitForSeconds(3.0f);

            if (notepad == null || !notepad.gameObject.activeInHierarchy)
            {
                notepad = RecoverNotepadWindow(ref winRect, "Computer Science is fun!");
                if (notepad != null) notepad.PushUndoState();
            }

            // Simulate an accidental mistake
            if (notepad != null) notepad.SetText("Computer Science is OOPS!");
            SoundManager.PlayError();

            VirtualMascot.Show(dialogs.l13_undoPrompt, winRect, new Vector2(0, 180));

            bool undone = false;
            Action onUndo = () => { undone = true; };
            NotepadApp.OnTextUndo += onUndo;
            RegisterCleanup(() => NotepadApp.OnTextUndo -= onUndo);

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
                yield return null;
            }
            CleanupActiveSubscriptions();

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

            notepad.SetText("Important Note: Always save your files before closing the app!");
            tutorialBlocker?.SetAllowedTarget(winRect);

            VirtualMascot.Show(dialogs.l14_saveShortcutPrompt, winRect, new Vector2(0, 180));

            bool saveOpened = false;
            Action onSaveKey = () => { saveOpened = true; };
            NotepadApp.OnSaveShortcut += onSaveKey;
            RegisterCleanup(() => NotepadApp.OnSaveShortcut -= onSaveKey);

            while (!saveOpened && !FileExplorerApp.IsSaveDialogOpen)
            {
                if (notepad == null || !notepad.gameObject.activeInHierarchy)
                {
                    notepad = RecoverNotepadWindow(ref winRect, "Important Note: Always save your files before closing the app!");
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

            notepad.SetText("");
            tutorialBlocker?.SetAllowedTarget(winRect);

            // Step 1: Type
            VirtualMascot.Show(dialogs.l15_step1, winRect, new Vector2(0, 180));
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
            VirtualMascot.Show(dialogs.l15_step2, winRect, new Vector2(0, 180));
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
            VirtualMascot.Show(dialogs.l15_step3, winRect, new Vector2(0, 180));
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
            VirtualMascot.Show(dialogs.l15_step4, winRect, new Vector2(0, 180));
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
                yield return null;
            }
            CleanupActiveSubscriptions();
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
            while (paint == null || !paint.gameObject.activeInHierarchy)
            {
                paint = FindAnyObjectByType<PaintApp>();
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
            float accuracy = 100f;
            int mistakes = 0;
            LevelMedal medal = LevelMedal.Gold;

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
