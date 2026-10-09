/**
 * Author: Diego
 * Date: 30/09/26
 * Description: Runtime state of a single skill for one player.
 */
using System;
using UnityEngine;

namespace ComputerLearning
{
    /// <summary>
    /// Stores the current progress state of a single skill.
    /// This is the "live snapshot" — it changes every time a task is completed.
    /// Separate from TaskResult history so that the historical record
    /// can be read independently for reports/charts.
    /// </summary>
    [Serializable]
    public class SkillState
    {
        #region Public Variables
        public string skillId;

        [Range(0f, 100f)]
        public float score;

        public int tasksCompleted;
        public int successCount;
        public int failureCount;

        // DateTime is not directly serializable by Unity's JsonUtility,
        // so we store the raw ticks and expose a DateTime property.
        public long lastPracticedTicks;
        #endregion

        #region Public Properties
        /// <summary>
        /// Success rate as a 0–1 fraction.
        /// Returns 0 if no tasks have been completed yet.
        /// </summary>
        public float SuccessRate => tasksCompleted > 0 ? (float)successCount / tasksCompleted : 0f;

        /// <summary>The last time this skill was practiced as a UTC DateTime.</summary>
        public DateTime LastPracticed
        {
            get => lastPracticedTicks > 0 ? new DateTime(lastPracticedTicks, DateTimeKind.Utc) : DateTime.MinValue;
            set => lastPracticedTicks = value.Ticks;
        }
        #endregion

        #region Constructor
        /// <summary>Creates a new SkillState with an optional starting score.</summary>
        public SkillState(string id, float initialScore = 0f)
        {
            skillId = id;
            score = Mathf.Clamp(initialScore, 0f, 100f);
        }
        #endregion
    }
}
