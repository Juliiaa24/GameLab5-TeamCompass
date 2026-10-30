using System;
using UnityEngine;

namespace ComputerLearning
{
    [Serializable]
    public class UIDialogs
    {
        // Main Menu Buttons (Menu.unity)
        public string menu_btn_play = "Play";
        public string menu_btn_virtual_desktop = "Virtual Desktop";
        public string menu_btn_stats = "Stats";
        public string menu_btn_reset_progress = "Reset Progress";
        public string menu_btn_exit = "Exit";

        // Desktop Start Menu (StartMenuController)
        public string start_menu_btn_start = "Start";
        public string start_menu_header = "My Computer";
        public string start_menu_file_explorer = "File Explorer";
        public string start_menu_notepad = "Notepad";
        public string start_menu_paint = "Paint";
        public string start_menu_minigames = "Minigames Hub";
        public string start_menu_stats = "Skills Report";
        public string start_menu_exit = "Exit to Menu";

        // Minigames Hub (MinigamesHubApp)
        public string minigames_btn_play = "Play!";
        public string minigames_targets = "Targets & Clicks";
        public string minigames_hover = "Tracking & Hover";
        public string minigames_double_click = "Fast Double Click";
        public string minigames_adaptive = "Adaptive Practice";

        // Level Display Names (L_Assessment & L_Level1..L_Level16)
        public string level_display_assessment = "Initial Assessment Test";
        public string level_display_1 = "Level 1: Move";
        public string level_display_2 = "Level 2: Hover";
        public string level_display_3 = "Level 3: Click";
        public string level_display_4 = "Level 4: Adaptive";
        public string level_display_5 = "Level 5: Double Click";
        public string level_display_6 = "Level 6: Right Click";
        public string level_display_7 = "Level 7: Click & Hold";
        public string level_display_8 = "Level 8: Drag & Drop";
        public string level_display_9 = "Level 9: Mixed Skills Consolidation";
        public string level_display_10 = "Level 10: Context Menu";
        public string level_display_11 = "Level 11: Open Notepad & Typing";
        public string level_display_12 = "Level 12: Copy, Paste & Cut";
        public string level_display_13 = "Level 13: Undo Shortcut";
        public string level_display_14 = "Level 14: Save Shortcut";
        public string level_display_15 = "Level 15: Notepad Evaluation";
        public string level_display_16 = "Level 16: Paint Usage";

        // Level Start Screen Instructions (L_Assessment & L_Level1..L_Level16)
        public string level_instruction_assessment = "Welcome to the Assessment!\nWe are going to do a few quick exercises to see what you already know how to do.\nDon't worry if it's hard!";
        public string level_instruction_1 = "Move your mouse over the brown dust spots to clean the window!";
        public string level_instruction_2 = "HOVER your mouse over the targets\nuntil they are full";
        public string level_instruction_3 = "LEFT CLICK on the red targets\nto earn practice points.";
        public string level_instruction_4 = "Welcome to the Adaptive Challenge!\nHere you will practice everything you've learned.";
        public string level_instruction_5 = "Double Click on the targets to practice opening files and folders!";
        public string level_instruction_6 = "Right Click on the targets to practice opening context menus!";
        public string level_instruction_7 = "Click and hold the button down until it fills completely!";
        public string level_instruction_8 = "Click and drag items to their correct drop zones!";
        public string level_instruction_9 = "Are you ready for the final challenge?";
        public string level_instruction_10 = "Learn how to use the desktop Context Menu!\nRight-click on the desktop to explore options.";
        public string level_instruction_11 = "Learn how to open Notepad and type phrases on your keyboard!";
        public string level_instruction_12 = "Learn keyboard shortcuts!\nCtrl + C to copy, Ctrl + V to paste, and Ctrl + X to cut.";
        public string level_instruction_13 = "Learn how to undo mistakes with Ctrl + Z!";
        public string level_instruction_14 = "Learn how to safely save files with Ctrl + S!";
        public string level_instruction_15 = "Complete test: type, copy, cut, undo, and save your document in Notepad!";
        public string level_instruction_16 = "Open Paint, draw artwork, undo with Ctrl + Z, and save with Ctrl + S!";

        // Medals
        public string medal_gold = "Gold";
        public string medal_silver = "Silver";
        public string medal_bronze = "Bronze";
        public string medal_none = "None";

        // LevelCelebrationModal
        public string celebration_title = "<b>★ LEVEL COMPLETED! ★</b>";
        public string celebration_subtitle_default = "Great Job!";
        public string celebration_stats_format = "Accuracy: <b>{0:F0}%</b>   |   Time: <b>{1:F1}s</b>   |   Mistakes: <color={2}><b>{3}</b></color>";
        public string celebration_continue_button = "<b>CONTINUE</b>";

