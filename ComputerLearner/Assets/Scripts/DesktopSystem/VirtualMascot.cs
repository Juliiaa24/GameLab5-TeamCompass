/**
 * Author: Diego
 * Date: 01/10/26
 * Description: A virtual pet that guides the kid through the virtual desktop.
*/
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace ComputerLearning
{
    [System.Serializable]
    public class MascotFeedbackDialogs
    {
        public string companionWelcome = "Hello! I'm your desktop pet! Click on me whenever you want tips.";
        public string[] companionMessages = new string[]
        {
            "Hello! I'm your desktop pet! Feel free to explore at your own pace.",
            "Open the Minigames folder to practice your clicking accuracy and speed!",
            "Did you know? Right-click anywhere on the desktop to create a New Folder.",
            "Try opening Notepad to practice typing words on the keyboard!",
            "Remember: double-click quickly to open folders and apps!",
            "Click the Start button on the bottom-left to see all apps and options.",
            "You're doing great with the mouse! Keep it up!",
            "You can drag windows from their top bar to move them around the desktop.",
            "If a window is too small, click the square button to maximize it!",
            "I'll always be here in the corner whenever you need advice!"
        };
        public string wrongLeftClick = "That was a left click! Try using the right mouse button!";
        public string wrongRightClick = "Wrong click! Try using the left mouse button.";
        public string almostOut = "Almost out! Aim closer to the center!";
        public string singleClick = "Just a single click here!";
        public string[] inactivityMessages = new string[]
        {
            "Take your time! Whenever you're ready, give it a click!",
            "Need a hand? Aim carefully at the target area!",
            "You're doing great! Keep going!",
            "Don't worry, take all the time you need!"
        };
        public string holdKeepPressing = "Keep pressing! Hold the button down a bit longer!";
        public string holdStayInside = "Stay inside the circle while holding down!";
        public string clickMissed = "Missed! Aim carefully inside the target circle!";
        public string doubleClickFast = "Click twice quickly! Double-click!";
        public string dragAlmost = "Almost! Drag the item all the way into the target zone!";
        public string hoverKeepSteady = "Oops! Keep the cursor steady inside the target until the bar fills!";
        public string hoverNoClick = "No need to click! Just hold the mouse cursor over the circle!";
        public string hoverDontClickOutside = "Don't click outside! Move your mouse inside the circle and hold it steady!";
        public string moveKeepMoving = "Keep moving the mouse to clean all the spots!";
        public string moveNoClick = "Just move the mouse over the spots without clicking!";
        public string rightClickReminder = "Click the right mouse button on the target circle!";
        public string rightClickAimInside = "Aim inside the circle and click the right mouse button!";
    }

    public class VirtualMascot : MonoBehaviour, IPointerClickHandler
    {
        public static VirtualMascot Instance { get; private set; }

        private static MascotFeedbackDialogs cachedFeedbackDialogs;
        private static string cachedFeedbackLang;

        public static MascotFeedbackDialogs FeedbackDialogs
        {
            get
            {
                string currentLang = LocalizationManager.CurrentLanguage;
                if (cachedFeedbackDialogs == null || cachedFeedbackLang != currentLang)
                {
                    cachedFeedbackLang = currentLang;
                    TextAsset json = LocalizationManager.LoadLocalizedResource("MascotFeedbackDialogs");
                    cachedFeedbackDialogs = json != null ? JsonUtility.FromJson<MascotFeedbackDialogs>(json.text) : null;
                    if (cachedFeedbackDialogs == null)
                    {
                        cachedFeedbackDialogs = new MascotFeedbackDialogs();
                    }
                }
                return cachedFeedbackDialogs;
            }
        }

        [Header("References")]
        public RectTransform mascotRect;
        public GameObject speechBubble;
        public Text speechText;

        [Header("Settings")]
        public float followSpeed = 6f;
        [Tooltip("The separation of the speech bubble from the mascot")]
        public Vector2 speechBubbleOffset = new Vector2(80f, -40f);
        [Tooltip("Should the mascot automatically flip its graphic if it is on the right side of the screen?")]
        public bool autoFlipMascot = true;

        [Header("Desktop Companion Mode")]
        [SerializeField] private bool isDesktopCompanion = false;
        [SerializeField] private Vector2 companionCornerOffset = new Vector2(-45f, -45f);

        public bool IsDesktopCompanion => isDesktopCompanion;

        private RectTransform targetRect;
        private float floatTimer;
        private Coroutine bounceCoroutine;
        private Coroutine hideBubbleCoroutine;
        private int lastMessageIndex = -1;

        private void Awake()
        {
            Instance = this;
            if (mascotRect == null) mascotRect = GetComponent<RectTransform>();
            EnsureSpeechBubbleLayout();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (mascotRect == null) mascotRect = GetComponent<RectTransform>();
            EnsureSpeechBubbleLayout();

            if (isDesktopCompanion)
            {
                InitAsDesktopCompanion();
                return;
            }

            // Make sure the mascot and its speech bubble NEVER block mouse clicks during tutorials!
            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        private void EnsureSpeechBubbleLayout()
        {
            if (speechBubble == null) return;

            Image bubbleImg = speechBubble.GetComponent<Image>();
            if (bubbleImg != null) bubbleImg.raycastTarget = false;

            UnityEngine.UI.HorizontalLayoutGroup hlg = speechBubble.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            if (hlg == null && speechBubble.GetComponent<UnityEngine.UI.VerticalLayoutGroup>() == null)
            {
                hlg = speechBubble.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                hlg.padding = new RectOffset(22, 22, 16, 16);
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.childControlHeight = true;
                hlg.childControlWidth = true;
                hlg.childForceExpandHeight = false;
                hlg.childForceExpandWidth = false;
            }

            UnityEngine.UI.ContentSizeFitter csf = speechBubble.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            if (csf == null) csf = speechBubble.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            csf.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

            if (speechText != null)
            {
                speechText.raycastTarget = false;
                speechText.horizontalOverflow = HorizontalWrapMode.Wrap;
                speechText.verticalOverflow = VerticalWrapMode.Overflow;
                if (speechText.fontSize > 22)
                {
                    speechText.fontSize = 22;
                }
            }
        }

        private void ApplyTextAndRebuildBubble(string message)
        {
            EnsureSpeechBubbleLayout();
            if (speechBubble == null || speechText == null) return;

            bool hasMessage = !string.IsNullOrEmpty(message);
            speechBubble.SetActive(hasMessage);
            speechText.text = message ?? string.Empty;

            if (hasMessage)
            {
                UnityEngine.UI.LayoutElement layoutElement = speechText.GetComponent<UnityEngine.UI.LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = speechText.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
                }

                // Reset to unconstrained to calculate raw width
                layoutElement.preferredWidth = -1;
                layoutElement.preferredHeight = -1;

                float rawWidth = speechText.preferredWidth;
                if (rawWidth > 380f)
                {
                    layoutElement.preferredWidth = 380f;
                }

                // Force the layout to rebuild immediately so it resizes vertically before the next frame
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(speechText.GetComponent<RectTransform>());
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(speechBubble.GetComponent<RectTransform>());
            }
        }

        private void Update()
        {
            if (isDesktopCompanion)
            {
                UpdateCompanionPosition();
                return;
            }

            if (targetRect != null && !targetRect.gameObject.activeInHierarchy)
            {
                // If target was destroyed or hidden, hide mascot
                Hide();
                return;
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            float scale = canvas != null ? canvas.scaleFactor : 1f;

            if (targetRect != null && targetRect.gameObject.activeInHierarchy)
            {
                // Get the true visual center of the target, regardless of where its pivot is set
                Vector3[] corners = new Vector3[4];
                targetRect.GetWorldCorners(corners);
                Vector3 basePos = (corners[0] + corners[2]) / 2f;
                
                // Float animation (reduced so the tip stays on target)
                floatTimer += Time.deltaTime;
                float floatOffset = Mathf.Sin(floatTimer * 4f) * 5f; 
                
                Vector3 finalTargetPos = basePos + new Vector3(0, floatOffset * scale, 0);

                // Clamp to screen bounds to prevent going completely off-screen
                float margin = 5f * scale; 
                finalTargetPos.x = Mathf.Clamp(finalTargetPos.x, margin, Screen.width - margin);
                finalTargetPos.y = Mathf.Clamp(finalTargetPos.y, margin, Screen.height - margin);

                mascotRect.position = Vector3.Lerp(mascotRect.position, finalTargetPos, Time.deltaTime * followSpeed);
            }

            UpdateSpeechBubblePlacement(canvas);
        }

        private void UpdateSpeechBubblePlacement(Canvas canvas)
        {
            if (mascotRect == null) return;

            bool isOnRightSide = false;
            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            if (autoFlipMascot)
            {
                Vector3 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, mascotRect.position);
                isOnRightSide = screenPoint.x > Screen.width * 0.5f;
            }

            float flipScale = isOnRightSide ? -1f : 1f;
            mascotRect.localScale = new Vector3(flipScale, 1f, 1f);

            if (speechBubble != null && speechBubble.activeSelf)
            {
                // Counter-flip the speech bubble so the text remains readable
                speechBubble.transform.localScale = new Vector3(flipScale, 1f, 1f);
                
                RectTransform bubbleRect = speechBubble.GetComponent<RectTransform>();
                Vector2 finalBubblePos = new Vector2(speechBubbleOffset.x, speechBubbleOffset.y);

                if (isOnRightSide)
                {
                    bubbleRect.pivot = new Vector2(1f, 1f);
                }
                else
                {
                    bubbleRect.pivot = new Vector2(0f, 1f);
                }
                
                bubbleRect.anchoredPosition = finalBubblePos;
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(bubbleRect);

                // Clamp vertically and horizontally to screen bounds
                Vector3[] bubbleCorners = new Vector3[4];
                bubbleRect.GetWorldCorners(bubbleCorners);
                
                Vector3 bottomEdge = RectTransformUtility.WorldToScreenPoint(cam, bubbleCorners[0]);
                Vector3 topEdge = RectTransformUtility.WorldToScreenPoint(cam, bubbleCorners[1]);
                
                float marginY = 14f;
                float currentScale = canvas != null ? canvas.scaleFactor : 1f;
                
                if (bottomEdge.y < marginY)
                {
                    float difference = (marginY - bottomEdge.y) / currentScale;
                    finalBubblePos.y += difference;
                }
                else if (topEdge.y > Screen.height - marginY)
                {
                    float difference = (topEdge.y - (Screen.height - marginY)) / currentScale;
                    finalBubblePos.y -= difference;
                }
                
                bubbleRect.anchoredPosition = finalBubblePos;
            }
        }

        public static void Show(string message, RectTransform target, Vector2? offset = null)
        {
            if (Instance == null)
            {
                Instance = Object.FindFirstObjectByType<VirtualMascot>(FindObjectsInactive.Include);
                
                if (Instance == null)
                {
                    Debug.LogWarning("[VirtualMascot] Mascot not found in the scene! Ensure DesktopManager has spawned it.");
                    return;
                }
            }
            if (Instance != null) Instance.ShowMessage(message, target, offset);
        }

        public static void HideMascot()
        {
            if (Instance != null) Instance.Hide();
        }

        public static void HideMascotIfTargeting(Transform potentialParent)
        {
            if (Instance != null && !Instance.isDesktopCompanion && Instance.targetRect != null)
            {
                if (Instance.targetRect.IsChildOf(potentialParent) || Instance.targetRect == potentialParent)
                {
                    Instance.Hide();
                }
            }
        }

        #region Contextual Mistake & Feedback System
        public static void ShowContextualFeedback(string message, RectTransform target = null, float duration = 3.5f)
        {
            if (Instance == null)
            {
                Instance = Object.FindFirstObjectByType<VirtualMascot>(FindObjectsInactive.Include);
            }
            if (Instance != null)
            {
                Instance.ShowTemporaryFeedback(message, target, duration);
            }
        }

        public static void ShowWrongClickFeedback(RectTransform target, bool isRightClickExpected)
        {
            MascotFeedbackDialogs fb = FeedbackDialogs;
            string msg = isRightClickExpected ? fb.wrongLeftClick : fb.wrongRightClick;
            ShowContextualFeedback(msg, target, 3.5f);
        }

        public static void ShowAlmostOutFeedback(RectTransform target)
        {
            ShowContextualFeedback(FeedbackDialogs.almostOut, target, 3.0f);
        }

        public static void ShowSingleClickFeedback(RectTransform target)
        {
            ShowContextualFeedback(FeedbackDialogs.singleClick, target, 3.0f);
        }

        public static void ShowInactivityReminder(RectTransform target = null)
        {
            string[] inactivityPool = FeedbackDialogs.inactivityMessages;
            if (inactivityPool == null || inactivityPool.Length == 0) return;
            string msg = inactivityPool[UnityEngine.Random.Range(0, inactivityPool.Length)];
            ShowContextualFeedback(msg, target, 4.0f);
        }

        public void ShowTemporaryFeedback(string message, RectTransform target, float duration)
        {
            ShowMessage(message, target);
            if (bounceCoroutine != null) StopCoroutine(bounceCoroutine);
            bounceCoroutine = StartCoroutine(BounceAnimation());

            if (hideBubbleCoroutine != null) StopCoroutine(hideBubbleCoroutine);
            hideBubbleCoroutine = StartCoroutine(AutoHideFeedback(duration));
        }

        private IEnumerator AutoHideFeedback(float delay)
        {
            yield return new WaitForSeconds(delay);
            Hide();
            hideBubbleCoroutine = null;
        }
        #endregion

        public void ShowMessage(string message, RectTransform target, Vector2? offset = null)
        {
            if (hideBubbleCoroutine != null)
            {
                StopCoroutine(hideBubbleCoroutine);
                hideBubbleCoroutine = null;
            }

            // Temporarily exit companion mode while showing targeted tutorial/intro messages
            isDesktopCompanion = false;

            gameObject.SetActive(true);
            targetRect = target;

            if (mascotRect == null) mascotRect = GetComponent<RectTransform>();
            if (mascotRect != null)
            {
                mascotRect.anchorMin = new Vector2(0.5f, 0.5f);
                mascotRect.anchorMax = new Vector2(0.5f, 0.5f);
                mascotRect.pivot = new Vector2(0.125f, 1.13f);
                if (targetRect == null)
                {
                    mascotRect.anchoredPosition = offset ?? new Vector2(-120f, 60f);
                }
            }

            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
            cg.interactable = false;
            cg.blocksRaycasts = false;

            ApplyTextAndRebuildBubble(message);

            Canvas canvas = GetComponentInParent<Canvas>();
            UpdateSpeechBubblePlacement(canvas);

            transform.SetAsLastSibling(); 
        }

        public void Hide()
        {
            if (DesktopManager.Instance != null && DesktopManager.Instance.EnableMascotCompanion)
            {
                InitAsDesktopCompanion();
                return;
            }
            if (isDesktopCompanion) return; // Companion is always visible!
            gameObject.SetActive(false);
            targetRect = null;
        }

        #region Desktop Companion Implementation
        /// <summary>
        /// Initializes the mascot as a persistent, interactive desktop pet in the top-right corner.
        /// </summary>
        public void InitAsDesktopCompanion()
        {
            isDesktopCompanion = true;
            targetRect = null;
            gameObject.SetActive(true);

            EnsureSpeechBubbleLayout();

            if (mascotRect == null) mascotRect = GetComponent<RectTransform>();

            // Anchor directly to top-right corner
            mascotRect.anchorMin = new Vector2(1f, 1f);
            mascotRect.anchorMax = new Vector2(1f, 1f);
            mascotRect.pivot = new Vector2(1f, 1f);
            mascotRect.anchoredPosition = companionCornerOffset;
            mascotRect.localScale = Vector3.one;

            // Make sure clicking on the mascot works smoothly!
            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
            cg.interactable = true;
            cg.blocksRaycasts = true;

            Image mascotImg = GetComponentInChildren<Image>();
            if (mascotImg != null) mascotImg.raycastTarget = true;

            // Configure speech bubble to display cleanly to the left of the mascot
            if (speechBubble != null)
            {
                Image bubbleImg = speechBubble.GetComponent<Image>();
                if (bubbleImg != null) bubbleImg.raycastTarget = false;

                RectTransform bubbleRt = speechBubble.GetComponent<RectTransform>();
                bubbleRt.pivot = new Vector2(1f, 1f);
                bubbleRt.anchoredPosition = new Vector2(-110f, -20f);
            }

            // Welcome greeting on desktop startup
            ShowCompanionMessage(FeedbackDialogs.companionWelcome);
        }

        private void UpdateCompanionPosition()
        {
            if (mascotRect == null) return;

            // Idle floating / breathing bobbing animation in top-right corner
            floatTimer += Time.unscaledDeltaTime;
            float floatOffset = Mathf.Sin(floatTimer * 3f) * 6f;
            mascotRect.anchoredPosition = companionCornerOffset + new Vector2(0f, floatOffset);
        }

        /// <summary>
        /// Detects user clicks on the mascot and triggers reaction dialogue.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isDesktopCompanion) return;
            if (eventData.button != PointerEventData.InputButton.Left) return;

            TriggerCompanionDialogue();
        }

        public void TriggerCompanionDialogue()
        {
            if (bounceCoroutine != null) StopCoroutine(bounceCoroutine);
            bounceCoroutine = StartCoroutine(BounceAnimation());

            string msg = GetNextCompanionMessage();
            ShowCompanionMessage(msg);
        }

        public void ShowCompanionMessage(string message)
        {
            if (speechBubble == null || speechText == null) return;

            if (hideBubbleCoroutine != null) StopCoroutine(hideBubbleCoroutine);

            ApplyTextAndRebuildBubble(message);

            RectTransform bubbleRt = speechBubble.GetComponent<RectTransform>();
            if (bubbleRt != null)
            {
                bubbleRt.pivot = new Vector2(1f, 1f);
                bubbleRt.anchoredPosition = new Vector2(-110f, -20f);
                speechBubble.transform.localScale = Vector3.one;
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(bubbleRt);
            }

            hideBubbleCoroutine = StartCoroutine(AutoHideBubble(5f));
        }

        private IEnumerator AutoHideBubble(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (speechBubble != null)
            {
                speechBubble.SetActive(false);
            }
            hideBubbleCoroutine = null;
        }

        private IEnumerator BounceAnimation()
        {
            Vector3 originalScale = Vector3.one;
            Vector3 squashed = new Vector3(1.22f, 0.82f, 1f);
            Vector3 stretched = new Vector3(0.88f, 1.18f, 1f);

            float elapsed = 0f;
            float dur1 = 0.08f;
            while (elapsed < dur1)
            {
                elapsed += Time.unscaledDeltaTime;
                mascotRect.localScale = Vector3.Lerp(originalScale, squashed, elapsed / dur1);
                yield return null;
            }

            elapsed = 0f;
            float dur2 = 0.09f;
            while (elapsed < dur2)
            {
                elapsed += Time.unscaledDeltaTime;
                mascotRect.localScale = Vector3.Lerp(squashed, stretched, elapsed / dur2);
                yield return null;
            }

            elapsed = 0f;
            float dur3 = 0.1f;
            while (elapsed < dur3)
            {
                elapsed += Time.unscaledDeltaTime;
                mascotRect.localScale = Vector3.Lerp(stretched, originalScale, elapsed / dur3);
                yield return null;
            }

            mascotRect.localScale = originalScale;
            bounceCoroutine = null;
        }

        private string GetNextCompanionMessage()
        {
            string[] pool = FeedbackDialogs.companionMessages;
            if (pool == null || pool.Length == 0) return "Hello!";

            int newIndex;
            if (pool.Length == 1) newIndex = 0;
            else
            {
                do
                {
                    newIndex = Random.Range(0, pool.Length);
                } while (newIndex == lastMessageIndex);
            }

            lastMessageIndex = newIndex;
            return pool[newIndex];
        }
        #endregion
    }
}
