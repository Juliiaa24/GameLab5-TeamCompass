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
END OF PROJECT CONTEXT
