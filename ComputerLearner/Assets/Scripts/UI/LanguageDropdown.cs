/**
 * Author: Julia & AI (TeamCompass)
 * Date: 09/10/26
 * Description: UI dropdown component that toggles between English and Spanish
 *              and keeps the dropdown state synchronized with LocalizationManager.
 */
using UnityEngine;
using TMPro;

namespace ComputerLearning
{
    [RequireComponent(typeof(TMP_Dropdown))]
    public class LanguageDropdown : MonoBehaviour
    {
        private TMP_Dropdown dropdown;

        private void Awake()
        {
            dropdown = GetComponent<TMP_Dropdown>();
            if (dropdown != null)
            {
                dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
            }
        }

        private void OnEnable()
        {
            UpdateDropdownSelection();
            LocalizationManager.OnLanguageChanged += UpdateDropdownSelection;
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= UpdateDropdownSelection;
        }

        private void OnDropdownValueChanged(int index)
        {
            string selectedLang = (index == 1) ? LocalizationManager.LANG_ES : LocalizationManager.LANG_EN;
            LocalizationManager.SetLanguage(selectedLang);
        }

        public void UpdateDropdownSelection()
        {
            if (dropdown != null)
            {
                int targetIndex = (LocalizationManager.CurrentLanguage == LocalizationManager.LANG_ES) ? 1 : 0;
                if (dropdown.value != targetIndex)
                {
                    dropdown.SetValueWithoutNotify(targetIndex);
                }
            }
        }
    }
}
