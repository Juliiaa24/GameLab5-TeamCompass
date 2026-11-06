using UnityEngine;
using TMPro;

namespace ComputerLearning
{
    /// <summary>
    /// Lightweight floating score popup (+100 in green, -25 in red) spawned at the target's position
    /// when a player hits a target or makes a mistake.
    /// </summary>
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public class FloatingScoreText : MonoBehaviour
    {
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private float elapsed;
        private const float Duration = 0.85f;
        private const float FloatSpeed = 55f;

        public static void Spawn(Transform contextTransform, Vector3 worldPosition, int pointDelta, string customText = null)
        {
            if (pointDelta == 0 && string.IsNullOrEmpty(customText)) return;

            // Choose a persistent parent (LevelRunner.taskContentArea or Window/Canvas) so the popup
            // is not destroyed when the last target of a task completes and destroys the task prefab.
            Transform spawnParent = null;
            if (contextTransform != null)
            {
                LevelRunner runner = contextTransform.GetComponentInParent<LevelRunner>();
                if (runner != null && runner.taskContentArea != null)
                {
                    spawnParent = runner.taskContentArea;
                }
                else
                {
                    Canvas canvas = contextTransform.GetComponentInParent<Canvas>();
                    if (canvas != null) spawnParent = canvas.transform;
                    else spawnParent = contextTransform;
                }
            }
            else if (LevelRunner.ActiveRunner != null && LevelRunner.ActiveRunner.taskContentArea != null)
            {
                spawnParent = LevelRunner.ActiveRunner.taskContentArea;
            }

            if (spawnParent == null) return;

            GameObject go = new GameObject("FloatingScoreText", typeof(RectTransform), typeof(CanvasGroup), typeof(TextMeshProUGUI), typeof(FloatingScoreText));
            go.transform.SetParent(spawnParent, false);
            go.transform.SetAsLastSibling();

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(180f, 44f);
            rt.position = worldPosition;

            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            cg.interactable = false;
            cg.blocksRaycasts = false;
            cg.alpha = 1f;

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.raycastTarget = false;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 28;
            tmp.fontStyle = FontStyles.Bold;
            tmp.text = !string.IsNullOrEmpty(customText)
                ? customText
                : (pointDelta > 0 ? $"+{pointDelta}" : $"{pointDelta}");

            if (pointDelta >= 0)
            {
                // Emerald green (#22C55E)
                tmp.color = new Color(0.133f, 0.773f, 0.369f, 1f);
            }
            else
            {
                // Coral red (#EF4444)
                tmp.color = new Color(0.937f, 0.267f, 0.267f, 1f);
            }

            FloatingScoreText popup = go.GetComponent<FloatingScoreText>();
            popup.rectTransform = rt;
            popup.canvasGroup = cg;
            popup.elapsed = 0f;
        }

        private void Awake()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Update()
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Duration);

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition += new Vector2(0f, FloatSpeed * Time.unscaledDeltaTime);

                // Subtle pop scale in first 0.2s
                float scale = t < 0.2f
                    ? Mathf.Lerp(0.85f, 1.18f, t / 0.2f)
                    : Mathf.Lerp(1.18f, 1.0f, Mathf.Clamp01((t - 0.2f) / 0.25f));
                rectTransform.localScale = new Vector3(scale, scale, 1f);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = t < 0.4f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.4f) / 0.6f);
            }

            if (elapsed >= Duration)
            {
                Destroy(gameObject);
            }
        }
    }
}
