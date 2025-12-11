# MC-Ending Scene Setup Guide

## Step 1: Create the Scene
1. In Unity, go to **File → New Scene**
2. Choose **Basic (Built-in)** or **Basic (URP)** (match your project setup)
3. Save the scene as **"MC-Ending"** in your `Assets/Scenes/` folder

## Step 2: Create Canvas Structure
1. Right-click in Hierarchy → **UI → Canvas**
2. Select the Canvas, in Inspector set:
   - **Render Mode**: Screen Space - Overlay
   - **Canvas Scaler → UI Scale Mode**: Scale With Screen Size
   - **Reference Resolution**: 1920 x 1080
   - **Match**: 0.5

## Step 3: Create Background
1. Right-click Canvas → **UI → Image**
2. Rename it to **"Background"**
3. Select Background, in Inspector find **Rect Transform** section
4. Click the **Anchor Preset** box (small square with target icon in Rect Transform)
5. **Hold Alt + Shift keys together**, then click the **bottom-right square in the preset grid**
   - This is the "stretch both" preset - stretches horizontally and vertically to fill screen
   - You'll see the square icon change to show arrows in 4 directions
6. In Rect Transform, verify these values:
   - **Left**: 0
   - **Top**: 0
   - **Right**: 0
   - **Bottom**: 0
7. In the **Image** component section:
   - **Color**: Click the color box, enter #3D2817 (dark brown temple color)
   - Or drag a temple background sprite into the **Source Image** field

## Step 4: Create Teacher Character Display
1. Right-click Canvas → **UI → Image**
2. Rename to **"TeacherImage"**
3. In Inspector:
   - **Anchor Preset**: Middle Left
   - **Pos X**: 300
   - **Pos Y**: 0
   - **Width**: 400
   - **Height**: 600
   - Assign your teacher character sprite
   - **Preserve Aspect**: Checked

## Step 5: Create Trophy Display
1. Right-click Canvas → **UI → Image**
2. Rename to **"TrophyImage"**
3. In Inspector:
   - **Anchor Preset**: Top Center
   - **Pos X**: 0
   - **Pos Y**: -150
   - **Width**: 300
   - **Height**: 300
   - **Color**: White (full alpha)
   - Assign gold trophy sprite (will change based on score)
   - **Preserve Aspect**: Checked

