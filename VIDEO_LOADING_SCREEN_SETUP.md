# Video Loading Screen Setup Guide

## Overview
This guide will help you replace the two gender-specific loading screens (loadingscreen.unity and loadingscreenF.unity) with a single unified video-based loading screen.

## What Changed
- **Before**: Two separate loading screens (male/female) using Timeline
- **After**: One unified loading screen playing an MP4 video

## Step 1: Prepare Your Video
1. Place your MP4 video file in `Assets/Resources/` or any folder in Assets
2. Recommended video specs:
   - **Resolution**: 1920x1080 (Full HD)
   - **Format**: MP4 (H.264 codec)
   - **Duration**: 3-10 seconds
   - **File size**: Under 50MB for best performance

## Step 2: Open the Loading Screen Scene
1. In Unity, open `Assets/Scenes/loadingscreen.unity`
2. We'll convert this scene to use video instead of Timeline

## Step 3: Remove Old Timeline Components
1. In Hierarchy, find and delete these objects (if they exist):
   - Cutscene Timeline GameObject
   - Playable Director
   - Any animation-related objects
2. Keep only:
   - Canvas (with UI elements)
   - EventSystem
   - Any skip button you have

## Step 4: Create Video Player Setup
1. Right-click in Hierarchy → **Create Empty**
2. Rename it to **"VideoLoadingScreen"**
3. With VideoLoadingScreen selected, click **Add Component**
4. Search for and add **"Video Player"**
5. Search for and add **"VideoLoadingScreen"** (the new script)

## Step 5: Configure Video Player Component
In the Inspector, find the **Video Player** component:

1. **Source**: Set to **"Video Clip"**
2. **Video Clip**: Drag your MP4 video file here
3. **Render Mode**: Set to **"Camera Far Plane"** or **"Render Texture"**
   - If using Camera Far Plane:
     - **Camera**: Drag your Main Camera from Hierarchy
   - If using Render Texture (better quality):
     - Create a Render Texture (Right-click in Assets → Create → Render Texture)
     - Assign it to **Target Texture**
     - Create a Raw Image in Canvas and assign the Render Texture to it
4. **Play On Awake**: **Checked** ✓
5. **Loop**: **Unchecked** (video plays once then loads next scene)
6. **Skip On Drop**: **Checked** (better performance)
7. **Playback Speed**: 1

## Step 6: Configure VideoLoadingScreen Script
In the Inspector, find the **Video Loading Screen (Script)** component:

1. **Video Player**: Drag the VideoPlayer component (or leave it to auto-detect)
2. **Scene To Load**: Enter **"NEWMAP"** (or your map scene name)
3. **Skip Button**: Drag your skip button from Canvas (if you have one)

## Step 7: Setup Canvas for Video Display (Option A - Camera Far Plane)
If using Camera Far Plane render mode:
1. Your video will render behind all UI elements automatically
2. Make sure Canvas has:
   - **Render Mode**: Screen Space - Overlay
   - **Sort Order**: 1 (so it renders above video)

## Step 7: Setup Canvas for Video Display (Option B - Render Texture)
If using Render Texture render mode:
1. Right-click Canvas → **UI → Raw Image**
2. Rename to **"VideoDisplay"**
3. Set **Rect Transform** to stretch full screen:
   - Click Anchor Preset → Hold Alt + Shift → Click bottom-right
   - Set Left, Top, Right, Bottom all to **0**
4. In **Raw Image** component:
   - **Texture**: Drag your Render Texture here
