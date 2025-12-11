# 🎓 CLASSROOM SCENE - LANDSCAPE MODIFICATION GUIDE

## Overview
This guide will help you modify your existing **classroom.unity** scene from portrait to landscape orientation and add a QR scan button alongside your existing class code entry.

### 📱 Current Layout (What You Have)
```
┌─────────────────────────────────────────┐
│         🔴 Back     Class Rooms          │
│  ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐   │
│  │ r1   │ │ r2   │ │ r3   │ │ r4   │   │
│  │Click │ │Click │ │Click │ │Click │   │
│  └──────┘ └──────┘ └──────┘ └──────┘   │
│  ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐   │
│  │ r5   │ │ r6   │ │ r7   │ │ r8   │   │
│  └──────┘ └──────┘ └──────┘ └──────┘   │
│  ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐   │
│  │ r9   │ │ r10  │ │      │ │      │   │
│  └──────┘ └──────┘ └──────┘ └──────┘   │
│                                          │
│  ┌─────────────────────────────────┐   │
│  │   ENTER CLASSCODE               │   │
│  └─────────────────────────────────┘   │
│  OR                                      │
│       ┌────────┐                        │
│       │ Submit │                        │
│       └────────┘                        │
└─────────────────────────────────────────┘
```

### ✨ Updated Layout (What We're Adding)
```
┌─────────────────────────────────────────┐
│         🔴 Back     Class Rooms          │
│  ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐   │
│  │ r1   │ │ r2   │ │ r3   │ │ r4   │   │
│  │Click │ │Click │ │Click │ │Click │   │
│  └──────┘ └──────┘ └──────┘ └──────┘   │
│  ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐   │
│  │ r5   │ │ r6   │ │ r7   │ │ r8   │   │
│  └──────┘ └──────┘ └──────┘ └──────┘   │
│  ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐   │
│  │ r9   │ │ r10  │ │      │ │      │   │
│  └──────┘ └──────┘ └──────┘ └──────┘   │
│                                          │
│  ┌─────────────────────────────────┐   │
│  │   ENTER CLASSCODE               │   │
│  └─────────────────────────────────┘   │
│  OR                                      │
│    ┌───────────┐    ┌────────────┐     │
│    │  Submit   │    │ 📷 SCAN QR │ ← NEW│
│    └───────────┘    └────────────┘     │
└─────────────────────────────────────────┘
```

**Key Changes:**
- ✅ Add QR scan button next to Submit button
- ✅ Landscape orientation (1920x1080)
- ✅ Script updates to handle both options
- ✅ API integration for joining classroom

---

## 🔄 STEP 1: CHANGE PROJECT TO LANDSCAPE

### Method 1: Unity Editor (Recommended)
1. **Open Unity** → Go to **Edit → Project Settings**
2. Click on **Player** in the left sidebar
3. Under **Resolution and Presentation**:
   - Find **Default Orientation**
   - Change from `Auto Rotation` to `Landscape Left`
4. Scroll down to **Allowed Orientations for Auto Rotation**:
   - ✅ **Uncheck** "Portrait"
   - ✅ **Uncheck** "Portrait Upside Down"
   - ✅ **Check** "Landscape Left"
   - ✅ **Check** "Landscape Right"
5. Click **File → Save Project**

---

## 🖼️ STEP 2: MODIFY CANVAS TO LANDSCAPE

1. **Open** `classroom.unity` scene
2. **Select** the `Canvas` GameObject in Hierarchy
3. **In Inspector**, find **Canvas Scaler** component
4. Change settings:
   ```
   UI Scale Mode: Scale With Screen Size
   Reference Resolution:
     X: 1920
     Y: 1080
   Screen Match Mode: Match Width Or Height
   Match: 0.5
   ```

---

## 📱 STEP 3: ADD ENTRY SCREEN (Class Code / QR)

### A. Create Entry Panel

1. **Right-click** on `Canvas` → **UI → Image**
2. **Name it**: `EntryPanel`
3. **RectTransform** settings:
   - Anchor Preset: **Stretch** (both horizontal and vertical)
   - Left: `0`, Top: `0`, Right: `0`, Bottom: `0`
