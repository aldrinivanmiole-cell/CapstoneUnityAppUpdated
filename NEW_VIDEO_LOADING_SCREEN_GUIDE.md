# Create New Video Loading Screen - Complete Guide

## Step 1: Create New Scene
1. In Unity, go to **File → New Scene**
2. Choose **Basic (Built-in)** or **Basic (URP)** (match your project)
3. Save as **"VideoLoadingScreen"** in `Assets/Scenes/` folder

## Step 2: Setup Canvas
1. Right-click in Hierarchy → **UI → Canvas**
2. Select Canvas, in Inspector:
   - **Render Mode**: Screen Space - Overlay
   - **Canvas Scaler → UI Scale Mode**: Scale With Screen Size
   - **Reference Resolution**: 1920 x 1080
   - **Match**: 0.5

## Step 3: Delete Video Display (Not needed for Camera Far Plane)
1. In Hierarchy, find and **delete** the **VideoDisplay** Raw Image (if you created it)
2. We'll use Camera Far Plane instead - simpler and works better on Android

## Step 4: Delete Render Texture (Not needed for Camera Far Plane)
1. In Project window, find and **delete** the **LoadingVideoTexture** (if you created it)
2. Camera Far Plane doesn't need a Render Texture

## Step 5: Create Skip Button (Optional)
1. Right-click Canvas → **UI → Button - TextMeshPro**
2. Rename to **"SkipButton"**
3. In Inspector:
   - **Anchor Preset**: Bottom Right
   - **Pos X**: -120
   - **Pos Y**: 60
   - **Width**: 200
   - **Height**: 60
   - Button **Color**: White with slight transparency (A: 200)
4. Select child **Text (TMP)**:
   - Text: **"SKIP ►"**
   - **Font Size**: 32
   - **Color**: Black or Dark Gray
   - **Alignment**: Center (horizontal and vertical)
   - **Font Style**: Bold

## Step 7: Import Your Videos FIRST
1. Drag both **intromale.mp4** and **introfemal.mp4** into Unity's **Assets** folder
2. Select each video file in Project window
3. In Inspector (for each video):
   - **Transcode**: Check this if video doesn't play
   - Click **Apply**
4. Both videos should now appear in your Assets folder

## Step 8: Create Video Player GameObject
1. Right-click in Hierarchy → **Create Empty**
2. Rename to **"VideoManager"**
3. In Inspector, click **Add Component**
4. Search for **"Video Player"** and add it
5. Click **Add Component** again
6. Search for **"Video Loading Screen"** and add it

## Step 9: Configure Video Player
Select **VideoManager**, in Inspector find **Video Player** component:

1. **Source**: **Video Clip**
2. **Video Clip**: Leave empty (will be set by script based on gender)
3. **Render Mode**: **Camera Far Plane**
4. **Camera**: Drag **Main Camera** from Hierarchy here
5. **Play On Awake**: ✓ **Checked**
6. **Wait For First Frame**: ✓ **Checked**
7. **Loop**: **Unchecked** (plays once then loads next scene)
8. **Skip On Drop**: ✓ **Checked**
9. **Playback Speed**: 1
10. **Audio Output Mode**: Direct (if video has audio)
11. **Aspect Ratio**: Fit Vertically

## Step 10: Configure VideoLoadingScreen Script
Select **VideoManager**, in Inspector find **Video Loading Screen (Script)**:

1. **Video Player**: Should auto-fill (or drag Video Player component)
2. **Male Intro Video**: From Project window, drag **intromale** video file here
3. **Female Intro Video**: From Project window, drag **introfemal** video file here
4. **Scene To Load**: Type **"NEWMAP"**
5. **Skip Button**: Drag **SkipButton** from Hierarchy Canvas

## Step 11: Update LoginManager to Use New Scene
The code is already updated! LoginManager now loads **"loadingscreen"**

You have two options:
- **Option A**: Delete old loadingscreen.unity and rename VideoLoadingScreen.unity to loadingscreen.unity
- **Option B**: Keep VideoLoadingScreen.unity and update LoginManager

### Option A (Recommended):
1. In Project window, go to `Assets/Scenes/`
2. Delete **loadingscreen.unity**
3. Delete **loadingscreenF.unity**
4. Rename **VideoLoadingScreen.unity** to **loadingscreen.unity**

### Option B:
Update LoginManager.cs line 122 to use the new scene name.

## Step 12: Verify Video Assignment
In the VideoManager Inspector, you should now see:
- **Male Intro Video**: intromale (VideoClip)
- **Female Intro Video**: introfemal (VideoClip)

