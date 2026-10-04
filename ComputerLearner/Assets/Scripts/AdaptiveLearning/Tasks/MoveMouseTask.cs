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
        
        protected override void SetupTask(TaskDefinition definition)
        {
            contentArea = GetComponent<RectTransform>();
            CreateProgressBar();
            SpawnDirtSpots();
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
            // Get content area size, fallback to 600x400 if layout isn't fully calculated yet
            float width = contentArea.rect.width > 100 ? contentArea.rect.width : 600f;
            float height = contentArea.rect.height > 100 ? contentArea.rect.height : 400f;
            
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
                    
                    GameObject spotObj = new GameObject("DirtSpot_" + spawned, typeof(RectTransform), typeof(Image), typeof(DirtSpot));
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
                    img.sprite = Resources.GetBuiltinResource<Sprite>("Knob.psd"); // Built-in circle sprite
                    
                    // Logic
                    DirtSpot spotLogic = spotObj.GetComponent<DirtSpot>();
                    spotLogic.task = this;
                    
                    spawned++;
                }
            }
        }
        
        public void OnSpotCleaned(GameObject spot)
        {
            if (IsFinished) return;
            
            spotsCleaned++;
            float progress = (float)spotsCleaned / spotsToSpawn;
            
            if (progressBarFill != null)
            {
                RectTransform fillRect = progressBarFill.GetComponent<RectTransform>();
                fillRect.anchorMax = new Vector2(progress, 1);
            }
            
            if (spotsCleaned >= spotsToSpawn)
            {
                Complete(true);
            }
        }
    }
    
    public class DirtSpot : MonoBehaviour, IPointerEnterHandler
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
    }
}
