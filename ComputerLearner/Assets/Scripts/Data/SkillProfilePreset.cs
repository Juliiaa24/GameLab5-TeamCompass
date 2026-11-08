/**
 * Author: TeamCompass
 * Description: Serializable skill score entry and preset factory / ScriptableObject for seeding
 *              SkillManager when a student starts a LearningPath without taking the Initial Assessment.
 */
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ComputerLearning
{
    [Serializable]
    public class SkillScoreEntry
    {
        public string skillId;
        [Range(0f, 100f)]
        public float score;

        public SkillScoreEntry() { }

        public SkillScoreEntry(string skillId, float score)
        {
            this.skillId = skillId;
            this.score = Mathf.Clamp(score, 0f, 100f);
        }
    }

    [CreateAssetMenu(fileName = "SkillProfilePreset", menuName = "Computer Learner/Learning Paths/Skill Profile Preset")]
    public class SkillProfilePreset : ScriptableObject
    {
        [Tooltip("Target LearningPath this preset corresponds to.")]
        public LearningPath learningPath = LearningPath.BeginnerFoundations;

        [Tooltip("Initial skill scores (0-100) applied when the Initial Assessment is bypassed.")]
        public List<SkillScoreEntry> skillScores = new List<SkillScoreEntry>();

        /// <summary>
        /// All skill IDs tracked across SkillManager, InitialTestManager, and SkillDefinition assets.
        /// </summary>
        public static readonly string[] AllKnownSkillIds =
        {
            "mouse_move",
            "hover",
            "click",
            "double_click",
            "right_click",
            "click_hold",
            "drag_drop",
            "scroll",
            "shortcuts"
        };

        /// <summary>
        /// Generates the canonical default skill score profile for a given LearningPath.
        /// Tier mapping in DifficultyConfig:
        ///   0-19  -> Difficulty 1
        ///   20-39 -> Difficulty 2
        ///   40-59 -> Difficulty 3
        ///   60-79 -> Difficulty 4
        ///   80+   -> Difficulty 5
        /// </summary>
        public static List<SkillScoreEntry> GetDefaultEntriesForPath(LearningPath path)
        {
            switch (path)
            {
                case LearningPath.BeginnerFoundations:
                    return CreateUniformProfile(10f);

                case LearningPath.StandardIntermediate:
                    return new List<SkillScoreEntry>
                    {
                        new SkillScoreEntry("mouse_move", 55f),
                        new SkillScoreEntry("hover", 50f),
                        new SkillScoreEntry("click", 50f),
                        new SkillScoreEntry("double_click", 35f),
                        new SkillScoreEntry("right_click", 25f),
                        new SkillScoreEntry("click_hold", 25f),
                        new SkillScoreEntry("drag_drop", 25f),
                        new SkillScoreEntry("scroll", 30f),
                        new SkillScoreEntry("shortcuts", 20f)
                    };

                case LearningPath.AdvancedApplications:
                    return new List<SkillScoreEntry>
                    {
                        new SkillScoreEntry("mouse_move", 75f),
                        new SkillScoreEntry("hover", 75f),
                        new SkillScoreEntry("click", 75f),
                        new SkillScoreEntry("double_click", 75f),
                        new SkillScoreEntry("right_click", 75f),
                        new SkillScoreEntry("click_hold", 75f),
                        new SkillScoreEntry("drag_drop", 75f),
                        new SkillScoreEntry("scroll", 75f),
                        new SkillScoreEntry("shortcuts", 60f)
                    };

                case LearningPath.FreePractice:
                    return CreateUniformProfile(50f);

                case LearningPath.DiagnosticFull:
                case LearningPath.Custom:
                default:
                    return CreateUniformProfile(0f);
            }
        }

        public static List<SkillScoreEntry> CreateUniformProfile(float score)
        {
            float clamped = Mathf.Clamp(score, 0f, 100f);
            List<SkillScoreEntry> list = new List<SkillScoreEntry>(AllKnownSkillIds.Length);
            for (int i = 0; i < AllKnownSkillIds.Length; i++)
            {
                list.Add(new SkillScoreEntry(AllKnownSkillIds[i], clamped));
            }
            return list;
        }
    }
}
