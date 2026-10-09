/**
 * Author: Diego
 * Date: 30/09/26
 * Description: Manages all skill states, score updates, difficulty calculation and selection weights.
 */
using System.Collections.Generic;
using UnityEngine;

namespace ComputerLearning
{
    /// <summary>
    /// Manages all skill states. Responsibilities:
    /// - Storing and exposing SkillStates (current score, success/failure counts)
    /// - Updating scores when task results arrive
    /// - Calculating difficulty from score via DifficultyConfig
    /// - Providing selection weights for the adaptive algorithm
    ///
    /// Singleton. Add to the persistent manager GameObject.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class SkillManager : MonoBehaviour
    {
        #region Public Variables
        public static SkillManager Instance { get; private set; }

        /// <summary>Read-only view of all current skill states.</summary>
        public IReadOnlyDictionary<string, SkillState> AllStates => skillStates;
        #endregion

        #region Private Variables
        [Header("Skills")]
        [Tooltip("All SkillDefinition assets available in this game. Register every skill here.")]
        [SerializeField] private List<SkillDefinition> allSkills;

        [Header("Configuration")]
        [Tooltip("The DifficultyConfig asset that defines thresholds, score changes, and selection weights.")]
        [SerializeField] private DifficultyConfig difficultyConfig;

        private readonly Dictionary<string, SkillState> skillStates = new Dictionary<string, SkillState>();
        #endregion

        #region Unity Methods
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            InitialiseSkills();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
        #endregion

        #region Public Methods
        /// <summary>Returns the current score (0–100) for a skill. Returns 0 if skill not found.</summary>
        public float GetScore(string skillId)
        {
            return skillStates.TryGetValue(skillId, out SkillState s) ? s.score : 0f;
        }

        /// <summary>Returns the SkillState for a skill, or null if not registered.</summary>
        public SkillState GetState(string skillId)
        {
            return skillStates.TryGetValue(skillId, out SkillState s) ? s : null;
        }

        /// <summary>
        /// Converts the current score of a skill to a difficulty level (1–5)
        /// using the DifficultyConfig thresholds.
        /// </summary>
        public int GetDifficulty(string skillId)
        {
            if (difficultyConfig == null) return 1;
            return difficultyConfig.ScoreToDifficulty(GetScore(skillId));
        }

        /// <summary>
        /// Applies a TaskResult: updates score (clamped 0–100), success/failure counts,
        /// and the last-practiced timestamp.
        /// </summary>
        public void ApplyResult(TaskResult result)
        {
            if (result == null) return;

            if (!skillStates.TryGetValue(result.skillId, out SkillState state))
            {
                Debug.LogWarning($"[SkillManager] ApplyResult: unknown skill id '{result.skillId}'");
                return;
            }

            float oldScore = state.score;
            state.score = Mathf.Clamp(state.score + result.scoreChange, 0f, 100f);
            state.tasksCompleted++;

            if (result.success) state.successCount++;
            else state.failureCount++;

            state.LastPracticed = System.DateTime.UtcNow;

            Debug.Log($"[SkillManager] {result.skillId}: {oldScore:F1} → {state.score:F1} " +
                      $"({(result.scoreChange >= 0 ? "+" : "")}{result.scoreChange:F1}) " +
                      $"[{(result.success ? "SUCCESS" : "FAILURE")} D{result.difficulty}]");
        }

        /// <summary>
        /// Returns a dictionary of skillId → selection weight.
        /// Higher weight means the skill will be selected more often.
        /// Weights are calculated by DifficultyConfig.GetSelectionWeight().
        /// </summary>
        public Dictionary<string, float> GetSkillWeights()
        {
            var weights = new Dictionary<string, float>(skillStates.Count);
            foreach (var pair in skillStates)
            {
                float weight = difficultyConfig != null
                    ? difficultyConfig.GetSelectionWeight(pair.Value.score)
                    : 1f;
                weights[pair.Key] = weight;
            }
            return weights;
        }

        /// <summary>
        /// Manually overrides a skill's score. Used by InitialTestManager to seed starting scores.
        /// Score is clamped to [0, 100].
        /// </summary>
        public void SetScore(string skillId, float score)
        {
            if (!skillStates.TryGetValue(skillId, out SkillState state))
            {
                Debug.LogWarning($"[SkillManager] SetScore: unknown skill '{skillId}'");
                return;
            }
            state.score = Mathf.Clamp(score, 0f, 100f);
        }

        /// <summary>
        /// Calculates the score change for a given difficulty and outcome without applying it.
        /// Used by BaseTask to fill in the TaskResult before firing OnTaskCompleted.
        /// </summary>
        public float CalculateScoreChange(int difficulty, bool success)
        {
            if (difficultyConfig == null) return success ? 2f : -1f;
            return difficultyConfig.GetScoreChange(difficulty, success);
        }

        /// <summary>Returns true if a skill with the given id is registered.</summary>
        public bool HasSkill(string skillId) => skillStates.ContainsKey(skillId);

        /// <summary>
        /// Resets all skill scores to 0.
        /// Call when starting a fresh player profile.
        /// </summary>
        public void ResetAllScores()
        {
            foreach (var pair in skillStates)
            {
                pair.Value.score = 0f;
                pair.Value.tasksCompleted = 0;
                pair.Value.successCount = 0;
                pair.Value.failureCount = 0;
                pair.Value.lastPracticedTicks = 0;
            }
            Debug.Log("[SkillManager] All skill scores reset to 0.");
        }
        #endregion

        #region Private Methods
        private void InitialiseSkills()
        {
            skillStates.Clear();
            if (allSkills == null) return;

            foreach (SkillDefinition def in allSkills)
            {
                if (def == null || string.IsNullOrEmpty(def.skillId))
                {
                    Debug.LogWarning("[SkillManager] Null or empty skillId found in allSkills list — skipping.");
                    continue;
                }
                if (skillStates.ContainsKey(def.skillId))
                {
                    Debug.LogWarning($"[SkillManager] Duplicate skill id '{def.skillId}' — skipping.");
                    continue;
                }
                skillStates[def.skillId] = new SkillState(def.skillId, 0f);
            }

            Debug.Log($"[SkillManager] Initialised {skillStates.Count} skills.");
        }
        #endregion

        #region Debug
        [ContextMenu("Debug: Print All Skill Scores")]
        private void DebugPrintScores()
        {
            if (skillStates.Count == 0) { Debug.Log("[SkillManager] No skills registered."); return; }
            Debug.Log("[SkillManager] ──── Current Skill Scores ────");
            foreach (var pair in skillStates)
            {
                SkillState s = pair.Value;
                Debug.Log($"  {s.skillId,-20} score={s.score,6:F1}  " +
                          $"difficulty={GetDifficulty(s.skillId)}  " +
                          $"completed={s.tasksCompleted}  " +
                          $"successRate={s.SuccessRate,5:P0}");
            }
        }
        #endregion
    }
}
