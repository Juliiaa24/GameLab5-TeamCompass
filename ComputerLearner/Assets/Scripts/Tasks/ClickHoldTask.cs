using UnityEngine;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    public class ClickHoldTask : BaseTask, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public float requiredHoldTime = 1.5f;
        public UnityEngine.UI.Image fillImage;
        private bool isHolding = false;
        private float currentHoldTime = 0f;
        private bool isHovering = false;
        private float hoverDwellTime = 0f;
        private bool hasWarnedHoverStagnant = false;
        private Vector2 lastPointerScreenPos;
        private Camera lastEventCamera;
        private Color fillImageBaseColor = new Color(0.0f, 0.502f, 0.251f, 1f);
        private TaskTargetMovement targetMover;

        // Normalized cavity inside the black border of HoldTarget.png (740x740: X=[259..479], Y_bottom_up=[129..150])
        private static readonly Vector2 BAR_CAVITY_ANCHOR_MIN = new Vector2(259f / 740f, 129f / 740f);
        private static readonly Vector2 BAR_CAVITY_ANCHOR_MAX = new Vector2(479f / 740f, 150f / 740f);
        private static Sprite s_solidBarSprite;

        private static Sprite GetOrCreateSolidBarSprite()
        {
            if (s_solidBarSprite == null)
            {
                Texture2D whiteTex = Texture2D.whiteTexture;
                s_solidBarSprite = Sprite.Create(
                    whiteTex,
                    new Rect(0f, 0f, whiteTex.width, whiteTex.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }
            return s_solidBarSprite;
        }

        protected override void SetupTask(TaskDefinition definition)
        {
            UnityEngine.UI.Image targetImage = GetComponent<UnityEngine.UI.Image>();
            if (targetImage != null)
            {
                targetImage.color = Color.white;
            }

            CircleHitbox hitbox = GetComponent<CircleHitbox>();
            if (hitbox != null && hitbox.RadiusRatio >= 0.49f)
            {
                hitbox.SetRadiusRatio(188f / 740f);
            }

            targetMover = GetComponent<TaskTargetMovement>();
            if (targetMover == null) targetMover = gameObject.AddComponent<TaskTargetMovement>();

            TargetMovementType holdMoveType = TargetMovementType.Stationary;
            if (definition != null)
            {
                if (definition.difficulty <= 1)
                    holdMoveType = BaseTask.ResolveTargetMovementType(definition);
                else if (definition.difficulty <= 3)
                    holdMoveType = TargetMovementType.Circular;
                else
                    holdMoveType = TargetMovementType.DvdBounce;
            }
            targetMover.SetCircularRadius(18f, 12f);
            targetMover.Setup(definition, holdMoveType, defaultDvdSpeed: 42f);

            if (definition != null)
            {
                requiredHoldTime = 0.5f + (definition.difficulty * 0.2f); // Max 1.5s on difficulty 5
            }
            isHolding = false;
            currentHoldTime = 0f;
            isHovering = false;
            hoverDwellTime = 0f;
            hasWarnedHoverStagnant = false;
            ConfigureHoldBarVisuals();
            SetupBackgroundClickInterceptor(OnBackgroundClicked);
        }

        private void ConfigureHoldBarVisuals()
        {
            Sprite solidSprite = GetOrCreateSolidBarSprite();

            Transform bgTransform = transform.Find("BarBackground");
            UnityEngine.UI.Image bgImage;
            if (bgTransform == null)
            {
                GameObject bgObj = new GameObject("BarBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
                bgObj.transform.SetParent(transform, false);
                bgTransform = bgObj.transform;
                bgImage = bgObj.GetComponent<UnityEngine.UI.Image>();
            }
            else
            {
                bgImage = bgTransform.GetComponent<UnityEngine.UI.Image>();
            }

            RectTransform bgRect = bgTransform as RectTransform;
            if (bgRect != null)
            {
                bgRect.anchorMin = BAR_CAVITY_ANCHOR_MIN;
                bgRect.anchorMax = BAR_CAVITY_ANCHOR_MAX;
                bgRect.pivot = new Vector2(0.5f, 0.5f);
                bgRect.anchoredPosition = Vector2.zero;
                bgRect.sizeDelta = Vector2.zero;
                bgRect.SetSiblingIndex(0);
            }
            if (bgImage != null)
            {
                bgImage.sprite = solidSprite;
                bgImage.type = UnityEngine.UI.Image.Type.Simple;
                bgImage.color = Color.white;
                bgImage.raycastTarget = false;
            }

            if (fillImage != null)
            {
                RectTransform fillRect = fillImage.rectTransform;
                fillRect.anchorMin = BAR_CAVITY_ANCHOR_MIN;
                fillRect.anchorMax = BAR_CAVITY_ANCHOR_MAX;
                fillRect.pivot = new Vector2(0f, 0.5f);
                fillRect.anchoredPosition = Vector2.zero;
                fillRect.sizeDelta = Vector2.zero;
                fillRect.SetAsLastSibling();

                fillImage.sprite = solidSprite;
                fillImage.type = UnityEngine.UI.Image.Type.Filled;
                fillImage.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
                fillImage.fillOrigin = (int)UnityEngine.UI.Image.OriginHorizontal.Left;
                fillImage.fillAmount = 0f;
                fillImage.color = fillImageBaseColor;
                fillImage.raycastTarget = false;
            }
        }

        protected override bool IsPlayerActivelyProgressing()
        {
            return isHolding;
        }

        protected override void ShowTaskInactivityReminder()
        {
            if (!isHolding)
            {
                VirtualMascot.ShowContextualFeedback(VirtualMascot.FeedbackDialogs.holdInactivityReminder, transform as RectTransform, 3.5f);
            }
        }

        protected override float GetSubTaskParGraceSeconds()
        {
            return 7.5f;
        }

        protected override void Update()
        {
            base.Update();
            if (IsFinished) return;

            if (isHovering && !isHolding)
            {
                hoverDwellTime += Time.deltaTime;
                if (hoverDwellTime >= 2.5f && !hasWarnedHoverStagnant)
                {
                    hasWarnedHoverStagnant = true;
                    VirtualMascot.ShowContextualFeedback(VirtualMascot.FeedbackDialogs.holdHoverStagnant, transform as RectTransform, 3.2f);
                }
            }

            if (isHolding)
            {
                if (UnityEngine.InputSystem.Mouse.current != null)
                {
                    lastPointerScreenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                }

                currentHoldTime += Time.deltaTime;
                NotifyTaskProgress();

                float progress = Mathf.Clamp01(currentHoldTime / Mathf.Max(0.1f, requiredHoldTime));
                if (fillImage != null)
                {
                    fillImage.fillAmount = progress;
                    fillImage.color = Color.Lerp(fillImageBaseColor, new Color(0.16f, 0.80f, 0.38f, 1f), progress);
                }
                NotifySubTaskProgress(progress);

                if (currentHoldTime >= requiredHoldTime)
                {
                    isHolding = false;
                    RectTransform rt = transform as RectTransform;
                    TargetMovementType moveType = targetMover != null ? targetMover.MovementType : BaseTask.ResolveTargetMovementType(Definition);
                    int scorePoints = BaseTask.CalculatePrecisionAndSizeScore(rt, lastPointerScreenPos, lastEventCamera, out bool isOuterRing, moveType);
                    int maxScorePoints = BaseTask.CalculateMaxPossiblePrecisionScore(rt, moveType);
                    if (isOuterRing && rt != null)
                    {
                        VirtualMascot.ShowAlmostOutFeedback(rt);
                    }
                    Debug.Log("[ClickHoldTask] Hold complete!");
                    Complete(true, transform, scorePoints, maxScorePoints);
                }
            }
            else
            {
                if (fillImage != null && fillImage.fillAmount > 0f)
                {
                    fillImage.fillAmount = 0f;
                    fillImage.color = fillImageBaseColor;
                }
            }
        }

        public void OnBackgroundClicked(PointerEventData eventData)
        {
            if (IsFinished) return;
            RegisterAttempt(true);

            if (eventData != null && eventData.button == PointerEventData.InputButton.Right)
            {
                VirtualMascot.ShowWrongClickFeedback(transform as RectTransform, false);
            }
            else
            {
                VirtualMascot.ShowContextualFeedback(VirtualMascot.FeedbackDialogs.holdMissedBackground, transform as RectTransform, 3.0f);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (IsFinished) return;
            isHovering = true;
            hoverDwellTime = 0f;
            hasWarnedHoverStagnant = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (IsFinished) return;

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                RegisterAttempt(true);
                VirtualMascot.ShowWrongClickFeedback(transform as RectTransform, false);
                return;
            }

            if (eventData.button != PointerEventData.InputButton.Left) return;

            isHolding = true;
            lastPointerScreenPos = eventData.position;
            lastEventCamera = eventData.pressEventCamera;
            currentHoldTime = 0f;
            hoverDwellTime = 0f;
            hasWarnedHoverStagnant = true;
            NotifyUserInteraction();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (IsFinished || eventData.button != PointerEventData.InputButton.Left) return;

            if (isHolding)
            {
                isHolding = false;
                float progress = currentHoldTime / Mathf.Max(0.1f, requiredHoldTime);
                if (currentHoldTime < requiredHoldTime)
                {
                    RegisterAttempt();
                    var fb = VirtualMascot.FeedbackDialogs;
                    string msg;
                    if (currentHoldTime < 0.25f)
                    {
                        msg = MascotFeedbackDialogs.PickRandom(fb.holdQuickTapMessages, fb.holdKeepPressing);
                    }
                    else if (progress >= 0.70f)
                    {
                        msg = MascotFeedbackDialogs.PickRandom(fb.holdAlmostDoneMessages, fb.holdKeepPressing);
                    }
                    else
                    {
                        msg = MascotFeedbackDialogs.PickRandom(fb.holdMidReleaseMessages, fb.holdKeepPressing);
                    }
                    VirtualMascot.ShowContextualFeedback(msg, transform as RectTransform, 3.0f);
                }
                currentHoldTime = 0f;
                NotifySubTaskProgress(0f);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
            hoverDwellTime = 0f;

            // If mouse leaves the target while holding, cancel it
            if (isHolding)
            {
                isHolding = false;
                currentHoldTime = 0f;
                NotifySubTaskProgress(0f);
                RegisterAttempt();
                var fb = VirtualMascot.FeedbackDialogs;
                string msg = MascotFeedbackDialogs.PickRandom(fb.holdSlippedOutsideMessages, fb.holdStayInside);
                VirtualMascot.ShowContextualFeedback(msg, transform as RectTransform, 3.0f);
            }
        }
    }
}
