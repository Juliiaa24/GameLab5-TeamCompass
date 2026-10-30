/**
 * Author: Diego
 * Date: 16/10/2026
 * Description: Educational Notepad application for typing practice, opening .txt files,
 *              and saving new or existing text documents using Inspector-serialized UI references.
 *              Enhanced with standard keyboard shortcuts: Ctrl+C, Ctrl+X, Ctrl+V, Ctrl+Z, Ctrl+S.
 */
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ComputerLearning
{
    public class NotepadApp : MonoBehaviour
    {
        #region Public Variables
        public string CurrentText => inputField != null ? inputField.text : string.Empty;
        public ExplorerItemData CurrentFileItem => currentFileItem;
        public TMP_InputField InputField => inputField;

        public static string VirtualClipboard { get; set; } = string.Empty;

        public static event Action<string> OnTextCopied;
        public static event Action<string> OnTextCut;
        public static event Action<string> OnTextPasted;
        public static event Action OnTextUndo;
        public static event Action OnSaveShortcut;
        #endregion

        #region Private Variables
        [Header("UI References")]
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Button btnSave;
        [SerializeField] private Button btnSaveAs;
        [SerializeField] private Button btnSaveFooter;
        [SerializeField] private Button btnClear;
        [SerializeField] private Button btnClearTop;
        [SerializeField] private TextMeshProUGUI feedbackMessage;

        [Header("Default Content")]
        [TextArea(3, 8)]
        [SerializeField] private string initialContent = "Welcome to Notepad!\n\nHere you can practice typing on your keyboard. Try typing your name and your favorite games!";

        [NonSerialized] private ExplorerItemData currentFileItem;
        [NonSerialized] private ExplorerItemData targetSaveFolder;
        private Window parentWindow;
        private bool hasLoadedFile;

        // Undo history
        private readonly Stack<string> undoHistory = new Stack<string>();
        private string previousTextForUndo = string.Empty;
        private bool isUndoing = false;
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (inputField == null) inputField = GetComponentInChildren<TMP_InputField>();
            parentWindow = GetComponentInParent<Window>();
        }

        private void Start()
        {
            if (parentWindow == null) parentWindow = GetComponentInParent<Window>();

            if (inputField != null)
            {
                if (!hasLoadedFile && string.IsNullOrEmpty(inputField.text))
                {
                    inputField.text = initialContent;
                }

                previousTextForUndo = inputField.text;

                inputField.onValueChanged.RemoveListener(OnTextUpdated);
                inputField.onValueChanged.AddListener(OnTextUpdated);
            }

            if (btnSave != null)
            {
                btnSave.onClick.RemoveListener(OnSaveClicked);
                btnSave.onClick.AddListener(OnSaveClicked);
            }

            if (btnSaveAs != null)
            {
                btnSaveAs.onClick.RemoveListener(OnSaveAsClicked);
                btnSaveAs.onClick.AddListener(OnSaveAsClicked);
            }

            if (btnSaveFooter != null)
            {
                btnSaveFooter.onClick.RemoveListener(OnSaveClicked);
                btnSaveFooter.onClick.AddListener(OnSaveClicked);
            }

            if (btnClear != null)
            {
                btnClear.onClick.RemoveListener(ClearText);
                btnClear.onClick.AddListener(ClearText);
            }

            if (btnClearTop != null)
            {
                btnClearTop.onClick.RemoveListener(ClearText);
                btnClearTop.onClick.AddListener(ClearText);
            }

            if (feedbackMessage != null) feedbackMessage.gameObject.SetActive(false);

            UpdateStats(inputField != null ? inputField.text : string.Empty);
        }

        private void Update()
        {
            if (parentWindow == null) parentWindow = GetComponentInParent<Window>();

            bool isAppFocused = (parentWindow != null && parentWindow.IsFocused) ||
                                (inputField != null && inputField.isFocused);

            if (!isAppFocused) return;

            bool isCtrl = false;
            bool isC = false;
            bool isX = false;
            bool isV = false;
            bool isZ = false;
            bool isS = false;

            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                isCtrl = kb.ctrlKey.isPressed;
                isC = kb.cKey.wasPressedThisFrame;
                isX = kb.xKey.wasPressedThisFrame;
                isV = kb.vKey.wasPressedThisFrame;
                isZ = kb.zKey.wasPressedThisFrame;
                isS = kb.sKey.wasPressedThisFrame;
            }
            else
            {
#if ENABLE_LEGACY_INPUT_MANAGER
                isCtrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                isC = Input.GetKeyDown(KeyCode.C);
                isX = Input.GetKeyDown(KeyCode.X);
                isV = Input.GetKeyDown(KeyCode.V);
                isZ = Input.GetKeyDown(KeyCode.Z);
                isS = Input.GetKeyDown(KeyCode.S);
#endif
            }

            if (!isCtrl) return;

            if (isC)
            {
                HandleCopyShortcut();
            }
            else if (isX)
            {
                HandleCutShortcut();
            }
            else if (isV)
            {
                HandlePasteShortcut();
            }
            else if (isZ)
            {
                HandleUndoShortcut();
            }
            else if (isS)
            {
                HandleSaveShortcut();
            }
        }
        #endregion

        #region Public Methods
        public void PushUndoState()
        {
            if (inputField == null) return;
            string current = inputField.text ?? string.Empty;
            if (undoHistory.Count == 0 || undoHistory.Peek() != current)
            {
                undoHistory.Push(current);
            }
        }

        public void HandleCopyShortcut()
        {
            if (inputField == null) return;

            string selected = GetSelectedText();
            if (!string.IsNullOrEmpty(selected))
            {
                VirtualClipboard = selected;
                GUIUtility.systemCopyBuffer = selected;
                OnTextCopied?.Invoke(selected);
                ShowFeedbackMessage($"Copied: \"{TruncateString(selected, 20)}\"");
                FeedbackManager.PlayClick();
            }
            else if (!string.IsNullOrEmpty(inputField.text))
            {
                VirtualClipboard = inputField.text;
                GUIUtility.systemCopyBuffer = inputField.text;
                OnTextCopied?.Invoke(inputField.text);
                ShowFeedbackMessage("Copied all text!");
                FeedbackManager.PlayClick();
            }
        }

        public void HandleCutShortcut()
        {
            if (inputField == null) return;

            int start = Mathf.Min(inputField.selectionAnchorPosition, inputField.selectionFocusPosition);
            int end = Mathf.Max(inputField.selectionAnchorPosition, inputField.selectionFocusPosition);

            if (end > start)
            {
                PushUndoState();
                string selected = inputField.text.Substring(start, end - start);
                VirtualClipboard = selected;
                GUIUtility.systemCopyBuffer = selected;

                isUndoing = true;
                inputField.text = inputField.text.Remove(start, end - start);
                inputField.caretPosition = start;
                inputField.selectionAnchorPosition = start;
                inputField.selectionFocusPosition = start;
                previousTextForUndo = inputField.text;
                isUndoing = false;

                OnTextCut?.Invoke(selected);
                ShowFeedbackMessage($"Cut: \"{TruncateString(selected, 20)}\"");
                FeedbackManager.PlayClick();
            }
        }

        public void HandlePasteShortcut()
        {
            if (inputField == null) return;

            string toPaste = !string.IsNullOrEmpty(VirtualClipboard) ? VirtualClipboard : GUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(toPaste)) return;

            PushUndoState();

            int start = Mathf.Min(inputField.selectionAnchorPosition, inputField.selectionFocusPosition);
            int end = Mathf.Max(inputField.selectionAnchorPosition, inputField.selectionFocusPosition);

            isUndoing = true;
            if (end > start)
            {
                inputField.text = inputField.text.Remove(start, end - start).Insert(start, toPaste);
                inputField.caretPosition = start + toPaste.Length;
            }
            else
            {
                int caret = Mathf.Clamp(inputField.caretPosition, 0, inputField.text.Length);
                inputField.text = inputField.text.Insert(caret, toPaste);
                inputField.caretPosition = caret + toPaste.Length;
            }
            inputField.selectionAnchorPosition = inputField.caretPosition;
            inputField.selectionFocusPosition = inputField.caretPosition;
            previousTextForUndo = inputField.text;
            isUndoing = false;

            OnTextPasted?.Invoke(toPaste);
            ShowFeedbackMessage($"Pasted: \"{TruncateString(toPaste, 20)}\"");
            FeedbackManager.PlaySuccess();
        }

        public void HandleUndoShortcut()
        {
            if (inputField == null) return;

            if (undoHistory.Count > 0)
            {
                isUndoing = true;
                string prev = undoHistory.Pop();
                inputField.text = prev;
                inputField.caretPosition = Mathf.Clamp(inputField.caretPosition, 0, prev.Length);
                previousTextForUndo = prev;
                isUndoing = false;

                OnTextUndo?.Invoke();
                ShowFeedbackMessage("Undo!");
                FeedbackManager.PlaySuccess();
            }
            else
            {
                ShowFeedbackMessage("Nothing to undo");
            }
        }

        public void HandleSaveShortcut()
        {
            OnSaveClicked();
            OnSaveShortcut?.Invoke();
            FeedbackManager.PlayClick();
        }

        private string GetSelectedText()
        {
            if (inputField == null || string.IsNullOrEmpty(inputField.text)) return string.Empty;
            int start = Mathf.Min(inputField.selectionAnchorPosition, inputField.selectionFocusPosition);
            int end = Mathf.Max(inputField.selectionAnchorPosition, inputField.selectionFocusPosition);
            if (end > start && start >= 0 && end <= inputField.text.Length)
            {
                return inputField.text.Substring(start, end - start);
            }
            return string.Empty;
        }

        private string TruncateString(string s, int maxLen)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Length <= maxLen ? s : s.Substring(0, maxLen) + "...";
        }

        /// <summary>
        /// Loads a specific ExplorerItemData text document into Notepad for viewing and editing.
        /// </summary>
        public void LoadFile(ExplorerItemData fileItem, Window window = null, ExplorerItemData folder = null)
        {
            if (fileItem == null) return;

            currentFileItem = fileItem;
            if (folder != null) targetSaveFolder = folder;
            hasLoadedFile = true;

            if (window != null) parentWindow = window;
            if (parentWindow == null) parentWindow = GetComponentInParent<Window>();
            if (parentWindow != null)
            {
                parentWindow.setTitle($"Notepad - {fileItem.name}");
            }

            if (inputField == null) inputField = GetComponentInChildren<TMP_InputField>();
            if (inputField != null)
            {
                inputField.text = fileItem.textContent ?? string.Empty;
                previousTextForUndo = inputField.text;
                UpdateStats(inputField.text);
            }
        }

        public void SetText(string newText)
        {
            hasLoadedFile = true;
            if (inputField != null)
            {
                PushUndoState();
                inputField.text = newText;
                previousTextForUndo = newText;
                UpdateStats(newText);
            }
        }

        public void ClearText()
        {
            if (inputField != null)
            {
                PushUndoState();
                inputField.text = string.Empty;
                previousTextForUndo = string.Empty;
                UpdateStats(string.Empty);
            }
        }

        public void OnSaveClicked()
        {
            if (currentFileItem != null && !string.IsNullOrWhiteSpace(currentFileItem.name))
            {
                SaveDocumentWithName(currentFileItem.name, targetSaveFolder);
            }
            else
            {
                OnSaveAsClicked();
            }
        }

        public void OnSaveAsClicked()
        {
            string defaultFileName = currentFileItem != null && !string.IsNullOrWhiteSpace(currentFileItem.name)
                ? currentFileItem.name
                : "New Document.txt";

            ExplorerItemData startFolder = targetSaveFolder ?? FileExplorerApp.FindOrCreateSystemFolder("Documents", true);

            FileExplorerApp.OpenSaveDialog(
                "Save As - Notepad",
                defaultFileName,
                ".txt",
                startFolder,
                (enteredName, chosenFolder) =>
                {
                    targetSaveFolder = chosenFolder;
                    SaveDocumentWithName(enteredName, chosenFolder);
                }
            );
        }

        public void SaveTextFeedback()
        {
            OnSaveClicked();
        }
        #endregion

        #region Private Methods
        private void SaveDocumentWithName(string enteredName, ExplorerItemData destinationFolder = null)
        {
            if (destinationFolder == null) destinationFolder = targetSaveFolder;

            if (string.IsNullOrWhiteSpace(enteredName))
            {
                enteredName = "New Document.txt";
            }

            enteredName = enteredName.Trim();
            if (!enteredName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                enteredName += ".txt";
            }

            string contentToSave = CurrentText;

            if (currentFileItem != null && currentFileItem.name.Equals(enteredName, StringComparison.OrdinalIgnoreCase) &&
                (destinationFolder == null || (destinationFolder.children != null && destinationFolder.children.Contains(currentFileItem))))
            {
                currentFileItem.textContent = contentToSave;
                Sprite txtSprite = FileExplorerApp.GetTextFileSprite();
                if (txtSprite != null) currentFileItem.icon = txtSprite;
                FileExplorerApp.NotifyFileSystemChanged();
                ShowFeedbackMessage($"Saved: '{currentFileItem.name}'");
            }
            else
            {
                ExplorerItemData savedItem = FileExplorerApp.SaveOrCreateTextDocument(enteredName, contentToSave, destinationFolder);
                if (savedItem != null)
                {
                    currentFileItem = savedItem;
                    targetSaveFolder = destinationFolder;
                    hasLoadedFile = true;

                    if (parentWindow == null) parentWindow = GetComponentInParent<Window>();
                    if (parentWindow != null)
                    {
                        parentWindow.setTitle($"Notepad - {savedItem.name}");
                    }
                    ShowFeedbackMessage($"Saved as '{savedItem.name}' in {(destinationFolder != null ? destinationFolder.name : "Documents")}");
                }
                else
                {
                    ShowFeedbackMessage("Saved successfully!");
                }
            }

            UpdateStats(contentToSave);
        }

        private void ShowFeedbackMessage(string msg)
        {
            if (feedbackMessage != null)
            {
                feedbackMessage.text = msg;
                feedbackMessage.gameObject.SetActive(true);
                CancelInvoke(nameof(HideFeedback));
                Invoke(nameof(HideFeedback), 3.0f);
            }
        }

        private void OnTextUpdated(string text)
        {
            if (!isUndoing && previousTextForUndo != text)
            {
                if (undoHistory.Count == 0 || undoHistory.Peek() != previousTextForUndo)
                {
                    undoHistory.Push(previousTextForUndo);
                }
                previousTextForUndo = text;
            }
            UpdateStats(text);
        }

        private void UpdateStats(string text)
        {
            if (statusText == null) return;

            int charCount = text != null ? text.Length : 0;
            int wordCount = 0;
            if (!string.IsNullOrWhiteSpace(text))
            {
                string[] words = text.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                wordCount = words.Length;
            }

            string fileLabel = currentFileItem != null ? $"[{currentFileItem.name}]  |  " : "[Untitled.txt]  |  ";
            statusText.text = $"{fileLabel}Characters: {charCount}  |  Words: {wordCount}";
        }

        private void HideFeedback()
        {
            if (feedbackMessage != null)
            {
                feedbackMessage.gameObject.SetActive(false);
            }
        }
        #endregion
    }
}
