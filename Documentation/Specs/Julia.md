GAME LAB PROJECT CONTEXT

IMPORTANT INSTRUCTIONS FOR THE AI

Treat all the information in this document as established context about my current Game Lab project unless I explicitly tell you that something has changed.

Do not assume that an architectural decision should be replaced just because another solution could theoretically be cleaner. Prefer adapting to the existing project structure unless there is a strong technical reason not to.

When suggesting code changes:

* Try to preserve the current architecture.
* Avoid unnecessarily rewriting systems that already work.
* Explain why a change is needed.
* Prefer reusable and modular Unity components.
* Keep the code understandable for a university-level game development project.
* Use the existing namespace and code conventions whenever possible.
* When I ask how something works, explain the underlying C# / Unity concept, not only what the code does.
* When debugging, first reason about the current implementation instead of proposing a completely different system.

I am studying Game Development and this project is part of Game Lab during an Erasmus semester in Norway.

==================================================

1. PROJECT OVERVIEW
    ==================================================

Project name / working name:
Computer Learning

Engine:
Unity 6
Unity version referenced during development:
Unity 6000.5

Main objective:
Create a virtual desktop / computer environment designed to teach children basic computer usage through interactive tasks and minigames.

The player interacts with something resembling a desktop operating system.

Important elements include:

* Desktop icons.
* Application windows.
* Taskbar.
* Window management.
* Mouse interactions.
* Keyboard interactions.
* Tasks / learning exercises.
* Skills.
* Levels.
* Minigames integrated inside application windows.

The target audience is children, so interaction should be:

* Clear.
* Visual.
* Responsive.
* Easy to understand.
* Accompanied by frequent feedback.

The project should behave similarly to a simplified desktop operating system while remaining appropriate for a learning game.

==================================================
2. UI SYSTEM DECISION

We discussed whether to use:

* Unity UI Toolkit.
* Unity Canvas / uGUI.

Current decision:
Use Unity Canvas / uGUI for the virtual desktop.

Reasoning:
The project relies heavily on:

* Draggable windows.
* Resizable windows.
* Desktop icons.
* Prefabs.
* Pointer interaction.
* Runtime UI manipulation.
* Windows containing different pieces of gameplay.

Canvas is considered more practical for this project because windows and icons can be created as reusable prefabs and manipulated directly through RectTransforms.

The goal is not to recreate a full operating system UI framework but to create a convincing and functional desktop metaphor inside Unity.

==================================================
3. RESOLUTION / CANVAS

The virtual desktop needs to work across different screen resolutions.

A reference resolution of approximately:

1920 x 1080

has been used.

The Canvas uses Unityâ€™s Canvas Scaler so the interface adapts to different resolutions.

Important:
Some window resizing logic previously broke when the Canvas resolution / scaling configuration was changed.

When implementing pointer-based UI movement or resizing, account for Canvas scaling correctly.

Avoid relying directly on raw Screen coordinates when local RectTransform coordinates are required.

RectTransformUtility functions may be necessary when converting pointer positions into coordinates relative to the correct parent RectTransform.

==================================================
4. NAMESPACE AND SCRIPT STYLE

Main namespace:

ComputerLearning

Scripts should generally use:

namespace ComputerLearning
{
â€¦
}

There is an existing C# script template used for the project.

The template follows a structure similar to:

/**

* Author:
* Date:
* Description:
    */

using â€¦

namespace ComputerLearning
{
/// 
///
/// 
public class Example : MonoBehaviour
{

    #region Public Variables
    // Public Constant Variables
    // Public Component References
    // Public Variables
    #endregion
    #region Private Variables
    // Private Constant Variables
    // Private Component References
    // Private Variables
    #endregion
    #region Unity Methods
    ...
    #endregion
    #region Public Methods
    ...
    #endregion
    #region Private Methods
    ...
    #endregion
}

}

When generating complete scripts for the project, preserve this general style when practical.

==================================================
5. WINDOW SYSTEM

A central part of the project is a reusable window system.

Windows should behave similarly to normal desktop windows.

Required features include:

* Open window.
* Close window.
* Drag window.
* Resize window.
* Bring window to front when clicked.
* Maximize window.
* Restore window.
* Remember previous size before maximizing.
* Remember previous position before maximizing.
* Contain arbitrary content / minigames.
* Integrate with the taskbar.

==================================================
6. WINDOW CLASS

There is a Window component representing a desktop window.

Responsibilities include things such as:

* Window state.
* Window content.
* Opening / closing.
* Maximize.
* Restore.
* Interacting with WindowManager.
* Potentially exposing callbacks/events when its state changes.

There has been a SetContent-type system intended to place content inside the window.

The content should stretch correctly with the window.

Typical RectTransform full-stretch configuration:

anchorMin = Vector2.zero
anchorMax = Vector2.one
offsetMin = Vector2.zero
offsetMax = Vector2.zero

However, remember:
This stretches relative to the objectâ€™s parent, not automatically relative to the Canvas.

Therefore, content should be parented to the intended window content container before applying full-stretch anchors.

==================================================
7. WINDOW MANAGER

There is a WindowManager responsible for coordinating windows.

Conceptually it manages things such as:

* Existing open windows.
* Window ordering.
* Bringing windows to the front.
* Possibly spawning or registering windows.
* Connecting windows with the taskbar.
* Tracking window events.

Properties discussed include patterns such as:

public IReadOnlyList Windows => windows;

and:

public RectTransform WindowsContainer => windowsContainer;

These are read-only public properties that expose private data.

The expression-bodied syntax:

=>

is just a shorter form of a getter.

Example:

public IReadOnlyList Windows => windows;

is approximately equivalent to:

public IReadOnlyList Windows
{
get
{
return windows;
}
}

The purpose of IReadOnlyList is to let other systems inspect the list without allowing them to directly modify the collection through that reference.

==================================================
8. WINDOW EVENTS / CALLBACKS

WindowManager listens to changes in windows using C# callbacks/events.

I have specifically been learning how this works.

When explaining the system, explain:

* Delegates.
* Actions.
* Events.
* Subscribing with +=.
* Unsubscribing with -=.
* Invoking events.
* Why events decouple systems.

Example conceptual flow:

Window changes state
â†’ Window invokes an event
â†’ WindowManager subscribed previously
â†’ WindowManager receives callback
â†’ WindowManager updates taskbar or window state

Avoid assuming I already fully understand event-driven programming.

If relevant, explain the lifecycle of the event from subscription to invocation.

==================================================
9. CLOSING WINDOWS

There has been discussion about whether windows are destroyed when closed or simply marked as closed.

At one point code included a condition similar to:

if (existing != null && !existing.IsClosed)

If a window is actually destroyed when closed, then maintaining IsClosed may be unnecessary depending on the architecture.

When reasoning about this, distinguish between:

A) Closing = destroying GameObject.

B) Closing = disabling / hiding / preserving GameObject.

C) Closing = changing a logical state while keeping the object.

Do not retain redundant state merely because older code used it.

==================================================
10. WINDOW DRAGGING

There is a script called:

WindowDrag

It uses Unity EventSystem interfaces such as:

IDragHandler
IPointerClickHandler

and potentially:
IBeginDragHandler

The top bar of the window is used to drag the window.

Important desired behavior:
When the user clicks / starts interacting with a window, the window should immediately come to the front.

There was previously a problem where the window only came to the front after releasing the mouse.

Bring-to-front should ideally happen on pointer down / begin drag rather than waiting until pointer click completes.

Dragging should respect the coordinate system of the windowâ€™s parent RectTransform.

==================================================
11. RESIZE HANDLE SYSTEM

There is a generic script called:

ResizeHandle

The window has eight resize handles:

* Left.
* Right.
* Top.
* Bottom.
* Top-left.
* Top-right.
* Bottom-left.
* Bottom-right.

A flags enum is used:

[Flags]
public enum ResizeDirection
{
None = 0,
Left = 1,
Right = 2,
Top = 4,
Bottom = 8
}

Corners combine flags.

Examples conceptually:

TopLeft = Top | Left

TopRight = Top | Right

BottomLeft = Bottom | Left

BottomRight = Bottom | Right

The ResizeHandle implements drag interfaces such as:

IBeginDragHandler
IDragHandler

Its job is to resize a target RectTransform representing the window.

==================================================
12. RESIZE HANDLE LAYOUT

A previous problem occurred where side resize handles scaled incorrectly when the window changed size.

Correct conceptual setup:

LEFT HANDLE

* Anchored to left side.
* Stretches vertically.
* Fixed width.

RIGHT HANDLE

* Anchored to right side.
* Stretches vertically.
* Fixed width.

TOP HANDLE

* Anchored to top.
* Stretches horizontally.
* Fixed height.

BOTTOM HANDLE

* Anchored to bottom.
* Stretches horizontally.
* Fixed height.

CORNER HANDLES

* Fixed width.
* Fixed height.
* Anchored to their corresponding corner.
* Should not stretch.

This prevents gaps or overlapping caused by the handles themselves scaling incorrectly.

==================================================
13. WINDOW TOP BAR

The top bar contains three window controls, similar to a desktop OS.

