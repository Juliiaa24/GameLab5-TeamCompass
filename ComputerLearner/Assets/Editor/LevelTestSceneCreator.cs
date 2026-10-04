using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace ComputerLearning.EditorTools
{
    public class LevelTestSceneCreator : Editor
    {
        [MenuItem("ComputerLearning/Create Level Test Scene")]
        public static void CreateTestScene()
        {
            // Create a new empty scene
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Create Canvas
            GameObject canvasGO = new GameObject("Canvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Create EventSystem
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            // Since they are using the new Input System, Unity might auto-replace this or they can handle it.
            // Actually, we should add the InputSystemUIInputModule if we can, but we don't have the explicit assembly ref.
            // StandaloneInputModule works as a fallback in the editor usually, or Unity upgrades it automatically.

            // Create Tester Script
            GameObject testerGO = new GameObject("LevelTester");
            LevelTester tester = testerGO.AddComponent<LevelTester>();
            tester.canvasTransform = canvasGO.transform;

            // Load all level definitions to populate the tester
            string[] guids = AssetDatabase.FindAssets("t:LevelDefinition");
            tester.availableLevels = new LevelDefinition[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                tester.availableLevels[i] = AssetDatabase.LoadAssetAtPath<LevelDefinition>(AssetDatabase.GUIDToAssetPath(guids[i]));
            }

            // Save the scene
            string scenePath = "Assets/LevelTestScene.unity";
            EditorSceneManager.SaveScene(newScene, scenePath);
            Debug.Log($"[ComputerLearning] Test Scene created at {scenePath}");
        }
    }
}
