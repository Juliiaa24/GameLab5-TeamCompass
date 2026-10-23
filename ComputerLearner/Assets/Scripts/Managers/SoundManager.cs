/**
 * Author: TeamCompass (Diego & AI)
 * Date: 23/10/2026
 * Description: Centralized Sound & Audio system with procedural sound synthesis fallback
 *              and screen-shake haptic feedback integration.
 */
using System;
using System.Collections;
using UnityEngine;

namespace ComputerLearning
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource uiSource;

        [Header("Audio Clips (Optional — Auto-generates procedural audio if null)")]
        [SerializeField] private AudioClip successClip;
        [SerializeField] private AudioClip errorClip;
        [SerializeField] private AudioClip clickClip;
        [SerializeField] private AudioClip medalChimeBronze;
        [SerializeField] private AudioClip medalChimeSilver;
        [SerializeField] private AudioClip medalChimeGold;

        [Header("Volume Settings")]
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.85f;
        [Range(0f, 1f)] [SerializeField] private float uiVolume = 0.90f;

        private Coroutine shakeCoroutine;
        private Transform activeShakeCamTransform;
        private Vector3 activeShakeCamOriginalPos;
        private RectTransform activeShakeRectTransform;
        private Vector2 activeShakeRectOriginalPos;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }

            SetupAudioSources();
            EnsureClipsExist();
        }

        private void SetupAudioSources()
        {
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                sfxSource.spatialBlend = 0f; // 2D Sound
            }

            if (uiSource == null)
            {
                uiSource = gameObject.AddComponent<AudioSource>();
                uiSource.playOnAwake = false;
                uiSource.spatialBlend = 0f; // 2D Sound
            }

            sfxSource.volume = sfxVolume;
            uiSource.volume = uiVolume;
        }

        private void EnsureClipsExist()
        {
            if (successClip == null) successClip = CreateSuccessClip();
            if (errorClip == null) errorClip = CreateErrorClip();
            if (clickClip == null) clickClip = CreateClickClip();
            if (medalChimeBronze == null) medalChimeBronze = CreateMedalChimeClip(523.25f, 659.25f); // C5 - E5
            if (medalChimeSilver == null) medalChimeSilver = CreateMedalChimeClip(659.25f, 783.99f); // E5 - G5
            if (medalChimeGold == null) medalChimeGold = CreateMedalChimeClip(783.99f, 1046.50f);    // G5 - C6
        }

        #region Public Playback API
        public static void PlaySuccess()
        {
            if (Instance != null) Instance.PlaySuccessInternal();
        }

        public static void PlayError()
        {
            if (Instance != null) Instance.PlayErrorInternal();
        }

        public static void PlayClick()
        {
            if (Instance != null) Instance.PlayClickInternal();
        }

        public static void PlayMedalChime(int medalRank)
        {
            if (Instance != null) Instance.PlayMedalChimeInternal(medalRank);
        }

        public void PlaySuccessInternal()
        {
            if (uiSource != null && successClip != null)
            {
                uiSource.pitch = UnityEngine.Random.Range(0.98f, 1.05f);
                uiSource.PlayOneShot(successClip, uiVolume);
            }
        }

        public void PlayErrorInternal()
        {
            if (sfxSource != null && errorClip != null)
            {
                sfxSource.pitch = UnityEngine.Random.Range(0.95f, 1.02f);
                sfxSource.PlayOneShot(errorClip, sfxVolume);
            }
        }

        public void PlayClickInternal()
        {
            if (uiSource != null && clickClip != null)
            {
                uiSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
                uiSource.PlayOneShot(clickClip, uiVolume * 0.7f);
            }
        }

        public void PlayMedalChimeInternal(int medalRank)
        {
            if (uiSource == null) return;

            AudioClip clip = medalChimeBronze;
            if (medalRank == 2) clip = medalChimeSilver;
            else if (medalRank >= 3) clip = medalChimeGold;

            if (clip != null)
            {
                uiSource.pitch = 1f;
                uiSource.PlayOneShot(clip, uiVolume);
            }
        }
        #endregion

        #region Screen Shake
        public static void TriggerScreenShake(float intensity = 5f, float duration = 0.2f)
        {
            if (Instance != null)
            {
                Instance.StartShake(intensity, duration);
            }
        }

        public void StartShake(float intensity, float duration)
        {
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
                RestoreShakeTargets();
            }
            shakeCoroutine = StartCoroutine(ShakeRoutine(intensity, duration));
        }

        private void RestoreShakeTargets()
        {
            if (activeShakeCamTransform != null)
            {
                activeShakeCamTransform.localPosition = activeShakeCamOriginalPos;
                activeShakeCamTransform = null;
            }
            if (activeShakeRectTransform != null)
            {
                activeShakeRectTransform.anchoredPosition = activeShakeRectOriginalPos;
                activeShakeRectTransform = null;
            }
        }

        private void OnDisable()
        {
            RestoreShakeTargets();
        }

        private IEnumerator ShakeRoutine(float intensity, float duration)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                activeShakeCamTransform = mainCam.transform;
                activeShakeCamOriginalPos = activeShakeCamTransform.localPosition;
            }

            // Also locate the active UI window/runner for ScreenSpaceOverlay canvases
            if (WindowManager.Instance != null && WindowManager.Instance.ActiveWindow != null && !WindowManager.Instance.ActiveWindow.IsClosed)
            {
                activeShakeRectTransform = WindowManager.Instance.ActiveWindow.GetComponent<RectTransform>();
            }
            else
            {
                LevelRunner runner = FindAnyObjectByType<LevelRunner>();
                if (runner != null)
                {
                    Window parentWin = runner.GetComponentInParent<Window>();
                    activeShakeRectTransform = parentWin != null ? parentWin.GetComponent<RectTransform>() : runner.GetComponent<RectTransform>();
                }
            }

            if (activeShakeRectTransform != null)
            {
                activeShakeRectOriginalPos = activeShakeRectTransform.anchoredPosition;
            }

            if (activeShakeCamTransform == null && activeShakeRectTransform == null)
            {
                shakeCoroutine = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float damp = 1f - Mathf.Clamp01(elapsed / duration);
                float offsetX = UnityEngine.Random.Range(-1f, 1f) * intensity * damp;
                float offsetY = UnityEngine.Random.Range(-1f, 1f) * intensity * damp;

                if (activeShakeCamTransform != null)
                {
                    activeShakeCamTransform.localPosition = activeShakeCamOriginalPos + new Vector3(offsetX * 0.05f, offsetY * 0.05f, 0f);
                }

                if (activeShakeRectTransform != null)
                {
                    activeShakeRectTransform.anchoredPosition = activeShakeRectOriginalPos + new Vector2(offsetX, offsetY);
                }

                yield return null;
            }

            RestoreShakeTargets();
            shakeCoroutine = null;
        }
        #endregion

        #region Procedural Audio Synthesis
        private static AudioClip CreateSuccessClip()
        {
            int sampleRate = 44100;
            float duration = 0.28f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            // 2-tone melodic chime: C5 (523Hz) transition to G5 (784Hz)
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = t < 0.12f ? 523.25f : 783.99f;
                float env = Mathf.Exp(-t * 9f); // Smooth exponential decay
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.7f +
                             Mathf.Sin(4f * Mathf.PI * freq * t) * 0.3f; // Gentle harmonic

                samples[i] = wave * env * 0.35f;
            }

            AudioClip clip = AudioClip.Create("Success_Procedural", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateErrorClip()
        {
            int sampleRate = 44100;
            float duration = 0.18f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            // Soft error buzz / low bonk (160Hz)
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float freq = 160f;
                float env = Mathf.Exp(-t * 14f);
                // Slight square-ish distortion
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t);
                wave = Mathf.Clamp(wave * 2.2f, -0.8f, 0.8f);

                samples[i] = wave * env * 0.30f;
            }

            AudioClip clip = AudioClip.Create("Error_Procedural", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateClickClip()
        {
            int sampleRate = 44100;
            float duration = 0.04f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            // Subtle mechanical click/tick
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 120f);
                float wave = Mathf.Sin(2f * Mathf.PI * 1800f * t) + (UnityEngine.Random.value * 0.4f - 0.2f);
                samples[i] = wave * env * 0.25f;
            }

            AudioClip clip = AudioClip.Create("Click_Procedural", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateMedalChimeClip(float f1, float f2)
        {
            int sampleRate = 44100;
            float duration = 0.55f;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            // Bright sparkling bell chime with resonance
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 5.5f);
                float wave = Mathf.Sin(2f * Mathf.PI * f1 * t) * 0.5f +
                             Mathf.Sin(2f * Mathf.PI * f2 * t) * 0.35f +
                             Mathf.Sin(2f * Mathf.PI * (f2 * 2f) * t) * 0.15f;

                samples[i] = wave * env * 0.40f;
            }

            AudioClip clip = AudioClip.Create($"MedalChime_{f1:F0}_{f2:F0}", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
        #endregion
    }

    /// <summary>
    /// Static facade and coordinator for Sound & Screen Shake feedback across the game.
    /// </summary>
    public static class FeedbackManager
    {
        public static void EnsureManagerExists()
        {
            if (SoundManager.Instance == null)
            {
                GameObject obj = new GameObject("SoundManager", typeof(SoundManager));
                UnityEngine.Object.DontDestroyOnLoad(obj);
            }
        }

        public static void PlaySuccess()
        {
            EnsureManagerExists();
            SoundManager.PlaySuccess();
        }

        public static void PlayError(bool withShake = true)
        {
            EnsureManagerExists();
            SoundManager.PlayError();
            if (withShake)
            {
                TriggerShake(4f, 0.18f);
            }
        }

        public static void PlayClick()
        {
            EnsureManagerExists();
            SoundManager.PlayClick();
        }

        public static void PlayMedalChime(int medalRank)
        {
            EnsureManagerExists();
            SoundManager.PlayMedalChime(medalRank);
        }

        public static void TriggerShake(float intensity = 4f, float duration = 0.18f)
        {
            EnsureManagerExists();
            SoundManager.TriggerScreenShake(intensity, duration);
        }
    }
}
