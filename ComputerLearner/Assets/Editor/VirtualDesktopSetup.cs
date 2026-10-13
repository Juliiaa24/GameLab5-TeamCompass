/**
 * Author: Diego
 * Date: 13/10/2026
 * Description: Editor tool to set up the simulated Windows OS environment, Context Menu, and File Explorer.
*/
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

namespace ComputerLearning.EditorTools
{
    public class VirtualDesktopSetup : Editor
    {
        private const string WINDOW_PREFAB_PATH = "Assets/Prefabs/Windows/BaseWindow.prefab";
        private const string PREFABS_DIR = "Assets/Prefabs/Windows";

        [MenuItem("ComputerLearning/Setup Virtual Desktop OS")]
        public static void SetupVirtualDesktop()
        {
            // 1. Ensure scene is open
            if (EditorSceneManager.GetActiveScene().name != "VirtualDesktop")
            {
                EditorSceneManager.OpenScene("Assets/Scenes/VirtualDesktop.unity");
            }

            GameObject baseWindowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WINDOW_PREFAB_PATH);
            if (baseWindowPrefab == null)
            {
                Debug.LogError($"[VirtualDesktopSetup] BaseWindow prefab not found at {WINDOW_PREFAB_PATH}");
                return;
            }

            // Enforce minimum window size on all resize handles to prevent UI breakdown when resized small
            ResizeHandle[] windowHandles = baseWindowPrefab.GetComponentsInChildren<ResizeHandle>(true);
            bool handlesModified = false;
            foreach (var h in windowHandles)
            {
                SerializedObject soH = new SerializedObject(h);
                SerializedProperty minProp = soH.FindProperty("minSize");
                if (minProp != null && minProp.vector2Value != new Vector2(380, 260))
                {
                    minProp.vector2Value = new Vector2(380, 260);
                    soH.ApplyModifiedProperties();
                    handlesModified = true;
                }
            }
            if (handlesModified)
            {
                PrefabUtility.SavePrefabAsset(baseWindowPrefab);
            }

            // Load app icons from Art/UI/App icons
            Sprite folderIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/App icons/FolderIcons/Folder_Icon01.png");
            Sprite purpleFolderIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/App icons/FolderIcons/Folder_Icon01_Purple.png");
            Sprite notepadIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/App icons/Text_File_Icon_W01_Scaled.png");
            Sprite minigamesIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/App icons/Maze_Minigame_Icon_01_Scaled.png");
            Sprite recycleIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/App icons/RecyclingBin_Icon_01.png");

            // 2. Ensure app content prefabs and Level 10 assets exist
            EnsureCopyPasteLevelAssets();
            GameObject notepadPrefab = EnsureNotepadPrefab();
            GameObject explorerPrefab = EnsureExplorerPrefab(folderIcon, notepadIcon, minigamesIcon, baseWindowPrefab);
            GameObject minigamesFolderPrefab = EnsureMinigamesFolderPrefab(explorerPrefab);
            GameObject levelsFolderPrefab = EnsureLevelsFolderPrefab(explorerPrefab);
            GameObject recycleBinFolderPrefab = EnsureRecycleBinFolderPrefab(explorerPrefab);
            GameObject minigamesPrefab = EnsureMinigamesPrefab(baseWindowPrefab);

            // 3. Find Desktop Grid (Main Canvas)
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[VirtualDesktopSetup] No Canvas found in VirtualDesktop scene!");
                return;
            }

            Transform desktopGrid = canvas.transform;

            // 4. Find Taskbar and setup Start Button
            Taskbar taskbar = Object.FindAnyObjectByType<Taskbar>();
            if (taskbar == null)
            {
                Debug.LogError("[VirtualDesktopSetup] No Taskbar found in scene!");
                return;
            }

            Transform taskbarTransform = taskbar.transform;
            Transform existingStartBtn = taskbarTransform.Find("StartButton");
            if (existingStartBtn != null) DestroyImmediate(existingStartBtn.gameObject);

            Transform desktopLabelTransform = taskbarTransform.Find("DesktopLabel");
            if (desktopLabelTransform != null) DestroyImmediate(desktopLabelTransform.gameObject);

            GameObject startBtnGo = new GameObject("StartButton", typeof(RectTransform), typeof(Image), typeof(Button));
            startBtnGo.transform.SetParent(taskbarTransform, false);
            startBtnGo.transform.SetAsFirstSibling();

            RectTransform rt = startBtnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot = new Vector2(0, 0.5f);
            rt.anchoredPosition = new Vector2(12, 0);
            rt.sizeDelta = new Vector2(110, 44);

            Image btnImg = startBtnGo.GetComponent<Image>();
            btnImg.color = new Color(0.18f, 0.24f, 0.35f, 0.95f);

            Button startBtn = startBtnGo.GetComponent<Button>();
            ColorBlock colors = startBtn.colors;
            colors.normalColor = new Color(0.18f, 0.24f, 0.35f, 0.95f);
            colors.highlightedColor = new Color(0.24f, 0.32f, 0.46f, 1f);
            colors.pressedColor = new Color(0.12f, 0.16f, 0.24f, 1f);
            startBtn.colors = colors;

            GameObject labelGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(startBtnGo.transform, false);
            RectTransform lRt = labelGo.GetComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero;
            lRt.anchorMax = Vector2.one;
            lRt.sizeDelta = Vector2.zero;

            TextMeshProUGUI labelText = labelGo.GetComponent<TextMeshProUGUI>();
            labelText.text = "Start";
            labelText.fontSize = 17;
            labelText.fontStyle = FontStyles.Bold;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = Color.white;

