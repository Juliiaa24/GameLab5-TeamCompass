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
* Always keep this document (Documentation/Specs/Diego.md) updated with all new implementations, features, refactors, bug fixes, and architectural decisions made in the project.

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

The Canvas uses Unity’s Canvas Scaler so the interface adapts to different resolutions.

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
…
}

There is an existing C# script template used for the project.

The template follows a structure similar to:

/**

* Author:
* Date:
* Description:
    */

using …

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
This stretches relative to the object’s parent, not automatically relative to the Canvas.

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
→ Window invokes an event
→ WindowManager subscribed previously
→ WindowManager receives callback
→ WindowManager updates taskbar or window state

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

Dragging should respect the coordinate system of the window’s parent RectTransform.

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
* Parent it to the window’s content RectTransform.
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

The window system was merged with Julia’s desktop icon implementation.

Desired architecture:

Desktop icon
→ opens corresponding application/window

Window
→ is managed by WindowManager

Taskbar
→ reflects currently open applications/windows

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

They should not simply become smaller because the window’s parent RectTransform changes.

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

The project uses Unity’s newer Input System package.

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

Unity’s EventSystem can be used.

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
* Merge with Julia’s desktop icon implementation: 2 hours.
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

Task moves to “Review” in GitHub Projects
→ automation detects the change
→ Discord webhook posts a review notification
→ one of the other team members can review it.

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

**Contexto:** Los niños necesitaban niveles más sencillos (mover ratón, hover, click) antes de enfrentarse al Desktop Tour. El flujo ahora orquesta estos niveles de forma automática al iniciar por primera vez el juego.

**Implementación con DesktopManager:**
- `DesktopManager.cs` tiene ahora un `Start()` que comprueba la variable `PlayerPrefs` `"AutoSequenceCompleted"`.
- Si es 0, ejecuta la corrutina `AutoOnboardingSequence()`.
- Esta corrutina spawnea los iconos iniciales, usa a la Virtual Mascot para apuntar a cada uno de ellos y llama automáticamente a `OpenApplication()` sin que el niño tenga que hacer doble click.
- El sistema de progreso (`ProgressData`) desbloquea a la fuerza cada nivel durante la secuencia para que sean jugables.
- Se ha creado una nueva task `MoveMouseTask.cs` que pide al usuario que mueva el ratón por la pantalla una distancia concreta. Nota: usa el nuevo `UnityEngine.InputSystem` en lugar de `Input.mousePosition`.
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
* The assessment currently records the baseline scores but does not yet skip levels (as requested). In a future iteration, a `LevelProgressionManager` could read the `SkillManager` scores after the assessment to dynamically unlock Levels 1–5 based on proficiency.

==================================================
68. CLEAN GEOMETRIC INITIAL ASSESSMENT (MINIMALIST, TIME-AWARE, JSON-LOCALIZED)

**Context & Motivation:**
User feedback required decoupling the Initial Assessment Test from game minigames (such as dirt cleaning and animated targets) and removing the Virtual Mascot. The target audience includes children and novice users who may have never touched a mouse before. The test needed to feel clean, serious, and direct (Task > Action) with minimal cognitive load, configurable timeouts, fail-safes (Skip / Exit), and external JSON localization.

**Key Architecture & Design:**
* **Standalone Minimalist UI:** The assessment runs inside its own dedicated high-contrast, dark-slate UI (AssessmentCleanRoot), replacing all playful minigame elements.
* **No Virtual Mascot:** VirtualMascot.HideMascot() is enforced upon test start. There are no mascot interruptions or character animations.
* **English JSON Localization:** All UI labels and task definitions are loaded from Resources/AssessmentInstructions.json. Translating or adding new languages requires only modifying or providing a JSON file without recompiling code.
* **Per-Task Timeouts & Skip Option:**
  - Every task has an independent timeoutSeconds (e.g. 15s or 20s) configured in the JSON.
  - A real-time countdown progress bar is displayed at the top of the header (turning red in the last 3 seconds).
  - If a child struggles or remains inactive until the timer expires, the task is marked as timed out (0 points) and smoothly auto-advances to the next task after a subtle notice.
  - A persistent **Skip** button allows tutor/child to immediately bypass impossible tasks.
  - An **Exit** button allows ending the test at any time, preserving recorded skills.
* **Geometric Tasks Implementation:**
  1. **Move Cursor (mouse_move):** Clean blue circle. Hovering inside turns it green and completes the task.
  2. **Hover (hover):** Clean purple square. Hovering steadily fills an inner white gauge over 2.0s without clicking.
  3. **Single Click (click):** Clean orange square. Left-clicking completes the action.
  4. **Double Click (double_click):** Clean teal circle. Two rapid clicks within 0.45s complete the action.
  5. **Right Click (right_click):** Clean amber rectangle. Evaluates PointerEventData.InputButton.Right.
  6. **Click and Hold (click_hold):** Clean red circle. Holding down fills an inner gauge over 2.0s. Releasing early resets gauge and increments extra attempts.
  7. **Drag and Drop (drag_drop):** Blue square draggable into a gray target slot on the right using Unity's EventSystem drag handlers.
* **Immediate Evaluation Visibility:**
  - Upon completion or exit, a clean summary card displays every tested skill with its percentage score (e.g., 100%, 90%, or 0% (Timed out)).
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
    - timestamp (ISO UTC) and formattedTime (HH:mm:ss).
    - totalTasksTested, completedTasks, and calculated averageScore.
    - Detailed skills list: skillId, skillName, completed, score, extraAttempts, and timeTakenSeconds.
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
   - Added an armingDelay = 0.35f to HoverController. The gauge cannot begin filling during the first 0.35 seconds after shape instantiation, giving the player visual notice and ensuring that a stationary cursor does not instantly trigger completion.


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
   - Evaluated as timeRatio = timeTaken / currentTaskTimeout.
   - **Agility Threshold (timeRatio <= 0.30):** Completing the task within the first 30% of the timeout incurs **0 time penalty** (full 100% speed credit).
   - **Gradual Time Penalty (0.30 < timeRatio <= 1.0):** For hesitant or slower completion, a penalty scales linearly up to 35 points:
     timePenalty = ((timeRatio - 0.30f) / 0.70f) * 35f;
3. **Combined Final Score:**
   - finalScore = Mathf.Clamp(accuracyScore - timePenalty, 20f, 100f);
   - If timed out or skipped: finalScore = 0f.
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

==================================================
75. INITIAL ASSESSMENT FIRST-SCREEN ROUTING & STATS DASHBOARD SCENE

**Overview:**
Integrated the Initial Assessment Test directly into the core game loop as the mandatory first experience after the Main Menu for new players, and built a dedicated Stats & Assessment dashboard scene accessible anytime from the Main Menu.

**1. Main Menu First-Screen Flow:**
- **Routing Logic (`SceneSystem.cs`):**
  - Updated `SceneSystem.ChangeToMain()` (triggered by "Play" / `Btn-Play`) to check `PlayerPrefs.GetInt("InitialAssessmentCompleted", 0) == 0`.
  - If the initial assessment has not been completed, the game routes immediately to `AssessmentScene`.
  - Once completed or finished, `InitialTestManager` sets `PlayerPrefs.SetInt("InitialAssessmentCompleted", 1)` and calls `SceneSystem.ChangeToMain()`, smoothly advancing the player into `Desktop1` (or their saved level sequence).
  - Clicking "Reset Progress" in the Main Menu wipes `PlayerPrefs`, ensuring that subsequent "Play" actions correctly restart the player at the Initial Assessment.

**2. Dedicated Assessment Scene (`AssessmentScene.unity`):**
- Standalone clean scene containing:
  - `Main Camera` with soft neutral background (`#F1F5F9`).
  - `Canvas` and `EventSystem` configured with `InputSystemUIInputModule`.
  - `SceneManager` prefab (`SceneSystem`).
  - `Managers` prefab (`SkillManager`, `ProgressData`, `DesktopManager`, etc.).
  - `InitialTestManager` & `LocalizationManager`: Automatically starts the clean geometric assessment on load (`SceneManager.GetActiveScene().name == "AssessmentScene"`).
  - Exit button safely navigates back to `Menu.unity`, while completion/finish progresses to `Desktop1`.

**3. Stats & Reports Dashboard Scene (`StatsScene.unity` & `StatsSceneController.cs`):**
- Accessible from the Main Menu via the new "Skills & Reports" button.
- **Scene-Authored UI Hierarchy (constructed via Unity MCP):**
  - All UI elements (Canvas, Background, Header bar, navigation buttons, Left/Right cards, summary panels, and table containers) are authored as persistent GameObjects within `StatsScene.unity` using the Unity MCP server rather than being built purely in runtime procedural code.
  - `StatsSceneController.cs` binds to these pre-existing scene objects via `[SerializeField]` references (`backToMenuButton`, `retakeAssessmentButton`, `summaryBox`, `summaryText`, `avgScoreText`, `emptyStateText`, `assessmentTableContainer`, `skillsTableContainer`).
- **Header:**
  - "Back to Menu" button: navigates back to `Menu.unity`.
  - "Retake Assessment" button: directly loads `AssessmentScene` so users or evaluators can re-run diagnostics anytime.
- **Left Card (Assessment History):**
  - Scans and parses the latest diagnostic evaluation from `Assets/Assessment/*.json`.
  - Displays completion timestamp, tasks completed ratio, overall average proficiency percentage, and itemized task breakdowns (status, score, time taken, errors/extra attempts).
  - Displays friendly empty state guidance if no diagnostic tests have been run yet.
- **Right Card (Live Skill Mastery):**
  - Queries active skill proficiencies from `SkillManager` and `PlayerPrefs` (`SkillScore_{skillId}`).
  - Renders color-coded progress bars (High: Emerald Green >= 75%, Medium: Amber >= 45%, Beginner: Orange/Red < 45%).
  - Summarizes current proficiency score and skill level status.

**4. Main Menu Button (`Menu.unity`):**
- Added `Btn-Stats` ("Skills & Reports") into `MenuBtnLayout` directly between "Play" and "Reset Progress".
- Button is automatically managed by the layout's `VerticalLayoutGroup`.
- Persistent `onClick` event is wired to `SceneSystem.ChangeToStats()`.

**5. Build Settings:**
- Both `Assets/Scenes/AssessmentScene.unity` and `Assets/Scenes/StatsScene.unity` are registered and enabled in `EditorBuildSettings.asset`.

==================================================
76. RESET PROGRESS STANDALONE BUILD FIX & DIRECT DESKTOP TRANSITION

**1. Reset Progress Standalone Build Fix (`SceneSystem.cs`):**
- **Problem Identified:**
  - In `SceneSystem.ResetProgress()`, `PlayerPrefs.DeleteAll()` was previously located inside an `else` branch of `if (ProgressData.Instance != null)`. Because `ProgressData.Instance` exists at runtime, `PlayerPrefs.DeleteAll()` never executed.
  - Furthermore, `ProgressData.Instance.ClearHistory()` only deleted its own save key, leaving `PlayerPrefs.GetInt("InitialAssessmentCompleted") == 1` intact. Consequently, clicking "Reset Progress" in a standalone build failed to reset the assessment state, causing the "Play" button to bypass the Initial Assessment.
- **Solution Applied:**
  - Cached the active language selection (`SelectedLanguage`) before wiping.
  - Explicitly called `ProgressData.Instance?.ClearHistory()` to reset in-memory records and flags.
  - Explicitly called `SkillManager.Instance?.ResetAllScores()` to reset active scores in memory and PlayerPrefs.
  - Executed `PlayerPrefs.DeleteAll()` unconditionally.
  - Re-applied `SelectedLanguage`, and explicitly set all progression flags (`InitialAssessmentCompleted`, `AutoSequenceCompleted`, `DesktopTourCompleted`, `SemiGuidedSequenceCompleted`) to `0`.
  - Saved `PlayerPrefs` (`PlayerPrefs.Save()`), guaranteeing that clicking "Play" after "Reset Progress" consistently presents the Initial Assessment.

