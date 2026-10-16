/**
 * Author: Diego
 * Date: 13/10/2026
 * Description: Interactive task teaching keyboard shortcuts Ctrl + C (Copy) and Ctrl + V (Paste)
 *              using Inspector-configured UI references on Task_CopyPaste.prefab.
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

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private TextMeshProUGUI statusFeedbackText;
        [SerializeField] private Image ctrlBadgeImage;
        [SerializeField] private TextMeshProUGUI ctrlBadgeText;
        [SerializeField] private Image keyBadgeImage;
        [SerializeField] private TextMeshProUGUI keyBadgeText;
        [SerializeField] private GameObject pastedStarObject;
        [SerializeField] private CanvasGroup pastedStarCg;

        [Header("Badge State Colors")]
        [SerializeField] private Color keyNormalColor = new Color(0.2f, 0.25f, 0.35f, 1f);
        [SerializeField] private Color keyActiveColor = new Color(0.15f, 0.65f, 0.35f, 1f);
        [SerializeField] private Color keyPendingColor = new Color(0.25f, 0.50f, 0.90f, 1f);

        protected override void Start()
        {
            base.Start();
            if (pastedStarObject != null)
            {
                pastedStarObject.SetActive(false);
            }
        }

        protected override void SetupTask(TaskDefinition definition)
        {
            currentStep = Step.WaitingForCopy;
            hasCopied = false;
        }

        private void Update()
        {
            if (IsFinished || currentStep == Step.Completed) return;

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
    }
}
