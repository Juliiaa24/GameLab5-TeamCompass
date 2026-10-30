/**
 * Author: Julia & AI (TeamCompass)
 * Date: 09/10/26
 * Description: UI dropdown component that switches between English (EN), Spanish (ES), and Norwegian (NO)
 *              and keeps the dropdown state and Main Menu button texts synchronized with LocalizationManager.
 */
using UnityEngine;
using UnityEngine.UI;
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
                EnsureNorwegianOptionExists();
                dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
            }
        }

        private void Start()
        {
            UpdateMenuButtonsLocalization();
        }

        private void EnsureNorwegianOptionExists()
        {
            if (dropdown == null || dropdown.options == null) return;

            bool hasNo = false;
            foreach (var opt in dropdown.options)
            {
                if (opt != null && opt.text != null && opt.text.Contains("(NO)"))
                {
                    hasNo = true;
                    break;
                }
            }

            if (!hasNo)
            {
                dropdown.options.Add(new TMP_Dropdown.OptionData("Norsk (NO)"));
                dropdown.RefreshShownValue();
            }
        }

        private void OnEnable()
        {
            UpdateDropdownSelection();
            UpdateMenuButtonsLocalization();
            LocalizationManager.OnLanguageChanged += OnLanguageChangedHandler;
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= OnLanguageChangedHandler;
        }

        private void OnLanguageChangedHandler()
        {
            UpdateDropdownSelection();
            UpdateMenuButtonsLocalization();
        }

        private void OnDropdownValueChanged(int index)
        {
            string selectedLang = LocalizationManager.LANG_EN;
            if (index == 1) selectedLang = LocalizationManager.LANG_ES;
            else if (index == 2) selectedLang = LocalizationManager.LANG_NO;

            LocalizationManager.SetLanguage(selectedLang);
        }

        public void UpdateDropdownSelection()
        {
            if (dropdown != null)
            {
                EnsureNorwegianOptionExists();
                int targetIndex = 0;
                if (LocalizationManager.CurrentLanguage == LocalizationManager.LANG_ES) targetIndex = 1;
                else if (LocalizationManager.CurrentLanguage == LocalizationManager.LANG_NO) targetIndex = 2;

                if (dropdown.value != targetIndex)
                {
                    dropdown.SetValueWithoutNotify(targetIndex);
                }
            }
        }

        public void UpdateMenuButtonsLocalization()
        {
            UIDialogs ui = UIDialogs.Current;
            Canvas canvas = GetComponentInParent<Canvas>();
            Transform root = canvas != null ? canvas.transform : transform.root;
            if (root == null) return;

            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            foreach (Button btn in buttons)
            {
                if (btn == null) continue;
                TMP_Text label = btn.GetComponentInChildren<TMP_Text>(true);
                if (label == null) continue;

                string btnName = btn.gameObject.name;
                if (btnName == "Btn-Play")
                {
                    label.text = ui.menu_btn_play;
                }
                else if (btnName == "Btn-VirtualDesktop")
                {
                    label.text = ui.menu_btn_virtual_desktop;
                }
                else if (btnName == "Btn-Stats")
                {
                    label.text = ui.menu_btn_stats;
                }
                else if (btnName == "Btn-1")
                {
                    label.text = ui.menu_btn_reset_progress;
                }
                else if (btnName == "Btn-2")
                {
                    label.text = ui.menu_btn_exit;
                }
            }
        }
    }
}