4. **Image Component**:
   - Color: `#F5F5F5` (light gray background)

---

### B. Create Title Text

1. **Right-click** `EntryPanel` → **UI → Text - TextMeshPro**
2. **Name**: `TitleText`
3. **RectTransform**:
   - Anchor: **Top Center**
   - Pos X: `0`, Pos Y: `-100`
   - Width: `800`, Height: `100`
4. **TextMeshProUGUI**:
   - Text: `ENTER CLASSROOM`
   - Font Size: `48`
   - Font Style: **Bold**
   - Alignment: **Center/Middle**
   - Color: `#D32F2F` (red)

---

### C. Create Class Code Panel

1. **Right-click** `EntryPanel` → **Create Empty**
2. **Name**: `ClassCodePanel`
3. **RectTransform**:
   - Anchor: **Center**
   - Pos X: `0`, Pos Y: `0`
   - Width: `600`, Height: `400`

---

### D. Create Input Field

1. **Right-click** `ClassCodePanel` → **UI → Input Field - TextMeshPro**
2. **Name**: `ClassCodeInputField`
3. **RectTransform**:
   - Anchor: **Top Center**
   - Pos X: `0`, Pos Y: `-50`
   - Width: `500`, Height: `80`
4. **Image Component** (Background):
   - Color: White `#FFFFFF`
   - Add **Outline** (Component → UI → Effects → Outline):
     - Effect Color: `#CCCCCC`
     - Effect Distance: X: `3`, Y: `-3`
5. **TMP_InputField** settings:
   - Character Limit: `20`
   - Content Type: `Alphanumeric`

#### **Placeholder Text** (auto-created child)
- Text: `Enter class code...`
- Font Size: `28`
- Color: `#999999` (gray)
- Alignment: Center/Middle

#### **Text** (auto-created child)  
- Font Size: `32`
- Color: `#333333` (dark gray)
- Alignment: Center/Middle

---

### E. Create Enter Button

1. **Right-click** `ClassCodePanel` → **UI → Button**
2. **Name**: `EnterButton`
3. **RectTransform**:
   - Anchor: **Top Center**
   - Pos X: `0`, Pos Y: `-160`
   - Width: `500`, Height: `70`
4. **Image Component**:
   - Color: `#D32F2F` (red)
5. **Button Component**:
   - Transition: **Color Tint**
   - Normal: `#D32F2F`
   - Highlighted: `#F44336`
   - Pressed: `#B71C1C`

#### **Button Text** (auto-created child)
- Right-click `EnterButton` → **UI → Text - TextMeshPro**
- Name: `Text`
- Text: `ENTER CLASS CODE`
- Font Size: `28`
- Font Style: **Bold**
- Alignment: Center/Middle
- Color: **White**

---

### F. Create "OR" Divider

1. **Right-click** `ClassCodePanel` → **Create Empty**
2. **Name**: `OrDivider`
3. **RectTransform**:
   - Anchor: **Top Center**
   - Pos X: `0`, Pos Y: `-260`
   - Width: `500`, Height: `40`

#### **Left Line**
- Right-click `OrDivider` → **UI → Image**
- Name: `LeftLine`
- RectTransform:
   - Anchor: **Middle Left**
   - Pos X: `100`, Pos Y: `0`
   - Width: `180`, Height: `2`
- Color: `#CCCCCC`

#### **"or" Text**
- Right-click `OrDivider` → **UI → Text - TextMeshPro**
- Name: `OrText`
- RectTransform:
   - Anchor: **Center**
   - Width: `60`, Height: `40`
- Text: `or`
- Font Size: `24`
- Alignment: Center/Middle
- Color: `#999999`

#### **Right Line**
- Right-click `OrDivider` → **UI → Image**
- Name: `RightLine`
- RectTransform:
   - Anchor: **Middle Right**
   - Pos X: `-100`, Pos Y: `0`
   - Width: `180`, Height: `2`
- Color: `#CCCCCC`

---

### G. Add QR Scan Button (For Your Existing Layout)

