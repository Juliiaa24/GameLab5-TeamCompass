/**
 * Author: Julia & AI (TeamCompass)
 * Date: 01/11/2026
 * Description: Manages Playtesting Student IDs (auto-incremental S01, S02... + custom override)
 *              and exports clear, human-readable English student reports (.txt, .html, Excel-ready .csv, and .json)
 *              directly to Desktop/ComputerLearner_PlaytestData.
 */
using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;

namespace ComputerLearning
{
    [Serializable]
    public class LevelAttemptLogEntry
    {
        public int attemptNumber;
        public string timestamp;
        public string levelId;
        public string levelName;
        public string medal;
        public float accuracyPercent;
        public float durationSeconds;
        public int mistakes;
    }

    [Serializable]
    public class SkillProgressionExportItem
    {
        public string skillId;
        public string skillName;
        public bool testedInAssessment;
        public float initialAssessmentScore;
        public float initialAssessmentTimeSeconds;
        public int initialAssessmentExtraAttempts;
        public float finalScore;
        public float improvementDelta;
        public int finalDifficulty;
        public int tasksAttempted;
        public int tasksSucceeded;
        public int tasksFailed;
        public float taskSuccessRatePercent;
    }

    [Serializable]
    public class LevelSummaryExportItem
    {
        public string levelId;
        public string levelName;
        public bool completed;
        public int attemptsCount;
        public string bestMedal;
        public float bestAccuracyPercent;
        public float bestTimeSeconds;
        public int bestMistakes;
    }

    [Serializable]
    public class TaskProgressionExportItem
    {
        public int stepIndex;
        public string timestamp;
        public string levelId;
        public string levelName;
        public string taskId;
        public string skillId;
        public string skillName;
        public int difficulty;
        public bool success;
        public int extraMistakes;
        public float completionTimeSeconds;
        public float scoreBefore;
        public float scoreChange;
        public float scoreAfter;
    }

    [Serializable]
    public class StudentFullPlaytestReport
    {
        public string studentId;
        public string sessionStartTime;
        public string lastExportTime;
        public string exportTrigger;
        public string language;
        public bool initialAssessmentCompleted;
        public float initialAssessmentAverageScore;
        public float finalSkillsAverageScore;
        public float averageImprovementDelta;
        public int totalLevelsCompleted;
        public int totalTasksAttempted;
        public int totalTasksSucceeded;
        public AssessmentExportReport initialAssessment;
        public List<SkillProgressionExportItem> skillProgression = new List<SkillProgressionExportItem>();
        public List<LevelSummaryExportItem> levelsSummary = new List<LevelSummaryExportItem>();
        public List<LevelAttemptLogEntry> levelAttemptsHistory = new List<LevelAttemptLogEntry>();
        public List<TaskProgressionExportItem> taskHistory = new List<TaskProgressionExportItem>();
    }

    public static class PlaytestDataManager
    {
        private const string CounterKey = "Playtest_StudentCounter";
        private const string CurrentStudentIdKey = "Playtest_CurrentStudentId";
        private const string SessionStartKey = "Playtest_SessionStartTime";
        private const string LastExportedFolderKey = "Playtest_LastExportedStudentFolder";
        private const string CurrentAssessmentJsonKey = "Playtest_CurrentAssessmentJson";
        private const string MainDesktopFolderName = "ComputerLearner_PlaytestData";

        public static readonly string[] AllSkillIds =
        {
            "mouse_move",
            "hover",
            "click",
            "double_click",
            "right_click",
            "click_hold",
            "drag_drop"
        };

        public static event Action<string> OnStudentIdChanged;
        public static event Action<string> OnDataExported;

        public static string CurrentStudentId
        {
            get
            {
                EnsureInitialized();
                return PlayerPrefs.GetString(CurrentStudentIdKey, "S01");
            }
        }

