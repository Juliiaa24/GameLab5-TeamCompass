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
                new Vector2(44f, -34f), new Vector2(470f, 156f),
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
                new Vector2(0.5f, 0.5f), new Vector2(0f, -45f), new Vector2(760f, 690f),
                RetroUITheme.TitleBlue, RetroUITheme.WindowCream, assets.computerIcon,
                "Welcome to Your Computer!", showDecorativeWindowControls: true, out TMP_Text windowTitleTmp, out _);
            menuRefs.desktopTitleText = windowTitleTmp;

            // Hero Computer Mascot Banner inside top of Window
            GameObject logoObj = CreateImageObject("MenuLogo", mainWindow.transform, assets.computerIcon ?? assets.mascotLogoNoBg, Color.white, false);
            RectTransform logoRt = logoObj.GetComponent<RectTransform>();
            logoRt.anchorMin = new Vector2(0.5f, 1f); logoRt.anchorMax = new Vector2(0.5f, 1f);
            logoRt.pivot = new Vector2(0.5f, 1f);
            logoRt.anchoredPosition = new Vector2(0f, -72f);
            logoRt.sizeDelta = new Vector2(180f, 148f);
            logoObj.GetComponent<Image>().preserveAspect = true;

            // Space Hint right above Play Button
            TMP_Text playSpaceHint = CreateTmp("PlaySpaceHint", mainWindow.transform, font, "Press SPACE or click Play to start", 17, FontStyles.Bold, RetroUITheme.InkMuted, TextAlignmentOptions.Center);
            RectTransform hintRt = playSpaceHint.GetComponent<RectTransform>();
            hintRt.anchorMin = new Vector2(0.5f, 1f); hintRt.anchorMax = new Vector2(0.5f, 1f);
            hintRt.pivot = new Vector2(0.5f, 1f);
            hintRt.anchoredPosition = new Vector2(0f, -228f);
            hintRt.sizeDelta = new Vector2(640f, 28f);
            menuRefs.playSpaceHintText = playSpaceHint;

            // Prominent Hero Play Button
            Button btnPlay = CreateRetroButton("Btn-Play", mainWindow.transform, assets, font, "Play", assets.iconPlay, RetroUITheme.BtnSunnyGold, RetroUITheme.InkDark, 36, out _);
            RectTransform playRt = btnPlay.GetComponent<RectTransform>();
            playRt.anchorMin = new Vector2(0.5f, 1f); playRt.anchorMax = new Vector2(0.5f, 1f);
            playRt.pivot = new Vector2(0.5f, 1f);
            playRt.anchoredPosition = new Vector2(0f, -264f);
            playRt.sizeDelta = new Vector2(560f, 108f);
            menuRefs.btnPlay = btnPlay;

            // Middle Row: My Medals & Progress + Join Class
            Button btnStats = CreateRetroButton("Btn-Stats", mainWindow.transform, assets, font, "My Medals & Progress", assets.goldMedal, RetroUITheme.BtnSkyBlue, RetroUITheme.InkWhite, 22, out _);
            RectTransform statsRt = btnStats.GetComponent<RectTransform>();
            statsRt.anchorMin = new Vector2(0.5f, 1f); statsRt.anchorMax = new Vector2(0.5f, 1f);
            statsRt.pivot = new Vector2(1f, 1f);
            statsRt.anchoredPosition = new Vector2(-12f, -396f);
            statsRt.sizeDelta = new Vector2(305f, 84f);
            menuRefs.btnStats = btnStats;

            Button btnJoinClass = CreateRetroButton("Btn-JoinClass", mainWindow.transform, assets, font, "Join Class", assets.iconClassroom, RetroUITheme.BtnMintGreen, RetroUITheme.InkWhite, 22, out _);
            RectTransform joinRt = btnJoinClass.GetComponent<RectTransform>();
            joinRt.anchorMin = new Vector2(0.5f, 1f); joinRt.anchorMax = new Vector2(0.5f, 1f);
            joinRt.pivot = new Vector2(0f, 1f);
            joinRt.anchoredPosition = new Vector2(12f, -396f);
            joinRt.sizeDelta = new Vector2(243f, 84f);
            menuRefs.btnJoinClass = btnJoinClass;

            // Bottom Row: Language Pill + Exit Button
            GameObject langContainer = CreateRetroCardWindow("LanguageBar", mainWindow.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(1f, 1f),
                new Vector2(-12f, -504f), new Vector2(305f, 92f),
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

            Button btnExit = CreateRetroButton("Btn-2", mainWindow.transform, assets, font, "Exit", assets.iconExit, RetroUITheme.BtnCoralRed, RetroUITheme.InkWhite, 22, out _);
            RectTransform exitRt = btnExit.GetComponent<RectTransform>();
            exitRt.anchorMin = new Vector2(0.5f, 1f); exitRt.anchorMax = new Vector2(0.5f, 1f);
            exitRt.pivot = new Vector2(0f, 1f);
            exitRt.anchoredPosition = new Vector2(12f, -504f);
            exitRt.sizeDelta = new Vector2(243f, 92f);
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
            hiRt.anchoredPosition = new Vector2(24f, 0f);
            hiRt.sizeDelta = new Vector2(46f, 46f);
            hdrIcon.GetComponent<Image>().preserveAspect = true;

            TMP_Text headerTitle = CreateTmp("Title", header.transform, font, "My Medals & Progress", 30, FontStyles.Bold, RetroUITheme.InkWhite, TextAlignmentOptions.Left);
            RectTransform htRt = headerTitle.GetComponent<RectTransform>();
            htRt.anchorMin = Vector2.zero; htRt.anchorMax = Vector2.one;
            htRt.offsetMin = new Vector2(84f, 6f); htRt.offsetMax = new Vector2(-460f, -6f);

            Button backBtn = CreateRetroButton("Btn-Back", header.transform, assets, font, "Back to Menu", assets.iconExit, RetroUITheme.BtnNeutralCream, RetroUITheme.InkDark, 18, out _);
            RectTransform bbRt = backBtn.GetComponent<RectTransform>();
            bbRt.anchorMin = new Vector2(1f, 0.5f); bbRt.anchorMax = new Vector2(1f, 0.5f);
            bbRt.pivot = new Vector2(1f, 0.5f);
            bbRt.anchoredPosition = new Vector2(-248f, 0f);
            bbRt.sizeDelta = new Vector2(198f, 52f);

            Button retakeBtn = CreateRetroButton("Btn-Retake", header.transform, assets, font, "Retake Assessment", assets.iconPlay, RetroUITheme.BtnSunnyGold, RetroUITheme.InkDark, 18, out _);
            RectTransform rbRt = retakeBtn.GetComponent<RectTransform>();
            rbRt.anchorMin = new Vector2(1f, 0.5f); rbRt.anchorMax = new Vector2(1f, 0.5f);
            rbRt.pivot = new Vector2(1f, 0.5f);
            rbRt.anchoredPosition = new Vector2(-20f, 0f);
            rbRt.sizeDelta = new Vector2(216f, 52f);

            // Medal Showcase Row
            GameObject medalStrip = new GameObject("MedalShowcaseStrip", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            medalStrip.transform.SetParent(canvas.transform, false);
            RectTransform msRt = medalStrip.GetComponent<RectTransform>();
            msRt.anchorMin = new Vector2(0f, 1f); msRt.anchorMax = new Vector2(1f, 1f);
            msRt.pivot = new Vector2(0.5f, 1f);
            msRt.offsetMin = new Vector2(36f, -214f); msRt.offsetMax = new Vector2(-36f, -126f);
            HorizontalLayoutGroup msHlg = medalStrip.GetComponent<HorizontalLayoutGroup>();
            msHlg.spacing = 18f;
            msHlg.childControlWidth = true; msHlg.childControlHeight = true;
            msHlg.childForceExpandWidth = true; msHlg.childForceExpandHeight = true;

            TMP_Text goldTxt = CreateStatMiniCard(medalStrip.transform, assets, font, "Gold", assets.goldMedal, RetroUITheme.CardPastelYellow);
            TMP_Text silverTxt = CreateStatMiniCard(medalStrip.transform, assets, font, "Silver", assets.silverMedal, RetroUITheme.CardWhite);
            TMP_Text bronzeTxt = CreateStatMiniCard(medalStrip.transform, assets, font, "Bronze", assets.bronzeMedal, RetroUITheme.CardPastelPeach);
            TMP_Text levelsTxt = CreateStatMiniCard(medalStrip.transform, assets, font, "Levels", assets.computerIcon, RetroUITheme.CardPastelMint);

            // Two Retro Windows Side-by-Side
            GameObject assessWindow = CreateRetroDesktopWindow("AssessmentCard", canvas.transform, assets, font,
                new Vector2(0.26f, 0.40f), Vector2.zero, new Vector2(880f, 760f),
                RetroUITheme.TitleTeal, RetroUITheme.WindowCream, assets.targetIcon,
                "First Assessment", false, out TMP_Text assessCardTitle, out _);
            RectTransform awRt = assessWindow.GetComponent<RectTransform>();
            awRt.anchorMin = new Vector2(0f, 0f); awRt.anchorMax = new Vector2(0.49f, 1f);
            awRt.offsetMin = new Vector2(36f, 32f); awRt.offsetMax = new Vector2(-10f, -230f);
            assessCardTitle.gameObject.name = "CardTitle";

            TMP_Text assessSub = CreateTmp("CardSubtitle", assessWindow.transform, font, "Most Recent Results", 15, FontStyles.Normal, RetroUITheme.InkMuted, TextAlignmentOptions.MidlineLeft);
            RectTransform asRt = assessSub.GetComponent<RectTransform>();
            asRt.anchorMin = new Vector2(0f, 1f); asRt.anchorMax = new Vector2(1f, 1f);
            asRt.offsetMin = new Vector2(22f, -92f); asRt.offsetMax = new Vector2(-22f, -62f);

            // Summary Box
            GameObject summaryBox = CreateRetroCardWindow("SummaryBox", assessWindow.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, Vector2.zero, assets.cardRounded, RetroUITheme.CardPastelBlue);
            RectTransform sbRt = summaryBox.GetComponent<RectTransform>();
            sbRt.offsetMin = new Vector2(20f, -152f); sbRt.offsetMax = new Vector2(-20f, -96f);

            TextMeshProUGUI summaryTxt = CreateTmp("SummaryText", summaryBox.transform, font, "Completed", 16, FontStyles.Bold, RetroUITheme.InkDark, TextAlignmentOptions.MidlineLeft) as TextMeshProUGUI;
            RectTransform stRt = summaryTxt.GetComponent<RectTransform>();
            stRt.anchorMin = Vector2.zero; stRt.anchorMax = new Vector2(0.72f, 1f);
            stRt.offsetMin = new Vector2(16f, 0f); stRt.offsetMax = Vector2.zero;

            TextMeshProUGUI avgTxt = CreateTmp("AvgScoreText", summaryBox.transform, font, "Average: 0%", 18, FontStyles.Bold, RetroUITheme.TitleBlue, TextAlignmentOptions.MidlineRight) as TextMeshProUGUI;
            RectTransform atRt = avgTxt.GetComponent<RectTransform>();
            atRt.anchorMin = new Vector2(0.72f, 0f); atRt.anchorMax = Vector2.one;
            atRt.offsetMin = Vector2.zero; atRt.offsetMax = new Vector2(-16f, 0f);

            TextMeshProUGUI emptyTxt = CreateTmp("EmptyStateText", assessWindow.transform, font, "No assessment records yet.", 20, FontStyles.Normal, RetroUITheme.InkMuted, TextAlignmentOptions.Center) as TextMeshProUGUI;
            RectTransform etRt = emptyTxt.GetComponent<RectTransform>();
            etRt.anchorMin = Vector2.zero; etRt.anchorMax = Vector2.one;
            etRt.offsetMin = new Vector2(40f, 60f); etRt.offsetMax = new Vector2(-40f, -130f);
            emptyTxt.gameObject.SetActive(false);

            GameObject assessContainer = new GameObject("AssessmentTableContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
            assessContainer.transform.SetParent(assessWindow.transform, false);
            RectTransform acRt = assessContainer.GetComponent<RectTransform>();
            acRt.anchorMin = Vector2.zero; acRt.anchorMax = Vector2.one;
            acRt.offsetMin = new Vector2(20f, 20f); acRt.offsetMax = new Vector2(-20f, -166f);
            VerticalLayoutGroup acVlg = assessContainer.GetComponent<VerticalLayoutGroup>();
            acVlg.spacing = 10f; acVlg.childControlWidth = true; acVlg.childControlHeight = false;
            acVlg.childForceExpandWidth = true; acVlg.childForceExpandHeight = false;

            // Right Skills Window
            GameObject skillsWindow = CreateRetroDesktopWindow("SkillsCard", canvas.transform, assets, font,
                new Vector2(0.74f, 0.40f), Vector2.zero, new Vector2(880f, 760f),
                RetroUITheme.TitleAmber, RetroUITheme.WindowCream, assets.mouseIcon,
                "Current Skill Mastery", false, out TMP_Text skillsCardTitle, out _);
            RectTransform swRt = skillsWindow.GetComponent<RectTransform>();
            swRt.anchorMin = new Vector2(0.51f, 0f); swRt.anchorMax = new Vector2(1f, 1f);
            swRt.offsetMin = new Vector2(10f, 32f); swRt.offsetMax = new Vector2(-36f, -230f);
            skillsCardTitle.gameObject.name = "CardTitle";

            TMP_Text skillsSub = CreateTmp("CardSubtitle", skillsWindow.transform, font, "Progress Bars", 15, FontStyles.Normal, RetroUITheme.InkMuted, TextAlignmentOptions.MidlineLeft);
            RectTransform ssRt = skillsSub.GetComponent<RectTransform>();
            ssRt.anchorMin = new Vector2(0f, 1f); ssRt.anchorMax = new Vector2(1f, 1f);
            ssRt.offsetMin = new Vector2(22f, -92f); ssRt.offsetMax = new Vector2(-22f, -62f);

            GameObject skillsContainer = new GameObject("SkillsTableContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
            skillsContainer.transform.SetParent(skillsWindow.transform, false);
            RectTransform scRt = skillsContainer.GetComponent<RectTransform>();
            scRt.anchorMin = Vector2.zero; scRt.anchorMax = Vector2.one;
            scRt.offsetMin = new Vector2(20f, 20f); scRt.offsetMax = new Vector2(-20f, -104f);
            VerticalLayoutGroup scVlg = skillsContainer.GetComponent<VerticalLayoutGroup>();
            scVlg.spacing = 12f; scVlg.childControlWidth = true; scVlg.childControlHeight = false;
            scVlg.childForceExpandWidth = true; scVlg.childForceExpandHeight = false;

            // Wire StatsSceneController
            SetPrivateField(ctrl, "backToMenuButton", backBtn);
            SetPrivateField(ctrl, "retakeAssessmentButton", retakeBtn);
            SetPrivateField(ctrl, "summaryBox", summaryBox);
            SetPrivateField(ctrl, "summaryText", summaryTxt);
            SetPrivateField(ctrl, "avgScoreText", avgTxt);
            SetPrivateField(ctrl, "emptyStateText", emptyTxt);
            SetPrivateField(ctrl, "assessmentTableContainer", assessContainer.transform);
            SetPrivateField(ctrl, "skillsTableContainer", skillsContainer.transform);
            SetPrivateField(ctrl, "goldMedalsText", goldTxt);
            SetPrivateField(ctrl, "silverMedalsText", silverTxt);
            SetPrivateField(ctrl, "bronzeMedalsText", bronzeTxt);
            SetPrivateField(ctrl, "completedLevelsText", levelsTxt);

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
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460f, 590f),
                RetroUITheme.TitlePlum, RetroUITheme.WindowCream, assets.iconLock,
                "Teacher Mode", false, out TMP_Text titleTmp, out Transform titleBarTr);

            Button closeBtn = CreateCloseIconButton("Btn-ClosePin", titleBarTr, assets);

            TMP_Text promptTmp = CreateTmp("PinPromptText", win.transform, font, "Enter the 4-digit teacher PIN:", 18, FontStyles.Bold, RetroUITheme.InkDark, TextAlignmentOptions.Center);
            RectTransform prRt = promptTmp.GetComponent<RectTransform>();
            prRt.anchorMin = new Vector2(0f, 1f); prRt.anchorMax = new Vector2(1f, 1f);
            prRt.offsetMin = new Vector2(24f, -108f); prRt.offsetMax = new Vector2(-24f, -72f);

            // 4 PIN Dots
            GameObject dotsRow = new GameObject("PinDotsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            dotsRow.transform.SetParent(win.transform, false);
            RectTransform drRt = dotsRow.GetComponent<RectTransform>();
            drRt.anchorMin = new Vector2(0.5f, 1f); drRt.anchorMax = new Vector2(0.5f, 1f);
            drRt.pivot = new Vector2(0.5f, 1f);
            drRt.anchoredPosition = new Vector2(0f, -120f);
            drRt.sizeDelta = new Vector2(260f, 44f);
            HorizontalLayoutGroup drHlg = dotsRow.GetComponent<HorizontalLayoutGroup>();
            drHlg.spacing = 22f; drHlg.childAlignment = TextAnchor.MiddleCenter;
            drHlg.childControlWidth = false; drHlg.childControlHeight = false;

            Image[] dotImgs = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                GameObject dot = CreateImageObject($"PinDot_{i}", dotsRow.transform, assets.badgePill, RetroUITheme.ProgressTrack, false);
                dot.GetComponent<Image>().type = Image.Type.Sliced;
                dot.GetComponent<RectTransform>().sizeDelta = new Vector2(42f, 42f);
                dotImgs[i] = dot.GetComponent<Image>();
            }

            TMP_Text errorTmp = CreateTmp("PinErrorText", win.transform, font, "", 15, FontStyles.Bold, RetroUITheme.DangerCoral, TextAlignmentOptions.Center);
            RectTransform errRt = errorTmp.GetComponent<RectTransform>();
            errRt.anchorMin = new Vector2(0f, 1f); errRt.anchorMax = new Vector2(1f, 1f);
            errRt.offsetMin = new Vector2(24f, -200f); errRt.offsetMax = new Vector2(-24f, -172f);

            // Numeric Keypad (Grid 3 cols)
            GameObject grid = new GameObject("PinKeypadGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            grid.transform.SetParent(win.transform, false);
            RectTransform grRt = grid.GetComponent<RectTransform>();
            grRt.anchorMin = new Vector2(0.5f, 0f); grRt.anchorMax = new Vector2(0.5f, 1f);
            grRt.pivot = new Vector2(0.5f, 0.5f);
            grRt.offsetMin = new Vector2(-175f, 24f); grRt.offsetMax = new Vector2(175f, -210f);

            GridLayoutGroup glg = grid.GetComponent<GridLayoutGroup>();
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 3;
            glg.cellSize = new Vector2(104f, 76f);
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
