using UnityEngine;
using System.Collections;

namespace ComputerLearning
{
    public class DesktopTour : MonoBehaviour
    {
    private IEnumerator Start()
    {
        // Give time for UI layout
        yield return new WaitForSeconds(0.5f);

        // Find icons dynamically
        GameObject level1Icon = GameObject.Find("Application Icon");
        GameObject statsIcon = GameObject.Find("Stats Icon");

        // Reparent Stats Icon if it was spawned at root (MCP workaround)
        if (statsIcon != null && statsIcon.transform.parent == null)
        {
            GameObject panel = GameObject.Find("Panel");
            if (panel != null)
            {
                statsIcon.transform.SetParent(panel.transform, false);
            }
        }

        // Wait a bit before starting the tour
        yield return new WaitForSeconds(1.0f);

        // 1. Point to Level 1
        if (level1Icon != null)
        {
            VirtualMascot.Show("Welcome!\nDouble-click this icon to play Level 1.", level1Icon.GetComponent<RectTransform>(), new Vector2(160, -80));
        }

        // Wait for 5 seconds
        yield return new WaitForSeconds(5.0f);

        // 2. Point to Stats
        if (statsIcon != null)
        {
            VirtualMascot.Show("This is the Stats menu.\nHere you can see your learning progress!", statsIcon.GetComponent<RectTransform>(), new Vector2(160, -80));
        }
        else
        {
            Debug.LogWarning("[DesktopTour] Stats Icon not found on desktop!");
        }

        // Leave it pointing to stats for a bit, then hide, or point back to level 1
        yield return new WaitForSeconds(5.0f);
        
        if (level1Icon != null)
        {
            VirtualMascot.Show("Let's tend the garden!\nOpen Level 1 to start.", level1Icon.GetComponent<RectTransform>(), new Vector2(160, -80));
        }
    }
}
}
