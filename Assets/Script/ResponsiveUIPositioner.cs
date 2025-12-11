using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ensures UI elements maintain proper position across different screen resolutions
/// Attach this to UI elements that need to stay in fixed positions
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ResponsiveUIPositioner : MonoBehaviour
{
    [Header("Anchor Presets")]
    [Tooltip("Where should this element be anchored?")]
    public AnchorPreset anchorPreset = AnchorPreset.TopCenter;
    
    [Header("Offset from Anchor")]
    [Tooltip("Distance from the anchor point in pixels")]
    public Vector2 offsetFromAnchor = Vector2.zero;
    
    [Header("Auto Apply")]
    [Tooltip("Apply positioning automatically on start")]
    public bool applyOnStart = true;
    
    private RectTransform rectTransform;
    
    public enum AnchorPreset
    {
        TopLeft,
        TopCenter,
        TopRight,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }
    
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        
        if (applyOnStart)
        {
            ApplyAnchorPreset();
        }
    }
    
    public void ApplyAnchorPreset()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
        
        // Set anchor based on preset
        switch (anchorPreset)
        {
            case AnchorPreset.TopLeft:
                rectTransform.anchorMin = new Vector2(0, 1);
                rectTransform.anchorMax = new Vector2(0, 1);
                rectTransform.pivot = new Vector2(0, 1);
                break;
                
            case AnchorPreset.TopCenter:
                rectTransform.anchorMin = new Vector2(0.5f, 1);
                rectTransform.anchorMax = new Vector2(0.5f, 1);
                rectTransform.pivot = new Vector2(0.5f, 1);
                break;
                
            case AnchorPreset.TopRight:
                rectTransform.anchorMin = new Vector2(1, 1);
                rectTransform.anchorMax = new Vector2(1, 1);
                rectTransform.pivot = new Vector2(1, 1);
                break;
                
            case AnchorPreset.MiddleLeft:
                rectTransform.anchorMin = new Vector2(0, 0.5f);
                rectTransform.anchorMax = new Vector2(0, 0.5f);
                rectTransform.pivot = new Vector2(0, 0.5f);
                break;
                
            case AnchorPreset.MiddleCenter:
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                break;
                
            case AnchorPreset.MiddleRight:
                rectTransform.anchorMin = new Vector2(1, 0.5f);
                rectTransform.anchorMax = new Vector2(1, 0.5f);
                rectTransform.pivot = new Vector2(1, 0.5f);
                break;
                
            case AnchorPreset.BottomLeft:
                rectTransform.anchorMin = new Vector2(0, 0);
                rectTransform.anchorMax = new Vector2(0, 0);
                rectTransform.pivot = new Vector2(0, 0);
                break;
                
            case AnchorPreset.BottomCenter:
                rectTransform.anchorMin = new Vector2(0.5f, 0);
                rectTransform.anchorMax = new Vector2(0.5f, 0);
                rectTransform.pivot = new Vector2(0.5f, 0);
                break;
                
            case AnchorPreset.BottomRight:
                rectTransform.anchorMin = new Vector2(1, 0);
                rectTransform.anchorMax = new Vector2(1, 0);
                rectTransform.pivot = new Vector2(1, 0);
                break;
        }
        
        // Apply offset
        rectTransform.anchoredPosition = offsetFromAnchor;
    }
    
    // Helper method to set position from code
    public void SetPosition(AnchorPreset preset, Vector2 offset)
    {
        anchorPreset = preset;
        offsetFromAnchor = offset;
        ApplyAnchorPreset();
    }
}
