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
        public string levelCompletedGoldMessage = "Fantastic job!";
        public string levelCompletedMedalMessage = "Well done! Replay anytime to beat your medal!";
        public string levelCompletedNoMedalMessage = "Level completed! Keep practicing to earn a medal!";

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
        private bool hasAssignedTarget;
        private bool isPinnedAtWorldPos;
        private Vector3 pinnedWorldPos;
        public bool IsPinnedAtWorldPos => isPinnedAtWorldPos;
        private Vector2 currentTargetOffset = Vector2.zero;
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
                bool isCelebration = targetRect != null && targetRect.GetComponentInParent<LevelCelebrationModal>() != null;
                float maxBubbleTextWidth = isCelebration ? 240f : 380f;
                if (rawWidth > maxBubbleTextWidth)
                {
                    layoutElement.preferredWidth = maxBubbleTextWidth;
                }

                // Force the layout to rebuild immediately so it resizes vertically before the next frame
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(speechText.GetComponent<RectTransform>());
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(speechBubble.GetComponent<RectTransform>());
            }
        }

        private void Update()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            float scale = canvas != null ? canvas.scaleFactor : 1f;

            // Early dismissal of speech bubble via Space or clicking on the bubble (only during sequential tutorial dialogues)
            if (AllowSpaceToSkipDialogue && speechBubble != null && speechBubble.activeSelf)
            {
                bool spacePressed = UnityEngine.InputSystem.Keyboard.current != null &&
                                    UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame;
                bool bubbleClicked = false;
                if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                {
                    RectTransform bubbleRt = speechBubble.GetComponent<RectTransform>();
                    Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                    if (bubbleRt != null && RectTransformUtility.RectangleContainsScreenPoint(bubbleRt, UnityEngine.InputSystem.Mouse.current.position.ReadValue(), cam))
                    {
                        bubbleClicked = true;
                    }
                }

                if (spacePressed || bubbleClicked)
                {
                    DismissActiveFeedback();
                }
            }

            if (isDesktopCompanion)
            {
                UpdateCompanionPosition();
                return;
            }

            if (hasAssignedTarget && (targetRect == null || !targetRect.gameObject.activeInHierarchy))
            {
                // If target was destroyed or hidden, hide mascot
                Hide();
                return;
            }

            if ((targetRect != null && targetRect.gameObject.activeInHierarchy) || isPinnedAtWorldPos)
            {
                Vector3 basePos;
                if (isPinnedAtWorldPos)
                {
                    basePos = pinnedWorldPos;
                }
                else
                {
                    // Get the true visual center of the target, regardless of where its pivot is set
                    Vector3[] corners = new Vector3[4];
                    targetRect.GetWorldCorners(corners);
                    basePos = (corners[0] + corners[2]) / 2f;
                }

                // Float animation isolated to mascotVisualRect so speechBubble does not bob
                floatTimer += Time.deltaTime;
                float floatOffset = Mathf.Sin(floatTimer * 4f) * 5f;

                RectTransform vis = GetMascotVisualRect();
                if (vis != null) vis.anchoredPosition = new Vector2(0f, floatOffset);
                Vector3 finalTargetPos = basePos + CanvasOffsetToWorldDelta(currentTargetOffset, canvas);

                bool isCelebrationTarget = targetRect != null && targetRect.GetComponentInParent<LevelCelebrationModal>() != null;
                float predictedFlipScale = ShouldFaceLeft(finalTargetPos, canvas, isCelebrationTarget) ? -1f : 1f;
                finalTargetPos = ClampMascotWorldPositionToScreen(finalTargetPos, canvas, predictedFlipScale);

                mascotRect.position = Vector3.Lerp(mascotRect.position, finalTargetPos, Time.deltaTime * followSpeed);
            }

            UpdateSpeechBubblePlacement(canvas);
        }

        public static int DialogueSkipVersion { get; private set; }
        public static bool AllowSpaceToSkipDialogue { get; private set; }

        public static IEnumerator WaitForDialogue(float duration)
        {
            AllowSpaceToSkipDialogue = true;
            try
            {
                int initialVersion = DialogueSkipVersion;
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    if (DialogueSkipVersion != initialVersion)
                    {
                        yield return null;
                        yield break;
                    }
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }
            finally
            {
                AllowSpaceToSkipDialogue = false;
            }
        }

        public void DismissActiveFeedback()
        {
            DialogueSkipVersion++;
            bool wasTemporary = hideBubbleCoroutine != null;
            if (hideBubbleCoroutine != null)
            {
                StopCoroutine(hideBubbleCoroutine);
                hideBubbleCoroutine = null;
            }
            if (speechBubble != null) speechBubble.SetActive(false);
            if (!isDesktopCompanion && wasTemporary)
            {
                Hide();
            }
        }

        private float GetPixelsPerCanvasUnit(Canvas canvas)
        {
            if (canvas == null) return 1f;
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return Mathf.Max(0.001f, canvas.scaleFactor);
            }

            Camera cam = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            Vector3 originWorld = canvas.transform.position;
            Vector3 oneCanvasUnitWorld = originWorld + canvas.transform.TransformVector(Vector3.right);
            Vector2 p0 = RectTransformUtility.WorldToScreenPoint(cam, originWorld);
            Vector2 p1 = RectTransformUtility.WorldToScreenPoint(cam, oneCanvasUnitWorld);
            float dist = Vector2.Distance(p0, p1);
            return dist > 0.001f ? dist : Mathf.Max(0.001f, canvas.scaleFactor);
        }

        private Vector3 CanvasOffsetToWorldDelta(Vector2 canvasOffset, Canvas canvas)
        {
            if (canvasOffset == Vector2.zero) return Vector3.zero;
            if (canvas != null)
            {
                // TransformVector converts Canvas-local units directly into world space
                // for both ScreenSpaceOverlay (pixels) and ScreenSpaceCamera (world units).
                return canvas.transform.TransformVector(new Vector3(canvasOffset.x, canvasOffset.y, 0f));
            }
            return new Vector3(canvasOffset.x, canvasOffset.y, 0f);
        }

        private bool ShouldFaceLeft(Vector3 mascotWorldPos, Canvas canvas, bool isCelebrationTarget)
        {
            if (isCelebrationTarget)
            {
                // On celebration cards, open the speech bubble outward away from the card.
                return currentTargetOffset.x < 0f;
            }

            if (!autoFlipMascot) return false;

            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector3 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, mascotWorldPos);
            float centerX = cam != null ? cam.pixelRect.center.x : Screen.width * 0.5f;
            return screenPoint.x > centerX;
        }

        private Vector3 ClampMascotWorldPositionToScreen(Vector3 worldPos, Canvas canvas, float flipScale)
        {
            float pxPerUnit = GetPixelsPerCanvasUnit(canvas);
            float margin = 12f * pxPerUnit;
            float rectWidth = mascotRect != null && mascotRect.rect.width > 1f ? mascotRect.rect.width : 100f;
            float rectHeight = mascotRect != null && mascotRect.rect.height > 1f ? mascotRect.rect.height : 100f;
            Vector2 pivot = mascotRect != null ? mascotRect.pivot : new Vector2(0.125f, 1.13f);

            // Account for pivot + horizontal flip so the full mascot sprite remains on-screen
            float leftExtent = (flipScale >= 0f ? pivot.x : (1f - pivot.x)) * rectWidth * pxPerUnit;
            float rightExtent = (flipScale >= 0f ? (1f - pivot.x) : pivot.x) * rectWidth * pxPerUnit;
            float bottomExtent = (pivot.y * rectHeight + 15f) * pxPerUnit;
            float topExtent = ((1f - pivot.y) * rectHeight + 15f) * pxPerUnit;

            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            float minScreenX = cam != null ? cam.pixelRect.xMin : 0f;
            float maxScreenX = cam != null ? cam.pixelRect.xMax : Screen.width;
            float minScreenY = cam != null ? cam.pixelRect.yMin : 0f;
            float maxScreenY = cam != null ? cam.pixelRect.yMax : Screen.height;

            float clampMinX = minScreenX + margin + leftExtent;
            float clampMaxX = maxScreenX - margin - rightExtent;
            if (clampMinX > clampMaxX)
            {
                float midX = (minScreenX + maxScreenX) * 0.5f;
                clampMinX = midX;
                clampMaxX = midX;
            }

            float clampMinY = minScreenY + margin + Mathf.Max(0f, bottomExtent);
            float clampMaxY = maxScreenY - margin - Mathf.Max(0f, topExtent);
            if (clampMinY > clampMaxY)
            {
                float midY = (minScreenY + maxScreenY) * 0.5f;
                clampMinY = midY;
                clampMaxY = midY;
            }

            if (cam != null)
            {
                Vector3 sp = cam.WorldToScreenPoint(worldPos);
                sp.x = Mathf.Clamp(sp.x, clampMinX, clampMaxX);
                sp.y = Mathf.Clamp(sp.y, clampMinY, clampMaxY);
                if (RectTransformUtility.ScreenPointToWorldPointInRectangle(canvas.transform as RectTransform, sp, cam, out Vector3 worldPt))
                {
                    return worldPt;
                }
                return worldPos;
            }

            worldPos.x = Mathf.Clamp(worldPos.x, clampMinX, clampMaxX);
            worldPos.y = Mathf.Clamp(worldPos.y, clampMinY, clampMaxY);
            return worldPos;
        }

        private Vector2 GetCelebrationOffsetForTarget(RectTransform cardRect)
        {
            const float fallbackHalfWidth = 240f;
            const float sideGap = 55f;
            const float verticalOffset = 30f;

            if (cardRect == null) return new Vector2(fallbackHalfWidth + sideGap, verticalOffset);

            Canvas.ForceUpdateCanvases();
            Canvas canvas = GetComponentInParent<Canvas>();
            float pxPerUnit = GetPixelsPerCanvasUnit(canvas);
            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

            Vector3[] corners = new Vector3[4];
            cardRect.GetWorldCorners(corners);
            Vector3 bottomLeft = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
            Vector3 topRight = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);

            float minX = cam != null ? cam.pixelRect.xMin : 0f;
            float maxX = cam != null ? cam.pixelRect.xMax : Screen.width;
            float cardHalfWidth = cardRect.rect.width > 1f
                ? cardRect.rect.width * 0.5f
                : ((topRight.x - bottomLeft.x) > 1f ? ((topRight.x - bottomLeft.x) / pxPerUnit) * 0.5f : fallbackHalfWidth);

            float spaceRight = maxX - topRight.x;
            float spaceLeft = bottomLeft.x - minX;
            bool useLeftSide = spaceRight < 440f * pxPerUnit && spaceLeft > spaceRight;

            float xOffset = (cardHalfWidth + sideGap) * (useLeftSide ? -1f : 1f);
            return new Vector2(xOffset, verticalOffset);
        }

        private void UpdateSpeechBubblePlacement(Canvas canvas)
        {
            if (mascotRect == null) return;

            bool isCelebrationTarget = targetRect != null && targetRect.GetComponentInParent<LevelCelebrationModal>() != null;
            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            bool isOnRightSide = ShouldFaceLeft(mascotRect.position, canvas, isCelebrationTarget);

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

                Vector3 bottomLeft = RectTransformUtility.WorldToScreenPoint(cam, bubbleCorners[0]);
                Vector3 topRight = RectTransformUtility.WorldToScreenPoint(cam, bubbleCorners[2]);

                float pxPerUnit = GetPixelsPerCanvasUnit(canvas);
                float marginX = 14f * pxPerUnit;
                float marginY = 14f * pxPerUnit;

                float minX = cam != null ? cam.pixelRect.xMin : 0f;
                float maxX = cam != null ? cam.pixelRect.xMax : Screen.width;
                float minY = cam != null ? cam.pixelRect.yMin : 0f;
                float maxY = cam != null ? cam.pixelRect.yMax : Screen.height;

                // Account for mascotRect.localScale.x (flipScale) so horizontal adjustment moves toward screen interior on both sides
                float denomX = pxPerUnit * flipScale;
                if (bottomLeft.x < minX + marginX)
                {
                    float shiftScreenX = (minX + marginX) - bottomLeft.x;
                    finalBubblePos.x += shiftScreenX / denomX;
                }
                else if (topRight.x > maxX - marginX)
                {
                    float shiftScreenX = (maxX - marginX) - topRight.x;
                    finalBubblePos.x += shiftScreenX / denomX;
                }

                if (bottomLeft.y < minY + marginY)
                {
                    float difference = ((minY + marginY) - bottomLeft.y) / pxPerUnit;
                    finalBubblePos.y += difference;
                }
                else if (topRight.y > maxY - marginY)
                {
                    float difference = (topRight.y - (maxY - marginY)) / pxPerUnit;
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
        public static void ShowContextualFeedback(string message, RectTransform target = null, float duration = -1f, Vector2? offset = null)
        {
            if (Instance == null)
            {
                Instance = Object.FindFirstObjectByType<VirtualMascot>(FindObjectsInactive.Include);
            }
            if (Instance != null)
            {
                bool isCelebration = target != null && target.GetComponentInParent<LevelCelebrationModal>() != null;
                float finalDuration = isCelebration ? (duration > 0f ? duration : 5.5f) : (duration > 0f ? Mathf.Min(duration, 2.0f) : 1.8f);
                Instance.ShowTemporaryFeedback(message, target, finalDuration, offset);
            }
        }

        public static void ShowWrongClickFeedback(RectTransform target, bool isRightClickExpected)
        {
            MascotFeedbackDialogs fb = FeedbackDialogs;
            string msg = isRightClickExpected ? fb.wrongLeftClick : fb.wrongRightClick;
            ShowContextualFeedback(msg, target, -1f);
        }

        public static void ShowAlmostOutFeedback(RectTransform target)
        {
            Vector3? worldCenter = null;
            if (target != null)
            {
                Vector3[] corners = new Vector3[4];
                target.GetWorldCorners(corners);
                worldCenter = (corners[0] + corners[2]) / 2f;
            }
            ShowContextualFeedback(FeedbackDialogs.almostOut, target, 1.8f);
            if (Instance != null && worldCenter.HasValue)
            {
                Instance.PinAtWorldPosition(worldCenter.Value);
            }
        }

        public void PinAtWorldPosition(Vector3 worldPos)
        {
            hasAssignedTarget = false;
            isPinnedAtWorldPos = false;
            targetRect = null;
            isPinnedAtWorldPos = true;
            pinnedWorldPos = worldPos;
            if (mascotRect != null)
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                Vector3 desiredPos = worldPos + CanvasOffsetToWorldDelta(currentTargetOffset, canvas);
                float flipScale = ShouldFaceLeft(desiredPos, canvas, false) ? -1f : 1f;
                mascotRect.position = ClampMascotWorldPositionToScreen(desiredPos, canvas, flipScale);
            }
        }

        public static void ShowSingleClickFeedback(RectTransform target)
        {
            ShowContextualFeedback(FeedbackDialogs.singleClick, target, -1f);
        }

        public static void ShowInactivityReminder(RectTransform target = null)
        {
            string[] inactivityPool = FeedbackDialogs.inactivityMessages;
            if (inactivityPool == null || inactivityPool.Length == 0) return;
            string msg = inactivityPool[UnityEngine.Random.Range(0, inactivityPool.Length)];
            ShowContextualFeedback(msg, target, -1f);
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
            if (Instance == null)
            {
                Instance = Object.FindFirstObjectByType<VirtualMascot>(FindObjectsInactive.Include);
            }

            MascotFeedbackDialogs fb = FeedbackDialogs;
            string msg;
            bool isUpgrade = previousMedal != LevelMedal.None && (int)newMedal > (int)previousMedal;
            if (isUpgrade)
            {
                string header = GetRandomMedalImprovementMessage();
                string prevName = LevelScoreRecord.GetMedalName(previousMedal);
                string newName = LevelScoreRecord.GetMedalName(newMedal);
                string fmt = !string.IsNullOrEmpty(fb.medalImprovementUpgradeFormat)
                    ? fb.medalImprovementUpgradeFormat
                    : "{0}\n({1} -> {2})";
                msg = string.Format(fmt, header, prevName, newName, accuracy);
            }
            else if (newMedal != LevelMedal.None)
            {
                string header = newMedal == LevelMedal.Gold
                    ? (!string.IsNullOrEmpty(fb.levelCompletedGoldMessage) ? fb.levelCompletedGoldMessage : "Fantastic job!")
                    : (!string.IsNullOrEmpty(fb.levelCompletedMedalMessage) ? fb.levelCompletedMedalMessage : "Well done! Replay anytime to beat your medal!");
                string newName = LevelScoreRecord.GetMedalName(newMedal);
                msg = !string.IsNullOrEmpty(fb.medalImprovementEarnedFormat)
                    ? string.Format(fb.medalImprovementEarnedFormat, header, newName, accuracy)
                    : $"{header}\n({newName})";
            }
            else
            {
                msg = !string.IsNullOrEmpty(fb.levelCompletedNoMedalMessage)
                    ? fb.levelCompletedNoMedalMessage
                    : "Level completed! Keep practicing to earn a medal!";
            }

            LevelCelebrationModal modal = Object.FindFirstObjectByType<LevelCelebrationModal>();
            if (modal != null && modal.CardRect != null && Instance != null)
            {
                RectTransform cardTarget = modal.CardRect;
                Vector2 sideOffset = Instance.GetCelebrationOffsetForTarget(cardTarget);
                // Keep level-end feedback visible alongside the modal until the modal closes (Update hides when target deactivates)
                Instance.ShowMessage(msg, cardTarget, sideOffset);
                if (Instance.bounceCoroutine != null) Instance.StopCoroutine(Instance.bounceCoroutine);
                Instance.bounceCoroutine = Instance.StartCoroutine(Instance.BounceAnimation());
                return;
            }

            ShowContextualFeedback(msg, target, displayDuration, new Vector2(310f, -20f));
        }

        public void ShowTemporaryFeedback(string message, RectTransform target, float duration, Vector2? offset = null)
        {
            ShowMessage(message, target, offset);
            if (bounceCoroutine != null) StopCoroutine(bounceCoroutine);
            bounceCoroutine = StartCoroutine(BounceAnimation());

            if (hideBubbleCoroutine != null) StopCoroutine(hideBubbleCoroutine);
            hideBubbleCoroutine = StartCoroutine(AutoHideFeedback(duration));
        }

        private IEnumerator AutoHideFeedback(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
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
            hasAssignedTarget = (target != null);
            isPinnedAtWorldPos = false;
            bool isCelebrationTarget = target != null && target.GetComponentInParent<LevelCelebrationModal>() != null;
            currentTargetOffset = isCelebrationTarget ? (offset ?? GetCelebrationOffsetForTarget(target)) : Vector2.zero;

            Canvas canvas = GetComponentInParent<Canvas>();
            float scale = canvas != null ? canvas.scaleFactor : 1f;
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
                else if ((!wasActive || isCelebrationTarget) && targetRect.gameObject.activeInHierarchy)
                {
                    Canvas.ForceUpdateCanvases();
                    Vector3[] corners = new Vector3[4];
                    targetRect.GetWorldCorners(corners);
                    Vector3 desiredPos = ((corners[0] + corners[2]) / 2f) + CanvasOffsetToWorldDelta(currentTargetOffset, canvas);
                    float flipScale = ShouldFaceLeft(desiredPos, canvas, isCelebrationTarget) ? -1f : 1f;
                    mascotRect.position = ClampMascotWorldPositionToScreen(desiredPos, canvas, flipScale);
                }
            }

            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
            cg.interactable = false;
            cg.blocksRaycasts = false;

            ApplyTextAndRebuildBubble(message);

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
            AllowSpaceToSkipDialogue = false;
            hasAssignedTarget = false;
            isPinnedAtWorldPos = false;
            targetRect = null;
            currentTargetOffset = Vector2.zero;
            gameObject.SetActive(false);
        }

        #region Desktop Companion Implementation
        /// <summary>
        /// Initializes the mascot as a persistent, interactive desktop pet in the top-right corner.
        /// </summary>
        public void InitAsDesktopCompanion()
        {
            isDesktopCompanion = true;
            hasAssignedTarget = false;
            isPinnedAtWorldPos = false;
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

            int wordCount = !string.IsNullOrEmpty(message) ? message.Split(new char[] { ' ', '\n' }, System.StringSplitOptions.RemoveEmptyEntries).Length : 0;
            float dynamicDuration = Mathf.Clamp(4.5f + (wordCount * 0.45f), 8.5f, 10.5f);
            hideBubbleCoroutine = StartCoroutine(AutoHideBubble(dynamicDuration));
        }

        private IEnumerator AutoHideBubble(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
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
