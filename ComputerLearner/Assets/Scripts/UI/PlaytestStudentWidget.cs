/**
 * Author: Julia & AI (TeamCompass)
 * Date: 01/11/2026
 * Description: Clean, minimalist Student ID & Export bar in the Main Menu ONLY.
 *              Matches the exact light rounded aesthetic of Menu.unity's buttons and Dropdown-Language,
 *              and uses a prefab-serialized TMP_InputField (plus direct key polling fallback)
 *              so the Student ID / Name can always be edited freely, with manual Export and Auto Download toggle.
 */
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

namespace ComputerLearning
{
    public class PlaytestStudentWidget : MonoBehaviour
    {
        public static PlaytestStudentWidget Instance { get; private set; }

        private TextMeshProUGUI studentIdLabel;
        private TMP_InputField studentIdInputField;
        private TextMeshProUGUI fallbackValueText;
        private TextMeshProUGUI exportButtonLabel;
        private TextMeshProUGUI autoDownloadButtonLabel;
        private GameObject toastContainer;
        private TextMeshProUGUI toastText;
        private Coroutine toastCoroutine;

        private bool isFocusedForManualTyping = false;

        public static PlaytestStudentWidget EnsureOnCanvas(Transform canvasTransform)
        {
            if (canvasTransform == null) return null;
            if (SceneManager.GetActiveScene().name != "Menu") return null;

            Transform existing = canvasTransform.Find("PlaytestStudentWidget");
            if (existing != null)
            {
                DestroyImmediate(existing.gameObject);
            }

            GameObject rootObj = new GameObject("PlaytestStudentWidget", typeof(RectTransform), typeof(PlaytestStudentWidget));
            rootObj.transform.SetParent(canvasTransform, false);
            rootObj.transform.SetAsLastSibling();

            PlaytestStudentWidget widget = rootObj.GetComponent<PlaytestStudentWidget>();
            widget.BuildUI(canvasTransform);
            return widget;
        }

