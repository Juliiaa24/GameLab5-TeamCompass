/**
 * Author: Diego
 * Date: 16/10/2026
 * Description: Educational Notepad application for typing practice, opening .txt files,
 *              and saving new or existing text documents with a Word-style Save dialog.
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
        [SerializeField] private Button btnClear;
        [SerializeField] private Button btnSave;
        [SerializeField] private TextMeshProUGUI feedbackMessage;

        [Header("Default Content")]
        [TextArea(3, 8)]
        [SerializeField] private string initialContent = "Welcome to Notepad!\n\nHere you can practice typing on your keyboard. Try typing your name and your favorite games!";

        private ExplorerItemData currentFileItem;
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

                inputField.onValueChanged.AddListener(OnTextUpdated);
            }

            if (btnClear != null) btnClear.onClick.AddListener(ClearText);
            if (btnSave != null) btnSave.onClick.AddListener(OnSaveClicked);

            if (feedbackMessage != null) feedbackMessage.gameObject.SetActive(false);

            UpdateStats(inputField != null ? inputField.text : string.Empty);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Loads a specific ExplorerItemData text document into Notepad for viewing and editing.
        /// </summary>
        public void LoadFile(ExplorerItemData fileItem, Window window = null)
        {
            if (fileItem == null) return;

            currentFileItem = fileItem;
            hasLoadedFile = true;

            if (window != null) parentWindow = window;
            if (parentWindow == null) parentWindow = GetComponentInParent<Window>();
            if (parentWindow != null)
            {
                parentWindow.setTitle($"Bloc de Notas - {fileItem.name}");
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
            string defaultFileName = currentFileItem != null && !string.IsNullOrWhiteSpace(currentFileItem.name)
                ? currentFileItem.name
                : "Nuevo Documento.txt";

            if (ContextMenuController.Instance != null)
            {
                ContextMenuController.Instance.ShowInputModal(
                    "Guardar documento de texto (.txt)",
                    defaultFileName,
                    "Guardar",
                    (enteredName) => SaveDocumentWithName(enteredName)
                );
            }
            else
            {
                SaveDocumentWithName(defaultFileName);
            }
        }

        public void SaveTextFeedback()
        {
            OnSaveClicked();
        }
        #endregion

        #region Private Methods
        private void SaveDocumentWithName(string enteredName)
        {
            if (string.IsNullOrWhiteSpace(enteredName))
            {
                enteredName = "Nuevo Documento.txt";
            }

            enteredName = enteredName.Trim();
            if (!enteredName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                enteredName += ".txt";
            }

            string contentToSave = CurrentText;

            if (currentFileItem != null && currentFileItem.name.Equals(enteredName, StringComparison.OrdinalIgnoreCase))
            {
                // Update existing opened file directly
                currentFileItem.textContent = contentToSave;
                Sprite txtSprite = FileExplorerApp.GetTextFileSprite();
                if (txtSprite != null) currentFileItem.icon = txtSprite;
                FileExplorerApp.NotifyFileSystemChanged();
                ShowFeedbackMessage($"Guardado: '{currentFileItem.name}'");
            }
            else
            {
                // Save as a new .txt file in the virtual file system
                ExplorerItemData savedItem = FileExplorerApp.SaveOrCreateTextDocument(enteredName, contentToSave);
                if (savedItem != null)
                {
                    currentFileItem = savedItem;
                    hasLoadedFile = true;

                    if (parentWindow == null) parentWindow = GetComponentInParent<Window>();
                    if (parentWindow != null)
                    {
                        parentWindow.setTitle($"Bloc de Notas - {savedItem.name}");
                    }
                    ShowFeedbackMessage($"Guardado como '{savedItem.name}'");
                }
                else
                {
                    ShowFeedbackMessage("Note saved successfully!");
                }
            }
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

            string fileLabel = currentFileItem != null ? $"[{currentFileItem.name}]  |  " : "";
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
