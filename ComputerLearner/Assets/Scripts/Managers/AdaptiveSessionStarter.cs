using UnityEngine;

namespace ComputerLearning
{
    public class AdaptiveSessionStarter : MonoBehaviour
    {
        private TutorialDialogs dialogs;
        private void Awake() {
            TextAsset json = LocalizationManager.LoadLocalizedResource("TutorialDialogs");
            if (json != null) dialogs = JsonUtility.FromJson<TutorialDialogs>(json.text);
            else dialogs = new TutorialDialogs();
        }

        public RectTransform taskContentArea;

        private void Start()
        {
            Window window = GetComponentInParent<Window>();
            if (window != null)
            {
                window.setTitle("Level 4 - Practice");
                window.StateChanged += HandleWindowState;
            }

            if (taskContentArea == null)
            {
                if (window != null)
                {
                    taskContentArea = window.ContentArea;
                }
            }

            StartCoroutine(TutorialSequence(window));
        }

        private System.Collections.IEnumerator TutorialSequence(Window window)
        {
            RectTransform target = window != null ? window.GetComponent<RectTransform>() : taskContentArea;
            
            // Mini tutorial message
            VirtualMascot.Show(dialogs.adaptive_welcome, target, new Vector2(250, -100));
            
            yield return new WaitForSeconds(4.0f);
            if (window != null && window.IsClosed) { VirtualMascot.HideMascot(); yield break; }
            
            VirtualMascot.Show(dialogs.adaptive_explanation, target, new Vector2(250, -100));
            
            yield return new WaitForSeconds(4.0f);
            if (window != null && window.IsClosed) { VirtualMascot.HideMascot(); yield break; }
            
            VirtualMascot.HideMascot();

            if (LearningSessionManager.Instance != null && taskContentArea != null)
            {
                LearningSessionManager.Instance.OnSessionEnded -= OnSessionFinished;
                LearningSessionManager.Instance.OnSessionEnded += OnSessionFinished;

                LearningSessionManager.Instance.SetContentArea(taskContentArea);
                LearningSessionManager.Instance.StartSession();
            }
            else
            {
                Debug.LogError($"LearningSessionManager ({LearningSessionManager.Instance != null}) or taskContentArea ({taskContentArea != null}) is missing.");
            }
        }

        private void OnSessionFinished()
        {
            Window window = GetComponentInParent<Window>();
            if (window != null && !window.IsClosed)
            {
                RectTransform closeBtn = window.CloseButton;
                if (closeBtn == null) closeBtn = window.TitleBar; 
                
                if (closeBtn != null)
                {
                    VirtualMascot.Show(dialogs.adaptive_done, closeBtn.GetComponent<RectTransform>(), new Vector2(-200, -80));
                }
            }
        }

        private void OnDestroy()
        {
            if (LearningSessionManager.Instance != null)
            {
                LearningSessionManager.Instance.OnSessionEnded -= OnSessionFinished;
            }
        }

        private void HandleWindowState(Window window)
        {
            if (window.IsClosed)
            {
                if (LearningSessionManager.Instance != null && LearningSessionManager.Instance.IsSessionActive)
                {
                    LearningSessionManager.Instance.EndSession();
                }
            }
        }
    }
}

