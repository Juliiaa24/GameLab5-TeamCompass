/**
 * Author: COMPASS TEAM
 * Date: 08/11/26
 * Description: Forces and maintains a 16:9 aspect ratio across all display resolutions,
 *              window sizes, and platforms. Provides letterboxing / pillarboxing (black bars)
 *              when running on non-16:9 screens (16:10, 4:3, 21:9 ultrawide, etc.) and
 *              configures Canvases to prevent distortion or positioning errors.
 */

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ComputerLearning
{
    [DefaultExecutionOrder(-1000)]
    public class AspectRatioController : MonoBehaviour
    {
        #region Constants
        public const float TARGET_ASPECT = 16f / 9f; // 1.7777778
        public const int REFERENCE_WIDTH = 1920;
        public const int REFERENCE_HEIGHT = 1080;
        #endregion

        #region Singleton
        public static AspectRatioController Instance { get; private set; }
        #endregion

        #region Private Variables
        private Camera letterboxCamera;
        private Camera activeMainCamera;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private FullScreenMode lastFullscreenMode;
        private Rect currentViewportRect = new Rect(0, 0, 1, 1);
        #endregion

        #region Public Properties
        /// <summary>The active 16:9 viewport rect normalized (0..1) on the current window.</summary>
        public Rect CurrentViewportRect => currentViewportRect;

        /// <summary>Active main camera rendering the 16:9 content.</summary>
        public Camera ContentCamera => activeMainCamera != null ? activeMainCamera : Camera.main;
        #endregion

        #region Runtime Initialization
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            EnsureExists();
        }

        public static AspectRatioController EnsureExists()
        {
            if (Instance != null) return Instance;

            Instance = FindAnyObjectByType<AspectRatioController>();
            if (Instance != null) return Instance;

            GameObject go = new GameObject("AspectRatioController");
            Instance = go.AddComponent<AspectRatioController>();
            DontDestroyOnLoad(go);
            return Instance;
        }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }

            CreateLetterboxCamera();
            Force16x9Resolution();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (letterboxCamera != null)
            {
                Destroy(letterboxCamera.gameObject);
            }
        }

        private void Start()
        {
            ApplyAspectRatio();
        }

        private float checkTimer;

        private void LateUpdate()
        {
            checkTimer += Time.unscaledDeltaTime;
            bool resolutionChanged = Screen.width != lastScreenWidth || Screen.height != lastScreenHeight || Screen.fullScreenMode != lastFullscreenMode;
            if (resolutionChanged || checkTimer >= 0.5f)
            {
                checkTimer = 0f;
                ApplyAspectRatio();
            }
        }
        #endregion

        #region Scene Callback
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyAspectRatio();
        }
        #endregion

        #region Resolution & Aspect Enforcement
        /// <summary>
        /// Selects the best 16:9 resolution fitting the user's primary monitor.
        /// </summary>
        public void Force16x9Resolution()
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            Resolution current = Screen.currentResolution;
            float currentAspect = (float)current.width / Mathf.Max(1, current.height);

            int targetW;
            int targetH;

            // If the display itself is already ~16:9, prefer native resolution
            if (Mathf.Abs(currentAspect - TARGET_ASPECT) < 0.05f)
            {
                targetW = current.width;
                targetH = current.height;
            }
            else
            {
                // Fit 16:9 inside the non-16:9 display (e.g., 1920x1200 monitor -> 1920x1080)
                targetW = current.width;
                targetH = Mathf.RoundToInt(targetW / TARGET_ASPECT);

                if (targetH > current.height)
                {
                    targetH = current.height;
                    targetW = Mathf.RoundToInt(targetH * TARGET_ASPECT);
                }

                // Ensure even pixel dimensions
                targetW = (targetW / 2) * 2;
                targetH = (targetH / 2) * 2;
            }

            // Cap at 1920x1080 if smaller monitor or set standard 1080p if display is >= 1080p
            if (targetW <= 0 || targetH <= 0)
            {
                targetW = REFERENCE_WIDTH;
                targetH = REFERENCE_HEIGHT;
            }

