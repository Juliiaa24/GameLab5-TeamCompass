using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ComputerLearning
{
    public class ClickTargetTask : BaseTask, IPointerClickHandler
    {
        #region Private Variables
        [SerializeField] private ClickTarget targetPrefab;
        [SerializeField] private RectTransform spawnArea;

        private readonly List<ClickTarget> activeTargets = new List<ClickTarget>();
        private int targetsRemaining;
        private int totalTargetsInitial = 1;

        private float timeLimit;
        private float elapsed;
        private bool  timeLimitActive;
        private float lastTargetHitTime = -10f;
        private float lastActionTime;
        private bool hasShownDelayReminder;
        #endregion

        #region Unity Methods
        protected override void Start()
        {
            base.Start();
            lastActionTime = Time.time;
            hasShownDelayReminder = false;
            Image img = GetComponent<Image>();
            if (img == null) img = gameObject.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0);
            img.raycastTarget = true;
        }

        protected override void Update()
        {
            base.Update();
            if (IsFinished) return;

            // Delayed click reminder (> 5s before first click or between targets)
            if (!hasShownDelayReminder && (Time.time - lastActionTime) >= 5.0f && targetsRemaining > 0)
            {
                hasShownDelayReminder = true;
                RectTransform anchor = activeTargets.Count > 0 && activeTargets[0] != null
                    ? activeTargets[0].GetComponent<RectTransform>()
                    : (spawnArea != null ? spawnArea : GetComponent<RectTransform>());
                VirtualMascot.ShowContextualFeedback(VirtualMascot.FeedbackDialogs.clickDelayReminder, anchor, 2.8f);
            }

            if (!timeLimitActive) return;
            elapsed += Time.deltaTime;
            if (elapsed >= timeLimit) Complete(false);
        }
        #endregion

        #region Protected Methods
        protected override void SetupTask(TaskDefinition definition)
        {
            TaskConfig cfg = definition.config;

            timeLimit        = cfg.timeLimit;
            timeLimitActive  = timeLimit > 0f;
            elapsed          = 0f;
            lastTargetHitTime = -10f;
            lastActionTime   = Time.time;
            hasShownDelayReminder = false;
            targetsRemaining = Mathf.Max(1, cfg.targetCount);
            totalTargetsInitial = targetsRemaining;

            activeTargets.Clear();
            for (int i = 0; i < targetsRemaining; i++)
                SpawnTarget(cfg);
        }
        #endregion

        #region Private Methods
        public void OnPointerClick(PointerEventData eventData)
        {
            if (IsFinished) return;

            // Ignore rapid follow-up clicks or accidental double-clicks right after hitting a target
            if (eventData != null && eventData.button == PointerEventData.InputButton.Left)
            {
                if ((Time.unscaledTime - lastTargetHitTime) < 0.35f || eventData.clickCount >= 2)
                {
                    NotifyUserInteraction();
                    return;
                }
            }

            RecordIntermediateResult(false);

            RectTransform feedbackAnchor = spawnArea != null ? spawnArea : GetComponent<RectTransform>();
            if (eventData != null && eventData.button == PointerEventData.InputButton.Right)
            {
                VirtualMascot.ShowWrongClickFeedback(feedbackAnchor, false);
            }
            else
            {
                VirtualMascot.ShowContextualFeedback(VirtualMascot.FeedbackDialogs.clickMissed, feedbackAnchor);
            }
        }

        private void SpawnTarget(TaskConfig cfg)
        {
            if (targetPrefab == null || spawnArea == null) return;

            ClickTarget target = Instantiate(targetPrefab, spawnArea, false);
            target.SetParentRect(spawnArea);
            target.Configure(cfg.targetSize, cfg.isMoving, cfg.moveSpeed);

            Rect  area     = spawnArea.rect;
            float halfSize = cfg.targetSize * 0.5f;

            float maxX = area.xMax - halfSize;
            float minX = area.xMin + halfSize;
            float maxY = area.yMax - halfSize;
            float minY = area.yMin + halfSize;

            float x = (maxX > minX) ? Random.Range(minX, maxX) : 0f;
            float y = (maxY > minY) ? Random.Range(minY, maxY) : 0f;

            RectTransform targetRect = target.GetComponent<RectTransform>();
            targetRect.anchoredPosition = new Vector2(x, y);

            target.OnClicked += () => OnTargetClicked(target);
            target.OnWrongButtonClicked += () =>
            {
                if (IsFinished) return;
                RecordIntermediateResult(false);
                VirtualMascot.ShowWrongClickFeedback(targetRect, false);
            };
            target.OnHoverStagnant += () =>
            {
                if (IsFinished) return;
                RegisterAttempt(false);
                VirtualMascot.ShowContextualFeedback(VirtualMascot.FeedbackDialogs.clickHoverStagnant, targetRect, 2.5f);
            };
            activeTargets.Add(target);
        }

        private void OnTargetClicked(ClickTarget target)
        {
            if (IsFinished) return;

            lastTargetHitTime = Time.unscaledTime;
            lastActionTime = Time.time;
            hasShownDelayReminder = false;

            activeTargets.Remove(target);
            if (target != null) Destroy(target.gameObject);
            targetsRemaining--;
            NotifySubTaskProgress((float)(totalTargetsInitial - targetsRemaining) / Mathf.Max(1, totalTargetsInitial));

            if (targetsRemaining <= 0)
            {
                Complete(true);
            }
            else
            {
                RecordIntermediateResult(true);
            }
        }
        #endregion
    }
}
