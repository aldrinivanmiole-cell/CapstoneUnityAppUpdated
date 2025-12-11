using UnityEngine;

public class ForceLandscapeOrientation : MonoBehaviour
{
    void Awake()
    {
        // Force landscape orientation
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        
        // Disable auto-rotation
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        
        Debug.Log("✅ Forced landscape orientation");
    }
}