For example:

* Minimize / equivalent.
* Maximize / restore.
* Close.

Important layout requirement:
When the window changes width:

* The top bar itself can stretch horizontally.
* The control buttons must keep a fixed size.
* The buttons should remain correctly aligned rather than stretching with the bar.

==================================================
14. MAXIMIZE / RESTORE

Before maximizing a window, store:

* Previous anchored position.
* Previous size.

Potentially other RectTransform state if necessary.

Then maximize to the available desktop/window container.

When restoring:
Return exactly to the stored previous state.

Do not assume the Canvas is always the direct parent.

Maximizing should operate relative to the actual window container / parent RectTransform.

==================================================
15. DRAW ORDER / BRING TO FRONT

Windows need to visually stack like desktop windows.

Typical Unity Canvas approach:

transform.SetAsLastSibling();

or equivalent.

The currently active window should be above other windows.

Be careful if other UI elements such as:

* Desktop.
* Taskbar.
* Overlays.

share the same hierarchy.

The window container exists partly so the sibling order can be managed without interfering with unrelated UI.

==================================================
16. WINDOW CONTENT

Windows can contain application content or minigames.

There is a designated content region inside a window.

Content should adapt to the current window size.

Usually:

* Instantiate content.
* Parent it to the windowâ€™s content RectTransform.
* Stretch it to fill that RectTransform.

However, not every child inside the content should necessarily scale with the window.

For example:
Targets in a minigame should not visually shrink just because the application window becomes smaller.

The difference between:

* Layout adapting to available space.
    and
* Objects changing physical scale.

must be preserved.

Prefer changing positioning/layout rather than scaling gameplay elements unless scaling is intentionally desired.

==================================================
17. TASKBAR

A taskbar has been added / is being integrated into the virtual desktop.

The taskbar should connect to the existing:

* Desktop icon system.
* Window system.
* WindowManager.

Expected behaviour resembles a desktop OS.

When an application is opened:

* A taskbar entry/button can represent that window/application.

Taskbar buttons should be able to interact with their corresponding windows.

Possible behaviours:

* Focus window.
* Restore window.
* Bring window to front.
* Possibly minimize / reopen depending on current design.

The taskbar should react to window lifecycle/state events instead of constantly searching the hierarchy.

==================================================
18. DESKTOP ICON SYSTEM

Desktop icons are another existing system.

Some of this was implemented by a teammate named Julia.

The window system was merged with Juliaâ€™s desktop icon implementation.

Desired architecture:

Desktop icon
â†’ opens corresponding application/window

Window
â†’ is managed by WindowManager

Taskbar
â†’ reflects currently open applications/windows

Avoid creating three completely independent systems.

==================================================
19. TASK / SKILL / LEVEL SYSTEM (DATA-DRIVEN)

The educational portion of the game uses a Data-Driven architecture to separate logic from configuration.

Core concepts:
* **Tasks (MonoBehaviours):** Reusable, event-driven mini-game elements (e.g. TargetTask). They detect input and emit events (OnComplete, OnError), but DO NOT contain level logic or grade evaluation.
* **Skills (ScriptableObjects):** `SkillData` assets store the configuration for a skill (SkillID, tutorial prefab). They are purely data containers.
* **Levels (ScriptableObjects + Generic Runner):** 
  * `LevelData` assets define the recipe for a level (name, tasks to spawn, required amount, skills to evaluate).
  * A single generic `LevelRunner` (or `LevelController`) MonoBehaviour sits on a generic window prefab. It reads the `LevelData`, spawns the tasks, listens to their events, and calculates the final grade.

When adding new minigames or levels, rely on creating new ScriptableObjects and reusing the generic `LevelRunner` instead of creating new `Level.cs` subclasses.

==================================================
20. MINIGAME: CLICK TARGETS & MODULAR TASKS

Tasks like clicking targets are implemented as modular, reusable components inside application windows.

A script like `TargetTask` inherits from a base `TaskBase` (or `Task`) and handles pointer clicks (e.g., `IPointerClickHandler`).

Crucially, in the new Data-Driven architecture, `TargetTask` does NOT manage level progression. It simply detects the click, triggers visuals/sounds, and invokes an event (`OnTaskCompleted`). The generic `LevelRunner` listens to this event to spawn the next target or finish the level.

Minigames must always remain contained inside the window rather than behaving as completely separate scenes or systems.

==================================================
21. TARGET MINIGAME LAYOUT

When the containing window resizes:

Targets should remain appropriately positioned within the content area.

They should not simply become smaller because the windowâ€™s parent RectTransform changes.

Prefer:

* Anchored positioning.
* Layout constraints.
* Recalculating available positions if necessary.

Avoid blindly scaling the entire minigame hierarchy with the window if this changes target size undesirably.

==================================================
22. CONFETTI / COMPLETION ANIMATION

There has been a completion animation involving confetti.

An Animator is used.

A problem occurred where adding an Animation Event caused the object to be destroyed immediately / interfered with the animation.

Important concept:
Animation Events execute when the animation reaches the frame where the event is placed.

If that event calls a method that destroys the object:

* Anything rendered or animated by that object stops immediately.

Therefore:
Place cleanup/destruction events at the actual end of the animation.

Alternatively:

* Use animation state exit logic.
* Animation event at last frame.
* Coroutine based on animation length.
* Callback from a dedicated animation controller.

Avoid destroying the GameObject before the visual effect has finished.

==================================================
23. ANIMATION SPEED

The confetti animation previously appeared too fast.

Changing animation playback can involve several different concepts:

* Animation clip sample rate.
* Animator state speed.
* Animator.speed.
* Duration / keyframe spacing.

Do not assume FPS is the only issue.

If keyframes are close together in time, the animation will still be fast regardless of how it is conceptually described.

When debugging animation speed, inspect:

* Clip duration.
* Samples value.
* Animator state Speed.
* Keyframe timestamps.

==================================================
24. INPUT SYSTEM

The project uses Unityâ€™s newer Input System package.

Mouse input is important because the game teaches computer use.

Relevant interactions include:

* Pointer movement.
* Mouse click.
* Dragging.
* Window resizing.
* Clicking UI.
* Desktop icons.
* Minigames.

Unity EventSystem remains useful for UI pointer events even when the new Input System package is active.

The project can use:
Input System UI Input Module

together with interfaces such as:

IPointerClickHandler
IPointerDownHandler
IBeginDragHandler
IDragHandler
IEndDragHandler

==================================================
25. KEYBOARD INPUT

Keyboard interaction is also expected.

This includes both:

A) Detecting individual keys / actions.

B) Actual typing / text entry.

These are different problems.

For text entry, prefer UI text input systems such as:

* TMP_InputField.

Do not implement complete text typing by manually checking every keyboard key unless the task specifically requires raw keyboard simulation.

==================================================
26. UI CLICK DETECTION

Sometimes it may be necessary to determine whether the pointer is currently interacting with UI.

Unityâ€™s EventSystem can be used.

For example conceptually:

EventSystem.current.IsPointerOverGameObject()

However, remember that behaviour may differ for:

* Mouse.
* Touch.
* Different pointer IDs.

Use the EventSystem architecture where appropriate rather than manually raycasting everything.

==================================================
27. WINDOW CURSORS

Resize handles may eventually change the mouse cursor to indicate:

* Horizontal resize.
* Vertical resize.
* Diagonal resize.

If implementing this:

* Different handles should expose their ResizeDirection.
* Cursor changes should occur on pointer enter/exit.
* Avoid duplicating separate scripts for all eight handles if one generic component can use configuration.

==================================================
28. PREFAB-BASED ARCHITECTURE

A major design principle in the project is reusable prefabs.

Prefer reusable prefabs for:

* Windows.
* Desktop icons.
* Resize handles.
* Taskbar entries.
* Targets / task elements.
* Applications when practical.

A generic window prefab can then be configured with different content.

This is preferred over manually building every application window separately.

==================================================
29. CURRENT DEVELOPMENT HISTORY

Work already done / tracked includes approximately:

* Basic window prefab: 1 hour.
* Windows moving and resizing: 3 hours.
* Closing and maximizing windows while remembering previous state: 2 hours.
* Final window tweaks and bug fixing: 2 hours.
* Merge with Juliaâ€™s desktop icon implementation: 2 hours.
* Planning the TaskSystem: part of the same work session.
* Defining Task / Skill / Level managers: 3 hours.

This means the window architecture is already an established part of the project and should not casually be replaced.

==================================================
30. GITHUB / TEAM CONTEXT

The project is collaborative.

There are approximately four team members.

A teammate named Julia has worked on parts such as:

* Desktop icons.
* Some gameplay / task scripts.

GitHub is used for collaboration.

There was work on connecting GitHub Projects with Discord so that when a task moves to:

Review

someone else on the team can be notified to review it.

The repository was originally created by a teammate rather than by me personally.

Do not assume I own every repository-level permission.

==================================================
31. GITHUB PROJECT / DISCORD WORKFLOW

Desired workflow:

Task moves to â€œReviewâ€� in GitHub Projects
â†’ automation detects the change
â†’ Discord webhook posts a review notification
â†’ one of the other team members can review it.

