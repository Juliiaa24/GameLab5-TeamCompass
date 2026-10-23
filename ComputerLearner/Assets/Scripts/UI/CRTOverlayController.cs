/**
 * Author: TeamCompass (Diego & AI)
 * Date: 23/10/2026
 * Description: Persistent fullscreen CRT monitor overlay controller.
 *              Instantiates the user-configured CRT prefab (Assets/Prefabs/UI/CRT_GlobalOverlay.prefab)
 *              and accurately applies its serialized inspector values to runtime material instances.
 */
using UnityEngine;
using UnityEngine.UI;

namespace ComputerLearning
{
    public class CRTOverlayController : MonoBehaviour
    {
        public static CRTOverlayController Instance { get; private set; }

        public const string PREFAB_RESOURCE_PATH = "CRT_GlobalOverlay";
        public const string PREFAB_ASSET_PATH = "Assets/Prefabs/UI/CRT_GlobalOverlay.prefab";

        [Header("Prefab UI References")]
        [SerializeField] private Canvas overlayCanvas;
        [SerializeField] private RawImage overlayImage;
        [SerializeField] private Material crtMaterial;

        [Header("General Settings")]
        [SerializeField] private bool showOverlay = true;
        [Range(0f, 1f)] [SerializeField] private float overlayAlpha = 1f;

        [Header("Curvature")]
        [Tooltip("Horizontal scanline curvature intensity.")]
        [Range(0f, 0.25f)] [SerializeField] private float curvatureX = 0.08f;
        [Tooltip("Vertical scanline curvature intensity.")]
        [Range(0f, 0.25f)] [SerializeField] private float curvatureY = 0.06f;

        [Header("Rolling Scan Beam (Line)")]
        [Tooltip("Speed of the upward-moving scan beam (lower is slower/smoother).")]
        [Range(0.005f, 0.2f)] [SerializeField] private float scanBeamSpeed = 0.035f;
        [Tooltip("Thickness of the rolling beam.")]
        [Range(0.005f, 0.08f)] [SerializeField] private float scanBeamWidth = 0.018f;
        [Tooltip("Brightness glow of the beam.")]
        [Range(0f, 1f)] [SerializeField] private float scanBeamIntensity = 0.12f;

        [Header("Scanlines & Atmosphere")]
        [SerializeField] private float scanlineCount = 540f;
        [Range(0f, 1f)] [SerializeField] private float scanlineIntensity = 0.18f;
        [Range(0f, 1f)] [SerializeField] private float vignetteStrength = 0.05f;
        [Range(0f, 0.05f)] [SerializeField] private float flickerIntensity = 0.003f;

        private Material runtimeMaterial;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            EnsureExists();
        }

        public static CRTOverlayController EnsureExists()
        {
            if (Instance != null) return Instance;

            Instance = FindAnyObjectByType<CRTOverlayController>();
            if (Instance != null) return Instance;

            GameObject prefab = null;

#if UNITY_EDITOR
            // 1. In Editor, ALWAYS prioritize the user's working prefab in Assets/Prefabs/UI/
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_ASSET_PATH);
#endif

            // 2. Load from Resources (for standalone player builds or fallback)
            if (prefab == null)
            {
                prefab = Resources.Load<GameObject>(PREFAB_RESOURCE_PATH);
            }
            if (prefab == null)
            {
                prefab = Resources.Load<GameObject>($"Prefabs/{PREFAB_RESOURCE_PATH}");
            }
            if (prefab == null)
            {
                prefab = Resources.Load<GameObject>($"Prefabs/UI/{PREFAB_RESOURCE_PATH}");
            }

            if (prefab != null)
            {
                GameObject instanceObj = Instantiate(prefab);
                instanceObj.name = "CRT_GlobalOverlay";
                Instance = instanceObj.GetComponent<CRTOverlayController>();
                DontDestroyOnLoad(instanceObj);
                return Instance;
            }

