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
        public string menu_press_space_to_start = "Press SPACE or click Play to start";

        // Assessment Intro Modal
        public string assessment_intro_title = "Welcome to the Skills Test!";
        public string assessment_intro_description = "Before starting, we will do a few quick tests to see how you use the mouse (moving, clicking, dragging...). Don't worry about making mistakes or rushing, take all the time you need!";
        public string assessment_intro_prompt = "Press SPACE or click to start";

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
        public string celebration_title = "<b>LEVEL COMPLETED!</b>";
        public string celebration_subtitle_default = "Great Job!";
        public string celebration_stats_format = "Accuracy: <b>{0:F0}%</b>   |   Time: <b>{1:F1}s</b>   |   Mistakes: <color={2}><b>{3}</b></color>";
        public string celebration_points_format = "You got <b>{0}/{1}</b> points";
        public string celebration_guided_criteria_format = "Gold Target: <= {0:F0}s & 0 mistakes (-50 pts/mistake)";
        public string celebration_continue_button = "<b>CONTINUE</b>";
        public string celebration_continuing_format = "<b>CONTINUING ({0}s)...</b>";
        public string celebration_medal_badge_format = "<b>{0} MEDAL</b>";
        public string celebration_completed_badge = "<b>COMPLETED</b>";

        // ScoresApp
        public string scores_window_title = "Scores & Achievements";
        public string scores_header_title = "<b>Hall of Fame & Level Records</b>";
        public string scores_subtitle = "Track your progress, medals, accuracy, and best times across all 16 levels";
        public string scores_loading = "Loading statistics...";
        public string scores_refresh_button = "Refresh Records";
        public string scores_reset_button = "Reset Records";
        public string scores_summary_format = "<color=#FFB800><b>Gold: {0}</b></color>    <color=#8E9CA8><b>Silver: {1}</b></color>    <color=#C87D3B><b>Bronze: {2}</b></color>   |   Completed: <b>{3}/{4}</b>   |   Avg Accuracy: <b>{5:F0}%</b>";
        public string scores_stat_completed = "COMPLETED";
        public string scores_stat_gold = "GOLD";
        public string scores_stat_silver = "SILVER";
        public string scores_stat_bronze = "BRONZE";
        public string scores_tab_all = "All Levels (16)";
        public string scores_tab_mouse = "Mouse Skills (1-9)";
        public string scores_tab_desktop = "Desktop & Apps (10-16)";
        public string scores_tab_completed = "Completed Only";
        public string scores_btn_page_up = "^ Page Up";
        public string scores_btn_page_down = "v Page Down";
        public string scores_empty_filter = "No levels match this filter yet. Play some levels to earn medals!";
        public string scores_cat_mouse = "Mouse & Cursor Practice";
        public string scores_cat_desktop = "Virtual Desktop & Apps";
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
        public string stats_gold_label = "Gold";
        public string stats_silver_label = "Silver";
        public string stats_bronze_label = "Bronze";
        public string stats_levels_label = "Levels";
        public string skill_mouse_move = "Move Cursor";
        public string skill_hover = "Hover";
        public string skill_click = "Single Click";
        public string skill_double_click = "Double Click";
        public string skill_right_click = "Right Click";
        public string skill_click_hold = "Click and Hold";
        public string skill_drag_drop = "Drag and Drop";

        // Playtest Student ID & Data Export
        public string playtest_student_id_label = "Student ID:";
        public string playtest_btn_export = "Export Data";
        public string playtest_btn_auto_download_on = "Auto Download: ON";
        public string playtest_btn_auto_download_off = "Auto Download: OFF";
        public string playtest_auto_download_enabled_toast = "Auto download enabled (data will save automatically)";
        public string playtest_auto_download_disabled_toast = "Auto download disabled (data will only save when clicking Export)";
        public string playtest_export_success = "Exported {0} to Desktop/ComputerLearner_PlaytestData";
        public string playtest_reset_success = "Progress reset! Next student ID: {0}";

        // Kid-Friendly Student Menu & Welcome Card
        public string menu_welcome_title = "Welcome to Your Computer!";
        public string menu_greeting_format = "Hi, {0}!";
        public string menu_name_label = "Your Name:";
        public string menu_name_placeholder = "Type your name...";
        public string menu_btn_join_class = "Join Class";
        public string menu_language_label = "Language";
        public string menu_connected_status_format = "Connected to {0}'s class";
        public string menu_btn_leave_class = "Leave Class";

        // Teacher Mode Gate, PIN & Panel
        public string teacher_btn_locked = "Teacher";
        public string teacher_btn_unlocked = "Teacher (Unlocked)";
        public string teacher_pin_title = "Teacher Mode";
        public string teacher_pin_prompt = "Enter the 4-digit teacher PIN:";
        public string teacher_pin_wrong = "Oops! Wrong PIN. Try again.";
        public string teacher_pin_backspace = "Del";
        public string teacher_pin_clear = "Clear";
        public string teacher_panel_title = "Teacher Control Window";
        public string teacher_panel_subtitle = "Classroom setup, learning paths & student tools";
        public string teacher_btn_lock = "Lock & Student Mode";
        public string teacher_section_student_controls = "Student & Progress Tools";
        public string teacher_section_class_network = "Classroom & Dashboard";
        public string teacher_section_pin_config = "Teacher PIN";
        public string teacher_pin_new_placeholder = "New 4 digits...";
        public string teacher_pin_save_btn = "Save PIN";
        public string teacher_pin_invalid_length = "Enter exactly 4 numbers.";
        public string teacher_pin_updated_format = "PIN updated to {0}";

        // Learning Path Selector
        public string path_section_title = "Learning Path & Start Flow";
        public string path_beginner_title = "Beginner";
        public string path_intermediate_title = "Intermediate";
        public string path_advanced_title = "Advanced";
        public string path_assess_on = "Initial Assessment: ON";
        public string path_assess_off = "Initial Assessment: OFF";
        public string path_locked_by_class = "Controlled by Teacher Host ({0})";

        // Classroom Network & Student Join
        public string lan_join_modal_title = "Join Your Class";
        public string lan_join_searching = "Looking for your teacher's class...";
        public string lan_join_no_hosts = "No classrooms found yet. Ask your teacher to start the class!";
        public string lan_join_host_entry_format = "{0}'s Class ({1})";
        public string lan_join_btn_connect = "Join";
        public string lan_join_connecting = "Joining...";
        public string lan_join_manual_toggle = "Use teacher IP address instead";
        public string lan_join_manual_placeholder = "Teacher IP (e.g. 192.168.1.50)";
        public string lan_join_ip_empty = "Please type the teacher IP.";
        public string lan_connected_banner_format = "Connected to class: {0}";
        public string lan_teacher_open_dashboard = "Open Teacher Dashboard";
        public string lan_teacher_host_offline = "Class server: Offline";
        public string lan_teacher_host_online_format = "Class active - {0} students - {1}:{2}";

        // Teacher Dashboard
        public string dash_title = "Classroom Dashboard";
        public string dash_server_off = "Server: Offline";
        public string dash_server_on_format = "IP {0}:{1} - {2} students";
        public string dash_btn_start_server = "Start Class Server";
        public string dash_btn_stop_server = "Stop Server";
        public string dash_btn_clear_offline = "Clear Offline";
        public string dash_batch_label = "Class Default:";
        public string dash_assess_on = "Assess: ON";
        public string dash_assess_off = "Assess: OFF";
        public string dash_btn_apply_all = "Apply to All";
        public string dash_btn_launch_all = "Launch All";
        public string dash_students_header = "Connected Students";
        public string dash_empty_students = "Waiting for students to join...\n\nTell students to open \"Join Class\" in the menu.";
        public string dash_inspector_placeholder = "Select a student to inspect progress and skills.";
        public string dash_status_online = "Online";
        public string dash_status_offline = "Offline";
        public string dash_btn_start_student = "Start";
        public string dash_btn_reset_student = "Reset";
        public string dash_kpi_progress = "Levels Completed";
        public string dash_kpi_medals = "Medals Earned";
        public string dash_kpi_score = "Total Points";

        // Assessment Header Badges
        public string assessment_badge_task_format = "Game {0} of {1}";
        public string assessment_btn_skip = "Skip Game";
        public string assessment_btn_menu = "Menu";

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