        public static string SessionStartTime
        {
            get
            {
                EnsureInitialized();
                return PlayerPrefs.GetString(SessionStartKey, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
        }

        public static void EnsureInitialized()
        {
            if (!PlayerPrefs.HasKey(CounterKey))
            {
                PlayerPrefs.SetInt(CounterKey, 1);
            }

            if (string.IsNullOrWhiteSpace(PlayerPrefs.GetString(CurrentStudentIdKey, string.Empty)))
            {
                int counter = Mathf.Max(1, PlayerPrefs.GetInt(CounterKey, 1));
                PlayerPrefs.SetString(CurrentStudentIdKey, $"S{counter:D2}");
            }

            if (string.IsNullOrWhiteSpace(PlayerPrefs.GetString(SessionStartKey, string.Empty)))
            {
                PlayerPrefs.SetString(SessionStartKey, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
        }

        public static void SetCurrentStudentId(string customId)
        {
            EnsureInitialized();
            string oldId = PlayerPrefs.GetString(CurrentStudentIdKey, "S01");
            string sanitized = SanitizeStudentId(customId);
            if (string.IsNullOrEmpty(sanitized))
            {
                int counter = Mathf.Max(1, PlayerPrefs.GetInt(CounterKey, 1));
                sanitized = $"S{counter:D2}";
            }

            string lastFolder = PlayerPrefs.GetString(LastExportedFolderKey, string.Empty);
            if (!string.IsNullOrEmpty(lastFolder) && Directory.Exists(lastFolder) && !string.Equals(oldId, sanitized, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    string newFolder = Path.Combine(GetDesktopExportRootPath(), sanitized);
                    if (!Directory.Exists(newFolder))
                    {
                        Directory.Move(lastFolder, newFolder);
                        foreach (string oldFile in Directory.GetFiles(newFolder, $"*_{SanitizeStudentId(oldId)}_*"))
                        {
                            File.Delete(oldFile);
                        }
                        PlayerPrefs.SetString(LastExportedFolderKey, newFolder);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[PlaytestDataManager] Could not rename previous folder: {e.Message}");
                }
            }

            PlayerPrefs.SetString(CurrentStudentIdKey, sanitized);
            PlayerPrefs.Save();
            OnStudentIdChanged?.Invoke(sanitized);
        }

        public static string AdvanceToNextStudent()
        {
            EnsureInitialized();
            int nextCounter = Mathf.Max(1, PlayerPrefs.GetInt(CounterKey, 1)) + 1;
            string nextId = $"S{nextCounter:D2}";
            string nowStart = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            PlayerPrefs.SetInt(CounterKey, nextCounter);
            PlayerPrefs.SetString(CurrentStudentIdKey, nextId);
            PlayerPrefs.SetString(SessionStartKey, nowStart);
            PlayerPrefs.DeleteKey(LastExportedFolderKey);
            PlayerPrefs.DeleteKey(CurrentAssessmentJsonKey);
            PlayerPrefs.Save();

            OnStudentIdChanged?.Invoke(nextId);
            return nextId;
        }

        public static bool HasAnyRecordedActivityForCurrentStudent()
        {
            if (PlayerPrefs.GetInt("InitialAssessmentCompleted", 0) == 1) return true;
            if (!string.IsNullOrEmpty(PlayerPrefs.GetString(CurrentAssessmentJsonKey, string.Empty))) return true;
            if (ProgressData.ExistingInstance != null)
            {
                if (ProgressData.ExistingInstance.TotalResultCount > 0) return true;
                if (ProgressData.ExistingInstance.LevelRecords != null && ProgressData.ExistingInstance.LevelRecords.Count > 0) return true;
            }
            else if (PlayerPrefs.HasKey("ComputerLearner_ProgressData_v1"))
            {
                return true;
            }
            foreach (string skillId in AllSkillIds)
            {
                if (PlayerPrefs.GetFloat($"SkillScore_{skillId}", 0f) > 0f) return true;
                if (PlayerPrefs.GetInt($"SkillTasks_{skillId}", 0) > 0) return true;
            }
            return false;
        }

        public static void SaveCurrentAssessmentReport(AssessmentExportReport report)
        {
            if (report == null) return;
            EnsureInitialized();
            string json = JsonUtility.ToJson(report, true);
            PlayerPrefs.SetString(CurrentAssessmentJsonKey, json);

            if (report.skills != null)
            {
                foreach (var item in report.skills)
                {
                    if (item == null || string.IsNullOrEmpty(item.skillId)) continue;
                    PlayerPrefs.SetFloat($"InitialAssessmentScore_{item.skillId}", item.score);
                    PlayerPrefs.SetFloat($"InitialAssessmentTime_{item.skillId}", item.timeTakenSeconds);
                    PlayerPrefs.SetInt($"InitialAssessmentAttempts_{item.skillId}", item.extraAttempts);
                    PlayerPrefs.SetInt($"InitialAssessmentTested_{item.skillId}", 1);
                }
            }

            PlayerPrefs.Save();
            ExportCurrentStudentData("initial_assessment_completed");
        }

        public static AssessmentExportReport GetCurrentAssessmentReport()
        {
            if (PlayerPrefs.GetInt("InitialAssessmentCompleted", 0) == 0)
            {
                return null;
            }

            string json = PlayerPrefs.GetString(CurrentAssessmentJsonKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    return JsonUtility.FromJson<AssessmentExportReport>(json);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[PlaytestDataManager] Failed to parse stored assessment report: {e.Message}");
                }
            }

            try
            {
                string folder = Path.Combine(Application.dataPath, "Assessment");
                if (Directory.Exists(folder))
                {
                    string[] files = Directory.GetFiles(folder, "*.json");
                    if (files.Length > 0)
                    {
                        Array.Sort(files, (a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));
                        string content = File.ReadAllText(files[0]);
                        if (!string.IsNullOrEmpty(content))
                        {
                            return JsonUtility.FromJson<AssessmentExportReport>(content);
                        }
                    }
                }
            }
            catch (Exception) { }

            return null;
        }

        public static void ClearCurrentStudentSessionData()
        {
            PlayerPrefs.DeleteKey(CurrentAssessmentJsonKey);
            PlayerPrefs.DeleteKey(LastExportedFolderKey);
            foreach (string skillId in AllSkillIds)
            {
                PlayerPrefs.DeleteKey($"InitialAssessmentScore_{skillId}");
                PlayerPrefs.DeleteKey($"InitialAssessmentTime_{skillId}");
                PlayerPrefs.DeleteKey($"InitialAssessmentAttempts_{skillId}");
                PlayerPrefs.DeleteKey($"InitialAssessmentTested_{skillId}");
                PlayerPrefs.DeleteKey($"SkillScore_{skillId}");
                PlayerPrefs.DeleteKey($"SkillTasks_{skillId}");
                PlayerPrefs.DeleteKey($"SkillSuccess_{skillId}");
                PlayerPrefs.DeleteKey($"SkillFail_{skillId}");
            }
            PlayerPrefs.Save();
        }

        public static string GetDesktopExportRootPath()
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                if (string.IsNullOrEmpty(desktop))
                {
                    desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                }
                if (!string.IsNullOrEmpty(desktop))
                {
                    return Path.Combine(desktop, MainDesktopFolderName);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PlaytestDataManager] Could not resolve Desktop path: {e.Message}");
            }

            return Path.Combine(Application.persistentDataPath, MainDesktopFolderName);
        }

        public static string GetCurrentStudentFolderPath()
        {
            EnsureInitialized();
            string root = GetDesktopExportRootPath();
            string safeId = SanitizeStudentId(CurrentStudentId);
            return Path.Combine(root, safeId);
        }

        /// <summary>
        /// Builds and exports the current student's English reports to Desktop/ComputerLearner_PlaytestData/{StudentId}/
        /// and updates the Master All_Students_Summary.csv file.
        /// </summary>
        public static string ExportCurrentStudentData(string triggerReason = "manual")
        {
            EnsureInitialized();
            try
            {
                string rootDir = GetDesktopExportRootPath();
                if (!Directory.Exists(rootDir))
                {
                    Directory.CreateDirectory(rootDir);
                }

                string studentDir = GetCurrentStudentFolderPath();
                if (!Directory.Exists(studentDir))
                {
                    Directory.CreateDirectory(studentDir);
                }

                PlayerPrefs.SetString(LastExportedFolderKey, studentDir);
                PlayerPrefs.Save();

                StudentFullPlaytestReport fullReport = BuildFullReport(triggerReason);
                string safeId = SanitizeStudentId(fullReport.studentId);

                // Clean up any legacy Spanish filenames if present in the folder
                foreach (string file in Directory.GetFiles(studentDir))
                {
                    string fName = Path.GetFileName(file);
                    if (fName.Contains("Informe_Legible") || fName.Contains("Ficha_Visual") || fName.Contains("Datos_Excel") || fName.Contains("_Datos.json"))
                    {
                        try { File.Delete(file); } catch { }
                    }
                }

                UTF8Encoding utf8WithBom = new UTF8Encoding(true);

                // 1. Human-readable English Text Report (.txt)
                string txtPath = Path.Combine(studentDir, $"1_{safeId}_Readable_Report.txt");
                File.WriteAllText(txtPath, BuildReadableTextReport(fullReport), utf8WithBom);

                // 2. Visual English HTML Dashboard (.html)
                string htmlPath = Path.Combine(studentDir, $"2_{safeId}_Visual_Report.html");
                File.WriteAllText(htmlPath, BuildVisualHtmlReport(fullReport), utf8WithBom);

                // 3. Single Excel-ready English CSV for this student (with sep=; and UTF-8 BOM for automatic Excel column splitting)
                string excelCsvPath = Path.Combine(studentDir, $"3_{safeId}_Excel_Data.csv");
                File.WriteAllText(excelCsvPath, BuildSingleStudentExcelCsv(fullReport), utf8WithBom);

                // 4. Structured English JSON data file
                string jsonPath = Path.Combine(studentDir, $"4_{safeId}_Raw_Data.json");
                File.WriteAllText(jsonPath, JsonUtility.ToJson(fullReport, true), utf8WithBom);

                // 5. Master All-Students Summary CSV at root (in English, with sep=; and UTF-8 BOM)
                string oldSpanishMaster = Path.Combine(rootDir, "Resumen_General_Alumnos.csv");
                if (File.Exists(oldSpanishMaster))
                {
                    try { File.Delete(oldSpanishMaster); } catch { }
                }
                string masterCsvPath = Path.Combine(rootDir, "All_Students_Summary.csv");
                UpdateMasterStudentsCsv(masterCsvPath, fullReport, utf8WithBom);

                Debug.Log($"[PlaytestDataManager] Exported English playtest data for '{fullReport.studentId}' ({triggerReason}) to: {studentDir}");
                OnDataExported?.Invoke(studentDir);
                return studentDir;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlaytestDataManager] Export failed: {ex.Message}");
                return string.Empty;
            }
        }

        public static StudentFullPlaytestReport BuildFullReport(string triggerReason)
        {
            EnsureInitialized();
            AssessmentExportReport assessment = GetCurrentAssessmentReport();

            StudentFullPlaytestReport report = new StudentFullPlaytestReport
            {
                studentId = CurrentStudentId,
                sessionStartTime = SessionStartTime,
                lastExportTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                exportTrigger = triggerReason,
                language = LocalizationManager.CurrentLanguage,
                initialAssessmentCompleted = assessment != null,
                initialAssessmentAverageScore = assessment != null ? assessment.averageScore : 0f,
                initialAssessment = assessment ?? new AssessmentExportReport()
            };

            Dictionary<string, float> initialScoreBySkill = new Dictionary<string, float>();
            Dictionary<string, float> initialTimeBySkill = new Dictionary<string, float>();
            Dictionary<string, int> initialAttemptsBySkill = new Dictionary<string, int>();
            Dictionary<string, bool> testedBySkill = new Dictionary<string, bool>();

            foreach (string skillId in AllSkillIds)
            {
                bool tested = PlayerPrefs.GetInt($"InitialAssessmentTested_{skillId}", 0) == 1;
                float initScore = PlayerPrefs.GetFloat($"InitialAssessmentScore_{skillId}", 0f);
                float initTime = PlayerPrefs.GetFloat($"InitialAssessmentTime_{skillId}", 0f);
                int initAttempts = PlayerPrefs.GetInt($"InitialAssessmentAttempts_{skillId}", 0);

                if (assessment != null && assessment.skills != null)
                {
                    foreach (var s in assessment.skills)
                    {
                        if (s != null && string.Equals(s.skillId, skillId, StringComparison.OrdinalIgnoreCase))
                        {
                            tested = true;
                            initScore = s.score;
                            initTime = s.timeTakenSeconds;
                            initAttempts = s.extraAttempts;
                            s.skillName = GetEnglishSkillName(skillId);
                            break;
                        }
                    }
                }

                testedBySkill[skillId] = tested;
                initialScoreBySkill[skillId] = initScore;
                initialTimeBySkill[skillId] = initTime;
                initialAttemptsBySkill[skillId] = initAttempts;
            }

            Dictionary<string, float> runningScores = new Dictionary<string, float>();
            Dictionary<string, int> histAttempts = new Dictionary<string, int>();
            Dictionary<string, int> histSuccesses = new Dictionary<string, int>();
            Dictionary<string, int> histFailures = new Dictionary<string, int>();

            foreach (string skillId in AllSkillIds)
            {
                runningScores[skillId] = initialScoreBySkill[skillId];
                histAttempts[skillId] = 0;
                histSuccesses[skillId] = 0;
                histFailures[skillId] = 0;
            }

            ProgressData.GetSavedDataSnapshot(
                out IReadOnlyList<TaskResult> savedTasks,
                out List<string> savedCompletedLevels,
                out List<LevelScoreRecord> savedLevelRecords,
                out IReadOnlyList<LevelAttemptLogEntry> savedAttemptsHistory);

            if (savedTasks != null)
            {
                int idx = 1;
                foreach (TaskResult tr in savedTasks)
                {
                    if (tr == null) continue;
                    string skId = !string.IsNullOrEmpty(tr.skillId) ? tr.skillId : "unknown";
                    float before = runningScores.TryGetValue(skId, out float cur) ? cur : 0f;
                    float after = Mathf.Clamp(before + tr.scoreChange, 0f, 100f);
                    runningScores[skId] = after;

                    if (histAttempts.ContainsKey(skId))
                    {
                        histAttempts[skId]++;
                        if (tr.success) histSuccesses[skId]++;
                        else histFailures[skId]++;
                    }

                    report.totalTasksAttempted++;
                    if (tr.success) report.totalTasksSucceeded++;

                    report.taskHistory.Add(new TaskProgressionExportItem
                    {
                        stepIndex = idx++,
                        timestamp = tr.timestampTicks > 0 ? tr.Timestamp.ToLocalTime().ToString("HH:mm:ss") : string.Empty,
                        levelId = tr.levelId ?? string.Empty,
                        levelName = !string.IsNullOrEmpty(tr.levelId) ? GetEnglishLevelName(tr.levelId) : (tr.levelName ?? string.Empty),
                        taskId = tr.taskId ?? string.Empty,
                        skillId = skId,
                        skillName = GetEnglishSkillName(skId),
                        difficulty = tr.difficulty,
                        success = tr.success,
                        extraMistakes = tr.attempts,
                        completionTimeSeconds = (float)Math.Round(tr.completionTime, 2),
                        scoreBefore = (float)Math.Round(before, 1),
                        scoreChange = (float)Math.Round(tr.scoreChange, 1),
                        scoreAfter = (float)Math.Round(after, 1)
                    });
                }
            }

            float sumFinalScores = 0f;
            float sumDelta = 0f;
            foreach (string skillId in AllSkillIds)
            {
                float initScore = initialScoreBySkill[skillId];
                float finalScore = SkillManager.GetPersistentScore(skillId);
                if (histAttempts[skillId] > 0 && Mathf.Approximately(finalScore, 0f) && runningScores[skillId] > 0f)
                {
                    finalScore = runningScores[skillId];
                }

                int attempts = Mathf.Max(histAttempts[skillId], SkillManager.GetPersistentTasksCompleted(skillId));
                int successes = Mathf.Max(histSuccesses[skillId], SkillManager.GetPersistentSuccessCount(skillId));
                int failures = Mathf.Max(histFailures[skillId], SkillManager.GetPersistentFailureCount(skillId));
                float successRate = attempts > 0 ? ((float)successes / attempts) * 100f : 0f;
                int difficulty = ScoreToDifficultyTier(finalScore);
                float delta = finalScore - initScore;

                sumFinalScores += finalScore;
                sumDelta += delta;

                report.skillProgression.Add(new SkillProgressionExportItem
                {
                    skillId = skillId,
                    skillName = GetEnglishSkillName(skillId),
                    testedInAssessment = testedBySkill[skillId],
                    initialAssessmentScore = (float)Math.Round(initScore, 1),
                    initialAssessmentTimeSeconds = (float)Math.Round(initialTimeBySkill[skillId], 2),
                    initialAssessmentExtraAttempts = initialAttemptsBySkill[skillId],
                    finalScore = (float)Math.Round(finalScore, 1),
                    improvementDelta = (float)Math.Round(delta, 1),
                    finalDifficulty = difficulty,
                    tasksAttempted = attempts,
                    tasksSucceeded = successes,
                    tasksFailed = failures,
                    taskSuccessRatePercent = (float)Math.Round(successRate, 1)
                });
            }

            report.finalSkillsAverageScore = (float)Math.Round(sumFinalScores / AllSkillIds.Length, 1);
            report.averageImprovementDelta = (float)Math.Round(sumDelta / AllSkillIds.Length, 1);

            foreach (string levelId in ProgressData.LevelOrder)
            {
                bool completed = savedCompletedLevels != null && savedCompletedLevels.Contains(levelId);
                LevelScoreRecord rec = null;
                if (savedLevelRecords != null)
                {
                    for (int i = 0; i < savedLevelRecords.Count; i++)
                    {
                        if (string.Equals(savedLevelRecords[i].levelId, levelId, StringComparison.OrdinalIgnoreCase))
                        {
                            rec = savedLevelRecords[i];
                            break;
                        }
                    }
                }
                if (completed) report.totalLevelsCompleted++;

                report.levelsSummary.Add(new LevelSummaryExportItem
                {
                    levelId = levelId,
                    levelName = GetEnglishLevelName(levelId),
                    completed = completed,
                    attemptsCount = rec != null ? rec.attemptsCount : 0,
                    bestMedal = rec != null ? rec.bestMedal.ToString() : LevelMedal.None.ToString(),
                    bestAccuracyPercent = rec != null ? (float)Math.Round(rec.bestScore, 1) : 0f,
                    bestTimeSeconds = rec != null ? (float)Math.Round(rec.bestTimeSeconds, 2) : 0f,
                    bestMistakes = rec != null ? rec.bestMistakes : 0
                });
            }

            if (savedAttemptsHistory != null)
            {
                foreach (var entry in savedAttemptsHistory)
                {
                    report.levelAttemptsHistory.Add(new LevelAttemptLogEntry
                    {
                        attemptNumber = entry.attemptNumber,
                        timestamp = entry.timestamp,
                        levelId = entry.levelId,
                        levelName = GetEnglishLevelName(entry.levelId),
                        medal = entry.medal,
                        accuracyPercent = entry.accuracyPercent,
                        durationSeconds = entry.durationSeconds,
                        mistakes = entry.mistakes
                    });
                }
            }

            return report;
        }

        private static string BuildReadableTextReport(StudentFullPlaytestReport r)
        {
            StringBuilder sb = new StringBuilder();
            string sepDouble = new string('=', 96);
            string sepSingle = new string('-', 96);

            sb.AppendLine(sepDouble);
            sb.AppendLine($"  STUDENT PLAYTEST EVALUATION & PROGRESS REPORT: {r.studentId}");
            sb.AppendLine(sepDouble);
            sb.AppendLine($"  Student ID / Name        : {r.studentId}");
            sb.AppendLine($"  Session Start Time       : {r.sessionStartTime}");
            sb.AppendLine($"  Last Saved Time          : {r.lastExportTime}");
            sb.AppendLine($"  In-Game Language         : {r.language}");
            sb.AppendLine($"  Initial Assessment       : {(r.initialAssessmentCompleted ? $"Completed (Average Score: {r.initialAssessmentAverageScore:0.#}%)" : "Not completed yet")}");
            sb.AppendLine($"  Final Skills Average     : {r.finalSkillsAverageScore:0.#}%  (Average Improvement: {(r.averageImprovementDelta >= 0 ? "+" : "")}{r.averageImprovementDelta:0.#}%)");
            sb.AppendLine($"  Levels Completed         : {r.totalLevelsCompleted} of {ProgressData.LevelOrder.Length}");
            sb.AppendLine($"  Total Tasks Played       : {r.totalTasksAttempted} ({r.totalTasksSucceeded} succeeded)");
            sb.AppendLine(sepDouble);
            sb.AppendLine();

            // SECTION 1: INITIAL ASSESSMENT & SKILL EVOLUTION
            sb.AppendLine("1. INITIAL ASSESSMENT & SKILL PROGRESSION");
            sb.AppendLine(sepSingle);
            sb.AppendLine(string.Format(
                "{0,-24} | {1,13} | {2,11} | {3,11} | {4,11} | {5,10} | {6,12}",
                "Skill", "Initial Score", "Test Time", "Final Score", "Improvement", "Difficulty", "Tasks (W/T)"));
            sb.AppendLine(sepSingle);

            foreach (var s in r.skillProgression)
            {
                string initStr = s.testedInAssessment ? $"{s.initialAssessmentScore:0}%" : "Not tested";
                string timeStr = s.testedInAssessment ? $"{s.initialAssessmentTimeSeconds:0.0}s ({s.initialAssessmentExtraAttempts}e)" : "-";
                string finalStr = $"{s.finalScore:0}%";
                string deltaStr = $"{(s.improvementDelta >= 0 ? "+" : "")}{s.improvementDelta:0}%";
                string diffStr = $"Tier {s.finalDifficulty}/5";
                string tasksStr = s.tasksAttempted > 0 ? $"{s.tasksSucceeded}/{s.tasksAttempted} ({s.taskSuccessRatePercent:0}%)" : "0 played";

                sb.AppendLine(string.Format(
                    "{0,-24} | {1,13} | {2,11} | {3,11} | {4,11} | {5,10} | {6,12}",
                    Truncate(s.skillName, 24), initStr, timeStr, finalStr, deltaStr, diffStr, tasksStr));
            }
            sb.AppendLine(sepSingle);
            sb.AppendLine();

            // SECTION 2: LEVELS RESULTS
            sb.AppendLine("2. LEVEL RESULTS (LEVELS 1 TO 16)");
            sb.AppendLine(sepSingle);
            sb.AppendLine(string.Format(
                "{0,-38} | {1,-12} | {2,-10} | {3,9} | {4,8} | {5,8} | {6,8}",
                "Level", "Status", "Best Medal", "Accuracy", "Time", "Mistakes", "Attempts"));
            sb.AppendLine(sepSingle);

            foreach (var l in r.levelsSummary)
            {
                string status = l.completed ? "Completed" : (l.attemptsCount > 0 ? "Attempted" : "Pending");
                string acc = l.attemptsCount > 0 ? $"{l.bestAccuracyPercent:0}%" : "-";
                string time = l.attemptsCount > 0 ? $"{l.bestTimeSeconds:0.1}s" : "-";
                string err = l.attemptsCount > 0 ? l.bestMistakes.ToString() : "-";

                sb.AppendLine(string.Format(
                    "{0,-38} | {1,-12} | {2,-10} | {3,9} | {4,8} | {5,8} | {6,8}",
                    Truncate(l.levelName, 38), status, Truncate(l.bestMedal, 10), acc, time, err, l.attemptsCount));
            }
            sb.AppendLine(sepSingle);
            sb.AppendLine();

            // SECTION 3: CHRONOLOGICAL TASK PROGRESSION
            sb.AppendLine("3. STEP-BY-STEP TASK HISTORY");
            sb.AppendLine(sepSingle);
            if (r.taskHistory.Count == 0)
            {
                sb.AppendLine("  (No level tasks have been played yet)");
            }
            else
            {
                sb.AppendLine(string.Format(
                    "{0,-4} | {1,-8} | {2,-26} | {3,-18} | {4,-4} | {5,-9} | {6,7} | {7}",
                    "#", "Time", "Level", "Skill", "Diff", "Outcome", "Time", "Score Progression"));
                sb.AppendLine(sepSingle);

                foreach (var t in r.taskHistory)
                {
                    string outcome = t.success ? "SUCCESS" : "FAILED";
                    string changeSign = t.scoreChange >= 0 ? "+" : "";
                    string scoreEvolution = $"{t.scoreBefore:0}% -> {t.scoreAfter:0}% ({changeSign}{t.scoreChange:0})";

                    sb.AppendLine(string.Format(
                        "{0,-4} | {1,-8} | {2,-26} | {3,-18} | D{4,-3} | {5,-9} | {6,6:0.1}s | {7}",
                        t.stepIndex,
                        t.timestamp,
                        Truncate(t.levelName, 26),
                        Truncate(t.skillName, 18),
                        t.difficulty,
                        outcome,
                        t.completionTimeSeconds,
                        scoreEvolution));
                }
            }
            sb.AppendLine(sepSingle);

            return sb.ToString();
        }

        private static string BuildVisualHtmlReport(StudentFullPlaytestReport r)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html lang='en'><head><meta charset='UTF-8'>");
            sb.AppendLine($"<title>Playtest Report - {r.studentId}</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: 'Segoe UI', Arial, sans-serif; background: #f1f5f9; color: #0f172a; margin: 24px; }");
            sb.AppendLine(".container { max-width: 1050px; margin: auto; }");
            sb.AppendLine(".card { background: #fff; border-radius: 10px; padding: 20px 24px; margin-bottom: 20px; box-shadow: 0 2px 6px rgba(0,0,0,0.07); }");
            sb.AppendLine("h1 { margin: 0 0 8px 0; color: #1e293b; font-size: 24px; }");
            sb.AppendLine("h2 { margin: 0 0 14px 0; color: #1e293b; font-size: 18px; border-bottom: 2px solid #e2e8f0; padding-bottom: 8px; }");
            sb.AppendLine(".kpi-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 12px; margin-top: 14px; }");
            sb.AppendLine(".kpi { background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 12px; text-align: center; }");
            sb.AppendLine(".kpi .val { font-size: 22px; font-weight: bold; color: #2563eb; }");
            sb.AppendLine(".kpi .lbl { font-size: 12px; color: #64748b; margin-top: 4px; }");
            sb.AppendLine("table { width: 100%; border-collapse: collapse; font-size: 14px; }");
            sb.AppendLine("th { background: #f8fafc; color: #334155; text-align: left; padding: 10px; border-bottom: 2px solid #cbd5e1; }");
            sb.AppendLine("td { padding: 9px 10px; border-bottom: 1px solid #e2e8f0; }");
            sb.AppendLine("tr:nth-child(even) { background: #f8fafc; }");
            sb.AppendLine(".badge-ok { background: #dcfce7; color: #166534; padding: 3px 8px; border-radius: 12px; font-weight: bold; font-size: 12px; }");
            sb.AppendLine(".badge-pending { background: #f1f5f9; color: #64748b; padding: 3px 8px; border-radius: 12px; font-size: 12px; }");
            sb.AppendLine(".bar-bg { background: #e2e8f0; border-radius: 6px; height: 10px; width: 110px; display: inline-block; vertical-align: middle; margin-right: 8px; overflow: hidden; }");
            sb.AppendLine(".bar-fill { background: #16a34a; height: 100%; }");
            sb.AppendLine("</style></head><body><div class='container'>");

            // Header Card
            sb.AppendLine("<div class='card'>");
            sb.AppendLine($"<h1>Student Playtest Report: {r.studentId}</h1>");
            sb.AppendLine($"<div style='color:#64748b; font-size:13px;'>Session Start: {r.sessionStartTime} &nbsp;|&nbsp; Last Saved: {r.lastExportTime} &nbsp;|&nbsp; Game Language: {r.language}</div>");
            sb.AppendLine("<div class='kpi-grid'>");
            sb.AppendLine($"<div class='kpi'><div class='val'>{(r.initialAssessmentCompleted ? $"{r.initialAssessmentAverageScore:0.#}%" : "N/A")}</div><div class='lbl'>Initial Assessment Average</div></div>");
            sb.AppendLine($"<div class='kpi'><div class='val'>{r.finalSkillsAverageScore:0.#}%</div><div class='lbl'>Final Skills Average</div></div>");
            sb.AppendLine($"<div class='kpi'><div class='val'>{(r.averageImprovementDelta >= 0 ? "+" : "")}{r.averageImprovementDelta:0.#}%</div><div class='lbl'>Average Improvement</div></div>");
            sb.AppendLine($"<div class='kpi'><div class='val'>{r.totalLevelsCompleted} / {ProgressData.LevelOrder.Length}</div><div class='lbl'>Levels Completed</div></div>");
            sb.AppendLine("</div></div>");

            // Skills Table Card
            sb.AppendLine("<div class='card'><h2>1. Initial Assessment & Skill Progression</h2><table>");
            sb.AppendLine("<tr><th>Skill</th><th>Initial Score (Assessment)</th><th>Assessment Time / Mistakes</th><th>Final Score</th><th>Improvement</th><th>Final Difficulty</th><th>Tasks (Succeeded / Total)</th></tr>");
            foreach (var s in r.skillProgression)
            {
                string initCell = s.testedInAssessment ? $"<b>{s.initialAssessmentScore:0}%</b>" : "<span class='badge-pending'>Not tested</span>";
                string initMeta = s.testedInAssessment ? $"{s.initialAssessmentTimeSeconds:0.1}s ({s.initialAssessmentExtraAttempts} mistakes)" : "-";
                string deltaColor = s.improvementDelta > 0 ? "#16a34a" : (s.improvementDelta < 0 ? "#dc2626" : "#64748b");
                string deltaStr = $"<span style='color:{deltaColor};font-weight:bold;'>{(s.improvementDelta >= 0 ? "+" : "")}{s.improvementDelta:0}%</span>";
                string barHtml = $"<div class='bar-bg'><div class='bar-fill' style='width:{Mathf.Clamp(s.finalScore, 0, 100):0}%'></div></div><b>{s.finalScore:0}%</b>";
                string tasksStr = s.tasksAttempted > 0 ? $"{s.tasksSucceeded}/{s.tasksAttempted} ({s.taskSuccessRatePercent:0}%)" : "0";

                sb.AppendLine($"<tr><td><b>{s.skillName}</b></td><td>{initCell}</td><td>{initMeta}</td><td>{barHtml}</td><td>{deltaStr}</td><td>Tier {s.finalDifficulty}/5</td><td>{tasksStr}</td></tr>");
            }
            sb.AppendLine("</table></div>");

            // Levels Table Card
            sb.AppendLine("<div class='card'><h2>2. Level Results (Levels 1 to 16)</h2><table>");
            sb.AppendLine("<tr><th>Level</th><th>Status</th><th>Best Medal</th><th>Accuracy</th><th>Best Time</th><th>Mistakes</th><th>Attempts</th></tr>");
            foreach (var l in r.levelsSummary)
            {
                string statusBadge = l.completed
                    ? "<span class='badge-ok'>Completed</span>"
                    : "<span class='badge-pending'>Pending</span>";
                string acc = l.attemptsCount > 0 ? $"{l.bestAccuracyPercent:0}%" : "-";
                string time = l.attemptsCount > 0 ? $"{l.bestTimeSeconds:0.1}s" : "-";
                string err = l.attemptsCount > 0 ? l.bestMistakes.ToString() : "-";

                sb.AppendLine($"<tr><td><b>{l.levelName}</b></td><td>{statusBadge}</td><td>{l.bestMedal}</td><td>{acc}</td><td>{time}</td><td>{err}</td><td>{l.attemptsCount}</td></tr>");
            }
            sb.AppendLine("</table></div>");

            // Task History Card
            sb.AppendLine("<div class='card'><h2>3. Step-by-Step Task History</h2>");
            if (r.taskHistory.Count == 0)
            {
                sb.AppendLine("<p style='color:#64748b;'>No level tasks have been recorded yet.</p>");
            }
            else
            {
                sb.AppendLine("<table><tr><th>#</th><th>Time</th><th>Level</th><th>Skill</th><th>Difficulty</th><th>Outcome</th><th>Duration</th><th>Score (Before → After)</th></tr>");
                foreach (var t in r.taskHistory)
                {
                    string resBadge = t.success
                        ? "<span class='badge-ok'>SUCCESS</span>"
                        : "<span class='badge-pending' style='background:#fee2e2;color:#991b1b;'>FAILED</span>";
                    string sign = t.scoreChange >= 0 ? "+" : "";
                    sb.AppendLine($"<tr><td>{t.stepIndex}</td><td>{t.timestamp}</td><td>{t.levelName}</td><td>{t.skillName}</td><td>D{t.difficulty}</td><td>{resBadge}</td><td>{t.completionTimeSeconds:0.1}s</td><td>{t.scoreBefore:0}% → <b>{t.scoreAfter:0}%</b> ({sign}{t.scoreChange:0})</td></tr>");
                }
                sb.AppendLine("</table>");
            }
            sb.AppendLine("</div></div></body></html>");
            return sb.ToString();
        }

        private static string BuildSingleStudentExcelCsv(StudentFullPlaytestReport r)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("sep=;");
            sb.AppendLine($"STUDENT REPORT;{EscapeSemi(r.studentId)};Session Start;{EscapeSemi(r.sessionStartTime)};Last Saved;{EscapeSemi(r.lastExportTime)};Game Language;{EscapeSemi(r.language)}");
            sb.AppendLine();

            sb.AppendLine("1. SKILL PROGRESSION (INITIAL ASSESSMENT VS FINAL)");
            sb.AppendLine("Skill;Tested in Initial Assessment;Initial Score (%);Initial Test Time (s);Initial Test Mistakes;Final Score (%);Improvement (%);Final Difficulty Tier (1-5);Tasks Attempted;Tasks Succeeded;Tasks Failed;Task Success Rate (%)");
            foreach (var s in r.skillProgression)
            {
                sb.AppendLine(string.Join(";",
                    EscapeSemi(s.skillName),
                    s.testedInAssessment ? "Yes" : "No",
                    F(s.initialAssessmentScore),
                    F(s.initialAssessmentTimeSeconds),
                    s.initialAssessmentExtraAttempts,
                    F(s.finalScore),
                    F(s.improvementDelta),
                    s.finalDifficulty,
                    s.tasksAttempted,
                    s.tasksSucceeded,
                    s.tasksFailed,
                    F(s.taskSuccessRatePercent)
                ));
            }
            sb.AppendLine();

            sb.AppendLine("2. LEVEL RESULTS (LEVELS 1 TO 16)");
            sb.AppendLine("Level ID;Level Name;Completed;Attempts;Best Medal;Best Accuracy (%);Best Time (s);Mistakes");
            foreach (var l in r.levelsSummary)
            {
                sb.AppendLine(string.Join(";",
                    EscapeSemi(l.levelId),
                    EscapeSemi(l.levelName),
                    l.completed ? "Yes" : "No",
                    l.attemptsCount,
                    EscapeSemi(l.bestMedal),
                    F(l.bestAccuracyPercent),
                    F(l.bestTimeSeconds),
                    l.bestMistakes
                ));
            }
            sb.AppendLine();

            sb.AppendLine("3. STEP-BY-STEP TASK PROGRESSION");
            sb.AppendLine("Step;Time;Level;Skill;Difficulty;Outcome;Extra Mistakes;Duration (s);Score Before (%);Score Change;Score After (%)");
            foreach (var t in r.taskHistory)
            {
                sb.AppendLine(string.Join(";",
                    t.stepIndex,
                    EscapeSemi(t.timestamp),
                    EscapeSemi(t.levelName),
                    EscapeSemi(t.skillName),
                    $"D{t.difficulty}",
                    t.success ? "Success" : "Failed",
                    t.extraMistakes,
                    F(t.completionTimeSeconds),
                    F(t.scoreBefore),
                    F(t.scoreChange),
                    F(t.scoreAfter)
                ));
            }

            return sb.ToString();
        }

        private static void UpdateMasterStudentsCsv(string masterCsvPath, StudentFullPlaytestReport r, Encoding encoding)
        {
            List<string> headers = new List<string>
            {
                "Student_ID",
                "Last_Saved_Time",
                "Game_Language",
                "Initial_Assessment_Completed",
                "Initial_Assessment_Avg (%)",
                "Final_Skills_Avg (%)",
                "Avg_Improvement (%)",
                "Levels_Completed (of 16)",
                "Total_Tasks_Attempted",
                "Total_Tasks_Succeeded",
                "MoveCursor (Init -> Final)",
                "Hover (Init -> Final)",
                "SingleClick (Init -> Final)",
                "DoubleClick (Init -> Final)",
                "RightClick (Init -> Final)",
                "ClickHold (Init -> Final)",
                "DragDrop (Init -> Final)"
            };

            for (int i = 1; i <= 16; i++)
            {
                headers.Add($"Level_{i}_Result");
            }

            List<string> rowValues = new List<string>
            {
                EscapeSemi(r.studentId),
                EscapeSemi(r.lastExportTime),
                EscapeSemi(r.language),
                r.initialAssessmentCompleted ? "Yes" : "No",
                F(r.initialAssessmentAverageScore),
                F(r.finalSkillsAverageScore),
                F(r.averageImprovementDelta),
                r.totalLevelsCompleted.ToString(CultureInfo.InvariantCulture),
                r.totalTasksAttempted.ToString(CultureInfo.InvariantCulture),
                r.totalTasksSucceeded.ToString(CultureInfo.InvariantCulture)
            };

            Dictionary<string, SkillProgressionExportItem> skillMap = new Dictionary<string, SkillProgressionExportItem>();
            foreach (var s in r.skillProgression) skillMap[s.skillId] = s;

            foreach (string skillId in AllSkillIds)
            {
                if (skillMap.TryGetValue(skillId, out var s))
                {
                    string deltaSign = s.improvementDelta >= 0 ? "+" : "";
                    rowValues.Add(EscapeSemi($"{s.initialAssessmentScore:0}% -> {s.finalScore:0}% ({deltaSign}{s.improvementDelta:0}%)"));
                }
                else
                {
                    rowValues.Add("0% -> 0%");
                }
            }

            Dictionary<string, LevelSummaryExportItem> levelMap = new Dictionary<string, LevelSummaryExportItem>();
            foreach (var l in r.levelsSummary) levelMap[l.levelId] = l;

            foreach (string levelId in ProgressData.LevelOrder)
            {
                if (levelMap.TryGetValue(levelId, out var l) && (l.completed || l.attemptsCount > 0))
                {
                    rowValues.Add(EscapeSemi($"{l.bestMedal} | {l.bestAccuracyPercent:0}% | {l.bestTimeSeconds:0.1}s ({l.bestMistakes} err)"));
                }
                else
                {
                    rowValues.Add("Not played");
                }
            }

            string sepLine = "sep=;";
            string headerLine = string.Join(";", headers);
            string studentRowLine = string.Join(";", rowValues);
            string studentPrefix = EscapeSemi(r.studentId) + ";";

            List<string> lines = new List<string> { sepLine, headerLine };
            if (File.Exists(masterCsvPath))
            {
                string[] existing = File.ReadAllLines(masterCsvPath, encoding);
                bool replaced = false;
                for (int i = 0; i < existing.Length; i++)
                {
                    string line = existing[i];
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (line.StartsWith("sep=", StringComparison.OrdinalIgnoreCase)) continue;
                    if (line.StartsWith("Student_ID;", StringComparison.OrdinalIgnoreCase) ||
                        line.StartsWith("Alumno_ID;", StringComparison.OrdinalIgnoreCase) ||
                        line.StartsWith("StudentId,", StringComparison.OrdinalIgnoreCase)) continue;

                    if (line.StartsWith(studentPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        lines.Add(studentRowLine);
                        replaced = true;
                    }
                    else
                    {
                        lines.Add(line);
                    }
                }
                if (!replaced)
                {
                    lines.Add(studentRowLine);
                }
            }
            else
            {
                lines.Add(studentRowLine);
            }

            File.WriteAllLines(masterCsvPath, lines, encoding);
        }

        private static int ScoreToDifficultyTier(float score)
        {
            if (score >= 80f) return 5;
            if (score >= 60f) return 4;
            if (score >= 40f) return 3;
            if (score >= 20f) return 2;
            return 1;
        }

        private static string GetEnglishSkillName(string skillId)
        {
            switch (skillId)
            {
                case "mouse_move": return "Move Cursor";
                case "hover": return "Hover";
                case "click": return "Single Click";
                case "double_click": return "Double Click";
                case "right_click": return "Right Click";
                case "click_hold": return "Click and Hold";
                case "drag_drop": return "Drag and Drop";
                default: return skillId;
            }
        }

        private static string GetEnglishLevelName(string levelId)
        {
            if (string.IsNullOrEmpty(levelId)) return string.Empty;
            switch (levelId.Trim().ToLowerInvariant())
            {
                case "assessment": return "Initial Assessment";
                case "level1": return "Level 1: Move";
                case "level2": return "Level 2: Hover";
                case "level3": return "Level 3: Click";
                case "level4": return "Level 4: Adaptive";
                case "level5": return "Level 5: Double Click";
                case "level6": return "Level 6: Right Click";
                case "level7": return "Level 7: Click & Hold";
                case "level8": return "Level 8: Drag & Drop";
                case "level9": return "Level 9: Mixed Skills Consolidation";
                case "level10": return "Level 10: Context Menu";
                case "level11": return "Level 11: Open Notepad & Typing";
                case "level12": return "Level 12: Copy, Paste & Cut";
                case "level13": return "Level 13: Undo Shortcut";
                case "level14": return "Level 14: Save Shortcut";
                case "level15": return "Level 15: Notepad Evaluation";
                case "level16": return "Level 16: Paint Usage";
                default: return levelId;
            }
        }

        private static string SanitizeStudentId(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            string trimmed = input.Trim();
            StringBuilder sb = new StringBuilder(trimmed.Length);
            foreach (char c in trimmed)
            {
                if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
                {
                    sb.Append(c);
                }
                else if (char.IsWhiteSpace(c))
                {
                    sb.Append('_');
                }
            }
            return sb.ToString();
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Length <= max ? s : s.Substring(0, max - 1) + "…";
        }

        private static string EscapeSemi(string val)
        {
            if (string.IsNullOrEmpty(val)) return string.Empty;
            if (val.Contains(";") || val.Contains("\"") || val.Contains("\n") || val.Contains("\r"))
            {
                return "\"" + val.Replace("\"", "\"\"") + "\"";
            }
            return val;
        }

        private static string F(float val)
        {
            return val.ToString("0.#", CultureInfo.InvariantCulture);
        }
    }
}
