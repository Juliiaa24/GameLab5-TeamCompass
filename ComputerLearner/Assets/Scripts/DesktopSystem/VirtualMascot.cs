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

        // Medal Improvement Feedback
        public string[] medalImprovementMessages = new string[]
        {
            "Hey, you really improved this one!",
            "Whoa, a shiny new medal! Look how much you've improved!",
            "New personal best medal! Your practice is really paying off!",
            "Amazing upgrade! You did even better than last time!",
            "Level up! You beat your old medal on this challenge!",
            "Look at that upgrade! You're becoming a true computer master!",
            "Fantastic improvement! That new medal looks great on you!"
        };
        public string medalImprovementUpgradeFormat = "{0}\nUpgraded from {1} to {2} Medal! ({3:F0}%)";
        public string medalImprovementEarnedFormat = "{0}\nYou earned the {1} Medal! ({2:F0}%)";

        // ClickHoldTask Feedback
        public string holdKeepPressing = "Keep pressing! Hold the button down a bit longer!";
        public string holdStayInside = "Stay inside the circle while holding down!";
        public string holdHoverStagnant = "You're on the target! Now press and hold down the left mouse button!";
        public string holdInactivityReminder = "Move inside the circle, then press and hold the left mouse button!";
        public string holdMissedBackground = "Missed! Move inside the target circle, then press and hold the left button!";
        public string[] holdQuickTapMessages = new string[]
        {
            "That was a quick tap! Press the left button and keep your finger held down!",
            "Don't let go right away! Press and hold the left mouse button down until the circle fills!",
            "Hold it down! Keep pressing the left button without lifting your finger!"
        };
        public string[] holdMidReleaseMessages = new string[]
        {
            "Good start! Keep holding the left button down a little bit longer!",
            "You're doing it! Just keep your finger pressed down until the ring is completely full!",
            "Don't lift your finger yet! Hold the left button down a bit longer!"
        };
        public string[] holdAlmostDoneMessages = new string[]
        {
            "So close! You almost filled it! Hold the button down just a tiny bit longer!",
            "Almost there! Just half a second more—keep that left button pressed!",
            "Ooh, right at the end! Hold it down until the circle finishes filling up!"
        };
        public string[] holdSlippedOutsideMessages = new string[]
        {
            "Oops, your mouse slipped outside! Keep the cursor steady inside the circle while holding!",
            "Stay inside the circle while holding the button down! You've got this!"
        };

        // ClickTargetTask Feedback
        public string clickMissed = "Missed! Aim carefully inside the target circle!";
        public string clickDelayReminder = "Aim carefully and click inside the circle!";
        public string clickHoverStagnant = "Click the left mouse button!";

        // DoubleClickTask Feedback
        public string doubleClickFast = "Click twice quickly! Double-click!";
        public string doubleClickHoverStagnant = "You're on the circle! Now click the left button twice super fast: click-click!";
        public string doubleClickInactivityReminder = "Move inside the circle and double-click quickly with the left button!";
        public string doubleClickLeftBeforeSecond = "Oops, keep the mouse steady inside the circle while clicking twice!";
        public string doubleClickMissedBackground = "Missed the circle! Move your cursor inside the target and double-click quickly!";
        public string[] doubleClickSingleClickMessages = new string[]
        {
            "That was just one click! Tap the left mouse button twice fast: click-click!",
            "Too slow between clicks! Try tapping the left button two times quickly without moving the mouse!",
            "Almost! Give the left mouse button two quick taps in a row: tap-tap!"
        };

        // DragDropTask Feedback
        public string dragAlmost = "Almost! Drag the item all the way into the target zone!";
        public string dragTapWithoutDrag = "That was a quick click! Hold down the left mouse button and move the mouse to drag it!";
        public string dragDwellReminder = "Great! Now move the piece into the target zone and release the button!";
        public string dragInactivityReminder = "Press and hold the left button on a piece, then drag it into the target zone!";
        public string dragMissedBackground = "Aim at the draggable piece, hold the left button down, and drag it to the target area!";
        public string[] dragMissedDropMessages = new string[]
        {
            "Almost! Keep holding the left button down and drag the piece all the way into the target zone!",
            "So close! Drag the item right inside the target zone before letting go of the button!",
            "Don't let go too early! Move the piece inside the target circle and then release!"
        };

        // HoverTargetTask Feedback
        public string hoverKeepSteady = "Oops! Keep the cursor steady inside the target until the bar fills!";
        public string hoverNoClick = "No need to click! Just hold the mouse cursor over the circle!";
        public string hoverDontClickOutside = "Don't click outside! Move your mouse inside the circle and hold it steady!";
        public string hoverStartReminder = "Move your mouse over the target circle and hold it steady!";

        // MoveMouseTask Feedback
        public string moveKeepMoving = "Keep moving the mouse to clean all the spots!";
        public string moveNoClick = "Just move the mouse over the spots without clicking!";

        // RightClickTask Feedback
        public string rightClickReminder = "Click the right mouse button on the target circle!";
        public string rightClickAimInside = "Aim inside the circle and click the right mouse button!";
        public string rightClickHoverStagnant = "You're right on target! Now press the RIGHT mouse button (with your middle finger)!";
        public string rightClickBackgroundLeftClick = "Careful! Aim inside the target circle and use the RIGHT mouse button!";
        public string rightClickBackgroundRightClick = "Good button! Now aim inside the target circle and right-click!";

        public static string PickRandom(string[] pool, string fallback)
        {
            if (pool != null && pool.Length > 0)
            {
                string picked = pool[UnityEngine.Random.Range(0, pool.Length)];
                if (!string.IsNullOrEmpty(picked)) return picked;
            }
            return fallback;
        }
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
        private static int lastMedalImprovementIndex = -1;
        private RectTransform mascotVisualRect;

        private RectTransform GetMascotVisualRect()
        {
            if (mascotVisualRect != null) return mascotVisualRect;
            Transform child = transform.Find("MascotImage");
            if (child != null)
            {
                mascotVisualRect = child as RectTransform;
                return mascotVisualRect;
            }
            Image[] images = GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img != null && img.gameObject != gameObject && (speechBubble == null || !img.transform.IsChildOf(speechBubble.transform)))
                {
                    mascotVisualRect = img.GetComponent<RectTransform>();
                    return mascotVisualRect;
                }
            }
            return null;
        }

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

                // Float animation isolated to mascotVisualRect so speechBubble does not bob
                floatTimer += Time.deltaTime;
                float floatOffset = Mathf.Sin(floatTimer * 4f) * 5f;

                RectTransform vis = GetMascotVisualRect();
                if (vis != null) vis.anchoredPosition = new Vector2(0f, floatOffset);
                Vector3 finalTargetPos = basePos;

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

        public static string GetRandomMedalImprovementMessage()
        {
            string[] pool = FeedbackDialogs.medalImprovementMessages;
            if (pool == null || pool.Length == 0)
                return "Hey, you really improved this one!";

            int idx;
            if (pool.Length == 1) idx = 0;
            else
            {
                do
                {
                    idx = UnityEngine.Random.Range(0, pool.Length);
                } while (idx == lastMedalImprovementIndex);
            }
            lastMedalImprovementIndex = idx;
            return pool[idx];
        }

        public static void ShowMedalImprovementFeedback(LevelMedal previousMedal, LevelMedal newMedal, float accuracy, float durationSeconds, RectTransform target = null, float displayDuration = 5.5f)
        {
            MascotFeedbackDialogs fb = FeedbackDialogs;
            string praise = GetRandomMedalImprovementMessage();
            string newMedalName = LevelScoreRecord.GetMedalName(newMedal).ToUpper();
            string prevMedalName = LevelScoreRecord.GetMedalName(previousMedal);

            string msg;
            if (previousMedal != LevelMedal.None)
            {
                string fmt = !string.IsNullOrEmpty(fb.medalImprovementUpgradeFormat)
                    ? fb.medalImprovementUpgradeFormat
                    : "{0}\nUpgraded from {1} to {2} Medal! ({3:F0}%)";
                msg = string.Format(fmt, praise, prevMedalName, newMedalName, accuracy);
            }
            else
            {
                string fmt = !string.IsNullOrEmpty(fb.medalImprovementEarnedFormat)
                    ? fb.medalImprovementEarnedFormat
                    : "{0}\nYou earned the {1} Medal! ({2:F0}%)";
                msg = string.Format(fmt, praise, newMedalName, accuracy);
            }
            ShowContextualFeedback(msg, target, displayDuration);
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
            bool wasActive = gameObject.activeSelf && !isDesktopCompanion;
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
                else if (!wasActive && targetRect.gameObject.activeInHierarchy)
                {
                    Vector3[] corners = new Vector3[4];
                    targetRect.GetWorldCorners(corners);
                    mascotRect.position = (corners[0] + corners[2]) / 2f;
                }
            }

            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
            cg.interactable = false;
            cg.blocksRaycasts = false;

            ApplyTextAndRebuildBubble(message);

            Canvas canvas = GetComponentInParent<Canvas>();
            UpdateSpeechBubblePlacement(canvas);

            RectTransform vis = GetMascotVisualRect();
            if (vis != null) vis.localScale = Vector3.one;
            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            if (hideBubbleCoroutine != null)
            {
                StopCoroutine(hideBubbleCoroutine);
                hideBubbleCoroutine = null;
            }
            if (bounceCoroutine != null)
            {
                StopCoroutine(bounceCoroutine);
                bounceCoroutine = null;
            }
            RectTransform vis = GetMascotVisualRect();
            if (vis != null) vis.localScale = Vector3.one;
            isDesktopCompanion = false;
            targetRect = null;
            gameObject.SetActive(false);
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
            mascotRect.anchoredPosition = companionCornerOffset;
            RectTransform vis = GetMascotVisualRect();
            if (vis != null) vis.anchoredPosition = new Vector2(0f, floatOffset);
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
            RectTransform bounceTarget = GetMascotVisualRect() ?? mascotRect;
            Vector3 originalScale = Vector3.one;
            Vector3 squashed = new Vector3(1.22f, 0.82f, 1f);
            Vector3 stretched = new Vector3(0.88f, 1.18f, 1f);

            float elapsed = 0f;
            float dur1 = 0.08f;
            while (elapsed < dur1)
            {
                elapsed += Time.unscaledDeltaTime;
                if (bounceTarget != null) bounceTarget.localScale = Vector3.Lerp(originalScale, squashed, elapsed / dur1);
                yield return null;
            }

            elapsed = 0f;
            float dur2 = 0.09f;
            while (elapsed < dur2)
            {
                elapsed += Time.unscaledDeltaTime;
                if (bounceTarget != null) bounceTarget.localScale = Vector3.Lerp(squashed, stretched, elapsed / dur2);
                yield return null;
            }

            elapsed = 0f;
            float dur3 = 0.1f;
            while (elapsed < dur3)
            {
                elapsed += Time.unscaledDeltaTime;
                if (bounceTarget != null) bounceTarget.localScale = Vector3.Lerp(stretched, originalScale, elapsed / dur3);
                yield return null;
            }

            if (bounceTarget != null) bounceTarget.localScale = originalScale;
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