        // ScoresApp
        public string scores_window_title = "Scores & Achievements";
        public string scores_header_title = "<b>Hall of Fame & Level Records</b>";
        public string scores_loading = "Loading statistics...";
        public string scores_refresh_button = "Refresh Records";
        public string scores_reset_button = "Reset Records";
        public string scores_summary_format = "<color=#FFB800>● <b>Gold: {0}</b></color>    <color=#8E9CA8>● <b>Silver: {1}</b></color>    <color=#C87D3B>● <b>Bronze: {2}</b></color>   |   Completed: <b>{3}/{4}</b>   |   Avg Accuracy: <b>{5:F0}%</b>";
        public string scores_col_level = "LEVEL NAME";
        public string scores_col_status = "STATUS";
        public string scores_col_medal = "MEDAL EARNED";
        public string scores_col_accuracy = "ACCURACY";
        public string scores_col_time = "BEST TIME";
        public string scores_col_tries = "TRIES";
        public string scores_status_completed = "Completed";
        public string scores_status_unlocked = "Unlocked";
        public string scores_status_locked = "Locked";
        public string scores_reset_modal_title = "Reset Scores & Records";
        public string scores_reset_modal_prompt = "Type RESET to confirm clearing all records:";
        public string scores_reset_modal_confirm = "Confirm Reset";
        public string[] canonical_level_names = new string[]
        {
            "Level 1: Mouse Movement",
            "Level 2: Precision Clicks",
            "Level 3: Hover & Hold",
            "Level 4: Drag & Drop",
            "Level 5: Double Click",
            "Level 6: Right Click",
            "Level 7: Hitbox Challenge",
            "Level 8: Moving Targets",
            "Level 9: Mixed Skills",
            "Level 10: Context Menu",
            "Level 11: Open Notepad & Typing",
            "Level 12: Copy, Paste & Cut",
            "Level 13: Undo Shortcut",
            "Level 14: Save Shortcut",
            "Level 15: Notepad Evaluation",
            "Level 16: Paint Usage"
        };

        // StatsSceneController
        public string stats_page_title = "Assessment & Skill Statistics";
        public string stats_btn_back = "Back to Menu";
        public string stats_btn_retake = "Retake Assessment";
        public string stats_card_assessment_title = "Initial Assessment History";
        public string stats_card_assessment_subtitle = "Most Recent Diagnostic Test Records";
        public string stats_card_skills_title = "Current Skill Mastery";
        public string stats_card_skills_subtitle = "Live Competency Progress Bars";
        public string stats_empty_state = "No assessment records found yet.\n\nPlay from the Main Menu or click 'Retake Assessment' to record your first diagnostic evaluation!";
        public string stats_recent_label = "Recent";
        public string stats_summary_format = "Completed at: {0} | Tasks: {1}/{2}";
        public string stats_average_format = "Average: {0:0}%";
        public string stats_level_mastered = "Mastered";
        public string stats_level_proficient = "Proficient";
        public string stats_level_practicing = "Practicing";
        public string skill_mouse_move = "Move Cursor";
        public string skill_hover = "Hover";
        public string skill_click = "Single Click";
        public string skill_double_click = "Double Click";
        public string skill_right_click = "Right Click";
        public string skill_click_hold = "Click and Hold";
        public string skill_drag_drop = "Drag and Drop";

        private static UIDialogs cached;
        private static string cachedLang;

        public static UIDialogs Current
        {
            get
            {
                string lang = LocalizationManager.CurrentLanguage;
                if (cached == null || cachedLang != lang)
                {
                    cachedLang = lang;
                    TextAsset json = LocalizationManager.LoadLocalizedResource("UIDialogs");
                    cached = json != null ? JsonUtility.FromJson<UIDialogs>(json.text) : null;
                    if (cached == null) cached = new UIDialogs();
                }
                return cached;
            }
        }

        public string GetLevelDisplayName(string levelId, string fallback = null)
        {
            if (string.IsNullOrEmpty(levelId)) return fallback ?? string.Empty;
            switch (levelId.Trim().ToLowerInvariant())
            {
                case "assessment": return level_display_assessment;
                case "level1": return level_display_1;
                case "level2": return level_display_2;
                case "level3": return level_display_3;
                case "level4": return level_display_4;
                case "level5": return level_display_5;
                case "level6": return level_display_6;
                case "level7": return level_display_7;
                case "level8": return level_display_8;
                case "level9": return level_display_9;
                case "level10": return level_display_10;
                case "level11": return level_display_11;
                case "level12": return level_display_12;
                case "level13": return level_display_13;
                case "level14": return level_display_14;
                case "level15": return level_display_15;
                case "level16": return level_display_16;
                default: return !string.IsNullOrEmpty(fallback) ? fallback : levelId;
            }
        }

        public string GetLevelInstructionText(string levelId, string fallback = null)
        {
            if (string.IsNullOrEmpty(levelId)) return fallback ?? string.Empty;
            switch (levelId.Trim().ToLowerInvariant())
            {
                case "assessment": return level_instruction_assessment;
                case "level1": return level_instruction_1;
                case "level2": return level_instruction_2;
                case "level3": return level_instruction_3;
                case "level4": return level_instruction_4;
                case "level5": return level_instruction_5;
                case "level6": return level_instruction_6;
                case "level7": return level_instruction_7;
                case "level8": return level_instruction_8;
                case "level9": return level_instruction_9;
                case "level10": return level_instruction_10;
                case "level11": return level_instruction_11;
                case "level12": return level_instruction_12;
                case "level13": return level_instruction_13;
                case "level14": return level_instruction_14;
                case "level15": return level_instruction_15;
                case "level16": return level_instruction_16;
                default: return fallback ?? string.Empty;
            }
        }
    }
}
