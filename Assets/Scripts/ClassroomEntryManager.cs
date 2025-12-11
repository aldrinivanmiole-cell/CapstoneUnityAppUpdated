using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;

/// <summary>
/// Manages the Classroom Entry Scene with three screens:
/// 1. Entry Screen - Enter class code or scan QR
/// 2. Classroom List Screen - Shows all enrolled classrooms
/// 3. Assignment List Screen - Shows assignments for selected classroom
/// </summary>
public class ClassroomEntryManager : MonoBehaviour
{
    [Header("═══ SCREEN 1 - ENTRY ═══")]
    [Tooltip("Entry screen panel")]
    public GameObject entryScreen;
    
    [Tooltip("Class code input field")]
    public TMP_InputField classCodeInput;
    
    [Tooltip("Enter code button")]
    public Button enterCodeButton;
    
    [Tooltip("Scan QR button")]
    public Button scanQRButton;
    
    [Tooltip("Back button on entry screen")]
    public Button backButtonEntry;
    
    [Header("═══ SCREEN 2 - CLASSROOM LIST ═══")]
    [Tooltip("Classroom list screen panel")]
    public GameObject classroomListScreen;
    
    [Tooltip("Content transform for classroom items (inside ScrollView)")]
    public Transform classroomListContent;
    
    [Tooltip("Prefab for classroom item")]
    public GameObject classroomItemPrefab;
    
    [Tooltip("Add class button at bottom")]
    public Button addClassButton;
    
    [Tooltip("Back button on classroom list screen")]
    public Button backButtonList;
    
    [Header("═══ SCREEN 3 - ASSIGNMENT LIST ═══")]
    [Tooltip("Assignment list screen panel")]
    public GameObject assignmentListScreen;
    
    [Tooltip("Content transform for assignment items (inside ScrollView)")]
    public Transform assignmentListContent;
    
    [Tooltip("Prefab for assignment item")]
    public GameObject assignmentItemPrefab;
    
    [Tooltip("Subject title text in top bar")]
    public TextMeshProUGUI subjectTitleText;
    
    [Tooltip("No assignment panel (shows when no assignments)")]
    public GameObject noAssignmentPanel;
    
    [Tooltip("Back button on assignment list screen")]
    public Button backButtonAssignment;
    
    [Header("═══ API SETTINGS ═══")]
    [Tooltip("Backend API URL")]
    public string apiUrl = "https://your-api-url.com";
    
    // Private variables
    private int studentId;
    private int currentClassroomId;
    private string currentSubjectName;
    
    void Start()
    {
        // Get student ID from PlayerPrefs
        studentId = PlayerPrefs.GetInt("StudentID", 0);
        
        if (studentId == 0)
        {
            Debug.LogError("⚠️ Student ID not found! Please login first.");
            // Optionally redirect to login
            // SceneManager.LoadScene("Login");
            return;
        }
        
        // Setup button listeners
        if (enterCodeButton != null)
            enterCodeButton.onClick.AddListener(OnEnterCodeClicked);
        
        if (scanQRButton != null)
            scanQRButton.onClick.AddListener(OnScanQRClicked);
        
        if (addClassButton != null)
            addClassButton.onClick.AddListener(OnAddClassClicked);
        
        if (backButtonEntry != null)
            backButtonEntry.onClick.AddListener(() => SceneManager.LoadScene("Dashboard"));
        
        if (backButtonList != null)
            backButtonList.onClick.AddListener(ShowEntryScreen);
        
        if (backButtonAssignment != null)
            backButtonAssignment.onClick.AddListener(ShowClassroomList);
        
        // Start with classroom list if student already has classes
        ShowClassroomList();
    }
    
    /// <summary>
    /// Shows the entry screen (Screen 1)
    /// </summary>
    void ShowEntryScreen()
    {
        if (entryScreen != null) entryScreen.SetActive(true);
        if (classroomListScreen != null) classroomListScreen.SetActive(false);
        if (assignmentListScreen != null) assignmentListScreen.SetActive(false);
        
        // Clear input
        if (classCodeInput != null)
            classCodeInput.text = "";
    }
    
    /// <summary>
    /// Shows the classroom list screen (Screen 2)
    /// </summary>
    void ShowClassroomList()
    {
        if (entryScreen != null) entryScreen.SetActive(false);
        if (classroomListScreen != null) classroomListScreen.SetActive(true);
        if (assignmentListScreen != null) assignmentListScreen.SetActive(false);
        
        LoadClassrooms();
    }
    
    /// <summary>
    /// Shows the assignment list screen (Screen 3)
    /// </summary>
    void ShowAssignmentList(int classroomId, string subjectName)
    {
        currentClassroomId = classroomId;
        currentSubjectName = subjectName;
        
        if (entryScreen != null) entryScreen.SetActive(false);
        if (classroomListScreen != null) classroomListScreen.SetActive(false);
        if (assignmentListScreen != null) assignmentListScreen.SetActive(true);
        
        // Update subject title
        if (subjectTitleText != null)
            subjectTitleText.text = subjectName.ToUpper();
        
        LoadAssignments(classroomId);
    }
    