A variable / configuration called:

REVIEW_STATUS

has been discussed.

The idea is that it corresponds to the status name/value used for review tasks.

There were issues around GitHub Apps / workflow visibility and testing the workflow.

==================================================
32. OTHER UNITY KNOWLEDGE CONTEXT

I have asked about several C# / Unity fundamentals while developing this project.

When relevant, explain them rather than assuming expert knowledge.

Topics discussed include:

* Singleton pattern in Unity.
* C# List.
* Equivalent of C++ std::vector in C#.
* Properties.
* get accessors.
* Expression-bodied members using =>.
* IReadOnlyList.
* Delegates.
* Events.
* Callbacks.
* Interfaces.
* Flags enums.
* RectTransform anchors.
* Canvas scaling.
* EventSystem.
* Input System.
* Animator.
* Animation events.

==================================================
33. SINGLETONS

Singletons may be used for manager-style systems such as:

* WindowManager.
* TaskManager.
* SkillManager.
* LevelManager.

However:
Do not automatically turn every manager into a global singleton.

Consider:

* Lifetime.
* Scene structure.
* Dependency management.
* Whether only one instance logically exists.

If a singleton is appropriate, use a conventional Unity pattern and avoid unnecessarily complex service locators.

==================================================
34. RECTTRANSFORM IMPORTANT CONCEPT

When working with RectTransforms, always distinguish:

* anchorMin / anchorMax.
* pivot.
* anchoredPosition.
* sizeDelta.
* offsetMin / offsetMax.
* localPosition.
* actual rendered rectangle.

Many bugs in the resize/window system come from mixing coordinate spaces.

When manipulating windows:
Always reason about the parent RectTransform and the coordinate system being used.

==================================================
35. GAME LAB ACADEMIC CONTEXT

This project is part of Game Lab / Games for Learning.

The project is not purely about creating an operating-system simulation.

The educational purpose matters.

The game should teach children how to interact with computers.

Examples of skills that could be taught include:

* Mouse movement.
* Clicking.
* Double clicking.
* Drag and drop.
* Window interaction.
* Typing.
* Keyboard usage.
* Basic desktop navigation.

Tasks and minigames should map onto these learnable skills.

==================================================
36. FEEDBACK FOR CHILDREN

Because the target audience is children, feedback is considered important.

Feedback may include:

* Animations.
* Sound.
* Visual confirmation.
* Progress indicators.
* Confetti or celebration.
* Clear success / failure states.

The feedback should reinforce what action was performed correctly.

Avoid excessive UI complexity.

==================================================
37. CODE CHANGE PHILOSOPHY

When helping me modify the project:

DO:

* Read the existing code first when I provide it.
* Preserve public APIs when practical.
* Reuse current managers.
* Prefer composition.
* Keep components reusable.
* Separate responsibilities.
* Explain changes clearly.
* Point out bugs in existing logic.
* Identify Unity lifecycle implications.
* Mention Inspector configuration when required.

DO NOT:

* Invent systems that conflict with the project architecture.
* Rename every class unnecessarily.
* Rewrite working systems for stylistic reasons.
* Introduce large external frameworks without justification.
* Assume an object hierarchy without checking screenshots/code when hierarchy matters.

==================================================
38. WHEN GENERATING SCRIPTS

When I ask for complete scripts:

Return complete compilable Unity C# files whenever possible.

Include:

* using statements.
* namespace.
* class declaration.
* fields.
* Unity lifecycle functions.
* public/private methods.
* required interfaces.

Do not give only isolated snippets if I explicitly ask for the whole script.

Also explain:

* Which GameObject each script goes on.
* Which Inspector references need assigning.
* Relevant hierarchy requirements.
* Any Input System/EventSystem requirement.

==================================================
39. WHEN DEBUGGING

Use this order when possible:

1. Inspect compiler/runtime error.
2. Inspect relevant code.
3. Inspect hierarchy / Inspector if relevant.
4. Determine whether issue is:
    * C# logic.
    * Unity lifecycle.
    * RectTransform coordinate space.
    * EventSystem.
    * Animator.
    * Prefab/reference configuration.
    * Input System.
5. Propose the smallest correction that fixes the actual issue.

Do not immediately replace the whole implementation.

==================================================
40. CURRENT PROJECT DIRECTION

The project is progressing from infrastructure toward educational gameplay.

Major infrastructure already developed or underway:

* Desktop.
* Desktop icons.
* Window prefab.
* Window dragging.
* Window resizing.
* Maximize / restore.
* WindowManager.
* Taskbar.
* Task / Skill / Level architecture.

Current/next gameplay focus includes:

* Minigames that teach specific computer skills.
* Integrating them into application windows.
* Integrating them with tasks, skills and levels.
* Proper completion feedback.
* Maintaining clean interaction between the desktop simulation and learning systems.

==================================================
41. SOURCE OF TRUTH

This document is contextual guidance, not a substitute for the actual repository.

Whenever I provide:

* Current scripts.
* GitHub repository files.
* Screenshots.
* Inspector values.
* Hierarchy screenshots.

Those should be treated as more current and authoritative than this document.

If the code contradicts this document because the project has evolved, use the current code and point out the difference.

==================================================
42. EXPECTED ASSISTANT BEHAVIOUR

When helping with this project, act like a technical collaborator familiar with the codebase.

For coding questions:
Explain both:

1. What to change.
2. Why it works.

For architecture questions:
Discuss consequences in the context of THIS project rather than giving generic Unity advice.

For bugs:
Try to identify the root cause.

For C# questions:
Relate the language feature back to how it is being used in the Game Lab code.

For Unity UI questions:
Take the existing desktop/window architecture into account.

For new features:
Determine how they should connect to:

* WindowManager.
* Windows.
* Taskbar.
* Desktop icons.
* Tasks.
* Skills.
* Levels.

before proposing an isolated implementation.

==================================================
43. RECENT UPDATES: SKILL EVALUATION & WINDOW ANIMATIONS

* **Skill Evaluation Architecture (Data-Driven Refactor):**
  * The project was recently refactored to remove hardcoded `Level1`, `Level2` classes.
  * **Tasks** (`TargetTask`, `HoverTask`) remain purely as input detection tools and expose events/raw data (`OnCompleted`, `OnError`).
  * **Levels** are now defined by `LevelData` ScriptableObjects. A single generic `LevelRunner` orchestrates the logic, reads the `LevelData`, instantiates tasks, tracks time/errors, and evaluates the skills when the level completes.
  * Skills are assigned to levels via the Inspector using `SkillData` ScriptableObjects, eliminating empty "ghost prefabs" for skills.
* **Skills Report Window:**
  * `SkillManager` stores grades per `SkillID` and `LevelID`, calculates averages (`GetAllAverageGrades()`), and fires a `GradesChanged` event.
  * `SkillsReportWindow` and `SkillGradeUI` display the grades inside a desktop window and auto-refresh via `GradesChanged`.
* **Window Open / Minimize / Close Animations:**
  * `Window` uses a Coroutine (`AnimateScale`) interpolating `windowRectTransform.localScale` with cubic ease-out on `OnEnable`, `Restore`, `Minimize`, and `CloseWindow`.
  * `Window.SetContent` uses `SetParent(contentArea, false)` and resets `localScale = Vector3.one` so child content does not distort when parented while the window is at scale 0.



==================================================
44. ARCHITECTURE UNIFICATION: ADAPTIVE LEARNING MERGED INTO LEVELRUNNER

**Context:** The project previously had two parallel window execution architectures:
1. LevelRunner.cs for fixed sequences (Levels 1, 2, 3) reading from LevelDefinition.cs.
2. AdaptiveSessionStarter.cs / LearningSessionManager.cs for procedural generation (originally isolated in Test_AdaptiveLearning).

**Changes Implemented:**
* The parallel AdaptiveSessionStarter.cs has been completely deprecated to unify the project under a single architecture.
* LevelDefinition.cs (ScriptableObject) was expanded with two new fields: isAdaptive (bool) and daptiveTaskCount (int).
* LevelRunner.cs now natively handles procedural task generation. If isAdaptive == true, it completely ignores the fixed 	asks list. Instead, inside SpawnNextTask(), it copies the weighted roulette logic from the old session manager: it queries SkillManager.Instance.GetSkillWeights() to find the player's weakest skills, randomly selects one weighted by its deficiency, queries the difficulty, and asks TaskManager.Instance.SelectTask for an appropriate prefab.
* **Level 4 Implementation:** A new asset L_Level4.asset was created, checked as isAdaptive = true, and configured with 10 tasks. In the MainScene, "Application Icon (3)" was renamed to "Level 4" and assigned this asset.
* **Unified UI:** Because Level 4 now runs through LevelRunner.cs, it automatically inherits the same GenericLevelWindow.prefab, the same dark green intro tutorial screen ("TEND THE GARDEN!"), and the same closing logic ("Garden cared for! Click the X") as the rest of the game.

==================================================
45. GRANULAR TASK EVALUATION: CLICKTARGET & HOVERTARGET OVERHAUL

