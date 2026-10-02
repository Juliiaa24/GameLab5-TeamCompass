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
        public Vector2 defaultOffset = new Vector2(150, -100);

        private RectTransform targetRect;
        private Vector2 currentOffset;
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
        }

        private void Update()
        {
            if (targetRect != null && targetRect.gameObject.activeInHierarchy)
            {
                Vector3 basePos = targetRect.position;
                
                // Float animation
                floatTimer += Time.deltaTime;
                float floatOffset = Mathf.Sin(floatTimer * 4f) * 10f; 
                
                Canvas canvas = GetComponentInParent<Canvas>();
                float scale = canvas != null ? canvas.scaleFactor : 1f;
                
                Vector3 finalTargetPos = basePos + new Vector3(currentOffset.x * scale, (currentOffset.y + floatOffset) * scale, 0);

                // Clamp to screen bounds to prevent going off-screen
                float margin = 100f * scale; // Keep some margin from the edges
                finalTargetPos.x = Mathf.Clamp(finalTargetPos.x, margin, Screen.width - margin);
                finalTargetPos.y = Mathf.Clamp(finalTargetPos.y, margin, Screen.height - margin);

                mascotRect.position = Vector3.Lerp(mascotRect.position, finalTargetPos, Time.deltaTime * followSpeed);

                // Auto-flip speech bubble if too close to right edge of screen
                if (speechBubble != null)
                {
                    RectTransform bubbleRect = speechBubble.GetComponent<RectTransform>();
                    if (mascotRect.position.x > Screen.width * 0.6f)
                    {
                        // Flip to left
                        bubbleRect.pivot = new Vector2(1, 0.5f);
                        bubbleRect.anchoredPosition = new Vector2(-60, 50);
                    }
                    else
                    {
                        // Default to right
                        bubbleRect.pivot = new Vector2(0, 0.5f);
                        bubbleRect.anchoredPosition = new Vector2(60, 50);
                    }
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
                Canvas canvas = target.GetComponentInParent<Canvas>();
                if (canvas != null) CreateDefaultMascot(canvas.transform);
                else return;
            }
            Instance.ShowMessage(message, target, offset);
        }

        public static void HideMascot()
        {
            if (Instance != null) Instance.Hide();
        }

        private static void CreateDefaultMascot(Transform parentCanvas)
        {
            GameObject mascotObj = new GameObject("VirtualMascot", typeof(RectTransform), typeof(VirtualMascot));
            mascotObj.transform.SetParent(parentCanvas, false);
            VirtualMascot mascot = mascotObj.GetComponent<VirtualMascot>();
            mascot.mascotRect = mascotObj.GetComponent<RectTransform>();
            
            // Mascot Image (cyan placeholder)
            GameObject imgObj = new GameObject("MascotImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imgObj.transform.SetParent(mascotObj.transform, false);
            imgObj.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 100);
            Image img = imgObj.GetComponent<Image>();
            img.color = new Color(0.2f, 0.8f, 0.9f); 
            
            // Speech Bubble
            GameObject bubbleObj = new GameObject("SpeechBubble", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bubbleObj.transform.SetParent(mascotObj.transform, false);
            RectTransform bubbleRect = bubbleObj.GetComponent<RectTransform>();
            bubbleRect.pivot = new Vector2(0, 0.5f);
            bubbleRect.anchoredPosition = new Vector2(60, 50);
            bubbleRect.sizeDelta = new Vector2(450, 160); // Increased width and height for readability
            bubbleObj.GetComponent<Image>().color = Color.white;
            mascot.speechBubble = bubbleObj;
            
            // Bubble Text
            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObj.transform.SetParent(bubbleObj.transform, false);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(20, 20); textRect.offsetMax = new Vector2(-20, -20);
            Text txt = textObj.GetComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null) txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.color = Color.black;
            txt.fontSize = 28; // Much bigger font
            txt.alignment = TextAnchor.MiddleCenter;
            mascot.speechText = txt;

            Instance = mascot;
        }

        public void ShowMessage(string message, RectTransform target, Vector2? offset = null)
        {
            gameObject.SetActive(true);
            targetRect = target;
            currentOffset = offset ?? defaultOffset;

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
