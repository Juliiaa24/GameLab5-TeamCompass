/**
 * Author: Diego
 * Date: 30/09/26
 * Description: Task where the player must hover the cursor over a target for a required duration.
 */
using UnityEngine;

namespace ComputerLearning
{
    /// <summary>
    /// Spawns one HoverTarget and succeeds when the player keeps the cursor over it
    /// for the required hover duration.
    ///
    /// Configurable via TaskConfig:
    ///   targetSize            – diameter of the target
    ///   requiredHoverDuration – seconds of continuous hover needed
    ///   isMoving              – whether the target moves
    ///   moveSpeed             – speed of movement
    ///   timeLimit             – total task time limit; 0 = unlimited
    /// </summary>
    public class HoverTargetTask : BaseTask
    {
        #region Private Variables
        [Tooltip("The HoverTarget prefab to instantiate.")]
        [SerializeField] private HoverTarget targetPrefab;

        [Tooltip("The RectTransform the target is placed inside.")]
        [SerializeField] private RectTransform spawnArea;

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

            if (targetPrefab == null)
            {
                Debug.LogError("[HoverTargetTask] targetPrefab is not assigned!");
                return;
            }
            if (spawnArea == null)
            {
                Debug.LogError("[HoverTargetTask] spawnArea is not assigned!");
                return;
            }

            HoverTarget target = Instantiate(targetPrefab, spawnArea, false);
            target.SetParentRect(spawnArea);
            target.Configure(cfg.targetSize, cfg.requiredHoverDuration, cfg.isMoving, cfg.moveSpeed);

            // Random position within spawn area
            Rect  area     = spawnArea.rect;
            float halfSize = cfg.targetSize * 0.5f;

            float maxX = area.xMax - halfSize;
            float minX = area.xMin + halfSize;
            float maxY = area.yMax - halfSize;
            float minY = area.yMin + halfSize;

            float x = (maxX > minX) ? Random.Range(minX, maxX) : 0f;
            float y = (maxY > minY) ? Random.Range(minY, maxY) : 0f;

            target.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);
            target.OnHoverComplete += () => Complete(true);
            target.OnFailedAttempt += () => RecordIntermediateResult(false);
        }
        #endregion
    }
}
