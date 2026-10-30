/**
 * Author: TeamCompass (Diego & AI)
 * Date: 23/10/2026
 * Description: Data structures for Level Medal evaluation and persistent player records.
 */
using System;
using UnityEngine;

namespace ComputerLearning
{
    public enum LevelMedal
    {
        None = 0,
        Bronze = 1,
        Silver = 2,
        Gold = 3
    }

    [Serializable]
    public class LevelScoreRecord
    {
        public string levelId;
        public LevelMedal bestMedal;
        public float bestScore;        // Accuracy percentage (0 - 100%)
        public float bestTimeSeconds;  // Best completion time in seconds
        public int attemptsCount;      // Total attempts made on this level
        public int bestMistakes;       // Mistakes recorded on best attempt
        public long lastCompletedTicks;

        public DateTime LastCompletedTime => new DateTime(lastCompletedTicks, DateTimeKind.Utc);

        public LevelScoreRecord()
        {
            levelId = string.Empty;
            bestMedal = LevelMedal.None;
            bestScore = 0f;
            bestTimeSeconds = 0f;
            attemptsCount = 0;
            bestMistakes = 0;
            lastCompletedTicks = DateTime.UtcNow.Ticks;
        }

        public LevelScoreRecord(string levelId, LevelMedal medal, float score, float timeSeconds, int mistakes)
        {
            this.levelId = levelId;
            this.bestMedal = medal;
            this.bestScore = score;
            this.bestTimeSeconds = timeSeconds;
            this.attemptsCount = 1;
            this.bestMistakes = mistakes;
            this.lastCompletedTicks = DateTime.UtcNow.Ticks;
        }

        public static string GetMedalName(LevelMedal medal)
        {
            UIDialogs ui = UIDialogs.Current;
            switch (medal)
            {
                case LevelMedal.Gold: return !string.IsNullOrEmpty(ui.medal_gold) ? ui.medal_gold : "Gold";
                case LevelMedal.Silver: return !string.IsNullOrEmpty(ui.medal_silver) ? ui.medal_silver : "Silver";
                case LevelMedal.Bronze: return !string.IsNullOrEmpty(ui.medal_bronze) ? ui.medal_bronze : "Bronze";
                default: return !string.IsNullOrEmpty(ui.medal_none) ? ui.medal_none : "None";
            }
        }

        public static string GetMedalEmoji(LevelMedal medal)
        {
            switch (medal)
            {
                case LevelMedal.Gold: return "[GOLD]";
                case LevelMedal.Silver: return "[SILVER]";
                case LevelMedal.Bronze: return "[BRONZE]";
                default: return "-";
            }
        }

        public static Color GetMedalColor(LevelMedal medal)
        {
            switch (medal)
            {
                case LevelMedal.Gold: return new Color(1.0f, 0.84f, 0.0f);     // #FFD700
                case LevelMedal.Silver: return new Color(0.78f, 0.82f, 0.86f); // #C7D1DC
                case LevelMedal.Bronze: return new Color(0.80f, 0.50f, 0.20f); // #CD7F32
                default: return new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }
        }

        public static string GetMedalColorHex(LevelMedal medal)
        {
            switch (medal)
            {
                case LevelMedal.Gold: return "#FFD700";
                case LevelMedal.Silver: return "#C0C0C0";
                case LevelMedal.Bronze: return "#CD7F32";
                default: return "#888888";
            }
        }
    }
}