5. Move VideoDisplay to top of Canvas children (so it's behind other UI)

## Step 8: Optional - Setup Skip Button
If you want a skip button:
1. Right-click Canvas → **UI → Button - TextMeshPro**
2. Rename to **"SkipButton"**
3. Position it (e.g., bottom-right corner):
   - **Anchor Preset**: Bottom Right
   - **Pos X**: -100
   - **Pos Y**: 50
   - **Width**: 150
   - **Height**: 50
4. Change button text to **"SKIP"**
5. In VideoLoadingScreen Inspector, drag SkipButton to **Skip Button** field

## Step 9: Delete the Second Loading Screen Scene
Since we're now using only one loading screen:
1. In Unity Project window, go to `Assets/Scenes/`
2. Find **loadingscreenF.unity**
3. Right-click → **Delete**
4. Confirm deletion
5. The **loadingscreenF.unity.meta** file will also be deleted

## Step 10: Update Build Settings
1. Go to **File → Build Settings**
2. Find **loadingscreenF** in the scenes list
3. Select it and press **Delete** or click the **X** button
4. Keep **loadingscreen** in the build
5. Click **Close**

## Step 11: Test the Flow
1. Save all changes
2. Play from **login** scene
3. After successful login, you should see:
   - Your MP4 video plays automatically
   - Video plays once (no loop)
   - When video ends, automatically loads NEWMAP scene
   - If skip button exists, clicking it immediately loads NEWMAP

## Troubleshooting

### Video doesn't play:
- Check Video Player has the video clip assigned
- Verify "Play On Awake" is checked
- Check video format is MP4 (H.264)
- Try reimporting the video (Right-click → Reimport)

### Black screen instead of video:
- If using Camera Far Plane: Verify Camera is assigned
- If using Render Texture: Check Raw Image has the texture assigned
- Check video file is not corrupted

### Scene doesn't load after video:
- Verify "Scene To Load" is set to "NEWMAP"
- Check scene name matches exactly (case-sensitive)
- Verify scene is in Build Settings

### Video is too small/wrong aspect ratio:
- If using Render Texture: Check Raw Image is stretched to full screen
- If using Camera Far Plane: Adjust camera settings

### Skip button doesn't work:
- Verify button has Button component
- Check button is assigned in VideoLoadingScreen script
- Make sure EventSystem exists in scene

## Video Player Render Modes Explained

### Camera Far Plane (Simpler)
- **Pros**: Easy setup, no extra assets needed
- **Cons**: Renders behind everything, limited control
- **Best for**: Simple fullscreen video backgrounds

### Render Texture (Recommended)
- **Pros**: Better quality, more control, can be treated as UI element
- **Cons**: Requires creating Render Texture asset
- **Best for**: Professional-looking video displays with UI overlays

## Performance Tips
1. Keep video under 10 seconds for faster loading
2. Use 1080p resolution (1920x1080) or lower
3. Compress video with H.264 codec
4. Avoid 4K videos (too large)
5. Set "Skip On Drop" to true in Video Player

## Scene Structure
```
Hierarchy (loadingscreen scene):
├── Canvas (UI)
│   ├── VideoDisplay (Raw Image) - Optional, if using Render Texture
│   ├── SkipButton (Button) - Optional
│   └── Other UI elements
├── VideoLoadingScreen (GameObject)
│   ├── Video Player (Component)
│   └── Video Loading Screen Script (Component)
├── Main Camera
└── EventSystem
```

## Code Changes Summary
- ✅ Created new script: `VideoLoadingScreen.cs`
- ✅ Updated `LoginManager.cs` to load single scene
- ✅ Removed gender-based scene selection
- ⚠️ Delete `loadingscreenF.unity` scene manually
- ⚠️ Remove old `cutscene.cs` if not used elsewhere

## Final Checklist
- [ ] MP4 video imported into Assets
- [ ] loadingscreen.unity scene updated with Video Player
- [ ] VideoLoadingScreen script assigned and configured
- [ ] Video clip assigned to Video Player
- [ ] Scene To Load set to "NEWMAP"
- [ ] Render mode configured (Camera Far Plane or Render Texture)
- [ ] Skip button setup (optional)
- [ ] loadingscreenF.unity deleted
- [ ] Build Settings updated (removed loadingscreenF)
- [ ] Tested: login → video plays → loads NEWMAP

## Notes
- The old `cutscene.cs` script is still in your project but no longer used for loading screens
- You can keep it if other scenes use it, or delete it if not needed
- The new VideoLoadingScreen script is cleaner and focused on video playback
- Video will automatically loop back to start if "Loop" is checked (not recommended)
- For best user experience, keep video duration under 5-8 seconds