**Context:** Previously, tasks only fired a single Complete(bool) at the end of their lifecycle. This caused statistical inaccuracies (e.g., clicking 1 correct target and missing 5 times only counted as '1 correct attempt' instead of tracking the misses).

**Changes Implemented:**
* BaseTask.cs exposes a RecordIntermediateResult(bool success) method which sends a TaskResult to SkillManager and ProgressData without destroying the task.
* ClickTargetTask.cs was completely rewritten. It now implements IPointerClickHandler on its background/container. Every click on a valid target triggers RecordIntermediateResult(true), while every click on the background triggers RecordIntermediateResult(false). The task only calls FinishWithoutResult() when all targets are exhausted.
* HoverTarget.cs and HoverTargetTask.cs were updated. If the player exits the bounds of a hover target before the fill timer completes, the script fires an OnFailedAttempt event, triggering RecordIntermediateResult(false) to properly penalize early exits in the stats.

==================================================
46. PROGRESSION EVENTS AND VIRTUAL MASCOT DYNAMIC POINTERS

**Context:** The Virtual Mascot (VirtualMascot.cs) was only checking for newly unlocked levels inside IconGrid.Start(), meaning the user had to reload the desktop scene to see the mascot point to new icons.

**Changes Implemented:**
* ProgressData.cs now defines a public System.Action<string> OnLevelUnlocked; event. This is invoked the exact millisecond UnlockLevel(string) adds a new level to the tracked list.
* IconGrid.cs handles the Desktop Tour. It was modified to subscribe to OnLevelUnlocked in OnEnable(). When the event fires (e.g., when the user closes Level 3 and it unlocks Level 4), IconGrid instantly finds the newly available icon by name (e.g., "Application Icon (3)") and commands VirtualMascot.Show() to point at it with a custom message.
* **Desktop Tour Lock:** A transparent UI blocker (
aycastTarget = true) is dynamically instantiated at the start of the initial IconGrid Mascot Tour coroutine and destroyed at the end. This prevents the user from clicking desktop icons prematurely while the Mascot is still explaining the UI.

==================================================
47. UI OVERHAUL: STATS WINDOW & TEXTMESHPRO

**Context:** The legacy SkillsReportWindow used standard Unity UI Text which was blurry and displayed raw percentages.

**Changes Implemented:**
* All text elements in SkillGradeUI.prefab were migrated to TextMeshProUGUI for crisp rendering.
* The UI logic was rewritten to group results hierachically. The view now dynamically generates collapsible/grouped entries that display: Skill -> Level ID -> [Successes / Fails / Total Attempts].
* Due to the unified LevelRunner architecture (Section 44), all tasks, including Level 4's adaptive tasks, automatically inject their correct levelId and levelName into the TaskResult, meaning Level 4 now properly shows up as "Level 4 (Practice)" in the stats window rather than "unknown".
==================================================
48. VIRTUAL MASCOT SYSTEM (RECENT ADDITION)

**Context:** A Virtual Mascot has been integrated throughout the project to guide the player, provide didactic feedback, and highlight UI elements dynamically. It acts as a global overlay.

**Architecture and Usage:**
* VirtualMascot.cs operates as a Singleton UI overlay. It is invoked statically via VirtualMascot.Show(string message, RectTransform target, Vector2 offset).
* If the Mascot is not present in the scene, calling Show() will automatically instantiate the default Mascot prefab into the target's Canvas.
* To dismiss it, scripts call VirtualMascot.HideMascot().

**Current Integrations:**
1. **Initial Desktop Tour:** Managed by IconGrid.cs. On the first boot, a Coroutine orchestrates the Mascot pointing sequentially to Level 1, the Stats icon, and back to Level 1. A transparent full-screen blocker (
aycastTarget = true) is temporarily spawned to prevent the player from interacting with the desktop until the tour concludes.
2. **Dynamic Unlocks:** IconGrid listens to ProgressData.Instance.OnLevelUnlocked. When a new level unlocks (e.g., Level 4), the Mascot automatically appears on the desktop pointing to the newly unlocked icon with a custom message.
3. **Level Intros:** In LevelRunner.cs, before tasks begin, a dark green instruction screen appears. The Mascot points directly to the "TEND THE GARDEN!" play button to ensure the child knows where to click.
4. **Level Outros:** When a level (or the adaptive session) finishes, LevelRunner.cs calls the Mascot to point directly to the Window's Close ('X') button, explicitly instructing the player to close the window.
==================================================
49. DESKTOP TOUR REWORK & SCENE SEPARATION

**Context:** The original Desktop Tour was hardcoded inside `IconGrid.cs` and used an invisible screen blocker, which collided with the new `TutorialManager` and adaptive level systems. The mascot logic was becoming tangled and causing race conditions on scene load.

**Changes Implemented:**
* **Scene Isolation & Interaction Flow:** Created `IntroTutorialScene` (automated via `TutorialSceneSetup.cs` editor script) to cleanly house the onboarding experience. The `DesktopTour.cs` script was rewritten into a state-machine coroutine that waits for real user interactions (open, drag, maximize, restore, minimize, close) instead of time-based delays. The initial "single click" step was removed to go straight to double-clicking.
* **Smart Reminder System:** `DesktopTour.cs` now includes a `WaitWithReminder` system that gently repeats instructions if the child takes too long, and dynamically redirects the mascot to the taskbar if the window is accidentally minimized.
* **JSON Externalization:** All tutorial and mascot strings have been moved to JSON files for easy localization and editing by designers. 
  - `DesktopTourDialogs.json` stores the intro sequence strings.
  - `LevelTutorialDialogs.json` stores the Level 1 / LevelRunner mascot strings ("Double click here", "Read carefully", "Close window").
* **IconGrid Cleanup:** Removed the legacy hardcoded Desktop Tour and invisible `TourBlocker` from `IconGrid.cs`. It now exclusively handles level unlock notifications.
* **GameFlowController Fix:** `GameFlowController` was inadvertently triggering `InitialTestManager` instantly after the `TutorialManager` panels closed. Because no window was open, the test immediately failed, triggering a chain reaction that instantly hid the Virtual Mascot. The automated startup of `InitialTestManager` was removed, allowing the mascot to securely point the child to the Level 1 icon so they can manually open the window via `DraggableIcon`.
* **DraggableIcon & Mascot Safeties:** Modified `DraggableIcon.cs` to expose `LevelDef` via a property, and prevented it from auto-maximizing windows or hiding the mascot globally while `DesktopTour.IsTourRunning` is true. 
* **Mascot Lifecycle & Bounds Fix:** Added `OnDestroy()` cleanup to `VirtualMascot.cs` to ensure the static `Instance` reference doesn't hold onto a destroyed object across scene loads. Additionally, clamped the mascot's target position to screen boundaries using `Mathf.Clamp` with `Screen.width/height` to prevent it from sliding off-screen when instructed to point outside the window bounds.

==================================================
END OF PROJECT CONTEXT



==================================================
50. DESKTOP MANAGER & DYNAMIC INSTANTIATION

**Context:** Previously, the `VirtualMascot` was responsible for instantiating itself via `Resources.Load`, and Desktop Icons were manually placed in the scene. As the project scales to support dynamic creation of files, folders, minigames, and drawings, a centralized manager is required.

**Changes Implemented:**
* **DesktopManager.cs:** A new Singleton added to manage the desktop environment. It acts as the central spawner for dynamic desktop elements.
* **Mascot Instantiation:** `DesktopManager` now holds a reference to `mascotPrefab` in the Inspector and instantiates it into the Desktop Canvas during `Awake()`, keeping it deactivated until needed. 
* **VirtualMascot.cs Refactor:** The `VirtualMascot` script no longer uses `Resources.Load()` or self-instantiates. It maintains its Singleton pattern (`VirtualMascot.Show`) but expects the `DesktopManager` to have injected it into the scene.
* **Dynamic Icon System:** `DesktopManager` can now spawn icons automatically on Start using the `initialDesktopIcons` list.
* **Efficient UI Architecture:** Icons are no longer created by instantiating different prefabs. Instead, the manager always instantiates a `defaultIconPrefab` and swaps its `Sprite` and text name at runtime via `DesktopIconData`.
* **Content Injection (BaseWindow):** All icons share a global `baseWindowPrefab`. `DesktopIconData` specifies a unique `contentPrefab` for each icon. When double-clicked, `DraggableIcon` opens the generic `BaseWindow` and injects the specific `contentPrefab` into it dynamically via `appWindow.SetContent()`.
==================================================
==================================================
51. VIRTUAL MASCOT VISUAL REFACTORING (CURSOR MODE)

**Context:** The Virtual Mascot was originally programmed with hardcoded `Vector2` offsets and generic targeting, under the assumption it was a character floating nearby. The design shifted to a cursor-like mascot (a star where the top-left tip is the actual pointer), which required precision targeting, smart flipping, and click-through capabilities.