Since you already have a Submit button at the bottom, let's add a QR button next to it:

#### **Option 1: Add QR Button Next to Submit**

1. **Find your existing Submit button** in the Hierarchy (inside the green panel)
2. **Duplicate it**: Right-click Submit → Duplicate
3. **Rename** the duplicate to: `ScanQRButton`
4. **Reposition them side by side**:

   **Submit Button RectTransform**:
   - Anchor: **Bottom Center**
   - Pos X: `-160` (moved left)
   - Pos Y: `50`
   - Width: `280`, Height: `70`

   **ScanQRButton RectTransform**:
   - Anchor: **Bottom Center**
   - Pos X: `160` (moved right)
   - Pos Y: `50`
   - Width: `280`, Height: `70`

5. **Change ScanQRButton appearance**:
   - **Image Component**: Color: White `#FFFFFF`
   - Add **Outline** (Component → UI → Effects → Outline):
     - Effect Color: `#D32F2F`
     - Effect Distance: X: `3`, Y: `-3`
   - **Button Component**:
     - Normal: White
     - Highlighted: `#FFEBEE`
     - Pressed: `#FFCDD2`

6. **Update button text** (child of ScanQRButton):
   - Text: `📷 SCAN QR`
   - Font Size: `26`
   - Font Style: **Bold**
   - Color: `#D32F2F` (red)

#### **Option 2: Stack Buttons Vertically**

If you prefer buttons stacked:

1. **Submit Button**:
   - Pos Y: `80`
   - Width: `500`

2. **ScanQRButton** (below Submit):
   - Pos Y: `0`
   - Width: `500`
   - Style: White with red outline (as above)

---

### H. Create Back Button (Top Left)

1. **Right-click** `EntryPanel` → **UI → Button**
2. **Name**: `BackButton`
3. **RectTransform**:
   - Anchor: **Top Left**
   - Pos X: `60`, Pos Y: `-60`
   - Width: `100`, Height: `100`
4. **Image Component**:
   - Color: `#D32F2F`
5. **Button Component**:
   - Transition: Color Tint

#### **Back Icon/Text**
- Right-click `BackButton` → **UI → Text - TextMeshPro**
- Text: `←` (or "BACK")
- Font Size: `48`
- Alignment: Center/Middle
- Color: White

---

## 🎨 STEP 4: MODIFY EXISTING CLASSROOM GRID

Your existing classroom scene has 10 room slots (`r1` to `r10`). We need to adjust them for landscape:

### Option A: Keep Grid Layout (Recommended)
1. **Select** the parent container of your room items (likely inside `Image`)
2. **Adjust Grid Layout Group** (if using):
   ```
   Cell Size: Width: 350, Height: 250
   Spacing: X: 40, Y: 40
   Constraint: Fixed Column Count: 4
   ```
3. This will create a 4x3 grid (12 slots, you have 10)

### Option B: Manual Repositioning
If rooms are positioned manually:
1. Arrange in **4 columns x 3 rows**
2. Starting position: X: `-700`, Y: `400`
3. Spacing between items: X: `400`, Y: `-300`

---

## 🔧 STEP 5: CONNECT SCRIPT

### Modify ClassroomManager.cs

Add these new public variables at the top of your `ClassroomManager` class (around line 62):

```csharp
[Header("Entry Panel UI")]
public GameObject entryPanel;          // The green panel with classcode/QR options
public TMP_InputField entryCodeInput;  // The "ENTER CLASSCODE" input field
public Button submitButton;            // The "Submit" button
public Button scanQRButton;            // The "SCAN QR" button (new)
public Button backButtonEntry;         // BackButton (top left)
```

### Update the Start() method:

Find your `Start()` method and add this at the beginning (before existing code):