        private void Awake()
        {
            if (SceneManager.GetActiveScene().name != "Menu")
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += RefreshUI;
            PlaytestDataManager.OnStudentIdChanged += OnStudentIdChangedHandler;
            PlaytestDataManager.OnAutoDownloadChanged += OnAutoDownloadChangedHandler;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            CommitPendingInput();
            LocalizationManager.OnLanguageChanged -= RefreshUI;
            PlaytestDataManager.OnStudentIdChanged -= OnStudentIdChangedHandler;
            PlaytestDataManager.OnAutoDownloadChanged -= OnAutoDownloadChangedHandler;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void CommitPendingInput()
        {
            if (studentIdInputField != null && !string.IsNullOrWhiteSpace(studentIdInputField.text))
            {
                if (studentIdInputField.text.Trim() != PlaytestDataManager.CurrentStudentId)
                {
                    PlaytestDataManager.SetCurrentStudentId(studentIdInputField.text.Trim());
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Menu")
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (SceneManager.GetActiveScene().name != "Menu")
            {
                Destroy(gameObject);
                return;
            }

            // If TMP_InputField is not present (fallback mode), allow direct key polling
            if (studentIdInputField == null && isFocusedForManualTyping && Keyboard.current != null)
            {
                PollManualKeyboardFallback();
            }
        }

        private void PollManualKeyboardFallback()
        {
            string current = PlaytestDataManager.CurrentStudentId;
            if (Keyboard.current.backspaceKey.wasPressedThisFrame && current.Length > 0)
            {
                current = current.Substring(0, current.Length - 1);
                PlaytestDataManager.SetCurrentStudentId(current);
                if (fallbackValueText != null) fallbackValueText.text = current;
                return;
            }

            if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                isFocusedForManualTyping = false;
                RefreshUI();
                return;
            }

            foreach (var key in Keyboard.current.allKeys)
            {
                if (key == null || !key.wasPressedThisFrame) continue;
                string kName = key.name;
                if (kName.Length == 1 && char.IsLetterOrDigit(kName[0]) && current.Length < 20)
                {
                    bool shift = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
                    char c = shift ? char.ToUpperInvariant(kName[0]) : kName[0];
                    current += c;
                    PlaytestDataManager.SetCurrentStudentId(current);
                    if (fallbackValueText != null) fallbackValueText.text = current;
                }
            }
        }

        private void OnStudentIdChangedHandler(string newId)
        {
            if (studentIdInputField != null && !studentIdInputField.isFocused && studentIdInputField.text != newId)
            {
                studentIdInputField.SetTextWithoutNotify(newId);
            }
            if (fallbackValueText != null)
            {
                fallbackValueText.text = newId;
            }
        }

        private void OnAutoDownloadChangedHandler(bool enabled)
        {
            RefreshUI();
        }

        private void BuildUI(Transform canvasTransform)
        {
            PlaytestDataManager.EnsureInitialized();

            // Extract exact sprite and font from Menu's existing UI elements (Btn-Play / Dropdown-Language)
            Sprite menuRoundedSprite = null;
            TMP_FontAsset menuFont = null;

            Transform btnPlayT = canvasTransform.Find("Panel (1)/Btn-Play") ?? canvasTransform.Find("Btn-Play");
            if (btnPlayT == null)
            {
                Button[] allBtns = canvasTransform.GetComponentsInChildren<Button>(true);
                foreach (var b in allBtns)
                {
                    if (b.name == "Btn-Play") { btnPlayT = b.transform; break; }
                }
            }

            if (btnPlayT != null)
            {
                Image btnImg = btnPlayT.GetComponent<Image>();
                if (btnImg != null) menuRoundedSprite = btnImg.sprite;
                TMP_Text btnTmp = btnPlayT.GetComponentInChildren<TMP_Text>(true);
                if (btnTmp != null) menuFont = btnTmp.font;
            }

            Color menuPillColor = new Color(0.95f, 0.96f, 0.98f, 1f); // Exact match to Dropdown-Language
            Color menuTextColor = new Color(0.196f, 0.196f, 0.196f, 1f); // Exact match to Btn-Play & Dropdown-Language text

            // Root container positioned at bottom-left, mirroring Dropdown-Language at (-40, 30) height 58.127
            RectTransform rootRt = GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0f, 0f);
            rootRt.anchorMax = new Vector2(0f, 0f);
            rootRt.pivot = new Vector2(0f, 0f);
            rootRt.anchoredPosition = new Vector2(40f, 30f);
            rootRt.sizeDelta = new Vector2(658f, 58.127f);

            // 1. Left Pill: Student ID Container (matches Dropdown-Language style)
            GameObject idPillObj = new GameObject("StudentIdPill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            idPillObj.transform.SetParent(transform, false);
            RectTransform idPillRt = idPillObj.GetComponent<RectTransform>();
            idPillRt.anchorMin = new Vector2(0f, 0f);
            idPillRt.anchorMax = new Vector2(0f, 1f);
            idPillRt.pivot = new Vector2(0f, 0.5f);
            idPillRt.anchoredPosition = Vector2.zero;
            idPillRt.sizeDelta = new Vector2(250f, 0f);

            Image idPillImg = idPillObj.GetComponent<Image>();
            idPillImg.color = menuPillColor;
            idPillImg.raycastTarget = false;
            if (menuRoundedSprite != null)
            {
                idPillImg.sprite = menuRoundedSprite;
                idPillImg.type = Image.Type.Sliced;
            }

            // Label inside Left Pill ("Student ID:" / "ID Alumno:")
            GameObject lblObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            lblObj.transform.SetParent(idPillObj.transform, false);
            RectTransform lblRt = lblObj.GetComponent<RectTransform>();
            lblRt.anchorMin = new Vector2(0f, 0f);
            lblRt.anchorMax = new Vector2(0f, 1f);
            lblRt.pivot = new Vector2(0f, 0.5f);
            lblRt.anchoredPosition = new Vector2(14f, 0f);
            lblRt.sizeDelta = new Vector2(100f, 0f);

            studentIdLabel = lblObj.GetComponent<TextMeshProUGUI>();
            if (menuFont != null) studentIdLabel.font = menuFont;
            studentIdLabel.fontSize = 18;
            studentIdLabel.color = menuTextColor;
            studentIdLabel.alignment = TextAlignmentOptions.MidlineLeft;
            studentIdLabel.raycastTarget = false;

            // Editable InputField inside Left Pill (Instantiated from serialized InputModalDialog prefab)
            GameObject modalPrefab = Resources.Load<GameObject>("InputModalDialog");
#if UNITY_EDITOR
            if (modalPrefab == null)
            {
                modalPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Windows/InputModalDialog.prefab");
            }
#endif
            TMP_InputField prefabInput = modalPrefab != null ? modalPrefab.GetComponentInChildren<TMP_InputField>(true) : null;

            if (prefabInput != null)
            {
                studentIdInputField = Instantiate(prefabInput, idPillObj.transform);
                studentIdInputField.name = "NameInputField";

                RectTransform inputRt = studentIdInputField.GetComponent<RectTransform>();
                inputRt.anchorMin = new Vector2(0f, 0.5f);
                inputRt.anchorMax = new Vector2(0f, 0.5f);
                inputRt.pivot = new Vector2(0f, 0.5f);
                inputRt.anchoredPosition = new Vector2(118f, 0f);
                inputRt.sizeDelta = new Vector2(120f, 40f);

                Image inputBg = studentIdInputField.GetComponent<Image>();
                if (inputBg != null)
                {
                    inputBg.color = Color.white;
                    inputBg.raycastTarget = true;
                    if (menuRoundedSprite != null)
                    {
                        inputBg.sprite = menuRoundedSprite;
                        inputBg.type = Image.Type.Sliced;
                    }
                }

                Outline inputOutline = studentIdInputField.GetComponent<Outline>();
                if (inputOutline != null)
                {
                    inputOutline.effectColor = new Color(0.78f, 0.82f, 0.88f, 1f);
                    inputOutline.effectDistance = new Vector2(1f, -1f);
                }

                if (studentIdInputField.textComponent != null)
                {
                    if (menuFont != null) studentIdInputField.textComponent.font = menuFont;
                    studentIdInputField.textComponent.fontSize = 19;
                    studentIdInputField.textComponent.color = menuTextColor;
                    studentIdInputField.textComponent.alignment = TextAlignmentOptions.Center;
                    studentIdInputField.textComponent.raycastTarget = false;
                }

                studentIdInputField.pointSize = 19f;
                studentIdInputField.characterLimit = 22;
                studentIdInputField.onFocusSelectAll = false;
                studentIdInputField.SetTextWithoutNotify(PlaytestDataManager.CurrentStudentId);

                studentIdInputField.onValueChanged.RemoveAllListeners();
                studentIdInputField.onEndEdit.RemoveAllListeners();
                studentIdInputField.onSubmit.RemoveAllListeners();

                studentIdInputField.onEndEdit.AddListener(OnInputEndEdit);
                studentIdInputField.onSubmit.AddListener(OnInputEndEdit);
            }
            else
            {
                // Fallback clickable box if prefab was not found
                GameObject fallbackBox = new GameObject("FallbackInputBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                fallbackBox.transform.SetParent(idPillObj.transform, false);
                RectTransform fbRt = fallbackBox.GetComponent<RectTransform>();
                fbRt.anchorMin = new Vector2(0f, 0.5f);
                fbRt.anchorMax = new Vector2(0f, 0.5f);
                fbRt.pivot = new Vector2(0f, 0.5f);
                fbRt.anchoredPosition = new Vector2(118f, 0f);
                fbRt.sizeDelta = new Vector2(120f, 40f);

                Image fbImg = fallbackBox.GetComponent<Image>();
                fbImg.color = Color.white;
                fbImg.raycastTarget = true;
                if (menuRoundedSprite != null)
                {
                    fbImg.sprite = menuRoundedSprite;
                    fbImg.type = Image.Type.Sliced;
                }

                fallbackBox.GetComponent<Button>().onClick.AddListener(() => { isFocusedForManualTyping = true; });

                GameObject fbTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                fbTxtObj.transform.SetParent(fallbackBox.transform, false);
                RectTransform fbtRt = fbTxtObj.GetComponent<RectTransform>();
                fbtRt.anchorMin = Vector2.zero;
                fbtRt.anchorMax = Vector2.one;
                fbtRt.offsetMin = Vector2.zero;
                fbtRt.offsetMax = Vector2.zero;

                fallbackValueText = fbTxtObj.GetComponent<TextMeshProUGUI>();
                if (menuFont != null) fallbackValueText.font = menuFont;
                fallbackValueText.fontSize = 19;
                fallbackValueText.color = menuTextColor;
                fallbackValueText.alignment = TextAlignmentOptions.Center;
                fallbackValueText.raycastTarget = false;
                fallbackValueText.text = PlaytestDataManager.CurrentStudentId;
            }

            // 2. Middle Pill: Export Data Button (Exact same aesthetic as Menu buttons & Dropdown-Language)
            GameObject btnObj = new GameObject("Btn-ExportPlaytest", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(transform, false);
            RectTransform btnRt = btnObj.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0f, 0f);
            btnRt.anchorMax = new Vector2(0f, 1f);
            btnRt.pivot = new Vector2(0f, 0.5f);
            btnRt.anchoredPosition = new Vector2(264f, 0f);
            btnRt.sizeDelta = new Vector2(170f, 0f);

            Image exportBg = btnObj.GetComponent<Image>();
            exportBg.color = menuPillColor;
            exportBg.raycastTarget = true;
            if (menuRoundedSprite != null)
            {
                exportBg.sprite = menuRoundedSprite;
                exportBg.type = Image.Type.Sliced;
            }

            Button exportBtn = btnObj.GetComponent<Button>();
            exportBtn.targetGraphic = exportBg;
            ColorBlock cb = exportBtn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(0.92f, 0.94f, 0.98f, 1f);
            cb.pressedColor = new Color(0.82f, 0.85f, 0.90f, 1f);
            cb.selectedColor = Color.white;
            exportBtn.colors = cb;
            exportBtn.navigation = new Navigation { mode = Navigation.Mode.None };
            exportBtn.onClick.RemoveAllListeners();
            exportBtn.onClick.AddListener(OnExportButtonClicked);

            GameObject btnTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            btnTxtObj.transform.SetParent(btnObj.transform, false);
            RectTransform btRt = btnTxtObj.GetComponent<RectTransform>();
            btRt.anchorMin = Vector2.zero;
            btRt.anchorMax = Vector2.one;
            btRt.offsetMin = new Vector2(8f, 0f);
            btRt.offsetMax = new Vector2(-8f, 0f);

            exportButtonLabel = btnTxtObj.GetComponent<TextMeshProUGUI>();
            if (menuFont != null) exportButtonLabel.font = menuFont;
            exportButtonLabel.fontSize = 17;
            exportButtonLabel.color = menuTextColor;
            exportButtonLabel.alignment = TextAlignmentOptions.Center;
            exportButtonLabel.raycastTarget = false;

            // 3. Right Pill: Auto-Download Toggle Button (English, matches Menu aesthetic)
            GameObject autoBtnObj = new GameObject("Btn-AutoDownload", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            autoBtnObj.transform.SetParent(transform, false);
            RectTransform autoBtnRt = autoBtnObj.GetComponent<RectTransform>();
            autoBtnRt.anchorMin = new Vector2(0f, 0f);
            autoBtnRt.anchorMax = new Vector2(0f, 1f);
            autoBtnRt.pivot = new Vector2(0f, 0.5f);
            autoBtnRt.anchoredPosition = new Vector2(448f, 0f);
            autoBtnRt.sizeDelta = new Vector2(210f, 0f);

            Image autoBg = autoBtnObj.GetComponent<Image>();
            autoBg.color = menuPillColor;
            autoBg.raycastTarget = true;
            if (menuRoundedSprite != null)
            {
                autoBg.sprite = menuRoundedSprite;
                autoBg.type = Image.Type.Sliced;
            }

            Button autoBtn = autoBtnObj.GetComponent<Button>();
            autoBtn.targetGraphic = autoBg;
            autoBtn.colors = cb;
            autoBtn.navigation = new Navigation { mode = Navigation.Mode.None };
            autoBtn.onClick.RemoveAllListeners();
            autoBtn.onClick.AddListener(OnAutoDownloadButtonClicked);

            GameObject autoTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            autoTxtObj.transform.SetParent(autoBtnObj.transform, false);
            RectTransform atRt = autoTxtObj.GetComponent<RectTransform>();
            atRt.anchorMin = Vector2.zero;
            atRt.anchorMax = Vector2.one;
            atRt.offsetMin = new Vector2(8f, 0f);
            atRt.offsetMax = new Vector2(-8f, 0f);

            autoDownloadButtonLabel = autoTxtObj.GetComponent<TextMeshProUGUI>();
            if (menuFont != null) autoDownloadButtonLabel.font = menuFont;
            autoDownloadButtonLabel.fontSize = 17;
            autoDownloadButtonLabel.color = menuTextColor;
            autoDownloadButtonLabel.alignment = TextAlignmentOptions.Center;
            autoDownloadButtonLabel.richText = true;
            autoDownloadButtonLabel.raycastTarget = false;

            // 4. Subtle Toast Banner above the widget (matching light Menu aesthetic)
            toastContainer = new GameObject("ToastBanner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            toastContainer.transform.SetParent(transform, false);
            RectTransform toastRt = toastContainer.GetComponent<RectTransform>();
            toastRt.anchorMin = new Vector2(0f, 1f);
            toastRt.anchorMax = new Vector2(0f, 1f);
            toastRt.pivot = new Vector2(0f, 0f);
            toastRt.anchoredPosition = new Vector2(0f, 10f);
            toastRt.sizeDelta = new Vector2(658f, 42f);

            Image toastImg = toastContainer.GetComponent<Image>();
            toastImg.color = menuPillColor;
            toastImg.raycastTarget = false;
            if (menuRoundedSprite != null)
            {
                toastImg.sprite = menuRoundedSprite;
                toastImg.type = Image.Type.Sliced;
            }

            GameObject toastTxtObj = new GameObject("ToastText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            toastTxtObj.transform.SetParent(toastContainer.transform, false);
            RectTransform ttRt = toastTxtObj.GetComponent<RectTransform>();
            ttRt.anchorMin = Vector2.zero;
            ttRt.anchorMax = Vector2.one;
            ttRt.offsetMin = new Vector2(14f, 0f);
            ttRt.offsetMax = new Vector2(-14f, 0f);

            toastText = toastTxtObj.GetComponent<TextMeshProUGUI>();
            if (menuFont != null) toastText.font = menuFont;
            toastText.fontSize = 15;
            toastText.color = menuTextColor;
            toastText.alignment = TextAlignmentOptions.MidlineLeft;
            toastText.raycastTarget = false;

            toastContainer.SetActive(false);

            RefreshUI();
        }

        private void OnInputEndEdit(string enteredValue)
        {
            PlaytestDataManager.SetCurrentStudentId(enteredValue);
            RefreshUI();
        }

        private void OnExportButtonClicked()
        {
            if (studentIdInputField != null && !string.IsNullOrWhiteSpace(studentIdInputField.text))
            {
                PlaytestDataManager.SetCurrentStudentId(studentIdInputField.text);
            }

            FeedbackManager.PlayClick();
            string folderPath = PlaytestDataManager.ExportCurrentStudentData("manual_export_button");
            if (!string.IsNullOrEmpty(folderPath))
            {
                UIDialogs ui = UIDialogs.Current;
                string msg = string.Format(ui.playtest_export_success, PlaytestDataManager.CurrentStudentId);
                ShowToast(msg, 3.5f);
            }
        }

        private void OnAutoDownloadButtonClicked()
        {
            FeedbackManager.PlayClick();
            bool newState = PlaytestDataManager.ToggleAutoDownload();
            RefreshUI();

            UIDialogs ui = UIDialogs.Current;
            string msg = newState
                ? (!string.IsNullOrEmpty(ui.playtest_auto_download_enabled_toast) ? ui.playtest_auto_download_enabled_toast : "Auto download enabled (data will save automatically)")
                : (!string.IsNullOrEmpty(ui.playtest_auto_download_disabled_toast) ? ui.playtest_auto_download_disabled_toast : "Auto download disabled (data will only save when clicking Export)");
            ShowToast(msg, 3.5f);
            Debug.Log($"[PlaytestStudentWidget] Auto Download toggled. State is now: {(newState ? "ON" : "OFF")}");
        }

        public void RefreshUI()
        {
            UIDialogs ui = UIDialogs.Current;
            if (studentIdLabel != null)
            {
                studentIdLabel.text = ui.playtest_student_id_label;
            }
            if (exportButtonLabel != null)
            {
                exportButtonLabel.text = ui.playtest_btn_export;
            }
            if (autoDownloadButtonLabel != null)
            {
                bool isAutoOn = PlaytestDataManager.AutoDownloadEnabled;
                autoDownloadButtonLabel.text = isAutoOn 
                    ? "Auto Download: <color=#16a34a><b>ON</b></color>" 
                    : "Auto Download: <color=#dc2626><b>OFF</b></color>";
                autoDownloadButtonLabel.ForceMeshUpdate();
            }
            if (studentIdInputField != null && !studentIdInputField.isFocused)
            {
                studentIdInputField.SetTextWithoutNotify(PlaytestDataManager.CurrentStudentId);
            }
            if (fallbackValueText != null)
            {
                fallbackValueText.text = PlaytestDataManager.CurrentStudentId;
            }
        }

        public void ShowToast(string message, float duration = 3.5f)
        {
            if (toastContainer == null || toastText == null) return;
            toastText.text = message;
            toastContainer.SetActive(true);
            if (toastCoroutine != null) StopCoroutine(toastCoroutine);
            toastCoroutine = StartCoroutine(HideToastAfterDelay(duration));
        }

        private IEnumerator HideToastAfterDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            if (toastContainer != null) toastContainer.SetActive(false);
            toastCoroutine = null;
        }
    }
}
