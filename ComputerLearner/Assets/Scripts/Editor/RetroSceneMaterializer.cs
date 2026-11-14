#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using ComputerLearning;

namespace RetroUIBuilder
{
    /// <summary>
    /// Materializes Colorful Retro Desktop hierarchies inside Menu.unity, StatsScene.unity, and AssessmentScene.unity,
    /// wires all serialized component references, enforces raycastTarget = false on decorative elements,
    /// and ensures TeacherModeConfig.asset exists under Assets/Resources/.
    /// </summary>
    public static class RetroSceneMaterializer
    {
        private const string MainFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        private const string TeacherConfigAssetPath = "Assets/Resources/TeacherModeConfig.asset";

        [MenuItem("Tools/Retro UI/2. Build Menu Scene Hierarchy")]
        public static void BuildMenuScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != "Menu")
            {
                Debug.LogError($"[RetroSceneMaterializer] Open Menu.unity first (active: {activeScene.name}).");
                return;
            }

            RetroThemeAssets assets = EnsureThemeAssets();
            EnsureTeacherConfigAsset();
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MainFontPath);

            Canvas canvas = FindPrimaryCanvas(activeScene, "Menu");
            if (canvas == null)
            {
                Debug.LogError("[RetroSceneMaterializer] Could not find Menu Canvas in Menu.unity.");
                return;
            }

            ConfigureCanvasScaler(canvas);

            // Preserve existing Dropdown-Language if configured, or clone/restyle cleanly
            Transform existingDropdownTr = FindChildRecursive(canvas.transform, "Dropdown-Language");
            GameObject preservedDropdown = existingDropdownTr != null ? existingDropdownTr.gameObject : null;
            if (preservedDropdown != null)
            {
                preservedDropdown.transform.SetParent(null, false);
            }

            // Clear old Menu Canvas children cleanly (including removing Btn-VirtualDesktop from student UI)
            ClearChildrenImmediate(canvas.transform);

            // Clean old stray Menu controllers on Canvas before rewiring
            StripComponent<MenuSceneReferences>(canvas.gameObject);
            StripComponent<TeacherModeGate>(canvas.gameObject);
            StripComponent<PlaytestStudentWidget>(canvas.gameObject);
            StripComponent<LearningPathSelectorWidget>(canvas.gameObject);
            StripComponent<ClassroomNetworkWidget>(canvas.gameObject);

            MenuSceneReferences menuRefs = canvas.gameObject.AddComponent<MenuSceneReferences>();
            TeacherModeGate teacherGate = canvas.gameObject.AddComponent<TeacherModeGate>();
            PlaytestStudentWidget playtestWidget = canvas.gameObject.AddComponent<PlaytestStudentWidget>();
            LearningPathSelectorWidget pathWidget = canvas.gameObject.AddComponent<LearningPathSelectorWidget>();
            ClassroomNetworkWidget classroomWidget = canvas.gameObject.AddComponent<ClassroomNetworkWidget>();

            menuRefs.teacherModeGate = teacherGate;
            menuRefs.playtestStudentWidget = playtestWidget;
            menuRefs.learningPathSelectorWidget = pathWidget;
            menuRefs.classroomNetworkWidget = classroomWidget;

            // =====================================================================
            // 1. Desktop Wallpaper & Cheerful Decorative Desktop Shortcuts
            // =====================================================================
            GameObject wallpaper = CreateImageObject("DesktopWallpaper", canvas.transform, assets.desktopWallpaper, RetroUITheme.SkyBackdropTint, false);
            StretchFull(wallpaper.GetComponent<RectTransform>());

            GameObject softVignette = CreateImageObject("SkyGlowTint", canvas.transform, null, new Color(0.95f, 0.98f, 1f, 0.22f), false);
            StretchFull(softVignette.GetComponent<RectTransform>());

            BuildDesktopShortcutDecoration(canvas.transform, assets, font);

            // =====================================================================
            // 2. Top-Left Student Welcome Card ("¡Hola, [Nombre]!")
            // =====================================================================
            GameObject welcomeCard = CreateRetroCardWindow("StudentWelcomeCard", canvas.transform,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(30f, -24f), new Vector2(456f, 156f),
                assets.windowBody, RetroUITheme.WindowCream);

            GameObject mascotBadge = CreateImageObject("MascotAvatar", welcomeCard.transform, assets.computerIcon ?? assets.mascotLogoNoBg, Color.white, false);
            RectTransform mascotRt = mascotBadge.GetComponent<RectTransform>();
            mascotRt.anchorMin = new Vector2(0f, 0.5f); mascotRt.anchorMax = new Vector2(0f, 0.5f);
            mascotRt.pivot = new Vector2(0f, 0.5f);
            mascotRt.anchoredPosition = new Vector2(18f, 0f);
            mascotRt.sizeDelta = new Vector2(106f, 106f);
            mascotBadge.GetComponent<Image>().preserveAspect = true;

            TMP_Text welcomeGreeting = CreateTmp("WelcomeGreetingText", welcomeCard.transform, font, "Hi, S01!", 26, FontStyles.Bold, RetroUITheme.TitleBlue, TextAlignmentOptions.MidlineLeft);
            RectTransform wgRt = welcomeGreeting.GetComponent<RectTransform>();
            wgRt.anchorMin = new Vector2(0f, 1f); wgRt.anchorMax = new Vector2(1f, 1f);
            wgRt.pivot = new Vector2(0f, 1f);
            wgRt.offsetMin = new Vector2(138f, -54f); wgRt.offsetMax = new Vector2(-16f, -16f);

            TMP_Text nameLabel = CreateTmp("NameLabelText", welcomeCard.transform, font, "Your Name:", 15, FontStyles.Bold, RetroUITheme.InkMuted, TextAlignmentOptions.MidlineLeft);
            RectTransform nlRt = nameLabel.GetComponent<RectTransform>();
            nlRt.anchorMin = new Vector2(0f, 1f); nlRt.anchorMax = new Vector2(1f, 1f);
            nlRt.pivot = new Vector2(0f, 1f);
            nlRt.offsetMin = new Vector2(138f, -82f); nlRt.offsetMax = new Vector2(-16f, -54f);

            TMP_InputField studentNameInput = CreateRetroInputField("Input-StudentName", welcomeCard.transform, assets, font, "Type your name...", 20, TMP_InputField.ContentType.Standard, out TMP_Text namePlaceholder);
            RectTransform sniRt = studentNameInput.GetComponent<RectTransform>();
            sniRt.anchorMin = new Vector2(0f, 0f); sniRt.anchorMax = new Vector2(1f, 0f);
            sniRt.pivot = new Vector2(0.5f, 0f);
            sniRt.offsetMin = new Vector2(138f, 16f); sniRt.offsetMax = new Vector2(-18f, 68f);

            // =====================================================================
            // 3. Top-Center Connected Classroom Pill Banner (hidden when offline)
            // =====================================================================
            GameObject connectedBanner = CreateImageObject("ConnectedClassBanner", canvas.transform, assets.badgePill, RetroUITheme.BtnMintGreen, false);
            RectTransform cbRt = connectedBanner.GetComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(0.5f, 1f); cbRt.anchorMax = new Vector2(0.5f, 1f);
            cbRt.pivot = new Vector2(0.5f, 1f);
            cbRt.anchoredPosition = new Vector2(210f, -34f);
            cbRt.sizeDelta = new Vector2(560f, 58f);
            connectedBanner.GetComponent<Image>().type = Image.Type.Sliced;

            GameObject cbIcon = CreateImageObject("Icon", connectedBanner.transform, assets.iconClassroom, Color.white, false);
            RectTransform cbiRt = cbIcon.GetComponent<RectTransform>();
            cbiRt.anchorMin = new Vector2(0f, 0.5f); cbiRt.anchorMax = new Vector2(0f, 0.5f);
            cbiRt.pivot = new Vector2(0f, 0.5f);
            cbiRt.anchoredPosition = new Vector2(18f, 0f);
            cbiRt.sizeDelta = new Vector2(32f, 32f);
            cbIcon.GetComponent<Image>().preserveAspect = true;

            TMP_Text cbText = CreateTmp("ConnectedBannerText", connectedBanner.transform, font, "Connected to Teacher's class", 17, FontStyles.Bold, RetroUITheme.InkWhite, TextAlignmentOptions.MidlineLeft);
            RectTransform cbtRt = cbText.GetComponent<RectTransform>();
            cbtRt.anchorMin = Vector2.zero; cbtRt.anchorMax = Vector2.one;
            cbtRt.offsetMin = new Vector2(60f, 4f); cbtRt.offsetMax = new Vector2(-152f, -4f);

            Button leaveClassBtn = CreateRetroButton("Btn-LeaveClass", connectedBanner.transform, assets, font, "Leave Class", null, RetroUITheme.BtnNeutralCream, RetroUITheme.InkDark, 14, out TMP_Text leaveClassLbl);
            RectTransform lcRt = leaveClassBtn.GetComponent<RectTransform>();
            lcRt.anchorMin = new Vector2(1f, 0.5f); lcRt.anchorMax = new Vector2(1f, 0.5f);
            lcRt.pivot = new Vector2(1f, 0.5f);
            lcRt.anchoredPosition = new Vector2(-12f, 0f);
            lcRt.sizeDelta = new Vector2(130f, 38f);
            connectedBanner.SetActive(false);

            // =====================================================================
            // 4. Center Main Student Desktop Window (Zero Btn-VirtualDesktop!)
            // =====================================================================
            GameObject mainWindow = CreateRetroDesktopWindow("MainMenuWindow", canvas.transform, assets, font,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -24f), new Vector2(860f, 760f),
                RetroUITheme.TitleBlue, RetroUITheme.WindowCream, assets.computerIcon,
                "Welcome to Your Computer!", showDecorativeWindowControls: true, out TMP_Text windowTitleTmp, out _);
            menuRefs.desktopTitleText = windowTitleTmp;

            // Mathematically centered content stack inside MainMenuWindow
            GameObject contentCenter = new GameObject("MenuContentCenter", typeof(RectTransform), typeof(VerticalLayoutGroup));
            contentCenter.transform.SetParent(mainWindow.transform, false);
            RectTransform ccRt = contentCenter.GetComponent<RectTransform>();
            ccRt.anchorMin = Vector2.zero; ccRt.anchorMax = Vector2.one;
            ccRt.offsetMin = new Vector2(56f, 26f); ccRt.offsetMax = new Vector2(-56f, -66f);

            VerticalLayoutGroup ccVlg = contentCenter.GetComponent<VerticalLayoutGroup>();
            ccVlg.spacing = 18f;
            ccVlg.childAlignment = TextAnchor.MiddleCenter;
            ccVlg.childControlWidth = false;
            ccVlg.childControlHeight = false;
            ccVlg.childForceExpandWidth = false;
            ccVlg.childForceExpandHeight = false;

            // Hero Computer Mascot Banner inside top of Window
            GameObject logoObj = CreateImageObject("MenuLogo", contentCenter.transform, assets.computerIcon ?? assets.mascotLogoNoBg, Color.white, false);
            RectTransform logoRt = logoObj.GetComponent<RectTransform>();
            logoRt.sizeDelta = new Vector2(176f, 138f);
            logoObj.GetComponent<Image>().preserveAspect = true;

            // Space Hint right above Play Button
            TMP_Text playSpaceHint = CreateTmp("PlaySpaceHint", contentCenter.transform, font, "Press SPACE or click Play to start", 17, FontStyles.Bold, RetroUITheme.InkMuted, TextAlignmentOptions.Center);
            playSpaceHint.GetComponent<RectTransform>().sizeDelta = new Vector2(640f, 28f);
            menuRefs.playSpaceHintText = playSpaceHint;

            // Prominent Hero Play Button
            Button btnPlay = CreateRetroButton("Btn-Play", contentCenter.transform, assets, font, "Play", assets.iconPlay, RetroUITheme.BtnSunnyGold, RetroUITheme.InkDark, 38, out _);
            btnPlay.GetComponent<RectTransform>().sizeDelta = new Vector2(640f, 114f);
            menuRefs.btnPlay = btnPlay;

            // Symmetric Middle Row: My Medals & Progress + Join Class
            GameObject middleRow = new GameObject("MiddleButtonsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            middleRow.transform.SetParent(contentCenter.transform, false);
            middleRow.GetComponent<RectTransform>().sizeDelta = new Vector2(640f, 88f);
            HorizontalLayoutGroup mrHlg = middleRow.GetComponent<HorizontalLayoutGroup>();
            mrHlg.spacing = 24f;
            mrHlg.childAlignment = TextAnchor.MiddleCenter;
            mrHlg.childControlWidth = true; mrHlg.childControlHeight = true;
            mrHlg.childForceExpandWidth = true; mrHlg.childForceExpandHeight = true;

            Button btnStats = CreateRetroButton("Btn-Stats", middleRow.transform, assets, font, "My Medals & Progress", assets.goldMedal, RetroUITheme.BtnSkyBlue, RetroUITheme.InkWhite, 21, out _);
            menuRefs.btnStats = btnStats;

            Button btnJoinClass = CreateRetroButton("Btn-JoinClass", middleRow.transform, assets, font, "Join Class", assets.iconClassroom, RetroUITheme.BtnMintGreen, RetroUITheme.InkWhite, 21, out _);
            menuRefs.btnJoinClass = btnJoinClass;

            // Symmetric Bottom Row: Language Pill + Exit Button
            GameObject bottomRow = new GameObject("BottomButtonsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            bottomRow.transform.SetParent(contentCenter.transform, false);
            bottomRow.GetComponent<RectTransform>().sizeDelta = new Vector2(640f, 92f);
            HorizontalLayoutGroup brHlg = bottomRow.GetComponent<HorizontalLayoutGroup>();
            brHlg.spacing = 24f;
            brHlg.childAlignment = TextAnchor.MiddleCenter;
            brHlg.childControlWidth = true; brHlg.childControlHeight = true;
            brHlg.childForceExpandWidth = true; brHlg.childForceExpandHeight = true;

            GameObject langContainer = CreateRetroCardWindow("LanguageBar", bottomRow.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(308f, 92f),
                assets.cardRounded, RetroUITheme.CardPastelBlue);

            GameObject globeImg = CreateImageObject("GlobeIcon", langContainer.transform, assets.iconGlobe, Color.white, false);
            RectTransform globeRt = globeImg.GetComponent<RectTransform>();
            globeRt.anchorMin = new Vector2(0f, 0.5f); globeRt.anchorMax = new Vector2(0f, 0.5f);
            globeRt.pivot = new Vector2(0f, 0.5f);
            globeRt.anchoredPosition = new Vector2(14f, 0f);
            globeRt.sizeDelta = new Vector2(36f, 36f);
            globeImg.GetComponent<Image>().preserveAspect = true;

            TMP_Text langLabel = CreateTmp("LanguageLabel", langContainer.transform, font, "Language", 15, FontStyles.Bold, RetroUITheme.InkDark, TextAlignmentOptions.Left);
            RectTransform llRt = langLabel.GetComponent<RectTransform>();
            llRt.anchorMin = new Vector2(0f, 1f); llRt.anchorMax = new Vector2(1f, 1f);
            llRt.pivot = new Vector2(0f, 1f);
            llRt.offsetMin = new Vector2(58f, -30f); llRt.offsetMax = new Vector2(-12f, -6f);
            menuRefs.languageLabelText = langLabel;

            GameObject dropdownObj = ConfigureOrCreateLanguageDropdown(preservedDropdown, langContainer.transform, assets, font);
            RectTransform ddRt = dropdownObj.GetComponent<RectTransform>();
            ddRt.anchorMin = new Vector2(0f, 0f); ddRt.anchorMax = new Vector2(1f, 0f);
            ddRt.pivot = new Vector2(0.5f, 0f);
            ddRt.offsetMin = new Vector2(58f, 10f); ddRt.offsetMax = new Vector2(-12f, 54f);

            Button btnExit = CreateRetroButton("Btn-2", bottomRow.transform, assets, font, "Exit", assets.iconExit, RetroUITheme.BtnCoralRed, RetroUITheme.InkWhite, 22, out _);
            menuRefs.btnExit = btnExit;

            // =====================================================================
            // 5. Discreet Bottom-Right Teacher Corner Button
            // =====================================================================
            GameObject teacherBtnRoot = CreateRetroButton("Btn-TeacherAccess", canvas.transform, assets, font, "Teacher", assets.iconLock, RetroUITheme.CardPastelYellow, RetroUITheme.InkDark, 16, out TMP_Text teacherBtnLbl).gameObject;
            RectTransform tbRt = teacherBtnRoot.GetComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(1f, 0f); tbRt.anchorMax = new Vector2(1f, 0f);
            tbRt.pivot = new Vector2(1f, 0f);
            tbRt.anchoredPosition = new Vector2(-34f, 28f);
            tbRt.sizeDelta = new Vector2(204f, 56f);

            Image teacherAccessIcon = teacherBtnRoot.transform.Find("Icon")?.GetComponent<Image>();
            GameObject holdBar = CreateImageObject("HoldProgressFill", teacherBtnRoot.transform, assets.badgePill, new Color(0.14f, 0.50f, 0.90f, 0.35f), false);
            RectTransform hbRt = holdBar.GetComponent<RectTransform>();
            hbRt.anchorMin = Vector2.zero; hbRt.anchorMax = Vector2.one;
            hbRt.offsetMin = new Vector2(4f, 4f); hbRt.offsetMax = new Vector2(-4f, -4f);
            Image hbImg = holdBar.GetComponent<Image>();
            hbImg.type = Image.Type.Filled;
            hbImg.fillMethod = Image.FillMethod.Horizontal;
            hbImg.fillAmount = 0f;
            holdBar.transform.SetAsFirstSibling();

            // =====================================================================
            // 6. Modals (Inactive by default in scene hierarchy)
            // =====================================================================
            GameObject studentJoinModal = BuildStudentJoinModal(canvas.transform, assets, font, classroomWidget);
            studentJoinModal.SetActive(false);

            GameObject pinModal = BuildTeacherPinModal(canvas.transform, assets, font, teacherGate);
            pinModal.SetActive(false);

            GameObject teacherPanel = BuildTeacherControlPanel(canvas.transform, assets, font, teacherGate, pathWidget, playtestWidget, classroomWidget);
            teacherPanel.SetActive(false);

            GameObject dashboardModal = BuildTeacherDashboardModal(canvas.transform, assets, font, out TeacherDashboardController dashboardCtrl);
            dashboardModal.SetActive(false);
            menuRefs.teacherDashboardController = dashboardCtrl;

            // Toast Banner (above everything)
            GameObject toastBanner = CreateImageObject("ToastBanner", canvas.transform, assets.badgePill, new Color(0.11f, 0.22f, 0.42f, 0.96f), false);
            RectTransform toastRt = toastBanner.GetComponent<RectTransform>();
            toastRt.anchorMin = new Vector2(0.5f, 0f); toastRt.anchorMax = new Vector2(0.5f, 0f);
            toastRt.pivot = new Vector2(0.5f, 0f);
            toastRt.anchoredPosition = new Vector2(0f, 30f);
            toastRt.sizeDelta = new Vector2(740f, 52f);
            toastBanner.GetComponent<Image>().type = Image.Type.Sliced;

            TMP_Text toastTmp = CreateTmp("ToastText", toastBanner.transform, font, "", 16, FontStyles.Bold, RetroUITheme.InkWhite, TextAlignmentOptions.Center);
            StretchFull(toastTmp.GetComponent<RectTransform>(), 16f, 6f);
            toastBanner.SetActive(false);

            // Wire remaining serialized references on PlaytestStudentWidget & ClassroomNetworkWidget & TeacherModeGate
            SetPrivateField(playtestWidget, "welcomeGreetingText", welcomeGreeting);
            SetPrivateField(playtestWidget, "studentNamePromptText", nameLabel);
            SetPrivateField(playtestWidget, "studentNameInputField", studentNameInput);
            SetPrivateField(playtestWidget, "studentNamePlaceholderText", namePlaceholder);
            SetPrivateField(playtestWidget, "toastContainer", toastBanner);
            SetPrivateField(playtestWidget, "toastText", toastTmp);

            SetPrivateField(classroomWidget, "connectedBannerRoot", connectedBanner);
            SetPrivateField(classroomWidget, "connectedBannerText", cbText);
            SetPrivateField(classroomWidget, "leaveClassButton", leaveClassBtn);
            SetPrivateField(classroomWidget, "leaveClassButtonLabel", leaveClassLbl);
            SetPrivateField(classroomWidget, "teacherDashboard", dashboardCtrl);

            SetPrivateField(teacherGate, "configOverride", AssetDatabase.LoadAssetAtPath<TeacherModeConfig>(TeacherConfigAssetPath));
            SetPrivateField(teacherGate, "teacherAccessRoot", teacherBtnRoot);
            SetPrivateField(teacherGate, "teacherAccessButton", teacherBtnRoot.GetComponent<Button>());
            SetPrivateField(teacherGate, "teacherAccessIcon", teacherAccessIcon);
            SetPrivateField(teacherGate, "teacherAccessLabel", teacherBtnLbl);
            SetPrivateField(teacherGate, "holdProgressFill", hbImg);
            SetPrivateField(teacherGate, "teacherDashboard", dashboardCtrl);
            SetPrivateField(teacherGate, "studentJoinModalRoot", studentJoinModal);

            EditorUtility.SetDirty(canvas.gameObject);
            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("[RetroSceneMaterializer] Menu.unity hierarchy materialized in Colorful Retro Desktop style.");
        }

        // =====================================================================
        // STATS SCENE MATERIALIZER
        // =====================================================================
        [MenuItem("Tools/Retro UI/3. Build Stats Scene Hierarchy")]
        public static void BuildStatsScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != "StatsScene")
            {
                Debug.LogError($"[RetroSceneMaterializer] Open StatsScene.unity first (active: {activeScene.name}).");
                return;
            }

            RetroThemeAssets assets = EnsureThemeAssets();
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MainFontPath);
            Canvas canvas = FindPrimaryCanvas(activeScene, "Canvas");
            if (canvas == null)
            {
                Debug.LogError("[RetroSceneMaterializer] Could not find Canvas in StatsScene.unity.");
                return;
            }

            ConfigureCanvasScaler(canvas);
            StatsSceneController ctrl = canvas.GetComponent<StatsSceneController>() ?? UnityEngine.Object.FindAnyObjectByType<StatsSceneController>();
            if (ctrl == null)
                ctrl = canvas.gameObject.AddComponent<StatsSceneController>();

            ClearChildrenImmediate(canvas.transform);

            // Wallpaper
            GameObject bg = CreateImageObject("Bg", canvas.transform, assets.desktopWallpaper, RetroUITheme.SkyBackdropTint, false);
            StretchFull(bg.GetComponent<RectTransform>());

            // Top Retro Header Bar (Window TitleBar style)
            GameObject header = CreateImageObject("Header", canvas.transform, assets.windowTitleBar, RetroUITheme.TitleBlue, false);
            header.GetComponent<Image>().type = Image.Type.Sliced;
            RectTransform headerRt = header.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f); headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.offsetMin = new Vector2(36f, -112f); headerRt.offsetMax = new Vector2(-36f, -24f);

            GameObject hdrIcon = CreateImageObject("MedalIcon", header.transform, assets.goldMedal, Color.white, false);
            RectTransform hiRt = hdrIcon.GetComponent<RectTransform>();
            hiRt.anchorMin = new Vector2(0f, 0.5f); hiRt.anchorMax = new Vector2(0f, 0.5f);
            hiRt.pivot = new Vector2(0f, 0.5f);
            hiRt.anchoredPosition = new Vector2(22f, 0f);
            hiRt.sizeDelta = new Vector2(48f, 48f);
            hdrIcon.GetComponent<Image>().preserveAspect = true;

            TMP_Text headerTitle = CreateTmp("Title", header.transform, font, "My Medals & Progress", 28, FontStyles.Bold, RetroUITheme.InkWhite, TextAlignmentOptions.BottomLeft);
            RectTransform htRt = headerTitle.GetComponent<RectTransform>();
            htRt.anchorMin = new Vector2(0f, 0.44f); htRt.anchorMax = new Vector2(1f, 1f);
            htRt.offsetMin = new Vector2(82f, 0f); htRt.offsetMax = new Vector2(-620f, -8f);

            TMP_Text headerSub = CreateTmp("Subtitle", header.transform, font, "Track your progress, medals, accuracy, and best times across all 16 levels", 14, FontStyles.Normal, new Color(0.90f, 0.96f, 1f, 1f), TextAlignmentOptions.TopLeft);
            RectTransform hsRt = headerSub.GetComponent<RectTransform>();
            hsRt.anchorMin = new Vector2(0f, 0f); hsRt.anchorMax = new Vector2(1f, 0.44f);
            hsRt.offsetMin = new Vector2(82f, 8f); hsRt.offsetMax = new Vector2(-620f, 0f);

            Button backBtn = CreateRetroButton("Btn-Back", header.transform, assets, font, "Back to Menu", assets.iconExit, RetroUITheme.BtnNeutralCream, RetroUITheme.InkDark, 16, out TMP_Text backLbl);
            RectTransform bbRt = backBtn.GetComponent<RectTransform>();
            bbRt.anchorMin = new Vector2(1f, 0.5f); bbRt.anchorMax = new Vector2(1f, 0.5f);
            bbRt.pivot = new Vector2(1f, 0.5f);
            bbRt.anchoredPosition = new Vector2(-416f, 0f);
            bbRt.sizeDelta = new Vector2(174f, 50f);

            Button refreshBtn = CreateRetroButton("Btn-Refresh", header.transform, assets, font, "Refresh Records", null, RetroUITheme.BtnSkyBlue, RetroUITheme.InkWhite, 16, out TMP_Text refreshLbl);
            RectTransform rfRt = refreshBtn.GetComponent<RectTransform>();
            rfRt.anchorMin = new Vector2(1f, 0.5f); rfRt.anchorMax = new Vector2(1f, 0.5f);
            rfRt.pivot = new Vector2(1f, 0.5f);
            rfRt.anchoredPosition = new Vector2(-236f, 0f);
            rfRt.sizeDelta = new Vector2(168f, 50f);

            Button retakeBtn = CreateRetroButton("Btn-Retake", header.transform, assets, font, "Retake Assessment", assets.iconPlay, RetroUITheme.BtnSunnyGold, RetroUITheme.InkDark, 16, out TMP_Text retakeLbl);
            RectTransform rbRt = retakeBtn.GetComponent<RectTransform>();
            rbRt.anchorMin = new Vector2(1f, 0.5f); rbRt.anchorMax = new Vector2(1f, 0.5f);
            rbRt.pivot = new Vector2(1f, 0.5f);
            rbRt.anchoredPosition = new Vector2(-18f, 0f);
            rbRt.sizeDelta = new Vector2(206f, 50f);

            // Unified Retro Body (Identical to in-game ScoresApp)
            GameObject bodyRoot = new GameObject("SharedStatsBody", typeof(RectTransform));
            bodyRoot.transform.SetParent(canvas.transform, false);
            RectTransform brRt = bodyRoot.GetComponent<RectTransform>();
            brRt.anchorMin = Vector2.zero; brRt.anchorMax = Vector2.one;
            brRt.offsetMin = new Vector2(36f, 26f); brRt.offsetMax = new Vector2(-36f, -124f);

            RetroStatsSharedUI.StatsUIBinding sharedBinding = RetroStatsSharedUI.BuildStatsBody(bodyRoot.transform, assets, font);
            RetroStatsSharedUI.RenderAll(sharedBinding, RetroStatsSharedUI.LevelFilter.All, assets, font);

            // Wire StatsSceneController
            SetPrivateField(ctrl, "headerTitleText", headerTitle);
            SetPrivateField(ctrl, "headerSubtitleText", headerSub);
            SetPrivateField(ctrl, "backToMenuButton", backBtn);
            SetPrivateField(ctrl, "backButtonLabel", backLbl);
            SetPrivateField(ctrl, "retakeAssessmentButton", retakeBtn);
            SetPrivateField(ctrl, "retakeButtonLabel", retakeLbl);
            SetPrivateField(ctrl, "refreshButton", refreshBtn);
            SetPrivateField(ctrl, "refreshButtonLabel", refreshLbl);
            SetPrivateField(ctrl, "sharedUI", sharedBinding);

            // Re-place header last so buttons never lose raycasts
            header.transform.SetAsLastSibling();

            EditorUtility.SetDirty(canvas.gameObject);
            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("[RetroSceneMaterializer] StatsScene.unity materialized in Colorful Retro Desktop style.");
        }

        // =====================================================================
        // ASSESSMENT SCENE MATERIALIZER
        // =====================================================================
        [MenuItem("Tools/Retro UI/4. Build Assessment Scene Hierarchy")]
        public static void BuildAssessmentScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != "AssessmentScene")
            {
                Debug.LogError($"[RetroSceneMaterializer] Open AssessmentScene.unity first (active: {activeScene.name}).");
                return;
            }

            RetroThemeAssets assets = EnsureThemeAssets();
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MainFontPath);
            Canvas canvas = FindPrimaryCanvas(activeScene, "Canvas");
            if (canvas == null)
            {
                Debug.LogError("[RetroSceneMaterializer] Could not find Canvas in AssessmentScene.unity.");
                return;
            }

            ConfigureCanvasScaler(canvas);
            InitialTestManager testMgr = UnityEngine.Object.FindAnyObjectByType<InitialTestManager>();
            if (testMgr == null)
            {
                Debug.LogError("[RetroSceneMaterializer] InitialTestManager not found in AssessmentScene.unity.");
                return;
            }

            ClearChildrenImmediate(canvas.transform);

            // Wallpaper
            GameObject wallpaper = CreateImageObject("DesktopWallpaper", canvas.transform, assets.desktopWallpaper, RetroUITheme.SkyBackdropTint, false);
            StretchFull(wallpaper.GetComponent<RectTransform>());

            // Main Retro Window Root Container
            GameObject rootWindow = CreateRetroDesktopWindow("AssessmentRetroWindow", canvas.transform, assets, font,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                RetroUITheme.TitleBlue, RetroUITheme.WindowCream, assets.targetIcon,
                "Skills Adventure", false, out TMP_Text stepBadgeText, out Transform titleBarTr);
            RectTransform rwRt = rootWindow.GetComponent<RectTransform>();
            rwRt.anchorMin = Vector2.zero; rwRt.anchorMax = Vector2.one;
            rwRt.offsetMin = new Vector2(40f, 30f); rwRt.offsetMax = new Vector2(-40f, -24f);
            stepBadgeText.fontSize = 22;
            RectTransform sbBadgeRt = stepBadgeText.GetComponent<RectTransform>();
            sbBadgeRt.offsetMax = new Vector2(-296f, -2f);

            // Buttons on right of Title Bar
            Button skipBtn = CreateRetroButton("Btn-Skip", titleBarTr, assets, font, "Skip", null, RetroUITheme.BtnSunnyGold, RetroUITheme.InkDark, 16, out TMP_Text skipLbl);
            RectTransform skRt = skipBtn.GetComponent<RectTransform>();
            skRt.anchorMin = new Vector2(1f, 0.5f); skRt.anchorMax = new Vector2(1f, 0.5f);
            skRt.pivot = new Vector2(1f, 0.5f);
            skRt.anchoredPosition = new Vector2(-148f, 0f);
            skRt.sizeDelta = new Vector2(130f, 40f);

            Button menuBtn = CreateRetroButton("Btn-Exit", titleBarTr, assets, font, "Menu", assets.iconExit, RetroUITheme.BtnCoralRed, RetroUITheme.InkWhite, 16, out TMP_Text menuLbl);
            RectTransform mnRt = menuBtn.GetComponent<RectTransform>();
            mnRt.anchorMin = new Vector2(1f, 0.5f); mnRt.anchorMax = new Vector2(1f, 0.5f);
            mnRt.pivot = new Vector2(1f, 0.5f);
            mnRt.anchoredPosition = new Vector2(-14f, 0f);
            mnRt.sizeDelta = new Vector2(124f, 40f);

            // Instruction Header Card
            GameObject promptCard = CreateRetroCardWindow("InstructionBanner", rootWindow.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, Vector2.zero, assets.cardRounded, RetroUITheme.CardPastelBlue);
            RectTransform pcRt = promptCard.GetComponent<RectTransform>();
            pcRt.offsetMin = new Vector2(24f, -184f); pcRt.offsetMax = new Vector2(-24f, -70f);

            TextMeshProUGUI taskTitle = CreateTmp("TaskTitleText", promptCard.transform, font, "Move Cursor", 28, FontStyles.Bold, RetroUITheme.InkDark, TextAlignmentOptions.Left) as TextMeshProUGUI;
            RectTransform ttRt = taskTitle.GetComponent<RectTransform>();
            ttRt.anchorMin = new Vector2(0f, 1f); ttRt.anchorMax = new Vector2(1f, 1f);
            ttRt.offsetMin = new Vector2(22f, -44f); ttRt.offsetMax = new Vector2(-22f, -10f);

            TextMeshProUGUI instrText = CreateTmp("InstructionText", promptCard.transform, font, "Move the cursor into the target!", 22, FontStyles.Normal, RetroUITheme.InkDark, TextAlignmentOptions.Left) as TextMeshProUGUI;
            RectTransform itRt = instrText.GetComponent<RectTransform>();
            itRt.anchorMin = new Vector2(0f, 0f); itRt.anchorMax = new Vector2(1f, 1f);
            itRt.offsetMin = new Vector2(22f, 22f); itRt.offsetMax = new Vector2(-22f, -46f);

            // Timer Progress Track
            GameObject timerTrack = CreateImageObject("TimerTrack", promptCard.transform, assets.inputBox, RetroUITheme.ProgressTrack, false);
            timerTrack.GetComponent<Image>().type = Image.Type.Sliced;
            RectTransform ttrRt = timerTrack.GetComponent<RectTransform>();
            ttrRt.anchorMin = new Vector2(0f, 0f); ttrRt.anchorMax = new Vector2(1f, 0f);
            ttrRt.pivot = new Vector2(0.5f, 0f);
            ttrRt.offsetMin = new Vector2(18f, 8f); ttrRt.offsetMax = new Vector2(-18f, 18f);

            GameObject timerFill = CreateImageObject("TimerFill", timerTrack.transform, assets.badgePill, RetroUITheme.BtnSkyBlue, false);
            timerFill.GetComponent<Image>().type = Image.Type.Sliced;
            RectTransform tfRt = timerFill.GetComponent<RectTransform>();
            tfRt.anchorMin = Vector2.zero; tfRt.anchorMax = Vector2.one;
            tfRt.pivot = new Vector2(0f, 0.5f);
            tfRt.offsetMin = new Vector2(1f, 1f); tfRt.offsetMax = new Vector2(-1f, -1f);

            // Interactive Play Stage Card
            GameObject stageCard = CreateRetroCardWindow("StageCardFrame", rootWindow.transform,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, assets.inputBox, RetroUITheme.CardWhite);
            RectTransform scRt = stageCard.GetComponent<RectTransform>();
            scRt.offsetMin = new Vector2(24f, 52f); scRt.offsetMax = new Vector2(-24f, -198f);

            GameObject stageAreaObj = new GameObject("TaskStage", typeof(RectTransform));
            stageAreaObj.transform.SetParent(stageCard.transform, false);
            RectTransform stageRt = stageAreaObj.GetComponent<RectTransform>();
            StretchFull(stageRt, 16f, 16f);

            TextMeshProUGUI statusText = CreateTmp("StatusText", rootWindow.transform, font, "", 18, FontStyles.Bold, RetroUITheme.WarningAmber, TextAlignmentOptions.Center) as TextMeshProUGUI;
            RectTransform stRt = statusText.GetComponent<RectTransform>();
            stRt.anchorMin = new Vector2(0f, 0f); stRt.anchorMax = new Vector2(1f, 0f);
            stRt.pivot = new Vector2(0.5f, 0f);
            stRt.offsetMin = new Vector2(24f, 12f); stRt.offsetMax = new Vector2(-24f, 44f);

            // Intro Modal
            GameObject introModal = CreateImageObject("AssessmentIntroModal", canvas.transform, null, RetroUITheme.OverlayScrim, true);
            StretchFull(introModal.GetComponent<RectTransform>());

            GameObject introCard = CreateRetroDesktopWindow("IntroCard", introModal.transform, assets, font,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 460f),
                RetroUITheme.TitleMint, RetroUITheme.WindowCream, assets.mascotLogoNoBg ?? assets.targetIcon,
                "Welcome to the Skills Test!", false, out TMP_Text introTitle, out _);

            GameObject mascotPreview = CreateImageObject("IntroMascot", introCard.transform, assets.mascotLogoNoBg ?? assets.computerIcon, Color.white, false);
            RectTransform mpRt = mascotPreview.GetComponent<RectTransform>();
            mpRt.anchorMin = new Vector2(0.5f, 1f); mpRt.anchorMax = new Vector2(0.5f, 1f);
            mpRt.pivot = new Vector2(0.5f, 1f);
            mpRt.anchoredPosition = new Vector2(0f, -72f);
            mpRt.sizeDelta = new Vector2(104f, 104f);
            mascotPreview.GetComponent<Image>().preserveAspect = true;

            TextMeshProUGUI introDesc = CreateTmp("Description", introCard.transform, font,
                "We will do a few fun quick challenges with the mouse. Take your time!", 20, FontStyles.Normal, RetroUITheme.InkDark, TextAlignmentOptions.Center) as TextMeshProUGUI;
            RectTransform idRt = introDesc.GetComponent<RectTransform>();
            idRt.anchorMin = new Vector2(0f, 0f); idRt.anchorMax = new Vector2(1f, 1f);
            idRt.offsetMin = new Vector2(44f, 114f); idRt.offsetMax = new Vector2(-44f, -186f);

            Button introStartBtn = CreateRetroButton("Btn-StartAssessment", introCard.transform, assets, font, "Press SPACE or click to start", assets.iconPlay, RetroUITheme.BtnSunnyGold, RetroUITheme.InkDark, 21, out TMP_Text introPrompt);
            RectTransform isRt = introStartBtn.GetComponent<RectTransform>();
            isRt.anchorMin = new Vector2(0.5f, 0f); isRt.anchorMax = new Vector2(0.5f, 0f);
            isRt.pivot = new Vector2(0.5f, 0f);
            isRt.anchoredPosition = new Vector2(0f, 28f);
            isRt.sizeDelta = new Vector2(480f, 68f);

            // Wire InitialTestManager serialized refs
            SetPrivateField(testMgr, "sceneCanvas", canvas);
            SetPrivateField(testMgr, "taskContentArea", canvas.GetComponent<RectTransform>());
            SetPrivateField(testMgr, "serializedRootContainer", rootWindow);
            SetPrivateField(testMgr, "serializedIntroModal", introModal);
            SetPrivateField(testMgr, "serializedIntroTitleText", introTitle);
            SetPrivateField(testMgr, "serializedIntroDescriptionText", introDesc);
            SetPrivateField(testMgr, "serializedIntroStartButton", introStartBtn);
            SetPrivateField(testMgr, "serializedIntroPromptText", introPrompt);
            SetPrivateField(testMgr, "serializedStepText", stepBadgeText as TextMeshProUGUI);
            SetPrivateField(testMgr, "serializedTitleText", taskTitle);
            SetPrivateField(testMgr, "serializedInstructionText", instrText);
            SetPrivateField(testMgr, "serializedStatusText", statusText);
            SetPrivateField(testMgr, "serializedTimerBarFill", tfRt);
            SetPrivateField(testMgr, "serializedStageArea", stageRt);
            SetPrivateField(testMgr, "serializedExitButton", menuBtn);
            SetPrivateField(testMgr, "serializedExitButtonLabel", menuLbl as TextMeshProUGUI);
            SetPrivateField(testMgr, "serializedSkipButton", skipBtn);
            SetPrivateField(testMgr, "serializedSkipButtonLabel", skipLbl as TextMeshProUGUI);

            EditorUtility.SetDirty(canvas.gameObject);
            EditorUtility.SetDirty(testMgr);
            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("[RetroSceneMaterializer] AssessmentScene.unity materialized in Colorful Retro Desktop style.");
        }

        // =====================================================================
        // MODAL & SUB-PANEL BUILDERS FOR MENU.UNITY
        // =====================================================================
        private static void BuildDesktopShortcutDecoration(Transform canvasTr, RetroThemeAssets assets, TMP_FontAsset font)
        {
            GameObject decoRoot = new GameObject("DesktopShortcutsDecor", typeof(RectTransform));
            decoRoot.transform.SetParent(canvasTr, false);
            StretchFull(decoRoot.GetComponent<RectTransform>());

            (string name, Sprite sp, float y)[] shortcuts =
            {
                ("Icon_FolderBlue", assets.folderBlue, -240f),
                ("Icon_FolderGreen", assets.folderGreen, -370f),
                ("Icon_FolderYellow", assets.folderYellow, -500f),
                ("Icon_Medal", assets.goldMedal, -630f),
            };

            foreach (var s in shortcuts)
            {
                GameObject tile = new GameObject(s.name, typeof(RectTransform));
                tile.transform.SetParent(decoRoot.transform, false);
                RectTransform rt = tile.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(54f, s.y);
                rt.sizeDelta = new Vector2(90f, 90f);

                GameObject icon = CreateImageObject("Sprite", tile.transform, s.sp, Color.white, false);
                StretchFull(icon.GetComponent<RectTransform>(), 8f, 8f);
                icon.GetComponent<Image>().preserveAspect = true;
            }
        }

        private static GameObject BuildStudentJoinModal(Transform canvasTr, RetroThemeAssets assets, TMP_FontAsset font, ClassroomNetworkWidget classroomWidget)
        {
            GameObject overlay = CreateImageObject("StudentJoinModal", canvasTr, null, RetroUITheme.OverlayScrim, true);
            StretchFull(overlay.GetComponent<RectTransform>());

            GameObject win = CreateRetroDesktopWindow("JoinClassWindow", overlay.transform, assets, font,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 560f),
                RetroUITheme.TitleMint, RetroUITheme.WindowCream, assets.iconClassroom,
                "Join Your Class", false, out TMP_Text titleTmp, out Transform titleBarTr);

            Button closeBtn = CreateCloseIconButton("Btn-CloseJoin", titleBarTr, assets);

            TMP_Text statusText = CreateTmp("DiscoveryStatusText", win.transform, font, "Looking for your teacher's class...", 18, FontStyles.Bold, RetroUITheme.TitleBlue, TextAlignmentOptions.Left);
            RectTransform stRt = statusText.GetComponent<RectTransform>();
            stRt.anchorMin = new Vector2(0f, 1f); stRt.anchorMax = new Vector2(1f, 1f);
            stRt.offsetMin = new Vector2(26f, -104f); stRt.offsetMax = new Vector2(-26f, -68f);

            // Hosts Scroll Area
            GameObject scrollBox = CreateRetroScrollList("HostsScroll", win.transform, assets, out Transform contentTr);
            RectTransform sbRt = scrollBox.GetComponent<RectTransform>();
            sbRt.anchorMin = Vector2.zero; sbRt.anchorMax = Vector2.one;
            sbRt.offsetMin = new Vector2(24f, 168f); sbRt.offsetMax = new Vector2(-24f, -112f);

            TMP_Text emptyText = CreateTmp("EmptyHostsText", scrollBox.transform, font, "No classrooms found yet.", 18, FontStyles.Normal, RetroUITheme.InkMuted, TextAlignmentOptions.Center);
            StretchFull(emptyText.GetComponent<RectTransform>(), 24f, 24f);

            // Host Row Template
            GameObject hostRow = CreateImageObject("HostRowTemplate", contentTr, assets.cardRounded, RetroUITheme.CardPastelBlue, false);
            hostRow.GetComponent<Image>().type = Image.Type.Sliced;
            LayoutElement hrLe = hostRow.AddComponent<LayoutElement>();
            hrLe.preferredHeight = 68f; hrLe.minHeight = 68f;

            TMP_Text hostNameTmp = CreateTmp("HostNameText", hostRow.transform, font, "Teacher's Class", 19, FontStyles.Bold, RetroUITheme.InkDark, TextAlignmentOptions.MidlineLeft);
            RectTransform hnRt = hostNameTmp.GetComponent<RectTransform>();
            hnRt.anchorMin = Vector2.zero; hnRt.anchorMax = Vector2.one;
            hnRt.offsetMin = new Vector2(18f, 4f); hnRt.offsetMax = new Vector2(-156f, -4f);

            Button joinHostBtn = CreateRetroButton("Btn-JoinHost", hostRow.transform, assets, font, "Join", assets.iconPlay, RetroUITheme.BtnMintGreen, RetroUITheme.InkWhite, 17, out _);
            RectTransform jhRt = joinHostBtn.GetComponent<RectTransform>();
            jhRt.anchorMin = new Vector2(1f, 0.5f); jhRt.anchorMax = new Vector2(1f, 0.5f);
            jhRt.pivot = new Vector2(1f, 0.5f);
            jhRt.anchoredPosition = new Vector2(-12f, 0f);
            jhRt.sizeDelta = new Vector2(132f, 46f);
            hostRow.SetActive(false);

            // Secondary Manual IP Link & Section
            Button toggleManualBtn = CreateRetroButton("Btn-ToggleManualIp", win.transform, assets, font, "Use teacher IP address instead", null, RetroUITheme.BtnNeutralCream, RetroUITheme.InkMuted, 15, out TMP_Text toggleManualLbl);
            RectTransform tmRt = toggleManualBtn.GetComponent<RectTransform>();
            tmRt.anchorMin = new Vector2(0f, 0f); tmRt.anchorMax = new Vector2(1f, 0f);
            tmRt.pivot = new Vector2(0.5f, 0f);
            tmRt.offsetMin = new Vector2(24f, 114f); tmRt.offsetMax = new Vector2(-24f, 154f);

            GameObject manualSection = new GameObject("ManualIpSection", typeof(RectTransform));
            manualSection.transform.SetParent(win.transform, false);
            RectTransform msRt = manualSection.GetComponent<RectTransform>();
            msRt.anchorMin = new Vector2(0f, 0f); msRt.anchorMax = new Vector2(1f, 0f);
            msRt.offsetMin = new Vector2(24f, 52f); msRt.offsetMax = new Vector2(-24f, 104f);

            TMP_InputField ipInput = CreateRetroInputField("Input-ManualIp", manualSection.transform, assets, font, "Teacher IP (e.g. 192.168.1.50)", 18, TMP_InputField.ContentType.Standard, out TMP_Text ipPlaceholder);
            RectTransform ipRt = ipInput.GetComponent<RectTransform>();
            ipRt.anchorMin = Vector2.zero; ipRt.anchorMax = Vector2.one;
            ipRt.offsetMin = Vector2.zero; ipRt.offsetMax = new Vector2(-150f, 0f);

            Button connectIpBtn = CreateRetroButton("Btn-ConnectManual", manualSection.transform, assets, font, "Join", null, RetroUITheme.BtnSkyBlue, RetroUITheme.InkWhite, 17, out TMP_Text connectIpLbl);
            RectTransform ciRt = connectIpBtn.GetComponent<RectTransform>();
            ciRt.anchorMin = new Vector2(1f, 0f); ciRt.anchorMax = new Vector2(1f, 1f);
            ciRt.pivot = new Vector2(1f, 0.5f);
            ciRt.offsetMin = new Vector2(-138f, 0f); ciRt.offsetMax = Vector2.zero;

            TMP_Text errorTmp = CreateTmp("ClientErrorText", win.transform, font, "", 15, FontStyles.Bold, RetroUITheme.DangerCoral, TextAlignmentOptions.Center);
            RectTransform errRt = errorTmp.GetComponent<RectTransform>();
            errRt.anchorMin = new Vector2(0f, 0f); errRt.anchorMax = new Vector2(1f, 0f);
            errRt.offsetMin = new Vector2(24f, 14f); errRt.offsetMax = new Vector2(-24f, 46f);

            SetPrivateField(classroomWidget, "joinModalRoot", overlay);
            SetPrivateField(classroomWidget, "joinModalTitleText", titleTmp);
            SetPrivateField(classroomWidget, "joinModalCloseButton", closeBtn);
            SetPrivateField(classroomWidget, "discoveryStatusText", statusText);
            SetPrivateField(classroomWidget, "discoveredHostsContainer", contentTr);
            SetPrivateField(classroomWidget, "hostRowTemplate", hostRow);
            SetPrivateField(classroomWidget, "emptyHostsText", emptyText);
            SetPrivateField(classroomWidget, "toggleManualIpButton", toggleManualBtn);
            SetPrivateField(classroomWidget, "toggleManualIpLabel", toggleManualLbl);
            SetPrivateField(classroomWidget, "manualIpSectionRoot", manualSection);
            SetPrivateField(classroomWidget, "manualIpInputField", ipInput);
            SetPrivateField(classroomWidget, "manualIpPlaceholderText", ipPlaceholder);
            SetPrivateField(classroomWidget, "connectManualButton", connectIpBtn);
            SetPrivateField(classroomWidget, "connectManualLabel", connectIpLbl);
            SetPrivateField(classroomWidget, "clientErrorText", errorTmp);

            return overlay;
        }

        private static GameObject BuildTeacherPinModal(Transform canvasTr, RetroThemeAssets assets, TMP_FontAsset font, TeacherModeGate gate)
        {
            GameObject overlay = CreateImageObject("TeacherPinModal", canvasTr, null, RetroUITheme.OverlayScrim, true);
            StretchFull(overlay.GetComponent<RectTransform>());

            GameObject win = CreateRetroDesktopWindow("PinWindow", overlay.transform, assets, font,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460f, 620f),
                RetroUITheme.TitlePlum, RetroUITheme.WindowCream, assets.iconLock,
                "Teacher Mode", false, out TMP_Text titleTmp, out Transform titleBarTr);

            Button closeBtn = CreateCloseIconButton("Btn-ClosePin", titleBarTr, assets);

            TMP_Text promptTmp = CreateTmp("PinPromptText", win.transform, font, "Enter the 4-digit teacher PIN:", 18, FontStyles.Bold, RetroUITheme.InkDark, TextAlignmentOptions.Center);
            RectTransform prRt = promptTmp.GetComponent<RectTransform>();
            prRt.anchorMin = new Vector2(0f, 1f); prRt.anchorMax = new Vector2(1f, 1f);
            prRt.offsetMin = new Vector2(24f, -104f); prRt.offsetMax = new Vector2(-24f, -68f);

            // 4 PIN Dots
            GameObject dotsRow = new GameObject("PinDotsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            dotsRow.transform.SetParent(win.transform, false);
            RectTransform drRt = dotsRow.GetComponent<RectTransform>();
            drRt.anchorMin = new Vector2(0.5f, 1f); drRt.anchorMax = new Vector2(0.5f, 1f);
            drRt.pivot = new Vector2(0.5f, 1f);
            drRt.anchoredPosition = new Vector2(0f, -112f);
            drRt.sizeDelta = new Vector2(260f, 40f);
            HorizontalLayoutGroup drHlg = dotsRow.GetComponent<HorizontalLayoutGroup>();
            drHlg.spacing = 22f; drHlg.childAlignment = TextAnchor.MiddleCenter;
            drHlg.childControlWidth = false; drHlg.childControlHeight = false;

            Image[] dotImgs = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                GameObject dot = CreateImageObject($"PinDot_{i}", dotsRow.transform, assets.badgePill, RetroUITheme.ProgressTrack, false);
                dot.GetComponent<Image>().type = Image.Type.Sliced;
                dot.GetComponent<RectTransform>().sizeDelta = new Vector2(40f, 40f);
                dotImgs[i] = dot.GetComponent<Image>();
            }

            // Physical Keyboard Focusable PIN Input Field (supports keyboard + Enter)
            TMP_InputField pinInput = CreateRetroInputField("PinKeyboardInput", win.transform, assets, font, "Type 4-digit PIN...", 4, TMP_InputField.ContentType.Pin, out TMP_Text pinPlaceholder);
            RectTransform piRt = pinInput.GetComponent<RectTransform>();
            piRt.anchorMin = new Vector2(0.5f, 1f); piRt.anchorMax = new Vector2(0.5f, 1f);
            piRt.pivot = new Vector2(0.5f, 1f);
            piRt.anchoredPosition = new Vector2(0f, -162f);
            piRt.sizeDelta = new Vector2(280f, 44f);
            pinInput.readOnly = true;
            if (pinInput.textComponent != null)
            {
                pinInput.textComponent.alignment = TextAlignmentOptions.Center;
                pinInput.textComponent.fontSize = 24f;
                pinInput.textComponent.fontStyle = FontStyles.Bold;
                pinInput.textComponent.characterSpacing = 14f;
            }
            if (pinPlaceholder != null)
            {
                pinPlaceholder.alignment = TextAlignmentOptions.Center;
                pinPlaceholder.fontSize = 15f;
            }

            TMP_Text errorTmp = CreateTmp("PinErrorText", win.transform, font, "", 15, FontStyles.Bold, RetroUITheme.DangerCoral, TextAlignmentOptions.Center);
            RectTransform errRt = errorTmp.GetComponent<RectTransform>();
            errRt.anchorMin = new Vector2(0f, 1f); errRt.anchorMax = new Vector2(1f, 1f);
            errRt.offsetMin = new Vector2(24f, -238f); errRt.offsetMax = new Vector2(-24f, -210f);

            // Numeric Keypad (Grid 3 cols)
            GameObject grid = new GameObject("PinKeypadGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            grid.transform.SetParent(win.transform, false);
            RectTransform grRt = grid.GetComponent<RectTransform>();
            grRt.anchorMin = new Vector2(0.5f, 0f); grRt.anchorMax = new Vector2(0.5f, 1f);
            grRt.pivot = new Vector2(0.5f, 0.5f);
            grRt.offsetMin = new Vector2(-175f, 20f); grRt.offsetMax = new Vector2(175f, -242f);

            GridLayoutGroup glg = grid.GetComponent<GridLayoutGroup>();
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 3;
            glg.cellSize = new Vector2(104f, 74f);
            glg.spacing = new Vector2(16f, 14f);
            glg.childAlignment = TextAnchor.UpperCenter;

            Button[] digits = new Button[10];
            for (int d = 1; d <= 9; d++)
                digits[d] = CreateRetroButton($"Btn-Key{d}", grid.transform, assets, font, d.ToString(), null, RetroUITheme.CardPastelBlue, RetroUITheme.InkDark, 28, out _);

            Button clearBtn = CreateRetroButton("Btn-KeyClear", grid.transform, assets, font, "Clear", null, RetroUITheme.BtnWarmOrange, RetroUITheme.InkWhite, 16, out TMP_Text clearLbl);
            digits[0] = CreateRetroButton("Btn-Key0", grid.transform, assets, font, "0", null, RetroUITheme.CardPastelBlue, RetroUITheme.InkDark, 28, out _);
            Button backBtn = CreateRetroButton("Btn-KeyBack", grid.transform, assets, font, "Del", null, RetroUITheme.BtnSoftSlate, RetroUITheme.InkDark, 16, out TMP_Text backLbl);

            SetPrivateField(gate, "pinModalRoot", overlay);
            SetPrivateField(gate, "pinTitleText", titleTmp);
            SetPrivateField(gate, "pinPromptText", promptTmp);
            SetPrivateField(gate, "pinInputField", pinInput);
            SetPrivateField(gate, "pinDotImages", dotImgs);
            SetPrivateField(gate, "pinErrorText", errorTmp);
            SetPrivateField(gate, "digitButtons", digits);
            SetPrivateField(gate, "pinBackspaceButton", backBtn);
            SetPrivateField(gate, "pinBackspaceLabel", backLbl);
            SetPrivateField(gate, "pinClearButton", clearBtn);
            SetPrivateField(gate, "pinClearLabel", clearLbl);
            SetPrivateField(gate, "pinCloseButton", closeBtn);

            return overlay;
        }

        private static GameObject BuildTeacherControlPanel(
            Transform canvasTr, RetroThemeAssets assets, TMP_FontAsset font,
            TeacherModeGate gate, LearningPathSelectorWidget pathWidget, PlaytestStudentWidget playtestWidget, ClassroomNetworkWidget classroomWidget)
        {
            GameObject overlay = CreateImageObject("TeacherModeOverlay", canvasTr, null, RetroUITheme.OverlayScrim, true);
            StretchFull(overlay.GetComponent<RectTransform>());

            GameObject win = CreateRetroDesktopWindow("TeacherControlWindow", overlay.transform, assets, font,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1260f, 760f),
                RetroUITheme.TitlePlum, RetroUITheme.WindowCream, assets.iconGear,
                "Teacher Control Window", false, out TMP_Text panelTitle, out Transform titleBarTr);

            Button closeBtn = CreateCloseIconButton("Btn-CloseTeacherPanel", titleBarTr, assets);

            TMP_Text subtitleTmp = CreateTmp("SubtitleText", win.transform, font, "Classroom setup, learning paths & student tools", 16, FontStyles.Normal, RetroUITheme.InkMuted, TextAlignmentOptions.Left);
            RectTransform subRt = subtitleTmp.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0f, 1f); subRt.anchorMax = new Vector2(1f, 1f);
            subRt.offsetMin = new Vector2(28f, -100f); subRt.offsetMax = new Vector2(-28f, -68f);

            // LAN Client Lock Notice Banner (shown when connected to another Teacher Host)
            GameObject lanBanner = CreateImageObject("LanLockNoticeBanner", win.transform, assets.badgePill, RetroUITheme.CardPastelYellow, false);
            lanBanner.GetComponent<Image>().type = Image.Type.Sliced;
            RectTransform lbRt = lanBanner.GetComponent<RectTransform>();
            lbRt.anchorMin = new Vector2(0f, 1f); lbRt.anchorMax = new Vector2(1f, 1f);
            lbRt.offsetMin = new Vector2(28f, -142f); lbRt.offsetMax = new Vector2(-28f, -104f);

            TMP_Text lanBannerText = CreateTmp("LanLockNoticeText", lanBanner.transform, font, "Connected to class", 16, FontStyles.Bold, RetroUITheme.WarningAmber, TextAlignmentOptions.Center);
            StretchFull(lanBannerText.GetComponent<RectTransform>(), 16f, 2f);
            lanBanner.SetActive(false);

            // Two Columns Content Area
            // Left Column: Learning Path + Assessment + Reset Progress
            GameObject leftCard = CreateRetroCardWindow("LeftColumn_LearningPath", win.transform,
                Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero,
                assets.cardRounded, RetroUITheme.WindowSkySoft);
            RectTransform lcRt = leftCard.GetComponent<RectTransform>();
            lcRt.anchorMin = new Vector2(0f, 0f); lcRt.anchorMax = new Vector2(0.49f, 1f);
            lcRt.offsetMin = new Vector2(28f, 96f); lcRt.offsetMax = new Vector2(-10f, -150f);

            TMP_Text pathSectionTitle = CreateTmp("PathSectionTitle", leftCard.transform, font, "Learning Path & Start Flow", 20, FontStyles.Bold, RetroUITheme.InkDark, TextAlignmentOptions.Left);
            RectTransform pstRt = pathSectionTitle.GetComponent<RectTransform>();
            pstRt.anchorMin = new Vector2(0f, 1f); pstRt.anchorMax = new Vector2(1f, 1f);
            pstRt.offsetMin = new Vector2(20f, -48f); pstRt.offsetMax = new Vector2(-20f, -14f);

            Button[] pathBtns = new Button[3];
            Image[] pathBgs = new Image[3];
            TMP_Text[] pathLbls = new TMP_Text[3];
            string[] defaultNames = { "Beginner", "Intermediate", "Advanced" };
            for (int i = 0; i < 3; i++)
            {
                pathBtns[i] = CreateRetroButton($"Btn-Path{i}", leftCard.transform, assets, font, defaultNames[i], null, RetroUITheme.CardWhite, RetroUITheme.InkDark, 18, out pathLbls[i]);
                pathBgs[i] = pathBtns[i].GetComponent<Image>();
                RectTransform pRt = pathBtns[i].GetComponent<RectTransform>();
                pRt.anchorMin = new Vector2(0f, 1f); pRt.anchorMax = new Vector2(1f, 1f);
                pRt.pivot = new Vector2(0.5f, 1f);
                pRt.offsetMin = new Vector2(20f, -118f - i * 64f);
                pRt.offsetMax = new Vector2(-20f, -62f - i * 64f);
            }

            Button assessBtn = CreateRetroButton("Btn-ToggleAssessment", leftCard.transform, assets, font, "Initial Assessment: ON", assets.iconCheck, RetroUITheme.BtnMintGreen, RetroUITheme.InkWhite, 18, out TMP_Text assessLbl);
            RectTransform aRt = assessBtn.GetComponent<RectTransform>();
            aRt.anchorMin = new Vector2(0f, 1f); aRt.anchorMax = new Vector2(1f, 1f);
            aRt.pivot = new Vector2(0.5f, 1f);
            aRt.offsetMin = new Vector2(20f, -326f); aRt.offsetMax = new Vector2(-20f, -264f);

            TMP_Text pathSummary = CreateTmp("SummaryFooterText", leftCard.transform, font, "", 15, FontStyles.Normal, RetroUITheme.InkMuted, TextAlignmentOptions.Left);
            RectTransform psRt = pathSummary.GetComponent<RectTransform>();
            psRt.anchorMin = new Vector2(0f, 1f); psRt.anchorMax = new Vector2(1f, 1f);
            psRt.offsetMin = new Vector2(20f, -390f); psRt.offsetMax = new Vector2(-20f, -336f);

            TMP_Text pathLanHint = CreateTmp("LanLockedHintText", leftCard.transform, font, "", 14, FontStyles.Bold, RetroUITheme.WarningAmber, TextAlignmentOptions.Left);
            RectTransform plhRt = pathLanHint.GetComponent<RectTransform>();
            plhRt.anchorMin = new Vector2(0f, 1f); plhRt.anchorMax = new Vector2(1f, 1f);
            plhRt.offsetMin = new Vector2(20f, -430f); plhRt.offsetMax = new Vector2(-20f, -392f);

            Button resetProgressBtn = CreateRetroButton("Btn-ResetProgress", leftCard.transform, assets, font, "Reset Progress", null, RetroUITheme.BtnCoralRed, RetroUITheme.InkWhite, 18, out TMP_Text resetProgressLbl);
            RectTransform rpRt = resetProgressBtn.GetComponent<RectTransform>();
            rpRt.anchorMin = new Vector2(0f, 0f); rpRt.anchorMax = new Vector2(1f, 0f);
            rpRt.pivot = new Vector2(0.5f, 0f);
            rpRt.offsetMin = new Vector2(20f, 18f); rpRt.offsetMax = new Vector2(-20f, 74f);

            // Right Column: Playtest Export + Classroom Dashboard + PIN Setup
            GameObject rightCard = CreateRetroCardWindow("RightColumn_ClassroomAndTools", win.transform,
                Vector2.zero, Vector2.one, new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero,
                assets.cardRounded, RetroUITheme.WindowSkySoft);
            RectTransform rcRt = rightCard.GetComponent<RectTransform>();
            rcRt.anchorMin = new Vector2(0.51f, 0f); rcRt.anchorMax = new Vector2(1f, 1f);
            rcRt.offsetMin = new Vector2(10f, 96f); rcRt.offsetMax = new Vector2(-28f, -150f);

            TMP_Text secStudentLbl = CreateTmp("SectionStudentToolsLabel", rightCard.transform, font, "Student & Progress Tools", 20, FontStyles.Bold, RetroUITheme.InkDark, TextAlignmentOptions.Left);
            RectTransform sstRt = secStudentLbl.GetComponent<RectTransform>();
            sstRt.anchorMin = new Vector2(0f, 1f); sstRt.anchorMax = new Vector2(1f, 1f);
            sstRt.offsetMin = new Vector2(20f, -48f); sstRt.offsetMax = new Vector2(-20f, -14f);

            TMP_Text tIdLabel = CreateTmp("TeacherStudentIdLabel", rightCard.transform, font, "Student ID:", 16, FontStyles.Bold, RetroUITheme.InkMuted, TextAlignmentOptions.Left);
            RectTransform tilRt = tIdLabel.GetComponent<RectTransform>();
            tilRt.anchorMin = new Vector2(0f, 1f); tilRt.anchorMax = new Vector2(0.5f, 1f);
            tilRt.offsetMin = new Vector2(20f, -84f); tilRt.offsetMax = Vector2.zero;

            TMP_Text tIdVal = CreateTmp("TeacherStudentIdValue", rightCard.transform, font, "S01", 20, FontStyles.Bold, RetroUITheme.TitleBlue, TextAlignmentOptions.Right);
            RectTransform tivRt = tIdVal.GetComponent<RectTransform>();
            tivRt.anchorMin = new Vector2(0.5f, 1f); tivRt.anchorMax = new Vector2(1f, 1f);
            tivRt.offsetMin = Vector2.zero; tivRt.offsetMax = new Vector2(-22f, -52f);

            Button exportBtn = CreateRetroButton("Btn-ExportPlaytest", rightCard.transform, assets, font, "Export Data", null, RetroUITheme.BtnSkyBlue, RetroUITheme.InkWhite, 17, out TMP_Text exportLbl);
            RectTransform ebRt = exportBtn.GetComponent<RectTransform>();
            ebRt.anchorMin = new Vector2(0f, 1f); ebRt.anchorMax = new Vector2(0.49f, 1f);
            ebRt.offsetMin = new Vector2(20f, -148f); ebRt.offsetMax = new Vector2(-6f, -94f);

            Button autoDlBtn = CreateRetroButton("Btn-AutoDownload", rightCard.transform, assets, font, "Auto Download: ON", null, RetroUITheme.BtnMintGreen, RetroUITheme.InkWhite, 16, out TMP_Text autoDlLbl);
            RectTransform adbRt = autoDlBtn.GetComponent<RectTransform>();
            adbRt.anchorMin = new Vector2(0.51f, 1f); adbRt.anchorMax = new Vector2(1f, 1f);
            adbRt.offsetMin = new Vector2(6f, -148f); adbRt.offsetMax = new Vector2(-20f, -94f);

            // Classroom Section
            TMP_Text secClassLbl = CreateTmp("SectionClassLabel", rightCard.transform, font, "Classroom & Dashboard", 20, FontStyles.Bold, RetroUITheme.InkDark, TextAlignmentOptions.Left);
            RectTransform sclRt = secClassLbl.GetComponent<RectTransform>();
            sclRt.anchorMin = new Vector2(0f, 1f); sclRt.anchorMax = new Vector2(1f, 1f);
            sclRt.offsetMin = new Vector2(20f, -210f); sclRt.offsetMax = new Vector2(-20f, -174f);

            Button openDashBtn = CreateRetroButton("Btn-OpenTeacherDashboard", rightCard.transform, assets, font, "Open Teacher Dashboard", assets.iconClassroom, RetroUITheme.BtnPlayfulPurple, RetroUITheme.InkWhite, 19, out TMP_Text openDashLbl);
            RectTransform odRt = openDashBtn.GetComponent<RectTransform>();
            odRt.anchorMin = new Vector2(0f, 1f); odRt.anchorMax = new Vector2(1f, 1f);
            odRt.offsetMin = new Vector2(20f, -278f); odRt.offsetMax = new Vector2(-20f, -218f);

            TMP_Text hostStatusTmp = CreateTmp("TeacherHostStatusText", rightCard.transform, font, "Class server: Offline", 15, FontStyles.Bold, RetroUITheme.InkMuted, TextAlignmentOptions.Left);
            RectTransform hsRt = hostStatusTmp.GetComponent<RectTransform>();
            hsRt.anchorMin = new Vector2(0f, 1f); hsRt.anchorMax = new Vector2(1f, 1f);
            hsRt.offsetMin = new Vector2(20f, -314f); hsRt.offsetMax = new Vector2(-20f, -284f);

            // PIN Section
            TMP_Text secPinLbl = CreateTmp("SectionPinLabel", rightCard.transform, font, "Teacher PIN", 20, FontStyles.Bold, RetroUITheme.InkDark, TextAlignmentOptions.Left);
            RectTransform splRt = secPinLbl.GetComponent<RectTransform>();
            splRt.anchorMin = new Vector2(0f, 1f); splRt.anchorMax = new Vector2(1f, 1f);
            splRt.offsetMin = new Vector2(20f, -370f); splRt.offsetMax = new Vector2(-20f, -334f);

            TMP_InputField newPinInput = CreateRetroInputField("Input-NewPin", rightCard.transform, assets, font, "New 4 digits...", 4, TMP_InputField.ContentType.IntegerNumber, out TMP_Text newPinPh);
            RectTransform npiRt = newPinInput.GetComponent<RectTransform>();
            npiRt.anchorMin = new Vector2(0f, 1f); npiRt.anchorMax = new Vector2(0.60f, 1f);
            npiRt.offsetMin = new Vector2(20f, -432f); npiRt.offsetMax = new Vector2(-8f, -378f);

            Button savePinBtn = CreateRetroButton("Btn-SavePin", rightCard.transform, assets, font, "Save PIN", assets.iconCheck, RetroUITheme.BtnSunnyGold, RetroUITheme.InkDark, 17, out TMP_Text savePinLbl);
            RectTransform spbRt = savePinBtn.GetComponent<RectTransform>();
            spbRt.anchorMin = new Vector2(0.62f, 1f); spbRt.anchorMax = new Vector2(1f, 1f);
            spbRt.offsetMin = new Vector2(4f, -432f); spbRt.offsetMax = new Vector2(-20f, -378f);

            TMP_Text pinChangeFb = CreateTmp("PinChangeFeedbackText", rightCard.transform, font, "", 15, FontStyles.Bold, RetroUITheme.SuccessMint, TextAlignmentOptions.Left);
            RectTransform pcfbRt = pinChangeFb.GetComponent<RectTransform>();
            pcfbRt.anchorMin = new Vector2(0f, 1f); pcfbRt.anchorMax = new Vector2(1f, 1f);
            pcfbRt.offsetMin = new Vector2(20f, -468f); pcfbRt.offsetMax = new Vector2(-20f, -438f);

            // Bottom Footer Lock Teacher Button
            Button lockBtn = CreateRetroButton("Btn-LockTeacherMode", win.transform, assets, font, "Lock & Student Mode", assets.iconLock, RetroUITheme.BtnWarmOrange, RetroUITheme.InkWhite, 20, out TMP_Text lockLbl);
            RectTransform lkRt = lockBtn.GetComponent<RectTransform>();
            lkRt.anchorMin = new Vector2(0.5f, 0f); lkRt.anchorMax = new Vector2(0.5f, 0f);
            lkRt.pivot = new Vector2(0.5f, 0f);
            lkRt.anchoredPosition = new Vector2(0f, 22f);
            lkRt.sizeDelta = new Vector2(390f, 58f);

            // Wire serialized fields on Gate, Path, Playtest, Classroom widgets
            SetPrivateField(gate, "teacherPanelRoot", overlay);
            SetPrivateField(gate, "teacherPanelTitleText", panelTitle);
            SetPrivateField(gate, "teacherPanelSubtitleText", subtitleTmp);
            SetPrivateField(gate, "teacherPanelCloseButton", closeBtn);
            SetPrivateField(gate, "lockTeacherModeButton", lockBtn);
            SetPrivateField(gate, "lockTeacherModeLabel", lockLbl);
            SetPrivateField(gate, "resetProgressButton", resetProgressBtn);
            SetPrivateField(gate, "resetProgressLabel", resetProgressLbl);
            SetPrivateField(gate, "lanLockNoticeBanner", lanBanner);
            SetPrivateField(gate, "lanLockNoticeText", lanBannerText);
            SetPrivateField(gate, "sectionStudentControlsLabel", secStudentLbl);
            SetPrivateField(gate, "sectionClassNetworkLabel", secClassLbl);
            SetPrivateField(gate, "sectionPinConfigLabel", secPinLbl);
            SetPrivateField(gate, "newPinInputField", newPinInput);
            SetPrivateField(gate, "newPinPlaceholderText", newPinPh);
            SetPrivateField(gate, "savePinButton", savePinBtn);
            SetPrivateField(gate, "savePinLabel", savePinLbl);
            SetPrivateField(gate, "pinChangeFeedbackText", pinChangeFb);

            SetPrivateField(pathWidget, "sectionTitleText", pathSectionTitle);
            SetPrivateField(pathWidget, "pathButtons", pathBtns);
            SetPrivateField(pathWidget, "pathButtonBackgrounds", pathBgs);
            SetPrivateField(pathWidget, "pathButtonTitles", pathLbls);
            SetPrivateField(pathWidget, "assessmentToggleButton", assessBtn);
            SetPrivateField(pathWidget, "assessmentToggleBg", assessBtn.GetComponent<Image>());
            SetPrivateField(pathWidget, "assessmentToggleLabel", assessLbl);
            SetPrivateField(pathWidget, "summaryFooterText", pathSummary);
            SetPrivateField(pathWidget, "lanLockedNoticeText", pathLanHint);

            SetPrivateField(playtestWidget, "teacherStudentIdLabelText", tIdLabel);
            SetPrivateField(playtestWidget, "teacherStudentIdValueText", tIdVal);
            SetPrivateField(playtestWidget, "exportButton", exportBtn);
            SetPrivateField(playtestWidget, "exportButtonLabel", exportLbl);
            SetPrivateField(playtestWidget, "autoDownloadButton", autoDlBtn);
            SetPrivateField(playtestWidget, "autoDownloadButtonLabel", autoDlLbl);

            SetPrivateField(classroomWidget, "openTeacherDashboardButton", openDashBtn);
            SetPrivateField(classroomWidget, "openTeacherDashboardLabel", openDashLbl);
            SetPrivateField(classroomWidget, "teacherHostStatusText", hostStatusTmp);

            return overlay;
        }

        private static GameObject BuildTeacherDashboardModal(Transform canvasTr, RetroThemeAssets assets, TMP_FontAsset font, out TeacherDashboardController ctrl)
        {
            GameObject overlay = CreateImageObject("TeacherDashboardModal", canvasTr, null, RetroUITheme.OverlayScrim, true);
            StretchFull(overlay.GetComponent<RectTransform>());
            ctrl = overlay.AddComponent<TeacherDashboardController>();

            GameObject win = CreateRetroDesktopWindow("DashboardWindow", overlay.transform, assets, font,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1740f, 940f),
                RetroUITheme.TitleBlue, RetroUITheme.WindowCream, assets.iconClassroom,
                "Classroom Dashboard", false, out TMP_Text titleTmp, out Transform titleBarTr);

            TMP_Text serverStatusTmp = CreateTmp("ServerStatusText", titleBarTr, font, "Server: Offline", 16, FontStyles.Bold, RetroUITheme.CardPastelYellow, TextAlignmentOptions.Right);
            RectTransform ssRt = serverStatusTmp.GetComponent<RectTransform>();
            ssRt.anchorMin = new Vector2(0.30f, 0f); ssRt.anchorMax = new Vector2(1f, 1f);
            ssRt.offsetMin = Vector2.zero; ssRt.offsetMax = new Vector2(-460f, 0f);

            Button clearOfflineBtn = CreateRetroButton("Btn-ClearOffline", titleBarTr, assets, font, "Clear Offline", null, RetroUITheme.BtnNeutralCream, RetroUITheme.InkDark, 14, out TMP_Text clearOfflineLbl);
            RectTransform coRt = clearOfflineBtn.GetComponent<RectTransform>();
            coRt.anchorMin = new Vector2(1f, 0.5f); coRt.anchorMax = new Vector2(1f, 0.5f);
            coRt.pivot = new Vector2(1f, 0.5f);
            coRt.anchoredPosition = new Vector2(-276f, 0f);
            coRt.sizeDelta = new Vector2(170f, 38f);

            Button toggleServerBtn = CreateRetroButton("Btn-ToggleServer", titleBarTr, assets, font, "Start Class Server", null, RetroUITheme.BtnMintGreen, RetroUITheme.InkWhite, 14, out TMP_Text toggleServerLbl);
            RectTransform tsRt = toggleServerBtn.GetComponent<RectTransform>();
            tsRt.anchorMin = new Vector2(1f, 0.5f); tsRt.anchorMax = new Vector2(1f, 0.5f);
            tsRt.pivot = new Vector2(1f, 0.5f);
            tsRt.anchoredPosition = new Vector2(-68f, 0f);
            tsRt.sizeDelta = new Vector2(196f, 38f);

            Button closeBtn = CreateCloseIconButton("Btn-CloseDashboard", titleBarTr, assets);

            // Batch Bar
            GameObject batchBar = CreateRetroCardWindow("BatchControlsBar", win.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, Vector2.zero, assets.cardRounded, RetroUITheme.CardPastelBlue);
            RectTransform bbRt = batchBar.GetComponent<RectTransform>();
            bbRt.offsetMin = new Vector2(22f, -138f); bbRt.offsetMax = new Vector2(-22f, -72f);

            TMP_Text batchLbl = CreateTmp("BatchLabel", batchBar.transform, font, "Class Default:", 16, FontStyles.Bold, RetroUITheme.InkDark, TextAlignmentOptions.MidlineLeft);
            RectTransform blRt = batchLbl.GetComponent<RectTransform>();
            blRt.anchorMin = new Vector2(0f, 0f); blRt.anchorMax = new Vector2(0f, 1f);
            blRt.offsetMin = new Vector2(18f, 0f); blRt.offsetMax = new Vector2(210f, 0f);

            Button[] batchPathBtns = new Button[3];
            Image[] batchPathBgs = new Image[3];
            TMP_Text[] batchPathLbls = new TMP_Text[3];
            string[] pathTitles = { "Beginner", "Intermediate", "Advanced" };
            for (int i = 0; i < 3; i++)
            {
                batchPathBtns[i] = CreateRetroButton($"Btn-BatchPath{i}", batchBar.transform, assets, font, pathTitles[i], null, RetroUITheme.BtnNeutralCream, RetroUITheme.InkDark, 15, out batchPathLbls[i]);
                batchPathBgs[i] = batchPathBtns[i].GetComponent<Image>();
                RectTransform bpRt = batchPathBtns[i].GetComponent<RectTransform>();
                bpRt.anchorMin = new Vector2(0f, 0.5f); bpRt.anchorMax = new Vector2(0f, 0.5f);
                bpRt.pivot = new Vector2(0f, 0.5f);
                bpRt.anchoredPosition = new Vector2(220f + i * 176f, 0f);
                bpRt.sizeDelta = new Vector2(166f, 44f);
            }

            Button batchAssessBtn = CreateRetroButton("Btn-BatchAssess", batchBar.transform, assets, font, "Assess: ON", null, RetroUITheme.BtnMintGreen, RetroUITheme.InkWhite, 15, out TMP_Text batchAssessLbl);
            RectTransform baRt = batchAssessBtn.GetComponent<RectTransform>();
            baRt.anchorMin = new Vector2(0f, 0.5f); baRt.anchorMax = new Vector2(0f, 0.5f);
            baRt.pivot = new Vector2(0f, 0.5f);
            baRt.anchoredPosition = new Vector2(760f, 0f);
            baRt.sizeDelta = new Vector2(170f, 44f);

            Button applyAllBtn = CreateRetroButton("Btn-ApplyAll", batchBar.transform, assets, font, "Apply to All", null, RetroUITheme.BtnSkyBlue, RetroUITheme.InkWhite, 16, out TMP_Text applyAllLbl);
            RectTransform aaRt = applyAllBtn.GetComponent<RectTransform>();
            aaRt.anchorMin = new Vector2(1f, 0.5f); aaRt.anchorMax = new Vector2(1f, 0.5f);
            aaRt.pivot = new Vector2(1f, 0.5f);
            aaRt.anchoredPosition = new Vector2(-214f, 0f);
            aaRt.sizeDelta = new Vector2(180f, 46f);

            Button launchAllBtn = CreateRetroButton("Btn-LaunchAll", batchBar.transform, assets, font, "Launch All", assets.iconPlay, RetroUITheme.BtnSunnyGold, RetroUITheme.InkDark, 16, out TMP_Text launchAllLbl);
            RectTransform laRt = launchAllBtn.GetComponent<RectTransform>();
            laRt.anchorMin = new Vector2(1f, 0.5f); laRt.anchorMax = new Vector2(1f, 0.5f);
            laRt.pivot = new Vector2(1f, 0.5f);
            laRt.anchoredPosition = new Vector2(-18f, 0f);
            laRt.sizeDelta = new Vector2(184f, 46f);

            // Left Students Card
            GameObject leftPanel = CreateRetroCardWindow("StudentsListCard", win.transform,
                Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero,
                assets.cardRounded, RetroUITheme.WindowSkySoft);
            RectTransform lpRt = leftPanel.GetComponent<RectTransform>();
            lpRt.anchorMin = new Vector2(0f, 0f); lpRt.anchorMax = new Vector2(0.57f, 1f);
            lpRt.offsetMin = new Vector2(22f, 22f); lpRt.offsetMax = new Vector2(-8f, -152f);

            TMP_Text studentsHdr = CreateTmp("StudentsHeaderText", leftPanel.transform, font, "Connected Students", 20, FontStyles.Bold, RetroUITheme.InkDark, TextAlignmentOptions.Left);
            RectTransform shRt = studentsHdr.GetComponent<RectTransform>();
            shRt.anchorMin = new Vector2(0f, 1f); shRt.anchorMax = new Vector2(1f, 1f);
            shRt.offsetMin = new Vector2(18f, -46f); shRt.offsetMax = new Vector2(-18f, -12f);

            GameObject studentsScroll = CreateRetroScrollList("StudentsScroll", leftPanel.transform, assets, out Transform studentsContent);
            RectTransform scRt = studentsScroll.GetComponent<RectTransform>();
            scRt.anchorMin = Vector2.zero; scRt.anchorMax = Vector2.one;
            scRt.offsetMin = new Vector2(14f, 14f); scRt.offsetMax = new Vector2(-14f, -52f);

            TMP_Text emptyStudentsTmp = CreateTmp("EmptyStudentsText", studentsScroll.transform, font, "Waiting for students...", 18, FontStyles.Normal, RetroUITheme.InkMuted, TextAlignmentOptions.Center);
            StretchFull(emptyStudentsTmp.GetComponent<RectTransform>(), 32f, 32f);

            GameObject studentRowTemplate = BuildStudentRowTemplate(studentsContent, assets, font);
            studentRowTemplate.SetActive(false);

            // Right Inspector Card
            GameObject rightPanel = CreateRetroCardWindow("InspectorCard", win.transform,
                Vector2.zero, Vector2.one, new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero,
                assets.cardRounded, RetroUITheme.WindowSkySoft);
            RectTransform rpRt = rightPanel.GetComponent<RectTransform>();
            rpRt.anchorMin = new Vector2(0.58f, 0f); rpRt.anchorMax = new Vector2(1f, 1f);
            rpRt.offsetMin = new Vector2(8f, 22f); rpRt.offsetMax = new Vector2(-22f, -152f);

            TMP_Text inspPlaceholder = CreateTmp("InspectorPlaceholderText", rightPanel.transform, font, "Select a student...", 19, FontStyles.Normal, RetroUITheme.InkMuted, TextAlignmentOptions.Center);
            StretchFull(inspPlaceholder.GetComponent<RectTransform>(), 32f, 32f);

            GameObject inspContent = new GameObject("InspectorContent", typeof(RectTransform));
            inspContent.transform.SetParent(rightPanel.transform, false);
            StretchFull(inspContent.GetComponent<RectTransform>(), 16f, 16f);

            GameObject avatarBadge = CreateImageObject("AvatarBadge", inspContent.transform, assets.badgePill, RetroUITheme.TitleBlue, false);
            avatarBadge.GetComponent<Image>().type = Image.Type.Sliced;
            RectTransform avRt = avatarBadge.GetComponent<RectTransform>();
            avRt.anchorMin = new Vector2(0f, 1f); avRt.anchorMax = new Vector2(0f, 1f);
            avRt.pivot = new Vector2(0f, 1f);
            avRt.anchoredPosition = new Vector2(4f, -4f);
            avRt.sizeDelta = new Vector2(68f, 68f);

            TMP_Text avatarTmp = CreateTmp("AvatarText", avatarBadge.transform, font, "S01", 22, FontStyles.Bold, RetroUITheme.InkWhite, TextAlignmentOptions.Center);
            StretchFull(avatarTmp.GetComponent<RectTransform>());

            TMP_Text inspTitle = CreateTmp("InspectorTitle", inspContent.transform, font, "S01", 22, FontStyles.Bold, RetroUITheme.InkDark, TextAlignmentOptions.Left);
            RectTransform itRt = inspTitle.GetComponent<RectTransform>();
            itRt.anchorMin = new Vector2(0f, 1f); itRt.anchorMax = new Vector2(1f, 1f);
            itRt.offsetMin = new Vector2(86f, -38f); itRt.offsetMax = new Vector2(-8f, -4f);

            TMP_Text inspSub = CreateTmp("InspectorSubtitle", inspContent.transform, font, "", 14, FontStyles.Normal, RetroUITheme.InkMuted, TextAlignmentOptions.Left);
            RectTransform isubRt = inspSub.GetComponent<RectTransform>();
            isubRt.anchorMin = new Vector2(0f, 1f); isubRt.anchorMax = new Vector2(1f, 1f);
            isubRt.offsetMin = new Vector2(86f, -70f); isubRt.offsetMax = new Vector2(-8f, -38f);

            // 3 KPI Cards
            GameObject kpiRow = new GameObject("KpiRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            kpiRow.transform.SetParent(inspContent.transform, false);
            RectTransform krRt = kpiRow.GetComponent<RectTransform>();
            krRt.anchorMin = new Vector2(0f, 1f); krRt.anchorMax = new Vector2(1f, 1f);
            krRt.offsetMin = new Vector2(0f, -166f); krRt.offsetMax = new Vector2(0f, -84f);
            HorizontalLayoutGroup krHlg = kpiRow.GetComponent<HorizontalLayoutGroup>();
            krHlg.spacing = 10f; krHlg.childControlWidth = true; krHlg.childControlHeight = true;
            krHlg.childForceExpandWidth = true; krHlg.childForceExpandHeight = true;

            TMP_Text kpiProgVal = CreateKpiBox(kpiRow.transform, assets, font, RetroUITheme.CardPastelBlue, out TMP_Text kpiProgLbl);
            TMP_Text kpiMedVal = CreateKpiBox(kpiRow.transform, assets, font, RetroUITheme.CardPastelYellow, out TMP_Text kpiMedLbl);
            TMP_Text kpiAccVal = CreateKpiBox(kpiRow.transform, assets, font, RetroUITheme.CardPastelMint, out TMP_Text kpiAccLbl);

            // 9 Skill Bars inside Inspector
            GameObject skillBarsGroup = new GameObject("SkillBarsGroup", typeof(RectTransform), typeof(VerticalLayoutGroup));
            skillBarsGroup.transform.SetParent(inspContent.transform, false);
            RectTransform sbgRt = skillBarsGroup.GetComponent<RectTransform>();
            sbgRt.anchorMin = Vector2.zero; sbgRt.anchorMax = Vector2.one;
            sbgRt.offsetMin = new Vector2(0f, 4f); sbgRt.offsetMax = new Vector2(0f, -180f);
            VerticalLayoutGroup sbgVlg = skillBarsGroup.GetComponent<VerticalLayoutGroup>();
            sbgVlg.spacing = 8f; sbgVlg.childControlWidth = true; sbgVlg.childControlHeight = true;
            sbgVlg.childForceExpandWidth = true; sbgVlg.childForceExpandHeight = true;

            string[] skillIds = { "mouse_move", "hover", "click", "double_click", "right_click", "click_hold", "drag_drop", "typing", "shortcuts" };
            TeacherDashboardController.SkillBarSlot[] slots = new TeacherDashboardController.SkillBarSlot[skillIds.Length];
            for (int i = 0; i < skillIds.Length; i++)
            {
                slots[i] = BuildSkillBarSlot(skillBarsGroup.transform, assets, font, skillIds[i]);
            }

            SetPrivateField(ctrl, "titleText", titleTmp);
            SetPrivateField(ctrl, "serverStatusText", serverStatusTmp);
            SetPrivateField(ctrl, "clearOfflineButton", clearOfflineBtn);
            SetPrivateField(ctrl, "clearOfflineLabel", clearOfflineLbl);
            SetPrivateField(ctrl, "toggleServerButton", toggleServerBtn);
            SetPrivateField(ctrl, "toggleServerLabel", toggleServerLbl);
            SetPrivateField(ctrl, "closeButton", closeBtn);
            SetPrivateField(ctrl, "batchLabelText", batchLbl);
            SetPrivateField(ctrl, "batchPathButtons", batchPathBtns);
            SetPrivateField(ctrl, "batchPathBgs", batchPathBgs);
            SetPrivateField(ctrl, "batchPathLabels", batchPathLbls);
            SetPrivateField(ctrl, "batchAssessButton", batchAssessBtn);
            SetPrivateField(ctrl, "batchAssessBg", batchAssessBtn.GetComponent<Image>());
            SetPrivateField(ctrl, "batchAssessLabel", batchAssessLbl);
            SetPrivateField(ctrl, "applyAllButton", applyAllBtn);
            SetPrivateField(ctrl, "applyAllLabel", applyAllLbl);
            SetPrivateField(ctrl, "launchAllButton", launchAllBtn);
            SetPrivateField(ctrl, "launchAllLabel", launchAllLbl);
            SetPrivateField(ctrl, "studentsHeaderText", studentsHdr);
            SetPrivateField(ctrl, "studentsListContainer", studentsContent);
            SetPrivateField(ctrl, "studentRowTemplate", studentRowTemplate);
            SetPrivateField(ctrl, "emptyStudentsText", emptyStudentsTmp);
            SetPrivateField(ctrl, "inspectorPlaceholderText", inspPlaceholder);
            SetPrivateField(ctrl, "inspectorContentRoot", inspContent);
            SetPrivateField(ctrl, "inspectorAvatarText", avatarTmp);
            SetPrivateField(ctrl, "inspectorTitleText", inspTitle);
            SetPrivateField(ctrl, "inspectorSubtitleText", inspSub);
            SetPrivateField(ctrl, "inspectorKpiProgressValue", kpiProgVal);
            SetPrivateField(ctrl, "inspectorKpiProgressLabel", kpiProgLbl);
            SetPrivateField(ctrl, "inspectorKpiMedalsValue", kpiMedVal);
            SetPrivateField(ctrl, "inspectorKpiMedalsLabel", kpiMedLbl);
            SetPrivateField(ctrl, "inspectorKpiScoreValue", kpiAccVal);
            SetPrivateField(ctrl, "inspectorKpiScoreLabel", kpiAccLbl);
            SetPrivateField(ctrl, "skillBarSlots", slots);

            return overlay;
        }

        private static GameObject BuildStudentRowTemplate(Transform parent, RetroThemeAssets assets, TMP_FontAsset font)
        {
            GameObject row = CreateImageObject("StudentRowTemplate", parent, assets.cardRounded, RetroUITheme.CardWhite, false);
            row.GetComponent<Image>().type = Image.Type.Sliced;
            LayoutElement le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 112f; le.minHeight = 112f;

            // Clickable selection overlay behind buttons
            GameObject selArea = new GameObject("SelectArea", typeof(RectTransform), typeof(Image), typeof(Button));
            selArea.transform.SetParent(row.transform, false);
            StretchFull(selArea.GetComponent<RectTransform>());
            selArea.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);

            GameObject dot = CreateImageObject("StatusDot", row.transform, assets.badgePill, RetroUITheme.SuccessMint, false);
            RectTransform dRt = dot.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0f, 1f); dRt.anchorMax = new Vector2(0f, 1f);
            dRt.pivot = new Vector2(0f, 1f);
            dRt.anchoredPosition = new Vector2(14f, -16f);
            dRt.sizeDelta = new Vector2(16f, 16f);

            TMP_Text idTmp = CreateTmp("StudentIdText", row.transform, font, "S01", 20, FontStyles.Bold, RetroUITheme.InkDark, TextAlignmentOptions.Left);
            RectTransform idRt = idTmp.GetComponent<RectTransform>();
            idRt.anchorMin = new Vector2(0f, 1f); idRt.anchorMax = new Vector2(0f, 1f);
            idRt.pivot = new Vector2(0f, 1f);
            idRt.anchoredPosition = new Vector2(38f, -10f);
            idRt.sizeDelta = new Vector2(96f, 30f);

            Button minusBtn = CreateRetroButton("Btn-IdMinus", row.transform, assets, font, "-", null, RetroUITheme.BtnSoftSlate, RetroUITheme.InkDark, 16, out _);
            RectTransform mRt = minusBtn.GetComponent<RectTransform>();
            mRt.anchorMin = new Vector2(0f, 1f); mRt.anchorMax = new Vector2(0f, 1f);
            mRt.pivot = new Vector2(0f, 1f);
            mRt.anchoredPosition = new Vector2(138f, -10f);
            mRt.sizeDelta = new Vector2(32f, 30f);

            Button plusBtn = CreateRetroButton("Btn-IdPlus", row.transform, assets, font, "+", null, RetroUITheme.BtnSoftSlate, RetroUITheme.InkDark, 16, out _);
            RectTransform pRt = plusBtn.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0f, 1f); pRt.anchorMax = new Vector2(0f, 1f);
            pRt.pivot = new Vector2(0f, 1f);
            pRt.anchoredPosition = new Vector2(174f, -10f);
            pRt.sizeDelta = new Vector2(32f, 30f);

            TMP_Text metaTmp = CreateTmp("MetaText", row.transform, font, "Online • PC • Menu (0/16)", 14, FontStyles.Normal, RetroUITheme.InkMuted, TextAlignmentOptions.Left);
            RectTransform mtRt = metaTmp.GetComponent<RectTransform>();
            mtRt.anchorMin = new Vector2(0f, 1f); mtRt.anchorMax = new Vector2(1f, 1f);
            mtRt.offsetMin = new Vector2(218f, -40f); mtRt.offsetMax = new Vector2(-14f, -10f);

            string[] pNames = { "Beg", "Int", "Adv" };
            for (int p = 0; p < 3; p++)
            {
                Button pBtn = CreateRetroButton($"Btn-Path{p}", row.transform, assets, font, pNames[p], null, RetroUITheme.BtnNeutralCream, RetroUITheme.InkDark, 14, out _);
                RectTransform pbRt = pBtn.GetComponent<RectTransform>();
                pbRt.anchorMin = new Vector2(0f, 0f); pbRt.anchorMax = new Vector2(0f, 0f);
                pbRt.pivot = new Vector2(0f, 0f);
                pbRt.anchoredPosition = new Vector2(14f + p * 124f, 12f);
                pbRt.sizeDelta = new Vector2(116f, 40f);
            }

            Button assessBtn = CreateRetroButton("Btn-ToggleAssess", row.transform, assets, font, "Assess: ON", null, RetroUITheme.BtnMintGreen, RetroUITheme.InkWhite, 13, out _);
            RectTransform abRt = assessBtn.GetComponent<RectTransform>();
            abRt.anchorMin = new Vector2(0f, 0f); abRt.anchorMax = new Vector2(0f, 0f);
            abRt.pivot = new Vector2(0f, 0f);
            abRt.anchoredPosition = new Vector2(392f, 12f);
            abRt.sizeDelta = new Vector2(138f, 40f);

            Button startBtn = CreateRetroButton("Btn-StartStudent", row.transform, assets, font, "Start", null, RetroUITheme.BtnSunnyGold, RetroUITheme.InkDark, 15, out _);
            RectTransform stBtnRt = startBtn.GetComponent<RectTransform>();
            stBtnRt.anchorMin = new Vector2(1f, 0f); stBtnRt.anchorMax = new Vector2(1f, 0f);
            stBtnRt.pivot = new Vector2(1f, 0f);
            stBtnRt.anchoredPosition = new Vector2(-118f, 12f);
            stBtnRt.sizeDelta = new Vector2(100f, 40f);

            Button resetBtn = CreateRetroButton("Btn-ResetStudent", row.transform, assets, font, "Reset", null, RetroUITheme.BtnCoralRed, RetroUITheme.InkWhite, 15, out _);
            RectTransform rBtnRt = resetBtn.GetComponent<RectTransform>();
            rBtnRt.anchorMin = new Vector2(1f, 0f); rBtnRt.anchorMax = new Vector2(1f, 0f);
            rBtnRt.pivot = new Vector2(1f, 0f);
            rBtnRt.anchoredPosition = new Vector2(-14f, 12f);
            rBtnRt.sizeDelta = new Vector2(94f, 40f);

            return row;
        }

        private static TeacherDashboardController.SkillBarSlot BuildSkillBarSlot(Transform parent, RetroThemeAssets assets, TMP_FontAsset font, string skillId)
        {
            GameObject row = CreateImageObject($"SkillBar_{skillId}", parent, assets.cardRounded, RetroUITheme.CardWhite, false);
            row.GetComponent<Image>().type = Image.Type.Sliced;

            TMP_Text nameTmp = CreateTmp("NameText", row.transform, font, skillId, 14, FontStyles.Bold, RetroUITheme.InkDark, TextAlignmentOptions.Left);
            RectTransform nRt = nameTmp.GetComponent<RectTransform>();
            nRt.anchorMin = new Vector2(0f, 0.5f); nRt.anchorMax = new Vector2(0.70f, 1f);
            nRt.offsetMin = new Vector2(12f, 0f); nRt.offsetMax = Vector2.zero;

            TMP_Text scoreTmp = CreateTmp("ScoreText", row.transform, font, "0%", 14, FontStyles.Bold, RetroUITheme.TitleBlue, TextAlignmentOptions.Right);
            RectTransform sRt = scoreTmp.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.70f, 0.5f); sRt.anchorMax = Vector2.one;
            sRt.offsetMin = Vector2.zero; sRt.offsetMax = new Vector2(-12f, 0f);

            GameObject track = CreateImageObject("Track", row.transform, assets.inputBox, RetroUITheme.ProgressTrack, false);
            track.GetComponent<Image>().type = Image.Type.Sliced;
            RectTransform trRt = track.GetComponent<RectTransform>();
            trRt.anchorMin = new Vector2(0f, 0f); trRt.anchorMax = new Vector2(1f, 0.45f);
            trRt.offsetMin = new Vector2(12f, 5f); trRt.offsetMax = new Vector2(-12f, -2f);

            GameObject fill = CreateImageObject("Fill", track.transform, assets.badgePill, RetroUITheme.BtnSkyBlue, false);
            Image fillImg = fill.GetComponent<Image>();
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 0f;
            RectTransform fRt = fill.GetComponent<RectTransform>();
            StretchFull(fRt, 1f, 1f);

            return new TeacherDashboardController.SkillBarSlot
            {
                skillId = skillId,
                rowRoot = row,
                skillNameText = nameTmp,
                skillScoreText = scoreTmp,
                barFillImage = fillImg
            };
        }

        // =====================================================================
        // SHARED RETRO UI PRIMITIVE FACTORIES
        // =====================================================================
        private static GameObject CreateRetroDesktopWindow(
            string name, Transform parent, RetroThemeAssets assets, TMP_FontAsset font,
            Vector2 anchorCenter, Vector2 anchoredPos, Vector2 size,
            Color titleColor, Color bodyColor, Sprite titleIcon, string titleText, bool showDecorativeWindowControls,
            out TMP_Text titleTmp, out Transform titleBarTr)
        {
            GameObject win = CreateImageObject(name, parent, assets.windowBody, bodyColor, false);
            win.GetComponent<Image>().type = Image.Type.Sliced;
            RectTransform rt = win.GetComponent<RectTransform>();
            rt.anchorMin = anchorCenter; rt.anchorMax = anchorCenter;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            GameObject bar = CreateImageObject("WindowTitleBar", win.transform, assets.windowTitleBar, titleColor, false);
            bar.GetComponent<Image>().type = Image.Type.Sliced;
            RectTransform barRt = bar.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0f, 1f); barRt.anchorMax = new Vector2(1f, 1f);
            barRt.pivot = new Vector2(0.5f, 1f);
            barRt.offsetMin = new Vector2(4f, -58f); barRt.offsetMax = new Vector2(-4f, -4f);
            titleBarTr = bar.transform;

            float textLeft = 18f;
            if (titleIcon != null)
            {
                GameObject ic = CreateImageObject("TitleIcon", bar.transform, titleIcon, Color.white, false);
                RectTransform icRt = ic.GetComponent<RectTransform>();
                icRt.anchorMin = new Vector2(0f, 0.5f); icRt.anchorMax = new Vector2(0f, 0.5f);
                icRt.pivot = new Vector2(0f, 0.5f);
                icRt.anchoredPosition = new Vector2(14f, 0f);
                icRt.sizeDelta = new Vector2(34f, 34f);
                ic.GetComponent<Image>().preserveAspect = true;
                textLeft = 58f;
            }

            titleTmp = CreateTmp("WindowTitleText", bar.transform, font, titleText, 21, FontStyles.Bold, RetroUITheme.InkWhite, TextAlignmentOptions.MidlineLeft);
            RectTransform tRt = titleTmp.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
            tRt.offsetMin = new Vector2(textLeft, 2f); tRt.offsetMax = new Vector2(-120f, -2f);

            if (showDecorativeWindowControls)
            {
                Sprite[] topSprites = { assets.topBarMinBtn, assets.topBarMaxBtn, assets.topBarCloseBtn };
                for (int i = 0; i < 3; i++)
                {
                    if (topSprites[i] == null) continue;
                    GameObject cap = CreateImageObject($"DecoCap_{i}", bar.transform, topSprites[i], Color.white, false);
                    RectTransform cRt = cap.GetComponent<RectTransform>();
                    cRt.anchorMin = new Vector2(1f, 0.5f); cRt.anchorMax = new Vector2(1f, 0.5f);
                    cRt.pivot = new Vector2(1f, 0.5f);
                    cRt.anchoredPosition = new Vector2(-14f - (2 - i) * 34f, 0f);
                    cRt.sizeDelta = new Vector2(28f, 28f);
                    cap.GetComponent<Image>().preserveAspect = true;
                }
            }

            return win;
        }

        private static GameObject CreateRetroCardWindow(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size,
            Sprite sprite, Color tint)
        {
            GameObject go = CreateImageObject(name, parent, sprite, tint, false);
            go.GetComponent<Image>().type = Image.Type.Sliced;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return go;
        }

        private static Button CreateRetroButton(
            string name, Transform parent, RetroThemeAssets assets, TMP_FontAsset font,
            string label, Sprite iconSprite, Color bgColor, Color textColor, int fontSize, out TMP_Text labelTmp)
        {
            GameObject btnObj = CreateImageObject(name, parent, assets.button3D, bgColor, true);
            btnObj.GetComponent<Image>().type = Image.Type.Sliced;
            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnObj.GetComponent<Image>();
            RetroUITheme.ConfigureButtonColors(btn, bgColor);

            float textLeft = 14f;
            if (iconSprite != null)
            {
                GameObject icon = CreateImageObject("Icon", btnObj.transform, iconSprite, Color.white, false);
                RectTransform iRt = icon.GetComponent<RectTransform>();
                iRt.anchorMin = new Vector2(0f, 0.5f); iRt.anchorMax = new Vector2(0f, 0.5f);
                iRt.pivot = new Vector2(0f, 0.5f);
                iRt.anchoredPosition = new Vector2(18f, 2f);
                float iconSize = Mathf.Clamp(fontSize * 1.45f, 22f, 44f);
                iRt.sizeDelta = new Vector2(iconSize, iconSize);
                icon.GetComponent<Image>().preserveAspect = true;
                textLeft = 24f + iconSize;
            }

            labelTmp = CreateTmp("Label", btnObj.transform, font, label, fontSize, FontStyles.Bold, textColor, TextAlignmentOptions.Center);
            labelTmp.enableAutoSizing = true;
            labelTmp.fontSizeMin = Mathf.Min(13, fontSize);
            labelTmp.fontSizeMax = fontSize;
            RectTransform lRt = labelTmp.GetComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one;
            lRt.offsetMin = new Vector2(iconSprite != null ? textLeft + 2f : 10f, 4f);
            lRt.offsetMax = new Vector2(-10f, -2f);

            return btn;
        }

        private static Button CreateCloseIconButton(string name, Transform parent, RetroThemeAssets assets)
        {
            GameObject go = CreateImageObject(name, parent, assets.topBarCloseBtn ?? assets.button3D, Color.white, true);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.5f); rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-12f, 0f);
            rt.sizeDelta = new Vector2(36f, 36f);
            go.GetComponent<Image>().preserveAspect = true;
            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = go.GetComponent<Image>();
            return btn;
        }

        private static TMP_InputField CreateRetroInputField(
            string name, Transform parent, RetroThemeAssets assets, TMP_FontAsset font,
            string placeholder, int charLimit, TMP_InputField.ContentType contentType, out TMP_Text placeholderTmp)
        {
            GameObject root = CreateImageObject(name, parent, assets.inputBox, RetroUITheme.InputSurface, true);
            root.GetComponent<Image>().type = Image.Type.Sliced;

            GameObject viewport = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(root.transform, false);
            RectTransform vpRt = viewport.GetComponent<RectTransform>();
            StretchFull(vpRt, 14f, 6f);

            placeholderTmp = CreateTmp("Placeholder", viewport.transform, font, placeholder, 18, FontStyles.Italic, RetroUITheme.InkMuted, TextAlignmentOptions.MidlineLeft);
            StretchFull(placeholderTmp.GetComponent<RectTransform>());

            TMP_Text textTmp = CreateTmp("Text", viewport.transform, font, "", 20, FontStyles.Bold, RetroUITheme.InkDark, TextAlignmentOptions.MidlineLeft);
            StretchFull(textTmp.GetComponent<RectTransform>());

            TMP_InputField input = root.AddComponent<TMP_InputField>();
            input.targetGraphic = root.GetComponent<Image>();
            input.textViewport = vpRt;
            input.textComponent = textTmp;
            input.placeholder = placeholderTmp;
            if (font != null) input.fontAsset = font;
            input.pointSize = 20f;
            input.characterLimit = charLimit;
            input.contentType = contentType;
            input.caretColor = RetroUITheme.TitleBlue;
            input.customCaretColor = true;
            input.selectionColor = new Color(0.22f, 0.62f, 0.96f, 0.35f);

            return input;
        }

        private static GameObject CreateRetroScrollList(string name, Transform parent, RetroThemeAssets assets, out Transform contentTr)
        {
            GameObject scrollObj = CreateImageObject(name, parent, assets.inputBox, RetroUITheme.CardWhite, true);
            scrollObj.GetComponent<Image>().type = Image.Type.Sliced;

            GameObject vp = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            vp.transform.SetParent(scrollObj.transform, false);
            RectTransform vpRt = vp.GetComponent<RectTransform>();
            StretchFull(vpRt, 8f, 8f);

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(vp.transform, false);
            RectTransform cRt = content.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0f, 1f); cRt.anchorMax = new Vector2(1f, 1f);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.offsetMin = Vector2.zero; cRt.offsetMax = Vector2.zero;

            VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 10f; vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.childControlWidth = true; vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect sr = scrollObj.AddComponent<ScrollRect>();
            sr.viewport = vpRt;
            sr.content = cRt;
            sr.horizontal = false;
            sr.vertical = true;
            sr.scrollSensitivity = 36f;

            contentTr = content.transform;
            return scrollObj;
        }

        private static TMP_Text CreateStatMiniCard(Transform parent, RetroThemeAssets assets, TMP_FontAsset font, string label, Sprite icon, Color tint)
        {
            GameObject card = CreateImageObject($"MiniStat_{label}", parent, assets.cardRounded, tint, false);
            card.GetComponent<Image>().type = Image.Type.Sliced;

            if (icon != null)
            {
                GameObject ic = CreateImageObject("Icon", card.transform, icon, Color.white, false);
                RectTransform iRt = ic.GetComponent<RectTransform>();
                iRt.anchorMin = new Vector2(0f, 0.5f); iRt.anchorMax = new Vector2(0f, 0.5f);
                iRt.pivot = new Vector2(0f, 0.5f);
                iRt.anchoredPosition = new Vector2(18f, 0f);
                iRt.sizeDelta = new Vector2(52f, 52f);
                ic.GetComponent<Image>().preserveAspect = true;
            }

            TMP_Text lbl = CreateTmp("Caption", card.transform, font, label, 20, FontStyles.Bold, RetroUITheme.InkDark, TextAlignmentOptions.MidlineLeft);
            RectTransform lRt = lbl.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0f, 0f); lRt.anchorMax = new Vector2(0.68f, 1f);
            lRt.offsetMin = new Vector2(84f, 0f); lRt.offsetMax = Vector2.zero;

            TMP_Text val = CreateTmp("Value", card.transform, font, "0", 30, FontStyles.Bold, RetroUITheme.InkDark, TextAlignmentOptions.Right);
            RectTransform vRt = val.GetComponent<RectTransform>();
            vRt.anchorMin = new Vector2(0.55f, 0f); vRt.anchorMax = Vector2.one;
            vRt.offsetMin = Vector2.zero; vRt.offsetMax = new Vector2(-22f, 0f);
            return val;
        }

        private static TMP_Text CreateKpiBox(Transform parent, RetroThemeAssets assets, TMP_FontAsset font, Color tint, out TMP_Text labelTmp)
        {
            GameObject box = CreateImageObject("KpiBox", parent, assets.cardRounded, tint, false);
            box.GetComponent<Image>().type = Image.Type.Sliced;

            TMP_Text val = CreateTmp("Value", box.transform, font, "0", 26, FontStyles.Bold, RetroUITheme.InkDark, TextAlignmentOptions.Center);
            RectTransform vRt = val.GetComponent<RectTransform>();
            vRt.anchorMin = new Vector2(0f, 0.35f); vRt.anchorMax = Vector2.one;
            vRt.offsetMin = new Vector2(6f, 0f); vRt.offsetMax = new Vector2(-6f, -6f);

            labelTmp = CreateTmp("Label", box.transform, font, "", 12, FontStyles.Bold, RetroUITheme.InkMuted, TextAlignmentOptions.Center);
            RectTransform lRt = labelTmp.GetComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero; lRt.anchorMax = new Vector2(1f, 0.42f);
            lRt.offsetMin = new Vector2(6f, 4f); lRt.offsetMax = new Vector2(-6f, 0f);

            return val;
        }

        private static GameObject ConfigureOrCreateLanguageDropdown(GameObject existing, Transform parent, RetroThemeAssets assets, TMP_FontAsset font)
        {
            if (existing != null)
            {
                existing.transform.SetParent(parent, false);
                Image bg = existing.GetComponent<Image>();
                if (bg != null && assets.button3D != null)
                {
                    bg.sprite = assets.button3D;
                    bg.type = Image.Type.Sliced;
                    bg.color = RetroUITheme.CardWhite;
                }
                TMP_Dropdown dd = existing.GetComponent<TMP_Dropdown>();
                if (dd != null && dd.captionText != null)
                    dd.captionText.color = RetroUITheme.InkDark;
                Transform arrowTr = existing.transform.Find("Arrow");
                if (arrowTr != null)
                {
                    Image arrowImg = arrowTr.GetComponent<Image>();
                    if (arrowImg != null) arrowImg.color = RetroUITheme.InkDark;
                }
                return existing;
            }

            // Build fresh Language Dropdown if missing
            GameObject go = CreateImageObject("Dropdown-Language", parent, assets.button3D, RetroUITheme.CardWhite, true);
            go.GetComponent<Image>().type = Image.Type.Sliced;
            TMP_Dropdown dropdown = go.AddComponent<TMP_Dropdown>();
            go.AddComponent<LanguageDropdown>();

            TMP_Text caption = CreateTmp("Label", go.transform, font, "English (EN)", 17, FontStyles.Bold, RetroUITheme.InkDark, TextAlignmentOptions.MidlineLeft);
            StretchFull(caption.GetComponent<RectTransform>(), 12f, 2f);
            dropdown.captionText = caption;

            // Template
            GameObject template = CreateImageObject("Template", go.transform, assets.windowBody, RetroUITheme.WindowCream, true);
            template.GetComponent<Image>().type = Image.Type.Sliced;
            RectTransform tRt = template.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0f); tRt.anchorMax = new Vector2(1f, 0f);
            tRt.pivot = new Vector2(0.5f, 1f);
            tRt.sizeDelta = new Vector2(0f, 120f);

            GameObject item = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
            item.transform.SetParent(template.transform, false);
            item.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 36f);
            GameObject itemBg = CreateImageObject("Item Background", item.transform, null, RetroUITheme.CardPastelBlue, true);
            StretchFull(itemBg.GetComponent<RectTransform>());
            TMP_Text itemLbl = CreateTmp("Item Label", item.transform, font, "English (EN)", 16, FontStyles.Bold, RetroUITheme.InkDark, TextAlignmentOptions.MidlineLeft);
            StretchFull(itemLbl.GetComponent<RectTransform>(), 12f, 2f);

            Toggle toggle = item.GetComponent<Toggle>();
            toggle.targetGraphic = itemBg.GetComponent<Image>();

            dropdown.template = tRt;
            dropdown.itemText = itemLbl;
            dropdown.options.Add(new TMP_Dropdown.OptionData("English (EN)"));
            dropdown.options.Add(new TMP_Dropdown.OptionData("Español (ES)"));
            dropdown.options.Add(new TMP_Dropdown.OptionData("Norsk (NO)"));
            template.SetActive(false);
            return go;
        }

        // =====================================================================
        // LOW-LEVEL HELPERS
        // =====================================================================
        private static RetroThemeAssets EnsureThemeAssets()
        {
            RetroThemeAssets assets = RetroThemeAssets.Instance;
            if (assets == null || assets.windowBody == null)
                assets = RetroThemeAssetGenerator.GenerateAll();
            return assets;
        }

        private static void EnsureTeacherConfigAsset()
        {
            TeacherModeConfig cfg = AssetDatabase.LoadAssetAtPath<TeacherModeConfig>(TeacherConfigAssetPath);
            if (cfg == null)
            {
                cfg = ScriptableObject.CreateInstance<TeacherModeConfig>();
                cfg.allowTeacherMode = true;
                cfg.defaultPin = "1234";
                cfg.requireLongPress = false;
                cfg.longPressDuration = 1.5f;
                AssetDatabase.CreateAsset(cfg, TeacherConfigAssetPath);
                AssetDatabase.SaveAssets();
            }
        }

        [MenuItem("Tools/Retro UI/5. Apply Retro Theme to Window Prefabs")]
        public static void ApplyRetroThemeToWindowPrefabs()
        {
            RetroThemeAssets assets = EnsureThemeAssets();
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MainFontPath);

            string[] windowPrefabs =
            {
                "Assets/Prefabs/Windows/BaseWindow.prefab",
                "Assets/Prefabs/Windows/GenericLevelWindow.prefab",
                "Assets/Prefabs/Windows/AdaptiveSessionWindow.prefab"
            };

            foreach (string path in windowPrefabs)
            {
                RetroizeWindowPrefab(path, assets, font);
            }

            RetroizeSkillsReportContentPrefab("Assets/Prefabs/Windows/SkillsReportContent.prefab");
            RetroizeSkillsReportWindowPrefab("Assets/Prefabs/Windows/SkillsReportWindow.prefab", assets, font);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[RetroSceneMaterializer] Applied Menu-identical Colorful Retro Desktop chrome to BaseWindow, GenericLevelWindow, AdaptiveSessionWindow, SkillsReportWindow, and SkillsReportContent.");
        }

        private static void RetroizeWindowPrefab(string prefabPath, RetroThemeAssets assets, TMP_FontAsset font)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                Debug.LogWarning($"[RetroSceneMaterializer] Prefab not found: {prefabPath}");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                ApplyMenuWindowChrome(root, assets, font);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ApplyMenuWindowChrome(GameObject root, RetroThemeAssets assets, TMP_FontAsset font)
        {
            if (root == null || assets == null) return;
            Window winComp = root.GetComponent<Window>();

            // 1. WindowBackground: Exact same 9-slice cream frame as Menu MainMenuWindow
            Transform bgTr = FindChildRecursive(root.transform, "WindowBackground");
            if (bgTr != null)
            {
                StripComponent<Outline>(bgTr.gameObject);
                Image bgImg = bgTr.GetComponent<Image>();
                if (bgImg != null)
                {
                    RetroUITheme.ApplySlicedSprite(bgImg, assets.windowBody, RetroUITheme.WindowCream, true);
                }
            }

            // 2. WindowTop: Exact 54px Royal Blue titlebar with inset (4, -58, -4, -4)
            Transform topTr = FindChildRecursive(root.transform, "WindowTop");
            bool hasIcon = false;
            if (topTr != null)
            {
                StripComponent<Outline>(topTr.gameObject);
                Image topImg = topTr.GetComponent<Image>();
                if (topImg != null)
                {
                    RetroUITheme.ApplySlicedSprite(topImg, assets.windowTitleBar, RetroUITheme.TitleBlue, true);
                }

                RectTransform topRt = topTr.GetComponent<RectTransform>();
                if (topRt != null)
                {
                    topRt.anchorMin = new Vector2(0f, 1f);
                    topRt.anchorMax = new Vector2(1f, 1f);
                    topRt.pivot = new Vector2(0.5f, 1f);
                    topRt.offsetMin = new Vector2(4f, -58f);
                    topRt.offsetMax = new Vector2(-4f, -4f);
                }

                // Ensure WindowIcon child exists and is wired to Window.windowIconImg
                Transform iconTr = topTr.Find("WindowIcon");
                GameObject iconGo = iconTr != null ? iconTr.gameObject : new GameObject("WindowIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                if (iconTr == null) iconGo.transform.SetParent(topTr, false);
                iconGo.transform.SetAsFirstSibling();

                RectTransform iconRt = iconGo.GetComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0f, 0.5f);
                iconRt.anchorMax = new Vector2(0f, 0.5f);
                iconRt.pivot = new Vector2(0f, 0.5f);
                iconRt.anchoredPosition = new Vector2(16f, 0f);
                iconRt.sizeDelta = new Vector2(32f, 32f);

                Image iconImg = iconGo.GetComponent<Image>();
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
                iconImg.color = Color.white;

                if (winComp != null)
                {
                    FieldInfo fiIcon = typeof(Window).GetField("windowIcon", BindingFlags.Instance | BindingFlags.NonPublic);
                    Sprite assignedIcon = fiIcon != null ? fiIcon.GetValue(winComp) as Sprite : null;
                    hasIcon = assignedIcon != null;
                    if (hasIcon) iconImg.sprite = assignedIcon;
                    iconGo.SetActive(hasIcon);
                    SetPrivateField(winComp, "windowIconImg", iconImg);
                }

                // Title Label
                Transform titleTr = FindChildRecursive(topTr, "WindowTitleText");
                if (titleTr != null)
                {
                    TMP_Text tmp = titleTr.GetComponent<TMP_Text>();
                    if (tmp != null)
                    {
                        if (font != null) tmp.font = font;
                        tmp.fontSize = 20f;
                        tmp.fontStyle = FontStyles.Bold;
                        tmp.color = RetroUITheme.InkWhite;
                        tmp.alignment = TextAlignmentOptions.MidlineLeft;
                        tmp.raycastTarget = false;
                    }
                    RectTransform titleRt = titleTr.GetComponent<RectTransform>();
                    if (titleRt != null)
                    {
                        titleRt.anchorMin = Vector2.zero;
                        titleRt.anchorMax = Vector2.one;
                        titleRt.offsetMin = new Vector2(hasIcon ? 56f : 18f, 4f);
                        titleRt.offsetMax = new Vector2(-128f, -4f);
                    }
                }

                // Window Control Buttons (Minimize, Maximize, Close) matching Menu TopBarIcons
                Transform btnsTr = topTr.Find("Buttons");
                if (btnsTr != null)
                {
                    RectTransform btnsRt = btnsTr.GetComponent<RectTransform>();
                    btnsRt.anchorMin = new Vector2(1f, 0.5f);
                    btnsRt.anchorMax = new Vector2(1f, 0.5f);
                    btnsRt.pivot = new Vector2(1f, 0.5f);
                    btnsRt.anchoredPosition = new Vector2(-12f, 0f);
                    btnsRt.sizeDelta = new Vector2(106f, 34f);

                    HorizontalLayoutGroup hlg = btnsTr.GetComponent<HorizontalLayoutGroup>() ?? btnsTr.gameObject.AddComponent<HorizontalLayoutGroup>();
                    hlg.spacing = 8f;
                    hlg.childAlignment = TextAnchor.MiddleRight;
                    hlg.childControlWidth = false;
                    hlg.childControlHeight = false;
                    hlg.childForceExpandWidth = false;
                    hlg.childForceExpandHeight = false;

                    Transform minTr = btnsTr.Find("Minimize");
                    Transform maxTr = btnsTr.Find("Maximize");
                    Transform closeTr = btnsTr.Find("Close");
                    if (minTr != null) minTr.SetSiblingIndex(0);
                    if (maxTr != null) maxTr.SetSiblingIndex(1);
                    if (closeTr != null) closeTr.SetSiblingIndex(2);

                    ApplyRetroCaptionButton(minTr, assets.topBarMinBtn);
                    ApplyRetroCaptionButton(maxTr, assets.topBarMaxBtn);
                    ApplyRetroCaptionButton(closeTr, assets.topBarCloseBtn);
                }
            }

            // 3. WindowContents inset below 54px TopBar and inside 6px retro bezel
            Transform contentsTr = root.transform.Find("WindowContents");
            if (contentsTr != null)
            {
                RectTransform cRt = contentsTr.GetComponent<RectTransform>();
                if (cRt != null)
                {
                    cRt.anchorMin = Vector2.zero;
                    cRt.anchorMax = Vector2.one;
                    cRt.offsetMin = new Vector2(6f, 6f);
                    cRt.offsetMax = new Vector2(-6f, -58f);
                }
            }
        }

        private static void ApplyRetroCaptionButton(Transform btnTr, Sprite sprite)
        {
            if (btnTr == null) return;
            ClearChildrenImmediate(btnTr);

            RectTransform rt = btnTr.GetComponent<RectTransform>();
            if (rt != null)
                rt.sizeDelta = new Vector2(30f, 30f);

            Image img = btnTr.GetComponent<Image>() ?? btnTr.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.color = Color.white;
            img.raycastTarget = true;

            Button btn = btnTr.GetComponent<Button>();
            if (btn != null)
            {
                btn.targetGraphic = img;
                ColorBlock cb = btn.colors;
                cb.normalColor = Color.white;
                cb.highlightedColor = new Color(1.06f, 1.06f, 1.06f, 1f);
                cb.pressedColor = new Color(0.84f, 0.88f, 0.94f, 1f);
                cb.selectedColor = Color.white;
                btn.colors = cb;
            }

            WindowCaptionButtonVisual vis = btnTr.GetComponent<WindowCaptionButtonVisual>();
            if (vis != null)
            {
                SetPrivateField(vis, "background", img);
                SetPrivateField(vis, "glyphGraphics", new Graphic[0]);
                SetPrivateField(vis, "normalBgColor", Color.white);
                SetPrivateField(vis, "hoverBgColor", new Color(0.86f, 0.94f, 1f, 1f));
                SetPrivateField(vis, "disabledBgColor", new Color(0.75f, 0.78f, 0.84f, 0.55f));
            }
        }

        private static void RetroizeSkillsReportContentPrefab(string contentPrefabPath)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(contentPrefabPath) == null) return;
            GameObject root = PrefabUtility.LoadPrefabContents(contentPrefabPath);
            try
            {
                RemoveMissingScriptsRecursive(root);
                ClearChildrenImmediate(root.transform);

                Image img = root.GetComponent<Image>() ?? root.AddComponent<Image>();
                img.sprite = null;
                img.color = RetroUITheme.WindowSkySoft;
                img.raycastTarget = false;

                if (root.GetComponent<ScoresApp>() == null)
                    root.AddComponent<ScoresApp>();

                PrefabUtility.SaveAsPrefabAsset(root, contentPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void RetroizeSkillsReportWindowPrefab(string reportPrefabPath, RetroThemeAssets assets, TMP_FontAsset font)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(reportPrefabPath) == null) return;
            GameObject root = PrefabUtility.LoadPrefabContents(reportPrefabPath);
            try
            {
                RemoveMissingScriptsRecursive(root);
                ApplyMenuWindowChrome(root, assets, font);

                RectTransform rootRt = root.GetComponent<RectTransform>();
                if (rootRt != null)
                    rootRt.sizeDelta = new Vector2(1480f, 820f);

                Window winComp = root.GetComponent<Window>();
                if (winComp != null)
                {
                    GameObject contentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Windows/SkillsReportContent.prefab");
                    if (contentPrefab != null)
                        SetPrivateField(winComp, "contentPrefab", contentPrefab);

                    SetPrivateField(winComp, "windowTitle", "Scores & Achievements");
                    if (assets.goldMedal != null)
                        SetPrivateField(winComp, "windowIcon", assets.goldMedal);

                    Transform iconTr = FindChildRecursive(root.transform, "WindowIcon");
                    if (iconTr != null && assets.goldMedal != null)
                    {
                        iconTr.gameObject.SetActive(true);
                        Image iconImg = iconTr.GetComponent<Image>();
                        if (iconImg != null) iconImg.sprite = assets.goldMedal;
                    }
                    Transform titleTr = FindChildRecursive(root.transform, "WindowTitleText");
                    if (titleTr != null)
                    {
                        RectTransform tRt = titleTr.GetComponent<RectTransform>();
                        if (tRt != null) tRt.offsetMin = new Vector2(56f, 4f);
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, reportPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void RemoveMissingScriptsRecursive(GameObject go)
        {
            if (go == null) return;
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            for (int i = 0; i < go.transform.childCount; i++)
                RemoveMissingScriptsRecursive(go.transform.GetChild(i).gameObject);
        }

        private static Canvas FindPrimaryCanvas(Scene scene, string preferredName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Canvas c = root.GetComponent<Canvas>();
                if (c != null && (root.name == preferredName || c.GetComponent<CRTOverlayController>() == null))
                    return c;
            }
            return null;
        }

        private static void ConfigureCanvasScaler(Canvas canvas)
        {
            CanvasScaler cs = canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920f, 1080f);
            cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            cs.matchWidthOrHeight = 0.5f;
        }

        private static GameObject CreateImageObject(string name, Transform parent, Sprite sprite, Color color, bool raycastTarget)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            if (sprite != null) img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycastTarget;
            return go;
        }

        private static TextMeshProUGUI CreateTmp(string name, Transform parent, TMP_FontAsset font, string text, float size, FontStyles style, Color color, TextAlignmentOptions align)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void StretchFull(RectTransform rt, float padX = 0f, float padY = 0f)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padX, padY);
            rt.offsetMax = new Vector2(-padX, -padY);
        }

        private static void ClearChildrenImmediate(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }

        private static void StripComponent<T>(GameObject go) where T : Component
        {
            T[] comps = go.GetComponents<T>();
            for (int i = 0; i < comps.Length; i++)
                UnityEngine.Object.DestroyImmediate(comps[i]);
        }

        private static Transform FindChildRecursive(Transform root, string targetName)
        {
            if (root == null) return null;
            if (root.name == targetName) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChildRecursive(root.GetChild(i), targetName);
                if (found != null) return found;
            }
            return null;
        }

        // Linear-calibrated navy ink so Overlay Canvas (vertexColorAlwaysGammaSpace=false) renders crisp deep navy #1E3354
        private static readonly Color DesktopDarkInk = new Color(0.015f, 0.035f, 0.085f, 1f);
        private static readonly Color DesktopMutedInk = new Color(0.10f, 0.16f, 0.28f, 1f);

        private static Sprite LoadRetroComputerIcon(RetroThemeAssets assets)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/App icons/Computer Presentation.png")
                ?? assets.computerIcon
                ?? assets.mascotLogoNoBg;
        }

        [MenuItem("Tools/Retro UI/6. Apply Retro Theme to Taskbar & Start Menu")]
        public static void ApplyRetroThemeToTaskbarAndStartMenu()
        {
            RetroThemeAssets assets = EnsureThemeAssets();
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MainFontPath);

            RetroizeTaskbarPrefab("Assets/Prefabs/Desktop/Taskbar.prefab", assets, font);
            RetroizeTaskbarWindowButtonPrefab("Assets/Prefabs/Desktop/TaskbarWindowButton.prefab", assets, font);

            string previousScenePath = SceneManager.GetActiveScene().path;
            try
            {
                RetroizeDesktopScene("Assets/Scenes/Desktop1.unity", false, assets, font);
                RetroizeDesktopScene("Assets/Scenes/VirtualDesktop.unity", true, assets, font);
            }
            finally
            {
                if (!string.IsNullOrEmpty(previousScenePath) && System.IO.File.Exists(previousScenePath))
                {
                    EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[RetroSceneMaterializer] Applied Colorful Retro Desktop theme to Taskbar.prefab, TaskbarWindowButton.prefab, Desktop1.unity, and VirtualDesktop.unity StartMenuPanel.");
        }

        private static void RetroizeTaskbarPrefab(string prefabPath, RetroThemeAssets assets, TMP_FontAsset font)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                Debug.LogWarning($"[RetroSceneMaterializer] Prefab not found: {prefabPath}");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                RectTransform rootRt = root.GetComponent<RectTransform>();
                if (rootRt != null)
                {
                    rootRt.sizeDelta = new Vector2(rootRt.sizeDelta.x, 68f);
                }

                StripComponent<Outline>(root);
                Image bgImg = root.GetComponent<Image>();
                if (bgImg != null)
                {
                    RetroUITheme.ApplySlicedSprite(bgImg, assets.windowBody, RetroUITheme.WindowCream, true);
                }

                // Subtle top trim line matching Menu window Royal Blue chrome
                Transform trimTr = root.transform.Find("TopTrim");
                GameObject trimGo = trimTr != null ? trimTr.gameObject : CreateImageObject("TopTrim", root.transform, assets.windowTitleBar, RetroUITheme.TitleBlue, false);
                trimGo.transform.SetAsFirstSibling();
                Image trimImg = trimGo.GetComponent<Image>();
                RetroUITheme.ApplySlicedSprite(trimImg, assets.windowTitleBar, RetroUITheme.TitleBlue, false);
                RectTransform trimRt = trimGo.GetComponent<RectTransform>();
                trimRt.anchorMin = new Vector2(0f, 1f);
                trimRt.anchorMax = new Vector2(1f, 1f);
                trimRt.pivot = new Vector2(0.5f, 1f);
                trimRt.offsetMin = new Vector2(4f, -5f);
                trimRt.offsetMax = new Vector2(-4f, -1f);

                // Style DesktopLabel for scenes without StartButton (e.g., Desktop1)
                Transform labelTr = root.transform.Find("DesktopLabel");
                if (labelTr != null)
                {
                    TMP_Text labelTmp = labelTr.GetComponent<TMP_Text>();
                    if (labelTmp != null)
                    {
                        if (font != null) labelTmp.font = font;
                        labelTmp.fontSize = 15f;
                        labelTmp.fontStyle = FontStyles.Bold;
                        labelTmp.color = RetroUITheme.TitleBlue;
                        labelTmp.alignment = TextAlignmentOptions.Center;
                        labelTmp.raycastTarget = false;
                    }
                    RectTransform labelRt = labelTr.GetComponent<RectTransform>();
                    if (labelRt != null)
                    {
                        labelRt.anchorMin = new Vector2(0f, 0.5f);
                        labelRt.anchorMax = new Vector2(0f, 0.5f);
                        labelRt.pivot = new Vector2(0f, 0.5f);
                        labelRt.anchoredPosition = new Vector2(14f, 0f);
                        labelRt.sizeDelta = new Vector2(174f, 48f);
                    }
                }

                // Right-side rounded System Clock Tray
                EnsureTaskbarClockTray(root.transform, assets, font);

                // Responsive Viewport between left control and right ClockTray
                Transform viewportTr = root.transform.Find("Viewport");
                if (viewportTr != null)
                {
                    RectTransform vpRt = viewportTr.GetComponent<RectTransform>();
                    vpRt.anchorMin = Vector2.zero;
                    vpRt.anchorMax = Vector2.one;
                    vpRt.pivot = new Vector2(0.5f, 0.5f);
                    vpRt.offsetMin = new Vector2(198f, 8f);
                    vpRt.offsetMax = new Vector2(-130f, -10f);
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureTaskbarClockTray(Transform taskbarRoot, RetroThemeAssets assets, TMP_FontAsset font)
        {
            Transform trayTr = taskbarRoot.Find("ClockTray");
            GameObject trayGo = trayTr != null ? trayTr.gameObject : CreateImageObject("ClockTray", taskbarRoot, assets.cardRounded, RetroUITheme.CardPastelBlue, false);
            Image trayImg = trayGo.GetComponent<Image>();
            RetroUITheme.ApplySlicedSprite(trayImg, assets.cardRounded, RetroUITheme.CardPastelBlue, false);

            RectTransform trayRt = trayGo.GetComponent<RectTransform>();
            trayRt.anchorMin = new Vector2(1f, 0.5f);
            trayRt.anchorMax = new Vector2(1f, 0.5f);
            trayRt.pivot = new Vector2(1f, 0.5f);
            trayRt.anchoredPosition = new Vector2(-12f, 0f);
            trayRt.sizeDelta = new Vector2(106f, 44f);

            Transform txtTr = trayGo.transform.Find("ClockText");
            TextMeshProUGUI clockTmp = txtTr != null
                ? txtTr.GetComponent<TextMeshProUGUI>()
                : CreateTmp("ClockText", trayGo.transform, font, "12:00", 20f, FontStyles.Bold, RetroUITheme.InkDark, TextAlignmentOptions.Center);
                if (clockTmp != null)
            {
                if (font != null) clockTmp.font = font;
                clockTmp.fontSize = 20f;
                clockTmp.fontStyle = FontStyles.Bold;
                clockTmp.color = DesktopDarkInk;
                clockTmp.alignment = TextAlignmentOptions.Center;
                clockTmp.raycastTarget = false;
                StretchFull(clockTmp.GetComponent<RectTransform>(), 6f, 2f);
            }

            TaskbarClock clockComp = trayGo.GetComponent<TaskbarClock>() ?? trayGo.AddComponent<TaskbarClock>();
            SetPrivateField(clockComp, "timeText", clockTmp);
        }

        private static void RetroizeTaskbarWindowButtonPrefab(string prefabPath, RetroThemeAssets assets, TMP_FontAsset font)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                Debug.LogWarning($"[RetroSceneMaterializer] Prefab not found: {prefabPath}");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                RectTransform rootRt = root.GetComponent<RectTransform>();
                if (rootRt != null)
                {
                    rootRt.sizeDelta = new Vector2(220f, 46f);
                }

                LayoutElement le = root.GetComponent<LayoutElement>() ?? root.AddComponent<LayoutElement>();
                le.minWidth = 170f;
                le.minHeight = 46f;
                le.preferredWidth = 220f;
                le.preferredHeight = 46f;

                StripComponent<Outline>(root);
                Image bgImg = root.GetComponent<Image>();
                if (bgImg != null)
                {
                    RetroUITheme.ApplySlicedSprite(bgImg, assets.button3D, RetroUITheme.CardPastelBlue, true);
                }

                Button btn = root.GetComponent<Button>();
                if (btn != null)
                {
                    btn.transition = Selectable.Transition.ColorTint;
                    btn.targetGraphic = bgImg;
                    RetroUITheme.ConfigureButtonColors(btn, RetroUITheme.CardPastelBlue);
                }

                Transform iconTr = root.transform.Find("Icon");
                Image iconImg = iconTr != null ? iconTr.GetComponent<Image>() : null;
                if (iconTr != null && iconImg != null)
                {
                    RectTransform iconRt = iconTr.GetComponent<RectTransform>();
                    iconRt.anchorMin = new Vector2(0f, 0.5f);
                    iconRt.anchorMax = new Vector2(0f, 0.5f);
                    iconRt.pivot = new Vector2(0f, 0.5f);
                    iconRt.anchoredPosition = new Vector2(12f, 1f);
                    iconRt.sizeDelta = new Vector2(28f, 28f);
                    iconImg.preserveAspect = true;
                    iconImg.raycastTarget = false;
                }

                Transform titleTr = root.transform.Find("Title");
                TMP_Text titleTmp = titleTr != null ? titleTr.GetComponent<TMP_Text>() : null;
                if (titleTr != null && titleTmp != null)
                {
                    RectTransform titleRt = titleTr.GetComponent<RectTransform>();
                    titleRt.anchorMin = Vector2.zero;
                    titleRt.anchorMax = Vector2.one;
                    titleRt.pivot = new Vector2(0.5f, 0.5f);
                    titleRt.offsetMin = new Vector2(48f, 6f);
                    titleRt.offsetMax = new Vector2(-12f, -4f);

                    if (font != null) titleTmp.font = font;
                    titleTmp.fontSize = 17f;
                    titleTmp.fontStyle = FontStyles.Bold;
                    titleTmp.color = DesktopDarkInk;
                    titleTmp.alignment = TextAlignmentOptions.MidlineLeft;
                    titleTmp.overflowMode = TextOverflowModes.Ellipsis;
                    titleTmp.textWrappingMode = TextWrappingModes.NoWrap;
                    titleTmp.raycastTarget = false;
                }

                Transform indTr = root.transform.Find("ActiveIndicator");
                if (indTr != null)
                {
                    RectTransform indRt = indTr.GetComponent<RectTransform>();
                    indRt.anchorMin = new Vector2(0f, 0f);
                    indRt.anchorMax = new Vector2(1f, 0f);
                    indRt.pivot = new Vector2(0.5f, 0f);
                    indRt.anchoredPosition = new Vector2(0f, 4f);
                    indRt.sizeDelta = new Vector2(-24f, 5f);

                    Image indImg = indTr.GetComponent<Image>();
                    RetroUITheme.ApplySlicedSprite(indImg, assets.badgePill, RetroUITheme.BtnSunnyGold, false);
                }

                TaskbarWindowButton twb = root.GetComponent<TaskbarWindowButton>();
                if (twb != null)
                {
                    SetPrivateField(twb, "title", titleTmp);
                    SetPrivateField(twb, "icon", iconImg);
                    SetPrivateField(twb, "background", bgImg);
                    SetPrivateField(twb, "activeIndicator", indTr != null ? indTr.gameObject : null);
                    SetPrivateField(twb, "normalColor", RetroUITheme.CardPastelBlue);
                    SetPrivateField(twb, "activeColor", RetroUITheme.TitleBlue);
                    SetPrivateField(twb, "minimizedColor", RetroUITheme.BtnSoftSlate);
                    SetPrivateField(twb, "normalTextColor", DesktopDarkInk);
                    SetPrivateField(twb, "activeTextColor", RetroUITheme.InkWhite);
                    SetPrivateField(twb, "minimizedTextColor", DesktopMutedInk);
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void RetroizeDesktopScene(string scenePath, bool hasStartMenu, RetroThemeAssets assets, TMP_FontAsset font)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Transform taskbarTr = null;
            Transform startMenuTr = null;

            foreach (GameObject rootObj in scene.GetRootGameObjects())
            {
                if (taskbarTr == null) taskbarTr = FindChildRecursive(rootObj.transform, "Taskbar");
                if (startMenuTr == null) startMenuTr = FindChildRecursive(rootObj.transform, "StartMenuPanel");
            }

            if (taskbarTr != null)
            {
                RectTransform taskbarRt = taskbarTr.GetComponent<RectTransform>();
                if (taskbarRt != null)
                {
                    taskbarRt.sizeDelta = new Vector2(taskbarRt.sizeDelta.x, 68f);
                }

                Transform desktopLabelTr = taskbarTr.Find("DesktopLabel");
                if (desktopLabelTr != null)
                {
                    TMP_Text lbl = desktopLabelTr.GetComponent<TMP_Text>();
                    if (lbl != null)
                    {
                        if (font != null) lbl.font = font;
                        lbl.fontSize = 15f;
                        lbl.fontStyle = FontStyles.Bold;
                        lbl.color = RetroUITheme.TitleBlue;
                        lbl.alignment = TextAlignmentOptions.Center;
                        lbl.raycastTarget = false;
                        EditorUtility.SetDirty(lbl);
                    }
                }

                if (hasStartMenu)
                {
                    Transform startBtnTr = taskbarTr.Find("StartButton");
                    if (startBtnTr != null)
                    {
                        RetroizeStartButton(startBtnTr, assets, font);
                    }

                    Transform viewportTr = taskbarTr.Find("Viewport");
                    if (viewportTr != null)
                    {
                        RectTransform vpRt = viewportTr.GetComponent<RectTransform>();
                        vpRt.anchorMin = Vector2.zero;
                        vpRt.anchorMax = Vector2.one;
                        vpRt.pivot = new Vector2(0.5f, 0.5f);
                        vpRt.offsetMin = new Vector2(162f, 8f);
                        vpRt.offsetMax = new Vector2(-130f, -10f);
                        EditorUtility.SetDirty(vpRt);
                    }
                }

                EditorUtility.SetDirty(taskbarTr.gameObject);
            }

            if (hasStartMenu && startMenuTr != null)
            {
                RetroizeStartMenuPanel(startMenuTr, assets, font);
                EditorUtility.SetDirty(startMenuTr.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void RetroizeStartButton(Transform startBtnTr, RetroThemeAssets assets, TMP_FontAsset font)
        {
            RectTransform rt = startBtnTr.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.anchoredPosition = new Vector2(12f, 0f);
                rt.sizeDelta = new Vector2(138f, 46f);
            }

            StripComponent<Outline>(startBtnTr.gameObject);
            Image bgImg = startBtnTr.GetComponent<Image>();
            if (bgImg != null)
            {
                RetroUITheme.ApplySlicedSprite(bgImg, assets.button3D, RetroUITheme.BtnSunnyGold, true);
            }

            Button btn = startBtnTr.GetComponent<Button>();
            if (btn != null)
            {
                btn.transition = Selectable.Transition.ColorTint;
                btn.targetGraphic = bgImg;
                RetroUITheme.ConfigureButtonColors(btn, RetroUITheme.BtnSunnyGold);
            }

            Sprite startIconSprite = LoadRetroComputerIcon(assets);
            Transform iconTr = startBtnTr.Find("Icon");
            GameObject iconGo = iconTr != null
                ? iconTr.gameObject
                : CreateImageObject("Icon", startBtnTr, startIconSprite, Color.white, false);
            iconGo.transform.SetAsFirstSibling();
            Image iconImg = iconGo.GetComponent<Image>();
            iconImg.sprite = startIconSprite;
            iconImg.type = Image.Type.Simple;
            iconImg.preserveAspect = true;
            iconImg.color = Color.white;
            iconImg.raycastTarget = false;

            RectTransform iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.anchoredPosition = new Vector2(11f, 1f);
            iconRt.sizeDelta = new Vector2(34f, 34f);

            Transform txtTr = startBtnTr.Find("Text");
            TMP_Text tmp = txtTr != null ? txtTr.GetComponent<TMP_Text>() : startBtnTr.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
            {
                if (font != null) tmp.font = font;
                tmp.fontSize = 20f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = DesktopDarkInk;
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.raycastTarget = false;

                RectTransform txtRt = tmp.GetComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.pivot = new Vector2(0.5f, 0.5f);
                txtRt.offsetMin = new Vector2(51f, 4f);
                txtRt.offsetMax = new Vector2(-10f, -2f);
            }
        }

        private static void RetroizeStartMenuPanel(Transform panelTr, RetroThemeAssets assets, TMP_FontAsset font)
        {
            RectTransform panelRt = panelTr.GetComponent<RectTransform>();
            if (panelRt != null)
            {
                panelRt.anchorMin = Vector2.zero;
                panelRt.anchorMax = Vector2.zero;
                panelRt.pivot = Vector2.zero;
                panelRt.anchoredPosition = new Vector2(14f, 74f);
                panelRt.sizeDelta = new Vector2(330f, 434f);
            }

            StripComponent<Outline>(panelTr.gameObject);
            Image panelImg = panelTr.GetComponent<Image>();
            if (panelImg != null)
            {
                RetroUITheme.ApplySlicedSprite(panelImg, assets.windowBody, RetroUITheme.WindowCream, true);
            }

            // Header matching Menu & BaseWindow Royal Blue 54px TopBar
            Transform headerTr = panelTr.Find("Header");
            if (headerTr != null)
            {
                StripComponent<Outline>(headerTr.gameObject);
                Image hdrImg = headerTr.GetComponent<Image>();
                if (hdrImg != null)
                {
                    RetroUITheme.ApplySlicedSprite(hdrImg, assets.windowTitleBar, RetroUITheme.TitleBlue, true);
                }

                RectTransform hdrRt = headerTr.GetComponent<RectTransform>();
                hdrRt.anchorMin = new Vector2(0f, 1f);
                hdrRt.anchorMax = new Vector2(1f, 1f);
                hdrRt.pivot = new Vector2(0.5f, 1f);
                hdrRt.offsetMin = new Vector2(4f, -58f);
                hdrRt.offsetMax = new Vector2(-4f, -4f);

                Sprite hdrComputerIcon = LoadRetroComputerIcon(assets);
                Transform hdrIconTr = headerTr.Find("HeaderIcon");
                GameObject hdrIconGo = hdrIconTr != null
                    ? hdrIconTr.gameObject
                    : CreateImageObject("HeaderIcon", headerTr, hdrComputerIcon, Color.white, false);
                hdrIconGo.transform.SetAsFirstSibling();
                Image hdrIconImg = hdrIconGo.GetComponent<Image>();
                hdrIconImg.sprite = hdrComputerIcon;
                hdrIconImg.preserveAspect = true;
                hdrIconImg.color = Color.white;
                hdrIconImg.raycastTarget = false;

                RectTransform hdrIconRt = hdrIconGo.GetComponent<RectTransform>();
                hdrIconRt.anchorMin = new Vector2(0f, 0.5f);
                hdrIconRt.anchorMax = new Vector2(0f, 0.5f);
                hdrIconRt.pivot = new Vector2(0f, 0.5f);
                hdrIconRt.anchoredPosition = new Vector2(14f, 0f);
                hdrIconRt.sizeDelta = new Vector2(32f, 32f);

                Transform titleTr = headerTr.Find("Title");
                if (titleTr != null)
                {
                    TMP_Text titleTmp = titleTr.GetComponent<TMP_Text>();
                    if (titleTmp != null)
                    {
                        if (font != null) titleTmp.font = font;
                        titleTmp.fontSize = 20f;
                        titleTmp.fontStyle = FontStyles.Bold;
                        titleTmp.color = RetroUITheme.InkWhite;
                        titleTmp.alignment = TextAlignmentOptions.MidlineLeft;
                        titleTmp.raycastTarget = false;
                    }

                    RectTransform titleRt = titleTr.GetComponent<RectTransform>();
                    titleRt.anchorMin = Vector2.zero;
                    titleRt.anchorMax = Vector2.one;
                    titleRt.pivot = new Vector2(0.5f, 0.5f);
                    titleRt.offsetMin = new Vector2(54f, 4f);
                    titleRt.offsetMax = new Vector2(-14f, -4f);
                }
            }

            // App Cards container
            Transform containerTr = panelTr.Find("ButtonsContainer");
            if (containerTr != null)
            {
                RectTransform cRt = containerTr.GetComponent<RectTransform>();
                cRt.anchorMin = Vector2.zero;
                cRt.anchorMax = Vector2.one;
                cRt.pivot = new Vector2(0.5f, 0.5f);
                cRt.offsetMin = new Vector2(14f, 14f);
                cRt.offsetMax = new Vector2(-14f, -66f);

                VerticalLayoutGroup vlg = containerTr.GetComponent<VerticalLayoutGroup>() ?? containerTr.gameObject.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = 10f;
                vlg.padding = new RectOffset(2, 2, 4, 2);
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;

                Sprite explorerIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/App icons/FolderIcons/Folder_Icon01.png") ?? assets.folderYellow;
                Sprite notepadIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/App icons/Text_File_Icon_T01_Scaled.png");
                Sprite paintIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/App icons/Image_File_Icon_01_Scaled.png");
                Sprite minigamesIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/App icons/Target_Minigame_Icon01_Scaled.png") ?? assets.targetIcon;

                RetroizeStartMenuAppButton(containerTr.Find("btnExplorer"), assets, font, explorerIcon, Color.white, RetroUITheme.CardPastelYellow, DesktopDarkInk);
                RetroizeStartMenuAppButton(containerTr.Find("btnNotepad"), assets, font, notepadIcon, Color.white, RetroUITheme.CardPastelBlue, DesktopDarkInk);
                RetroizeStartMenuAppButton(containerTr.Find("BtnPaint"), assets, font, paintIcon, Color.white, RetroUITheme.CardPastelMint, DesktopDarkInk);
                RetroizeStartMenuAppButton(containerTr.Find("btnMinigames"), assets, font, minigamesIcon, Color.white, RetroUITheme.CardPastelPeach, DesktopDarkInk);
                RetroizeStartMenuAppButton(containerTr.Find("btnStats"), assets, font, assets.goldMedal, Color.white, RetroUITheme.CardPastelLavend, DesktopDarkInk);
                RetroizeStartMenuAppButton(containerTr.Find("btnExit"), assets, font, assets.iconExit, RetroUITheme.InkWhite, RetroUITheme.BtnCoralRed, RetroUITheme.InkWhite);
            }
        }

        private static void RetroizeStartMenuAppButton(
            Transform btnTr,
            RetroThemeAssets assets,
            TMP_FontAsset font,
            Sprite iconSprite,
            Color iconTint,
            Color bgTint,
            Color textTint)
        {
            if (btnTr == null) return;

            StripComponent<Outline>(btnTr.gameObject);
            LayoutElement le = btnTr.GetComponent<LayoutElement>() ?? btnTr.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 60f;
            le.preferredHeight = 60f;

            Image bgImg = btnTr.GetComponent<Image>();
            if (bgImg != null)
            {
                RetroUITheme.ApplySlicedSprite(bgImg, assets.button3D, bgTint, true);
            }

            Button btn = btnTr.GetComponent<Button>();
            if (btn != null)
            {
                btn.transition = Selectable.Transition.ColorTint;
                btn.targetGraphic = bgImg;
                RetroUITheme.ConfigureButtonColors(btn, bgTint);
            }

            Transform iconTr = btnTr.Find("Icon");
            GameObject iconGo = iconTr != null
                ? iconTr.gameObject
                : CreateImageObject("Icon", btnTr, iconSprite, iconTint, false);
            iconGo.transform.SetAsFirstSibling();
            Image iconImg = iconGo.GetComponent<Image>();
            if (iconSprite != null) iconImg.sprite = iconSprite;
            iconImg.type = Image.Type.Simple;
            iconImg.preserveAspect = true;
            iconImg.color = iconTint;
            iconImg.raycastTarget = false;

            RectTransform iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.anchoredPosition = new Vector2(14f, 1f);
            iconRt.sizeDelta = new Vector2(36f, 36f);

            Transform txtTr = btnTr.Find("Text");
            TMP_Text tmp = txtTr != null ? txtTr.GetComponent<TMP_Text>() : btnTr.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
            {
                if (font != null) tmp.font = font;
                tmp.fontSize = 19f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = textTint;
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.overflowMode = TextOverflowModes.Ellipsis;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.raycastTarget = false;

                RectTransform txtRt = tmp.GetComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.pivot = new Vector2(0.5f, 0.5f);
                txtRt.offsetMin = new Vector2(62f, 4f);
                txtRt.offsetMax = new Vector2(-12f, -2f);
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            if (target == null) return;
            FieldInfo fi = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (fi != null)
                fi.SetValue(target, value);
            else
                Debug.LogWarning($"[RetroSceneMaterializer] Missing field '{fieldName}' on {target.GetType().Name}");
        }
    }
}
#endif