            Debug.LogWarning($"[CRTOverlayController] Prefab '{PREFAB_ASSET_PATH}' not found.");
            return null;
        }

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

            ResolveReferences();
            InitMaterialInstance();
            ApplyMaterialProperties();
        }

        private void OnDestroy()
        {
            if (runtimeMaterial != null)
            {
                Destroy(runtimeMaterial);
                runtimeMaterial = null;
            }
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void ResolveReferences()
        {
            if (overlayCanvas == null) overlayCanvas = GetComponent<Canvas>();
            if (overlayImage == null) overlayImage = GetComponentInChildren<RawImage>(true);

            // Ensure this overlay NEVER blocks raycasts or input under any circumstances
            CanvasGroup group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster != null) Destroy(raycaster);

            if (overlayImage != null && crtMaterial == null)
            {
                crtMaterial = overlayImage.material;
            }

            if (overlayImage != null)
            {
                overlayImage.raycastTarget = false;
                overlayImage.gameObject.SetActive(showOverlay);
            }
        }

        private void InitMaterialInstance()
        {
            if (overlayImage == null) return;

            Material baseMat = crtMaterial != null ? crtMaterial : overlayImage.material;
            if (baseMat == null)
            {
                Shader shader = Shader.Find("UI/CRT_Scanlines");
                if (shader != null) baseMat = new Material(shader);
            }

            if (baseMat != null)
            {
                // Create a runtime clone so material modifications apply directly to this renderer
                // without modifying the project asset on disk
                runtimeMaterial = new Material(baseMat);
                runtimeMaterial.name = "CRT_Scanlines_Runtime";
                overlayImage.material = runtimeMaterial;
            }
        }

        public void ApplyMaterialProperties()
        {
            if (runtimeMaterial == null && overlayImage != null)
            {
                InitMaterialInstance();
            }

            Material mat = runtimeMaterial != null ? runtimeMaterial : (overlayImage != null ? overlayImage.material : crtMaterial);
            if (mat == null) return;

            mat.SetFloat("_CurvatureX", curvatureX);
            mat.SetFloat("_CurvatureY", curvatureY);

            mat.SetFloat("_ScanBeamSpeed", scanBeamSpeed);
            mat.SetFloat("_ScanBeamWidth", scanBeamWidth);
            mat.SetFloat("_ScanBeamIntensity", scanBeamIntensity);

            mat.SetFloat("_ScanlineCount", scanlineCount);
            mat.SetFloat("_ScanlineIntensity", scanlineIntensity);
            mat.SetFloat("_VignetteStrength", vignetteStrength);
            mat.SetFloat("_FlickerIntensity", flickerIntensity);

            if (overlayImage != null)
            {
                overlayImage.color = new Color(1f, 1f, 1f, overlayAlpha);
                overlayImage.gameObject.SetActive(showOverlay);
            }
        }

        private void Update()
        {
            // Allow real-time tweaking in Editor play mode
#if UNITY_EDITOR
            ApplyMaterialProperties();
#endif
        }

        private void OnValidate()
        {
            ApplyMaterialProperties();
#if UNITY_EDITOR
            SyncResourcesPrefab();
#endif
        }

#if UNITY_EDITOR
        private static void SyncResourcesPrefab()
        {
            if (Application.isPlaying) return;
            try
            {
                string src = "Assets/Prefabs/UI/CRT_GlobalOverlay.prefab";
                string dst = "Assets/Resources/CRT_GlobalOverlay.prefab";
                if (System.IO.File.Exists(src) && System.IO.Directory.Exists("Assets/Resources"))
                {
                    System.IO.File.Copy(src, dst, true);
                }
            }
            catch {}
        }
#endif

        public void SetOverlayVisible(bool visible)
        {
            showOverlay = visible;
            if (overlayImage != null)
            {
                overlayImage.gameObject.SetActive(visible);
            }
        }
    }
}
