using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class ClassroomManager : MonoBehaviour
{
    [System.Serializable]
    public class RoomUI
    {
        public Image roomImage;
        public TMP_Text roomText;
        public int roomId;
    }

    [System.Serializable]
    public class ClassroomData
    {
        public int room_no;
        public int class_id;
        public string description;
    }

    [System.Serializable]
    public class AssignmentTypeData
    {
        public int category_id;
        public string description;
        public bool is_completed;
    }

    private class ClassroomListWrapper
    {
        public List<ClassroomData> classrooms;
    }

    private class AssignmentTypeListWrapper
    {
        public List<AssignmentTypeData> categories;
    }

    [Header("Classroom Entry UI (Submit/QR Buttons)")]
    public TMP_InputField entryCodeInput;
    public Button submitButton;
    public Button scanQRButton;

    [Header("Rooms (r1–r12)")]
    public List<RoomUI> rooms = new List<RoomUI>();

    [Header("Panels")]
    public GameObject stagePanel;
    public GameObject addClassPanel;
    public GameObject noQuestionPanel;

    [Header("Add Class Panel UI")]
    public TMP_InputField classCodeInput;
    public Button joinClassButton;

    [Header("Stage Panel UI")]
    public Transform categoryButtonContainer;  // The container inside scroll view for buttons
    public GameObject categoryButtonPrefab;    // Prefab for creating assignment buttons dynamically
    public Button exitButton;                  // Button to close the assignment panel
    
    // Keep these for backward compatibility (optional, can be removed later)
    public Button alchemyButton;               // Alchemy = True/False gameplay
    public Button identificationButton;
    public Button multipleChoiceButton;        // Multiple Choice = Treasure Hunt/Indiana Jones
    
    private List<GameObject> spawnedButtons = new List<GameObject>();

    [Header("Warning & Error")]
    public TMP_Text warningText;

    private string baseUrl = "https://homequest-c3k7.onrender.com/";
    private int studentId;
    private int currentClassId;
    private int selectedRoomId;
    private string selectedAssignmentType = ""; // Store the type of selected assignment

    private Dictionary<int, ClassroomData> classroomDataByRoom = new Dictionary<int, ClassroomData>();

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

        if (stagePanel != null) stagePanel.SetActive(false);
        if (addClassPanel != null) addClassPanel.SetActive(false);
        if (noQuestionPanel != null) noQuestionPanel.SetActive(false);
        if (warningText != null) warningText.gameObject.SetActive(false);

        // Auto-uppercase for class code input
        if (classCodeInput != null)
        {
            classCodeInput.onValueChanged.AddListener(ConvertToUppercase);
            classCodeInput.characterValidation = TMP_InputField.CharacterValidation.Alphanumeric;
        }

        // Setup Submit and QR button listeners
        if (submitButton != null)
            submitButton.onClick.AddListener(OnSubmitClassCode);

        if (scanQRButton != null)
            scanQRButton.onClick.AddListener(OnScanQR);
        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitButtonClicked);
        if (joinClassButton != null)
            joinClassButton.onClick.AddListener(OnJoinClassClicked);

        foreach (var room in rooms)
            AddClickListener(room.roomImage, room.roomId);

        StartCoroutine(LoadRoomAssignments());
    }

    void AddClickListener(Image roomImage, int roomId)
    {
        EventTrigger trigger = roomImage.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = roomImage.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener((data) => { OnRoomClicked(roomId); });
        trigger.triggers.Add(entry);
    }

    void OnRoomClicked(int roomId)
    {
        selectedRoomId = roomId;

        if (classroomDataByRoom.ContainsKey(roomId))
        {
            ClassroomData classroom = classroomDataByRoom[roomId];
            currentClassId = classroom.class_id;
            StartCoroutine(LoadAssignmentTypes(currentClassId));
        }
        else
        {
            if (addClassPanel != null) addClassPanel.SetActive(true);
        }
    }

    void OnJoinClassClicked()
    {
        string code = classCodeInput.text.Trim();
        if (string.IsNullOrEmpty(code))
        {
            Debug.LogWarning("Class code is empty!");
            return;
        }

        StartCoroutine(SaveClassroom(selectedRoomId, code));
    }

    /// <summary>
    /// Called when Submit button is clicked (bottom of ClassRooms panel)
    /// </summary>
    void OnSubmitClassCode()
    {
        if (entryCodeInput == null || string.IsNullOrEmpty(entryCodeInput.text))
        {
            Debug.LogWarning("⚠️ Please enter a class code!");
            if (warningText != null)
            {
                warningText.text = "Please enter a class code";
                warningText.gameObject.SetActive(true);
                StartCoroutine(HideWarningAfterDelay(3f));
            }
            return;
        }

        string code = entryCodeInput.text.Trim();
        Debug.Log($"🔑 Joining class with code: {code}");

        // Find first empty room
        int emptyRoomId = FindFirstEmptyRoom();
        if (emptyRoomId == -1)
        {
            Debug.LogWarning("⚠️ All rooms are full!");
            if (warningText != null)
            {
                warningText.text = "All rooms are full! Please remove a class first.";
                warningText.gameObject.SetActive(true);
                StartCoroutine(HideWarningAfterDelay(3f));
            }
            return;
        }

        // Join the classroom in the first empty room
        StartCoroutine(SaveClassroom(emptyRoomId, code));
    }

    /// <summary>
    /// Called when Scan QR button is clicked
    /// </summary>
    void OnScanQR()
    {
        Debug.Log("📷 Opening QR Scanner...");

#if UNITY_EDITOR
        // Test mode in Unity Editor
        Debug.Log("🧪 EDITOR MODE: Using test code 'TEST123'");
        int emptyRoomId = FindFirstEmptyRoom();
        if (emptyRoomId == -1)
        {
            Debug.LogWarning("⚠️ All rooms are full!");
            if (warningText != null)
            {
                warningText.text = "All rooms are full!";
                warningText.gameObject.SetActive(true);
                StartCoroutine(HideWarningAfterDelay(3f));
            }
            return;
        }
        StartCoroutine(SaveClassroom(emptyRoomId, "TEST123"));
#else
        // Production mode - implement actual QR scanner
        // TODO: Add ZXing or similar QR scanner plugin
        StartQRScanner();
#endif
    }

    /// <summary>
    /// Start QR code scanner (requires QR scanner plugin like ZXing)
    /// </summary>
    void StartQRScanner()
    {
        // TODO: Implement with QR scanner plugin
        // Example with ZXing:
        /*
        QRCodeReader reader = new QRCodeReader();
        reader.OnQRCodeScanned += (scannedCode) => {
            Debug.Log($"📷 Scanned QR Code: {scannedCode}");
            int emptyRoomId = FindFirstEmptyRoom();
            if (emptyRoomId != -1)
            {
                StartCoroutine(SaveClassroom(emptyRoomId, scannedCode));
            }
        };
        reader.StartScanning();
        */

        Debug.LogWarning("⚠️ QR Scanner not implemented yet. Add ZXing or similar plugin.");
        if (warningText != null)
        {
            warningText.text = "QR Scanner not available yet";
            warningText.gameObject.SetActive(true);
            StartCoroutine(HideWarningAfterDelay(3f));
        }
    }

    /// <summary>
    /// Find the first empty room slot
    /// </summary>
    int FindFirstEmptyRoom()
    {
        for (int i = 1; i <= rooms.Count; i++)
        {
            if (!classroomDataByRoom.ContainsKey(i))
            {
                Debug.Log($"✅ Found empty room: r{i}");
                return i;
            }
        }
        return -1; // All rooms are full
    }

    IEnumerator SaveClassroom(int roomNo, string classCode)
    {
        WWWForm form = new WWWForm();
        form.AddField("student_id", studentId);
        form.AddField("room_no", roomNo);
        form.AddField("code", classCode);  // Backend expects "code" not "class_code"

        using (UnityWebRequest request = UnityWebRequest.Post(baseUrl + "save_classroom", form))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string errorMsg = request.error;
                string responseText = request.downloadHandler.text;
                Debug.LogError($"Error saving classroom: {errorMsg}");
                Debug.LogError($"Server response: {responseText}");
                Debug.LogError($"Data sent - student_id: {studentId}, room_no: {roomNo}, class_code: {classCode}");
                
                if (warningText != null)
                {
                    warningText.text = "Failed to join class. Check code.";
                    warningText.gameObject.SetActive(true);
                    StartCoroutine(HideWarningAfterDelay(3f));
                }
            }
            else
            {
                Debug.Log("✅ Classroom saved successfully!");
                
                // Clear input fields
                if (classCodeInput != null) classCodeInput.text = "";
                if (entryCodeInput != null) entryCodeInput.text = "";
                
                if (addClassPanel != null) addClassPanel.SetActive(false);
                StartCoroutine(LoadRoomAssignments());
            }
        }
    }

    IEnumerator LoadRoomAssignments()
    {
        string url = baseUrl + "get_classrooms?student_id=" + studentId;

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error loading classrooms: " + request.error);
            }
            else
            {
                string jsonResponse = request.downloadHandler.text;
                Debug.Log("Classrooms JSON: " + jsonResponse);

                // Parse the JSON array
                List<ClassroomData> classroomList = JsonUtilityWrapper.FromJsonList<ClassroomData>(jsonResponse);

                classroomDataByRoom.Clear();

                foreach (var room in rooms)
                {
                    room.roomText.text = "";
                    room.roomImage.color = new Color(0.8f, 0.8f, 0.8f);
                }

                if (classroomList != null && classroomList.Count > 0)
                {
                    Debug.Log($"📚 Found {classroomList.Count} classrooms");
                    foreach (var classroom in classroomList)
                    {
                        Debug.Log($"📖 Classroom - room_no: {classroom.room_no}, class_id: {classroom.class_id}, description: {classroom.description}");
                        classroomDataByRoom[classroom.room_no] = classroom;

                        RoomUI roomUI = rooms.Find(r => r.roomId == classroom.room_no);
                        Debug.Log($"🔍 Looking for room with ID {classroom.room_no}, found: {(roomUI != null ? "YES" : "NO")}");
                        if (roomUI != null)
                        {
                            roomUI.roomText.text = classroom.description;
                            roomUI.roomImage.color = Color.white;
                        }
                    }
                }
            }
        }
    }

    IEnumerator LoadAssignmentTypes(int classId)
    {
        string url = baseUrl + "get_assignment_types?student_id=" + studentId + "&class_id=" + classId;

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error loading assignment types: " + request.error);
            }
            else
            {
                string jsonResponse = request.downloadHandler.text;
                Debug.Log("Assignment Types JSON: " + jsonResponse);

                // Parse the JSON array
                List<AssignmentTypeData> categoryList = JsonUtilityWrapper.FromJsonList<AssignmentTypeData>(jsonResponse);
                Debug.Log($"📋 Parsed {(categoryList != null ? categoryList.Count : 0)} assignment types");

                if (categoryList != null && categoryList.Count > 0)
                {
                    if (stagePanel != null) stagePanel.SetActive(true);

                    // Clear any previously spawned buttons
                    ClearSpawnedButtons();
                    
                    // Use dynamic button spawning if container and prefab are set
                    if (categoryButtonContainer != null && categoryButtonPrefab != null)
                    {
                        foreach (var category in categoryList)
                        {
                            Debug.Log($"📝 Assignment: {category.description}, ID: {category.category_id}");
                            
                            // Determine assignment type
                            string assignmentType = GetAssignmentType(category.description);
                            if (string.IsNullOrEmpty(assignmentType)) continue;
                            
                            // Create button from prefab
                            GameObject buttonObj = Instantiate(categoryButtonPrefab, categoryButtonContainer);
                            buttonObj.SetActive(true);
                            spawnedButtons.Add(buttonObj);
                            
                            // Setup button text
                            TMP_Text buttonText = buttonObj.GetComponentInChildren<TMP_Text>();
                            if (buttonText != null)
                            {
                                buttonText.text = category.description.ToUpper();
                            }
                            
                            // Color-code button by assignment type
                            Image buttonImage = buttonObj.GetComponent<Image>();
                            if (buttonImage != null)
                            {
                                if (assignmentType == "Alchemy")
                                    buttonImage.color = new Color(0.4f, 0.85f, 0.4f); // Green for True/False
                                else if (assignmentType == "MultipleChoice")
                                    buttonImage.color = new Color(1f, 0.75f, 0.3f); // Gold for Multiple Choice
                                else if (assignmentType == "Identification")
                                    buttonImage.color = new Color(0.4f, 0.7f, 1f); // Blue for Identification
                            }
                            
                            // Setup button click event
                            Button button = buttonObj.GetComponent<Button>();
                            if (button != null)
                            {
                                int capturedCategoryId = category.category_id;
                                string capturedType = assignmentType;
                                button.onClick.AddListener(() => OnCategorySelected(capturedCategoryId, capturedType));
                            }
                        }
                    }
                    else
                    {
                        // Fallback to old system if container/prefab not set
                        UseOldButtonSystem(categoryList);
                    }
                }
                else
                {
                    if (noQuestionPanel != null) noQuestionPanel.SetActive(true);
                }
            }
        }
    }

    void OnCategorySelected(int assignmentId, string assignmentType)
    {
        // Set session data
        CurrentClassSession.SelectedClassId = currentClassId;
        CurrentClassSession.SelectedCategoryId = assignmentId;

        // Also save to PlayerPrefs for backward compatibility
        PlayerPrefs.SetInt("CategoryId", assignmentId);
        PlayerPrefs.SetInt("ClassId", currentClassId);
        PlayerPrefs.Save();

        Debug.Log($"🎯 Selected assignment ID: {assignmentId}, Type: {assignmentType}");

        // Load scene based on assignment type
        if (assignmentType == "Alchemy")
            SceneManager.LoadScene("Alchemy");  // Alchemy is True/False gameplay
        else if (assignmentType == "Identification")
            SceneManager.LoadScene("Identification");
        else if (assignmentType == "MultipleChoice")
            SceneManager.LoadScene("MC-Opening");  // Multiple Choice opening scene (Treasure Hunt/Indiana Jones)
    }

    IEnumerator HideWarningAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (warningText != null)
            warningText.gameObject.SetActive(false);
    }

    void OnExitButtonClicked()
    {
        Debug.Log("🚪 Exit button clicked - closing assignment panel");
        
        // Hide stage panel
        if (stagePanel != null)
            stagePanel.SetActive(false);
        
        // Clear spawned buttons
        ClearSpawnedButtons();
        
        // Optionally: Show classroom view or go back to map
        // SceneManager.LoadScene("NEWMAP");
    }

    void ConvertToUppercase(string input)
    {
        if (classCodeInput != null && !string.IsNullOrEmpty(input))
        {
            string upperText = input.ToUpper();
            if (classCodeInput.text != upperText)
            {
                classCodeInput.text = upperText;
                classCodeInput.caretPosition = upperText.Length;
            }
        }
    }

    void ClearSpawnedButtons()
    {
        foreach (GameObject btn in spawnedButtons)
        {
            if (btn != null) Destroy(btn);
        }
        spawnedButtons.Clear();
    }

    string GetAssignmentType(string description)
    {
        if (description.Contains("True") || description.Contains("False"))
            return "Alchemy";
        else if (description.Contains("Identification"))
            return "Identification";
        else if (description.Contains("Multiple Choice"))
            return "MultipleChoice";
        return "";
    }

    void UseOldButtonSystem(List<AssignmentTypeData> categoryList)
    {
        if (alchemyButton != null)
            alchemyButton.onClick.RemoveAllListeners();
        if (identificationButton != null)
            identificationButton.onClick.RemoveAllListeners();
        if (multipleChoiceButton != null)
            multipleChoiceButton.onClick.RemoveAllListeners();

        // Hide all buttons first
        if (alchemyButton != null) alchemyButton.gameObject.SetActive(false);
        if (identificationButton != null) identificationButton.gameObject.SetActive(false);
        if (multipleChoiceButton != null) multipleChoiceButton.gameObject.SetActive(false);
        
        foreach (var category in categoryList)
        {
            int capturedCategoryId = category.category_id;
            
            if (category.description.Contains("True") || category.description.Contains("False"))
            {
                if (alchemyButton != null)
                {
                    alchemyButton.gameObject.SetActive(true);
                    alchemyButton.onClick.AddListener(() => OnCategorySelected(capturedCategoryId, "Alchemy"));
                }
            }
            else if (category.description.Contains("Identification"))
            {
                if (identificationButton != null)
                {
                    identificationButton.gameObject.SetActive(true);
                    identificationButton.onClick.AddListener(() => OnCategorySelected(capturedCategoryId, "Identification"));
                }
            }
            else if (category.description.Contains("Multiple Choice"))
            {
                if (multipleChoiceButton != null)
                {
                    multipleChoiceButton.gameObject.SetActive(true);
                    multipleChoiceButton.onClick.AddListener(() => OnCategorySelected(capturedCategoryId, "MultipleChoice"));
                }
            }
        }
    }
}

public static class CurrentClassSession
{
    public static int SelectedClassId { get; set; }
    public static int SelectedCategoryId { get; set; }
}

public static class JsonUtilityWrapper
{
    public static T FromJson<T>(string json)
    {
        string wrappedJson = "{\"items\":" + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(wrappedJson);
        return wrapper.items;
    }

    public static List<T> FromJsonList<T>(string json)
    {
        string wrappedJson = "{\"items\":" + json + "}";
        Wrapper<List<T>> wrapper = JsonUtility.FromJson<Wrapper<List<T>>>(wrappedJson);
        return wrapper.items;
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public T items;
    }
}
