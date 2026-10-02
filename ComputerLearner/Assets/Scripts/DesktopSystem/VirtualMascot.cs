using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace ComputerLearning
{
    public class VirtualMascot : MonoBehaviour
    {
        public static VirtualMascot Instance { get; private set; }

        [Header("References")]
        public RectTransform mascotRect;
        public GameObject speechBubble;
        public Text speechText;

        [Header("Settings")]
        public float followSpeed = 6f;
        [Tooltip("The separation of the speech bubble from the mascot")]
        public Vector2 speechBubbleOffset = new Vector2(80f, -40f);
        [Tooltip("Should the mascot automatically flip its graphic if it is on the right side of the screen?")]
        public bool autoFlipMascot = true;

        private RectTransform targetRect;
        private float floatTimer;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (mascotRect == null) mascotRect = GetComponent<RectTransform>();

            // Make sure the mascot and its speech bubble NEVER block mouse clicks!
            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        private void Update()
        {
            if (targetRect != null && targetRect.gameObject.activeInHierarchy)
            {
                // Get the true visual center of the target, regardless of where its pivot is set
                Vector3[] corners = new Vector3[4];
                targetRect.GetWorldCorners(corners);
                Vector3 basePos = (corners[0] + corners[2]) / 2f;
                
                // Float animation (reduced so the tip stays on target)
                floatTimer += Time.deltaTime;
                float floatOffset = Mathf.Sin(floatTimer * 4f) * 5f; 
                
                Canvas canvas = GetComponentInParent<Canvas>();
                float scale = canvas != null ? canvas.scaleFactor : 1f;
                
                // IGNORE currentOffset from old code, because now we act as a direct cursor
                Vector3 finalTargetPos = basePos + new Vector3(0, floatOffset * scale, 0);

                // Clamp to screen bounds to prevent going completely off-screen, 
                // but keep margin very small so the tip can reach top/right buttons like the 'X'.
                float margin = 5f * scale; 
                finalTargetPos.x = Mathf.Clamp(finalTargetPos.x, margin, Screen.width - margin);
                finalTargetPos.y = Mathf.Clamp(finalTargetPos.y, margin, Screen.height - margin);

                mascotRect.position = Vector3.Lerp(mascotRect.position, finalTargetPos, Time.deltaTime * followSpeed);

                // Flip the mascot horizontally if it's on the right side of the screen
                // We use Camera.main if possible, or screen pixels, to robustly determine side.
                bool isOnRightSide = false;
                if (autoFlipMascot)
                {
                    Camera cam = canvas != null ? canvas.worldCamera : null;
                    Vector3 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, mascotRect.position);
                    isOnRightSide = screenPoint.x > Screen.width * 0.5f;
                }

                float flipScale = isOnRightSide ? -1f : 1f;
                mascotRect.localScale = new Vector3(flipScale, 1f, 1f);

                if (speechBubble != null)
                {
                    // Counter-flip the speech bubble so the text remains readable
                    speechBubble.transform.localScale = new Vector3(flipScale, 1f, 1f);
                    
                    RectTransform bubbleRect = speechBubble.GetComponent<RectTransform>();
                    
                    // Use the exposed variable instead of magic numbers
                    Vector2 finalBubblePos = new Vector2(speechBubbleOffset.x, speechBubbleOffset.y);

                    if (isOnRightSide)
                    {
                        // Mascot body goes LEFT. Speech bubble is pushed LEFT.
                        bubbleRect.pivot = new Vector2(1f, 1f);
                    }
                    else
                    {
                        // Mascot body goes RIGHT. Speech bubble is pushed RIGHT.
                        bubbleRect.pivot = new Vector2(0f, 1f);
                    }
                    
                    // Apply position temporarily to calculate world corners
                    bubbleRect.anchoredPosition = finalBubblePos;
                    
                    // Force a layout update so the ContentSizeFitter calculates the correct size based on text
                    UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(bubbleRect);

                    // Clamp vertically to screen bounds
                    Vector3[] bubbleCorners = new Vector3[4];
                    bubbleRect.GetWorldCorners(bubbleCorners);
                    
                    // Convert world corners to screen space to check bounds
                    Camera cam = canvas != null ? canvas.worldCamera : null;
                    Vector3 bottomEdge = RectTransformUtility.WorldToScreenPoint(cam, bubbleCorners[0]);
                    Vector3 topEdge = RectTransformUtility.WorldToScreenPoint(cam, bubbleCorners[1]);
                    
                    float marginY = 10f;
                    float currentScale = canvas != null ? canvas.scaleFactor : 1f;
                    
                    // If bubble goes below screen
                    if (bottomEdge.y < marginY)
                    {
                        // Shift it up by the difference
                        float difference = (marginY - bottomEdge.y) / currentScale;
                        finalBubblePos.y += difference;
                    }
                    // If bubble goes above screen
                    else if (topEdge.y > Screen.height - marginY)
                    {
                        // Shift it down by the difference
                        float difference = (topEdge.y - (Screen.height - marginY)) / currentScale;
                        finalBubblePos.y -= difference;
                    }
                    
                    bubbleRect.anchoredPosition = finalBubblePos;
                }
            }
            else if (targetRect != null && !targetRect.gameObject.activeInHierarchy)
            {
                // If target was destroyed or hidden, hide mascot
                Hide();
            }
        }

        public static void Show(string message, RectTransform target, Vector2? offset = null)
        {
            if (Instance == null)
            {
                // Busca en la escena aunque esté desactivado
                Instance = Object.FindFirstObjectByType<VirtualMascot>(FindObjectsInactive.Include);
                
                if (Instance == null)
                {
                    Debug.LogWarning("[VirtualMascot] Mascot not found in the scene! Ensure DesktopManager has spawned it.");
                    return;
                }
            }
            if (Instance != null) Instance.ShowMessage(message, target, offset);
        }

        public static void HideMascot()
        {
            if (Instance != null) Instance.Hide();
        }

        public static void HideMascotIfTargeting(Transform potentialParent)
        {
            if (Instance != null && Instance.targetRect != null)
            {
                // If the current target is the window itself, or a child of the window (like the X button)
                if (Instance.targetRect.IsChildOf(potentialParent) || Instance.targetRect == potentialParent)
                {
                    Instance.Hide();
                }
            }
        }

        public void ShowMessage(string message, RectTransform target, Vector2? offset = null)
        {
            gameObject.SetActive(true);
            targetRect = target;
            // The offset parameter is now ignored because the mascot acts as a direct cursor

            if (speechBubble != null && speechText != null)
            {
                bool hasMessage = !string.IsNullOrEmpty(message);
                speechBubble.SetActive(hasMessage);
                speechText.text = message;
            }
            
            transform.SetAsLastSibling(); 
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            targetRect = null;
        }
    }
}
