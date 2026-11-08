using UnityEngine;
using TMPro;

namespace ComputerLearning
{
    /// <summary>
    /// Lightweight floating score popup (+pts in green at target position, -pts in red above the mouse cursor)
    /// rendered on top of the active Canvas so targets never obscure it.
    /// </summary>
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public class FloatingScoreText : MonoBehaviour
    {
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private float elapsed;
        private const float Duration = 0.90f;
        private const float FloatSpeed = 60f;

        public static void Spawn(Transform contextTransform, Vector3 worldPosition, int pointDelta, string customText = null)
        {
            if (pointDelta == 0 && string.IsNullOrEmpty(customText)) return;

            Canvas canvas = FindTargetCanvas(contextTransform);
            Transform spawnParent = canvas != null ? canvas.transform : null;
            if (spawnParent == null && LevelRunner.ActiveRunner != null && LevelRunner.ActiveRunner.taskContentArea != null)
            {
                spawnParent = LevelRunner.ActiveRunner.taskContentArea;
            }
            if (spawnParent == null && contextTransform != null)
            {
                spawnParent = contextTransform;
            }
            if (spawnParent == null) return;

            CreatePopup(spawnParent, canvas, worldPosition, false, Vector2.zero, pointDelta, customText);
        }

        public static void SpawnAtMouseCursor(Transform contextTransform, int pointDelta, string customText = null)
        {
            if (pointDelta == 0 && string.IsNullOrEmpty(customText)) return;

            Canvas canvas = FindTargetCanvas(contextTransform);
            Transform spawnParent = canvas != null ? canvas.transform : null;
            if (spawnParent == null && LevelRunner.ActiveRunner != null && LevelRunner.ActiveRunner.taskContentArea != null)
            {
                spawnParent = LevelRunner.ActiveRunner.taskContentArea;
            }
            if (spawnParent == null && contextTransform != null)
            {
                spawnParent = contextTransform;
            }
            if (spawnParent == null) return;

            Vector2 screenPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                screenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            }
            else if (Input.mousePresent)
            {
                screenPos = Input.mousePosition;
            }

            float scale = canvas != null ? canvas.scaleFactor : 1f;
            // Offset slightly above the cursor tip so the cursor does not cover the text
            screenPos += new Vector2(0f, 28f * scale);

            CreatePopup(spawnParent, canvas, Vector3.zero, true, screenPos, pointDelta, customText);
        }

        private static Canvas FindTargetCanvas(Transform contextTransform)
        {
            if (contextTransform != null)
            {
                Canvas c = contextTransform.GetComponentInParent<Canvas>();
                if (c != null) return c.rootCanvas != null ? c.rootCanvas : c;
            }
            if (LevelRunner.ActiveRunner != null)
            {
                Canvas c = LevelRunner.ActiveRunner.GetComponentInParent<Canvas>();
                if (c != null) return c.rootCanvas != null ? c.rootCanvas : c;
            }
            return Object.FindAnyObjectByType<Canvas>();
        }

        private static void CreatePopup(
            Transform spawnParent,
            Canvas canvas,
            Vector3 worldPosition,
            bool useScreenPos,
            Vector2 screenPos,
            int pointDelta,
            string customText)
        {
            GameObject go = new GameObject("FloatingScoreText", typeof(RectTransform), typeof(CanvasGroup), typeof(TextMeshProUGUI), typeof(FloatingScoreText));
            go.transform.SetParent(spawnParent, false);
            go.transform.SetAsLastSibling();

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200f, 48f);

            if (useScreenPos)
            {
                RectTransform parentRt = spawnParent as RectTransform;
                Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
                if (parentRt != null && RectTransformUtility.ScreenPointToWorldPointInRectangle(parentRt, screenPos, cam, out Vector3 worldPt))
                {
                    rt.position = worldPt;
                }
                else
                {
                    rt.position = new Vector3(screenPos.x, screenPos.y, 0f);
                }
            }
            else
            {
                rt.position = worldPosition;
            }

            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            cg.interactable = false;
            cg.blocksRaycasts = false;
            cg.alpha = 1f;

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.raycastTarget = false;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 30;
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
                // Bright coral red (#EF4444)
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
                    ? Mathf.Lerp(0.85f, 1.22f, t / 0.2f)
                    : Mathf.Lerp(1.22f, 1.0f, Mathf.Clamp01((t - 0.2f) / 0.25f));
                rectTransform.localScale = new Vector3(scale, scale, 1f);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = t < 0.45f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.45f) / 0.55f);
            }

            if (elapsed >= Duration)
            {
                Destroy(gameObject);
            }
        }
    }
}