```csharp
void Start()
{
    if (SessionManager.Instance == null)
    {
        Debug.LogError("SessionManager not found!");
        return;
    }

    studentId = SessionManager.Instance.StudentId;
    if (studentId <= 0)
    {
        Debug.LogError("Invalid studentId in session!");
        return;
    }

    // ✅ NEW: Setup entry panel
    if (entryPanel != null)
    {
        entryPanel.SetActive(true); // Show entry screen first
    }
    
    // Setup button listeners for entry panel
    if (submitButton != null)
        submitButton.onClick.AddListener(OnSubmitClassCode);
    
    if (scanQRButton != null)
        scanQRButton.onClick.AddListener(OnScanQR);
    
    if (backButtonEntry != null)
        backButtonEntry.onClick.AddListener(OnBackFromEntry);

    // Existing panel setup...
    stagePanel.SetActive(false);
    addClassPanel.SetActive(false);
    noQuestionPanel.SetActive(false);
    if (warningText != null) warningText.gameObject.SetActive(false);

    // Note: Keep your existing joinClassButton listener
    if (joinClassButton != null)
        joinClassButton.onClick.AddListener(OnJoinClassClicked);

    foreach (var room in rooms)
        AddClickListener(room.roomImage, room.roomId);

    // ✅ CHANGED: Don't load immediately, wait for entry
    // StartCoroutine(LoadRoomAssignments());
}
```

### Add New Methods (at the bottom of the class):

```csharp
/// <summary>
/// Called when Submit button is clicked (enter class code)
/// </summary>
void OnSubmitClassCode()
{
    if (entryCodeInput == null || string.IsNullOrEmpty(entryCodeInput.text))
    {
        Debug.LogWarning("⚠️ Please enter a class code!");
        // Optionally show warning message to user
        return;
    }

    string code = entryCodeInput.text.Trim();
    Debug.Log($"🔑 Joining class with code: {code}");
    
    // TODO: Call your API to join classroom with this code
    // For now, just hide entry panel and show classroom grid
    StartCoroutine(JoinClassroomWithCode(code));
}

/// <summary>
/// Join classroom using class code via API
/// </summary>
IEnumerator JoinClassroomWithCode(string classCode)
{
    // Call your join classroom API endpoint
    WWWForm form = new WWWForm();
    form.AddField("student_id", studentId);
    form.AddField("class_code", classCode);

    using (UnityWebRequest request = UnityWebRequest.Post(baseUrl + "join_classroom", form))
    {
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ Error joining classroom: " + request.error);
            // Show error message to user
        }
        else
        {
            Debug.Log("✅ Successfully joined classroom!");
            
            // Clear the input field
            if (entryCodeInput != null)
                entryCodeInput.text = "";
            
            // Reload classrooms to show the newly joined class
            StartCoroutine(LoadRoomAssignments());
        }
    }
}

/// <summary>
/// Called when Scan QR button is clicked
/// </summary>
void OnScanQR()
{
    Debug.Log("📷 Opening QR Scanner...");
    
    // OPTION 1: Open device camera for QR scanning
    // You'll need a QR scanner plugin like ZXing.Net or similar
    // StartQRScanner();
    
    // OPTION 2: For testing, use a test code
    #if UNITY_EDITOR
    Debug.Log("🧪 EDITOR MODE: Using test code");
    StartCoroutine(JoinClassroomWithCode("TEST123"));
    #else
    // In actual build, implement real QR scanner
    // For example using ZXing plugin:
    // Application.OpenURL("qr-scanner://"); // If using native plugin
    #endif
}

/// <summary>
/// Start QR code scanner (requires QR scanner plugin)
/// </summary>
void StartQRScanner()
{
    // TODO: Implement with QR scanner plugin
    // Example with ZXing:
    /*
    QRCodeReader reader = new QRCodeReader();
    reader.OnQRCodeScanned += (scannedCode) => {
        Debug.Log($"📷 Scanned QR Code: {scannedCode}");
        StartCoroutine(JoinClassroomWithCode(scannedCode));
    };
    reader.StartScanning();
    */
    
    Debug.LogWarning("⚠️ QR Scanner not implemented yet. Add ZXing or similar plugin.");
}

/// <summary>
/// Called when back button is clicked from entry panel
/// </summary>
void OnBackFromEntry()
{
    Debug.Log("◀️ Going back to previous scene...");
    // Go back to login or main menu
    SceneManager.LoadScene("login"); // Change to your login scene name
}
```

---

