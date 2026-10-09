/**
 * Author: Diego
 * Date: 30/09/26
 * Description: Immutable record of a single completed task attempt.
 */
using System;
using UnityEngine;

namespace ComputerLearning
{
    /// <summary>
    /// A complete record of one task attempt.
    /// Created by BaseTask when the task finishes (success or failure).
    /// Stored in ProgressData for historical reporting and future progress charts.
    /// </summary>
    [Serializable]
    public class TaskResult
    {
        #region Public Variables
        public string taskId;
        public string skillId;
        public string levelId;
        public string levelName;
        public int difficulty;
        public bool success;
        public int attempts;
        public float completionTime;  // seconds
        public float scoreChange;     // positive = gain, negative = loss
        public long timestampTicks;   // DateTime.UtcNow.Ticks at creation
        #endregion

        #region Public Properties
        /// <summary>The UTC timestamp when this result was created.</summary>
        public DateTime Timestamp => new DateTime(timestampTicks, DateTimeKind.Utc);
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a TaskResult. Timestamp is set automatically to DateTime.UtcNow.
        /// </summary>
        public TaskResult(
            string taskId,
            string skillId,
            int difficulty,
            bool success,
            int attempts,
            float completionTime,
            float scoreChange)
        {
            this.taskId         = taskId;
            this.skillId        = skillId;
            this.difficulty     = difficulty;
            this.success        = success;
            this.attempts       = attempts;
            this.completionTime = completionTime;
            this.scoreChange    = scoreChange;
            this.timestampTicks = DateTime.UtcNow.Ticks;
        }
        #endregion
    }
}