    #region Screen 1 - Entry Actions
    
    /// <summary>
    /// Called when "Enter Code" button is clicked
    /// </summary>
    void OnEnterCodeClicked()
    {
        if (classCodeInput == null)
        {
            Debug.LogError("⚠️ Class code input field is not assigned!");
            return;
        }
        
        string code = classCodeInput.text.Trim();
        
        if (string.IsNullOrEmpty(code))
        {
            Debug.LogWarning("⚠️ Please enter a class code");
            // TODO: Show error message to user
            return;
        }
        
        StartCoroutine(JoinClassroom(code));
    }
    
    /// <summary>
    /// Called when "Scan QR" button is clicked
    /// </summary>
    void OnScanQRClicked()
    {
        Debug.Log("📱 Opening QR Scanner...");
        // TODO: Implement QR scanner
        // Option 1: Load QR scanner scene
        // SceneManager.LoadScene("QRScanner");
        
        // Option 2: Use native camera plugin
        // StartQRScanner();
    }
    
    /// <summary>
    /// Called when "Add Class" button is clicked from classroom list
    /// </summary>
    void OnAddClassClicked()
    {
        ShowEntryScreen();
    }
    
    /// <summary>
    /// Joins a classroom using class code
    /// </summary>
    IEnumerator JoinClassroom(string classCode)
    {
        Debug.Log($"🔗 Joining classroom with code: {classCode}");
        
        WWWForm form = new WWWForm();
        form.AddField("student_id", studentId);
        form.AddField("class_code", classCode);
        
        using (UnityWebRequest request = UnityWebRequest.Post($"{apiUrl}/join_classroom", form))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("✅ Successfully joined classroom!");
                // TODO: Show success message
                ShowClassroomList();
            }
            else
            {
                Debug.LogError($"❌ Failed to join classroom: {request.error}");
                // TODO: Show error message to user
            }
        }
    }
    
    #endregion
    
    #region Screen 2 - Classroom List
    
    /// <summary>
    /// Loads all classrooms for the student
    /// </summary>
    void LoadClassrooms()
    {
        StartCoroutine(FetchClassrooms());
    }
    
    /// <summary>
    /// Fetches classrooms from API
    /// </summary>
    IEnumerator FetchClassrooms()
    {
        Debug.Log("📚 Loading classrooms...");
        
        // Clear existing items
        if (classroomListContent != null)
        {
            foreach (Transform child in classroomListContent)
            {
                Destroy(child.gameObject);
            }
        }
        
        using (UnityWebRequest request = UnityWebRequest.Get($"{apiUrl}/get_classrooms?student_id={studentId}"))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                Debug.Log($"📥 Received: {json}");
                
                // Wrap response in object if API returns array directly
                if (json.StartsWith("["))
                {
                    json = "{\"classrooms\":" + json + "}";
                }
                
                ClassroomListResponse response = JsonUtility.FromJson<ClassroomListResponse>(json);
                
                if (response != null && response.classrooms != null)
                {
                    Debug.Log($"✅ Loaded {response.classrooms.Length} classrooms");
                    
                    foreach (ClassroomData classroom in response.classrooms)
                    {
                        CreateClassroomItem(classroom);
                    }
                }
            }
            else
            {
                Debug.LogError($"❌ Failed to load classrooms: {request.error}");
            }
        }
    }
    
    /// <summary>
    /// Creates a classroom item in the list
    /// </summary>
    void CreateClassroomItem(ClassroomData data)
    {
        if (classroomItemPrefab == null || classroomListContent == null)
        {
            Debug.LogError("⚠️ Classroom item prefab or content is not assigned!");
            return;
        }
        
        GameObject item = Instantiate(classroomItemPrefab, classroomListContent);
        
        // Set subject name
        TextMeshProUGUI subjectText = item.transform.Find("SubjectNameText")?.GetComponent<TextMeshProUGUI>();
        if (subjectText != null)
            subjectText.text = data.subjectName.ToUpper();
        
        // Show/hide exclamation icon based on assignments
        GameObject exclamationIcon = item.transform.Find("ExclamationIcon")?.gameObject;
        if (exclamationIcon != null)
            exclamationIcon.SetActive(data.hasAssignments);
        
        // Setup button click
        Button button = item.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => ShowAssignmentList(data.classroomId, data.subjectName));
        }
    }
    
    #endregion
    
    #region Screen 3 - Assignment List
    
    /// <summary>
    /// Loads assignments for a classroom
    /// </summary>
    void LoadAssignments(int classroomId)
    {
        StartCoroutine(FetchAssignments(classroomId));
    }
    
    /// <summary>
    /// Fetches assignments from API
    /// </summary>
    IEnumerator FetchAssignments(int classroomId)
    {
        Debug.Log($"📝 Loading assignments for classroom {classroomId}...");
        
        // Clear existing items
        if (assignmentListContent != null)
        {
            foreach (Transform child in assignmentListContent)
            {
                Destroy(child.gameObject);
            }
        }
        
        using (UnityWebRequest request = UnityWebRequest.Get($"{apiUrl}/get_assignments?student_id={studentId}&classroom_id={classroomId}"))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                Debug.Log($"📥 Received: {json}");
                
                // Wrap response in object if API returns array directly
                if (json.StartsWith("["))
                {
                    json = "{\"assignments\":" + json + "}";
                }
                
                AssignmentListResponse response = JsonUtility.FromJson<AssignmentListResponse>(json);
                
                if (response != null && response.assignments != null && response.assignments.Length > 0)
                {
                    Debug.Log($"✅ Loaded {response.assignments.Length} assignments");
                    
                    // Hide no assignment panel
                    if (noAssignmentPanel != null)
                        noAssignmentPanel.SetActive(false);
                    
                    foreach (AssignmentData assignment in response.assignments)
                    {
                        CreateAssignmentItem(assignment);
                    }
                }
                else
                {
                    Debug.Log("📭 No assignments found");
                    
                    // Show no assignment panel
                    if (noAssignmentPanel != null)
                        noAssignmentPanel.SetActive(true);
                }
            }
            else
            {
                Debug.LogError($"❌ Failed to load assignments: {request.error}");
            }
        }
    }
    
    /// <summary>
    /// Creates an assignment item in the list
    /// </summary>
    void CreateAssignmentItem(AssignmentData data)
    {
        if (assignmentItemPrefab == null || assignmentListContent == null)
        {
            Debug.LogError("⚠️ Assignment item prefab or content is not assigned!");
            return;
        }
        
        GameObject item = Instantiate(assignmentItemPrefab, assignmentListContent);
        
        // Set assignment name
        TextMeshProUGUI nameText = item.transform.Find("AssignmentNameText")?.GetComponent<TextMeshProUGUI>();
        if (nameText != null)
            nameText.text = data.assignmentName;
        
        // Show/hide submitted overlay
        GameObject statusOverlay = item.transform.Find("StatusOverlay")?.gameObject;
        if (statusOverlay != null)
            statusOverlay.SetActive(data.isSubmitted);
        
        // Setup button
        Button button = item.GetComponent<Button>();
        if (button != null)
        {
            button.interactable = !data.isSubmitted;
            
            if (!data.isSubmitted)
            {
                button.onClick.AddListener(() => OpenAssignment(data));
            }
        }
    }
    
    /// <summary>
    /// Opens an assignment (loads appropriate game scene)
    /// </summary>
    void OpenAssignment(AssignmentData data)
    {
        Debug.Log($"🎮 Opening assignment: {data.assignmentName} (Type: {data.assignmentType})");
        
        // Store assignment data in PlayerPrefs
        PlayerPrefs.SetInt("AssignmentID", data.assignmentId);
        PlayerPrefs.SetString("AssignmentType", data.assignmentType);
        PlayerPrefs.SetString("AssignmentName", data.assignmentName);
        PlayerPrefs.Save();
        
        // Load appropriate scene based on assignment type
        string sceneToLoad = "";
        
        switch (data.assignmentType.ToLower())
        {
            case "multiple choice":
                sceneToLoad = "TreasureHuntersOpening";
                break;
            case "true/false":
            case "true false":
                sceneToLoad = "AlchemyOpening";
                break;
            case "enumeration":
                sceneToLoad = "EnumerationOpening";
                break;
            case "fill in the blank":
            case "fill-in-the-blank":
                sceneToLoad = "FillInTheBlankOpening";
                break;
            case "essay":
                sceneToLoad = "EssayOpening";
                break;
            default:
                Debug.LogWarning($"⚠️ Unknown assignment type: {data.assignmentType}");
                sceneToLoad = "TreasureHuntersOpening"; // Default fallback
                break;
        }
        
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.Log($"🚀 Loading scene: {sceneToLoad}");
            SceneManager.LoadScene(sceneToLoad);
        }
    }
    
    #endregion
}

#region Data Classes

/// <summary>
/// Data structure for a classroom
/// </summary>
[System.Serializable]
public class ClassroomData
{
    public int classroomId;
    public string subjectName;
    public bool hasAssignments;
}

/// <summary>
/// Response structure for classroom list
/// </summary>
[System.Serializable]
public class ClassroomListResponse
{
    public ClassroomData[] classrooms;
}

/// <summary>
/// Data structure for an assignment
/// </summary>
[System.Serializable]
public class AssignmentData
{
    public int assignmentId;
    public string assignmentName;
    public string assignmentType;
    public bool isSubmitted;
}

/// <summary>
/// Response structure for assignment list
/// </summary>
[System.Serializable]
public class AssignmentListResponse
{
    public AssignmentData[] assignments;
}

#endregion
