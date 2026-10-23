/**
 * Author: TeamCompass (Diego & AI)
 * Date: 23/10/2026
 * Description: Animated level results modal displaying a single earned medal (Bronze, Silver, Gold).
 *              Featuring a large rewarding hero medal reveal with overshoot pop and audio chime.
 *              Includes automated progression (railroading) for the first few levels where clicking
 *              has not yet been introduced.
 */
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace ComputerLearning
{
    public class LevelCelebrationModal : MonoBehaviour
    {
        public static LevelCelebrationModal Show(
            Transform parent, 
            LevelDefinition level, 
            LevelMedal medal, 
            float accuracy, 
            float duration, 
            int mistakes, 
            Action onClosed,
            bool? autoAdvance = null)
        {
            GameObject modalObj = new GameObject("LevelCelebrationModal", typeof(RectTransform), typeof(CanvasGroup), typeof(LevelCelebrationModal));
            modalObj.transform.SetParent(parent, false);

            RectTransform rt = modalObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            LevelCelebrationModal modal = modalObj.GetComponent<LevelCelebrationModal>();
            modal.Initialize(level, medal, accuracy, duration, mistakes, onClosed, autoAdvance);
            return modal;
        }

        public static bool ShouldAutoAdvance(LevelDefinition level)
        {
            if (level == null) return false;
            string id = level.levelId != null ? level.levelId.Trim().ToLowerInvariant() : "";
            // First few tutorial levels: player hasn't learned how to click yet (Level 1: Move, Level 2: Hover).
            // Railroad the results screen so player doesn't get stuck.
            return id == "level1" || id == "level2" || id == "assessment";
        }

        private LevelDefinition levelDef;
        private LevelMedal earnedMedal;
        private float accuracyVal;
        private float durationVal;
        private int mistakesVal;
        private Action onClosedCallback;
        private bool isAutoAdvancing;
        private bool isClosed = false;

        private Image medalImage;
        private RectTransform medalRect;
        private TextMeshProUGUI medalBadgeText;
        private TextMeshProUGUI btnTxt;
        private Button continueBtn;
        private Coroutine autoAdvanceCoroutine;

        public void Initialize(
            LevelDefinition level, 
            LevelMedal medal, 
            float accuracy, 
            float duration, 
            int mistakes, 
            Action onClosed,
            bool? autoAdvance = null)
        {
            levelDef = level;
            earnedMedal = medal;
            accuracyVal = accuracy;
            durationVal = duration;
            mistakesVal = mistakes;
            onClosedCallback = onClosed;
            isAutoAdvancing = autoAdvance.HasValue ? autoAdvance.Value : ShouldAutoAdvance(level);

            BuildVisuals();
            StartCoroutine(AnimateSequence());
        }

        private void BuildVisuals()
        {
            // Dimmed backdrop
            GameObject backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
            backdrop.transform.SetParent(transform, false);
            RectTransform bdRect = backdrop.GetComponent<RectTransform>();
            bdRect.anchorMin = Vector2.zero;
            bdRect.anchorMax = Vector2.one;
            bdRect.offsetMin = Vector2.zero;
            bdRect.offsetMax = Vector2.zero;
            backdrop.GetComponent<Image>().color = new Color(0.08f, 0.12f, 0.18f, 0.88f);

            // Card Panel
            GameObject card = new GameObject("Card", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            card.transform.SetParent(transform, false);
            RectTransform cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(460f, 380f);
            cardRt.anchoredPosition = Vector2.zero;

            Image cardImg = card.GetComponent<Image>();
            cardImg.color = new Color(0.93f, 0.95f, 0.98f, 1f);

            VerticalLayoutGroup vlg = card.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(22, 22, 20, 20);
            vlg.spacing = 10;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;

            // Title
            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(card.transform, false);
            TextMeshProUGUI title = titleObj.GetComponent<TextMeshProUGUI>();
            title.text = "<b>★ LEVEL COMPLETED! ★</b>";
            title.fontSize = 24;
            title.alignment = TextAlignmentOptions.Center;
            title.color = new Color(0.12f, 0.22f, 0.38f);
            title.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 30);

            // Subtitle (Level Name)
            GameObject subObj = new GameObject("SubTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            subObj.transform.SetParent(card.transform, false);
            TextMeshProUGUI subtitle = subObj.GetComponent<TextMeshProUGUI>();
            subtitle.text = levelDef != null ? levelDef.displayName : "Great Job!";
            subtitle.fontSize = 17;
            subtitle.alignment = TextAlignmentOptions.Center;
            subtitle.color = new Color(0.35f, 0.40f, 0.48f);
            subtitle.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 22);

            // ── Single Hero Medal Showcase (Only the earned medal, scaled up to feel rewarding) ──
            GameObject medalShowcase = new GameObject("MedalShowcase", typeof(RectTransform), typeof(VerticalLayoutGroup));
            medalShowcase.transform.SetParent(card.transform, false);
            RectTransform msRt = medalShowcase.GetComponent<RectTransform>();
            msRt.sizeDelta = new Vector2(0, 150f);

            VerticalLayoutGroup msVlg = medalShowcase.GetComponent<VerticalLayoutGroup>();
            msVlg.spacing = 6;
            msVlg.childControlWidth = false;
            msVlg.childControlHeight = false;
            msVlg.childAlignment = TextAnchor.MiddleCenter;

            // Hero Medal Slot
            GameObject medalObj = new GameObject("HeroMedal", typeof(RectTransform), typeof(Image));
            medalObj.transform.SetParent(medalShowcase.transform, false);
            medalRect = medalObj.GetComponent<RectTransform>();
            float medalSize = 116f; // Large, prominent and rewarding
            medalRect.sizeDelta = new Vector2(medalSize, medalSize);
            medalRect.localScale = Vector3.zero;

            medalImage = medalObj.GetComponent<Image>();
            medalImage.sprite = MedalVisualUtility.GetMedalSprite(earnedMedal);
            medalImage.preserveAspect = true;

            // Medal Title Badge Text
            GameObject badgeObj = new GameObject("MedalBadgeText", typeof(RectTransform), typeof(TextMeshProUGUI));
            badgeObj.transform.SetParent(medalShowcase.transform, false);
            RectTransform badgeRt = badgeObj.GetComponent<RectTransform>();
            badgeRt.sizeDelta = new Vector2(300, 24);

            medalBadgeText = badgeObj.GetComponent<TextMeshProUGUI>();
            medalBadgeText.alignment = TextAlignmentOptions.Center;
            medalBadgeText.fontSize = 17;
            medalBadgeText.fontStyle = FontStyles.Bold;

            switch (earnedMedal)
            {
                case LevelMedal.Gold:
                    medalBadgeText.text = "★ GOLD MEDAL ★";
                    medalBadgeText.color = new Color(0.92f, 0.70f, 0.00f);
                    break;
                case LevelMedal.Silver:
                    medalBadgeText.text = "★ SILVER MEDAL ★";
                    medalBadgeText.color = new Color(0.55f, 0.62f, 0.72f);
                    break;
                case LevelMedal.Bronze:
                    medalBadgeText.text = "★ BRONZE MEDAL ★";
                    medalBadgeText.color = new Color(0.78f, 0.46f, 0.18f);
                    break;
                default:
                    medalBadgeText.text = "★ COMPLETED ★";
                    medalBadgeText.color = new Color(0.40f, 0.45f, 0.55f);
                    break;
            }

            // Stats row (Accuracy, Time, Mistakes)
            GameObject statsObj = new GameObject("StatsText", typeof(RectTransform), typeof(TextMeshProUGUI));
            statsObj.transform.SetParent(card.transform, false);
            TextMeshProUGUI stats = statsObj.GetComponent<TextMeshProUGUI>();
            string mistakesColor = mistakesVal == 0 ? "#28A745" : "#D9534F";
            stats.text = $"Accuracy: <b>{accuracyVal:F0}%</b>   |   Time: <b>{durationVal:F1}s</b>   |   Mistakes: <color={mistakesColor}><b>{mistakesVal}</b></color>";
            stats.fontSize = 15;
            stats.alignment = TextAlignmentOptions.Center;
            stats.color = new Color(0.18f, 0.22f, 0.28f);
            stats.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 26);

            // Continue Button
            GameObject btnObj = new GameObject("BtnContinue", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            btnObj.transform.SetParent(card.transform, false);
            LayoutElement btnLe = btnObj.GetComponent<LayoutElement>();
            btnLe.preferredHeight = 44;
            btnLe.preferredWidth = 200;

            Image btnImg = btnObj.GetComponent<Image>();
            btnImg.color = new Color(0.18f, 0.55f, 0.88f);

            continueBtn = btnObj.GetComponent<Button>();
            continueBtn.onClick.AddListener(CloseModal);

            // Add hover trigger (especially rewarding in Level 2: Hover)
            EventTrigger trigger = btnObj.AddComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            entry.callback.AddListener((data) => OnButtonPointerEnter());
            trigger.triggers.Add(entry);

            GameObject btnTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTxtObj.transform.SetParent(btnObj.transform, false);
            RectTransform btRt = btnTxtObj.GetComponent<RectTransform>();
            btRt.anchorMin = Vector2.zero;
            btRt.anchorMax = Vector2.one;
            btRt.offsetMin = Vector2.zero;
            btRt.offsetMax = Vector2.zero;

            btnTxt = btnTxtObj.GetComponent<TextMeshProUGUI>();
            btnTxt.text = isAutoAdvancing ? "<b>CONTINUING...</b>" : "<b>CONTINUE</b>";
            btnTxt.fontSize = 18;
            btnTxt.alignment = TextAlignmentOptions.Center;
            btnTxt.color = Color.white;
        }

        private IEnumerator AnimateSequence()
        {
            yield return new WaitForSeconds(0.25f);

            // Animate hero medal pop
            if (medalRect != null && medalImage != null)
            {
                int chimeRank = earnedMedal == LevelMedal.Gold ? 3 : (earnedMedal == LevelMedal.Silver ? 2 : 1);
                FeedbackManager.PlayMedalChime(chimeRank);

                yield return StartCoroutine(PopMedalRoutine(medalRect));
            }

            // If auto-advance (e.g. railroaded tutorial levels)
            if (isAutoAdvancing)
            {
                autoAdvanceCoroutine = StartCoroutine(AutoAdvanceRoutine());
                yield return autoAdvanceCoroutine;
            }
        }

        private IEnumerator PopMedalRoutine(RectTransform rt)
        {
            if (rt == null) yield break;

            Vector3 targetScale = Vector3.one;
            rt.localScale = Vector3.zero;

            float elapsed = 0f;
            float popDuration = 0.24f;

            while (elapsed < popDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / popDuration);
                float scaleFactor = Mathf.Sin(t * Mathf.PI * 0.75f) * 1.35f;
                rt.localScale = targetScale * scaleFactor;
                yield return null;
            }

            elapsed = 0f;
            float settleDuration = 0.16f;
            Vector3 startOvershoot = rt.localScale;
            while (elapsed < settleDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / settleDuration);
                rt.localScale = Vector3.Lerp(startOvershoot, targetScale, t);
                yield return null;
            }

            rt.localScale = targetScale;
        }

        private IEnumerator AutoAdvanceRoutine()
        {
            float totalWait = 3.5f;
            float elapsed = 0f;

            while (elapsed < totalWait)
            {
                elapsed += Time.unscaledDeltaTime;
                float remaining = Mathf.Max(0f, totalWait - elapsed);
                int secondsLeft = Mathf.CeilToInt(remaining);

                if (btnTxt != null)
                {
                    btnTxt.text = $"<b>CONTINUING ({secondsLeft}s)...</b>";
                }

                yield return null;
            }

            CloseModalInternal(false);
        }

        private void OnButtonPointerEnter()
        {
            if (isClosed) return;

            // In level 2 (Hover) or when auto-advancing, hovering the continue button advances
            if (levelDef != null && string.Equals(levelDef.levelId, "level2", StringComparison.OrdinalIgnoreCase))
            {
                CloseModalInternal(true);
            }
        }

        private void Update()
        {
            if (isClosed) return;

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Escape))
            {
                CloseModalInternal(true);
            }
        }

        public void CloseModal()
        {
            CloseModalInternal(true);
        }

        private void CloseModalInternal(bool isUserAction)
        {
            if (isClosed) return;
            isClosed = true;

            if (autoAdvanceCoroutine != null)
            {
                StopCoroutine(autoAdvanceCoroutine);
                autoAdvanceCoroutine = null;
            }

            if (isUserAction)
            {
                FeedbackManager.PlayClick();
            }

            onClosedCallback?.Invoke();
            Destroy(gameObject);
        }
    }
}