**Changes Implemented:**
* **True Center Targeting:** `VirtualMascot.cs` no longer relies on `targetRect.position` (which returns the UI Pivot, causing the mascot to point at the edges of objects like the 'X' button). It now calculates the exact visual center using `targetRect.GetWorldCorners()`.
* **Ignored Legacy Offsets:** The mascot now ignores all legacy `Vector2` offsets passed by other scripts, acting as a direct precision cursor. The floating animation was reduced to keep the tip on target.
* **Auto-Flip Architecture:** The mascot automatically mirrors its graphic (`localScale.x = -1`) when pointing at UI elements on the right half of the screen. This is calculated robustly using `RectTransformUtility.WorldToScreenPoint` to avoid Canvas scaling issues.
* **Smart Speech Bubble Layout:** Instead of hardcoded magic numbers, the speech bubble's distance is exposed in the Inspector via `speechBubbleOffset`. The code automatically counter-flips the speech bubble's scale so the text remains readable. It also dynamically alters the bubble's `pivot` (Left/Right) depending on the mascot's flip state, ensuring the `ContentSizeFitter` always grows the text box *away* from the mascot, preventing overlap regardless of string length.
* **Raycast Blocking:** Added a script-enforced `CanvasGroup` in `Start()` with `blocksRaycasts = false` to guarantee the mascot and its speech bubble never block mouse clicks intended for the UI underneath it.
==================================================
END OF PROJECT CONTEXT

==================================================
50. PRE-TUTORIAL ONBOARDING (NIVEL 1, 2, 3, 4 AUTO-SEQUENCE)

**Contexto:** Los niÃ±os necesitaban niveles mÃ¡s sencillos (mover ratÃ³n, hover, click) antes de enfrentarse al Desktop Tour. El flujo ahora orquesta estos niveles de forma automÃ¡tica al iniciar por primera vez el juego.

**ImplementaciÃ³n con DesktopManager:**
- `DesktopManager.cs` tiene ahora un `Start()` que comprueba la variable `PlayerPrefs` `"AutoSequenceCompleted"`.
- Si es 0, ejecuta la corrutina `AutoOnboardingSequence()`.
- Esta corrutina spawnea los iconos iniciales, usa a la Virtual Mascot para apuntar a cada uno de ellos y llama automÃ¡ticamente a `OpenApplication()` sin que el niÃ±o tenga que hacer doble click.
- El sistema de progreso (`ProgressData`) desbloquea a la fuerza cada nivel durante la secuencia para que sean jugables.
- Se ha creado una nueva task `MoveMouseTask.cs` que pide al usuario que mueva el ratÃ³n por la pantalla una distancia concreta. Nota: usa el nuevo `UnityEngine.InputSystem` en lugar de `Input.mousePosition`.
- Al terminar el Nivel 4 (Adaptativo), se guarda `"AutoSequenceCompleted"` como 1 y se carga la escena `IntroTutorialScene` (Desktop Tour).

==================================================

==================================================
52. ARCHITECTURE BUG FIXES (TUTORIAL BLOCKER, MASCOT, ONBOARDING SEQUENCE)

**Context:** The new onboarding systems and mascot visual refactors introduced several critical architecture bugs that broke the tutorial flow.

**Changes Implemented:**
* **TutorialBlocker Event Consumption:** TutorialBlocker.cs was intercepting raycasts via ICanvasRaycastFilter but failing to consume them because it didn't implement event interfaces. The UI EventSystem allowed the events to bubble down to desktop icons. Added empty implementations of IPointerClickHandler, IDragHandler, etc., to physically consume the blocked input.
* **Smart Mascot Hiding:** Previously, closing *any* window unconditionally hid the Virtual Mascot. Replaced VirtualMascot.HideMascot() with HideMascotIfTargeting(transform) in Window.cs, ensuring the mascot only hides if it was explicitly pointing at the window being closed.
* **Canvas Scale Math Fix:** The Mascot's speech bubble clamp logic incorrectly added Screen Space pixels to Local Space anchored coordinates. Fixed by dividing the pixel difference by canvas.scaleFactor before applying it to nchoredPosition.
* **AutoOnboardingSequence Robustness:** The DesktopManager loop was previously bypassing gameplay checks and unlocking levels merely when a window closed. It was modified to wait and verify ProgressData.Instance.IsLevelUnlocked(nextLevelId). Crucially, if the user closes a window prematurely, the sequence no longer aborts (which broke the flow and failed to transition to IntroTutorialScene) nor does it create an infinite frame loop. Instead, the coroutine now safely uses yield return null to wait for the user to reopen the target application window and successfully complete the level.

==================================================

==================================================
51. MOUSE SKILL DEFINITIONS ADDED

**Context:** The educational design required explicit SkillDefinitions for advanced mouse interactions so they can be assigned to Tasks later in the curriculum.

**Implementation:**
- Added a `description` string field (with a `[TextArea]` attribute) to `SkillDefinition.cs` to allow designers to document the pedagogical purpose of each skill.
- Created/Updated the following 4 new SkillDefinitions in `Assets/ScriptableObjects/Skills/`:
  1. `SK_DoubleClick`: "The ability to perform two consecutive left clicks within a short time interval."
  2. `SK_RightClick`: "The ability to perform a right mouse click to interact with an element or access additional options."
  3. `SK_ClickHold`: "The ability to press and hold the left mouse button for a continuous period of time."
  4. `SK_DragDrop`: "The ability to click and hold an object, move it to another location, and release it."

==================================================

==================================================
52. CORE MOUSE TASK DEFINITIONS GENERATED

**Context:** Now that the advanced mouse skills were defined, the `TaskDefinition` instances needed to be generated systematically so designers can begin assigning gameplay prefabs to them.

**Implementation:**
- Automatically generated 17 new `TaskDefinition` ScriptableObjects across 4 new directories within `Assets/ScriptableObjects/Tasks/`:
  - `/DoubleClick`: 4 tasks (Difficulty 1 to 4) mapped to `SK_DoubleClick`.
  - `/RightClick`: 4 tasks (Difficulty 1 to 4) mapped to `SK_RightClick`.
  - `/ClickHold`: 4 tasks (Difficulty 1 to 4) mapped to `SK_ClickHold`.
  - `/DragDrop`: 5 tasks (Difficulty 1 to 5) mapped to `SK_DragDrop`.
- Each task correctly uses the existing `TaskDefinition.cs` script, with progressive target descriptions set in the `displayName` and linked perfectly to their primary skill via GUIDs. 
- No actual prefabs or gameplay logic were implemented or modified; strictly the data architecture was set up as requested.

==================================================

==================================================
53. GAMEPLAY TASK SCRIPTS & PREFABS IMPLEMENTED

**Context:** The architecture required actual implementation of the new mouse tasks (`DoubleClickTask`, `RightClickTask`, `ClickHoldTask`, `DragDropTask`) and generation of their prefabs, linked to the `TaskDefinitions`.

**Implementation:**
- Implemented `DoubleClickTask.cs` (detects `eventData.clickCount == 2`).
- Implemented `RightClickTask.cs` (detects `PointerEventData.InputButton.Right`).
- Implemented `ClickHoldTask.cs` (uses `IPointerDownHandler`, `IPointerUpHandler`, and `Update` timer).
- Implemented `DragDropTask.cs` (uses `IBeginDragHandler`, `IDragHandler`, `IEndDragHandler` to move a rect and snap to a drop target).
- Created a `TaskPrefabGenerator` Editor tool to automatically construct the 4 prefabs (`Task_DoubleClick`, `Task_RightClick`, `Task_ClickHold`, `Task_DragDrop`) using basic primitives (Images) and dynamically link them into the `taskPrefab` field of all 17 previously created `TaskDefinition` instances.

==================================================

==================================================
54. LEVELS 5 TO 9 CONFIGURED

**Context:** The progression curve of the educational game required new fixed levels to systematically teach the advanced mouse skills, without interfering with the Level 4 Adaptive Session logic.

**Implementation:**
- Five new ScriptableObjects (`L_Level5` to `L_Level9`) were created in `Assets/ScriptableObjects/Levels/`.
- All of them were strictly set to `isAdaptive = false`.
- **Level 5 (Double Click):** Progresses through Double Click tasks from Difficulty 1 to 4.
- **Level 6 (Right Click):** Progresses through Right Click tasks from Difficulty 1 to 4.
- **Level 7 (Click & Hold):** Progresses through Click & Hold tasks from Difficulty 1 to 4.
- **Level 8 (Drag & Drop):** Progresses through Drag & Drop tasks from Difficulty 1 to 5.
- **Level 9 (Mixed Consolidation):** Combines tasks from Click, Double Click, Right Click, Drag & Drop, Hover, Click & Hold, and Move Mouse to force context switching.
- **Level 4 Integrity Maintained:** `L_Level4.asset` and the adaptive runner architecture were deliberately untouched.

==================================================

==================================================
55. LEVEL TESTER SCENE CREATOR

**Context:** The team needed a way to instantly test any level (1 to 9) without playing through the forced auto-sequence or resetting `PlayerPrefs` every time.

