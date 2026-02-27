using UnityEngine;

public class SafeArea : MonoBehaviour
{
    RectTransform rectTransform;
    Rect lastSafeArea = new Rect(0, 0, 0, 0);

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        Refresh();
    }

    void Update()
    {
        if (lastSafeArea != Screen.safeArea)
        {
            Refresh();
        }
    }

    void Refresh()
    {
        if (rectTransform == null) return;
        
        Rect safeArea = Screen.safeArea;
        
        // Only update if safe area changed
        if (safeArea != lastSafeArea)
        {
            lastSafeArea = safeArea;
            ApplySafeArea(safeArea);
        }
    }

    void ApplySafeArea(Rect r)
    {
        // Check if we have a valid screen size
        if (Screen.width == 0 || Screen.height == 0) return;

        // Convert safe area rectangle from absolute pixels to normalized anchor coordinates
        Vector2 anchorMin = r.position;
        Vector2 anchorMax = r.position + r.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        // Apply to RectTransform
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        
        // Reset offsets to zero to snap to the anchors
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
