# Android Landscape Mode - Universal Compatibility Guide

## Overview
This guide ensures your game works perfectly in landscape mode on all Android devices with different screen sizes and aspect ratios.

## Step 1: Set Project to Landscape Only

1. Go to **Edit → Project Settings**
2. Select **Player** from the left sidebar
3. Click the **Android tab** (Android robot icon)
4. Scroll down to **Resolution and Presentation** section
5. Find **Default Orientation**
6. Set to **Landscape Left** (or **Auto Rotation** if you want both landscape orientations)
7. If using Auto Rotation, uncheck:
   - ❌ Portrait
   - ❌ Portrait Upside Down
   - ✅ Landscape Left
   - ✅ Landscape Right

## Step 2: Configure Canvas Scaler for All Screens

For EVERY scene with a Canvas:

1. Select the **Canvas** in Hierarchy
2. In Inspector, find **Canvas Scaler** component
3. Set these values:
   - **UI Scale Mode**: Scale With Screen Size
   - **Reference Resolution**: 1920 x 1080 (landscape standard)
   - **Screen Match Mode**: Match Width Or Height
   - **Match**: 0.5 (balances between width and height)

## Step 3: Safe Area Support (Already Done!)

Your `SafeAreaHandler.cs` already handles:
- ✅ Notches (like Samsung, iPhone with notch)
- ✅ Camera cutouts
- ✅ Navigation bars
- ✅ Different aspect ratios

Make sure SafeAreaHandler is attached to:
- Canvas or UI panels in each scene
- Any full-screen UI elements

## Step 4: Test Different Aspect Ratios

In Unity Editor, test these common Android landscape ratios:

1. Go to **Game** view
2. Click the dropdown that says "Free Aspect"
3. Test with these:
   - **16:9** (1920x1080) - Most common
   - **16:10** (1920x1200) - Tablets
   - **18:9** (2160x1080) - Modern phones
   - **19.5:9** (2340x1080) - Newer phones
   - **21:9** (2560x1080) - Ultra-wide

## Step 5: Update All Canvas Settings

Run this checklist for ALL your scenes:

### Scenes to Check:
- ✅ login
- ✅ VideoLoadingScreen (or loadingscreen)
- ✅ NewMap (NEWMAP)
- ✅ classroom
- ✅ MC-Opening
- ✅ IndianaJonesMultipleChoice
- ✅ MC-Ending
- ✅ PotionMixingTrueFalse
- ✅ YN-Opening
- ✅ All other game scenes

### For Each Scene:
1. Open the scene
2. Select Canvas
3. Verify Canvas Scaler settings:
   - Reference Resolution: **1920 x 1080**
   - Match: **0.5**
4. Save scene

## Step 6: Anchor UI Elements Properly

For UI elements to work on all screen sizes:

### Buttons (Like Skip, Next, Exit):
- Use **Corner anchors** (Top-right, Bottom-right, etc.)
- Set position relative to anchor point
- Example: Skip button at bottom-right:
  - Anchor: Bottom Right
  - Pos X: -120 (120 pixels from right)
  - Pos Y: 60 (60 pixels from bottom)

### Full-Screen Images (Like backgrounds, video):
- Use **Stretch anchors** (all 4 corners)
- Click anchor preset → Hold Alt+Shift → Click bottom-right square
- Set all edges to 0: Left, Top, Right, Bottom = 0

### Text Panels (Like dialogue boxes):
- Anchor to appropriate screen edge
- Use **stretch horizontally** if text should span screen width
- Leave padding from edges (e.g., Left: 100, Right: 100)

## Step 7: Force Landscape in Code (Optional)

If you want to force landscape at runtime, add this script:

```csharp
using UnityEngine;

public class ForceLandscape : MonoBehaviour
{
    void Awake()
    {
        // Force landscape orientation
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
    }
}
```

Attach this to a GameObject in your first scene (login).

## Step 8: Android Build Settings