**Implementation:**
- Created `LevelTestSceneCreator.cs` (Editor script) which provides a new menu item: `ComputerLearning -> Create Level Test Scene`.
- Clicking this creates and saves a new `LevelTestScene.unity` containing a generic `Canvas` and a `LevelTester` component.
- `LevelTester.cs` renders an in-game UI listing all available `LevelDefinition` assets.
- Clicking a level in the UI dynamically instantiates the `windowPrefab`, injects the `LevelRunner`, automatically maximizes the window, and calls `runner.StartLevel(levelDef)` to simulate identical behaviour to `DraggableIcon.OpenApplication()`.

==================================================

==================================================
56. TASK PREFAB ARCHITECTURE & CONTAINERS

**Context:** Earlier task prefabs (like Double Click) appeared as giant green screens because TaskManager.SpawnTask forces the root instantiated GameObject to stretch to fill the 	askContentArea (nchorMin = 0, nchorMax = 1).
If the BaseTask script and visual Image were on the root object, the target became impossible to miss (and broke visually).

**Implementation:**
- Built RebuildPrefabsWithContainers.cs to wrap all task visuals in a generic RectTransform container.
- The **Container** stretches to fill the window.
- The **Target** (child object) maintains its relative size and anchors to the center, housing the Image, text, and specific Task script (e.g., DoubleClickTask).
- TaskManager.SpawnTask was modified to use go.GetComponentInChildren<BaseTask>() so it can find the script regardless of how deep the visual target is nested.
- LevelRunner.HandleTaskCompleted was modified to destroy the entire prefab hierarchy (by searching up to 	askContentArea) instead of just ctiveTask.gameObject, preventing "orphaned" drop zones or containers from staying on screen.

==================================================
57. DATA-DRIVEN TASK CONFIGURATION

**Context:** The newly added tasks (Double Click, Drag Drop, etc.) originally hardcoded their logic (speed, size, movement) based purely on TaskDefinition.difficulty. This bypassed the existing TaskConfig fields (isMoving, 	argetSize, 	argetCount), breaking the data-driven architecture.

**Implementation:**
- Restored data-driven control by making scripts read directly from TaskDefinition.config.
- TaskTargetMovement.cs now reads config.isMoving and config.moveSpeed.
- DragDropTask.cs and ClickHoldTask.cs scale their sizes based on config.targetSize.
- Created an Editor tool UpdateTaskDataConfig.cs to mass-update the ScriptableObjects in Assets/Data/Tasks/, correctly assigning speeds, counts, and sizes based on difficulty, so they work correctly out of the box.

==================================================
58. DYNAMIC TASK MOVEMENT (TASKTARGETMOVEMENT)

**Context:** Targets always spawned in the exact center, making the game repetitive. Harder levels needed targets to bounce around like the older ClickTargetTask.

**Implementation:**
- Created TaskTargetMovement.cs, attached to the Target GameObject inside the new prefabs.
- **Random Spawning:** Regardless of whether they move or not, targets now spawn at a random coordinate within the window bounds.
- **Bouncing:** If config.isMoving == true, the target travels in a random direction and bounces seamlessly off the RectTransform bounds of the window using Time.deltaTime.

==================================================
59. DRAG & DROP MULTIPLE PIECES

**Context:** The Drag & Drop task needed to be more challenging in higher difficulties by having multiple items to drop into the same container.

**Implementation:**
- Separated Drag & Drop logic into a manager (DragDropTask.cs) and a draggable component (DraggablePiece.cs).
- In SetupTask(), DragDropTask checks config.targetCount. If it's > 1, it clones the draggable piece programmatically.
- Both the pieces and the Drop Zone spawn in randomized positions within the container bounds, dynamically ensuring they are spaced apart.
- DragDropTask waits for all DraggablePiece components to report OnPieceDropped before calling Complete(true).

==================================================
60. CLICK & HOLD VISUAL FEEDBACK

**Context:** The Click & Hold task was confusing without feedback and too difficult if it moved around.

**Implementation:**
- Disabled movement explicitly for ClickHoldTask (even if data requests it).
- Shortened the hold time (0.5s to 1.5s max).
- Added a FillImage to Task_ClickHold.prefab using Image.Type.Filled (Vertical, Bottom origin) with the default Unity Background sprite.
- In ClickHoldTask.Update(), illImage.fillAmount is mathematically tied to currentHoldTime / requiredHoldTime.
- If the user releases early (OnPointerUp) or leaves the box (OnPointerExit), the bar resets to 0 instantly and registers a failed attempt.

==================================================
61. DOUBLE CLICK INPUT BUG FIX

**Context:** Unity's new Input System (InputSystemUIInputModule) notoriously fails to consistently register PointerEventData.clickCount == 2 in UI elements.

**Implementation:**
- Rewrote the double-click evaluation in DoubleClickTask.cs.
- It now uses a custom DOUBLE_CLICK_THRESHOLD (set to 1.0 seconds for accessibility for children).
- Tracks Time.time on the first left-click. If a second left-click occurs within the threshold, it triggers Complete(true).

==================================================
==================================================
62. REFINED ONBOARDING, NO MAGIC STRINGS & GARDEN THEME REMOVAL

**Context:** The initial automated onboarding still allowed the player to click outside the guided flow, felt too robotic, lacked visual feedback in Level 1, and the game text leaned heavily on an abandoned "Garden" theme. Furthermore, the codebase relied on brittle GameObject.Find lookups.

**Changes Implemented:**
* **No Magic Strings / GameObject.Find Removed:** Purged brittle 	ransform.Find and GameObject.Find throughout the codebase. Window.cs now exposes direct references (TitleBar, ContentArea, etc.). DesktopManager caches ctiveIcons on spawn and exposes GetIconByLevelId().
* **Strict & Seamless Onboarding Flow:** The TutorialBlocker is now an independent ScreenSpaceOverlay Canvas, removing dependency on specific canvas naming. During the AutoOnboardingSequence (Levels 1-4), the blocker strictly isolates input to the current app window's ContentArea. The player can no longer drag, close, minimize, maximize, or interact with the taskbar/desktop during onboarding.
* **Auto-Start & Auto-Close Automation:** The manual "START!" button and "Click 'X' to close" prompts were removed during onboarding. The Virtual Mascot automatically introduces the level with lively dialogue, waits 5 seconds, and auto-starts the level. Upon completion, the window auto-closes after a congratulatory message, making the guided sequence completely seamless.
* **Garden Theme Removed:** Eliminated all "Harvest/Garden" text from L_Level3, L_Level4, LevelRunner, DesktopManager, and JSON dialog files (LevelTutorialDialogs.json), replacing them with standard computer-learning terminology (apps, practice points, challenges) to fit the theme.
* **Dynamic Mascot Speech Bubble:** Fixed an issue where long mascot text overflowed the speech bubble. VirtualMascot.cs now forces the text to wrap using a LayoutElement with a maximum width constraint (350px) and recalculates the bubble height using LayoutRebuilder.ForceRebuildLayoutImmediate().
* **Hover-to-Wipe Minigame (Level 1 Redesign):** MoveMouseTask.cs was completely rewritten from an invisible mouse distance tracker into an interactive visual minigame. It dynamically spawns 24 "dirt spots" inside the window. The player must simply hover (no clicks required) over each spot to clean them, filling a progress bar. This forces deliberate mouse movement across the entire window area.

==================================================

==================================================
62. SECOND TUTORIAL (ADVANCED DESKTOP TOUR)

A second tutorial was created to explain advanced desktop actions (moving windows, resizing, and the recycle bin) to users AFTER they complete levels 1 to 5.
It operates independently in a new scene (SecondTutorial.unity) so as not to clutter DesktopManager.

Key architecture choices:
* **AdvancedDesktopTour.cs**: An IEnumerator state machine script orchestrating the sequence. It uses WaitWithReminder() just like the first tutorial.
* **RecycleBinIcon.cs**: A new component attached to the Application Icon prefab. It tracks distance to other DraggableIcon elements and destroys them if they are dropped within 100 pixels, emitting an OnIconRecycled event.
* **TutorialBlocker Unblocking**: For the Recycle Bin step (drag & drop an icon across the desktop), 	utorialBlocker.SetAllowedTarget(null) is called to completely unblock raycasts, as Unity EventSystem requires the canvas or grid to receive drag events.
* **Minimize Prevention**: If the user minimizes the window during a step that requires moving or resizing, 	argetWindow.Restore() is explicitly called within the wait loop to prevent soft-locks.

==================================================
63. ADVANCED DESKTOP TOUR - RECYCLE BIN COLLISION & EXCLUSIONS FIX

Fixes applied to SecondTutorial and AdvancedDesktopTour:
* **TutorialBlocker Exclusions:** Modifying TutorialBlocker.cs to allow excluding multiple specific targets (AddExcludedTarget()). This was used to block the close, minimize, and maximize buttons during the resize step, preventing the user from breaking the sequence by clicking them while keeping the rest of the window interactive.
* **Recycle Bin Slot Occupation:** We kept the DraggableIcon component on the Recycle Bin but disabled its initialization logic. This allows it to physically occupy a slot in the IconGrid, preventing other icons from snapping into the same space and overlapping it. 
* **Top Right Positioning:** The Recycle Bin is moved automatically to the top right of the screen (around x=800, y=400) where it snaps perfectly to the grid.