            // 5. Build StartMenuPanel on Canvas
            Transform existingMenu = desktopGrid.Find("StartMenuPanel");
            GameObject startMenuPanelGo = existingMenu != null ? existingMenu.gameObject : null;
            if (startMenuPanelGo == null)
            {
                startMenuPanelGo = new GameObject("StartMenuPanel", typeof(RectTransform), typeof(Image), typeof(Outline));
                startMenuPanelGo.transform.SetParent(desktopGrid, false);
            }

            RectTransform menuRect = startMenuPanelGo.GetComponent<RectTransform>();
            menuRect.anchorMin = new Vector2(0, 0);
            menuRect.anchorMax = new Vector2(0, 0);
            menuRect.pivot = new Vector2(0, 0);
            menuRect.anchoredPosition = new Vector2(12, 68);
            menuRect.sizeDelta = new Vector2(300, 390);

            Image menuImg = startMenuPanelGo.GetComponent<Image>();
            menuImg.color = new Color(0.09f, 0.13f, 0.20f, 0.98f);

            Outline outline = startMenuPanelGo.GetComponent<Outline>();
            outline.effectColor = new Color(0.30f, 0.39f, 0.52f, 0.5f);
            outline.effectDistance = new Vector2(1, -1);

            while (startMenuPanelGo.transform.childCount > 0)
            {
                DestroyImmediate(startMenuPanelGo.transform.GetChild(0).gameObject);
            }

