using UnityEngine;
using System.Collections.Generic;

namespace ComputerLearning
{
    [System.Serializable]
    public class TutorialDialogs
    {
        // First Desktop Tutorial
        public string first_welcome = "Welcome to the computer! Let's learn how to use it.";
        public string first_default = "Watch how I open this folder!";
        public string first_assessment = "Let's start with a quick assessment! Double click the Assessment icon!";
        public string first_move = "Let's start by moving the mouse! Watch how I open the Move app.";
        public string first_hover = "Now let's practice Hovering! Keep an eye on how I open it.";
        public string first_click = "Time to Click! I'll open this app so you can try.";
        public string first_adaptive = "Let's mix what we've learned in a fun challenge! Watch closely.";
        public string first_double_click = "Almost done with the basics! One last trick: Double Click!";

        // Second Desktop Tutorial
        public string second_welcome = "You've completed all the computer basics! You're ready to use the desktop freely.";
        public string second_default = "Double click here to start the next practice!";
        public string second_right_click = "Now let's learn some advanced interactions! Double-click the Right Click app to start.";
        public string second_click_hold = "Great! Now double-click the Click & Hold app.";
        public string second_drag_drop = "Awesome! Let's try Drag & Drop. Open it up!";
        public string second_mixed = "You're a pro! Time for the final challenge: Mix everything together!";
        public string second_closed_early = "Oops! You closed it without finishing! Double click to try again.";
        public string tutorial_double_click_reminder = "Double-click quickly with the left mouse button on the icon to open it!";

        // Level Runner
        public string level_completed = "Level Completed! Great job!";
        public string level_ready_prompt = "Read carefully and click here\nwhen you are ready!";
        public string level_start_button = "START!";
        public string level_celebration_format = "Awesome job! You earned the {0} Medal!\nAccuracy: {1:F0}%  Time: {2:F1}s";
        public string intro_move = "Welcome to your very first app!\nLet's practice moving the mouse. Just hover over the dirt spots to clean them!";
        public string intro_hover = "Awesome job so far!\nNow things will move a bit faster. Keep those moves sharp!";
        public string intro_click = "Ready for some clicking?\nClick on the targets as soon as they appear!";
        public string intro_adaptive = "Time for a challenge!\nLet's see what you've learned so far.";
        public string intro_double_click = "One more trick!\nDouble click quickly to clear the targets!";
        public string intro_right_click = "Let's learn how to use the right button on your mouse!";
        public string intro_click_hold = "Sometimes you need to hold the button down. Give it a try!";
        public string intro_drag_drop = "Click, hold, and move to drag items around!";
        public string intro_mixed = "The ultimate test!\nUse everything you've learned so far.";
        public string intro_default = "Get ready! ";

        // Adaptive Session Starter
        public string adaptive_welcome = "Welcome to Level 4!\nKeep practicing to improve your skills.";
        public string adaptive_explanation = "I will give you tasks based on what you need to learn.\nGood luck!";
        public string adaptive_done = "Great practice!\nYou can click the 'X' to close this window now.";

        // Virtual Desktop Intro
        public string virtual_welcome = "Welcome to your full Virtual Desktop! Here you can explore and practice freely.";
        public string virtual_levels = "Double-click the Levels folder anytime to challenge yourself with more tasks.";
        public string virtual_apps = "You can also open fun apps like Paint, Notepad, and Minigames to create and play!";
        public string virtual_start_and_pet = "Use the Start button on the bottom-left for all options, and click me in the corner whenever you want tips!";
    }
}