==================================================
64. POST TUTORIAL TRANSITION (END DEMO)

A new transition scene was created: PostTutorialScene.unity.
* It triggers immediately after the user finishes the AdvancedDesktopTour.
* **Visuals:** Shows a clean layout with the TeamCompass logo and an English message indicating that the child would continue to manage the virtual desktop naturally.
* **Return Logic:** Instead of creating a redundant one-off script, the return button is directly wired to Julia's SceneSystem.ChangeToMenu() method. This highlights the architectural choice of reusing existing managers and avoiding script bloat.

==================================================
65. SCENE SYSTEM (INPUT FIX & RESET PROGRESS)

Two important upgrades were applied to SceneSystem.cs:
* **New Input System Compliance:** The old Input.GetKeyDown(KeyCode.Escape) line was crashing because the project relies on the modern Input System package. It was migrated to UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame.
* **Reset Progress Method:** Added public void ResetProgress() which can be called directly from UI Buttons (e.g. in the Main Menu). It automatically wipes both runtime data (ProgressData.Instance.ClearHistory()) and persistence layers (PlayerPrefs.DeleteAll()), safely returning the game to Level 1.
==================================================
66. DESKTOP ICON LAYOUT, CIRCULAR HITBOXES, DATA-DRIVEN DIALOGUES & ADAPTIVE POOL REFINEMENT

Several critical quality-of-life and architectural improvements were made:
* **Strict Column-Based Icon Layout:** The IconGrid.cs placement algorithm was rewritten. Instead of relying on distance calculations that caused icons to cluster into squished blocks or squares, it now evaluates candidate = x * 1000f + y. This forces the desktop to strictly populate icons column-by-column (top-to-bottom, then left-to-right), perfectly mirroring standard OS behavior.
* **Circular UI Hitboxes:** Created CircleHitbox.cs implementing ICanvasRaycastFilter. This component restricts UI raycast clicks to a perfect circle (sqrMagnitude <= 0.25f), ignoring clicks on the transparent corners of square images. This was injected programmatically into dynamic tasks and added to task prefabs via MCP.
* **Fully Data-Driven Dialogues:** Purged all remaining hardcoded tutorial and level onboarding strings from C# scripts (FirstDesktopTutorial.cs, SecondDesktopTutorial.cs, LevelRunner.cs, AdaptiveSessionStarter.cs). Everything now routes through a central TutorialDialogs.cs struct parsing TutorialDialogs.json, paving the way for localization.
* **Adaptive Pool Refinement:** Removed Level 1 (SK_MouseMovement / mouse_move) from the allSkills list inside SkillManager (on Managers.prefab). As a result, the Adaptive Session (Level 4+) will no longer test or spawn the basic "Move Mouse" task, ensuring adaptive challenges focus on advanced skills.
* **Instruction Screens for Advanced Levels:** Populated the instructionText field in L_Level5 through L_Level9. This ensures LevelRunner.cs renders the dark blue intro screen with a "START!" button before launching these levels, paving the way for future video tutorial integrations.

==================================================
67. INITIAL ASSESSMENT TEST IMPLEMENTATION

**Context:** The project required an initial assessment to evaluate a child's baseline computer skills before they start playing the regular learning levels. The previous `InitialTestManager` was deprecated because it ran blindly without a window, breaking the flow and interacting poorly with the new `VirtualMascot`.

**What was implemented:**
* The Initial Assessment was integrated as a standard desktop application. It acts similarly to a `LevelDefinition` but uses a distinct grading system.
* **Flow:** The `FirstDesktopTutorial` now points the child to the "Assessment" icon *before* starting Level 1.
* When the Assessment icon is clicked, it opens a window with simple child-friendly instructions. It dynamically runs one or two tasks of each major skill (Click, Hover, Double Click, etc.).

**Reused Systems:**
* **Window System:** The assessment runs perfectly inside the generic `Window` prefab.
* **TaskManager / Tasks:** Existing modular tasks (e.g. `DoubleClickTask`, `DragDropTask`) were reused. No duplicate task scripts were created.
* **TaskResult:** Task results are still captured and logged, but handled specially by the orchestrator.
* **Virtual Mascot:** The mascot provides step-by-step guidance for each task (e.g., "Double click quickly!").

**Modified Files:**
* `InitialTestManager.cs`: Completely rewritten to act as the assessment orchestrator (similar to `LevelRunner` but exclusively for the diagnostic). It handles the instructions, spawns tasks sequentially, and calculates the final score.
* `DraggableIcon.cs`: Modified to inject and launch `InitialTestManager` when the `levelId` is `"assessment"`.
* `FirstDesktopTutorial.cs`: Modified to add `"assessment"` to the `levelSequence`, ensuring the Mascot points to it at the very start of the onboarding flow.
* `InitialAssessmentSetup.cs`: Created an Editor script (`ComputerLearning -> Setup Initial Assessment`) to easily generate the `L_Assessment.asset` dynamically from existing skills and inject it into the `DesktopManager`'s initial icons list.

**Score Calculation:**
* Instead of incrementally adjusting scores by `+2` or `-1` (like normal gameplay), the Initial Assessment determines a definitive starting proficiency for each skill.
* **Calculation Rules:** For every skill, it tracks `totalTasks`, `completedTasks`, and `totalAttempts`. 
  - Base Score = `(completedTasks / totalTasks) * 100`
  - Repeated Attempt Penalty = For every attempt beyond the minimum required, it subtracts 10 points. 
  - This perfectly distinguishes between a child who can perform a skill independently on the first try and one who requires repeated attempts, keeping the results within `0 - 100`.
* The final calculated score is passed to `SkillManager.Instance.SetScore(skillId, finalScore)`.

**Persistence & Distinction:**
* The assessment saves a specific `PlayerPrefs` flag (`InitialAssessmentCompleted`).
* Task results generated during the assessment are explicitly marked with `levelId = "assessment"`, allowing historical logs (`ProgressData`) to distinguish diagnostic data from normal gameplay data.
* If a window is closed prematurely or the test is interrupted, the manager gracefully calculates the scores based *only* on the tasks completed up to that point, without crashing or marking untested skills as failed.

**How to Test in Unity:**
1. In the Unity Editor menu, click `ComputerLearning -> Setup Initial Assessment`. This generates `L_Assessment.asset` and adds it to the `DesktopManager`.
2. Clear your `PlayerPrefs` (to simulate a first-time player).
3. Play the game. The Virtual Mascot will welcome you and point to the new "Assessment" icon.
4. Double-click it and complete the tasks!

**Future Iterations:**
* The assessment currently records the baseline scores but does not yet skip levels (as requested). In a future iteration, a `LevelProgressionManager` could read the `SkillManager` scores after the assessment to dynamically unlock Levels 1â€“5 based on proficiency.

==================================================
68. CLEAN GEOMETRIC INITIAL ASSESSMENT (MINIMALIST, TIME-AWARE, JSON-LOCALIZED)

**Context & Motivation:**
User feedback required decoupling the Initial Assessment Test from game minigames (such as dirt cleaning and animated targets) and removing the Virtual Mascot. The target audience includes children and novice users who may have never touched a mouse before. The test needed to feel clean, serious, and direct (Task > Action) with minimal cognitive load, configurable timeouts, fail-safes (Skip / Exit), and external JSON localization.

**Key Architecture & Design:**
* **Standalone Minimalist UI:** The assessment runs inside its own dedicated high-contrast, dark-slate UI (AssessmentCleanRoot), replacing all playful minigame elements.
* **No Virtual Mascot:** VirtualMascot.HideMascot() is enforced upon test start. There are no mascot interruptions or character animations.
* **English JSON Localization:** All UI labels and task definitions are loaded from Resources/AssessmentInstructions.json. Translating or adding new languages requires only modifying or providing a JSON file without recompiling code.
* **Per-Task Timeouts & Skip Option:**
  - Every task has an independent 	imeoutSeconds (e.g. 15s or 20s) configured in the JSON.
  - A real-time countdown progress bar is displayed at the top of the header (turning red in the last 3 seconds).
  - If a child struggles or remains inactive until the timer expires, the task is marked as timed out (0 points) and smoothly auto-advances to the next task after a subtle notice.
  - A persistent **Skip** button allows tutor/child to immediately bypass impossible tasks.
  - An **Exit** button allows ending the test at any time, preserving recorded skills.
* **Geometric Tasks Implementation:**
  1. **Move Cursor (mouse_move):** Clean blue circle. Hovering inside turns it green and completes the task.
  2. **Hover (hover):** Clean purple square. Hovering steadily fills an inner white gauge over 2.0s without clicking.
  3. **Single Click (click):** Clean orange square. Left-clicking completes the action.
  4. **Double Click (double_click):** Clean teal circle. Two rapid clicks within 0.45s complete the action.
  5. **Right Click (ight_click):** Clean amber rectangle. Evaluates PointerEventData.InputButton.Right.
  6. **Click and Hold (click_hold):** Clean red circle. Holding down fills an inner gauge over 2.0s. Releasing early resets gauge and increments extra attempts.
  7. **Drag and Drop (drag_drop):** Blue square draggable into a gray target slot on the right using Unity's EventSystem drag handlers.