            // Header
            GameObject headerGo = new GameObject("Header", typeof(RectTransform), typeof(Image));
            headerGo.transform.SetParent(startMenuPanelGo.transform, false);
            RectTransform headerRt = headerGo.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0, 1);
            headerRt.anchorMax = new Vector2(1, 1);
            headerRt.pivot = new Vector2(0.5f, 1);
            headerRt.anchoredPosition = new Vector2(0, 0);
            headerRt.sizeDelta = new Vector2(0, 48);
            headerGo.GetComponent<Image>().color = new Color(0.15f, 0.20f, 0.29f, 1f);

            GameObject headerTextGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            headerTextGo.transform.SetParent(headerGo.transform, false);
            RectTransform hTextRt = headerTextGo.GetComponent<RectTransform>();
            hTextRt.anchorMin = Vector2.zero;
            hTextRt.anchorMax = Vector2.one;
            hTextRt.sizeDelta = Vector2.zero;
            hTextRt.offsetMin = new Vector2(16, 0);
            TextMeshProUGUI hText = headerTextGo.GetComponent<TextMeshProUGUI>();
            hText.text = "My Computer";
            hText.fontSize = 18;
            hText.fontStyle = FontStyles.Bold;
            hText.alignment = TextAlignmentOptions.MidlineLeft;
            hText.color = Color.white;

            // Buttons Container
            GameObject buttonsContainer = new GameObject("ButtonsContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
            buttonsContainer.transform.SetParent(startMenuPanelGo.transform, false);
            RectTransform bContRt = buttonsContainer.GetComponent<RectTransform>();
            bContRt.anchorMin = new Vector2(0, 0);
            bContRt.anchorMax = new Vector2(1, 1);
            bContRt.offsetMin = new Vector2(12, 12);
            bContRt.offsetMax = new Vector2(-12, -56);

            VerticalLayoutGroup vlg = buttonsContainer.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;

            Button btnExplorer = CreateMenuButton(buttonsContainer.transform, "btnExplorer", "File Explorer", folderIcon);
            Button btnNotepad = CreateMenuButton(buttonsContainer.transform, "btnNotepad", "Notepad", notepadIcon);
            Button btnMinigames = CreateMenuButton(buttonsContainer.transform, "btnMinigames", "Minigames Hub", minigamesIcon);
            Button btnStats = CreateMenuButton(buttonsContainer.transform, "btnStats", "Stats & Achievements", null);
            Button btnExit = CreateMenuButton(buttonsContainer.transform, "btnExit", "Sign Out / Exit", null);

            StartMenuController controller = startMenuPanelGo.GetComponent<StartMenuController>();
            if (controller == null) controller = startMenuPanelGo.AddComponent<StartMenuController>();

            SerializedObject soController = new SerializedObject(controller);
            soController.FindProperty("startButton").objectReferenceValue = startBtn;
            soController.FindProperty("startMenuPanel").objectReferenceValue = startMenuPanelGo;
            soController.FindProperty("btnFileExplorer").objectReferenceValue = btnExplorer;
            soController.FindProperty("btnNotepad").objectReferenceValue = btnNotepad;
            soController.FindProperty("btnMinigamesHub").objectReferenceValue = btnMinigames;
            soController.FindProperty("btnStats").objectReferenceValue = btnStats;
            soController.FindProperty("btnExitMenu").objectReferenceValue = btnExit;
            soController.FindProperty("baseWindowPrefab").objectReferenceValue = baseWindowPrefab;
            soController.FindProperty("fileExplorerPrefab").objectReferenceValue = explorerPrefab;
            soController.FindProperty("notepadPrefab").objectReferenceValue = notepadPrefab;
            soController.FindProperty("minigamesHubPrefab").objectReferenceValue = minigamesPrefab;
            soController.ApplyModifiedProperties();

            startMenuPanelGo.SetActive(false);

            // 6. Build ContextMenuPanel on Canvas
            SetupContextMenu(desktopGrid, canvas);

            // 7. Attach DesktopContextMenuTrigger to Background & Panel
            SetupDesktopContextMenuTriggers(desktopGrid, folderIcon, explorerPrefab);
            
            // Clean up any lingering VirtualMascot in the scene so DesktopManager manages it cleanly
            Transform existingMascot = desktopGrid.Find("VirtualMascot");
            if (existingMascot != null) DestroyImmediate(existingMascot.gameObject);

            // 8. Configure DesktopManager with real desktop icons and mascot companion
            DesktopManager desktopMgr = Object.FindAnyObjectByType<DesktopManager>();
            if (desktopMgr != null)
            {
                SerializedObject soMgr = new SerializedObject(desktopMgr);
                SerializedProperty iconsProp = soMgr.FindProperty("initialDesktopIcons");
                iconsProp.ClearArray();

                AddIconData(iconsProp, "Levels", DesktopItemType.Folder, purpleFolderIcon != null ? purpleFolderIcon : folderIcon, levelsFolderPrefab);
                AddIconData(iconsProp, "Files", DesktopItemType.Folder, folderIcon, explorerPrefab);
                AddIconData(iconsProp, "Minigames", DesktopItemType.Folder, folderIcon, minigamesFolderPrefab);
                AddIconData(iconsProp, "Notepad", DesktopItemType.TextDocument, notepadIcon, notepadPrefab);
                AddIconData(iconsProp, "Recycle Bin", DesktopItemType.Application, recycleIcon, recycleBinFolderPrefab);

                SerializedProperty companionProp = soMgr.FindProperty("enableMascotCompanion");
                if (companionProp != null) companionProp.boolValue = true;

                soMgr.ApplyModifiedProperties();
            }

            // Save Scene
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("[VirtualDesktopSetup] Virtual Desktop OS environment setup complete and saved!");
        }

        private static void SetupContextMenu(Transform desktopGrid, Canvas canvas)
        {
            Transform existing = desktopGrid.Find("ContextMenuPanel");
            GameObject contextMenuGo = existing != null ? existing.gameObject : null;
            if (contextMenuGo == null)
            {
                contextMenuGo = new GameObject("ContextMenuPanel", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(ContextMenuController));
                contextMenuGo.transform.SetParent(desktopGrid, false);
            }

            RectTransform cmRt = contextMenuGo.GetComponent<RectTransform>();
            cmRt.sizeDelta = new Vector2(180, 48);
            cmRt.pivot = new Vector2(0, 1);

            Image cmImg = contextMenuGo.GetComponent<Image>();
            cmImg.color = new Color(0.12f, 0.16f, 0.24f, 0.98f); // Slate 900

            Outline outline = contextMenuGo.GetComponent<Outline>();
            outline.effectColor = new Color(0.35f, 0.45f, 0.60f, 0.6f);
            outline.effectDistance = new Vector2(1, -1);

            while (contextMenuGo.transform.childCount > 0)
            {
                DestroyImmediate(contextMenuGo.transform.GetChild(0).gameObject);
            }

            // Button: Nueva Carpeta
            GameObject btnNewFolderGo = new GameObject("BtnNewFolder", typeof(RectTransform), typeof(Image), typeof(Button));
            btnNewFolderGo.transform.SetParent(contextMenuGo.transform, false);
            RectTransform nfRt = btnNewFolderGo.GetComponent<RectTransform>();
            nfRt.anchorMin = new Vector2(0, 0);
            nfRt.anchorMax = new Vector2(1, 1);
            nfRt.offsetMin = new Vector2(6, 6);
            nfRt.offsetMax = new Vector2(-6, -6);

            btnNewFolderGo.GetComponent<Image>().color = new Color(0.18f, 0.24f, 0.35f, 0.8f);

            Button btnNf = btnNewFolderGo.GetComponent<Button>();
            ColorBlock cb = btnNf.colors;
            cb.normalColor = new Color(0.18f, 0.24f, 0.35f, 0.8f);
            cb.highlightedColor = new Color(0.28f, 0.38f, 0.55f, 1f);
            cb.pressedColor = new Color(0.14f, 0.18f, 0.26f, 1f);
            btnNf.colors = cb;

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(btnNewFolderGo.transform, false);
            RectTransform tRt = textGo.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.sizeDelta = Vector2.zero;
            tRt.offsetMin = new Vector2(10, 0);

            TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text = "+ New Folder";
            tmp.fontSize = 14;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = Color.white;

            ContextMenuController cmController = contextMenuGo.GetComponent<ContextMenuController>();
            SerializedObject so = new SerializedObject(cmController);
            so.FindProperty("menuPanel").objectReferenceValue = cmRt;
            so.FindProperty("btnNewFolder").objectReferenceValue = btnNf;
            so.FindProperty("targetCanvas").objectReferenceValue = canvas;
            so.ApplyModifiedProperties();
            contextMenuGo.SetActive(true);
            cmController.Hide();
        }

        private static void SetupDesktopContextMenuTriggers(Transform desktopGrid, Sprite folderIcon, GameObject explorerPrefab)
        {
            Transform bgTransform = desktopGrid.Find("Background");
            if (bgTransform != null)
            {
                DesktopContextMenuTrigger trigger = bgTransform.GetComponent<DesktopContextMenuTrigger>();
                if (trigger == null) trigger = bgTransform.gameObject.AddComponent<DesktopContextMenuTrigger>();
                SerializedObject so = new SerializedObject(trigger);
                so.FindProperty("folderSprite").objectReferenceValue = folderIcon;
                so.FindProperty("fileExplorerContentPrefab").objectReferenceValue = explorerPrefab;
                so.ApplyModifiedProperties();
            }

            Transform panelTransform = desktopGrid.Find("Panel");
            if (panelTransform != null)
            {
                DesktopContextMenuTrigger trigger = panelTransform.GetComponent<DesktopContextMenuTrigger>();
                if (trigger == null) trigger = panelTransform.gameObject.AddComponent<DesktopContextMenuTrigger>();
                SerializedObject so = new SerializedObject(trigger);
                so.FindProperty("folderSprite").objectReferenceValue = folderIcon;
                so.FindProperty("fileExplorerContentPrefab").objectReferenceValue = explorerPrefab;
                so.ApplyModifiedProperties();
            }
        }

        private static Button CreateMenuButton(Transform parent, string name, string label, Sprite icon)
        {
            GameObject btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            btnGo.transform.SetParent(parent, false);

            LayoutElement le = btnGo.GetComponent<LayoutElement>();
            le.preferredHeight = 44;
            le.minHeight = 44;

            Image img = btnGo.GetComponent<Image>();
            img.color = new Color(0.15f, 0.20f, 0.29f, 0.8f);

            Button btn = btnGo.GetComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = new Color(0.15f, 0.20f, 0.29f, 0.8f);
            colors.highlightedColor = new Color(0.22f, 0.30f, 0.44f, 1f);
            colors.pressedColor = new Color(0.10f, 0.14f, 0.22f, 1f);
            btn.colors = colors;

            float textLeftOffset = 16f;

            if (icon != null)
            {
                GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(btnGo.transform, false);
                RectTransform iRt = iconGo.GetComponent<RectTransform>();
                iRt.anchorMin = new Vector2(0, 0.5f);
                iRt.anchorMax = new Vector2(0, 0.5f);
                iRt.pivot = new Vector2(0, 0.5f);
                iRt.anchoredPosition = new Vector2(12, 0);
                iRt.sizeDelta = new Vector2(24, 24);

                Image iconImg = iconGo.GetComponent<Image>();
                iconImg.sprite = icon;
                iconImg.color = Color.white;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;

                textLeftOffset = 44f;
            }

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(btnGo.transform, false);
            RectTransform trt = textGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;
            trt.offsetMin = new Vector2(textLeftOffset, 0);
            trt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 15;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            return btn;
        }

        private static void AddIconData(SerializedProperty listProp, string name, DesktopItemType type, Sprite sprite, GameObject contentPrefab)
        {
            int index = listProp.arraySize;
            listProp.InsertArrayElementAtIndex(index);
            SerializedProperty element = listProp.GetArrayElementAtIndex(index);

            element.FindPropertyRelative("iconName").stringValue = name;
            element.FindPropertyRelative("itemType").enumValueIndex = (int)type;
            element.FindPropertyRelative("iconSprite").objectReferenceValue = sprite;
            element.FindPropertyRelative("contentPrefab").objectReferenceValue = contentPrefab;
            element.FindPropertyRelative("levelDefinition").objectReferenceValue = null;
            element.FindPropertyRelative("isLocked").boolValue = false;
        }

        private static GameObject EnsureNotepadPrefab()
        {
            string path = $"{PREFABS_DIR}/NotepadContent.prefab";
            AssetDatabase.DeleteAsset(path);

            GameObject root = new GameObject("NotepadContent", typeof(RectTransform), typeof(Image), typeof(NotepadApp));
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            root.GetComponent<Image>().color = new Color(0.96f, 0.97f, 0.98f, 1f);

            // Input Field container
            GameObject inputGo = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            inputGo.transform.SetParent(root.transform, false);
            RectTransform inRt = inputGo.GetComponent<RectTransform>();
            inRt.anchorMin = new Vector2(0, 0);
            inRt.anchorMax = new Vector2(1, 1);
            inRt.offsetMin = new Vector2(16, 44);
            inRt.offsetMax = new Vector2(-16, -16);
            inputGo.GetComponent<Image>().color = Color.white;

            GameObject textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(inputGo.transform, false);
            RectTransform taRt = textArea.GetComponent<RectTransform>();
            taRt.anchorMin = Vector2.zero;
            taRt.anchorMax = Vector2.one;
            taRt.offsetMin = new Vector2(8, 8);
            taRt.offsetMax = new Vector2(-8, -8);

            GameObject text = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            text.transform.SetParent(textArea.transform, false);
            RectTransform textRt = text.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;
            TextMeshProUGUI tmpText = text.GetComponent<TextMeshProUGUI>();
            tmpText.fontSize = 18;
            tmpText.color = new Color(0.1f, 0.1f, 0.1f, 1f);

            TMP_InputField inputField = inputGo.GetComponent<TMP_InputField>();
            inputField.textViewport = taRt;
            inputField.textComponent = tmpText;
            inputField.lineType = TMP_InputField.LineType.MultiLineNewline;

            // Bottom bar
            GameObject bottomBar = new GameObject("BottomBar", typeof(RectTransform));
            bottomBar.transform.SetParent(root.transform, false);
            RectTransform bbRt = bottomBar.GetComponent<RectTransform>();
            bbRt.anchorMin = new Vector2(0, 0);
            bbRt.anchorMax = new Vector2(1, 0);
            bbRt.anchoredPosition = new Vector2(0, 20);
            bbRt.sizeDelta = new Vector2(0, 36);

            GameObject statusGo = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
            statusGo.transform.SetParent(bottomBar.transform, false);
            RectTransform sRt = statusGo.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0, 0);
            sRt.anchorMax = new Vector2(0.7f, 1);
            sRt.offsetMin = new Vector2(16, 0);
            TextMeshProUGUI statusTmp = statusGo.GetComponent<TextMeshProUGUI>();
            statusTmp.text = "Characters: 0  |  Words: 0";
            statusTmp.fontSize = 14;
            statusTmp.alignment = TextAlignmentOptions.MidlineLeft;
            statusTmp.color = new Color(0.35f, 0.40f, 0.48f, 1f);

            GameObject btnClearGo = new GameObject("BtnClear", typeof(RectTransform), typeof(Image), typeof(Button));
            btnClearGo.transform.SetParent(bottomBar.transform, false);
            RectTransform cRt = btnClearGo.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(1, 0.5f);
            cRt.anchorMax = new Vector2(1, 0.5f);
            cRt.anchoredPosition = new Vector2(-70, 0);
            cRt.sizeDelta = new Vector2(100, 30);
            btnClearGo.GetComponent<Image>().color = new Color(0.85f, 0.90f, 0.95f, 1f);

            GameObject clearLabel = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            clearLabel.transform.SetParent(btnClearGo.transform, false);
            TextMeshProUGUI clTmp = clearLabel.GetComponent<TextMeshProUGUI>();
            clTmp.text = "Clear";
            clTmp.fontSize = 14;
            clTmp.alignment = TextAlignmentOptions.Center;
            clTmp.color = new Color(0.1f, 0.15f, 0.25f, 1f);

            NotepadApp app = root.GetComponent<NotepadApp>();
            SerializedObject so = new SerializedObject(app);
            so.FindProperty("inputField").objectReferenceValue = inputField;
            so.FindProperty("statusText").objectReferenceValue = statusTmp;
            so.FindProperty("btnClear").objectReferenceValue = btnClearGo.GetComponent<Button>();
            so.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            DestroyImmediate(root);
            return prefab;
        }

        private static GameObject EnsureExplorerItemPrefab()
        {
            string path = $"{PREFABS_DIR}/ExplorerItemPrefab.prefab";
            AssetDatabase.DeleteAsset(path);

            GameObject root = new GameObject("ExplorerItemPrefab", typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(110, 110);

            Image cardBg = root.GetComponent<Image>();
            cardBg.color = Color.clear;

            Button btn = root.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(0.9f, 0.95f, 1f, 1f);
            cb.pressedColor = new Color(0.8f, 0.9f, 1f, 1f);
            btn.colors = cb;

            // Icon child
            GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(root.transform, false);
            RectTransform iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 1f);
            iconRt.anchorMax = new Vector2(0.5f, 1f);
            iconRt.pivot = new Vector2(0.5f, 1f);
            iconRt.anchoredPosition = new Vector2(0, -8);
            iconRt.sizeDelta = new Vector2(64, 64);

            Image iconImg = iconGo.GetComponent<Image>();
            iconImg.color = Color.white;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            // Label child
            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(root.transform, false);
            RectTransform labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0, 0);
            labelRt.anchorMax = new Vector2(1, 0);
            labelRt.pivot = new Vector2(0.5f, 0);
            labelRt.anchoredPosition = new Vector2(0, 6);
            labelRt.sizeDelta = new Vector2(-6, 32);

            TextMeshProUGUI label = labelGo.GetComponent<TextMeshProUGUI>();
            label.fontSize = 12;
            label.alignment = TextAlignmentOptions.Top;
            label.color = new Color(0.09f, 0.13f, 0.24f, 1f);
            label.raycastTarget = false;
            label.overflowMode = TextOverflowModes.Ellipsis;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            DestroyImmediate(root);
            return prefab;
        }

        private static GameObject EnsureExplorerPrefab(Sprite folderIcon, Sprite textIcon, Sprite gameIcon, GameObject baseWindowPrefab)
        {
            string path = $"{PREFABS_DIR}/FileExplorerContent.prefab";
            // Delete old asset if exists so it regenerates cleanly with updated fields
            AssetDatabase.DeleteAsset(path);

            GameObject itemPrefab = EnsureExplorerItemPrefab();

            GameObject root = new GameObject("FileExplorerContent", typeof(RectTransform), typeof(Image), typeof(FileExplorerApp));
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            root.GetComponent<Image>().color = new Color(0.95f, 0.96f, 0.98f, 1f);

            // Top Bar
            GameObject topBar = new GameObject("TopBar", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            topBar.transform.SetParent(root.transform, false);
            RectTransform tbRt = topBar.GetComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(0, 1);
            tbRt.anchorMax = new Vector2(1, 1);
            tbRt.pivot = new Vector2(0.5f, 1);
            tbRt.anchoredPosition = Vector2.zero;
            tbRt.sizeDelta = new Vector2(0, 46);
            topBar.GetComponent<Image>().color = Color.white;

            HorizontalLayoutGroup hlg = topBar.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(10, 10, 7, 7);
            hlg.spacing = 8;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // Back Button
            GameObject btnBackGo = new GameObject("BtnBack", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            btnBackGo.transform.SetParent(topBar.transform, false);
            LayoutElement bbLe = btnBackGo.GetComponent<LayoutElement>();
            bbLe.preferredWidth = 72;
            bbLe.minWidth = 60;
            bbLe.preferredHeight = 32;
            bbLe.flexibleWidth = 0;
            btnBackGo.GetComponent<Image>().color = new Color(0.90f, 0.92f, 0.96f, 1f);

            GameObject bbText = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            bbText.transform.SetParent(btnBackGo.transform, false);
            RectTransform bbTextRt = bbText.GetComponent<RectTransform>();
            bbTextRt.anchorMin = Vector2.zero;
            bbTextRt.anchorMax = Vector2.one;
            bbTextRt.sizeDelta = Vector2.zero;
            TextMeshProUGUI bbTmp = bbText.GetComponent<TextMeshProUGUI>();
            bbTmp.text = "< Back";
            bbTmp.fontSize = 13;
            bbTmp.fontStyle = FontStyles.Bold;
            bbTmp.alignment = TextAlignmentOptions.Center;
            bbTmp.color = new Color(0.15f, 0.2f, 0.3f, 1f);

            // Path Text
            GameObject pathGo = new GameObject("PathText", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            pathGo.transform.SetParent(topBar.transform, false);
            LayoutElement ptLe = pathGo.GetComponent<LayoutElement>();
            ptLe.flexibleWidth = 1;
            ptLe.minWidth = 40;
            ptLe.preferredHeight = 32;

            TextMeshProUGUI ptTmp = pathGo.GetComponent<TextMeshProUGUI>();
            ptTmp.text = "This PC > Files";
            ptTmp.fontSize = 13;
            ptTmp.fontStyle = FontStyles.Bold;
            ptTmp.alignment = TextAlignmentOptions.MidlineLeft;
            ptTmp.color = new Color(0.2f, 0.25f, 0.35f, 1f);
            ptTmp.textWrappingMode = TextWrappingModes.NoWrap;
            ptTmp.overflowMode = TextOverflowModes.Ellipsis;

            // New Folder Toolbar Button
            GameObject btnNfGo = new GameObject("BtnNewFolder", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            btnNfGo.transform.SetParent(topBar.transform, false);
            LayoutElement nfLe = btnNfGo.GetComponent<LayoutElement>();
            nfLe.preferredWidth = 110;
            nfLe.minWidth = 85;
            nfLe.preferredHeight = 32;
            nfLe.flexibleWidth = 0;
            btnNfGo.GetComponent<Image>().color = new Color(0.20f, 0.50f, 0.90f, 1f);

            GameObject nfLabel = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            nfLabel.transform.SetParent(btnNfGo.transform, false);
            RectTransform nfLabelRt = nfLabel.GetComponent<RectTransform>();
            nfLabelRt.anchorMin = Vector2.zero;
            nfLabelRt.anchorMax = Vector2.one;
            nfLabelRt.sizeDelta = Vector2.zero;
            TextMeshProUGUI nfTmp = nfLabel.GetComponent<TextMeshProUGUI>();
            nfTmp.text = "+ New Folder";
            nfTmp.fontSize = 12;
            nfTmp.fontStyle = FontStyles.Bold;
            nfTmp.alignment = TextAlignmentOptions.Center;
            nfTmp.color = Color.white;

            // Scroll View with Masking
            GameObject scrollViewGo = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            scrollViewGo.transform.SetParent(root.transform, false);
            RectTransform svRt = scrollViewGo.GetComponent<RectTransform>();
            svRt.anchorMin = Vector2.zero;
            svRt.anchorMax = Vector2.one;
            svRt.offsetMin = new Vector2(8, 30); // above footer
            svRt.offsetMax = new Vector2(-8, -48); // below top bar
            scrollViewGo.GetComponent<Image>().color = Color.clear;

            ScrollRect scrollRect = scrollViewGo.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.scrollSensitivity = 25f;

            // Items Container (Content)
            GameObject itemsGo = new GameObject("ItemsContainer", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            itemsGo.transform.SetParent(scrollViewGo.transform, false);
            RectTransform iRt = itemsGo.GetComponent<RectTransform>();
            iRt.anchorMin = new Vector2(0, 1);
            iRt.anchorMax = new Vector2(1, 1);
            iRt.pivot = new Vector2(0.5f, 1);
            iRt.anchoredPosition = Vector2.zero;
            iRt.sizeDelta = Vector2.zero;

            GridLayoutGroup glg = itemsGo.GetComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(105, 110);
            glg.spacing = new Vector2(12, 12);
            glg.padding = new RectOffset(6, 6, 8, 8);
            glg.childAlignment = TextAnchor.UpperLeft;

            ContentSizeFitter csf = itemsGo.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = iRt;
            scrollRect.viewport = svRt;

            // Bottom Status Footer
            GameObject footerGo = new GameObject("Footer", typeof(RectTransform));
            footerGo.transform.SetParent(root.transform, false);
            RectTransform fRt = footerGo.GetComponent<RectTransform>();
            fRt.anchorMin = new Vector2(0, 0);
            fRt.anchorMax = new Vector2(1, 0);
            fRt.anchoredPosition = new Vector2(0, 14);
            fRt.sizeDelta = new Vector2(0, 28);

            GameObject footerText = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
            footerText.transform.SetParent(footerGo.transform, false);
            RectTransform ftRt = footerText.GetComponent<RectTransform>();
            ftRt.anchorMin = Vector2.zero;
            ftRt.anchorMax = Vector2.one;
            ftRt.offsetMin = new Vector2(16, 0);
            TextMeshProUGUI ftTmp = footerText.GetComponent<TextMeshProUGUI>();
            ftTmp.text = "0 items";
            ftTmp.fontSize = 12;
            ftTmp.alignment = TextAlignmentOptions.MidlineLeft;
            ftTmp.color = new Color(0.4f, 0.45f, 0.55f, 1f);

            // Assign references
            FileExplorerApp app = root.GetComponent<FileExplorerApp>();
            SerializedObject so = new SerializedObject(app);
            so.FindProperty("pathText").objectReferenceValue = ptTmp;
            so.FindProperty("btnBack").objectReferenceValue = btnBackGo.GetComponent<Button>();
            so.FindProperty("btnNewFolderToolbar").objectReferenceValue = btnNfGo.GetComponent<Button>();
            so.FindProperty("statusFooterText").objectReferenceValue = ftTmp;
            so.FindProperty("itemsContainer").objectReferenceValue = itemsGo.transform;
            so.FindProperty("itemPrefab").objectReferenceValue = itemPrefab;

            // Minigames levels and icons
            LevelDefinition targetsLvl = AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/ScriptableObjects/Levels/L_Level3.asset");
            LevelDefinition hoverLvl = AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/ScriptableObjects/Levels/L_Level2.asset");
            LevelDefinition doubleClickLvl = AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/ScriptableObjects/Levels/L_Level5.asset");
            LevelDefinition adaptiveLvl = AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/ScriptableObjects/Levels/L_Level4.asset");
            Sprite targetGameSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/App icons/Target_Minigame_Icon01_Scaled.png");
            Sprite mazeGameSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/App icons/Maze_Minigame_Icon_01_Scaled.png");

            so.FindProperty("targetsLevel").objectReferenceValue = targetsLvl;
            so.FindProperty("hoverLevel").objectReferenceValue = hoverLvl;
            so.FindProperty("doubleClickLevel").objectReferenceValue = doubleClickLvl;
            so.FindProperty("adaptiveLevel").objectReferenceValue = adaptiveLvl;
            so.FindProperty("targetGameIcon").objectReferenceValue = targetGameSprite;
            so.FindProperty("mazeGameIcon").objectReferenceValue = mazeGameSprite;
            so.FindProperty("baseWindowPrefab").objectReferenceValue = baseWindowPrefab;

            // Assign Mouse Levels (Level 1 to 9)
            SerializedProperty mLevelsProp = so.FindProperty("mouseLevels");
            mLevelsProp.ClearArray();
            for (int i = 1; i <= 9; i++)
            {
                LevelDefinition lvl = AssetDatabase.LoadAssetAtPath<LevelDefinition>($"Assets/ScriptableObjects/Levels/L_Level{i}.asset");
                mLevelsProp.InsertArrayElementAtIndex(i - 1);
                mLevelsProp.GetArrayElementAtIndex(i - 1).objectReferenceValue = lvl;
            }

            // Assign Level 10 (Copy & Paste)
            LevelDefinition lvl10 = AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/ScriptableObjects/Levels/L_Level10.asset");
            so.FindProperty("level10CopyPaste").objectReferenceValue = lvl10;

            so.FindProperty("defaultFolderIcon").objectReferenceValue = folderIcon;
            so.FindProperty("defaultTextIcon").objectReferenceValue = textIcon;
            so.FindProperty("defaultGameIcon").objectReferenceValue = gameIcon;
            so.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            DestroyImmediate(root);
            return prefab;
        }

        private static GameObject EnsureLevelsFolderPrefab(GameObject explorerPrefab)
        {
            string path = $"{PREFABS_DIR}/LevelsFolderContent.prefab";
            AssetDatabase.DeleteAsset(path);

            GameObject instance = Object.Instantiate(explorerPrefab);
            instance.name = "LevelsFolderContent";
            FileExplorerApp app = instance.GetComponent<FileExplorerApp>();
            if (app != null)
            {
                SerializedObject so = new SerializedObject(app);
                so.FindProperty("initialFolderName").stringValue = "Levels";
                so.ApplyModifiedProperties();
            }
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        private static GameObject EnsureRecycleBinFolderPrefab(GameObject explorerPrefab)
        {
            string path = $"{PREFABS_DIR}/RecycleBinContent.prefab";
            AssetDatabase.DeleteAsset(path);

            GameObject instance = Object.Instantiate(explorerPrefab);
            instance.name = "RecycleBinContent";
            FileExplorerApp app = instance.GetComponent<FileExplorerApp>();
            if (app != null)
            {
                SerializedObject so = new SerializedObject(app);
                so.FindProperty("initialFolderName").stringValue = "Recycle Bin";
                so.ApplyModifiedProperties();
            }
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        private static void EnsureCopyPasteLevelAssets()
        {
            // 1. Skill Definition
            string skillPath = "Assets/ScriptableObjects/Skills/SK_KeyboardShortcuts.asset";
            SkillDefinition skill = AssetDatabase.LoadAssetAtPath<SkillDefinition>(skillPath);
            if (skill == null)
            {
                skill = ScriptableObject.CreateInstance<SkillDefinition>();
                skill.skillId = "shortcuts";
                skill.displayName = "Keyboard Shortcuts";
                skill.description = "Using keyboard shortcuts like Ctrl+C and Ctrl+V";
                AssetDatabase.CreateAsset(skill, skillPath);
            }

            // 2. Task Prefab
            string taskPrefabPath = "Assets/Prefabs/Tasks/Task_CopyPaste.prefab";
            GameObject taskPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(taskPrefabPath);
            if (taskPrefab == null)
            {
                GameObject go = new GameObject("Task_CopyPaste", typeof(RectTransform), typeof(CopyPasteTask));
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                taskPrefab = PrefabUtility.SaveAsPrefabAsset(go, taskPrefabPath);
                Object.DestroyImmediate(go);
            }

            // 3. Task Definition
            string taskDefPath = "Assets/ScriptableObjects/Tasks/T_CopyPaste.asset";
            TaskDefinition taskDef = AssetDatabase.LoadAssetAtPath<TaskDefinition>(taskDefPath);
            if (taskDef == null)
            {
                taskDef = ScriptableObject.CreateInstance<TaskDefinition>();
                taskDef.taskId = "task_copypaste";
                taskDef.displayName = "Copy & Paste Task";
                taskDef.primarySkill = skill;
                taskDef.difficulty = 1;
                taskDef.taskPrefab = taskPrefab;
                AssetDatabase.CreateAsset(taskDef, taskDefPath);
            }
            else if (taskDef.taskPrefab == null)
            {
                taskDef.taskPrefab = taskPrefab;
                EditorUtility.SetDirty(taskDef);
            }

            // 4. Level Definition
            string levelPath = "Assets/ScriptableObjects/Levels/L_Level10.asset";
            LevelDefinition level10 = AssetDatabase.LoadAssetAtPath<LevelDefinition>(levelPath);
            if (level10 == null)
            {
                level10 = ScriptableObject.CreateInstance<LevelDefinition>();
                level10.levelId = "level10";
                level10.displayName = "Level 10: Copy & Paste";
                level10.instructionText = "Learn how to use keyboard shortcuts!\nPress Ctrl + C to copy and Ctrl + V to paste.";
                level10.isAdaptive = false;
                level10.tasks = new System.Collections.Generic.List<TaskDefinition> { taskDef };
                AssetDatabase.CreateAsset(level10, levelPath);
            }
            else if (level10.tasks == null || level10.tasks.Count == 0)
            {
                level10.tasks = new System.Collections.Generic.List<TaskDefinition> { taskDef };
                EditorUtility.SetDirty(level10);
            }

            AssetDatabase.SaveAssets();
        }

        private static GameObject EnsureMinigamesFolderPrefab(GameObject explorerPrefab)
        {
            string path = $"{PREFABS_DIR}/MinigamesFolderContent.prefab";
            AssetDatabase.DeleteAsset(path);

            GameObject instance = Object.Instantiate(explorerPrefab);
            instance.name = "MinigamesFolderContent";
            FileExplorerApp app = instance.GetComponent<FileExplorerApp>();
            if (app != null)
            {
                SerializedObject so = new SerializedObject(app);
                so.FindProperty("initialFolderName").stringValue = "Minigames";
                so.ApplyModifiedProperties();
            }
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        private static GameObject EnsureMinigamesPrefab(GameObject baseWindowPrefab)
        {
            string path = $"{PREFABS_DIR}/MinigamesHubContent.prefab";
            AssetDatabase.DeleteAsset(path);

            GameObject root = new GameObject("MinigamesHubContent", typeof(RectTransform), typeof(Image), typeof(MinigamesHubApp));
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            root.GetComponent<Image>().color = new Color(0.94f, 0.95f, 0.98f, 1f);

            GameObject scrollViewGo = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            scrollViewGo.transform.SetParent(root.transform, false);
            RectTransform svRt = scrollViewGo.GetComponent<RectTransform>();
            svRt.anchorMin = Vector2.zero;
            svRt.anchorMax = Vector2.one;
            svRt.offsetMin = new Vector2(12, 12);
            svRt.offsetMax = new Vector2(-12, -12);
            scrollViewGo.GetComponent<Image>().color = Color.clear;

            ScrollRect scrollRect = scrollViewGo.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.scrollSensitivity = 25f;

            GameObject gridGo = new GameObject("GamesGrid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            gridGo.transform.SetParent(scrollViewGo.transform, false);
            RectTransform gRt = gridGo.GetComponent<RectTransform>();
            gRt.anchorMin = new Vector2(0, 1);
            gRt.anchorMax = new Vector2(1, 1);
            gRt.pivot = new Vector2(0.5f, 1);
            gRt.offsetMin = Vector2.zero;
            gRt.offsetMax = Vector2.zero;

            GridLayoutGroup glg = gridGo.GetComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(170, 150);
            glg.spacing = new Vector2(16, 16);
            glg.padding = new RectOffset(8, 8, 8, 8);
            glg.childAlignment = TextAnchor.UpperLeft;

            ContentSizeFitter csf = gridGo.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = gRt;
            scrollRect.viewport = svRt;

            Sprite targetSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/App icons/Target_Minigame_Icon01_Scaled.png");
            Sprite mazeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/App icons/Maze_Minigame_Icon_01_Scaled.png");

            Button btnTargets = CreateGameCard(gridGo.transform, "CardTargets", "Targets & Clicks", targetSprite);
            Button btnHover = CreateGameCard(gridGo.transform, "CardHover", "Tracking & Hover", targetSprite);
            Button btnDouble = CreateGameCard(gridGo.transform, "CardDoubleClick", "Fast Double Click", targetSprite);
            Button btnAdaptive = CreateGameCard(gridGo.transform, "CardAdaptive", "Adaptive Practice", mazeSprite);

            MinigamesHubApp app = root.GetComponent<MinigamesHubApp>();
            SerializedObject so = new SerializedObject(app);
            so.FindProperty("btnPlayTargets").objectReferenceValue = btnTargets;
            so.FindProperty("btnPlayHover").objectReferenceValue = btnHover;
            so.FindProperty("btnPlayDoubleClick").objectReferenceValue = btnDouble;
            so.FindProperty("btnPlayAdaptive").objectReferenceValue = btnAdaptive;

            so.FindProperty("targetsLevel").objectReferenceValue = AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/ScriptableObjects/Levels/L_Level3.asset");
            so.FindProperty("hoverLevel").objectReferenceValue = AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/ScriptableObjects/Levels/L_Level2.asset");
            so.FindProperty("doubleClickLevel").objectReferenceValue = AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/ScriptableObjects/Levels/L_Level5.asset");
            so.FindProperty("adaptiveLevel").objectReferenceValue = AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/ScriptableObjects/Levels/L_Level4.asset");
            so.FindProperty("baseWindowPrefab").objectReferenceValue = baseWindowPrefab;
            so.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            DestroyImmediate(root);
            return prefab;
        }

        private static Button CreateGameCard(Transform parent, string name, string title, Sprite icon)
        {
            GameObject card = new GameObject(name, typeof(RectTransform), typeof(Image));
            card.transform.SetParent(parent, false);
            card.GetComponent<Image>().color = Color.white;

            GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(card.transform, false);
            RectTransform iRt = iconGo.GetComponent<RectTransform>();
            iRt.anchorMin = new Vector2(0.5f, 0.7f);
            iRt.anchorMax = new Vector2(0.5f, 0.7f);
            iRt.sizeDelta = new Vector2(50, 50);
            if (icon != null) iconGo.GetComponent<Image>().sprite = icon;

            GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(card.transform, false);
            RectTransform tRt = titleGo.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0, 0.35f);
            tRt.anchorMax = new Vector2(1, 0.35f);
            tRt.sizeDelta = new Vector2(0, 24);
            TextMeshProUGUI tmp = titleGo.GetComponent<TextMeshProUGUI>();
            tmp.text = title;
            tmp.fontSize = 14;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.15f, 0.2f, 0.3f, 1f);

            GameObject btnGo = new GameObject("BtnPlay", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(card.transform, false);
            RectTransform bRt = btnGo.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.5f, 0.15f);
            bRt.anchorMax = new Vector2(0.5f, 0.15f);
            bRt.sizeDelta = new Vector2(120, 30);
            btnGo.GetComponent<Image>().color = new Color(0.15f, 0.45f, 0.85f, 1f);

            GameObject btnLabel = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnLabel.transform.SetParent(btnGo.transform, false);
            TextMeshProUGUI blTmp = btnLabel.GetComponent<TextMeshProUGUI>();
            blTmp.text = "Play!";
            blTmp.fontSize = 13;
            blTmp.fontStyle = FontStyles.Bold;
            blTmp.alignment = TextAlignmentOptions.Center;
            blTmp.color = Color.white;

            return btnGo.GetComponent<Button>();
        }
    }
}