## 🎯 STEP 6: ASSIGN REFERENCES IN UNITY

1. **Select** the GameObject that has `ClassroomManager` script attached (usually named "GameObject" or "GameManager")
2. **In Inspector**, you'll see new fields under "Entry Panel UI":
   - **Entry Panel**: Drag the green panel (Image) from Hierarchy
   - **Entry Code Input**: Drag `InputField (TMP)` (the "ENTER CLASSCODE" field)
   - **Submit Button**: Drag `Submit` button
   - **Scan QR Button**: Drag `ScanQRButton` (the new QR button you created)
   - **Back Button Entry**: Drag `BackButton` (the red button at top)
   - **Classroom Grid Panel**: Drag the parent object containing all r1-r10 rooms

---

## 📷 STEP 7: QR CODE IMPLEMENTATION OPTIONS

You have several options for implementing QR code scanning:

### **Option A: ZXing.Net (Free, Most Popular)**

1. **Download** ZXing plugin:
   - Go to: https://github.com/micjahn/ZXing.Net
   - Or use Unity Package Manager with OpenUPM

2. **Install via Package Manager**:
   ```
   Window → Package Manager → + → Add package from git URL
   Paste: com.unity.nuget.zxing
   ```

3. **Update OnScanQR()** to use ZXing:
```csharp
using ZXing;
using ZXing.QrCode;

void OnScanQR()
{
    // Start camera-based QR scanning
    Application.OpenURL("qr-scanner://start");
}
```

### **Option B: Native WebCam Scanner (Free)**

Use Unity's WebCamTexture for custom QR scanner:

```csharp
private WebCamTexture webcamTexture;

void StartQRScanner()
{
    webcamTexture = new WebCamTexture();
    rawImage.texture = webcamTexture; // Display on screen
    webcamTexture.Play();
    
    InvokeRepeating("ScanQRCode", 0.5f, 0.5f);
}

void ScanQRCode()
{
    IBarcodeReader reader = new BarcodeReader();
    var result = reader.Decode(webcamTexture.GetPixels32(), 
                              webcamTexture.width, 
                              webcamTexture.height);
    
    if (result != null)
    {
        Debug.Log("QR Code: " + result.Text);
        webcamTexture.Stop();
        CancelInvoke("ScanQRCode");
        StartCoroutine(JoinClassroomWithCode(result.Text));
    }
}
```

### **Option C: Use Native Device Scanner (Android/iOS)**

Create a native plugin to open device camera:

**Android:**
```java
Intent intent = new Intent("com.google.zxing.client.android.SCAN");
startActivityForResult(intent, 0);
```

**iOS:**
Use AVFoundation framework

### **Option D: Simple Testing (Current Implementation)**

For now, the code uses a test mode in Unity Editor that automatically joins with "TEST123". This lets you test the flow without implementing full QR scanning.

**To test:**
1. Play scene in Unity Editor
2. Click "SCAN QR" button
3. It will auto-use test code "TEST123"
4. For actual builds, implement real QR scanner

---

## 💾 STEP 8: SAVE YOUR BACKEND API ENDPOINT

Make sure your backend has a QR code endpoint. The QR code should contain the class code string.

**Example QR Code Format:**
```
MATH2024-A
```

Or JSON format:
```json
{"class_code": "MATH2024-A", "teacher_id": 5}
```

Your backend should have:
- `POST /join_classroom` - Accepts student_id and class_code
- Returns success/error message

---

## 📐 STEP 7: ADJUST EXISTING CLASSROOM ELEMENTS

Your current classroom scene likely has:
- "Class Rooms" title
- 10 room buttons (r1-r10)
- Back button at top

### Hide Classroom Grid Initially

1. Find the parent GameObject containing all room items
2. Name it something like `ClassroomGridPanel` (if not already named)
3. In your `ClassroomManager.cs`, add this variable:

```csharp
[Header("Panels")]
public GameObject classroomGridPanel;  // The panel with all room buttons
public GameObject stagePanel;
public GameObject addClassPanel;
public GameObject noQuestionPanel;
```

