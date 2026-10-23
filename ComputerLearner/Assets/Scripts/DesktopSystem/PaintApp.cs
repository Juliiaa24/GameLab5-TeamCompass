/**
 * Author: Diego
 * Date: 16/10/2026
 * Description: MS Paint-style drawing application for creating, editing, viewing, and saving .png images
 *              using Inspector-configured UI references on PaintContent.prefab.
 */
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace ComputerLearning
{
    public class PaintApp : MonoBehaviour
    {
        #region Enums
        public enum PaintTool
        {
            Brush,
            Eraser,
            Fill
        }
        #endregion

        #region Public Variables
        public ExplorerItemData CurrentFileItem => currentFileItem;
        public static event Action OnPaintUndo;
        public static event Action OnPaintSaveShortcut;
        public static event Action OnStrokeDrawn;
        #endregion

        #region Private Variables
        [Header("Canvas Settings")]
        [SerializeField] private int textureWidth = 640;
        [SerializeField] private int textureHeight = 400;

        [Header("Canvas UI References")]
        [SerializeField] private RawImage canvasDisplay;
        [SerializeField] private RectTransform canvasRect;

        [Header("Toolbar Action Buttons")]
        [SerializeField] private Button btnSave;
        [SerializeField] private Button btnSaveAs;
        [SerializeField] private Button btnClear;

        [Header("Drawing Tool Buttons")]
        [SerializeField] private Button btnBrush;
        [SerializeField] private Button btnEraser;
        [SerializeField] private Button btnFill;

        [Header("Brush Size Buttons")]
        [SerializeField] private Button btnSizeS;
        [SerializeField] private Button btnSizeM;
        [SerializeField] private Button btnSizeL;

        [Header("Color Palette & Status")]
        [SerializeField] private Image activeColorPreview;
        [SerializeField] private Button[] swatchButtons;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI feedbackText;

        [Header("Visual State Colors")]
        [SerializeField] private Color activeToolBgColor = new Color(0.80f, 0.90f, 1.00f, 1f);
        [SerializeField] private Color normalToolBgColor = Color.white;
        [SerializeField] private Color selectedSwatchBorderColor = new Color(0f, 0.45f, 0.85f, 1f);
        [SerializeField] private Color normalSwatchBorderColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);

        private Texture2D drawingTexture;
        private Color32[] clearBuffer;

        private PaintTool activeTool = PaintTool.Brush;
        private Color activeColor = new Color(0.10f, 0.10f, 0.12f, 1f);
        private int brushRadius = 4;

        [NonSerialized] private ExplorerItemData currentFileItem;
        [NonSerialized] private ExplorerItemData targetSaveFolder;
        private Window parentWindow;
        private bool listenersBound;
        private readonly Stack<Color32[]> undoHistory = new Stack<Color32[]>();
        #endregion

        #region Unity Methods
        private void Awake()
        {
            parentWindow = GetComponentInParent<Window>();
            EnsureTexture();
            BindUIListeners();
        }

        private void Start()
        {
            if (parentWindow == null) parentWindow = GetComponentInParent<Window>();
            EnsureTexture();
            BindUIListeners();
            UpdateToolHighlight();
            UpdateStatusText();
        }

        private void Update()
        {
            if (parentWindow == null) parentWindow = GetComponentInParent<Window>();

            bool isAppFocused = parentWindow != null ? parentWindow.IsFocused : true;
            if (!isAppFocused) return;

            bool isCtrl = false;
            bool isZ = false;
            bool isS = false;

            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                isCtrl = kb.ctrlKey.isPressed;
                isZ = kb.zKey.wasPressedThisFrame;
                isS = kb.sKey.wasPressedThisFrame;
            }
            else
            {
                isCtrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                isZ = Input.GetKeyDown(KeyCode.Z);
                isS = Input.GetKeyDown(KeyCode.S);
            }

            if (!isCtrl) return;

            if (isZ)
            {
                HandleUndoShortcut();
            }
            else if (isS)
            {
                HandleSaveShortcut();
            }
        }

        private void OnDestroy()
        {
            if (drawingTexture != null)
            {
                Destroy(drawingTexture);
                drawingTexture = null;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Loads an existing .png ExplorerItemData into Paint for viewing and editing.
        /// </summary>
        public void LoadImageFile(ExplorerItemData fileItem, Window window = null, ExplorerItemData folder = null)
        {
            EnsureTexture();
            BindUIListeners();

            currentFileItem = fileItem;
            if (folder != null) targetSaveFolder = folder;
            if (window != null) parentWindow = window;
            if (parentWindow == null) parentWindow = GetComponentInParent<Window>();

            if (parentWindow != null && fileItem != null)
            {
                parentWindow.setTitle($"Paint - {fileItem.name}");
            }

            if (fileItem != null)
            {
                Sprite sourceSprite = fileItem.imageContent != null ? fileItem.imageContent : fileItem.icon;
                if (sourceSprite != null && sourceSprite.texture != null && sourceSprite != FileExplorerApp.GetImageFileSprite())
                {
                    BlitSpriteToCanvas(sourceSprite);
                }
                else
                {
                    ClearCanvas();
                }
            }

            UpdateStatusText();
        }

        public void SetTargetSaveFolder(ExplorerItemData folder)
        {
            targetSaveFolder = folder;
        }

        public void PushUndo()
        {
            EnsureTexture();
            if (drawingTexture != null)
            {
                Color32[] snapshot = drawingTexture.GetPixels32();
                undoHistory.Push((Color32[])snapshot.Clone());
            }
        }

        public void HandleUndoShortcut()
        {
            if (undoHistory.Count > 0)
            {
                EnsureTexture();
                Color32[] prev = undoHistory.Pop();
                drawingTexture.SetPixels32(prev);
                drawingTexture.Apply();
                ShowFeedback("Undo!");
                FeedbackManager.PlaySuccess();
                OnPaintUndo?.Invoke();
            }
            else
            {
                ShowFeedback("Nothing to undo");
            }
        }

        public void HandleSaveShortcut()
        {
            OnSaveClicked();
            OnPaintSaveShortcut?.Invoke();
            FeedbackManager.PlayClick();
        }

        public void ClearCanvas()
        {
            PushUndo();
            EnsureTexture();
            drawingTexture.SetPixels32(clearBuffer);
            drawingTexture.Apply();
            ShowFeedback("Canvas cleared");
        }

        public void OnSaveClicked()
        {
            if (currentFileItem != null && !string.IsNullOrWhiteSpace(currentFileItem.name))
            {
                SaveImageWithName(currentFileItem.name, targetSaveFolder);
            }
            else
            {
                OnSaveAsClicked();
            }
        }

        public void OnSaveAsClicked()
        {
            string defaultName = currentFileItem != null && !string.IsNullOrWhiteSpace(currentFileItem.name)
                ? currentFileItem.name
                : "New Drawing.png";

            ExplorerItemData startFolder = targetSaveFolder ?? FileExplorerApp.FindOrCreateSystemFolder("Pictures", true);

            FileExplorerApp.OpenSaveDialog(
                "Save As - Paint",
                defaultName,
                ".png",
                startFolder,
                (enteredName, chosenFolder) =>
                {
                    targetSaveFolder = chosenFolder;
                    SaveImageWithName(enteredName, chosenFolder);
                }
            );
        }
        #endregion

        #region Drawing Operations
        internal void HandleCanvasPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (!TryGetPixelCoord(eventData, out int px, out int py)) return;

            PushUndo();
            OnStrokeDrawn?.Invoke();

            if (activeTool == PaintTool.Fill)
            {
                FloodFill(px, py, activeColor);
                drawingTexture.Apply();
            }
            else
            {
                Color drawCol = activeTool == PaintTool.Eraser ? Color.white : activeColor;
                int rad = activeTool == PaintTool.Eraser ? Mathf.Max(brushRadius * 2, 8) : brushRadius;
                DrawCircle(px, py, rad, drawCol);
                drawingTexture.Apply();
            }
        }

        internal void HandleCanvasDrag(Vector2 prevScreenPos, PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (activeTool == PaintTool.Fill) return;

            bool hasPrev = TryGetPixelCoordFromScreen(prevScreenPos, eventData.pressEventCamera, out int x0, out int y0);
            bool hasCurr = TryGetPixelCoord(eventData, out int x1, out int y1);

            if (!hasCurr) return;
            if (!hasPrev)
            {
                x0 = x1;
                y0 = y1;
            }

            Color drawCol = activeTool == PaintTool.Eraser ? Color.white : activeColor;
            int rad = activeTool == PaintTool.Eraser ? Mathf.Max(brushRadius * 2, 8) : brushRadius;

            DrawLine(x0, y0, x1, y1, rad, drawCol);
            drawingTexture.Apply();
        }

        private bool TryGetPixelCoord(PointerEventData eventData, out int px, out int py)
        {
            return TryGetPixelCoordFromScreen(eventData.position, eventData.pressEventCamera, out px, out py);
        }

        private bool TryGetPixelCoordFromScreen(Vector2 screenPos, Camera cam, out int px, out int py)
        {
            px = 0;
            py = 0;
            if (canvasRect == null && canvasDisplay != null) canvasRect = canvasDisplay.rectTransform;
            if (canvasRect == null || drawingTexture == null) return false;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, cam, out Vector2 localPos))
                return false;

            Rect r = canvasRect.rect;
            if (r.width <= 0f || r.height <= 0f) return false;

            float normX = (localPos.x - r.xMin) / r.width;
            float normY = (localPos.y - r.yMin) / r.height;

            px = Mathf.Clamp(Mathf.RoundToInt(normX * (textureWidth - 1)), 0, textureWidth - 1);
            py = Mathf.Clamp(Mathf.RoundToInt(normY * (textureHeight - 1)), 0, textureHeight - 1);
            return true;
        }

        private void DrawLine(int x0, int y0, int x1, int y1, int radius, Color color)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                DrawCircle(x0, y0, radius, color);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        private void DrawCircle(int cx, int cy, int radius, Color color)
        {
            int rSq = radius * radius;
            int minX = Mathf.Max(0, cx - radius);
            int maxX = Mathf.Min(textureWidth - 1, cx + radius);
            int minY = Mathf.Max(0, cy - radius);
            int maxY = Mathf.Min(textureHeight - 1, cy + radius);

            for (int y = minY; y <= maxY; y++)
            {
                int dy = y - cy;
                for (int x = minX; x <= maxX; x++)
                {
                    int dx = x - cx;
                    if (dx * dx + dy * dy <= rSq)
                    {
                        drawingTexture.SetPixel(x, y, color);
                    }
                }
            }
        }

        private void FloodFill(int startX, int startY, Color fillColor)
        {
            Color targetColor = drawingTexture.GetPixel(startX, startY);
            if (ColorsSimilar(targetColor, fillColor)) return;

            Color[] pixels = drawingTexture.GetPixels();
            Queue<int> queue = new Queue<int>();
            bool[] visited = new bool[textureWidth * textureHeight];

            int startIdx = startY * textureWidth + startX;
            queue.Enqueue(startIdx);
            visited[startIdx] = true;

            int processed = 0;
            int maxPixels = textureWidth * textureHeight;

            while (queue.Count > 0 && processed < maxPixels)
            {
                int idx = queue.Dequeue();
                processed++;
                pixels[idx] = fillColor;

                int x = idx % textureWidth;
                int y = idx / textureWidth;

                TryEnqueueNeighbor(x - 1, y, targetColor, pixels, visited, queue);
                TryEnqueueNeighbor(x + 1, y, targetColor, pixels, visited, queue);
                TryEnqueueNeighbor(x, y - 1, targetColor, pixels, visited, queue);
                TryEnqueueNeighbor(x, y + 1, targetColor, pixels, visited, queue);
            }

            drawingTexture.SetPixels(pixels);
        }

        private void TryEnqueueNeighbor(int x, int y, Color targetColor, Color[] pixels, bool[] visited, Queue<int> queue)
        {
            if (x < 0 || x >= textureWidth || y < 0 || y >= textureHeight) return;
            int idx = y * textureWidth + x;
            if (visited[idx]) return;
            if (ColorsSimilar(pixels[idx], targetColor))
            {
                visited[idx] = true;
                queue.Enqueue(idx);
            }
        }

        private bool ColorsSimilar(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.05f &&
                   Mathf.Abs(a.g - b.g) < 0.05f &&
                   Mathf.Abs(a.b - b.b) < 0.05f;
        }
        #endregion

        #region Saving & Blitting
        private void SaveImageWithName(string enteredName, ExplorerItemData destinationFolder = null)
        {
            if (destinationFolder == null) destinationFolder = targetSaveFolder;

            if (string.IsNullOrWhiteSpace(enteredName))
            {
                enteredName = "New Drawing.png";
            }

            enteredName = enteredName.Trim();
            if (!enteredName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                enteredName += ".png";
            }

            EnsureTexture();

            Texture2D savedTex = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            savedTex.filterMode = FilterMode.Bilinear;
            savedTex.SetPixels32(drawingTexture.GetPixels32());
            savedTex.Apply();

            byte[] pngBytes = savedTex.EncodeToPNG();
            if (pngBytes != null && pngBytes.Length > 0)
            {
                savedTex.LoadImage(pngBytes);
            }

            Sprite savedSprite = Sprite.Create(
                savedTex,
                new Rect(0, 0, savedTex.width, savedTex.height),
                new Vector2(0.5f, 0.5f),
                100f
            );

            if (currentFileItem != null && currentFileItem.name.Equals(enteredName, StringComparison.OrdinalIgnoreCase) &&
                (destinationFolder == null || (destinationFolder.children != null && destinationFolder.children.Contains(currentFileItem))))
            {
                currentFileItem.imageContent = savedSprite;
                currentFileItem.icon = FileExplorerApp.GetImageFileSprite();
                FileExplorerApp.NotifyFileSystemChanged();
                ShowFeedback($"Saved: '{currentFileItem.name}'");
            }
            else
            {
                ExplorerItemData savedItem = FileExplorerApp.SaveOrCreateImageDocument(enteredName, savedSprite, destinationFolder);
                if (savedItem != null)
                {
                    currentFileItem = savedItem;
                    targetSaveFolder = destinationFolder;
                    if (parentWindow == null) parentWindow = GetComponentInParent<Window>();
                    if (parentWindow != null)
                    {
                        parentWindow.setTitle($"Paint - {savedItem.name}");
                    }
                    ShowFeedback($"Saved as '{savedItem.name}' in {(destinationFolder != null ? destinationFolder.name : "Pictures")}");
                }
            }

            UpdateStatusText();
        }

        private void BlitSpriteToCanvas(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null) return;
            EnsureTexture();

            Texture2D srcTex = sprite.texture;
            RenderTexture rt = RenderTexture.GetTemporary(textureWidth, textureHeight, 0, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;

            try
            {
                RenderTexture.active = rt;
                GL.Clear(true, true, Color.white);
                Graphics.Blit(srcTex, rt);

                drawingTexture.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0);
                drawingTexture.Apply();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PaintApp] Could not blit texture: {e.Message}");
                ClearCanvas();
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
            }
        }
        #endregion

        #region UI Binding & State
        private void BindUIListeners()
        {
            if (canvasDisplay != null)
            {
                canvasDisplay.texture = drawingTexture;
                if (canvasRect == null) canvasRect = canvasDisplay.rectTransform;

                PaintCanvasInputRelay relay = canvasDisplay.GetComponent<PaintCanvasInputRelay>();
                if (relay != null) relay.owner = this;
            }

            if (listenersBound) return;
            listenersBound = true;

            if (btnSave != null) btnSave.onClick.AddListener(OnSaveClicked);
            if (btnSaveAs != null) btnSaveAs.onClick.AddListener(OnSaveAsClicked);
            if (btnClear != null) btnClear.onClick.AddListener(ClearCanvas);

            if (btnBrush != null) btnBrush.onClick.AddListener(() => SelectTool(PaintTool.Brush));
            if (btnEraser != null) btnEraser.onClick.AddListener(() => SelectTool(PaintTool.Eraser));
            if (btnFill != null) btnFill.onClick.AddListener(() => SelectTool(PaintTool.Fill));

            if (btnSizeS != null) btnSizeS.onClick.AddListener(() => SelectBrushSize(2));
            if (btnSizeM != null) btnSizeM.onClick.AddListener(() => SelectBrushSize(4));
            if (btnSizeL != null) btnSizeL.onClick.AddListener(() => SelectBrushSize(9));

            if (swatchButtons != null)
            {
                for (int i = 0; i < swatchButtons.Length; i++)
                {
                    int idx = i;
                    Button swBtn = swatchButtons[i];
                    if (swBtn == null) continue;
                    Image swImg = swBtn.GetComponent<Image>();
                    Color col = swImg != null ? swImg.color : Color.black;
                    swBtn.onClick.AddListener(() => SelectColor(col, idx));
                }
            }

            if (feedbackText != null) feedbackText.gameObject.SetActive(false);
        }

        private void EnsureTexture()
        {
            if (drawingTexture != null) return;

            drawingTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            drawingTexture.filterMode = FilterMode.Bilinear;
            drawingTexture.wrapMode = TextureWrapMode.Clamp;

            clearBuffer = new Color32[textureWidth * textureHeight];
            Color32 white = new Color32(255, 255, 255, 255);
            for (int i = 0; i < clearBuffer.Length; i++)
            {
                clearBuffer[i] = white;
            }

            drawingTexture.SetPixels32(clearBuffer);
            drawingTexture.Apply();

            if (canvasDisplay != null)
            {
                canvasDisplay.texture = drawingTexture;
            }
        }

        public void SelectTool(PaintTool tool)
        {
            activeTool = tool;
            UpdateToolHighlight();
            UpdateStatusText();
        }

        public void SelectBrushSize(int radius)
        {
            brushRadius = radius;
            UpdateToolHighlight();
            UpdateStatusText();
        }

        public void SelectColor(Color c, int swatchIndex)
        {
            activeColor = c;
            if (activeTool == PaintTool.Eraser)
            {
                activeTool = PaintTool.Brush;
            }
            if (activeColorPreview != null)
            {
                activeColorPreview.color = c;
            }

            if (swatchButtons != null)
            {
                for (int i = 0; i < swatchButtons.Length; i++)
                {
                    if (swatchButtons[i] == null) continue;
                    Outline swOutline = swatchButtons[i].GetComponent<Outline>();
                    if (swOutline != null)
                    {
                        swOutline.effectColor = (i == swatchIndex) ? selectedSwatchBorderColor : normalSwatchBorderColor;
                    }
                }
            }

            UpdateToolHighlight();
            UpdateStatusText();
        }

        private void UpdateToolHighlight()
        {
            SetButtonHighlight(btnBrush, activeTool == PaintTool.Brush);
            SetButtonHighlight(btnEraser, activeTool == PaintTool.Eraser);
            SetButtonHighlight(btnFill, activeTool == PaintTool.Fill);

            SetButtonHighlight(btnSizeS, brushRadius == 2);
            SetButtonHighlight(btnSizeM, brushRadius == 4);
            SetButtonHighlight(btnSizeL, brushRadius == 9);
        }

        private void SetButtonHighlight(Button btn, bool isActive)
        {
            if (btn == null) return;
            Image img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = isActive ? activeToolBgColor : normalToolBgColor;
            }
        }

        private void UpdateStatusText()
        {
            if (statusText == null) return;
            string fileLabel = currentFileItem != null ? $"[{currentFileItem.name}]  |  " : "[Untitled.png]  |  ";
            statusText.text = $"{fileLabel}{textureWidth} x {textureHeight}px  |  Tool: {activeTool} ({brushRadius * 2}px)";
        }

        private void ShowFeedback(string msg)
        {
            if (feedbackText == null) return;
            feedbackText.text = msg;
            feedbackText.gameObject.SetActive(true);
            CancelInvoke(nameof(HideFeedback));
            Invoke(nameof(HideFeedback), 3.0f);
        }

        private void HideFeedback()
        {
            if (feedbackText != null)
            {
                feedbackText.gameObject.SetActive(false);
            }
        }
        #endregion
    }
}
