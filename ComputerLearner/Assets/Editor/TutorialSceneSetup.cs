using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ComputerLearning;
using System.Linq;

public static class TutorialSceneSetup
{
    [MenuItem("Tools/Setup Intro Tutorial Scene")]
    public static void SetupScene()
    {
        string scenePath = "Assets/Scenes/IntroTutorialScene.unity";
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // 1. Remove Managers that conflict with the tutorial
        var flowControllers = Object.FindObjectsByType<GameFlowController>(FindObjectsSortMode.None);
        foreach (var fc in flowControllers) Object.DestroyImmediate(fc);

        var tutManagers = Object.FindObjectsByType<TutorialManager>(FindObjectsSortMode.None);
        foreach (var tm in tutManagers) Object.DestroyImmediate(tm);

        var testManagers = Object.FindObjectsByType<InitialTestManager>(FindObjectsSortMode.None);
        foreach (var tm in testManagers) Object.DestroyImmediate(tm);
        
        var sessionManagers = Object.FindObjectsByType<LearningSessionManager>(FindObjectsSortMode.None);
        foreach (var sm in sessionManagers) Object.DestroyImmediate(sm);

        // 2. Clean up DraggableIcons (remove LevelDefinition so no levels start)
        var icons = Object.FindObjectsByType<DraggableIcon>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var icon in icons)
        {
            SerializedObject so = new SerializedObject(icon);
            so.FindProperty("levelDefinition").objectReferenceValue = null;
            so.ApplyModifiedProperties();
        }

        // 3. Ensure DesktopTour is attached to Canvas
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            DesktopTour tour = canvas.GetComponent<DesktopTour>();
            if (tour == null) tour = canvas.gameObject.AddComponent<DesktopTour>();
            tour.forcePlayTutorial = true;
        }

        // 4. Save Scene
        EditorSceneManager.SaveScene(scene);

        // 5. Add to Build Settings at Index 0
        var originalScenes = EditorBuildSettings.scenes;
        if (!originalScenes.Any(s => s.path == scenePath))
        {
            var newScenes = new EditorBuildSettingsScene[originalScenes.Length + 1];
            newScenes[0] = new EditorBuildSettingsScene(scenePath, true); // Put at start
            for (int i = 0; i < originalScenes.Length; i++)
            {
                newScenes[i + 1] = originalScenes[i];
            }
            EditorBuildSettings.scenes = newScenes;
        }

        Debug.Log("✅ IntroTutorialScene successfully configured! You can now hit Play.");
    }
}
