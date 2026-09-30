/**
 * Author: Diego
 * Date: 30/09/26
 * Description: Task where the player clicks one or more targets to succeed.
 */
using System.Collections.Generic;
using UnityEngine;

namespace ComputerLearning
{
    /// <summary>
    /// Spawns one or more ClickTarget elements and succeeds when the player clicks all of them.
    ///
    /// Configurable via TaskConfig:
    ///   targetCount   – how many targets appear simultaneously
    ///   targetSize    – diameter of each target in UI units
    ///   isMoving      – whether targets bounce around the area
    ///   moveSpeed     – speed of movement when isMoving is true
    ///   timeLimit     – seconds before the task fails; 0 = unlimited
    /// </summary>
    public class ClickTargetTask : BaseTask
    {
        #region Private Variables
        [Tooltip("The ClickTarget prefab to instantiate for each target.")]
        [SerializeField] private ClickTarget targetPrefab;

        [Tooltip("The RectTransform targets are placed inside. Usually assigned in the prefab.")]
        [SerializeField] private RectTransform spawnArea;

        private readonly List<ClickTarget> activeTargets = new List<ClickTarget>();
        private int targetsRemaining;

        private float timeLimit;
        private float elapsed;
        private bool  timeLimitActive;
        #endregion

        #region Unity Methods
        private void Update()
        {
            if (IsFinished || !timeLimitActive) return;
            elapsed += Time.deltaTime;
            if (elapsed >= timeLimit) Complete(false); // Time ran out → failure
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
        private void SpawnTarget(TaskConfig cfg)
        {
            if (targetPrefab == null)
            {
                Debug.LogError("[ClickTargetTask] targetPrefab is not assigned!");
                return;
            }
            if (spawnArea == null)
            {
                Debug.LogError("[ClickTargetTask] spawnArea is not assigned!");
                return;
            }

            ClickTarget target = Instantiate(targetPrefab, spawnArea, false);
            target.SetParentRect(spawnArea);
            target.Configure(cfg.targetSize, cfg.isMoving, cfg.moveSpeed);

            // Place at a random position within the spawn area, keeping the target fully visible
            Rect  area     = spawnArea.rect;
            float halfSize = cfg.targetSize * 0.5f;

            float maxX = area.xMax - halfSize;
            float minX = area.xMin + halfSize;
            float maxY = area.yMax - halfSize;
            float minY = area.yMin + halfSize;

            // Guard against an area that is smaller than the target
            float x = (maxX > minX) ? Random.Range(minX, maxX) : 0f;
            float y = (maxY > minY) ? Random.Range(minY, maxY) : 0f;

            target.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);
            target.OnClicked += () => OnTargetClicked(target);
            activeTargets.Add(target);
        }

        private void OnTargetClicked(ClickTarget target)
        {
            if (IsFinished) return;

            activeTargets.Remove(target);
            Destroy(target.gameObject);
            targetsRemaining--;

            if (targetsRemaining <= 0) Complete(true);
        }
        #endregion
    }
}
