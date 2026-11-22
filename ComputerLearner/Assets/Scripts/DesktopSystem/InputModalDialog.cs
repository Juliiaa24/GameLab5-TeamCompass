/**
 * Author: Diego
 * Date: 16/10/2026
 * Description: Prefab-backed modal input dialog for Rename and Save As operations.
 */
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ComputerLearning
{
    public class InputModalDialog : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button btnConfirm;
        [SerializeField] private TextMeshProUGUI confirmLabel;
        [SerializeField] private Button btnCancel;

        private Action<string> onConfirmCallback;
        private Action onCancelCallback;
        private string initialTextValue = string.Empty;

        private void Awake()
        {
            if (btnConfirm != null)
            {
                btnConfirm.onClick.RemoveListener(Submit);
                btnConfirm.onClick.AddListener(Submit);
            }
            if (btnCancel != null)
            {
                btnCancel.onClick.RemoveListener(Cancel);
                btnCancel.onClick.AddListener(Cancel);
            }
            if (inputField != null)
            {
                inputField.onSubmit.RemoveListener(OnInputSubmit);
                inputField.onSubmit.AddListener(OnInputSubmit);
            }
        }

        public void Initialize(string title, string initialValue, string confirmButtonText, Action<string> onConfirm, Action onCancel = null)
        {
            onConfirmCallback = onConfirm;
            onCancelCallback = onCancel;

            if (titleText != null)
            {
                titleText.text = title;
            }

            if (confirmLabel != null)
            {
                confirmLabel.text = string.IsNullOrEmpty(confirmButtonText) ? "Save" : confirmButtonText;
            }

            if (inputField != null)
            {
                inputField.characterLimit = FileExplorerApp.MAX_ITEM_NAME_LENGTH;
                string safeInit = initialValue ?? string.Empty;
                if (safeInit.Length > FileExplorerApp.MAX_ITEM_NAME_LENGTH)
                {
                    safeInit = safeInit.Substring(0, FileExplorerApp.MAX_ITEM_NAME_LENGTH);
                }
                initialTextValue = safeInit;
                inputField.text = safeInit;
                inputField.ActivateInputField();
                inputField.Select();
            }
        }

        private void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            bool isCtrl = kb != null && (kb.ctrlKey.isPressed || kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed);
            bool isZ = kb != null && kb.zKey.wasPressedThisFrame;
            bool isEsc = kb != null && kb.escapeKey.wasPressedThisFrame;
#if ENABLE_LEGACY_INPUT_MANAGER
            if (!isCtrl) isCtrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (!isZ) isZ = Input.GetKeyDown(KeyCode.Z);
            if (!isEsc) isEsc = Input.GetKeyDown(KeyCode.Escape);
#endif
            if (isEsc)
            {
                Cancel();
                return;
            }

            if (isCtrl && isZ && FileExplorerApp.HasCreatedItemsToUndo)
            {
                string currentText = inputField != null ? inputField.text : string.Empty;
                if (string.Equals(currentText, initialTextValue, StringComparison.Ordinal) &&
                    FileExplorerApp.TryConsumeClipboardShortcutFrame())
                {
                    FileExplorerApp.UndoLastCreatedItems();
                }
            }
        }

        private void OnInputSubmit(string _)
        {
            Submit();
        }

        public void Submit()
        {
            string val = inputField != null ? inputField.text.Trim() : string.Empty;
            if (!string.IsNullOrEmpty(val))
            {
                Action<string> callback = onConfirmCallback;
                Destroy(gameObject);
                callback?.Invoke(val);
            }
        }

        public void Cancel()
        {
            Action callback = onCancelCallback;
            Destroy(gameObject);
            callback?.Invoke();
        }
    }
}
