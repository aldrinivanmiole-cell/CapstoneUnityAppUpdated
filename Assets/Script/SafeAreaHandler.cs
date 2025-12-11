using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Safe Area handler for mobile devices with notches and different aspect ratios.
/// Attach this script to the main Canvas or any panel that needs safe area adjustment.
/// This script will automatically adjust UI elements to prevent overlapping with notches and system bars.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaHandler : MonoBehaviour
{
    [Header("Safe Area Settings")]
    [Tooltip("Apply safe area to this RectTransform")]
    public bool applySafeArea = true;
    
    [Tooltip("Add extra padding (in pixels) if needed")]
    public Vector2 extraPadding = Vector2.zero;

    private RectTransform rectTransform;
    private Rect lastSafeArea = new Rect(0, 0, 0, 0);
    private Vector2Int lastScreenSize = new Vector2Int(0, 0);
    private ScreenOrientation lastOrientation = ScreenOrientation.Portrait;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // Also ensure Canvas Scaler is properly configured
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            EnsureCanvasScalerSettings(canvas);
        }
        
        ApplySafeArea();
    }

    void Start()
    {
        // Apply again on Start to ensure all UI is loaded
        ApplySafeArea();
    }

    void Update()
    {
        // Check if safe area, screen size, or orientation has changed
        if (applySafeArea && 
            (lastSafeArea != Screen.safeArea || 
             lastScreenSize.x != Screen.width || 
             lastScreenSize.y != Screen.height ||
             lastOrientation != Screen.orientation))
        {
            ApplySafeArea();
        }
    }

    void ApplySafeArea()
    {
        if (!applySafeArea)
            return;

        Rect safeArea = Screen.safeArea;
        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        lastOrientation = Screen.orientation;

        // Convert safe area to normalized coordinates
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        // Apply extra padding
        anchorMin += extraPadding;
        anchorMax -= extraPadding;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        // Clamp to valid range
        anchorMin.x = Mathf.Clamp01(anchorMin.x);
        anchorMin.y = Mathf.Clamp01(anchorMin.y);
        anchorMax.x = Mathf.Clamp01(anchorMax.x);
        anchorMax.y = Mathf.Clamp01(anchorMax.y);

        // Apply to RectTransform
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;

        Debug.Log($"[SafeArea] Applied: {safeArea} | Screen: {Screen.width}x{Screen.height} | Orientation: {Screen.orientation}");
    }

    void EnsureCanvasScalerSettings(Canvas canvas)
    {
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        }

        // Configure for best mobile support
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920); // Portrait
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f; // Balance between width and height
        scaler.referencePixelsPerUnit = 100;

        Debug.Log("[SafeArea] Canvas Scaler configured for mobile devices");
    }

    // Call this from inspector or code to manually refresh
    [ContextMenu("Refresh Safe Area")]
    public void RefreshSafeArea()
    {
        ApplySafeArea();
    }
}