1. Go to **File → Build Settings**
2. Click **Player Settings**
3. Under **Android** tab:
   
   **Resolution and Presentation:**
   - Default Orientation: **Landscape Left**
   - Allowed Orientations for Auto Rotation:
     - Portrait: ❌ Unchecked
     - Portrait Upside Down: ❌ Unchecked
     - Landscape Right: ✅ Checked
     - Landscape Left: ✅ Checked
   
   **Other Settings:**
   - Minimum API Level: **Android 7.0 (API 24)** or higher
   - Target API Level: **Automatic (highest installed)**
   
   **Identification:**
   - Package Name: Set your unique package (e.g., com.yourname.studentacademic)
   - Version: 1.0
   - Bundle Version Code: 1

## Step 9: Test on Multiple Devices

### Small Screens (5-6 inch phones):
- Resolution: ~1920x1080 or 2340x1080
- Aspect: 16:9 or 18:9

### Medium Screens (6-7 inch phones):
- Resolution: ~2400x1080
- Aspect: 19.5:9 or 20:9

### Tablets (7-10 inches):
- Resolution: ~1920x1200 or 2560x1600
- Aspect: 16:10

### Test Checklist:
- [ ] All UI elements visible
- [ ] Text readable (not too small)
- [ ] Buttons clickable (not too small)
- [ ] Videos display properly
- [ ] No UI elements cut off
- [ ] Safe areas respected (notches, nav bars)

## Step 10: Common Issues and Fixes

### UI elements cut off on some devices:
- **Fix**: Use Safe Area Handler on Canvas
- Make sure all UI has proper margins from screen edges

### Text too small on tablets:
- **Fix**: Increase Canvas Scaler Match value to 0.6-0.7
- Or use TextMeshPro's Auto Size feature

### Video doesn't fill screen on ultra-wide:
- **Fix**: Set Video Player's Aspect Ratio to "Fit Horizontally"
- Or use Raw Image with "Preserve Aspect" unchecked

### Buttons too close to notch:
- **Fix**: Add Safe Area Handler to Canvas
- Or manually add padding in RectTransform

### Different orientations between scenes:
- **Fix**: Add ForceLandscape script to persistent GameObject
- Or check Default Orientation in Player Settings

## Best Practices

1. **Always use Canvas Scaler** with Reference Resolution
2. **Always anchor UI properly** - don't use absolute positions
3. **Test in Game view** with different aspect ratios
4. **Add Safe Area support** for modern devices with notches
5. **Use TextMeshPro** for better text scaling
6. **Avoid pixel-perfect positioning** - use percentages/anchors instead

## Unity Game View Test Setup

1. In Game view, click dropdown → **+** (Add custom size)
2. Add these test resolutions:
   - **Android 16:9**: 1920 x 1080
   - **Android 18:9**: 2160 x 1080
   - **Android 19.5:9**: 2340 x 1080
   - **Android 21:9**: 2560 x 1080
   - **Tablet 16:10**: 1920 x 1200

3. Test your scenes by switching between these resolutions

## Final Checklist

- [ ] Player Settings → Default Orientation set to Landscape
- [ ] All Canvas Scalers configured (1920x1080, Match 0.5)
- [ ] All UI elements properly anchored
- [ ] Safe Area Handler attached where needed
- [ ] Tested in multiple aspect ratios in Game view
- [ ] Portrait modes disabled in Player Settings
- [ ] Build tested on at least 2 different Android devices
- [ ] All scenes maintain landscape orientation
- [ ] No UI elements cut off or too small
- [ ] Videos display correctly in landscape

## Recommended Screen Support

Your game should support:
- ✅ **Minimum**: 5 inch phones (16:9 ratio)
- ✅ **Maximum**: 10 inch tablets (16:10 ratio)
- ✅ **All modern ratios**: 16:9, 18:9, 19.5:9, 20:9, 21:9
- ✅ **Notch/cutout devices**: Samsung, Xiaomi, OnePlus, etc.

## Notes

- Landscape Left = Home button on LEFT side
- Landscape Right = Home button on RIGHT side
- Auto Rotation allows device to rotate between left/right
- Most games use Landscape Left as default
- Unity's Safe Area API automatically handles notches
- Canvas Scaler Match of 0.5 balances width and height scaling
