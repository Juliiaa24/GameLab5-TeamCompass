/**
 * Author: Diego
 * Date: 30/09/26
 * Description: Configuration that controls when a learning session ends.
 */
using System;
using UnityEngine;

namespace ComputerLearning
{
    /// <summary>
    /// Configures the end conditions for one learning session.
    /// All conditions are checked after each task completes.
    /// Set any value to 0 to disable that particular condition.
    ///
    /// Example:
    ///   maxTasks = 10, maxDurationSeconds = 0  → ends after exactly 10 tasks
    ///   maxTasks = 0,  maxDurationSeconds = 300 → ends after 5 minutes (if minDistinctSkills met)
    ///   maxTasks = 10, maxDurationSeconds = 300 → ends at whichever comes first
    /// </summary>
    [Serializable]
    public class SessionConfig
    {
        [Tooltip("Maximum number of tasks per session. 0 = unlimited.")]
        public int maxTasks = 10;

        [Tooltip("Maximum session duration in seconds. 0 = unlimited.")]
        public float maxDurationSeconds = 300f;

        [Tooltip("Minimum number of DISTINCT skills that must be practiced before the session " +
                 "is allowed to end via the time limit. Does not block the maxTasks limit.")]
        public int minDistinctSkills = 2;

        [Tooltip("How many recent task IDs to remember for anti-repetition filtering.")]
        public int antiRepetitionWindowSize = 5;
    }
}
