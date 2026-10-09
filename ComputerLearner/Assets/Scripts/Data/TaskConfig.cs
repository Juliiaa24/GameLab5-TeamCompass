/**
 * Author: Diego
 * Date: 30/09/26
 * Description: Configuration data passed to a task prefab at runtime.
 *              Stored inside a TaskDefinition ScriptableObject.
 */
using System;
using UnityEngine;

namespace ComputerLearning
{
    /// <summary>
    /// Data-only configuration that tells a BaseTask prefab how to behave.
    /// Stored inside a TaskDefinition ScriptableObject and passed to the task on Initialise().
    ///
    /// Add new fields here as new task types require them.
    /// Fields that are irrelevant to a specific task type are simply ignored.
    /// </summary>
    [Serializable]
    public class TaskConfig
    {
        [Header("Target Properties")]
        [Tooltip("Visual diameter of the target in UI units.")]
        public float targetSize = 100f;

        [Tooltip("Number of targets to spawn simultaneously.")]
        public int targetCount = 1;

        [Tooltip("If true, the target moves around the content area after spawning.")]
        public bool isMoving = false;

        [Tooltip("Movement speed in UI units per second when isMoving is true.")]
        public float moveSpeed = 50f;

        [Header("Timing")]
        [Tooltip("Time limit in seconds. Set to 0 for unlimited time.")]
        public float timeLimit = 0f;

        [Tooltip("Required hover duration in seconds (used by HoverTargetTask).")]
        public float requiredHoverDuration = 1.0f;

        [Header("Attempts")]
        [Tooltip("Maximum number of incorrect attempts before the task is failed. 0 = unlimited.")]
        public int maxAttempts = 0;
    }
}
