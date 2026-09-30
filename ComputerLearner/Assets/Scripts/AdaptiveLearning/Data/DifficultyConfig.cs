/**
 * Author: Diego
 * Date: 30/09/26
 * Description: Central configuration for difficulty thresholds, score changes and selection weights.
 *              All tunable numbers live here — edit in the Inspector, no code changes needed.
 */
using UnityEngine;

namespace ComputerLearning
{
    /// <summary>
    /// Central configuration for the scoring and difficulty system.
    /// Create one asset and assign it to SkillManager.
    /// All "magic numbers" live here — edit in the Inspector, no code changes needed.
    /// </summary>
    [CreateAssetMenu(fileName = "DifficultyConfig", menuName = "ComputerLearning/Difficulty Config")]
    public class DifficultyConfig : ScriptableObject
    {
        #region Public Variables
        [Header("Score → Difficulty Thresholds")]
        [Tooltip(
            "Four values that divide the 0–100 score range into five difficulty bands.\n" +
            "Default: [20, 40, 60, 80]\n" +
            "  score  0–19  → Difficulty 1\n" +
            "  score 20–39  → Difficulty 2\n" +
            "  score 40–59  → Difficulty 3\n" +
            "  score 60–79  → Difficulty 4\n" +
            "  score 80–100 → Difficulty 5")]
        public int[] scoreThresholds = { 20, 40, 60, 80 };

        [Header("Score Changes per Difficulty (indexed by difficulty - 1)")]
        [Tooltip("Points added to a skill score when a task is completed SUCCESSFULLY.\n" +
                 "Index 0 = Difficulty 1, Index 4 = Difficulty 5.")]
        public float[] successGains = { 2f, 4f, 6f, 8f, 10f };

        [Tooltip("Points removed from a skill score when a task FAILS.\n" +
                 "Stored as positive values — subtracted internally.")]
        public float[] failureLosses = { 1f, 2f, 3f, 4f, 5f };

        [Header("Adaptive Selection")]
        [Tooltip(
            "Exponent used in the weight formula:  weight = (101 - score) ^ exponent\n" +
            "1.0 = linear (mild preference for weak skills)\n" +
            "1.5 = default (moderate preference)\n" +
            "2.0 = aggressive (very strong preference for weak skills)")]
        public float selectionExponent = 1.5f;
        #endregion

        #region Public Methods
        /// <summary>
        /// Converts a skill score (0–100) into a difficulty level (1–5)
        /// using the configured thresholds.
        /// </summary>
        public int ScoreToDifficulty(float score)
        {
            for (int i = 0; i < scoreThresholds.Length; i++)
            {
                if (score < scoreThresholds[i]) return i + 1;
            }
            // Score is above all thresholds → highest difficulty
            return scoreThresholds.Length + 1;
        }

        /// <summary>
        /// Returns the score change for a given difficulty and outcome.
        /// Positive on success, negative on failure.
        /// </summary>
        public float GetScoreChange(int difficulty, bool success)
        {
            int index = Mathf.Clamp(difficulty - 1, 0, 4);

            if (success)
                return (index < successGains.Length) ? successGains[index] : 0f;
            else
                return (index < failureLosses.Length) ? -failureLosses[index] : 0f;
        }

        /// <summary>
        /// Calculates the adaptive selection weight for a skill with the given score.
        /// Lower score → higher weight → selected more often.
        /// A fully mastered skill (score 100) still gets weight 1 for occasional review.
        /// </summary>
        public float GetSelectionWeight(float score)
        {
            float b = 101f - Mathf.Clamp(score, 0f, 100f);
            return Mathf.Pow(b, selectionExponent);
        }
        #endregion
    }
}