* **Immediate Evaluation Visibility:**
  - Upon completion or exit, a clean summary card displays every tested skill with its percentage score (e.g., 100%, 90%, or  % (Timed out)).
  - Results are persisted to SkillManager.Instance.SetScore(...) and ProgressData.Instance.RecordResult(...).


==================================================
69. LIGHT MODE & AUTOMATIC EVALUATION REPORT JSON EXPORT

**Enhancements:**
* **Light Mode Transformation:**
  - Migrated the assessment UI from dark slate to a clean modern Light Mode palette.
  - Background is soft light off-white (#F1F5F9), the header is pure white (#FFFFFF) with a subtle bottom divider, and typography uses crisp slate hues (#0F172A for titles, #334155 for instructions, #64748B for step indicators).
  - High-contrast geometric shapes (Royal Blue, Purple, Vibrant Orange, Teal, Amber, Crimson Red) provide immediate clarity on a light canvas.
  - The completion card features a clean white container with alternating row backgrounds and high-contrast green/red score indicators.
* **Evaluation Report JSON Auto-Export:**
  - When the assessment completes or is exited, InitialTestManager now automatically exports a structured evaluation report to Assets/Assessment/{HH-mm-ss}.json.
  - The file name is timestamped with the exact current time (HH-mm-ss).
  - The exported JSON contains:
    - 	imestamp (ISO UTC) and ormattedTime (HH:mm:ss).
    - 	otalTasksTested, completedTasks, and calculated verageScore.
    - Detailed skills list: skillId, skillName, completed, score, extraAttempts, and 	imeTakenSeconds.
  - Ensures seamless integration with external analysis tools, tutors, and teachers without requiring manual data extraction.


==================================================
70. SPATIAL DISTRIBUTION & ANTI-INSTANT HOVER COMPLETION

**Problem Identified:**
When Task 1 (Move Cursor) was completed, the user's cursor naturally rested at the center of the screen (0, 0). Because Task 2 (Hover) previously spawned in the exact same spot, the cursor was already positioned inside the square upon appearance. Consequently, if the user didn't move, the 2-second hover timer counted down automatically and completed the task without requiring deliberate interaction.

**Solutions Applied:**
1. **Diverse Spatial Placement Across Stages:**
   - Every task now spawns in distinct coordinates across the screen canvas to force deliberate hand/mouse movements between exercises:
     - Task 1 (Move Cursor): Right side (180, 40).
     - Task 2 (Hover): Opposite Left side (-180, -40) — more than 360 pixels away from where Task 1 ends.
     - Task 3 (Click): Bottom-Right (140, -60).
     - Task 4 (Double Click): Top-Left (-120, 70).
     - Task 5 (Right Click): Top-Right (160, 60).
     - Task 6 (Click & Hold): Center-Bottom (0, -40).
     - Task 7 (Drag & Drop): Source at Left (-160, 0), target slot at Right (160, 0).
2. **Hover Arming Delay:**
   - Added an rmingDelay = 0.35f to HoverController. The gauge cannot begin filling during the first 0.35 seconds after shape instantiation, giving the player visual notice and ensuring that a stationary cursor does not instantly trigger completion.


==================================================
71. TIME-WEIGHTED SKILL EVALUATION FORMULA

**Overview:**
Skill proficiency calculation was upgraded to evaluate both accuracy (attempts/misclicks) and execution speed relative to each task's timeout limit.

**Formula & Logic:**
1. **Accuracy Factor:**
   - Starts at 100 points.
   - Deducts 10 points per extra attempt (misclick or releasing hold/drag prematurely).
   - Clamped to a minimum base of 20 points for completion.
2. **Time Factor (Speed Efficiency):**
   - Evaluated as 	imeRatio = timeTaken / currentTaskTimeout.
   - **Agility Threshold (	imeRatio <= 0.30):** Completing the task within the first 30% of the timeout incurs **0 time penalty** (full 100% speed credit).
   - **Gradual Time Penalty ( .30 < timeRatio <= 1.0):** For hesitant or slower completion, a penalty scales linearly up to 35 points:
     	imePenalty = ((timeRatio - 0.30f) / 0.70f) * 35f;
3. **Combined Final Score:**
   - inalScore = Mathf.Clamp(accuracyScore - timePenalty, 20f, 100f);
   - If timed out or skipped: inalScore = 0f.
4. **Enhanced Feedback Display:**
   - The completion results screen and the exported JSON (Assets/Assessment/{HH-mm-ss}.json) now record and display both the final score and the exact seconds taken (e.g. 94% (3.2s) or 62% (14.1s)).


==================================================
72. DOUBLE CLICK ATTEMPT CALCULATION FIX

**Problem Identified:**
In DoubleClickController, the very first click of a double-click gesture was incorrectly invoking onAttempt?.Invoke(). Since lastClickTime started at -1f, the initial click fell into the else branch, registering as a failed attempt and adding an unnecessary 10-point penalty. As a result, even flawless double clicks on the first attempt always capped at 90%.

**Solution Applied:**
- Updated DoubleClickController.OnPointerClick to only invoke onAttempt if lastClickTime > 0f (meaning a previous click had actually occurred and the user waited too long before clicking the second time).
- Expanded the double-click window to a child-friendly 0.50s threshold.
- A clean double click on the first try now correctly yields 0 extra attempts and a full 100% score.


==================================================
73. LOCALIZATION SYSTEM (JSON LANGUAGE SWITCHER & MENU BUTTON)

**Overview:**
Implemented a dynamic localization system allowing the game to switch between languages (English 'EN' and Spanish 'ES') via JSON resource naming conventions, with language selection persisted across sessions.

**JSON Naming Convention:**
- All dialog and instruction files in `Assets/Resources/` now follow the `[LANG]-[BaseName].json` format:
  - English: `EN-DesktopTourDialogs.json`, `EN-TutorialDialogs.json`, `EN-AssessmentInstructions.json`.
  - Spanish: `ES-DesktopTourDialogs.json`, `ES-TutorialDialogs.json`, `ES-AssessmentInstructions.json`.
- In accordance with project instructions, Spanish JSON files currently contain duplicate English text awaiting future translation.

**Localization Manager:**
- `LocalizationManager.cs`:
  - Central singleton managing active language (`CurrentLanguage`), persisted via `PlayerPrefs` (`SelectedLanguage`).
  - Exposes `LoadLocalizedResource(string baseName)`: dynamically resolves `Resources.Load<TextAsset>(lang + "-" + baseName)` with automatic fallbacks to `EN-` and un-prefixed versions.
  - Exposes `OnLanguageChanged` event and `ToggleLanguage()` helper.

**Scripts Updated to Use Localized Resources:**
- `DesktopTour.cs`: Loads `DesktopTourDialogs` via `LocalizationManager`.
- `FirstDesktopTutorial.cs`: Loads `TutorialDialogs` via `LocalizationManager`.
- `SecondDesktopTutorial.cs`: Loads `TutorialDialogs` via `LocalizationManager`.
- `AdaptiveSessionStarter.cs`: Loads `TutorialDialogs` via `LocalizationManager`.
- `GameFlowController.cs`: Loads `TutorialDialogs` via `LocalizationManager`.
- `LevelRunner.cs`: Loads `TutorialDialogs` via `LocalizationManager`.
- `InitialTestManager.cs`: Loads `AssessmentInstructions` via `LocalizationManager`.

**Main Menu Language Toggle Button:**
- Created `LanguageButton.cs` attached to a newly spawned `Btn-Language` in `Menu.unity` (inside `MenuBtnLayout`).
- Displays current language (`Language: EN` / `Language: ES`) and toggles on click, dynamically updating gameplay text resources.

==================================================
74. MENU LANGUAGE DROPDOWN (BOTTOM-RIGHT CORNER)

**Overview:**
Replaced the prominent language button in the main menu button layout with an elegant, compact dropdown situated in the bottom-right corner of the menu screen.

**Implementation Details:**
- **Removal of Btn-Language:** Removed the previous button from `MenuBtnLayout` to keep the primary vertical menu clean (Play, Reset Progress, Exit).
- **LanguageDropdown Component (`LanguageDropdown.cs`):**
  - Manages a TextMeshPro `TMP_Dropdown` component.
  - Automatically synchronizes selected option with `LocalizationManager.CurrentLanguage` (`English (EN)` index 0, `Español (ES)` index 1).
  - Listens to `onValueChanged` to update `LocalizationManager.SetLanguage(...)` and responds to `OnLanguageChanged` events without triggering feedback loops.
- **UI Positioning & Behavior in `Menu.unity`:**
  - Anchored directly to the Canvas bottom-right corner (`anchorMin = (1, 0)`, `anchorMax = (1, 0)`, `pivot = (1, 0)`, `anchoredPosition = (-40, 30)`).
  - The dropdown's internal template pivot and position were inverted to unfold **upwards** (`pivot = (0.5, 0)`, `anchoredPosition = (0, 44)`), ensuring the options list remains fully visible without clipping below screen boundaries.