#if UNITY_STANDALONE && !UNITY_EDITOR
            if (Screen.width != targetW || Screen.height != targetH)
            {
                Screen.SetResolution(targetW, targetH, Screen.fullScreenMode);
            }
#endif
#endif
        }

        /// <summary>
        /// Creates a persistent background camera that clears the screen buffer to black
        /// behind the 16:9 viewport (creating letterbox / pillarbox bars).
        /// </summary>
        private void CreateLetterboxCamera()
        {
            if (letterboxCamera != null) return;

            GameObject camObj = new GameObject("LetterboxBackgroundCamera");
            camObj.transform.SetParent(transform, false);

            letterboxCamera = camObj.AddComponent<Camera>();
            letterboxCamera.clearFlags = CameraClearFlags.SolidColor;
            letterboxCamera.backgroundColor = Color.black;
            letterboxCamera.cullingMask = 0; // Does not render any geometry
            letterboxCamera.depth = -100; // Always renders first
            letterboxCamera.rect = new Rect(0f, 0f, 1f, 1f);
            letterboxCamera.allowHDR = false;
            letterboxCamera.allowMSAA = false;
        }

        /// <summary>
        /// Recalculates the 16:9 viewport rect and binds scene cameras and canvases.
        /// </summary>
        public void ApplyAspectRatio()
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            lastFullscreenMode = Screen.fullScreenMode;

            if (lastScreenWidth <= 0 || lastScreenHeight <= 0) return;

            float windowAspect = (float)lastScreenWidth / (float)lastScreenHeight;
            float scaleHeight = windowAspect / TARGET_ASPECT;

            if (scaleHeight < 1.0f)
            {
                // Window is taller than 16:9 (e.g. 16:10, 4:3) -> Letterbox (black bars top & bottom)
                float barHeight = (1.0f - scaleHeight) * 0.5f;
                currentViewportRect = new Rect(0f, barHeight, 1.0f, scaleHeight);
            }
            else
            {
                // Window is wider than 16:9 (e.g. 21:9 ultrawide) -> Pillarbox (black bars left & right)
                float scaleWidth = 1.0f / scaleHeight;
                float barWidth = (1.0f - scaleWidth) * 0.5f;
                currentViewportRect = new Rect(barWidth, 0f, scaleWidth, 1.0f);
            }

            ConfigureCameras();
            ConfigureCanvases();
        }

        private void ConfigureCameras()
        {
            if (letterboxCamera != null)
            {
                letterboxCamera.rect = new Rect(0f, 0f, 1f, 1f);
            }

            Camera[] allCameras = Camera.allCameras;
            activeMainCamera = Camera.main;

            if (activeMainCamera == null && allCameras.Length > 0)
            {
                foreach (Camera c in allCameras)
                {
                    if (c != letterboxCamera)
                    {
                        activeMainCamera = c;
                        break;
                    }
                }
            }

            foreach (Camera cam in allCameras)
            {
                if (cam == letterboxCamera) continue;
                cam.rect = currentViewportRect;
            }
        }

        private void ConfigureCanvases()
        {
            Camera cam = activeMainCamera != null ? activeMainCamera : Camera.main;
            if (cam == null) return;

            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (Canvas c in canvases)
            {
                if (c == null) continue;

                // Configure ScreenSpaceCamera so Canvas is strictly bound to the 16:9 camera viewport
                if (c.renderMode != RenderMode.WorldSpace)
                {
                    c.renderMode = RenderMode.ScreenSpaceCamera;
                    c.worldCamera = cam;
                    c.planeDistance = 100f;
                }

                // Ensure CanvasScaler has 1920x1080 reference resolution
                CanvasScaler scaler = c.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(REFERENCE_WIDTH, REFERENCE_HEIGHT);
                    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    scaler.matchWidthOrHeight = 0.5f;
                }
            }
        }
        #endregion
    }
}