**2. Direct Desktop Transition Without Summary Screen (`InitialTestManager.cs`):**
- **Overview:**
  - Removed the intermediate evaluation summary screen at the conclusion of the Initial Assessment level to create a seamless, child-friendly flow directly into the desktop environment.
- **Implementation:**
  - In `InitialTestManager.SpawnCurrentTask()`, upon completing all tasks (`currentTaskIndex >= config.tasks.Count`), the manager now directly calls `CalculateAndStoreResults()`, closes any open window (`appWindow?.CloseWindow()`), and calls `FinishTest()`.
  - `CalculateAndStoreResults()` continues to evaluate accuracy/time metrics, updates `SkillManager`, sets `InitialAssessmentCompleted = 1`, and exports the diagnostic JSON report to `Assets/Assessment/{HH-mm-ss}.json`.

**3. Mid-Assessment Reset & Re-entry Fix (`InitialTestManager.cs` & `LocalizationManager.cs`):**
- **Problems Identified:**
  1. The Exit ("Salir") button on the assessment window header was previously calling `CalculateAndStoreResults()`. When a user exited midway through an assessment, it mistakenly marked `InitialAssessmentCompleted = 1`, preventing the assessment from showing again upon pressing Play.
  2. `LocalizationManager` was previously co-located with `InitialTestManager` on the `AssessmentManager` GameObject in `AssessmentScene.unity`. In `LocalizationManager.Awake()`, `DontDestroyOnLoad(gameObject)` made the entire `AssessmentManager` persist across scene unloads into `Menu`. When `AssessmentScene` was re-entered, the duplicate check destroyed the new scene's manager, while the old manager had stale/destroyed references and never re-triggered `Start()`.
- **Solutions Applied:**
  1. Removed `CalculateAndStoreResults()` from the Exit button callback in `InitialTestManager.cs`. Cancelling an assessment midway now cleanly closes the window or routes back to the Menu without marking it as completed.
  2. Removed `DontDestroyOnLoad(gameObject)` from `LocalizationManager.cs` and decoupled it from `AssessmentManager` in `AssessmentScene.unity`.
  3. Ensured `InitialTestManager.Start()` unconditionally resets `IsTestActive = false` and initializes `StartAssessment()` fresh whenever `AssessmentScene` is loaded.
  4. Added a safety cleanup in `SceneSystem.ResetProgress()` to destroy any lingering `InitialTestManager` GameObject.

==================================================
77. SIMULATED OPERATING SYSTEM (VIRTUALDESKTOP SCENE, START MENU & NATIVE APPS)

**Context & Objective:**
Following the guided tutorials in `Desktop1` and `Desktop2` that teach basic mouse manipulation (single click, hover, drag, resize, and recycling bin), the player transitions to `VirtualDesktop.unity`. This scene represents the true simulated operating system ("Windows Simulado"), offering a free, unguided desktop playground equipped with system tools, productivity apps, and minigames tailored for children.

**1. Scene Clean-up & Mode Isolation (`VirtualDesktop.unity`):**
* Removed legacy tutorial orchestrator components (`FirstDesktopTutorial` / `SecondDesktopTutorial`) from the `Managers` GameObject.
* Verified absence of any modal `TutorialBlocker` overlays.
* Registered `Assets/Scenes/VirtualDesktop.unity` into `EditorBuildSettings.asset`.
* Preserved the core UI infrastructure: reference Canvas (1920x1080), `WindowManager` (bound to `WindowsArea`), `IconGrid` (bound to `Panel`), `Taskbar`, and `Managers`.

