using UnityEngine;

namespace ComputerLearning
{
    public class AdaptiveSessionStarter : MonoBehaviour
    {
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
                Transform contentTr = transform.Find("WindowContents");
                if (contentTr != null)
                {
                    taskContentArea = contentTr.GetComponent<RectTransform>();
                }
            }

            StartCoroutine(TutorialSequence(window));
        }

        private System.Collections.IEnumerator TutorialSequence(Window window)
        {
            RectTransform target = window != null ? window.GetComponent<RectTransform>() : taskContentArea;
            
            // Mini tutorial message
            VirtualMascot.Show("Welcome to Level 4!\nKeep practicing to improve your skills.", target, new Vector2(250, -100));
            
            yield return new WaitForSeconds(4.0f);
            if (window != null && window.IsClosed) { VirtualMascot.HideMascot(); yield break; }
            
            VirtualMascot.Show("I will give you tasks based on what you need to learn.\nGood luck!", target, new Vector2(250, -100));
            
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
                Transform closeBtn = window.transform.Find("WindowTop/Buttons/Close");
                if (closeBtn == null) closeBtn = window.transform.Find("WindowTop"); 
                
                if (closeBtn != null)
                {
                    VirtualMascot.Show("Great practice!\nYou can click the 'X' to close this window now.", closeBtn.GetComponent<RectTransform>(), new Vector2(-200, -80));
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
