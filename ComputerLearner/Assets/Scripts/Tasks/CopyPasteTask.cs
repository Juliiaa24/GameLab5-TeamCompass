/**
 * Author: Diego
 * Date: 13/10/2026
 * Description: Interactive task teaching keyboard shortcuts Ctrl + C (Copy) and Ctrl + V (Paste).
 */
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ComputerLearning
{
    public class CopyPasteTask : BaseTask
    {
        private enum Step
        {
            WaitingForCopy,
            WaitingForPaste,
            Completed
        }

        private Step currentStep = Step.WaitingForCopy;
        private bool hasCopied = false;

        // UI references (built procedurally for robustness)
        private GameObject rootUI;
        private TextMeshProUGUI promptText;
        private TextMeshProUGUI statusFeedbackText;
        private Image ctrlBadgeImage;
        private TextMeshProUGUI ctrlBadgeText;
        private Image keyBadgeImage;
        private TextMeshProUGUI keyBadgeText;
        private GameObject pastedStarObject;
        private CanvasGroup pastedStarCg;

        private Color keyNormalColor = new Color(0.2f, 0.25f, 0.35f, 1f);
        private Color keyActiveColor = new Color(0.15f, 0.65f, 0.35f, 1f);
        private Color keyPendingColor = new Color(0.25f, 0.50f, 0.90f, 1f);

        protected override void Start()
        {
            base.Start();
            BuildUI();
        }

        protected override void SetupTask(TaskDefinition definition)
        {
            currentStep = Step.WaitingForCopy;
            hasCopied = false;
        }

        private void Update()
        {
            if (IsFinished || currentStep == Step.Completed) return;

            // Check keyboard state using new Input System with legacy fallback
            bool isCtrl = false;
            bool isC = false;
            bool isV = false;

            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                isCtrl = kb.ctrlKey.isPressed;
                isC = kb.cKey.wasPressedThisFrame;
                isV = kb.vKey.wasPressedThisFrame;
            }
            else
            {
                isCtrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                isC = Input.GetKeyDown(KeyCode.C);
                isV = Input.GetKeyDown(KeyCode.V);
            }

            // Real-time badge visual feedback
            if (ctrlBadgeImage != null)
            {
                ctrlBadgeImage.color = isCtrl ? keyActiveColor : keyPendingColor;
            }

            if (currentStep == Step.WaitingForCopy)
            {
                if (keyBadgeImage != null)
                {
                    keyBadgeImage.color = (isCtrl && isC) ? keyActiveColor : keyNormalColor;
                }

                if (isCtrl && isC)
                {
                    OnCopySuccess();
                }
            }
            else if (currentStep == Step.WaitingForPaste)
            {
                if (keyBadgeImage != null)
                {
                    keyBadgeImage.color = (isCtrl && isV) ? keyActiveColor : keyNormalColor;
                }

                if (isCtrl && isV)
                {
                    OnPasteSuccess();
                }
            }
        }

        private void OnCopySuccess()
        {
            currentStep = Step.WaitingForPaste;
            hasCopied = true;
            RecordIntermediateResult(true);

            if (statusFeedbackText != null)
            {
                statusFeedbackText.text = "Copied to clipboard!";
                statusFeedbackText.color = new Color(0.1f, 0.7f, 0.3f, 1f);
            }

            if (promptText != null)
            {
                promptText.text = "Step 2: Hold <color=#2563EB>[Ctrl]</color> and press <color=#2563EB>[V]</color> to Paste!";
            }

            if (keyBadgeText != null)
            {
                keyBadgeText.text = "V";
            }
        }

        private void OnPasteSuccess()
        {
            currentStep = Step.Completed;

            if (statusFeedbackText != null)
            {
                statusFeedbackText.text = "Pasted successfully! Excellent work!";
                statusFeedbackText.color = new Color(0.1f, 0.75f, 0.35f, 1f);
            }

            if (promptText != null)
            {
                promptText.text = "<color=#16A34A>Great Job! You mastered Copy & Paste shortcuts!</color>";
            }

            if (pastedStarObject != null)
            {
                pastedStarObject.SetActive(true);
                StartCoroutine(PopPastedStar());
            }

            StartCoroutine(FinishAfterDelay(1.8f));
        }

        private IEnumerator PopPastedStar()
        {
            Transform t = pastedStarObject.transform;
            t.localScale = Vector3.zero;
            float elapsed = 0f;
            float duration = 0.35f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                t.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 1.15f, progress);
                yield return null;
            }

            t.localScale = Vector3.one;
        }

        private IEnumerator FinishAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            Complete(true);
        }

        private void BuildUI()
        {
            if (rootUI != null) return;

            rootUI = new GameObject("CopyPasteTask_UI", typeof(RectTransform));
            rootUI.transform.SetParent(transform, false);
            RectTransform rt = rootUI.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Background soft panel
            GameObject bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(rootUI.transform, false);
            RectTransform bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            bgGo.GetComponent<Image>().color = new Color(0.96f, 0.97f, 0.99f, 1f);

            // Title
            GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(rootUI.transform, false);
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.1f, 0.85f);
            titleRt.anchorMax = new Vector2(0.9f, 0.97f);
            titleRt.offsetMin = Vector2.zero;
            titleRt.offsetMax = Vector2.zero;
            TextMeshProUGUI titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
            titleTmp.text = "Keyboard Shortcuts Practice: Copy & Paste";
            titleTmp.fontSize = 20;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(0.12f, 0.16f, 0.24f, 1f);

            // Prompt Instruction
            GameObject promptGo = new GameObject("Prompt", typeof(RectTransform), typeof(TextMeshProUGUI));
            promptGo.transform.SetParent(rootUI.transform, false);
            RectTransform promptRt = promptGo.GetComponent<RectTransform>();
            promptRt.anchorMin = new Vector2(0.05f, 0.72f);
            promptRt.anchorMax = new Vector2(0.95f, 0.84f);
            promptRt.offsetMin = Vector2.zero;
            promptRt.offsetMax = Vector2.zero;
            promptText = promptGo.GetComponent<TextMeshProUGUI>();
            promptText.text = "Step 1: Hold <color=#2563EB>[Ctrl]</color> and press <color=#2563EB>[C]</color> to Copy!";
            promptText.fontSize = 16;
            promptText.alignment = TextAlignmentOptions.Center;
            promptText.color = new Color(0.20f, 0.25f, 0.35f, 1f);

            // Cards Container
            GameObject cardsContainer = new GameObject("CardsContainer", typeof(RectTransform));
            cardsContainer.transform.SetParent(rootUI.transform, false);
            RectTransform cRt = cardsContainer.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.1f, 0.32f);
            cRt.anchorMax = new Vector2(0.9f, 0.68f);
            cRt.offsetMin = Vector2.zero;
            cRt.offsetMax = Vector2.zero;

            // Source Card (Original Star)
            GameObject srcCard = new GameObject("SourceCard", typeof(RectTransform), typeof(Image));
            srcCard.transform.SetParent(cardsContainer.transform, false);
            RectTransform srcRt = srcCard.GetComponent<RectTransform>();
            srcRt.anchorMin = new Vector2(0.05f, 0.05f);
            srcRt.anchorMax = new Vector2(0.40f, 0.95f);
            srcRt.offsetMin = Vector2.zero;
            srcRt.offsetMax = Vector2.zero;
            srcCard.GetComponent<Image>().color = Color.white;

            GameObject srcStar = new GameObject("StarText", typeof(RectTransform), typeof(TextMeshProUGUI));
            srcStar.transform.SetParent(srcCard.transform, false);
            RectTransform sStarRt = srcStar.GetComponent<RectTransform>();
            sStarRt.anchorMin = new Vector2(0, 0.25f);
            sStarRt.anchorMax = new Vector2(1, 0.95f);
            sStarRt.offsetMin = Vector2.zero;
            sStarRt.offsetMax = Vector2.zero;
            TextMeshProUGUI sStarTmp = srcStar.GetComponent<TextMeshProUGUI>();
            sStarTmp.text = "<color=#F59E0B><size=48>★</size></color>\n<size=14>Star Item</size>";
            sStarTmp.alignment = TextAlignmentOptions.Center;
            sStarTmp.color = new Color(0.2f, 0.25f, 0.35f, 1f);

            GameObject srcLabel = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            srcLabel.transform.SetParent(srcCard.transform, false);
            RectTransform sLblRt = srcLabel.GetComponent<RectTransform>();
            sLblRt.anchorMin = new Vector2(0, 0);
            sLblRt.anchorMax = new Vector2(1, 0.25f);
            sLblRt.offsetMin = Vector2.zero;
            sLblRt.offsetMax = Vector2.zero;
            TextMeshProUGUI sLblTmp = srcLabel.GetComponent<TextMeshProUGUI>();
            sLblTmp.text = "[ Original ]";
            sLblTmp.fontSize = 12;
            sLblTmp.fontStyle = FontStyles.Bold;
            sLblTmp.alignment = TextAlignmentOptions.Center;
            sLblTmp.color = new Color(0.4f, 0.5f, 0.6f, 1f);

            // Arrow in middle
            GameObject arrowGo = new GameObject("Arrow", typeof(RectTransform), typeof(TextMeshProUGUI));
            arrowGo.transform.SetParent(cardsContainer.transform, false);
            RectTransform arrowRt = arrowGo.GetComponent<RectTransform>();
            arrowRt.anchorMin = new Vector2(0.42f, 0.4f);
            arrowRt.anchorMax = new Vector2(0.58f, 0.6f);
            arrowRt.offsetMin = Vector2.zero;
            arrowRt.offsetMax = Vector2.zero;
            TextMeshProUGUI arrTmp = arrowGo.GetComponent<TextMeshProUGUI>();
            arrTmp.text = "➜";
            arrTmp.fontSize = 28;
            arrTmp.alignment = TextAlignmentOptions.Center;
            arrTmp.color = new Color(0.5f, 0.6f, 0.75f, 1f);

            // Target Box (Pasted Star Destination)
            GameObject targetCard = new GameObject("TargetBox", typeof(RectTransform), typeof(Image));
            targetCard.transform.SetParent(cardsContainer.transform, false);
            RectTransform tgtRt = targetCard.GetComponent<RectTransform>();
            tgtRt.anchorMin = new Vector2(0.60f, 0.05f);
            tgtRt.anchorMax = new Vector2(0.95f, 0.95f);
            tgtRt.offsetMin = Vector2.zero;
            tgtRt.offsetMax = Vector2.zero;
            targetCard.GetComponent<Image>().color = new Color(0.92f, 0.94f, 0.97f, 1f);

            // Placeholder text inside target box
            GameObject placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            placeholderGo.transform.SetParent(targetCard.transform, false);
            RectTransform plRt = placeholderGo.GetComponent<RectTransform>();
            plRt.anchorMin = Vector2.zero;
            plRt.anchorMax = Vector2.one;
            plRt.offsetMin = Vector2.zero;
            plRt.offsetMax = Vector2.zero;
            TextMeshProUGUI plTmp = placeholderGo.GetComponent<TextMeshProUGUI>();
            plTmp.text = "<size=13>Paste Target Box\n(Empty)</size>";
            plTmp.alignment = TextAlignmentOptions.Center;
            plTmp.color = new Color(0.60f, 0.65f, 0.75f, 1f);

            // Pasted Star Container (initially hidden)
            pastedStarObject = new GameObject("PastedStar", typeof(RectTransform), typeof(CanvasGroup));
            pastedStarObject.transform.SetParent(targetCard.transform, false);
            RectTransform pStarRt = pastedStarObject.GetComponent<RectTransform>();
            pStarRt.anchorMin = Vector2.zero;
            pStarRt.anchorMax = Vector2.one;
            pStarRt.offsetMin = Vector2.zero;
            pStarRt.offsetMax = Vector2.zero;
            pastedStarCg = pastedStarObject.GetComponent<CanvasGroup>();

            GameObject pStarTxt = new GameObject("StarTxt", typeof(RectTransform), typeof(TextMeshProUGUI));
            pStarTxt.transform.SetParent(pastedStarObject.transform, false);
            RectTransform pstRt = pStarTxt.GetComponent<RectTransform>();
            pstRt.anchorMin = Vector2.zero;
            pstRt.anchorMax = Vector2.one;
            pstRt.offsetMin = Vector2.zero;
            pstRt.offsetMax = Vector2.zero;
            TextMeshProUGUI pstTmp = pStarTxt.GetComponent<TextMeshProUGUI>();
            pstTmp.text = "<color=#F59E0B><size=48>★</size></color>\n<size=14><color=#16A34A>Pasted Copy!</color></size>";
            pstTmp.alignment = TextAlignmentOptions.Center;
            pstTmp.color = new Color(0.2f, 0.25f, 0.35f, 1f);
            pastedStarObject.SetActive(false);

            // Key Badges Container
            GameObject keysContainer = new GameObject("KeysContainer", typeof(RectTransform));
            keysContainer.transform.SetParent(rootUI.transform, false);
            RectTransform kRt = keysContainer.GetComponent<RectTransform>();
            kRt.anchorMin = new Vector2(0.2f, 0.14f);
            kRt.anchorMax = new Vector2(0.8f, 0.28f);
            kRt.offsetMin = Vector2.zero;
            kRt.offsetMax = Vector2.zero;

            // [Ctrl] badge
            GameObject ctrlBadge = new GameObject("CtrlBadge", typeof(RectTransform), typeof(Image));
            ctrlBadge.transform.SetParent(keysContainer.transform, false);
            RectTransform cbrt = ctrlBadge.GetComponent<RectTransform>();
            cbrt.anchorMin = new Vector2(0.25f, 0.1f);
            cbrt.anchorMax = new Vector2(0.48f, 0.9f);
            cbrt.offsetMin = Vector2.zero;
            cbrt.offsetMax = Vector2.zero;
            ctrlBadgeImage = ctrlBadge.GetComponent<Image>();
            ctrlBadgeImage.color = keyPendingColor;

            GameObject ctrlTxtGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            ctrlTxtGo.transform.SetParent(ctrlBadge.transform, false);
            RectTransform ctrt = ctrlTxtGo.GetComponent<RectTransform>();
            ctrt.anchorMin = Vector2.zero;
            ctrt.anchorMax = Vector2.one;
            ctrt.offsetMin = Vector2.zero;
            ctrt.offsetMax = Vector2.zero;
            ctrlBadgeText = ctrlTxtGo.GetComponent<TextMeshProUGUI>();
            ctrlBadgeText.text = "Ctrl";
            ctrlBadgeText.fontSize = 15;
            ctrlBadgeText.fontStyle = FontStyles.Bold;
            ctrlBadgeText.alignment = TextAlignmentOptions.Center;
            ctrlBadgeText.color = Color.white;

            // '+' separator
            GameObject plusGo = new GameObject("Plus", typeof(RectTransform), typeof(TextMeshProUGUI));
            plusGo.transform.SetParent(keysContainer.transform, false);
            RectTransform plusRt = plusGo.GetComponent<RectTransform>();
            plusRt.anchorMin = new Vector2(0.48f, 0.1f);
            plusRt.anchorMax = new Vector2(0.54f, 0.9f);
            plusRt.offsetMin = Vector2.zero;
            plusRt.offsetMax = Vector2.zero;
            TextMeshProUGUI plusTmp = plusGo.GetComponent<TextMeshProUGUI>();
            plusTmp.text = "+";
            plusTmp.fontSize = 20;
            plusTmp.fontStyle = FontStyles.Bold;
            plusTmp.alignment = TextAlignmentOptions.Center;
            plusTmp.color = new Color(0.3f, 0.4f, 0.5f, 1f);

            // [C] or [V] badge
            GameObject keyBadge = new GameObject("KeyBadge", typeof(RectTransform), typeof(Image));
            keyBadge.transform.SetParent(keysContainer.transform, false);
            RectTransform kbrt = keyBadge.GetComponent<RectTransform>();
            kbrt.anchorMin = new Vector2(0.54f, 0.1f);
            kbrt.anchorMax = new Vector2(0.72f, 0.9f);
            kbrt.offsetMin = Vector2.zero;
            kbrt.offsetMax = Vector2.zero;
            keyBadgeImage = keyBadge.GetComponent<Image>();
            keyBadgeImage.color = keyNormalColor;

            GameObject keyTxtGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            keyTxtGo.transform.SetParent(keyBadge.transform, false);
            RectTransform ktrt = keyTxtGo.GetComponent<RectTransform>();
            ktrt.anchorMin = Vector2.zero;
            ktrt.anchorMax = Vector2.one;
            ktrt.offsetMin = Vector2.zero;
            ktrt.offsetMax = Vector2.zero;
            keyBadgeText = keyTxtGo.GetComponent<TextMeshProUGUI>();
            keyBadgeText.text = "C";
            keyBadgeText.fontSize = 16;
            keyBadgeText.fontStyle = FontStyles.Bold;
            keyBadgeText.alignment = TextAlignmentOptions.Center;
            keyBadgeText.color = Color.white;

            // Status feedback bottom bar
            GameObject statusGo = new GameObject("StatusFeedback", typeof(RectTransform), typeof(TextMeshProUGUI));
            statusGo.transform.SetParent(rootUI.transform, false);
            RectTransform sRt = statusGo.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.05f, 0.02f);
            sRt.anchorMax = new Vector2(0.95f, 0.12f);
            sRt.offsetMin = Vector2.zero;
            sRt.offsetMax = Vector2.zero;
            statusFeedbackText = statusGo.GetComponent<TextMeshProUGUI>();
            statusFeedbackText.text = "Press the keyboard shortcut to continue.";
            statusFeedbackText.fontSize = 14;
            statusFeedbackText.alignment = TextAlignmentOptions.Center;
            statusFeedbackText.color = new Color(0.45f, 0.50f, 0.60f, 1f);
        }
    }
}
