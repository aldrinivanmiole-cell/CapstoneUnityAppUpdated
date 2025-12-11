using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Automatically configures Canvas for optimal mobile display.
/// This script ensures your UI looks correct on all devices.
/// Add this to a GameObject in your scene (it will find the Canvas automatically).
/// </summary>
public class AutoConfigureMobileUI : MonoBehaviour
{
    [Header("Canvas Configuration")]
    [Tooltip("Target resolution for UI design (portrait mode)")]
    public Vector2 referenceResolution = new Vector2(1080, 1920);
    
    [Tooltip("0 = Match Width, 1 = Match Height, 0.5 = Balance")]
    [Range(0f, 1f)]
    public float matchWidthOrHeight = 0.5f;

    [Header("Auto-Apply Settings")]
    [Tooltip("Automatically apply safe area to main panel")]
    public bool autoApplySafeArea = true;
    
    [Tooltip("Apply on every scene load")]
    public bool applyOnSceneLoad = true;

    void Awake()
    {
        ConfigureCanvas();
        
        if (autoApplySafeArea)
        {
            ApplySafeAreaToMainPanel();
        }
    }

    void ConfigureCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        
        foreach (Canvas canvas in canvases)
        {
            // Configure Canvas
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            // Configure Canvas Scaler
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = matchWidthOrHeight;
            scaler.referencePixelsPerUnit = 100;

            // Configure Graphic Raycaster (for touch input)
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
            }

            Debug.Log($"[AutoConfigureMobileUI] Configured Canvas: {canvas.gameObject.name}");
        }
    }

    void ApplySafeAreaToMainPanel()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        // Look for main panels or containers
        Transform[] allTransforms = canvas.GetComponentsInChildren<Transform>(true);
        
        foreach (Transform t in allTransforms)
        {
            // Apply to panels or containers
            if (t.name.ToLower().Contains("panel") || 
                t.name.ToLower().Contains("container") ||
                t.name.ToLower().Contains("main") ||
                t == canvas.transform)
            {
                SafeAreaHandler safeArea = t.GetComponent<SafeAreaHandler>();
                if (safeArea == null)
                {
                    safeArea = t.gameObject.AddComponent<SafeAreaHandler>();
                    Debug.Log($"[AutoConfigureMobileUI] Added SafeAreaHandler to: {t.name}");
                }
            }
        }
    }

    // Helper method to fix common UI issues
    public void FixCommonUIIssues()
    {
        // Fix all Images to preserve aspect ratio
        Image[] images = FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Image img in images)
        {
            if (img.sprite != null && !img.preserveAspect)
            {
                img.preserveAspect = true;
            }
        }

        // Ensure all Buttons have proper transitions
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button btn in buttons)
        {
            if (btn.transition == Selectable.Transition.None)
            {
                btn.transition = Selectable.Transition.ColorTint;
            }
        }

        Debug.Log("[AutoConfigureMobileUI] Fixed common UI issues");
    }

    [ContextMenu("Apply Configuration Now")]
    public void ApplyNow()
    {
        ConfigureCanvas();
        if (autoApplySafeArea)
        {
            ApplySafeAreaToMainPanel();
        }
        FixCommonUIIssues();
    }
}