If the videos don't appear:
1. Make sure they're imported into Unity (visible in Project window)
2. Try dragging from the **Project** window (not from Windows Explorer)
3. Videos should show as VideoClip assets in Unity

## Step 13: Add Scene to Build Settings
1. Go to **File → Build Settings**
2. If old loading screens exist in the list:
   - Select **loadingscreenF** → Click **Remove**
3. Click **Add Open Scenes** (to add VideoLoadingScreen)
4. Scene order should be:
   - login
   - **VideoLoadingScreen** (or loadingscreen)
   - NEWMAP
   - classroom
   - (other scenes)

## Step 14: Test Everything
1. Save scene (Ctrl+S)
2. Save project (Ctrl+S again)
3. Go to **login** scene
4. Press Play
5. Login with a **male** student account
   - Should play **intromale.mp4** video
6. Test again with a **female** student account
   - Should play **introfemal.mp4** video
7. After video ends, should load NEWMAP
8. Test skip button (if added)

## Complete Scene Structure
```
Hierarchy:
├── Canvas
│   ├── VideoDisplay (Raw Image with LoadingVideoTexture)
│   ├── SkipButton (Button - Optional)
│   └── [Any other UI elements]
├── VideoManager (GameObject)
│   ├── Video Player (Component)
│   └── Video Loading Screen Script (Component)
├── Main Camera
└── EventSystem (auto-created with Canvas)
```

## Troubleshooting

### Video doesn't play:
- Check Video Player has video clip assigned
- Check Render Texture is assigned to Target Texture
- Check Raw Image has the Render Texture assigned
- Try checking "Transcode" on video import settings

### Black screen:
- Verify Raw Image is stretched to full screen (all edges = 0)
- Check Render Texture size matches video resolution
- Ensure Video Player Render Mode is "Render Texture"

### Scene doesn't load after video:
- Check "Scene To Load" is exactly "NEWMAP" (case-sensitive)
- Verify NEWMAP is in Build Settings
- Check Console for errors

### Skip button doesn't work:
- Verify button is assigned in VideoLoadingScreen script
- Check EventSystem exists in scene
- Make sure button has Button component

### Video is choppy:
- Enable "Skip On Drop" in Video Player
- Reduce video resolution to 1080p or 720p
- Compress video file size

## Video Recommendations
- **Format**: MP4 (H.264 codec)
- **Resolution**: 1920x1080 (Full HD) or 1280x720 (HD)
- **Duration**: 3-8 seconds
- **File Size**: Under 30MB
- **Frame Rate**: 30fps
- **Bitrate**: 5-10 Mbps

## Optional Enhancements

### Add Loading Text:
1. Right-click Canvas → **UI → Text - TextMeshPro**
2. Rename to **"LoadingText"**
3. Set text to **"LOADING..."**
4. Position at bottom center
5. Make it pulse with animation (optional)

### Add Progress Bar:
1. Right-click Canvas → **UI → Slider**
2. Remove handle
3. Animate fill amount based on video time

### Add Fade In/Out:
1. Right-click Canvas → **UI → Image**
2. Rename to **"FadeOverlay"**
3. Stretch full screen
4. Color: Black, Alpha: 0
5. Animate alpha in VideoLoadingScreen script

## Cleanup After Setup
Once new scene is working:
1. Delete **Assets/Scenes/loadingscreen.unity** (old)
2. Delete **Assets/Scenes/loadingscreenF.unity** (old)
3. Delete **Assets/Script/cutscene.cs** (if not used elsewhere)
4. Remove old scenes from Build Settings

## Final Checklist
- [ ] New scene created and saved as VideoLoadingScreen.unity
- [ ] Canvas setup with proper scaling
- [ ] Raw Image stretched full screen
- [ ] Render Texture created (1920x1080)
- [ ] Render Texture assigned to both Video Player and Raw Image
- [ ] MP4 video imported and assigned to Video Player
- [ ] VideoLoadingScreen script configured
- [ ] Scene To Load set to "NEWMAP"
- [ ] Skip button setup (optional)
- [ ] Scene renamed to loadingscreen.unity OR LoginManager updated
- [ ] Old loading screens deleted
- [ ] Build Settings updated
- [ ] Tested: login → video → NEWMAP

## Notes
- Video plays automatically when scene loads
- Video plays once (no loop)
- When video ends, automatically loads NEWMAP
- Skip button immediately loads NEWMAP
- **Male students** see **intromale.mp4**
- **Female students** see **introfemal.mp4**
- Gender is detected automatically from SessionManager
- Original Timeline-based loading screens are replaced
- Only ONE scene needed (handles both genders)
