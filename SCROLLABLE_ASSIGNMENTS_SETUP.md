# Scrollable Assignment Buttons Setup Guide

## Overview
This guide shows you how to set up a scrollable list of assignment buttons that dynamically creates buttons based on what the teacher has created.

## Part 1: Create the ScrollView UI

### Step 1: Locate StagePanel in Hierarchy
1. Open the **classroom** scene
2. Find the **StagePanel** GameObject in Hierarchy
3. Select it

### Step 2: Create Scroll View Inside StagePanel
1. Right-click **StagePanel** → **UI → Scroll View**
2. Rename it to **"AssignmentScrollView"**
3. In Inspector, configure Rect Transform:
   - **Anchor Preset**: Stretch Both (Alt+Shift + bottom-right)
   - **Left**: 50
   - **Top**: 100
   - **Right**: 50
   - **Bottom**: 50
4. In **Scroll Rect** component:
   - **Horizontal**: Unchecked (no horizontal scrolling)
   - **Vertical**: Checked
   - **Movement Type**: Elastic or Clamped
   - **Scroll Sensitivity**: 20

### Step 3: Configure Viewport
1. Select **AssignmentScrollView → Viewport** (child object)
2. Verify Rect Transform is stretched (should be automatic)

### Step 4: Configure Content Container
1. Select **AssignmentScrollView → Viewport → Content**
2. In Inspector:
   - **Rect Transform**:
     - **Anchor Preset**: Top Stretch (top-middle icon, stretch horizontally)
     - **Pivot**: X: 0.5, Y: 1 (top center)
     - **Pos Y**: 0
     - **Height**: 400 (will auto-expand with content)
   
3. **Add Component** → **Vertical Layout Group**
   - **Child Alignment**: Upper Center
   - **Control Child Size**: Width ✓, Height ✗
   - **Child Force Expand**: Width ✓, Height ✗
   - **Spacing**: 20
   - **Padding**: Left 20, Right 20, Top 20, Bottom 20

4. **Add Component** → **Content Size Fitter**
   - **Horizontal Fit**: Unconstrained
   - **Vertical Fit**: Preferred Size

## Part 2: Create Button Prefab

### Step 5: Create Assignment Button Prefab
1. Right-click in Hierarchy → **UI → Button - TextMeshPro**
2. Rename to **"AssignmentButtonPrefab"**
3. **Move it OUTSIDE StagePanel** (drag to root of Hierarchy temporarily)

### Step 6: Configure Button Appearance
1. Select **AssignmentButtonPrefab**
2. In Inspector:
   - **Width**: 600
   - **Height**: 100
   - **Image Component**:
     - **Color**: White (#FFFFFF) or your theme color
   - **Button Component**:
     - **Transition**: Color Tint
     - **Normal Color**: White
     - **Highlighted Color**: Light Gray (#E0E0E0)
     - **Pressed Color**: Dark Gray (#C0C0C0)

### Step 7: Configure Button Text
1. Select **AssignmentButtonPrefab → Text (TMP)** (child)
2. In Inspector:
   - **Text**: "ASSIGNMENT NAME" (placeholder)
   - **Font Size**: 32
   - **Color**: Black or your preferred color
   - **Alignment**: Center horizontal, Center vertical
   - **Font Style**: Bold
   - **Wrapping**: Enabled

### Step 8: Create Prefab from Button
1. Create folder: **Assets/Prefabs** (if it doesn't exist)
2. **Drag AssignmentButtonPrefab** from Hierarchy into **Assets/Prefabs** folder
3. You'll see it turn blue in Hierarchy (it's now a prefab instance)
4. **Delete AssignmentButtonPrefab** from Hierarchy (we don't need it there anymore)

## Part 3: Connect Everything in ClassroomManager

### Step 9: Assign References
1. Find the **ClassroomManager** GameObject in Hierarchy (usually attached to a manager object)
2. Select it, look at Inspector
3. Find **Classroom Manager (Script)** component
4. In **Stage Panel UI** section:
   - **Category Button Container**: Drag **Content** (from AssignmentScrollView → Viewport → Content)
   - **Category Button Prefab**: Drag **AssignmentButtonPrefab** from **Assets/Prefabs** folder

### Step 10: Optional - Hide Old Buttons
If you still have the old fixed buttons (alchemyButton, identificationButton, multipleChoiceButton):
1. You can leave them for backward compatibility OR
2. Delete them from the scene and set those fields to **None** in Inspector

## Part 4: Test the System

### Step 11: Test in Play Mode
1. Save the scene
2. Press **Play**
3. Login and enter a classroom
4. You should see:
   - Assignment buttons created dynamically
   - They stack vertically with spacing
   - Scroll appears if there are many assignments
   - Each button shows the assignment name in uppercase

## Visual Hierarchy
```
StagePanel
└── AssignmentScrollView (Scroll View)
    └── Viewport
        └── Content (Vertical Layout Group + Content Size Fitter)
            ├── AssignmentButtonPrefab (Clone) - Created dynamically
            ├── AssignmentButtonPrefab (Clone) - Created dynamically
            └── AssignmentButtonPrefab (Clone) - Created dynamically...
```

## Troubleshooting

### Buttons not appearing:
- Check that **Category Button Container** points to **Content** object
- Check that **Category Button Prefab** is assigned in Inspector
- Check Console for errors

### Buttons not scrolling:
- Verify **Scroll Rect** component is on AssignmentScrollView
- Check that **Content** is set in Scroll Rect's **Content** field
- Make sure Vertical is checked, Horizontal is unchecked

### Buttons overlapping or wrong size:
- Check **Vertical Layout Group** settings on Content
- Verify **Content Size Fitter** is set to Preferred Size for Vertical
- Adjust **Spacing** in Vertical Layout Group

### Button text is cut off:
- Increase button **Height** in prefab (e.g., 100-120)
- Enable **Wrapping** in TextMeshPro component
- Reduce **Font Size** if needed

### Old buttons still showing:
- The code uses the new system if **both** container and prefab are assigned
- If either is missing, it falls back to old fixed buttons
- Make sure both fields are properly assigned in ClassroomManager

## Customization Tips

### Change Button Colors by Assignment Type:
You can modify the code to color-code buttons. In `LoadAssignmentTypes`, after creating the button:

```csharp
// Change button color based on type
Image buttonImage = buttonObj.GetComponent<Image>();
if (buttonImage != null)
{
    if (assignmentType == "Alchemy")
        buttonImage.color = new Color(0.2f, 0.8f, 0.2f); // Green
    else if (assignmentType == "MultipleChoice")
        buttonImage.color = new Color(0.8f, 0.6f, 0.2f); // Gold
    else if (assignmentType == "Identification")
        buttonImage.color = new Color(0.2f, 0.6f, 0.8f); // Blue
}
```

### Add Icons to Buttons:
1. Add an **Image** child to the prefab
2. Position it on the left side
3. Assign different sprites for different assignment types in code

### Adjust Scrollbar:
1. Select **AssignmentScrollView → Scrollbar Vertical**
2. Customize colors and width in Inspector
3. Or delete it for invisible scrolling

## Summary
- ✅ Dynamic button creation based on teacher's assignments
- ✅ Automatic stacking with Vertical Layout Group
- ✅ Scrolling when content exceeds view
- ✅ Clean, maintainable code
- ✅ Backward compatible with old button system