**2. Start Menu System (`StartMenuController.cs`):**
* **Start Button (`StartButton`):** Converted `DesktopLabel` on the bottom-left of the `Taskbar` into an interactive uGUI `Button` labeled "❖ Inicio" (#1E293B background with hover states).
* **Start Menu Panel (`StartMenuPanel`):** Authored a modern dark slate (#0F172A) popup window anchored directly above the Taskbar (300 x 390 px) featuring:
  - Header: "Mi Ordenador" with sub-bar styling.
  - App Shortcuts:
    * 📁 Explorador de Archivos
    * 📝 Bloc de Notas
    * 🎮 Centro de Minijuegos
  - System Actions:
    * 📊 Estadísticas y Logros (routes to `SceneSystem.ChangeToStats()`).
    * 🚪 Cerrar Sesión / Salir (routes to `SceneSystem.ChangeToMenu()`).
* **Auto-Close Behavior:** Automatically detects clicks outside the panel or button clicks to close the menu cleanly.

**3. Native Application Prefabs & Content Architecture:**
* **Bloc de Notas (`NotepadApp.cs` & `NotepadContent.prefab`):**
  - Full-window typing sandbox with multiline `TMP_InputField`.
  - Educational character and word counters updated in real-time (`Caracteres: X | Palabras: Y`).
  - Action buttons: "Limpiar" (resets text) and "Guardar" (displays feedback confirmation).
* **Explorador de Archivos (`FileExplorerApp.cs` & `FileExplorerContent.prefab`):**
  - Top navigation bar with "← Atrás" button and dynamic path display (`Este Equipo > Archivos > ...`).
  - Hierarchical folder model: `Documentos` (text notes), `Imágenes`, and `Minijuegos`.
  - Double-click file handling: opens text files in Notepad, images in viewer, or launches minigames.
* **Centro de Minijuegos (`MinigamesHubApp.cs` & `MinigamesHubContent.prefab`):**
  - Grid of interactive cards for rapid minigame launching:
    * Dianas y Clics (`L_Level3`)
    * Seguimiento y Hover (`L_Level2`)
    * Doble Clic Rápido (`L_Level5`)
    * Entrenamiento Adaptativo (`L_Level4`)
  - "¡Jugar!" buttons dynamically spawn generic windows and inject `LevelRunner`.

**4. Desktop Icons Setup (`DesktopManager.cs`):**
* Configured `DesktopManager.initialDesktopIcons` to populate the free desktop with OS applications:
  1. **Archivos** (`Folder_Icon01.png` -> `FileExplorerContent.prefab`)
  2. **Bloc de Notas** (`Text_File_Icon_W01_Scaled.png` -> `NotepadContent.prefab`)
  3. **Minijuegos** (`Maze_Minigame_Icon_01_Scaled.png` -> `MinigamesHubContent.prefab`)
  4. **Papelera** (`RecyclingBin_Icon_01.png` -> `FileExplorerContent.prefab`)

**5. Progression & Scene Routing:**
* **`SceneSystem.cs`:**
  - Added `SceneNames.VirtualDesktop`.
  - In `ChangeToMain()`, when `PlayerPrefs.GetInt("SemiGuidedSequenceCompleted", 0) == 1`, players directly load `VirtualDesktop`.
  - Added public helper `ChangeToVirtualDesktop()`.
* **`AdvancedDesktopTour.cs`:**
  - Updated completion transition from placeholder `PostTutorialScene` to `VirtualDesktop`.

**6. Editor Automation Tool (`VirtualDesktopSetup.cs`):**
* Created a one-click setup tool under `ComputerLearning -> Setup Virtual Desktop OS` to construct prefabs, wire serialized references, and rebuild the scene hierarchy deterministically.

==================================================
78. DEEP FILE EXPLORER ARCHITECTURE & RIGHT-CLICK CONTEXT MENU (FOLDER CREATION)

**Context & Motivation:**
User prioritized building a robust, educational simulated file system and implementing an OS-style Right-Click Context Menu. In this phase, the right-click menu is purposefully focused exclusively on creating folders ("📁 Nueva Carpeta"), allowing children to practice hierarchical organization, right-clicking, and mouse dexterity in a safe environment before text file authoring is introduced.

**1. Context Menu System (`ContextMenuController.cs`):**
* **Dynamic Placement & Boundary Clamping:** A dedicated uGUI popup panel (`ContextMenuPanel`) anchored to the Canvas. Converts screen coordinates via `RectTransformUtility.ScreenPointToLocalPointInRectangle` and automatically clamps positioning so the popup never clips outside display edges.
* **Dismissal Rules:** Automatically hides when the user left-clicks or right-clicks outside the menu boundaries, or presses the `Escape` key.
* **Focused Action:** Currently features exclusively the **"📁 Nueva Carpeta"** button with hover highlights, modern slate styling (`#101827`), and high-contrast typography.

**2. Desktop Right-Click Trigger (`DesktopContextMenuTrigger.cs`):**
* Attached to both `Background` and `Panel` in `VirtualDesktop.unity`.
* Detects right clicks (`eventData.button == PointerEventData.InputButton.Right`) on any empty desktop area.
* **Folder Spawning Logic:** When "Nueva Carpeta" is chosen, it automatically calculates unique naming ("Nueva Carpeta", "Nueva Carpeta (1)", etc.), spawns a new `DraggableIcon` via `DesktopManager.Instance.SpawnIcon`, assigns `Folder_Icon01.png`, and wires `FileExplorerContent.prefab`. The newly created folder automatically snaps into the `IconGrid`.

**3. Deep File Explorer Overhaul (`FileExplorerApp.cs` & `FileExplorerContent.prefab`):**
* **Hierarchical Virtual Directory Model:** Built with an extensible tree structure (`ExplorerItemData` / `ExplorerFileType`) supporting folders, text notes, images, and minigames.
* **In-Window Right-Click Context Menu:** `FileExplorerApp` implements `IPointerClickHandler`. Right-clicking anywhere within the empty area of the Explorer window summons the Context Menu to create new subfolders inside the currently open directory.
* **Toolbar & Navigation:**
  - "← Atrás" button with disabled state when at the root directory.
  - Interactive breadcrumbs path display (`📁 Este Equipo > Documentos > ...`).
  - Prominent "📁 + Nueva Carpeta" button on the top toolbar for accessible folder creation.
* **Item Selection & Double-Click Flow:**
  - Single click selects the file/folder with a subtle highlight (`#38BDF8` tint).
  - Double-click (< 0.45s) opens the item: entering subfolders, launching Notepad for text files, or launching minigames.
* **Dynamic Status Footer:** Bottom bar dynamically displays item count ("X elementos").

**4. Editor Setup Synchronization (`VirtualDesktopSetup.cs`):**
* Updated the `ComputerLearning -> Setup Virtual Desktop OS` MenuItem to construct, bind, and serialize the `ContextMenuPanel`, desktop triggers, and updated `FileExplorerContent.prefab` deterministically.

==================================================
79. MINIGAMES FOLDER ARCHITECTURE & PERSISTENT COMPANION MASCOT

**Context & Motivation:**
User requested two essential desktop operating system features:
1. Encapsulate all educational minigames inside a dedicated Desktop Folder ("Minijuegos") rather than leaving them as loose standalone applications or separate buttons on the desktop grid.
2. Position the Virtual Mascot permanently in the top-right corner of the screen as a desktop pet/companion that greets the child, bobs gently in idle, and speaks helpful tips, instructions, and encouraging phrases upon being clicked.

**1. Minigames Folder Architecture (`FileExplorerApp.cs` & `MinigamesFolderContent.prefab`):**
* **Desktop Folder Integration:**
  - In `DesktopManager.initialDesktopIcons`, the "Minijuegos" icon is now configured as a `DesktopItemType.Folder` utilizing `Folder_Icon01_Purple.png`.
  - When double-clicked from the desktop, it opens `MinigamesFolderContent.prefab`, which is initialized with `initialFolderName = "Minijuegos"`.
  - The Explorer window opens immediately focused on the `Minijuegos` subfolder, showing path breadcrumbs `📁 Este Equipo > Minijuegos`, active "← Atrás" navigation button, and footer item count.
* **Direct Minigame Execution from Explorer:**
  - The `Minijuegos` directory contains four modular educational games:
    1. **Dianas y Clics:** `Target_Minigame_Icon01_Scaled.png` wired to `L_Level3.asset` (Single click / target accuracy).
    2. **Seguimiento y Hover:** `Maze_Minigame_Icon_01_Scaled.png` wired to `L_Level2.asset` (Path tracking / hover).
    3. **Doble Clic Rápido:** `Target_Minigame_Icon01_Scaled.png` wired to `L_Level5.asset` (Speed / double click).
    4. **Entrenamiento Adaptativo:** `Maze_Minigame_Icon_01_Scaled.png` wired to `L_Level4.asset` (Dynamic session).
  - Double-clicking any minigame inside the folder dynamically instantiates a `BaseWindow`, attaches `LevelRunner`, and calls `runner.StartLevel(item.levelToLaunch)`, launching the interactive minigame right inside that window.

**2. Persistent Companion Mascot (`VirtualMascot.cs`):**
* **Companion Mode:**
  - Added `enableMascotCompanion` to `DesktopManager.cs`. In `VirtualDesktop.unity`, this automatically instantiates the mascot in companion mode.
  - Positioned permanently in the top-right corner: `anchorMin = (1, 1)`, `anchorMax = (1, 1)`, `pivot = (1, 1)`, `anchoredPosition = (-45, -45)`.
  - Layered on the desktop surface directly before `WindowsArea` so it is clearly visible on the desktop without occluding or interfering with opened application controls (like the window close 'X' button).
  - Idle breathing/bobbing animation: `anchoredPosition = companionCornerOffset + new Vector2(0f, Mathf.Sin(Time.unscaledTime * 3f) * 6f)`.
* **Click Interaction & Animated Speech (`IPointerClickHandler`):**
  - Enabled raycasts on `CanvasGroup` (`blocksRaycasts = true`, `interactable = true`) and `MascotImage`.
  - Clicking the mascot plays an elastic squash-and-stretch bounce reaction coroutine (`Vector3(1.22, 0.82, 1)` -> `Vector3(0.88, 1.18, 1)` -> `Vector3(1, 1, 1)`).
  - Speech bubble dynamically displays to the left/below the mascot (`pivot = (1, 1)`, `anchoredPosition = (-110, -20)`) with automatic text wrapping and layout rebuild.
  - Automatically hides after 5.0 seconds or refreshes immediately if clicked again.
* **Bilingual Dialogue Pool:**
  - Supports English and Spanish via `LocalizationManager.CurrentLanguage`.
  - Features 10 varied child-friendly tips and encouraging remarks (using the mouse, right-clicking for folders, keyboard typing in Notepad, maximizing windows, and friendly greetings).
  - Maintains random selection with a non-repeating constraint so consecutive clicks cycle through different advice.
* **Persistent Visibility:**
  - `VirtualMascot.HideMascot()` and `HideMascotIfTargeting()` now explicitly preserve the mascot if `isDesktopCompanion` is active, guaranteeing that opening windows or clicking icons never hides the companion.

**3. Automation & Synchronization (`VirtualDesktopSetup.cs`):**
* Updated `VirtualDesktopSetup` to automatically:
  - Configure `FileExplorerContent.prefab` with serialized minigame levels and icons.
  - Generate `MinigamesFolderContent.prefab` with `initialFolderName = "Minijuegos"`.
  - Configure `DesktopManager` with the updated icons array and `enableMascotCompanion = true`.
  - Save the clean scene `VirtualDesktop.unity`.

==================================================
80. EXPLORER UI REFINEMENT, PREFAB STANDARDIZATION & FONT GLYPH FIXES

**Context & Problems Identified:**
After testing the File Explorer and desktop minigames folder in `VirtualDesktop.unity`, several visual and layout defects were identified from screenshots:
1. **Broken Toolbar Alignment:** The "← Atrás" button was protruding 28px outside the left window border.
2. **Missing Font Glyphs (`□`):** Unity's default `LiberationSans SDF` font asset does not include emoji glyphs like `📁` (U+1F4C1) or special arrows (`←`), rendering empty square boxes `□` across `PathText` and button labels.
3. **Transparent / Invisible Icons:** Explorer items appeared virtually invisible (rendered at 5% opacity) without their text names.
4. **Start Menu Typography:** Menu buttons relied on emoji characters instead of dedicated graphical sprites.

**Root Causes:**
* **Icon Opacity:** When `itemPrefab` was unassigned, the procedural fallback in `FileExplorerApp.CreateItemUI` created a card background with `color = new Color(1, 1, 1, 0.05f)` and assigned the sprite to `GetComponentInChildren<Image>()`, which resolved to this 5% translucent card background. Additionally, no `TextMeshProUGUI` child was created for the item label.
* **Button Anchor & Pivot:** In `VirtualDesktopSetup.EnsureExplorerPrefab`, `btnBackGo` had its pivot set to `(0.5, 0.5)` with `anchoredPosition = (10, 0)`. With a width of 76px, its left edge landed at `10 - 38 = -28px` outside the container.
* **Selection Tinting:** `SelectItem` and `ClearSelection` previously modified the main `Image` color without isolating the background from the icon sprite.

**Solutions & Implementation:**

**1. Standalone Explorer Item Prefab (`ExplorerItemPrefab.prefab`):**
* Created a dedicated prefab in `Assets/Prefabs/Windows/ExplorerItemPrefab.prefab`:
  - Root: 110 x 110 px with transparent `Image` (acting as selection highlight surface) and `Button`.
  - Child `Icon`: 64 x 64 px `Image` positioned at top with `color = Color.white` (100% full solid opacity, `preserveAspect = true`, `raycastTarget = false`).
  - Child `Label`: `TextMeshProUGUI` anchored at bottom with 12pt dark slate typography (`#0F172A`), centered alignment, and ellipsis overflow.
* Bound `itemPrefab` to both `FileExplorerContent.prefab` and `MinigamesFolderContent.prefab`.

**2. Procedural Fallback Parity (`FileExplorerApp.cs`):**
* Rewrote `CreateItemUI` so that both prefab and procedural code paths instantiate isolated `Icon` and `Label` child GameObjects.
* In `SelectItem` and `ClearSelection`, only the root card background is toggled between `Color.clear` and light sky blue (`#3399FF` at 25% alpha), leaving the icon and label completely untouched.

**3. Window Toolbar & Typography Fixes (`VirtualDesktopSetup.cs`):**
* **Back Button:** Set `pivot = (0, 0.5f)`, `anchoredPosition = (12, 0)`, and size `(80, 32)`. Stretched the child text across the entire button with `< Atrás` in bold ASCII.
* **Path Breadcrumbs:** Adjusted `offsetMin = (104, 0)` and `offsetMax = (-160, 0)`. Replaced emoji strings with clean ASCII: `Este Equipo > Archivos` (and `Este Equipo > Minijuegos`).
* **Toolbar & Context Menu Buttons:** Changed labels to `+ Nueva Carpeta` (no unicode emoji).
* **Start Menu Icons:** Refactored `CreateMenuButton` to dynamically instantiate a dedicated 24 x 24 px `Image` using the actual application sprite (`folderIcon`, `notepadIcon`, `minigamesIcon`) with 44px text padding, removing emojis completely.

**4. Verification:**
* Reimported assets and executed `ComputerLearning -> Setup Virtual Desktop OS`.
* Inspected Unity console: 0 errors and 0 font fallback warnings.
* Verified `VirtualDesktop.unity` scene saved cleanly.

==================================================
81. FULL SYSTEM LOCALIZATION TO ENGLISH (SIMULATED OS & APPS)

**Context & Motivation:**
To align with the project's primary English presentation standard during the Erasmus semester and `LocalizationManager.CurrentLanguage` default ("EN"), all text elements across `VirtualDesktop.unity`, its native applications, virtual file system, and interactive menus were translated to English.

**1. Virtual Desktop & Taskbar:**
* **Start Button:** Labeled "Start" (was "Inicio").
* **Start Menu Header:** "My Computer" (was "Mi Ordenador").
* **Start Menu Items:**
  - "File Explorer" (was "Explorador de Archivos")
  - "Notepad" (was "Bloc de Notas")
  - "Minigames Hub" (was "Centro de Minijuegos")
  - "Stats & Achievements" (was "Estadísticas y Logros")
  - "Sign Out / Exit" (was "Cerrar Sesión / Salir")
* **Initial Desktop Icons:** "Files", "Minigames", "Notepad", and "Recycle Bin".

**2. File Explorer & Virtual File System (`FileExplorerApp.cs`):**
* **Toolbar & Status:**
  - Navigation: "< Back" (was "< Atrás") and "+ New Folder" (was "+ Nueva Carpeta").
  - Breadcrumbs: "This PC > Files" and "This PC > Minigames" (was "Este Equipo > ...").
  - Footer counter: "X items" (was "X elementos").
* **Default Directory Hierarchy:**
  - Root: "Files" (was "Archivos").
  - "Documents" (was "Documentos") containing:
    * `Welcome.txt`: "Welcome to your virtual computer!\nHere you will learn how to use all the tools."
    * `Mouse_Guide.txt`: "Tip: Use left click to select and double click quickly to open programs."
  - "Pictures" (was "Imágenes") containing `Mascot.png`.
  - "Minigames" (was "Minijuegos") containing:
    * `Targets & Clicks` (`L_Level3`)
    * `Tracking & Hover` (`L_Level2`)
    * `Fast Double Click` (`L_Level5`)
    * `Adaptive Practice` (`L_Level4`)
* **Folder Creation:** Default naming scheme updated to "New Folder", "New Folder (1)", etc.

**3. Context Menu (`ContextMenuController.cs` & `DesktopContextMenuTrigger.cs`):**
* Popup button: "+ New Folder" (was "+ Nueva Carpeta").
* Desktop right-click trigger automatically spawns folders named "New Folder".

**4. Notepad Application (`NotepadApp.cs`):**
* Default document: "Welcome to Notepad!\n\nHere you can practice typing on your keyboard. Try typing your name and your favorite games!"
* Action button: "Clear" (was "Limpiar").
* Feedback message: "Note saved successfully!" (was "¡Nota guardada con éxito!").
* Live counters: "Characters: X | Words: Y" (was "Caracteres: X | Palabras: Y").

**5. Minigames Hub (`MinigamesHubApp.cs`):**
* Game Cards: "Targets & Clicks", "Tracking & Hover", "Fast Double Click", "Adaptive Practice".
* Action buttons: "Play!" (was "¡Jugar!").

**6. Verification & Persistence:**
* Rebuilt all content prefabs via `ComputerLearning -> Setup Virtual Desktop OS`.
* Tested Play mode: verified English strings on spawned desktop icons, Start menu, File Explorer, and companion mascot.
* Saved `VirtualDesktop.unity` scene cleanly.




==================================================
82. RESPONSIVE WINDOW RESIZING, VIEWPORT MASKING & MINIMUM BOUNDS

**Context & Motivation:**
When resizing application windows to small or compact sizes in `VirtualDesktop.unity`, several UI layout and clipping defects emerged:
1. **Vertical Text Collapse in Explorer Toolbar:** The path breadcrumb label ("This PC > Files") collapsed into a single-character-wide vertical column.
2. **Toolbar Overlap & Rect Inversion:** The Back button (80px), Path text, and New Folder button (140px) previously relied on manual pixel anchors with static offsets (`offsetMin.x = 104`, `offsetMax.x = -160`). When the window width dropped below 264px, the calculated rect width became negative, scrambling the text layout and causing buttons to overlap.
3. **Item Bleeding Over Window Borders:** The item grid (`ItemsContainer`) was parented directly to the window root without clipping. Shrinking window height caused icons to hang out past the window frame and render onto the desktop wallpaper.
4. **Overly Permissive Minimum Window Size:** Window resize handles allowed shrinking windows down to 300x200 px or smaller, which was too compact for a toolbar, title bar, and grid.

**Root Causes & Architectural Solutions:**

**1. Responsive Toolbar Layout (`HorizontalLayoutGroup` & `LayoutElement`):**
* Converted the Explorer `TopBar` into a `HorizontalLayoutGroup` (padding `10, 10, 7, 7`, spacing `8`, `childControlWidth = true`, `childControlHeight = true`, `childForceExpandHeight = true`).
* Assigned explicit `LayoutElement` components to all toolbar items:
  - **Back Button:** `LayoutElement` (`preferredWidth = 72`, `minWidth = 60`, `flexibleWidth = 0`).
  - **Path Breadcrumb Text:** `LayoutElement` (`flexibleWidth = 1`, `minWidth = 40`), `textWrappingMode = TextWrappingModes.NoWrap`, `overflowMode = TextOverflowModes.Ellipsis`. Takes all remaining horizontal space without pushing buttons out of view and truncates with "..." if constrained.
  - **New Folder Button:** `LayoutElement` (`preferredWidth = 110`, `minWidth = 85`, `flexibleWidth = 0`).
* Overlaps, collisions, and negative rect widths are now geometrically impossible regardless of window size.

**2. Viewport Masking & Elastic Scrolling (`ScrollRect` & `RectMask2D`):**
* **File Explorer (`FileExplorerContent.prefab`):** Created a scrollable `ScrollView` hierarchy anchored between the top bar and status footer (`offsetMin = (0, 26)`, `offsetMax = (0, -42)`). Attached `ScrollRect` (vertical only, elastic) and `RectMask2D` to `Viewport`. Added `ContentSizeFitter` (`verticalFit = PreferredSize`) to `ItemsContainer`.
* **Minigames Hub (`MinigamesHubContent.prefab`):** Wrapped `GamesGrid` inside a masked `ScrollView` with `RectMask2D` and `ContentSizeFitter`.
* Icons and grid items are strictly clipped inside the window frame and can be smoothly scrolled vertically when the window height is reduced.

**3. Window Minimum Size Boundary Clamping (`ResizeHandle.cs` & `BaseWindow.prefab`):**
* In `ResizeHandle.cs`, updated default minimum window size from `(300, 200)` to a robust `(380, 260)`.
* Exposed public property `MinSize { get => minSize; set => minSize = value; }`.
* Updated `VirtualDesktopSetup.cs` to iterate across all 8 resize handles in `BaseWindow.prefab` (Top, Bottom, Left, Right, and 4 corners), serializing `minSize = new Vector2(380, 260)`.

**4. Deprecation & Clean Compilation:**
* Replaced Unity 6 obsolete `TMP_Text.enableWordWrapping = false` with `TMP_Text.textWrappingMode = TextWrappingModes.NoWrap`.
* Executed `ComputerLearning -> Setup Virtual Desktop OS` and verified 0 errors and 0 warnings in the Unity console.

==================================================
83. SCENE PROGRESSION CHAIN SYNCHRONIZATION (DESKTOP1 -> TUTORIAL1 -> DESKTOP2 -> TUTORIAL2 -> VIRTUALDESKTOP)

**Context & Problem:**
Following a project reorganization that standardized scene naming, the transitions between tutorial and desktop scenes became disjointed:
- Upon completing `Desktop1`, the game attempted to load `IntroTutorialScene` (an old scene name), breaking the flow into `Tutorial1`.
- `DesktopTour` in `Tutorial1` attempted to load `SecondDesktopScene` instead of `Desktop2`.
- `SecondDesktopTutorial` in `Desktop2` attempted to load `SecondTutorial` instead of `Tutorial2`.
- In `ProjectSettings/EditorBuildSettings.asset`, `VirtualDesktop.unity` had `enabled: 0`, preventing it from properly participating in builds and scene lookups.

**Solutions & Implementation:**

**1. Desktop1 -> Tutorial1 Transition (`FirstDesktopTutorial.cs`):**
* Updated the sequence completion in `AutoOnboardingSequence()`:
  - Sets `PlayerPrefs.SetInt("AutoSequenceCompleted", 1)`.
  - Saves `PlayerPrefs.Save()`.
  - Explicitly executes `UnityEngine.SceneManagement.SceneManager.LoadScene("Tutorial1")`.

**2. Tutorial1 -> Desktop2 Transition (`DesktopTour.cs`):**
* Updated `DesktopTour.TourSequence()` completion:
  - Sets `PlayerPrefs.SetInt("DesktopTourCompleted", 1)`.
  - Saves `PlayerPrefs.Save()`.
  - Explicitly executes `UnityEngine.SceneManagement.SceneManager.LoadScene("Desktop2")`.

**3. Desktop2 -> Tutorial2 Transition (`SecondDesktopTutorial.cs`):**
* Updated `SemiGuidedOnboardingSequence()` completion:
  - Sets `PlayerPrefs.SetInt("SemiGuidedSequenceCompleted", 1)`.
  - Saves `PlayerPrefs.Save()`.
  - Explicitly executes `UnityEngine.SceneManagement.SceneManager.LoadScene("Tutorial2")`.

**4. Tutorial2 -> VirtualDesktop Transition (`AdvancedDesktopTour.cs`):**
* Confirmed and secured completion in `AdvancedDesktopTour.Start()`:
  - Sets `PlayerPrefs.SetInt("AdvancedDesktopTourCompleted", 1)`.
  - Saves `PlayerPrefs.Save()`.
  - Seamlessly routes into `UnityEngine.SceneManagement.SceneManager.LoadScene("VirtualDesktop")`.

**5. Central Scene Controller Synchronization (`SceneSystem.cs`):**
* Expanded `SceneNames` enum and `ChangeScene()` to include all standard scene names: `Desktop1`, `Tutorial1`, `Desktop2`, `Tutorial2`, `VirtualDesktop`.
* Updated `ChangeToMain()` stateful routing logic:
  - If `InitialAssessmentCompleted == 0` -> `AssessmentScene`.
  - Else if `AdvancedDesktopTourCompleted == 1` -> `VirtualDesktop`.
  - Else if `SemiGuidedSequenceCompleted == 1` -> `Tutorial2`.
  - Else if `DesktopTourCompleted == 1` -> `Desktop2`.
  - Else if `AutoSequenceCompleted == 1` -> `Tutorial1`.
  - Default -> `Desktop1`.
* Updated `ResetProgress()` to cleanly reset `AdvancedDesktopTourCompleted = 0` alongside other progression flags.

**6. Build Settings & Verification:**
* In `ProjectSettings/EditorBuildSettings.asset`, enabled `Assets/Scenes/VirtualDesktop.unity` (`enabled: 1`).
* Reimported all modified scripts with 0 errors and 0 warnings.

==================================================
84. VIRTUAL DESKTOP PROGRESSION SYSTEM: LEVELS FOLDER, NESTED MOUSE LEVELS, LEVEL 10 (CTRL+C / CTRL+V), AND SELECTIVE PULSING ANIMATION

**Context & User Requirements:**
To balance player agency in the open-ended `VirtualDesktop.unity` simulated operating system with guided pedagogical progression:
1. **Levels Desktop Folder:** A prominent "Levels" folder must reside on the desktop.
2. **Subfolder Hierarchy:** Inside "Levels", all subsequent levels and challenges are organized, including a dedicated "Mouse Levels" subfolder housing the original 9 mouse levels (`Level 1` through `Level 9`).
3. **Selective Icon Pulsing Animation:** Instead of all unlocked levels pulsating at once, only the single *next* level in progression must perform the pulsing breathing animation. Furthermore, if a pending level is inside a folder, that parent folder (e.g., "Levels" on the desktop, and "Mouse Levels" inside the explorer) must pulse to guide the child player directly to the objective. Once completed, the animation transitions immediately to the next task.
4. **Level 10 (Keyboard Shortcuts - Ctrl+C / Ctrl+V):** A didactic provisional level teaching standard clipboard shortcuts (`Ctrl + C` to copy, `Ctrl + V` to paste).

---

**Architectural Solutions & Implementation:**

**1. Centralized Level Progression Tracking (`ProgressData.cs`):**
* Added canonical level progression list:
  `public static readonly string[] LevelOrder = { "level1", "level2", "level3", "level4", "level5", "level6", "level7", "level8", "level9", "level10" };`
* Added `completedLevels` list in `ProgressData` and persistent serialization wrapper `ProgressDataWrapper`.
* Backward Compatibility Fallback: If existing players completed onboarding or tutorial sequences, `ProgressData` automatically populates tutorial-associated levels (`level1` to `level9`) upon load.
* Query & Event API:
  - `bool IsLevelCompleted(string levelId)`
  - `void MarkLevelCompleted(string levelId)`: Appends completed level, writes to JSON disk storage, and triggers `OnProgressChanged`.
  - `string GetCurrentNextLevelId()`: Returns the first uncompleted level ID from `LevelOrder`, or `null` if all levels are mastered.
* Auto-completion in `LevelRunner.cs`: When a task sequence finishes (`SpawnNextTask()` completion branch), `ProgressData.Instance.MarkLevelCompleted(currentLevel.levelId)` is invoked automatically.

**2. Breathing Scale Component (`PulsingIcon.cs`):**
* Reusable component attached to desktop icons and explorer item views.
* Computes an unscaled sinusoidal breathing scale:
  `scale = baseScale * (1f + Mathf.Sin(Time.unscaledTime * frequency) * amplitude)` (with `frequency = 3f`, `amplitude = 0.08f`).
* Includes `SetPulsing(bool)` with smooth reset to `Vector3.one` upon deactivation.

**3. Desktop Icon Progression Guidance (`DraggableIcon.cs`):**
* Updated `Update()` to enforce selective breathing:
  - If the icon represents a level (`levelDefinition != null`): only pulses if `levelDefinition.levelId == ProgressData.Instance.GetCurrentNextLevelId()`.
  - If the icon represents the desktop "Levels" folder: pulses whenever `GetCurrentNextLevelId() != null` (any level remains incomplete).
  - All other desktop icons remain static.

**4. File Explorer Hierarchical Pulsing (`FileExplorerApp.cs`):**
* Subscribed to `ProgressData.Instance.OnProgressChanged` on `OnEnable`/`OnDisable` to dynamically refresh folder icons when levels complete.
* Recursive level detection:
  `bool FolderContainsLevel(ExplorerItem folderItem, string levelId)` recursively inspects folder hierarchies to identify if a folder hosts the target level.
* In `CreateItemUI()`:
  - Levels only pulse if their `levelId == nextLevelId`.
  - Folders pulse if `FolderContainsLevel(item, nextLevelId)` evaluates to true (e.g., "Mouse Levels" pulses when any of Level 1-9 is the next goal).
* Built initial filesystem hierarchy in `BuildInitialFileSystem()`:
  - Root: "Levels", "Minigames", "Documents".
  - Inside "Levels":
    - Subfolder "Mouse Levels": populated with `L_Level1` through `L_Level9`.
    - Level item: `Level 10: Copy & Paste` (`L_Level10`).

**5. Level 10 Didactic Challenge (`CopyPasteTask.cs`):**
* Procedural uGUI didactic interface:
  - Instruction header with real-time step guidance.
  - Source box containing a bright, rotatable yellow star sticker with interactive hover/click feedback.
  - Destination paste box with animated dashed cyan border.
  - Visual keyboard badge indicators: `[Ctrl]`, `[C]`, and `[V]` which dynamically light up with green borders and glow when pressed on the physical keyboard.
* Input detection: Supports both `UnityEngine.InputSystem.Keyboard.current` and legacy input fallback.
* Progression steps:
  1. Detects `Ctrl + C`: Highlights badges, emits chime, stores clipboard state, and prompts the player to paste.
  2. Detects `Ctrl + V`: Animates the star sticker into the target zone with a bounce effect, emits celebratory particles/sound, and completes the task with `Complete(true)`.

**6. Automated Pipeline & Asset Setup (`VirtualDesktopSetup.cs`):**
* `EnsureCopyPasteLevelAssets()`:
  - Generates `SK_KeyboardShortcuts.asset` (`SkillDefinition`).
  - Generates `Task_CopyPaste.prefab` with `CopyPasteTask` component.
  - Generates `T_CopyPaste.asset` (`TaskDefinition`).
  - Generates `L_Level10.asset` (`LevelDefinition`).
* `EnsureLevelsFolderPrefab()`:
  - Creates `LevelsFolderContent.prefab` configured with `initialFolderName = "Levels"`.
  - Registers the "Levels" folder icon as the primary icon in `DesktopManager.initialDesktopIcons`.
  - Configures `FileExplorerContent.prefab` with references to `mouseLevels` (1..9) and `level10CopyPaste`.
* Executed `ComputerLearning -> Setup Virtual Desktop OS` in the Unity Editor: 0 compilation errors, clean scene save.

==================================================
85. VISUAL DARKENING AND ACCESS RESTRICTION FOR LOCKED LEVELS (FILE EXPLORER, DESKTOP & MINIGAMES HUB)

**Context & User Requirements:**
To visually distinguish available and active challenges from locked future challenges in the simulated operating system (`VirtualDesktop.unity`):
* Levels that have not yet been unlocked by the player must appear darkened ("oscurecidos").
* Locked levels must not be launchable by the player until prerequisites are completed.
* The visual feedback must be clear, intuitive, and responsive to progression changes.

---

**Implementation Details:**

**1. Central Progression State Synchronization (`ProgressData.cs`):**
* Reinforced singleton access with fallback lazy-instantiation (`FindAnyObjectByType<ProgressData>()` and `DontDestroyOnLoad`) so progress checks are guaranteed valid in all standalone or direct editor playtests.
* Upgraded `IsLevelUnlocked(string levelId)`:
  - Empty / null IDs return `true`.
  - Level 1 is always unlocked by default.
  - Returns `true` if `levelId` is recorded in `completedLevels` or `unlockedLevels`.
  - Always returns `true` for the active next level in sequence (`GetCurrentNextLevelId()`).
  - Returns `false` for any subsequent locked levels.

**2. File Explorer Visual Darkening & Lock Badging (`FileExplorerApp.cs`):**
* In `CreateItemUI(ExplorerItemData item)`:
  - Detects if the item is a minigame/level and evaluates `isUnlocked = ProgressData.Instance.IsLevelUnlocked(item.levelToLaunch.levelId)`.
  - **Darkened Icon Visuals:** When locked, `iconImg.color = new Color(0.28f, 0.28f, 0.28f, 0.7f)`. Full-brightness `Color.white` is reserved for unlocked levels.
  - **Dimmed Card Opacity:** Attaches/configures `CanvasGroup` on the item GameObject (`alpha = 0.6f` when locked, `1.0f` when unlocked).
  - **Dimmed Label & Status Tag:** Text color dims to `new Color(0.45f, 0.45f, 0.45f, 0.7f)` and appends `<color=#777777>[Locked]</color>`.
  - **Pulsing Guard:** The pulsing animation (`PulsingIcon`) is restricted exclusively to unlocked levels matching the active target.
* Double-Click / Selection Enforcement:
  - If a player clicks a locked item, the status bar displays: `"{item.name} (Locked - Complete previous levels first)"`.
  - If a player double-clicks a locked item, execution is prevented and an alert is displayed in the status footer: `"<color=#D9534F>Locked: Complete previous levels to unlock {item.name}</color>"`.
  - `OpenMinigame(ExplorerItemData item)` additionally validates `IsLevelUnlocked` before opening any window.

**3. Desktop Icon Darkening (`DraggableIcon.cs`):**
* In `Start()` and `Update()`:
  - Evaluates `actuallyLocked` dynamically against `ProgressData.Instance.IsLevelUnlocked`.
  - Dims the icon graphic: `img.color = actuallyLocked ? new Color(0.28f, 0.28f, 0.28f, 0.7f) : originalColor`.
  - Dims the `CanvasGroup.alpha = 0.6f`.
  - Dims the child label text to `new Color(0.5f, 0.5f, 0.5f, 0.7f)`.
  - Drags and click executions remain blocked while locked.

**4. Minigames Hub Synchronization (`MinigamesHubApp.cs`):**
* Added `UpdateButtonStates()` subscribed to `ProgressData.Instance.OnProgressChanged`.
* Each activity button (`Targets & Clicks`, `Tracking & Hover`, `Fast Double Click`, `Adaptive Practice`) automatically disables (`interactable = false`), applies `CanvasGroup.alpha = 0.5f`, and tints its image dark gray if its target level is locked.
* `LaunchLevel()` validates unlock status before spawning windows.

**5. Verification:**
* Verified in Unity Editor with 0 errors and 0 warnings.

==================================================
86. VIRTUAL DESKTOP SYSTEM POLISH & BUG FIXES: DYNAMIC WINDOW TITLES, DUPLICATE WINDOW PREVENTION, CONTEXT MENU RIGHT-CLICK RELAYS, AND RECYCLE BIN

**Context & Issues Identified:**
1. **Generic Window Titles ("baseWindow"):** Opening File Explorer or other applications from desktop icons displayed the fallback prefab name "baseWindow" on the taskbar and window header instead of the application name.
2. **Duplicate Window Spawning:** Double-clicking an application or level multiple times from the File Explorer instantiated multiple redundant windows and conflicting `LevelRunner` instances instead of refocusing the existing window.
3. **Context Menu Failure on Right-Click:** Right-clicking on the desktop or within the File Explorer failed to summon the Context Menu because the menu GameObject was inactive on startup (blocking `Awake()` and nullifying `ContextMenuController.Instance`), and the Explorer ScrollView intercepted raycasts without routing them to `FileExplorerApp.OnPointerClick`.
4. **Recycle Bin Misdirection:** The "Recycle Bin" desktop icon pointed directly to the root "Files" directory rather than a dedicated trash bin view.

---

**Architectural Solutions & Implementation:**

**1. Window Title Resolution (`DraggableIcon.cs` & `Window.cs`):**
* In `DraggableIcon.OpenApplication()`: Dynamically calculates `resolvedTitle`:
  - If `levelDefinition != null`: uses `levelDefinition.displayName`.
  - Else: inspects `TextMeshProUGUI` child or icon GameObject name.
  - Normalizes "Files" to "File Explorer".
* Passes `resolvedTitle` directly to `WindowManager.OpenWindow(windowPrefab, appWindow, resolvedTitle)` and executes `appWindow.setTitle(resolvedTitle)`.
* In `Window.setTitle()`: Updates `windowTitle`, synchronizes any text component in `titleBar` (`WindowTop`), and invokes `StateChanged`, ensuring the taskbar button displays the true application title immediately.

**2. Duplicate Window Prevention (`WindowManager.cs` & `FileExplorerApp.cs`):**
* In `WindowManager.cs`: Added `FindOpenWindowByTitle(string title)`.
* In `OpenWindow()`: Before instantiating a new prefab, if a window with the matching title is already registered and not closed, it restores, brings to front, and returns the existing window.
* In `FileExplorerApp.cs`: Maintains `Dictionary<string, Window> openedWindows`.
  - Double-clicking an open level immediately restores and focuses the active window rather than starting a duplicate `LevelRunner`.

**3. Context Menu Right-Click Activation (`ContextMenuController.cs` & `FileExplorerApp.cs`):**
* In `ContextMenuController.cs`:
  - Implemented lazy search fallback in `Instance`: `FindAnyObjectByType<ContextMenuController>(FindObjectsInactive.Include)` to guarantee non-null singleton access regardless of activation state.
  - Refactored `Show()` to guarantee `gameObject.SetActive(true)` and ensure canvas references are cached.
* In `VirtualDesktopSetup.cs`: Kept `contextMenuGo` active on startup, delegating hiding to `cmController.Hide()` in `Start()`.
* In `FileExplorerApp.cs`:
  - Implemented `ExplorerClickRelay` component attached to `itemsContainer` and its parent `ScrollView`.
  - Routes background click events directly to `FileExplorerApp.OnPointerClick()`, ensuring right-clicks anywhere in empty explorer areas trigger the Context Menu.

**4. Dedicated Recycle Bin Integration (`FileExplorerApp.cs` & `VirtualDesktopSetup.cs`):**
* In `FileExplorerApp.BuildInitialFileSystem()`: Created a dedicated `"Recycle Bin"` folder with discarded sample files (`Old_Drawing.png`, `Draft_Notes.txt`, `Deleted_Shortcut.txt`).
* In `VirtualDesktopSetup.cs`:
  - Added `EnsureRecycleBinFolderPrefab()` to generate `RecycleBinContent.prefab` configured with `initialFolderName = "Recycle Bin"`.
  - Configured the desktop "Recycle Bin" icon with `RecycleBinContent.prefab`.
  - Opening the Recycle Bin navigates directly to `This PC > Recycle Bin` with "Recycle Bin" window title and taskbar button.

**5. Verification:**
* Executed `ComputerLearning -> Setup Virtual Desktop OS`.
* Clean compilation and execution: 0 errors in Unity Editor console.

==================================================
87. RESIZABLE WINDOW SYSTEM & INSPECTOR-FIRST UI ARCHITECTURE

**Context & Requirements:**
Windows in the Virtual Desktop originally had fixed dimensions or relied on basic maximize/minimize buttons. Users needed the ability to dynamically resize windows by dragging their borders and corners, similar to real-world desktop operating systems (Windows/macOS), while respecting minimum size boundaries and canvas bounds.

---

**Architectural Solutions & Implementation:**

**1. Window Resizing Logic (`Window.cs`):**
* Added `minSize` constraint (`Vector2(250f, 180f)`) and `maxSize` settings to ensure windows cannot be collapsed into an unreadable or unrecoverable state.
* Added `Resize(Vector2 delta, ResizeDirection direction)`:
  - Supports 8 resizing directions: `Left`, `Right`, `Top`, `Bottom`, `TopLeft`, `TopRight`, `BottomLeft`, `BottomRight`.
  - Calculates updated `sizeDelta` and adjusts `anchoredPosition` accordingly depending on pivot and anchor positioning.
  - Clamps new dimensions between `minSize` and available canvas dimensions.
  - Ensures maximizing/restoring states properly handle manually resized window dimensions (`previousSize` and `previousPosition` cached before maximize).

**2. Dedicated Resize Handles (`ResizeHandle.cs`):**
* Implemented `ResizeHandle` component attached to borders and corner colliders/rects on the window prefab.
* Implements Unity UI event interfaces: `IPointerDownHandler`, `IDragHandler`, `IPointerUpHandler`, `IPointerEnterHandler`, `IPointerExitHandler`.
* On pointer enter/exit: changes mouse cursor to standard bidirectional resize cursor arrows (horizontal, vertical, diagonal).
* On drag: passes screen position delta converted through parent canvas space directly to `Window.Resize()`.
* Inspector-first setup: Configurable margins, thickness, and directional enum flags directly assignable in prefabs.

**3. Verification:**
* Verified in Unity Editor across various window types (`File Explorer`, `Paint`, `Notepad`, `ImageViewer`). Windows smoothly scale from all 8 handles while keeping content layouts responsive via Anchors and Layout Groups.

==================================================
88. NATIVE VIRTUAL APPLICATIONS & VIRTUAL FILE SYSTEM PERSISTENCE (PAINT, NOTEPAD, IMAGE VIEWER)

**Context & Requirements:**
To enrich the educational operating system simulation, the Virtual Desktop needed native productivity and creativity applications capable of interacting directly with the virtual file system.

---

**Architectural Solutions & Implementation:**

**1. Virtual Paint Application (`PaintApp.cs`):**
* Interactive canvas drawing tool with custom brush size, color picker palette, eraser tool, and canvas clearing.
* Generates dynamic `Texture2D` runtime buffers.
* Virtual File System integration: Supports saving user artwork directly as `.png` files into virtual folders (`Desktop`, `Pictures`, `Documents`).
* Supports opening existing images from disk or virtual file system for viewing/editing.

**2. Virtual Notepad Application (`NotepadApp.cs`):**
* Interactive text editing application with `TMP_InputField`, word count, character count, and title bar file status (e.g. `*Untitled.txt`).
* Operations: "New File", "Open File", "Save", and "Save As".
* Creates and modifies `VirtualFile` items with `.txt` extension in the virtual file system.

**3. Virtual Image Viewer (`ImageViewerApp.cs`):**
* Lightweight photo/image viewer for inspecting exported `.png` drawings or picture files.
* Supports zoom, pan, and fit-to-window controls.

**4. Verification:**
* Created files in Paint and Notepad persist in memory within the current session's virtual file system and appear in the File Explorer and Desktop.

==================================================
89. BIDIRECTIONAL DESKTOP DRAG & DROP: RECYCLE BIN DELETION & CORE APP PROTECTION

**Context & Issues Identified:**
1. **Recycle Bin Drag-to-Delete:** Users could not delete desktop files or shortcuts by dragging them onto the Recycle Bin icon.
2. **Core App Deletion Risk:** Core desktop applications (such as `Paint`, `Notepad`, `Files`, `Recycle Bin`) could mistakenly be dropped into the Recycle Bin, breaking essential OS functionality.
3. **App Deactivation / Freeze Bug on Drag:** When users attempted to drag protected icons (`Paint` or `Notepad`) to the Recycle Bin, the icons were prevented from deletion, but their click handlers broke completely—double clicking no longer opened the applications.

---

**Architectural Solutions & Implementation:**

**1. Recycle Bin Drop Target (`RecycleBinIcon.cs`):**
* Implements `IDropHandler`, `IPointerEnterHandler`, and `IPointerExitHandler`.
* Highlights icon visuals on hover during active drag.
* In `OnDrop(PointerEventData eventData)`:
  - Extracts the dragged `DraggableIcon` from `eventData.pointerDrag`.
  - Checks if the icon represents a protected core application (`IsProtectedApp`).
  - If protected: rejects the drop with visual feedback and restores the icon to its grid slot.
  - If deletable: unregisters the icon from `IconGrid`, moves the corresponding virtual item to the `Recycle Bin` virtual folder, and destroys/deactivates the desktop icon GameObject.

**2. Drag State Recovery & Raycast Unblocking (`DraggableIcon.cs`):**
* Root cause of app freeze: In `OnPointerDown` or `OnBeginDrag`, `CanvasGroup.blocksRaycasts` was set to `false` and the icon was unregistered from `grid`. If the drag was cancelled or rejected by Recycle Bin, `blocksRaycasts` remained `false` or the icon was left in a dragging state.
* Fixed drag lifecycle:
  - Moved `grid.Unregister(this)` out of `OnPointerDown` and strictly into `OnBeginDrag`. Pure clicks and double-clicks no longer vacate grid slots.
  - Guaranteed in `OnEndDrag` and drop cancellation that `CanvasGroup.blocksRaycasts = true` and `dragging = false`.
  - In `OpenApplication()`: Removed faulty checks that blocked launching if an icon had protected flags. Protected status only governs deletion, not execution.

**3. Verification:**
* Tested dragging user folders and files to Recycle Bin: successfully deleted and moved to trash.
* Tested dragging Paint and Notepad to Recycle Bin: drop rejected, icon snaps back to grid, and double-clicking still opens the applications without issue.

==================================================
90. DYNAMIC DESKTOP INITIALIZATION & ICON GRID POSITIONING (FIXING ICON OVERLAPPING)

**Context & Issues Identified:**
1. **Desktop Clutter & Duplicate Icons:** Multiple duplicate folders and icons appeared on the desktop. The `VirtualDesktop.unity` scene contained hardcoded icon GameObjects under `Panel` (`Levels`, `Files`, `Minigames`, `Notepad`, `Recycle Bin`, `Paint`), while `DesktopManager.cs` was also spawning icons dynamically at runtime.
2. **Icon Overlapping at (0, 0):** When creating a new folder from the desktop Context Menu, the new folder spawned at `(0, 0)` (Column 0, Row 0), overlapping directly over the existing top-left icon (`Levels`).
3. **Empty Folder Spawning for Core Apps:** `DesktopManager` was generating placeholder yellow folder entries inside `This PC > Desktop` for system applications (`Paint`, `Notepad`, `Files`, `Recycle Bin`).

---

**Architectural Solutions & Implementation:**

**1. Clean Scene Hierarchy (`VirtualDesktop.unity`):**
* Removed all hardcoded static icon GameObjects from `Panel` in `VirtualDesktop.unity`. The desktop canvas starts cleanly with 0 children under the icon container.
* Established `DesktopManager.cs` as the single authoritative source of truth for spawning initial desktop icons on startup (`Levels`, `Files`, `Minigames`, `Notepad`, `Recycle Bin`, `Paint`).

**2. Dynamic Grid Slot Allocation (`IconGrid.cs`):**
* Added `PlaceInNextAvailableSlot(DraggableIcon icon)`:
  - Iterates through columns and rows based on screen width/height and grid dimensions (`cellSize = (100, 100)`, `spacing = (20, 20)`).
  - Checks occupied slots in `Dictionary<Vector2Int, DraggableIcon> occupiedSlots`.
  - Places the icon into the first unoccupied slot `(col, row)` and registers it.
* In `DesktopManager.SpawnIcon()`:
  - Immediately calls `grid.PlaceInNextAvailableSlot(draggable)` upon instantiating any desktop icon.
  - When creating a new folder via Context Menu ("New Folder"), it automatically places the new folder into the next free cell (e.g., Column 1, Row 0 at position `(240, -90)`), completely resolving the overlap issue.

**3. Application vs Folder Separation (`DesktopManager.cs`):**
* Added `IsDesktopOnlyApplication(string name)` helper:
  - Filters out `Paint`, `Notepad`, `Files`, `File Explorer`, and `Recycle Bin`.
  - Prevents creating dummy yellow virtual folders for standalone executables inside the File Explorer's `This PC > Desktop` directory.

**4. Verification:**
* Tested in Unity Editor Play Mode via `Unity_RunCommand`. Clean desktop layout with exactly 6 icons, no duplicates. Created new folders via desktop context menu and verified they spawn in Column 1 without overlapping `Levels`.

==================================================
91. VIRTUAL FILE SYSTEM HIERARCHY, DIRECT LEVEL ACCESSIBILITY & DYNAMIC BREADCRUMBS

**Context & Issues Identified:**
1. **Flat File Explorer Structure:** Entering `Levels` from the desktop displayed `This PC > Levels` instead of reflecting the actual hierarchical desktop path (`This PC > Desktop > Levels`).
2. **Missing Levels and Minigames:** Levels 1 through 10 and minigames were not reliably populated inside their corresponding directories in the File Explorer when opened from clean start.
3. **Static File Explorer Fallback Shell:** If `FileExplorerApp.GetRootFolder()` was called before the prefab finished initializing, it created a blank root fallback folder with missing links.

---

**Architectural Solutions & Implementation:**

**1. Realistic File System Hierarchy (`FileExplorerApp.cs`):**
* Restructured root tree in `BuildInitialFileSystem()`:
  - Root: `"This PC"`.
  - Child folders of `This PC`: `"Desktop"`, `"Documents"`, `"Pictures"`, `"Recycle Bin"`.
  - Child folders of `"Desktop"`: `"Levels"`, `"Minigames"`, and user-created folders.
* Populated all 10 educational levels directly inside `"Levels"` (`Level 1` through `Level 9`, plus `Level 10 Copy-Paste`).
* Populated all 4 minigames directly inside `"Minigames"` (`Targets & Clicks`, `Tracking & Hover`, `Fast Double Click`, `Adaptive Practice`).

**2. Dynamic Breadcrumb Path Resolution (`FileExplorerApp.cs`):**
* Added `GetBreadcrumbPath(VirtualFolder folder)` with recursive search `FindPathRecursive(VirtualFolder current, VirtualFolder target, List<string> path)`:
  - Dynamically builds the full path from `"This PC"` down to any nested folder.
  - When opening `Levels` from the desktop, the path display correctly renders:
    `This PC > Desktop > Levels`
  - When opening `Minigames`, displays:
    `This PC > Desktop > Minigames`
* Added `FindFolderChainRecursive()` to build the list of parent `VirtualFolder` objects, allowing the Explorer's "Back" (Up) navigation button to traverse back step-by-step (`Levels` -> `Desktop` -> `This PC`).

**3. Robust Static Root Access (`FileExplorerApp.cs`):**
* Updated `GetRootFolder()`:
  - If `staticRootFolder` is not yet populated, loads the virtual file system directly from `FileExplorerContent.prefab` or runs `BuildInitialFileSystem()` to guarantee a fully populated tree rather than an empty shell.

**4. Verification:**
* Verified in Unity Editor Play Mode. Navigated to `Levels` and `Minigames`: breadcrumbs show full paths (`This PC > Desktop > Levels`), all 10 levels and 4 minigames are present and launchable, and back navigation smoothly traverses up the hierarchy.

==================================================
92. FILE EXPLORER SUBFOLDER BACK NAVIGATION, PNG GENERIC THUMBNAILS & SAVE AS DIALOG WORKFLOW

**Context & Issues Identified:**
1. **File Explorer Subfolder Back Navigation:** When opening desktop folders like `Levels`, `Minigames`, or user-created folders, the back button was disabled or failed to navigate back up to `Desktop` and `This PC`.
2. **PNG Thumbnail vs Generic Icon:** Paint files saved as `.png` were displaying the raw drawn texture directly as their icon in File Explorer and on Desktop, cluttering the view instead of using the standard generic image icon. The actual drawn image was expected to display only upon opening the file.
3. **Save As Destination Picker:** In Paint and Notepad, the "Save As" button lacked a folder browser, preventing users from selecting where to save their files in the virtual file system.

---

**Architectural Solutions & Implementation:**

**1. Hierarchical Back Navigation & Parent Resolution (`FileExplorerApp.cs`, `DraggableIcon.cs`):**
* Added `GetParentFolder(ExplorerItemData target)`:
  - Traverses the virtual directory tree from `rootFolder` downward to resolve the exact parent `ExplorerItemData` of any given item or folder.
* Upgraded `NavigateBack()`:
  - If `navigationHistory` has entries, pops and navigates to the previous folder.
  - If `navigationHistory` is empty but `currentFolder != rootFolder`, smoothly falls back to navigating up to `GetParentFolder(currentFolder)`.
* Updated `UpdatePathDisplay()`:
  - Back button (`btnBack.interactable`) is enabled whenever `navigationHistory.Count > 0` OR `(currentFolder != null && currentFolder != rootFolder && !currentFolder.name.Equals("This PC"))`.
* Added `OpenFolder(ExplorerItemData target)`:
  - Automatically reconstructs ancestor path history (e.g. `[This PC, Desktop]`) before setting the target folder, ensuring the Back button is immediately interactable when opened from desktop shortcuts.
* Updated `DraggableIcon.cs`:
  - Launching a folder shortcut delegates to `explorer.OpenFolder(LinkedExplorerItem)`.

**2. Generic Image File Icons & Native Image Viewer (`FileExplorerApp.cs`, `DesktopManager.cs`, `StartMenuController.cs`, `DraggableIcon.cs`):**
* Generic `.png` Icon Representation:
  - In `FileExplorerApp.SaveOrCreateImageDocument()`: sets `item.icon = GetImageFileSprite()` while keeping the rendered drawing in `item.imageContent`.
  - In `FileExplorerApp.CreateItemUI()`, `DesktopManager.SpawnIcon()`, `DesktopManager.SpawnIconForExplorerItem()`, and `DesktopManager.SyncDesktopWithExplorerFolder()`: strictly renders the default image file icon sprite for all `.png` files.
* Native Image Viewer Launching:
  - Double-clicking `.png` files in `FileExplorerApp` or desktop shortcuts opens `StartMenuController.OpenImageViewer(item)`.
  - Added context menu options: "Open" (opens in Image Viewer) and "Open in Paint" (opens drawing in Paint for editing).

**3. Interactive "Save As" File Dialog (`FileExplorerApp.cs`, `PaintApp.cs`, `NotepadApp.cs`, `Window.cs`):**
* Dynamic File Dialog Mode (`FileExplorerApp.OpenSaveDialog`):
  - Spawns a dedicated `FileExplorerContent.prefab` instance configured for file saving.
  - Injects a bottom bar with a file name input field (`TMP_InputField`), "Save", and "Cancel" buttons.
  - Users can freely browse through folders (`Desktop`, `Documents`, `Pictures`, custom folders) to choose where to save. Clicking an existing file automatically populates its name in the input field.
  - Clicking "Save" validates against protected folders and invokes `onSaveCallback(enteredName, currentFolder)` before closing the window.
* Paint & Notepad Integration:
  - `PaintApp.OnSaveAsClicked()` calls `FileExplorerApp.OpenSaveDialog("Save As - Paint", defaultName, ".png", startFolder, callback)` and saves canvas data to the selected folder.
  - `NotepadApp.OnSaveAsClicked()` calls `FileExplorerApp.OpenSaveDialog("Save As - Notepad", defaultFileName, ".txt", startFolder, callback)` and saves document text to the selected folder.
* Added `Window.Close()` alias mapping to `CloseWindow()`.

**4. Verification & Testing:**
* Verified in Unity Editor Play Mode:
  - Back button from `Levels` and `Minigames` traverses cleanly back to `Desktop` -> `This PC`.
  - `.png` icons consistently display generic image file icons; double-clicking opens Image Viewer.
  - Save As dialog allows folder selection and file writing with zero compiler or runtime errors.

---

### 93. DESKTOP ICON SELECTION & BLUE HIGHLIGHT SYSTEM (PARITY WITH FILE EXPLORER) - 16/10/2026

**Context & Motivation:**
* In File Explorer (`FileExplorerApp.cs`, `ExplorerItemPointerHandler.cs`), files and folders have a light pastel ice-blue highlight card:
  - Fill Color: `new Color(0.80f, 0.90f, 1.0f, 0.85f)` for selection; `new Color(0.90f, 0.95f, 1.0f, 0.75f)` for hover.
  - The card renders strictly *behind* the icon sprite and text label, leaving the item icon completely crisp, vibrant, and un-tinted.
  - No outline: clean, flat rectangular card.
* Initial implementation used a child object with an outline and darker blue overlay, which rendered over the icon sprite and altered the yellow folder's color. The user provided visual feedback requesting exact parity with File Explorer.

**Architectural Solutions & Implementation:**

**1. Sibling Card Rendering Architecture (`DraggableIcon.cs`):**
* Sibling Placement Under `Panel`:
  - In uGUI, child GameObjects render on top of their parent. To ensure the card renders *behind* the icon sprite and label without modifying existing `GetComponent<Image>()` usages on the root, the selection card (`DesktopSelectionHighlightCard`) and hover card (`DesktopHoverHighlightCard`) are created as direct siblings under `Panel`.
  - Implemented `PlaceBehind(Transform card, Transform target)`:
    - Automatically sets `card.SetSiblingIndex(target.GetSiblingIndex() - 1)`, guaranteeing the card renders immediately before the target icon in the uGUI Canvas render pipeline.
    - Zero tint on the icon sprite: the folder, app, and document icons sit on top in 100% full, vibrant, opaque color.
* Translucent Desktop Styling (Windows Desktop Parity):
  - To match standard Windows desktop behavior, the highlight cards are now translucent instead of solid opaque.
  - This allows the desktop wallpaper to shine through smoothly while displaying a clearly recognizable, elegant blue selection rectangle behind the icon.
  - Selected Fill: `new Color(144f / 255f, 198f / 255f, 244f / 255f, 0.45f)` (45% opacity translucent sky-blue).
  - Hover Fill: `new Color(184f / 255f, 220f / 255f, 248f / 255f, 0.25f)` (25% opacity subtle translucent hover).
  - Both values are enforced in `DraggableIcon.Awake()` to prevent stale inspector or scene serialized overrides.
  - Clean flat rectangle without outline, matching the desktop aesthetic.
* Dimensions & Sizing:
  - `sizeDelta = new Vector2(124f, 146f)`, centered at `anchoredPosition = icon.anchoredPosition + (0, -11)`.
  - Copies `anchorMin`, `anchorMax`, and `pivot` from the icon's `RectTransform` to ensure mathematical alignment across all resolutions.
  - Leaves comfortable margins within the 150px grid spacing (120px icon + 30px spacing).
* Dynamic Tracking & Pulsing:
  - In `Update()` and `OnDrag()`, the card's position and scale (`transform.localScale`) track the icon in real-time, maintaining pixel-perfect synchronization during drags and level unlock pulsing.

**2. Static Selection Coordinator (`DraggableIcon.cs`):**
* Single Selection State:
  - `public static DraggableIcon SelectedIcon { get; private set; }`
  - `SelectIcon(DraggableIcon icon)`: Activates and positions `selectionCard` behind the newly selected icon; deselects any previously selected icon.
  - `ClearSelection()`: Deactivates `selectionCard` and resets `SelectedIcon = null`.
* Hover State:
  - `hoverCard` appears behind any unselected icon on `OnPointerEnter`, and deactivates on `OnPointerExit`.
  - When hovering over an already selected icon, `hoverCard` is suppressed in favor of the active `selectionCard`.

**3. Pointer & Drag Integration:**
* In `OnPointerDown`: Left click immediately selects the icon.
* In `OnBeginDrag` & `OnDrag`: Dragged icon remains selected and the card smoothly moves with it.
* In `OnPointerClick`: Right-click selects and opens context menu; double-click opens application.
* Deselection on Background & Window Clicks:
  - `DesktopContextMenuTrigger.cs`: Left-click down or click on `Panel`/`Background` calls `DraggableIcon.ClearSelection()`.
  - `Window.cs`: Clicking any window calls `DraggableIcon.ClearSelection()`.
  - `StartMenuController.cs`: Opening the Start Menu calls `DraggableIcon.ClearSelection()`.

**4. Verification & Visual Testing:**
* Translucent visual verification:
  - Verified in Play Mode: `DesktopSelectionHighlightCard` active with `RGBA(0.565, 0.776, 0.957, 0.450)`.
  - Over the desktop wallpaper, the background is visible through the translucent card, producing a soft, native Windows desktop selection look.
  - Hovering, dragging, and clearing selection operate cleanly with 0 console errors.

---

### 94. DESKTOP GRID GEOMETRY (6 ICONS PER COLUMN), DYNAMIC HIGHLIGHT SIZING, & OVERLAP PREVENTION - 16/10/2026

**Context & Motivation:**
* Long icon names (such as "Recycle Bin") were breaking lines and stepping onto the icon directly below ("se pisan").
* The static selection card (`124 x 146`) did not expand to accommodate multi-line or wide text, leaving parts of the name outside the blue highlight.
* Desktop grid layout was unevenly distributed across the screen height, not fitting exactly 6 icons per column.
* User requested:
  1. Fix icon name overlap ("se pisan").
  2. Adapt highlight card to completely cover multi-line and long names with generous margins.
  3. Adjust desktop grid and icon spacing so exactly 6 icons fit per column vertically with a uniform pattern across the whole desktop.

**Architectural Solutions & Implementation:**

**1. Desktop Grid Geometry (`IconGrid.cs` & `VirtualDesktop.unity`):**
* Panel & Canvas Dimensions:
  - Reference resolution: `1920 x 1080`.
  - Taskbar takes bottom 64px, leaving `Panel` rect at `1920 x 1016.25`.
* Grid Math & Auto-Fitting:
  - Target rows: `targetRows = 6`.
  - `autoFitRows = true`: `GetCellHeight()` calculates `Mathf.Max(1f, (rect.height - initialPosY * 2f) / targetRows)`.
  - For $1016.25\text{px}$, cell height is exactly `(1016.25 - 32) / 6 = 164f`.
  - Cell width: `155f`.
  - Top and bottom margins: Symmetric `16f` (`initialPosY = 16f`).
  - Left and right margins: Symmetric `30f` (`initialPosX = 30f`, $12 \text{ cols} \times 155\text{px} + 60\text{px} = 1920\text{px}$).
  - Total desktop capacity: 12 columns $\times$ 6 rows = 72 slots.
  - Icons are positioned at the exact center of each cell:
    `rect.rect.xMin + startX + col * stepX + stepX * 0.5f`
    `rect.rect.yMax - startY - row * stepY - stepY * 0.5f`

**2. Overlap Prevention & Text Expansion (`Application Icon.prefab`, `DraggableIcon.cs`):**
* Icon Sprite Sizing:
  - Standardized root icon `sizeDelta` to `105 x 105` (down from `120 x 120`), providing generous breathing room inside the $164\text{px}$ cell.
* Text Box Sizing:
  - Expanded child `ApplicationText` `sizeDelta` to `290 x 60` (from `200 x 50`) at `anchoredPosition = (0, -65)`.
  - At child local scale of `0.44138`, this yields an effective width of $\approx 128\text{px}$, enabling "Recycle Bin" to render on a single clean line.
  - Even with a 2-line title, the $164\text{px}$ cell height guarantees $>25\text{px}$ of clear space to the icon below, completely eliminating overlap.

**3. Dynamic Adaptive Highlight Card (`DraggableIcon.cs`):**
* Replaced hardcoded card dimensions with dynamic calculation in `UpdateCardTransform()`:
  - Measures rendered text dimensions via `label.GetRenderedValues(false)` and icon sprite bounds via `iconRect.rect`.
  - Evaluates sprite top (`spriteTop = iconRect.rect.yMax`) and text bottom (`textBottom = (labelRect.anchoredPosition.y - renderedTextHeight * 0.5f) * labelScale`).
  - Card Height: `cardHeight = Mathf.Max(135f, (spriteTop + 7f) - (textBottom - 8f))`, ensuring symmetrical top padding ($7\text{px}$) and bottom padding ($8\text{px}$).
  - Card Width: `cardWidth = Mathf.Clamp(Mathf.Max(iconRect.rect.width + 24f, renderedTextWidth * labelScale + 24f), 126f, 150f)`.
  - Center Offset: `centerY = ((spriteTop + 7f) + (textBottom - 8f)) * 0.5f`.
  - Highlight card automatically expands and contracts based on icon and text dimensions, ensuring all names (single-line or multi-line) are fully covered.
  - Translucent fill color: `RGBA(144, 198, 244, 0.45)` for selection and `RGBA(184, 220, 248, 0.25)` for hover.

**4. Verification & Testing:**
* Verified in Unity Editor Play Mode:
  - Checked `IconGrid` placement of 6 desktop icons in Column 0:
    - Levels: Row 0 (`anchoredPosition.y = -98.00`)
    - Files: Row 1 (`anchoredPosition.y = -262.00`)
    - Minigames: Row 2 (`anchoredPosition.y = -426.00`)
    - Notepad: Row 3 (`anchoredPosition.y = -590.00`)
    - Recycle Bin: Row 4 (`anchoredPosition.y = -754.00`)
    - Paint: Row 5 (`anchoredPosition.y = -918.00`)
  - Exactly 6 icons placed in column 0 with identical $164\text{px}$ step and symmetric $16\text{px}$ top/bottom margins.
  - Population across multiple columns confirmed a clean, uniform grid pattern across the full 1920x1080 screen.
  - "Recycle Bin" text fits comfortably without overlapping "Paint", and the translucent blue highlight card expands to cover the entire label with clean padding.
  - Zero compiler errors, zero warnings, and zero runtime errors in Unity console.

---

### 95. PRESENTATION BUILD PREPARATION: CONTINUOUS SCENE FLOW, VIRTUAL DESKTOP INTRO WITH INPUT BLOCKER, & 100% ENGLISH LOCALIZATION AUDIT - 19/10/2026

**Context & Motivation:**
* Prepared the project for a functional, robust presentation build with three core technical requirements:
  1. Continuous linear scene flow: `Desktop1 -> Tutorial1 -> Desktop2 -> Tutorial2 -> VirtualDesktop`, ensuring the transition from `Tutorial2` to `VirtualDesktop` happens reliably right at the end of the `Tutorial2` sequence.
  2. A guided `VirtualDesktop` introduction (`VirtualDesktopIntro.cs`) that blocks all player input via `TutorialBlocker` while the `VirtualMascot` introduces the desktop, folders, apps, and Start button, before releasing control and transitioning into `DesktopCompanion` mode.
  3. 100% English localization across all UI elements, mascot dialogues, task instructions, and default settings.

**Architectural Solutions & Implementation:**

**1. Continuous Scene Flow (`Desktop1 -> Tutorial1 -> Desktop2 -> Tutorial2 -> VirtualDesktop`):**
* `FirstDesktopTutorial.cs` (`Desktop1 -> Tutorial1`):
  - Added `forcePlayTutorial = true` so `Desktop1` reliably runs `AutoOnboardingSequence()`, sets `AutoSequenceCompleted = 1`, and loads `"Tutorial1"`.
* `DesktopTour.cs` (`Tutorial1 -> Desktop2`):
  - Verified window tour completion saves `DesktopTourCompleted = 1` and transitions to `"Desktop2"`.
* `SecondDesktopTutorial.cs` (`Desktop2 -> Tutorial2`):
  - Added `forcePlayTutorial = true` and localized `TutorialDialogs` strings with English fallbacks, saving `SemiGuidedSequenceCompleted = 1` and loading `"Tutorial2"` upon completing Level 9.
* `AdvancedDesktopTour.cs` (`Tutorial2 -> VirtualDesktop`):
  - Hardened Step 7 (Recycle Bin) so dragging any application icon on `Tutorial2`'s desktop grid into the Recycle Bin completes the step (`onRecycled = (icon) => { if (icon != null) recycled = true; };`).
  - Re-enabled `TutorialBlocker` (`SetAllowedTarget(null)`) during the final congratulatory speech so player input is blocked until the scene transition occurs.
  - Added `CompleteAndTransitionToVirtualDesktop()` which sets `AdvancedDesktopTourCompleted = 1`, resets `VirtualDesktopIntroCompleted = 0`, saves `PlayerPrefs`, and loads `"VirtualDesktop"`.
* `SceneSystem.cs` & `InitialTestManager.cs`:
  - Updated `ResetProgress()` to also reset `VirtualDesktopIntroCompleted = 0` and force `SelectedLanguage` to `LocalizationManager.LANG_EN` (`"EN"`).
  - Added `InitializeSessionFlags()` with `[RuntimeInitializeOnLoadMethod]` to clear incomplete intermediate tutorial flags on new session startup, and updated `InitialTestManager.FinishTest()` to transition cleanly to `"Desktop1"`.
* Per-Scene `Managers` Lifecycle Fix (`ProgressData.cs`, `SkillManager.cs`, `TaskManager.cs`, `LearningSessionManager.cs`, `DesktopManager.cs`, `VirtualMascot.cs`, `TutorialBlocker.cs`):
  - Fixed a critical bug where `ProgressData.Awake()` called `DontDestroyOnLoad(gameObject)` on the shared `Managers` GameObject in `AssessmentScene` and `Tutorial1`. When transitioning to `Desktop1` or `Desktop2`, the surviving `Managers` GameObject from the previous scene caused `SkillManager`, `ProgressData`, and `DesktopManager` in the new scene to execute `Destroy(gameObject)` on the new scene's `Managers` (destroying `DesktopManager`, `FirstDesktopTutorial`, and `SecondDesktopTutorial` before icons or the mascot could spawn).
  - Removed `DontDestroyOnLoad(gameObject)` from `ProgressData` (data persists across scenes via `PlayerPrefs`) and updated manager `Awake()` methods so each scene's `Managers` instance cleanly becomes the active singleton.
  - Restricted `FileExplorerApp` desktop folder mirroring (`RegisterSceneIconsIntoDesktopFolder` / `SyncDesktopWithExplorerFolder`) to `VirtualDesktop` so tutorial level icons from `Desktop1` and `Desktop2` are not mirrored into `This PC > Desktop`.

**2. Virtual Desktop Introduction with Input Blocker (`VirtualDesktopIntro.cs`, `VirtualMascot.cs`, `DesktopManager.cs`, `VirtualDesktop.unity`):**
* Created `VirtualDesktopIntro.cs` (`Assets/Scripts/DesktopSystem/VirtualDesktopIntro.cs`):
  - Instantiates/reuses `TutorialBlocker` with `SetAllowedTarget(null)` to block 100% of mouse clicks on the desktop, icons, and taskbar during the intro sequence.
  - Runs `IntroSequence()` using `VirtualMascot.Show(...)` across 4 guided steps (`virtual_welcome`, `virtual_levels`, `virtual_apps`, `virtual_start_and_pet`), pointing to the desktop center, `"Levels"` folder, `"Paint"`/`"Minigames"`/`"Notepad"`, and `"StartButton"`.
  - On completion (`CompleteIntro()`), saves `VirtualDesktopIntroCompleted = 1`, destroys the `TutorialBlocker`, and calls `VirtualMascot.HideMascot()`.
* Updated `VirtualMascot.cs`:
  - `ShowMessage(...)` temporarily suspends `isDesktopCompanion`, stops `hideBubbleCoroutine`, restores pointer pivot `(0.125f, 1.13f)`, and disables raycast blocking so the mascot acts as a pointer during `VirtualDesktopIntro`.
  - `Hide()` / `HideMascot()` checks `DesktopManager.Instance.EnableMascotCompanion` and automatically calls `InitAsDesktopCompanion()` so the mascot returns to the top-right corner as an interactive desktop pet after the intro finishes.
* Attached `VirtualDesktopIntro` to the `Managers` GameObject in `VirtualDesktop.unity` and added automatic attachment fallback in `DesktopManager.Start()`.

**3. 100% English Localization Audit:**
* `LocalizationManager.cs`: Added `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` to ensure `SelectedLanguage` defaults to `"EN"` at session startup.
* `Menu.unity`: Updated language `TMP_Dropdown` default serialized value from `1` (`Español (ES)`) to `0` (`English (EN)`).
* `TutorialDialogs.cs`, `EN-TutorialDialogs.json`, `TutorialDialogs.json`, `ES-TutorialDialogs.json`: Added `virtual_welcome`, `virtual_levels`, `virtual_apps`, and `virtual_start_and_pet` fields and ensured all dialog JSON files contain 100% English text.
* `VirtualMascot.cs`: Replaced remaining Spanish fallback strings in companion mode with English.
* `LevelRunner.cs`: Removed the debug placeholder string `"\n\n(Animated tutorial will go here)"` from `ShowInstructionScreen()`.

**4. Direct Main Menu Button to `VirtualDesktop` (`Menu.unity`, `ProgressData.cs`, `StartMenuController.cs`, `ContextMenuController.cs`):**
* Added `Btn-VirtualDesktop` (`"Virtual Desktop"`) inside `MenuBtnLayout` in `Menu.unity` right below `Btn-Play`, wired to `SceneSystem.ChangeToVirtualDesktop()`.
* Updated `ProgressData.IsLevelUnlocked()` to return `true` when the active scene is `"VirtualDesktop"`, so all levels and minigames inside `VirtualDesktop` are immediately playable even if the user enters `VirtualDesktop` directly from the Main Menu without completing `Desktop1` and `Desktop2` first, while keeping the normal `"Play"` tutorial progression untouched.
* Updated `StartMenuController` and `ContextMenuController` `Awake()`/`OnDestroy()` singleton lifecycles to cleanly bind per scene without destroying scene instances.

---

### 96. MID-LEVEL ERROR CONDITIONS & CONTEXTUAL FEEDBACK STANDARDIZATION (LEVELS 1 TO 16) - 23/10/2026

**Context & Motivation:**
* Real-time mistake detection during tasks required precise definitions across all 16 levels to provide didactic, non-punitive feedback without resetting levels abruptly.
* User requested unifying failure conditions covering missed clicks, wrong mouse buttons, hovering stagnation, premature exits, inactivity timeouts, and application shortcut mistakes.

**Architectural Solutions & Implementation:**

**1. Base Task Error Architecture (BaseTask.cs):**
* Added SetupBackgroundClickInterceptor() and TaskBackgroundClickCatcher to cleanly intercept clicks occurring on empty content areas without modifying prefab assets.
* Overloaded RegisterAttempt(bool withShake = true) to increment ttempts, invoke NotifyUserInteraction(), and trigger FeedbackManager.PlayError().
* Exposed public int Attempts => attempts;.

**2. Basic Mouse Skills Tasks (Levels 1 to 9):**
* **Level 1 (Move - MoveMouseTask.cs):** Clicks on window background or dirt spots register mistakes and remind user to only glide cursor without clicking. Inactivity between spots (>4.5s) prompts mascot guidance.
* **Level 2 (Hover - HoverTargetTask.cs, HoverTarget.cs):** Premature hover exit (hoverTime > 0.15s), clicking the target, or clicking the background trigger contextual feedback and register attempts. Start delay (>5s) prompts mascot reminder.
* **Level 3 (Click - ClickTargetTask.cs, ClickTarget.cs):** Right-clicking target or background triggers button correction (ShowWrongClickFeedback). Stagnant hover on target without clicking (>2.5s) and inter-target delay (>5s) trigger reminders.
* **Level 4 (Adaptive):** Dynamically generated tasks automatically benefit from updated task controllers.
* **Level 5 (Double Click - DoubleClickTask.cs):** Missing target via background click, moving outside target between clicks, and single click timeout trigger feedback and register attempts.
* **Level 6 (Right Click - RightClickTask.cs):** Background clicks and left clicks on target register attempts and remind user to use the right mouse button.
* **Level 7 (Click & Hold - ClickHoldTask.cs):** Early release, moving outside target during hold, right click, and background clicks register attempts and guide the player.
* **Level 8 (Drag & Drop - DragDropTask.cs, DraggablePiece.cs):** Drop misses, tapping without dragging, right-clicking piece, dragging stagnation (>5s), and background clicks register attempts with contextual guidance.
* **Level 9 (Mixed Skills):** Multi-target sequence inherits all unified error feedback behaviors.

**3. Desktop Guided Levels (Levels 10 to 16 - VirtualDesktopGuidedLevels.cs):**
* Added currentLevelMistakes tracking and RecordMistake(message, target, isRightClickExpected).
* Updated CompleteLevel() to calculate real accuracy and medals based on mistakes and save them to ProgressData and LevelCelebrationModal.
* **Level 10 (Context Menu):** Left-clicking desktop when right-click is expected or closing menu without selecting an option records mistakes.
* **Level 11 (Notepad & Typing):** Right-clicking icon or typing without focusing the text area records mistakes.
* **Level 12 (Copy, Paste & Cut):** Pasting before copying, copying without selecting text, and wrong shortcuts record mistakes.
* **Levels 13, 14, 15, 16:** Pressing shortcut keys without Ctrl ('Z', 'S') or drawing outside canvas records mistakes with mascot guidance.