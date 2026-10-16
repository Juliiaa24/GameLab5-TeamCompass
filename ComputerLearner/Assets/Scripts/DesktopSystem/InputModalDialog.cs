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
                inputField.text = initialValue ?? string.Empty;
                inputField.ActivateInputField();
                inputField.Select();
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