## Step 6: Create Dialogue Box
1. Right-click Canvas → **UI → Image**
2. Rename to **"DialoguePanel"**
3. In Inspector:
   - **Anchor Preset**: Bottom Center
   - **Pos X**: 0
   - **Pos Y**: 150
   - **Width**: 1600
   - **Height**: 250
   - **Color**: Dark with transparency (e.g., #1A1A1A, Alpha: 200)
   - Optional: Add **UI → Outline** component for border

## Step 7: Create Dialogue Text
1. Right-click DialoguePanel → **UI → Text - TextMeshPro**
2. Rename to **"DialogText"**
3. Select DialogText, in Inspector find **Rect Transform**
4. Click **Anchor Preset** box
5. **Hold Alt + Shift**, click **bottom-right square** (stretch both)
6. In Rect Transform, set padding from edges:
   - **Left**: 50
   - **Top**: 30
   - **Right**: 50
   - **Bottom**: 30
7. Scroll down to **TextMeshPro - Text (UI)** component:
   - **Font Asset**: Select a bold font (or keep default)
   - **Font Style**: Click **B** (Bold) button
   - **Font Size**: 36
   - **Vertex Color**: Click color, enter #FFD700 (gold) or use white
   - **Alignment**: Click center horizontal button, click center vertical button
   - **Wrapping**: Check **Enable** checkbox
   - **Overflow**: Dropdown select **Ellipsis**

## Step 8: Create Next Button
1. Right-click DialoguePanel → **UI → Button - TextMeshPro**
2. Rename to **"NextButton"**
3. In Inspector:
   - **Anchor Preset**: Bottom Right
   - **Pos X**: -100
   - **Pos Y**: -80
   - **Width**: 180
   - **Height**: 60
   - **Color**: Orange/Gold (#FFA500)
4. Select the child **Text (TMP)**, change text to **"NEXT"**
   - **Font Size**: 28
   - **Color**: White
   - **Alignment**: Center and Middle
   - **Font Style**: Bold

## Step 9: Create Exit Button
1. Duplicate NextButton (Ctrl+D)
2. Rename to **"ExitButton"**
3. In Inspector:
   - Keep same position and size
   - **Color**: Green (#4CAF50)
4. Select the child **Text (TMP)**, change text to **"EXIT"**
5. **Uncheck Active** in top-left of Inspector (button will be hidden initially)

## Step 10: Create Flash Overlay (for animations)
1. Right-click Canvas → **UI → Image**
2. Rename to **"FlashOverlay"**
3. Select FlashOverlay, in Inspector find **Rect Transform**
4. Click the **Anchor Preset** square (small target icon in top-left of Rect Transform)
5. **Hold Alt + Shift** keys, then click the **bottom-right square** (stretch both horizontal and vertical)
   - This makes the image fill the entire screen
   - The square will show arrows pointing in all 4 directions
6. Set these values in Rect Transform:
   - **Left**: 0
   - **Top**: 0
   - **Right**: 0
   - **Bottom**: 0
7. In the **Image** component below Rect Transform:
   - **Color**: Click color picker, set to White (#FFFFFF)
   - Drag the **Alpha slider** (A) to 0 (fully transparent initially)
8. In Hierarchy, **drag FlashOverlay to the bottom** of the Canvas children list (so it renders on top)
9. **Uncheck the checkbox** next to "FlashOverlay" name at top of Inspector (deactivates it, script will activate when needed)

## Step 11: Create Particle Effects
### Sparkle Effect (for good scores):
1. Right-click Canvas → **Effects → Particle System**
2. Rename to **"SparkleEffect"**
3. In Inspector, configure Particle System:
   - **Duration**: 2
   - **Looping**: Unchecked
   - **Start Lifetime**: 1.5
   - **Start Speed**: 50
   - **Start Size**: 20
   - **Start Color**: Gold (#FFD700)
   - **Emission → Rate over Time**: 50
   - **Shape**: Cone, Angle: 25, Radius: 0.1
   - Position near trophy (X: 0, Y: -150, Z: 0)
4. Stop it by default (uncheck **Play On Awake**)

### Glow Effect (for perfect scores):
1. Duplicate SparkleEffect
2. Rename to **"GlowEffect"**
3. In Inspector:
   - **Start Color**: Bright gold with glow (#FFEE00)
   - **Start Size**: 30
   - **Shape**: Sphere
   - Position around trophy
4. Stop it by default (uncheck **Play On Awake**)

## Step 12: Create Floating Stars
1. Right-click Canvas → **UI → Image**
2. Rename to **"Star1"**
3. In Inspector:
   - **Width**: 60, **Height**: 60
   - **Anchor Preset**: Top Right
   - **Pos X**: -200, **Pos Y**: -200
   - Assign a star sprite (or use solid color with gold)
4. Duplicate to create **Star2, Star3, Star4**
5. Position them around the scene:
   - Star2: Top Left (X: 200, Y: -200)
   - Star3: Middle Right (X: -150, Y: 100)
   - Star4: Middle Left (X: 150, Y: -100)
6. **Uncheck Active** for all stars (script will animate them)

## Step 13: Create Script GameObject
1. Right-click in Hierarchy → **Create Empty**
2. Rename to **"EndingManager"**
3. In Inspector, click **Add Component**
4. Search for **"TreasureHuntersEnding"** and add it

## Step 14: Assign Script References
Select **EndingManager**, in the Inspector find **Treasure Hunters Ending (Script)**:

### UI Elements Section:
- **Dialog Text**: Drag **DialogText** from Hierarchy
- **Next Button**: Drag **NextButton** GameObject
- **Exit Button**: Drag **ExitButton** GameObject

### Character Display Section:
- **Character Image**: Drag **TeacherImage** (the Image component)
- **Teacher Character**: Drag your teacher sprite from Assets

### Trophy Display Section:
- **Trophy Image**: Drag **TrophyImage** (the Image component)
- **Gold Trophy**: Drag your gold trophy sprite
- **Silver Trophy**: Drag your silver trophy sprite
- **Bronze Trophy**: Drag your bronze trophy sprite

### Animation Section:
- **Trophy Animator**: Leave empty (animation is code-based)

### Particle Effects Section:
- **Sparkle Effect**: Drag **SparkleEffect** (ParticleSystem)
- **Glow Effect**: Drag **GlowEffect** (ParticleSystem)

### Additional Visual Elements Section:
- **Flash Overlay**: Drag **FlashOverlay** (Image component)
- **Floating Stars**: Set Size to 4, then drag:
  - Element 0: Star1
  - Element 1: Star2
  - Element 2: Star3
  - Element 3: Star4

## Step 15: Add Scene to Build Settings
1. Go to **File → Build Settings**
2. Click **"Add Open Scenes"** to add MC-Ending
3. Make sure these scenes are in order:
   - login
   - loadingscreen (or loadingscreenF)
   - NEWMAP
   - classroom
   - MC-Opening
   - IndianaJonesMultipleChoice
   - **MC-Ending** ← New scene

## Step 16: Test the Flow
1. Save all your changes
2. Play the game from **login** scene
3. Go through: NEWMAP → Classroom → Enter Code → Multiple Choice → Answer Questions
4. After finishing all questions, you should see:
   - **White flash entrance effect**
   - **Teacher character slides in from left**
   - **Trophy bounces in with overshoot effect**
   - **Floating stars appear one by one and rotate**
   - **Particles play** (sparkles for 80%+, glow for 100%)
   - **Story-based dialogue** based on your score:
     - **100%**: "Temple trembles, guardians bow, Crystal of Infinite Wisdom revealed!"
     - **80-99%**: "Doors open, golden path appears, spirits whisper approval"
     - **60-79%**: "Emerge from temple, struggled but stood ground, path remains open"
     - **Below 60%**: "Overwhelmed by illusions, stumble out but guardians see fire in eyes"
   - Click NEXT to advance through 6 story dialogue messages
   - EXIT button appears with **white flash transition** back to NEWMAP

## Troubleshooting

### Scene doesn't load after questions:
- Check that scene is named exactly **"MC-Ending"** (case-sensitive)
- Verify scene is added to Build Settings

### Trophy doesn't appear:
- Check Trophy Image has a sprite assigned
- Verify trophy sprites are assigned in EndingManager Inspector

### Text looks strange:
- Import TextMesh Pro if prompted
- Check font size and panel dimensions

### Buttons don't work:
- Verify NextButton and ExitButton are assigned in Inspector
- Check that buttons have Event System in scene (should auto-create)

### Wrong dialogue appears:
- The script automatically selects dialogue based on score
- Debug: Check TreasureHuntersEnding.FinalScore value

### Animation doesn't play:
- Trophy animation is automatic via code
- Check that TrophyImage is assigned correctly

## Visual Hierarchy
```
Canvas
├── Background (Image)
├── TeacherImage (Image) - Left side, slides in
├── TrophyImage (Image) - Top center, bounces + floats
├── Star1 (Image) - Animates in, floats + rotates
├── Star2 (Image) - Animates in, floats + rotates
├── Star3 (Image) - Animates in, floats + rotates
├── Star4 (Image) - Animates in, floats + rotates
├── SparkleEffect (ParticleSystem) - Plays for 80%+ scores
├── GlowEffect (ParticleSystem) - Plays for 100% score
├── DialoguePanel (Image) - Bottom
│   ├── DialogText (TextMeshPro)
│   ├── NextButton (Button)
│   └── ExitButton (Button) - Hidden initially
├── FlashOverlay (Image) - White flash entrance/exit
└── EndingManager (GameObject with script)
```

## Color Scheme Suggestions
- **Background**: #3D2817 (Temple brown)
- **Dialogue Panel**: #1A1A1A with Alpha 200 (Semi-transparent black)
- **Dialogue Text**: #FFD700 (Gold)
- **Next Button**: #FFA500 (Orange)
- **Exit Button**: #4CAF50 (Green)

## Animation Summary
All animations are automatic and code-based:
- **Entrance Flash**: White flash fades from 100% to 0% over 1 second
- **Character Slide**: Teacher slides in from left over 0.8 seconds
- **Trophy Bounce**: Bounces in with sine wave over 0.8 seconds, then floats continuously
- **Stars Appear**: Each star pops in with delay, then floats at different speeds and rotates
- **Particles**: Sparkle plays for 80%+ scores, Glow plays for 100% perfect score
- **Exit Flash**: Fades to white over 0.5 seconds before loading NEWMAP

## Storyline Themes by Score
Each score range has a unique **narrative arc**:

### Perfect Score (100%):
*"The temple trembles, guardians bow, ancient heart opens, legendary crystal appears, name carved in Hall of Legends"*

### Excellent (80-99%):
*"Doors open, guardians approve, golden path appears, spirits whisper, journey begins"*

### Good (60-79%):
*"Emerge from treacherous path, treasures lost but stood ground, temple remains open for return"*

### Encouragement (Below 60%):
*"Overwhelmed by illusions, stumble out exhausted, guardians see potential, study and return stronger"*

## Final Notes
- The script handles all dialogue logic and animations automatically
- Trophy sprites change based on score percentage
- Typewriter effect speed is 0.05 seconds per character
- All dialogue follows a **narrative storyline** format in UPPERCASE
- Particles and animations trigger based on performance
- Each star has randomized float speed and rotation for variety
- Flash overlay provides cinematic transitions
