/**
 * Author: Julia & AI (TeamCompass)
 * Date: 09/10/26
 * Description: Central manager for loading localized JSON dialogs and instructions.
 *              Supports language codes (EN, ES), persistence via PlayerPrefs,
 *              and dynamic runtime fallback loading.
 */
using System;
using UnityEngine;

namespace ComputerLearning
{
    public class LocalizationManager : MonoBehaviour
    {
        public static LocalizationManager Instance { get; private set; }

        public const string LANG_EN = "EN";
        public const string LANG_ES = "ES";

        public static event Action OnLanguageChanged;

        public static string CurrentLanguage
        {
            get => PlayerPrefs.GetString("SelectedLanguage", LANG_EN);
            private set
            {
                PlayerPrefs.SetString("SelectedLanguage", value);
                PlayerPrefs.Save();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Loads a localized TextAsset based on CurrentLanguage with format: [LANG]-[baseName].
        /// Falls back to EN-[baseName] and then [baseName] if missing.
        /// </summary>
        public static TextAsset LoadLocalizedResource(string baseName)
        {
            string lang = CurrentLanguage;
            TextAsset asset = Resources.Load<TextAsset>($"{lang}-{baseName}");
            if (asset == null)
            {
                asset = Resources.Load<TextAsset>($"{LANG_EN}-{baseName}");
            }
            if (asset == null)
            {
                asset = Resources.Load<TextAsset>(baseName);
            }

            if (asset == null)
            {
                Debug.LogWarning($"[LocalizationManager] Failed to load localized resource for '{baseName}' (Lang: {lang})");
            }

            return asset;
        }

        /// <summary>
        /// Sets the active language (e.g. "EN" or "ES") and notifies all listeners.
        /// </summary>
        public static void SetLanguage(string lang)
        {
            if (CurrentLanguage != lang)
            {
                CurrentLanguage = lang;
                OnLanguageChanged?.Invoke();
                Debug.Log($"[LocalizationManager] Language changed to: {lang}");
            }
        }

        /// <summary>
        /// Toggles between English ("EN") and Spanish ("ES").
        /// </summary>
        public static void ToggleLanguage()
        {
            SetLanguage(CurrentLanguage == LANG_EN ? LANG_ES : LANG_EN);
        }
    }
}
