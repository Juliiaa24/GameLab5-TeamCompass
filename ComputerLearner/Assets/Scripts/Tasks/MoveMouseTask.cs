using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace ComputerLearning
{
    public class MoveMouseTask : BaseTask
    {
        [Header("Visual Settings")]
        [Tooltip("How many spots the user has to hover over to clean the window.")]
        public int spotsToSpawn = 24;
        
        private int spotsCleaned = 0;
        private RectTransform contentArea;
        private Image progressBarFill;
        private float lastCleanedTime;
        private bool hasShownBetweenReminder = false;
        private readonly List<RectTransform> activeSpots = new List<RectTransform>();

        protected override RectTransform GetActiveFeedbackTarget()
        {
            for (int i = 0; i < activeSpots.Count; i++)
            {
                if (activeSpots[i] != null && activeSpots[i].gameObject.activeInHierarchy)
                {
                    return activeSpots[i];
                }
            }
            return contentArea != null ? contentArea : (transform as RectTransform);
        }
        
        protected override void SetupTask(TaskDefinition definition)
        {
            contentArea = GetComponent<RectTransform>();
            lastCleanedTime = Time.time;
            hasShownBetweenReminder = false;
            activeSpots.Clear();
            CreateProgressBar();
            SpawnDirtSpots();
            SetupBackgroundClickInterceptor(OnBackgroundClicked);
        }

        protected override void Update()
        {
            base.Update();
            if (IsFinished) return;

            // Check if player spends > 4.5s without cleaning a dirt spot
            if (!hasShownBetweenReminder && (Time.time - lastCleanedTime) >= 4.5f && spotsCleaned < spotsToSpawn)
            {
                hasShownBetweenReminder = true;
                VirtualMascot.ShowContextualFeedback(VirtualMascot.FeedbackDialogs.moveKeepMoving, GetActiveFeedbackTarget(), 2.8f);
            }
        }

        public void OnBackgroundClicked(PointerEventData eventData)
        {
            if (IsFinished) return;
            RectTransform targetAnchor = GetActiveFeedbackTarget();
            RegisterAttempt(true, targetAnchor);
            VirtualMascot.ShowContextualFeedback(VirtualMascot.FeedbackDialogs.moveNoClick, targetAnchor, 2.8f);
        }

        public void OnSpotClicked(Transform spotTransform = null)
        {
            if (IsFinished) return;
            Transform targetAnchor = spotTransform != null ? spotTransform : GetActiveFeedbackTarget();
            RegisterAttempt(true, targetAnchor);
            VirtualMascot.ShowContextualFeedback(VirtualMascot.FeedbackDialogs.moveNoClick, targetAnchor as RectTransform, 2.8f);
        }

        private void CreateProgressBar()
        {
            // Create a simple UI progress bar at the top
            GameObject bgObj = new GameObject("ProgressBg", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(contentArea, false);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.1f, 0.9f);
            bgRect.anchorMax = new Vector2(0.9f, 0.95f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            bgObj.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);
            
            GameObject fillObj = new GameObject("ProgressFill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(bgObj.transform, false);
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0, 0);
            fillRect.anchorMax = new Vector2(0, 1); // Starts at 0 width
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            
            progressBarFill = fillObj.GetComponent<Image>();
            progressBarFill.color = new Color(0.2f, 0.8f, 0.2f, 1f); // Green
        }

        private void SpawnDirtSpots()
        {
            if (contentArea.rect.width <= 100f || contentArea.rect.height <= 100f)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentArea);
            }
            // Get content area size with safe interior margin, fallback to 600x400 if layout isn't calculated yet
            float rawWidth = contentArea.rect.width > 100f ? contentArea.rect.width : 600f;
            float rawHeight = contentArea.rect.height > 100f ? contentArea.rect.height : 400f;
            float width = Mathf.Max(200f, rawWidth - 80f);
            float height = Mathf.Max(160f, rawHeight - 60f);
            
            // We reserve the top 15% for the progress bar
            float availableHeight = height * 0.8f;
            
            // Grid calculation
            int cols = 6;
            int rows = Mathf.CeilToInt((float)spotsToSpawn / cols);
            
            float cellWidth = width / cols;
            float cellHeight = availableHeight / rows;
            
            int spawned = 0;
            
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (spawned >= spotsToSpawn) break;
                    
                    GameObject spotObj = new GameObject("DirtSpot_" + spawned, typeof(RectTransform), typeof(Image), typeof(DirtSpot), typeof(ComputerLearning.CircleHitbox));
                    spotObj.transform.SetParent(contentArea, false);
                    
                    RectTransform rect = spotObj.GetComponent<RectTransform>();
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    
                    // Calculate grid position relative to center
                    float startX = -width / 2f + cellWidth / 2f;
                    float startY = availableHeight / 2f - cellHeight / 2f - (height * 0.1f); // Offset down a bit
                    
                    float x = startX + (c * cellWidth);
                    float y = startY - (r * cellHeight);
                    
                    // Add some random noise so it doesn't look like a perfect grid
                    x += Random.Range(-cellWidth * 0.2f, cellWidth * 0.2f);
                    y += Random.Range(-cellHeight * 0.2f, cellHeight * 0.2f);
                    
                    rect.anchoredPosition = new Vector2(x, y);
                    rect.sizeDelta = new Vector2(40, 40);
                    
                    // Visuals
                    Image img = spotObj.GetComponent<Image>();
                    img.color = new Color(0.6f, 0.4f, 0.2f, 0.8f); // Dirt brown
                    
                    // Logic
                    DirtSpot spotLogic = spotObj.GetComponent<DirtSpot>();
                    spotLogic.task = this;
                    activeSpots.Add(rect);
                    
                    spawned++;
                }
            }
        }
        
        public void OnSpotCleaned(GameObject spot)
        {
            if (IsFinished) return;
            
            NotifyUserInteraction();
            lastCleanedTime = Time.time;
            hasShownBetweenReminder = false;

            if (spot != null)
            {
                RectTransform spotRt = spot.GetComponent<RectTransform>();
                if (spotRt != null) activeSpots.Remove(spotRt);
                TriggerHitScoreFeedback(spot.transform, 15);
            }
            
            spotsCleaned++;
            float progress = (float)spotsCleaned / spotsToSpawn;
            
            if (progressBarFill != null)
            {
                RectTransform fillRect = progressBarFill.GetComponent<RectTransform>();
                fillRect.anchorMax = new Vector2(progress, 1);
            }
            
            if (spotsCleaned >= spotsToSpawn)
            {
                hasSpawnedCompletionScorePopup = true;
                Complete(true);
            }
        }
    }
    
    public class DirtSpot : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        public MoveMouseTask task;
        private bool isCleaned = false;
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isCleaned) return;
            isCleaned = true;
            task.OnSpotCleaned(gameObject);
            
            // Simple visual feedback: destroy the dirt spot
            Destroy(gameObject);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isCleaned) return;
            task?.OnSpotClicked(transform);
        }
    }
}
