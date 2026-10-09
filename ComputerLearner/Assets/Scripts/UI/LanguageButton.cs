/**
 * Author: Julia & AI (TeamCompass)
 * Date: 09/10/26
 * Description: UI button component that toggles the game's language and updates its label.
 */
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ComputerLearning
{
    [RequireComponent(typeof(Button))]
    public class LanguageButton : MonoBehaviour
    {
        private TextMeshProUGUI buttonText;
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            buttonText = GetComponentInChildren<TextMeshProUGUI>();

            if (button != null)
            {
                button.onClick.AddListener(OnButtonClicked);
            }
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += UpdateButtonText;
            UpdateButtonText();
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= UpdateButtonText;
        }

        private void OnButtonClicked()
        {
            LocalizationManager.ToggleLanguage();
        }

        public void UpdateButtonText()
        {
            if (buttonText != null)
            {
                string lang = LocalizationManager.CurrentLanguage;
                buttonText.text = $"Language: {lang}";
            }
        }
    }
}