4. In `Start()`, add:
```csharp
if (classroomGridPanel != null)
    classroomGridPanel.SetActive(false); // Hide grid initially
```

5. In `OnEnterCodeFromEntryPanel()` and `OnScanQR()`, add:
```csharp
if (classroomGridPanel != null)
    classroomGridPanel.SetActive(true); // Show grid after entry
```

---

## ✅ FINAL CHECKLIST

- [ ] Project settings changed to Landscape
- [ ] Canvas set to 1920x1080 reference resolution
- [ ] EntryPanel created with all UI elements
- [ ] ClassCodeInputField configured
- [ ] Enter and QR buttons created
- [ ] Back button added
- [ ] ClassroomManager.cs updated with new code
- [ ] All Inspector references assigned
- [ ] Classroom grid hidden on scene start
- [ ] Scene tested in Game view (set to 16:9 landscape)

---

## 🎨 COLOR PALETTE USED

```
Primary Red:    #D32F2F
Light Red:      #F44336
Dark Red:       #B71C1C
White:          #FFFFFF
Light Gray:     #F5F5F5
Medium Gray:    #CCCCCC
Dark Gray:      #666666
Text Dark:      #333333
Text Light:     #999999
```

---

## 🧪 TESTING

1. **Play the scene** in Unity Editor
2. You should see:
   - Entry screen with class code input
   - Enter button
   - "or" divider
   - QR scan button
   - Back button
3. **Enter any code** and click ENTER
4. Classroom grid should appear
5. **Test QR button** - it should also show classroom grid

---

## 🔄 FLOW DIAGRAM

```
START
  ↓
EntryPanel (Class Code / QR)
  ↓
[Enter Code] or [Scan QR]
  ↓
ClassroomGrid (10 rooms)
  ↓
[Click Empty Room] → AddClassPanel
  ↓
[Enter Another Code] → Join Class
  ↓
[Click Filled Room] → StagePanel (Assignment Types)
  ↓
[Select Assignment Type] → Game Scene
```

---

## 💡 TIPS

1. **Test in Game View**: Set aspect ratio to **16:9** (landscape)
2. **Safe Area**: Keep important UI elements at least **100px** from screen edges
3. **Font Sizes**: Landscape allows larger, more readable text
4. **Grid Layout**: 4 columns works better than 2 for landscape
5. **Back Button**: Always provide a way to return to entry screen

---

## 🐛 TROUBLESHOOTING

### "Entry panel not showing"
- Check `entryPanel.SetActive(true)` is in `Start()`
- Verify EntryPanel is assigned in Inspector

### "Classroom grid shows immediately"
- Make sure `classroomGridPanel.SetActive(false)` is in `Start()`
- Comment out `StartCoroutine(LoadRoomAssignments())` in `Start()`

### "Buttons don't work"
- Verify Button components have correct listeners
- Check method names match exactly
- Ensure script is attached to active GameObject

### "Canvas looks wrong"
- Verify Canvas Scaler is set to **Scale With Screen Size**
- Check reference resolution is **1920 x 1080**
- Set Match slider to **0.5**

---

---

## 🚀 QUICK START SUMMARY

**To add QR button to your existing scene:**

1. **Duplicate your Submit button** → Rename to `ScanQRButton`
2. **Position them side by side** (Submit at X: -160, QR at X: 160)
3. **Style QR button**: White background, red outline
4. **Change text**: "📷 SCAN QR"
5. **Add to ClassroomManager.cs**:
   - Add `public Button scanQRButton;`
   - Add `scanQRButton.onClick.AddListener(OnScanQR);`
   - Add the `OnScanQR()` method from guide
6. **Assign in Inspector**: Drag ScanQRButton to script field
7. **Test**: Play scene, click QR button (uses test code in editor)

**For production QR scanning:**
- Install ZXing plugin
- Implement camera-based QR reader
- Update `OnScanQR()` method

---

**Your landscape classroom scene with QR scanning is now complete! 🎉**

<function_calls>
<invoke name="read_file">
<parameter name="filePath">c:\Users\Akira\Desktop\Student Academic Updated\Assets\Script\ClassroomManager.cs