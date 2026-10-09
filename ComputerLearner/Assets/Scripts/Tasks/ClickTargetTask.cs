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

        private float timeLimit;
        private float elapsed;
        private bool  timeLimitActive;
        #endregion

        #region Unity Methods
        protected override void Start()
        {
            base.Start();
            Image img = GetComponent<Image>();
            if (img == null) img = gameObject.AddComponent<Image>();
            img.color = new Color(0,0,0,0);
            img.raycastTarget = true;
        }

        private void Update()
        {
            if (IsFinished || !timeLimitActive) return;
            elapsed += Time.deltaTime;
            if (elapsed >= timeLimit) Complete(false);
        }
        #endregion

        #region Protected Methods
        protected override void SetupTask(TaskDefinition definition)
        {
            TaskConfig cfg = definition.config;

            timeLimit       = cfg.timeLimit;
            timeLimitActive = timeLimit > 0f;
            elapsed         = 0f;
            targetsRemaining = Mathf.Max(1, cfg.targetCount);

            activeTargets.Clear();
            for (int i = 0; i < targetsRemaining; i++)
                SpawnTarget(cfg);
        }
        #endregion

        #region Private Methods
        public void OnPointerClick(PointerEventData eventData)
        {
            if (IsFinished) return;
            RecordIntermediateResult(false);
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

            target.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);
            target.OnClicked += () => OnTargetClicked(target);
            activeTargets.Add(target);
        }

        private void OnTargetClicked(ClickTarget target)
        {
            if (IsFinished) return;

            RecordIntermediateResult(true);

            activeTargets.Remove(target);
            Destroy(target.gameObject);
            targetsRemaining--;

            if (targetsRemaining <= 0) 
            {
                FinishWithoutResult();
            }
        }
        #endregion
    }
}
