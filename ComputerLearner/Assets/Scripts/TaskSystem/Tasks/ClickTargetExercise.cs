/**
 * Author: Diego
 * Date: 25/09/26
 * Description: Present targets and task progress without depending on a window.
*/
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ComputerLearning
{
    public class ClickTargetExercise : MonoBehaviour
    {
        #region Private Variables
        [SerializeField] private TargetTask task;
        [SerializeField] private TrainingLevel level;
        [SerializeField] private RectTransform playArea;
        [SerializeField] private ClickTarget target;
        [SerializeField] private TMP_Text progressLabel;
        [SerializeField] private TMP_Text resultLabel;
        [SerializeField] private Button restartButton;
        [SerializeField] private TMP_Text restartLabel;
        [SerializeField, Min(0f)] private float targetPadding = 12f;
        [SerializeField, Min(0f)] private float nextTargetDelay = 0.18f;
        private int shownProgress = -1;
        private int shownAttempt = -1;
        private SkillManager skillManager;
        private LevelManager levelManager;
        #endregion

        #region Unity Methods
        private void OnEnable()
        {
            if (task == null || target == null || level == null) return;
            task.ProgressChanged += OnProgress;
            target.Hit += OnHit;
            level.StateChanged += OnLevelChanged;
            if (restartButton != null) restartButton.onClick.AddListener(level.StartExercise);
            skillManager = SkillManager.Instance;
            levelManager = LevelManager.Instance;
            if (skillManager != null) skillManager.SkillProgressChanged += OnSkillChanged;
            if (levelManager != null) levelManager.LevelProgressChanged += OnLevelChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (task != null) task.ProgressChanged -= OnProgress;
            if (target != null) target.Hit -= OnHit;
            if (level != null) level.StateChanged -= OnLevelChanged;
            if (restartButton != null && level != null) restartButton.onClick.RemoveListener(level.StartExercise);
            if (skillManager != null) skillManager.SkillProgressChanged -= OnSkillChanged;
            if (levelManager != null) levelManager.LevelProgressChanged -= OnLevelChanged;
        }

        private void OnRectTransformDimensionsChange()
        {
            if (target != null && playArea != null) ClampTarget();
        }
        #endregion

        #region Private Methods
        private void OnHit() { task.RegisterHit(); }
        private void OnProgress(Task changed) { Refresh(); }
        private void OnLevelChanged(Level changed) { Refresh(); }
        private void OnSkillChanged(Skill changed) { Refresh(); }

        private void Refresh()
        {
            if (task == null || target == null || playArea == null) return;
            if (progressLabel != null)
                progressLabel.text = $"Targets: {task.CurrentProgress} / {task.RequiredProgress}";
            target.gameObject.SetActive(task.IsRunning);
            if (task.IsRunning && (shownProgress != task.CurrentProgress || shownAttempt != task.Attempt))
            {
                shownProgress = task.CurrentProgress;
                shownAttempt = task.Attempt;
                PlaceTarget();
                target.Prepare(task.CurrentProgress == 0 ? 0f : nextTargetDelay);
            }

            Skill clickSkill = skillManager != null ? skillManager.GetSkill(SkillID.CLICK) : null;
            string skillResult = clickSkill != null ? $"Click skill: {clickSkill.CompletedTasks} completed exercises" : "";
            if (resultLabel != null)
                resultLabel.text = level != null && level.IsCompleted()
                    ? $"Level complete!\n{skillResult}"
                    : task.IsRunning ? "Click each target with the left mouse button." : "Ready to practise?";
            if (restartLabel != null) restartLabel.text = task.IsCompleted() ? "Play again" : "Restart";
        }

        private void PlaceTarget()
        {
            RectTransform rect = (RectTransform)target.transform;
            Vector2 extent = GetExtent(rect);
            rect.anchoredPosition = new Vector2(Random.Range(-extent.x, extent.x), Random.Range(-extent.y, extent.y));
        }

        private Vector2 GetExtent(RectTransform rect)
        {
            Vector2 available = playArea.rect.size;
            // Keep the whole target usable even in a very small window.
            Vector2 targetSize = new Vector2(Mathf.Min(72f, Mathf.Max(1f, available.x - targetPadding * 2f)),
                Mathf.Min(72f, Mathf.Max(1f, available.y - targetPadding * 2f)));
            rect.sizeDelta = targetSize;
            return new Vector2(Mathf.Max(0f, (available.x - targetSize.x) * 0.5f - targetPadding),
                Mathf.Max(0f, (available.y - targetSize.y) * 0.5f - targetPadding));
        }

        private void ClampTarget()
        {
            RectTransform rect = (RectTransform)target.transform;
            Vector2 extent = GetExtent(rect);
            Vector2 point = rect.anchoredPosition;
            rect.anchoredPosition = new Vector2(Mathf.Clamp(point.x, -extent.x, extent.x),
                Mathf.Clamp(point.y, -extent.y, extent.y));
        }
        #endregion
    }
}
