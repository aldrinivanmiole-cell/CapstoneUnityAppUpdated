# Landscape Mode & Auto-Uppercase Setup

## Part 1: Force Landscape Orientation on Android

### Step 1: Add ForceLandscapeOrientation Script to Login Scene
1. Open **login** scene in Unity
2. In Hierarchy, find or create a GameObject (e.g., "GameManager" or create new empty GameObject)
3. Rename it to **"OrientationController"**
4. Select it, click **Add Component**
5. Search for **"Force Landscape Orientation"** and add it
6. This GameObject will persist across scenes (DontDestroyOnLoad is automatic)

### Step 2: Configure Player Settings
1. Go to **Edit → Project Settings**
2. Select **Player** from left sidebar
3. Click **Android** tab (robot icon)
4. Under **Resolution and Presentation**:
   - **Default Orientation**: Landscape Left
   - **Allowed Orientations for Auto Rotation**:
     - Portrait: ❌ Unchecked
     - Portrait Upside Down: ❌ Unchecked  
     - Landscape Left: ✅ Checked
     - Landscape Right: ✅ Checked

### How It Works:
- The script forces landscape mode when the app starts
- It prevents rotation to portrait modes
- Allows rotation between landscape left and right only
- Works automatically on Android devices

## Part 2: Auto-Uppercase Class Code Input

### Already Implemented!
The ClassroomManager script has been updated to:
- ✅ Automatically convert class code input to UPPERCASE as you type
- ✅ Only allow alphanumeric characters (letters and numbers)
- ✅ Maintain cursor position after conversion

### How It Works:
1. When user types in the class code field
2. Each character is immediately converted to uppercase
3. Invalid characters are blocked
4. User sees uppercase text in real-time

### Testing:
1. Open **classroom** scene
2. Play the scene
3. Try typing in the class code field
4. Type "abc123" → Should automatically show "ABC123"
5. Special characters like !@#$ should be blocked

## Complete Setup Checklist

### Landscape Mode:
- [ ] ForceLandscapeOrientation.cs created in Assets/Script/
- [ ] Script added to GameObject in login scene
- [ ] Player Settings → Default Orientation set to Landscape Left
- [ ] Portrait modes disabled in Player Settings
- [ ] Tested in Unity Editor (rotate Game view to check)
- [ ] Built APK and tested on device

### Auto-Uppercase Class Code:
- [ ] ClassroomManager.cs updated with ConvertToUppercase method
- [ ] classCodeInput listener added in Start()
- [ ] Tested typing lowercase letters (should auto-convert)
- [ ] Tested special characters (should be blocked)
- [ ] Built APK and tested on device

## Testing on Device

### Test Landscape Lock:
1. Install APK on Android device
2. Open the app
3. Try rotating device to portrait → Should stay in landscape
4. Rotate between landscape left/right → Should follow rotation
5. All scenes should remain in landscape mode

### Test Auto-Uppercase:
1. Open app and login
2. Navigate to classroom scene (NEWMAP → Click classroom)
3. Click "Add Class" or wherever class code input appears
4. Type lowercase letters: "test123"
5. Should immediately show: "TEST123"
6. Try typing special characters → Should be ignored

## Troubleshooting

### App doesn't lock to landscape:
- Check Player Settings → Default Orientation
- Verify ForceLandscapeOrientation script is in first scene
- Check Console for "✅ Forced landscape orientation" message
- Make sure portrait modes are unchecked in Player Settings

### Uppercase doesn't work:
- Verify ClassroomManager has the ConvertToUppercase method
- Check that classCodeInput is assigned in Inspector
- Look for errors in Console
- Make sure you're testing on the correct input field

### Rotation still happens:
- Check Screen.autorotateToPortrait is false
- Verify both portrait modes are disabled in Player Settings
- Test on actual device (Editor may behave differently)

## Additional Notes

- Landscape lock applies to ALL scenes automatically
- Auto-uppercase only applies to class code input field
- Both features work immediately after building APK
- No additional configuration needed per scene
- Features persist across app restarts
- Compatible with all Android versions (API 24+)

## Build Settings Reminder

Before building APK:
1. **File → Build Settings**
2. Ensure all scenes are added
3. **Player Settings → Android**:
   - Minimum API Level: 24 (Android 7.0)
   - Default Orientation: Landscape Left
4. Click **Build And Run**
5. Test on device!
