/**
 * Author: Diego
 * Date: 16/10/2026
 * Description: Educational Notepad application for typing practice, opening .txt files,
 *              and saving new or existing text documents using Inspector-serialized UI references.
 */
using System;
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
        #endregion

        #region Public Methods
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
                UpdateStats(inputField.text);
            }
        }

        public void SetText(string newText)
        {
            hasLoadedFile = true;
            if (inputField != null)
            {
                inputField.text = newText;
                UpdateStats(newText);
            }
        }

        public void ClearText()
        {
            if (inputField != null)
            {
                inputField.text = string.Empty;
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
