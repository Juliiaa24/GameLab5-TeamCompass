/**
 * Author: Diego
 * Date: 13/10/2026
 * Description: Educational Notepad application for typing practice and text manipulation.
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ComputerLearning
{
    public class NotepadApp : MonoBehaviour
    {
        #region Public Variables
        public string CurrentText => inputField != null ? inputField.text : string.Empty;
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
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (inputField == null) inputField = GetComponentInChildren<TMP_InputField>();
        }

        private void Start()
        {
            if (inputField != null)
            {
                if (string.IsNullOrEmpty(inputField.text))
                {
                    inputField.text = initialContent;
                }

                inputField.onValueChanged.AddListener(OnTextUpdated);
            }

            if (btnClear != null) btnClear.onClick.AddListener(ClearText);
            if (btnSave != null) btnSave.onClick.AddListener(SaveTextFeedback);

            if (feedbackMessage != null) feedbackMessage.gameObject.SetActive(false);

            UpdateStats(inputField != null ? inputField.text : string.Empty);
        }
        #endregion

        #region Public Methods
        public void SetText(string newText)
        {
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

        public void SaveTextFeedback()
        {
            if (feedbackMessage != null)
            {
                feedbackMessage.text = "Note saved successfully!";
                feedbackMessage.gameObject.SetActive(true);
                CancelInvoke(nameof(HideFeedback));
                Invoke(nameof(HideFeedback), 2.5f);
            }
        }
        #endregion

        #region Private Methods
        private void OnTextUpdated(string text)
        {
            UpdateStats(text);
        }

        private void UpdateStats(string text)
        {
            if (statusText == null) return;

            int charCount = text.Length;
            int wordCount = 0;
            if (!string.IsNullOrWhiteSpace(text))
            {
                string[] words = text.Split(new char[] { ' ', '\t', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
                wordCount = words.Length;
            }

            statusText.text = $"Characters: {charCount}  |  Words: {wordCount}";
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
