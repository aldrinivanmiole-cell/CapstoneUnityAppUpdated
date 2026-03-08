using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Text;
using System.Globalization;

public class ClassroomManager : MonoBehaviour
{
    private const string ClassroomCacheKey = "LegacyRoomAssignments";

    [System.Serializable]
    private class ApiErrorPayload
    {
        public string detail;
        public string message;
    }

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
        public string subjectName;
        public string name;
        public string dueDate;
    }

    [System.Serializable]
    private class StudentSubjectData
    {
        public int class_id;
        public string subject;
        public string subject_name;
        public string activity;
        public string activity_title;
        public string latest_activity_due_date;
        public string activity_due_date;
        public string latest_activity_deadline;
        public string latest_activity_due_date_display;
    }

    [System.Serializable]
    private class StudentSubjectsResponse
    {
        public List<StudentSubjectData> subjects;
    }

    [System.Serializable]
    private class JoinClassResponse
    {
        public string status;
        public string subject;
        public string message;
        public JoinClassInfo class_info;
    }

    [System.Serializable]
    private class JoinClassInfo
    {
        public int id;
        public string name;
        public string class_code;
    }

    [System.Serializable]
    public class AssignmentTypeData
    {
        public int category_id;
        public string description;
        public string assignment_type;
        public bool is_completed;
        public string due_date_text;
    }

    [System.Serializable]
    private class AssignmentServerItem
    {
        public int assignment_id;
        public int id;
        public string title;
        public string assignment_type;
        public string type;
        public bool is_submitted;
        public bool isSubmitted;
        public bool is_completed;
        public string due_date;
        public string deadline;
        public string due_at;
        public string dueDate;
        public string due_date_display;
        public string deadline_display;
        public string deadlineDisplay;
    }

    [System.Serializable]
    private class AssignmentsResponse
    {
        public List<AssignmentServerItem> assignments;
        public List<AssignmentServerItem> activities;
    }

    [System.Serializable]
    private class LegacyAssignmentItem
    {
        public int assignmentId;
        public int assignment_id;
        public string assignmentName;
        public string assignment_name;
        public string title;
        public string due_date;
        public string deadline;
        public string due_at;
        public string dueDate;
        public string due_date_display;
        public string deadline_display;
        public string deadlineDisplay;
        public string assignmentType;
        public string assignment_type;
        public bool isSubmitted;
    }

    [System.Serializable]
    private class LegacyAssignmentsResponse
    {
        public List<LegacyAssignmentItem> assignments;
    }

    [System.Serializable]
    private class ClassroomListWrapper
    {
        public List<ClassroomData> classrooms;
    }

    // Persistent class_id → room_no assignments (one entry per enrolled class).
    [System.Serializable]
    private class ClassRoomMapEntry
    {
        public int classId;
        public int roomNo;
    }

    [System.Serializable]
    private class ClassRoomMapWrapper
    {
        public List<ClassRoomMapEntry> entries = new List<ClassRoomMapEntry>();
    }

    [System.Serializable]
    private class AssignmentTypeListWrapper
    {
        public List<AssignmentTypeData> categories;
    }

    [Header("Classroom Entry UI (Submit/QR Buttons)")]
    public TMP_InputField entryCodeInput;
    public Button submitButton;
    public Button scanQRButton;

    [Header("QR Camera Preview (Optional)")]
    public GameObject qrCameraPreviewPanel;
    public RawImage qrCameraPreviewImage;
    public Button qrCameraCloseButton;

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

    private bool enterCodeButtonHidden;
    private bool isLoadingRoomAssignments;

    private string baseUrl = "https://homequest-c3k7.onrender.com/";
    private int studentId;
    private int currentClassId;
    private int selectedRoomId;
    private Dictionary<int, ClassroomData> classroomDataByRoom = new Dictionary<int, ClassroomData>();
    private HashSet<int> pendingRoomIds = new HashSet<int>();
    private WebCamTexture qrCameraTexture;
    private bool isQrCameraOpening;

    private string BuildStudentScopedKey(string baseKey)
    {
        if (string.IsNullOrWhiteSpace(baseKey))
            return string.Empty;

        if (studentId > 0)
            return baseKey + "_student_" + studentId;

        return baseKey;
    }

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

        string savedBaseUrl = PlayerPrefs.GetString("ApiBaseUrl", string.Empty);
        if (!string.IsNullOrWhiteSpace(savedBaseUrl))
            baseUrl = savedBaseUrl.Trim().TrimEnd('/') + "/";

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
            submitButton.onClick.AddListener(OnEnterCodeButtonClicked);

        if (scanQRButton != null)
            scanQRButton.onClick.AddListener(OnScanQR);
        if (qrCameraCloseButton != null)
            qrCameraCloseButton.onClick.AddListener(CloseQrCameraPreview);
        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitButtonClicked);
        if (joinClassButton != null)
            joinClassButton.onClick.AddListener(OnJoinClassClicked);

        if (entryCodeInput != null)
            entryCodeInput.onSubmit.AddListener(_ => OnSubmitClassCode());

        foreach (var room in rooms)
            AddClickListener(room.roomImage, room.roomId);

        StartCoroutine(LoadRoomAssignments());
    }

    void OnEnterCodeButtonClicked()
    {
        if (entryCodeInput == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(entryCodeInput.text))
        {
            if (submitButton != null && !submitButton.gameObject.activeSelf)
                submitButton.gameObject.SetActive(true);
            FocusEntryInput();
            return;
        }

        OnSubmitClassCode();
    }

    void FocusEntryInput()
    {
        entryCodeInput.Select();
        entryCodeInput.ActivateInputField();
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
        string code = NormalizeClassCode(classCodeInput.text);
        if (string.IsNullOrEmpty(code))
        {
            Debug.LogWarning("Class code is empty!");
            return;
        }

        pendingRoomIds.Add(selectedRoomId);
        StartCoroutine(SaveClassroom(selectedRoomId, code));
    }

    /// <summary>
    /// Called when Submit button is clicked (bottom of ClassRooms panel)
    /// </summary>
    void OnSubmitClassCode()
    {
        if (isLoadingRoomAssignments)
        {
            if (warningText != null)
            {
                warningText.text = "Please wait, classrooms are still loading";
                warningText.gameObject.SetActive(true);
                StartCoroutine(HideWarningAfterDelay(2f));
            }
            return;
        }

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

        string code = NormalizeClassCode(entryCodeInput.text);
        if (string.IsNullOrEmpty(code))
        {
            if (warningText != null)
            {
                warningText.text = "Invalid class code format";
                warningText.gameObject.SetActive(true);
                StartCoroutine(HideWarningAfterDelay(3f));
            }
            return;
        }
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
        pendingRoomIds.Add(emptyRoomId);
        StartCoroutine(SaveClassroom(emptyRoomId, code));
    }

    /// <summary>
    /// Called when Scan QR button is clicked
    /// </summary>
    void OnScanQR()
    {
        Debug.Log("📷 Opening QR Scanner...");

        if (isLoadingRoomAssignments)
        {
            if (warningText != null)
            {
                warningText.text = "Please wait, classrooms are still loading";
                warningText.gameObject.SetActive(true);
                StartCoroutine(HideWarningAfterDelay(2f));
            }
            return;
        }

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
        pendingRoomIds.Add(emptyRoomId);
        StartCoroutine(SaveClassroom(emptyRoomId, "TEST123"));
#else
        StartQRScanner();
#endif
    }

    /// <summary>
    /// Start QR code scanner (requires QR scanner plugin like ZXing)
    /// </summary>
    void StartQRScanner()
    {
        if (!isActiveAndEnabled)
            return;

        StartCoroutine(OpenDeviceCameraForQr());
    }

    private IEnumerator OpenDeviceCameraForQr()
    {
        if (isQrCameraOpening)
            yield break;

        isQrCameraOpening = true;

        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);

        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            if (warningText != null)
            {
                warningText.text = "Camera permission denied";
                warningText.gameObject.SetActive(true);
                StartCoroutine(HideWarningAfterDelay(3f));
            }

            isQrCameraOpening = false;
            yield break;
        }

        EnsureQrCameraOverlay();

        if (qrCameraPreviewImage == null)
        {
            Debug.LogError("QR preview image is missing.");
            isQrCameraOpening = false;
            yield break;
        }

        WebCamDevice[] cameras = WebCamTexture.devices;
        if (cameras == null || cameras.Length == 0)
        {
            if (warningText != null)
            {
                warningText.text = "No camera found on device";
                warningText.gameObject.SetActive(true);
                StartCoroutine(HideWarningAfterDelay(3f));
            }

            isQrCameraOpening = false;
            yield break;
        }

        int cameraIndex = 0;
        for (int i = 0; i < cameras.Length; i++)
        {
            if (!cameras[i].isFrontFacing)
            {
                cameraIndex = i;
                break;
            }
        }

        StopQrCameraPreview();

        qrCameraTexture = new WebCamTexture(cameras[cameraIndex].name, 1280, 720, 30);
        qrCameraPreviewImage.texture = qrCameraTexture;
        qrCameraPreviewImage.color = Color.white;

        if (qrCameraPreviewPanel != null)
            qrCameraPreviewPanel.SetActive(true);

        qrCameraTexture.Play();

        if (warningText != null)
        {
            warningText.text = "Camera opened. Point to class QR code.";
            warningText.gameObject.SetActive(true);
            StartCoroutine(HideWarningAfterDelay(2f));
        }

        isQrCameraOpening = false;
    }

    private void EnsureQrCameraOverlay()
    {
        if (qrCameraPreviewPanel != null && qrCameraPreviewImage != null)
            return;

        Canvas parentCanvas = FindFirstObjectByType<Canvas>();
        if (parentCanvas == null)
        {
            Debug.LogError("No Canvas found for QR camera preview.");
            return;
        }

        GameObject panelObj = new GameObject("QrCameraPreviewPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObj.transform.SetParent(parentCanvas.transform, false);

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = panelObj.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.88f);

        GameObject rawObj = new GameObject("QrCameraPreviewImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        rawObj.transform.SetParent(panelObj.transform, false);

        RectTransform rawRect = rawObj.GetComponent<RectTransform>();
        rawRect.anchorMin = new Vector2(0.05f, 0.16f);
        rawRect.anchorMax = new Vector2(0.95f, 0.86f);
        rawRect.offsetMin = Vector2.zero;
        rawRect.offsetMax = Vector2.zero;

        RawImage rawImage = rawObj.GetComponent<RawImage>();
        rawImage.color = new Color(1f, 1f, 1f, 0.2f);

        GameObject closeObj = new GameObject("QrCameraCloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        closeObj.transform.SetParent(panelObj.transform, false);

        RectTransform closeRect = closeObj.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0.06f);
        closeRect.anchorMax = new Vector2(0.5f, 0.06f);
        closeRect.pivot = new Vector2(0.5f, 0.5f);
        closeRect.sizeDelta = new Vector2(260f, 74f);

        Image closeImage = closeObj.GetComponent<Image>();
        closeImage.color = new Color(0.85f, 0.18f, 0.18f, 0.95f);

        Button closeButton = closeObj.GetComponent<Button>();
        closeButton.onClick.AddListener(CloseQrCameraPreview);

        GameObject labelObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(closeObj.transform, false);

        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObj.GetComponent<TextMeshProUGUI>();
        label.text = "Close Camera";
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 30f;
        label.color = Color.white;

        qrCameraPreviewPanel = panelObj;
        qrCameraPreviewImage = rawImage;
        qrCameraCloseButton = closeButton;
        qrCameraPreviewPanel.SetActive(false);
    }

    private void CloseQrCameraPreview()
    {
        StopQrCameraPreview();
        if (qrCameraPreviewPanel != null)
            qrCameraPreviewPanel.SetActive(false);
    }

    private void StopQrCameraPreview()
    {
        if (qrCameraTexture != null)
        {
            if (qrCameraTexture.isPlaying)
                qrCameraTexture.Stop();

            Destroy(qrCameraTexture);
            qrCameraTexture = null;
        }
    }

    private void OnDisable()
    {
        StopQrCameraPreview();
    }

    private void OnDestroy()
    {
        StopQrCameraPreview();
    }

    /// <summary>
    /// Find the first empty room slot
    /// </summary>
    int FindFirstEmptyRoom()
    {
        if (rooms == null)
            return -1;

        HashSet<int> occupied = new HashSet<int>();

        // 1. Persistent map (class_id OR placeholder) → room_no.
        Dictionary<int, int> classRoomMap = LoadClassRoomMap();
        foreach (int r in classRoomMap.Values)
            if (r > 0) occupied.Add(r);

        // 2. In-memory state — catches edge cases where the map wasn't saved.
        foreach (int r in classroomDataByRoom.Keys)
            if (r > 0) occupied.Add(r);

        // 3. In-flight reservations (waiting for server response).
        foreach (int r in pendingRoomIds)
            if (r > 0) occupied.Add(r);

        for (int i = 0; i < rooms.Count; i++)
        {
            RoomUI room = rooms[i];
            if (room == null || room.roomId <= 0)
                continue;

            if (!occupied.Contains(room.roomId))
            {
                Debug.Log($"✅ Found empty room: r{room.roomId}");
                return room.roomId;
            }
        }

        return -1; // All rooms are full
    }

    IEnumerator SaveClassroom(int roomNo, string classCode)
    {
        string normalizedCode = NormalizeClassCode(classCode);
        if (string.IsNullOrEmpty(normalizedCode))
        {
            pendingRoomIds.Remove(roomNo);
            if (warningText != null)
            {
                warningText.text = "Invalid class code format";
                warningText.gameObject.SetActive(true);
                StartCoroutine(HideWarningAfterDelay(3f));
            }
            yield break;
        }

        bool joined = false;
        string lastError = "";
        string joinedSubject = string.Empty;
        ClassroomData joinedClassroom = null;

        string joinUrl = baseUrl.TrimEnd('/') + "/student/join-class";
        string json = "{\"student_id\":" + studentId + ",\"class_code\":\"" + normalizedCode + "\"}";

        using (UnityWebRequest request = new UnityWebRequest(joinUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseBody = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                joinedClassroom = ExtractJoinedClassroom(responseBody);
                joinedSubject = ResolveClassroomLabel(joinedClassroom);
                joined = true;
            }
            else
            {
                string responseText = request.downloadHandler != null ? request.downloadHandler.text : request.error;
                lastError = "student/join-class(json) => " + responseText;
                if (IsAlreadyEnrolledError(lastError))
                {
                    Debug.Log("ℹ️ Student already enrolled in this class. Loading existing classrooms.");
                    joined = true;
                }
            }
        }

        if (!joined)
        {
            WWWForm modernForm = new WWWForm();
            modernForm.AddField("student_id", studentId);
            modernForm.AddField("class_code", normalizedCode);

            using (UnityWebRequest request = UnityWebRequest.Post(baseUrl.TrimEnd('/') + "/student/join-class", modernForm))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseBody = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                    joinedClassroom = ExtractJoinedClassroom(responseBody);
                    joinedSubject = ResolveClassroomLabel(joinedClassroom);
                    joined = true;
                }
                else
                {
                    string responseText = request.downloadHandler != null ? request.downloadHandler.text : request.error;
                    lastError = "student/join-class(form) => " + responseText;
                    if (IsAlreadyEnrolledError(lastError))
                    {
                        Debug.Log("ℹ️ Student already enrolled in this class. Loading existing classrooms.");
                        joined = true;
                    }
                }
            }
        }

        if (!joined)
        {
            WWWForm fallbackForm = new WWWForm();
            fallbackForm.AddField("student_id", studentId);
            fallbackForm.AddField("class_code", normalizedCode);

            using (UnityWebRequest request = UnityWebRequest.Post(baseUrl + "join_classroom", fallbackForm))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseBody = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                    joinedClassroom = ExtractJoinedClassroom(responseBody);
                    joinedSubject = ResolveClassroomLabel(joinedClassroom);
                    joined = true;
                }
                else
                {
                    string responseText = request.downloadHandler != null ? request.downloadHandler.text : request.error;
                    lastError = "join_classroom => " + responseText;
                    if (IsAlreadyEnrolledError(lastError))
                    {
                        Debug.Log("ℹ️ Student already enrolled in this class. Loading existing classrooms.");
                        joined = true;
                    }
                }
            }
        }

        if (!joined)
        {
            WWWForm legacyForm = new WWWForm();
            legacyForm.AddField("student_id", studentId);
            legacyForm.AddField("room_no", roomNo);
            legacyForm.AddField("code", normalizedCode);

            using (UnityWebRequest request = UnityWebRequest.Post(baseUrl + "save_classroom", legacyForm))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseBody = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                    joinedClassroom = ExtractJoinedClassroom(responseBody);
                    joinedSubject = ResolveClassroomLabel(joinedClassroom);
                    joined = true;
                }
                else
                {
                    string responseText = request.downloadHandler != null ? request.downloadHandler.text : request.error;
                    lastError = "save_classroom => " + responseText;
                    if (IsAlreadyEnrolledError(lastError))
                    {
                        Debug.Log("ℹ️ Student already enrolled in this class. Loading existing classrooms.");
                        joined = true;
                    }
                }
            }
        }

        if (!joined)
        {
            pendingRoomIds.Remove(roomNo);
            Debug.LogError($"Error saving classroom: {lastError}");

            if (warningText != null)
            {
                warningText.text = MapJoinErrorToUserMessage(lastError);
                warningText.gameObject.SetActive(true);
                StartCoroutine(HideWarningAfterDelay(3f));
            }
            yield break;
        }

        Debug.Log("✅ Classroom saved successfully!");

        if (joinedClassroom != null)
        {
            joinedClassroom.room_no = roomNo;
            joinedSubject = ResolveClassroomLabel(joinedClassroom);
            classroomDataByRoom[roomNo] = joinedClassroom;
        }

        // Always persist the slot as occupied.
        // Use real class_id as key when available; otherwise use a negative-roomNo placeholder
        // so the slot stays reserved even if class_id wasn't returned by the server.
        {
            Dictionary<int, int> classRoomMap = LoadClassRoomMap();
            classRoomMap.Remove(-roomNo); // remove any old placeholder for this slot
            if (joinedClassroom != null && joinedClassroom.class_id > 0)
                classRoomMap[joinedClassroom.class_id] = roomNo;
            else
                classRoomMap[-roomNo] = roomNo; // placeholder: negative key, positive value
            SaveClassRoomMap(classRoomMap);
        }

        pendingRoomIds.Remove(roomNo);

        if (!string.IsNullOrWhiteSpace(joinedSubject))
            UpdateRoomLabel(roomNo, joinedSubject.Trim());

        if (classCodeInput != null) classCodeInput.text = "";
        if (entryCodeInput != null) entryCodeInput.text = "";

        if (addClassPanel != null) addClassPanel.SetActive(false);
        StartCoroutine(LoadRoomAssignments());
    }

    private string NormalizeClassCode(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "";

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (char.IsLetterOrDigit(c))
                builder.Append(char.ToUpperInvariant(c));
        }

        return builder.ToString();
    }

    private bool IsAlreadyEnrolledError(string rawError)
    {
        if (string.IsNullOrEmpty(rawError))
            return false;

        string normalized = rawError.ToLowerInvariant();
        return normalized.Contains("already enrolled");
    }

    private string MapJoinErrorToUserMessage(string rawError)
    {
        if (string.IsNullOrEmpty(rawError))
            return "Failed to join class. Check code.";

        string lowered = rawError.ToLowerInvariant();
        if (lowered.Contains("student not found"))
            return "Session expired. Please log in again.";
        if (lowered.Contains("not found") && lowered.Contains("student/join-class") || lowered.Contains("detail\":\"not found\""))
            return "Server endpoint mismatch. Please update the website/backend deployment.";
        if (lowered.Contains("invalid class code") || lowered.Contains("not found"))
            return "Invalid class code.";
        if (lowered.Contains("archived"))
            return "Class is archived and cannot be joined.";
        if (lowered.Contains("already enrolled"))
            return "You are already enrolled in this class.";

        try
        {
            ApiErrorPayload payload = JsonUtility.FromJson<ApiErrorPayload>(rawError);
            if (payload != null)
            {
                if (!string.IsNullOrEmpty(payload.detail))
                    return payload.detail;
                if (!string.IsNullOrEmpty(payload.message))
                    return payload.message;
            }
        }
        catch
        {
        }

        return "Failed to join class. Check code.";
    }

    IEnumerator LoadRoomAssignments()
    {
        isLoadingRoomAssignments = true;
        List<ClassroomData> classroomList = null;
        List<ClassroomData> firstEmptyResult = null;
        string firstEmptyBase = string.Empty;
        string lastError = "";
        List<string> apiBases = BuildCandidateApiBases();

        for (int i = 0; i < apiBases.Count && classroomList == null; i++)
        {
            string baseApi = apiBases[i];
            string legacyUrl = baseApi + "/get_classrooms?student_id=" + studentId;

            using (UnityWebRequest request = UnityWebRequest.Get(legacyUrl))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = request.downloadHandler != null ? request.downloadHandler.text : "";
                    List<ClassroomData> parsed = JsonUtilityWrapper.FromJsonList<ClassroomData>(jsonResponse);
                    if (parsed != null)
                    {
                        if (parsed.Count > 0)
                        {
                            classroomList = parsed;
                            PlayerPrefs.SetString("ApiBaseUrl", baseApi);
                            PlayerPrefs.Save();
                            break;
                        }

                        if (firstEmptyResult == null)
                        {
                            firstEmptyResult = parsed;
                            firstEmptyBase = baseApi;
                        }
                    }
                }

                lastError = request.error + " | endpoint=" + legacyUrl + " | code=" + request.responseCode;
            }

            string[] modernPaths = new string[] { "/student/subjects", "/api/student/subjects" };
            for (int p = 0; p < modernPaths.Length && classroomList == null; p++)
            {
                string endpoint = baseApi + modernPaths[p];
                string payload = "{\"student_id\":" + studentId + "}";

                using (UnityWebRequest request = new UnityWebRequest(endpoint, "POST"))
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.timeout = 12;

                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string body = request.downloadHandler != null ? request.downloadHandler.text : "";
                        StudentSubjectsResponse parsed = null;
                        try
                        {
                            parsed = JsonUtility.FromJson<StudentSubjectsResponse>(body);
                        }
                        catch
                        {
                        }

                        if (parsed != null && parsed.subjects != null)
                        {
                            List<ClassroomData> converted = ConvertSubjectsToClassrooms(parsed.subjects);
                            if (converted != null)
                            {
                                if (converted.Count > 0)
                                {
                                    classroomList = converted;
                                    PlayerPrefs.SetString("ApiBaseUrl", baseApi);
                                    PlayerPrefs.Save();
                                }
                                else if (firstEmptyResult == null)
                                {
                                    firstEmptyResult = converted;
                                    firstEmptyBase = baseApi;
                                }
                            }
                        }
                    }
                    else
                    {
                        lastError = request.error + " | endpoint=" + endpoint + " | code=" + request.responseCode;
                    }
                }
            }
        }

        if (classroomList == null && firstEmptyResult != null)
        {
            classroomList = firstEmptyResult;
            if (!string.IsNullOrWhiteSpace(firstEmptyBase))
            {
                PlayerPrefs.SetString("ApiBaseUrl", firstEmptyBase);
                PlayerPrefs.Save();
            }
        }

        if (classroomList == null)
        {
            Debug.LogError("Error loading classrooms: " + lastError);
            classroomList = LoadCachedClassrooms();
            if (classroomList == null || classroomList.Count == 0)
            {
                isLoadingRoomAssignments = false;
                yield break;
            }
        }

        // class_id → room_no map is the single authority for slot assignments.
        // Subject names always come from the server (teacher can rename anytime).
        Dictionary<int, int> classRoomMap = LoadClassRoomMap();

        // Replace placeholder entries (negative key = slot reserved before class_id was known)
        // with proper class_id entries as the server now tells us the real IDs.
        // First, collect the room_nos that placeholders are holding.
        HashSet<int> placeholderRooms = new HashSet<int>();
        List<int> placeholderKeys = new List<int>();
        foreach (var kv in classRoomMap)
        {
            if (kv.Key < 0)
            {
                placeholderKeys.Add(kv.Key);
                placeholderRooms.Add(kv.Value);
            }
        }
        // Remove placeholders whose rooms are now claimed by a real class_id below.
        // (Any placeholders for rooms not in this server list stay until the next load.)

        HashSet<int> usedRooms = new HashSet<int>();

        classroomDataByRoom.Clear();
        foreach (var room in rooms)
        {
            if (room != null)
            {
                room.roomText.text = "";
                room.roomImage.color = new Color(0.8f, 0.8f, 0.8f);
            }
        }

        Debug.Log($"📚 Loading {classroomList.Count} classrooms from server");

        foreach (var classroom in classroomList)
        {
            if (classroom == null || classroom.class_id <= 0)
                continue;

            int roomNo = 0;

            // Use the previously saved slot for this class.
            if (classRoomMap.TryGetValue(classroom.class_id, out int savedRoomNo) && savedRoomNo > 0)
            {
                bool roomExists = rooms.Exists(r => r != null && r.roomId == savedRoomNo);
                if (roomExists && !usedRooms.Contains(savedRoomNo))
                    roomNo = savedRoomNo;
            }

            // No saved slot or it's already taken — assign the next free slot.
            if (roomNo <= 0)
            {
                for (int i = 0; i < rooms.Count; i++)
                {
                    if (rooms[i] != null && rooms[i].roomId > 0 && !usedRooms.Contains(rooms[i].roomId))
                    {
                        roomNo = rooms[i].roomId;
                        break;
                    }
                }
            }

            if (roomNo <= 0)
            {
                Debug.LogWarning($"⚠️ No slot available for class {classroom.class_id}");
                continue;
            }

            // Persist any newly assigned or confirmed slot.
            // Remove placeholder for this room if one exists.
            if (placeholderRooms.Contains(roomNo))
            {
                for (int pi = placeholderKeys.Count - 1; pi >= 0; pi--)
                {
                    if (classRoomMap.TryGetValue(placeholderKeys[pi], out int pRoom) && pRoom == roomNo)
                    {
                        classRoomMap.Remove(placeholderKeys[pi]);
                        placeholderKeys.RemoveAt(pi);
                    }
                }
                placeholderRooms.Remove(roomNo);
            }
            classRoomMap[classroom.class_id] = roomNo;
            usedRooms.Add(roomNo);
            classroom.room_no = roomNo;
            classroomDataByRoom[roomNo] = classroom;

            Debug.Log($"📖 class_id={classroom.class_id} '{ResolveClassroomLabel(classroom)}' → slot {roomNo}");

            RoomUI roomUI = rooms.Find(r => r != null && r.roomId == roomNo);
            if (roomUI != null)
            {
                roomUI.roomText.text = ResolveClassroomLabel(classroom);
                roomUI.roomImage.color = Color.white;
            }
        }

        SaveClassRoomMap(classRoomMap);
        isLoadingRoomAssignments = false;
    }

    private List<ClassroomData> ConvertSubjectsToClassrooms(List<StudentSubjectData> subjects)
    {
        List<ClassroomData> result = new List<ClassroomData>();
        if (subjects == null)
            return result;

        for (int i = 0; i < subjects.Count; i++)
        {
            StudentSubjectData subject = subjects[i];
            if (subject == null)
                continue;

            string subjectLabel = !string.IsNullOrWhiteSpace(subject.subject_name)
                ? subject.subject_name.Trim()
                : (!string.IsNullOrWhiteSpace(subject.subject) ? subject.subject.Trim() : "");

            if (string.IsNullOrWhiteSpace(subjectLabel))
                continue;

            result.Add(new ClassroomData
            {
                room_no = 0,
                class_id = subject.class_id,
                description = subjectLabel,
                subjectName = subjectLabel,
                name = subjectLabel,
                dueDate = ResolveSubjectDueDate(subject)
            });
        }

        return result;
    }

    private int ResolveFallbackRoomNo(int classId)
    {
        if (rooms == null || rooms.Count == 0)
            return 0;

        int index = Mathf.Abs(classId) % rooms.Count;
        return rooms[index].roomId;
    }

    private int ResolveUniqueRoomNo(int preferredRoomNo, int previousRoomNo, int classId, HashSet<int> usedRoomIds)
    {
        if (TryClaimRoom(preferredRoomNo, usedRoomIds, out int claimedRoomNo))
            return claimedRoomNo;

        if (TryClaimRoom(previousRoomNo, usedRoomIds, out claimedRoomNo))
            return claimedRoomNo;

        if (TryClaimRoom(ResolveFallbackRoomNo(classId), usedRoomIds, out claimedRoomNo))
            return claimedRoomNo;

        if (rooms != null)
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                if (TryClaimRoom(rooms[i].roomId, usedRoomIds, out claimedRoomNo))
                    return claimedRoomNo;
            }
        }

        return 0;
    }

    private bool TryClaimRoom(int roomNo, HashSet<int> usedRoomIds, out int claimedRoomNo)
    {
        claimedRoomNo = 0;

        if (roomNo <= 0 || usedRoomIds == null || rooms == null)
            return false;

        for (int i = 0; i < rooms.Count; i++)
        {
            if (rooms[i].roomId != roomNo)
                continue;

            if (usedRoomIds.Contains(roomNo))
                return false;

            claimedRoomNo = roomNo;
            return true;
        }

        return false;
    }

    private List<string> BuildCandidateApiBases()
    {
        List<string> bases = new List<string>();
        AddBaseIfMissing(bases, PlayerPrefs.GetString("ApiBaseUrl", string.Empty));
        AddBaseIfMissing(bases, baseUrl);
        AddBaseIfMissing(bases, "https://homequest-c3k7.onrender.com");
        AddBaseIfMissing(bases, "http://localhost:8001");
        return bases;
    }

    private static void AddBaseIfMissing(List<string> list, string baseCandidate)
    {
        if (list == null || string.IsNullOrWhiteSpace(baseCandidate))
            return;

        string normalized = baseCandidate.Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(normalized))
            return;

        for (int i = 0; i < list.Count; i++)
        {
            if (string.Equals(list[i], normalized, System.StringComparison.OrdinalIgnoreCase))
                return;
        }

        list.Add(normalized);
    }

    private static string ResolveClassroomLabel(ClassroomData classroom)
    {
        if (classroom == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(classroom.description))
            return classroom.description.Trim();

        if (!string.IsNullOrWhiteSpace(classroom.subjectName))
            return classroom.subjectName.Trim();

        if (!string.IsNullOrWhiteSpace(classroom.name))
            return classroom.name.Trim();

        return "Classroom";
    }

    private static string ExtractJoinedSubject(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
            return string.Empty;

        try
        {
            JoinClassResponse parsed = JsonUtility.FromJson<JoinClassResponse>(responseBody);
            if (parsed != null)
            {
                if (!string.IsNullOrWhiteSpace(parsed.subject))
                    return parsed.subject.Trim();

                if (parsed.class_info != null && !string.IsNullOrWhiteSpace(parsed.class_info.name))
                    return parsed.class_info.name.Trim();
            }
        }
        catch
        {
        }

        return string.Empty;
    }

    private static ClassroomData ExtractJoinedClassroom(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
            return null;

        try
        {
            JoinClassResponse parsed = JsonUtility.FromJson<JoinClassResponse>(responseBody);
            if (parsed == null)
                return null;

            string subjectLabel = !string.IsNullOrWhiteSpace(parsed.subject)
                ? parsed.subject.Trim()
                : (parsed.class_info != null ? parsed.class_info.name : string.Empty);

            if (string.IsNullOrWhiteSpace(subjectLabel))
                return null;

            return new ClassroomData
            {
                room_no = 0,
                class_id = parsed.class_info != null ? parsed.class_info.id : 0,
                description = subjectLabel,
                subjectName = subjectLabel,
                name = subjectLabel
            };
        }
        catch
        {
        }

        return null;
    }

    private void UpdateRoomLabel(int roomNo, string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return;

        RoomUI roomUI = rooms.Find(r => r.roomId == roomNo);
        if (roomUI == null)
            return;

        if (roomUI.roomText != null)
            roomUI.roomText.text = label;

        if (roomUI.roomImage != null)
            roomUI.roomImage.color = Color.white;
    }

    private List<ClassroomData> LoadCachedClassrooms()
    {
        string raw = PlayerPrefs.GetString(BuildStudentScopedKey(ClassroomCacheKey), string.Empty);
        if (string.IsNullOrWhiteSpace(raw))
            return new List<ClassroomData>();

        try
        {
            ClassroomListWrapper wrapped = JsonUtility.FromJson<ClassroomListWrapper>(raw);
            if (wrapped != null && wrapped.classrooms != null)
                return NormalizeClassrooms(wrapped.classrooms);
        }
        catch
        {
        }

        return new List<ClassroomData>();
    }

    private void SaveCachedClassrooms(List<ClassroomData> classrooms)
    {
        ClassroomListWrapper wrapper = new ClassroomListWrapper();
        wrapper.classrooms = NormalizeClassrooms(classrooms);
        PlayerPrefs.SetString(BuildStudentScopedKey(ClassroomCacheKey), JsonUtility.ToJson(wrapper));
        PlayerPrefs.Save();
    }

    private const string ClassRoomMapKey = "ClassRoomMap";

    /// <summary>
    /// Loads the persisted class_id → room_no map for this student.
    /// This is the ONLY authority for which slot each enrolled class occupies.
    /// </summary>
    private Dictionary<int, int> LoadClassRoomMap()
    {
        string raw = PlayerPrefs.GetString(BuildStudentScopedKey(ClassRoomMapKey), "");
        if (string.IsNullOrEmpty(raw))
            return new Dictionary<int, int>();

        try
        {
            ClassRoomMapWrapper wrapper = JsonUtility.FromJson<ClassRoomMapWrapper>(raw);
            Dictionary<int, int> map = new Dictionary<int, int>();
            if (wrapper != null && wrapper.entries != null)
            {
                for (int i = 0; i < wrapper.entries.Count; i++)
                {
                    ClassRoomMapEntry e = wrapper.entries[i];
                    if (e == null || e.roomNo <= 0)
                        continue;
                    // Accept both real class_id (positive) and placeholder (negative) entries.
                    if (e.classId != 0)
                        map[e.classId] = e.roomNo;
                }
            }
            return map;
        }
        catch
        {
            return new Dictionary<int, int>();
        }
    }

    private void SaveClassRoomMap(Dictionary<int, int> map)
    {
        ClassRoomMapWrapper wrapper = new ClassRoomMapWrapper();
        foreach (var kv in map)
            wrapper.entries.Add(new ClassRoomMapEntry { classId = kv.Key, roomNo = kv.Value });
        PlayerPrefs.SetString(BuildStudentScopedKey(ClassRoomMapKey), JsonUtility.ToJson(wrapper));
        PlayerPrefs.Save();
    }

    private static List<ClassroomData> MergeClassroomLists(List<ClassroomData> primary, List<ClassroomData> fallback)
    {
        List<ClassroomData> merged = new List<ClassroomData>();
        Dictionary<string, int> indexByKey = new Dictionary<string, int>();

        AppendClassrooms(merged, indexByKey, fallback, false);
        AppendClassrooms(merged, indexByKey, primary, true);
        return NormalizeClassrooms(merged);
    }

    private static void AppendClassrooms(List<ClassroomData> target, Dictionary<string, int> indexByKey, List<ClassroomData> source, bool preferSource)
    {
        if (target == null || indexByKey == null || source == null)
            return;

        for (int i = 0; i < source.Count; i++)
        {
            ClassroomData normalized = NormalizeClassroom(source[i]);
            if (normalized == null)
                continue;

            string key = BuildClassroomKey(normalized);
            if (string.IsNullOrWhiteSpace(key))
                continue;

            if (indexByKey.TryGetValue(key, out int existingIndex))
            {
                target[existingIndex] = preferSource
                    ? MergeClassroom(target[existingIndex], normalized)
                    : MergeClassroom(normalized, target[existingIndex]);
            }
            else
            {
                indexByKey[key] = target.Count;
                target.Add(normalized);
            }
        }
    }

    private static ClassroomData MergeClassroom(ClassroomData fallback, ClassroomData preferred)
    {
        fallback = NormalizeClassroom(fallback);
        preferred = NormalizeClassroom(preferred);

        if (preferred == null)
            return fallback;
        if (fallback == null)
            return preferred;

        if (preferred.room_no <= 0)
            preferred.room_no = fallback.room_no;
        if (preferred.class_id <= 0)
            preferred.class_id = fallback.class_id;
        if (string.IsNullOrWhiteSpace(preferred.description))
            preferred.description = fallback.description;
        if (string.IsNullOrWhiteSpace(preferred.subjectName))
            preferred.subjectName = fallback.subjectName;
        if (string.IsNullOrWhiteSpace(preferred.name))
            preferred.name = fallback.name;

        return preferred;
    }

    private static List<ClassroomData> NormalizeClassrooms(List<ClassroomData> classrooms)
    {
        List<ClassroomData> normalized = new List<ClassroomData>();
        if (classrooms == null)
            return normalized;

        for (int i = 0; i < classrooms.Count; i++)
        {
            ClassroomData classroom = NormalizeClassroom(classrooms[i]);
            if (classroom != null)
                normalized.Add(classroom);
        }

        return normalized;
    }

    private static ClassroomData NormalizeClassroom(ClassroomData classroom)
    {
        if (classroom == null)
            return null;

        string label = ResolveClassroomLabel(classroom);
        if (string.IsNullOrWhiteSpace(label) && classroom.class_id <= 0)
            return null;

        if (string.IsNullOrWhiteSpace(classroom.description))
            classroom.description = label;
        if (string.IsNullOrWhiteSpace(classroom.subjectName))
            classroom.subjectName = label;
        if (string.IsNullOrWhiteSpace(classroom.name))
            classroom.name = label;

        return classroom;
    }

    private static string BuildClassroomKey(ClassroomData classroom)
    {
        if (classroom == null)
            return string.Empty;

        if (classroom.class_id > 0)
            return "id:" + classroom.class_id;

        string label = ResolveClassroomLabel(classroom);
        return string.IsNullOrWhiteSpace(label) ? string.Empty : "label:" + label.Trim().ToLowerInvariant();
    }

    IEnumerator LoadAssignmentTypes(int classId)
    {
        if (noQuestionPanel != null)
            noQuestionPanel.SetActive(false);

        List<AssignmentTypeData> categoryList = null;
        List<AssignmentTypeData> firstEmptyCategories = null;
        string firstEmptyBase = string.Empty;
        string lastError = "";
        List<string> apiBases = BuildCandidateApiBases();

        for (int i = 0; i < apiBases.Count && categoryList == null; i++)
        {
            string baseApi = apiBases[i];
            string legacyUrl = baseApi + "/get_assignment_types?student_id=" + studentId + "&class_id=" + classId;

            using (UnityWebRequest request = UnityWebRequest.Get(legacyUrl))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = request.downloadHandler != null ? request.downloadHandler.text : "";
                    List<AssignmentTypeData> parsed = JsonUtilityWrapper.FromJsonList<AssignmentTypeData>(jsonResponse);
                    if (parsed != null)
                    {
                        if (parsed.Count > 0)
                        {
                            categoryList = parsed;
                            PlayerPrefs.SetString("ApiBaseUrl", baseApi);
                            PlayerPrefs.Save();
                            break;
                        }

                        if (firstEmptyCategories == null)
                        {
                            firstEmptyCategories = parsed;
                            firstEmptyBase = baseApi;
                        }
                    }
                }

                lastError = request.error + " | endpoint=" + legacyUrl + " | code=" + request.responseCode;
            }

            if (categoryList == null)
            {
                string legacyAssignmentsUrl = baseApi + "/get_assignments?student_id=" + studentId + "&classroom_id=" + classId;
                using (UnityWebRequest request = UnityWebRequest.Get(legacyAssignmentsUrl))
                {
                    request.timeout = 12;
                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string body = request.downloadHandler != null ? request.downloadHandler.text : "";
                        List<AssignmentTypeData> parsed = ParseAssignmentTypesFromLegacyAssignments(body);
                        if (parsed != null)
                        {
                            if (parsed.Count > 0)
                            {
                                categoryList = parsed;
                                PlayerPrefs.SetString("ApiBaseUrl", baseApi);
                                PlayerPrefs.Save();
                                break;
                            }

                            if (firstEmptyCategories == null)
                            {
                                firstEmptyCategories = parsed;
                                firstEmptyBase = baseApi;
                            }
                        }
                    }
                    else
                    {
                        lastError = request.error + " | endpoint=" + legacyAssignmentsUrl + " | code=" + request.responseCode;
                    }
                }
            }

            string[] modernPaths = new string[] { "/student/assignments", "/api/student/assignments" };
            for (int p = 0; p < modernPaths.Length && categoryList == null; p++)
            {
                string endpoint = baseApi + modernPaths[p] + "?student_id=" + studentId + "&class_id=" + classId;
                using (UnityWebRequest request = UnityWebRequest.Get(endpoint))
                {
                    request.timeout = 12;
                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string body = request.downloadHandler != null ? request.downloadHandler.text : "";
                        List<AssignmentTypeData> parsed = ParseAssignmentTypesFromModern(body);
                        if (parsed != null)
                        {
                            if (parsed.Count > 0)
                            {
                                categoryList = parsed;
                                PlayerPrefs.SetString("ApiBaseUrl", baseApi);
                                PlayerPrefs.Save();
                                break;
                            }

                            if (firstEmptyCategories == null)
                            {
                                firstEmptyCategories = parsed;
                                firstEmptyBase = baseApi;
                            }
                        }
                    }
                    else
                    {
                        lastError = request.error + " | endpoint=" + endpoint + " | code=" + request.responseCode;
                    }
                }
            }
        }

        if (categoryList == null && firstEmptyCategories != null)
        {
            categoryList = firstEmptyCategories;
            if (!string.IsNullOrWhiteSpace(firstEmptyBase))
            {
                PlayerPrefs.SetString("ApiBaseUrl", firstEmptyBase);
                PlayerPrefs.Save();
            }
        }

        if (categoryList == null)
        {
            Debug.LogError("Error loading assignment types: " + lastError);
            yield break;
        }

        yield return StartCoroutine(HydrateDueDatesFromModern(classId, categoryList));

        Debug.Log($"📋 Parsed {(categoryList != null ? categoryList.Count : 0)} assignment types");

        if (categoryList != null && categoryList.Count > 0)
        {
            if (stagePanel != null) stagePanel.SetActive(true);
            if (noQuestionPanel != null) noQuestionPanel.SetActive(false);

            ClearSpawnedButtons();

            if (categoryButtonContainer != null && categoryButtonPrefab != null)
            {
                foreach (var category in categoryList)
                {
                    Debug.Log($"📝 Assignment: {category.description}, ID: {category.category_id}");

                    string assignmentType = GetAssignmentType(category.description, category.assignment_type);
                    if (string.IsNullOrEmpty(assignmentType))
                        assignmentType = "MultipleChoice";

                    GameObject buttonObj = Instantiate(categoryButtonPrefab, categoryButtonContainer);
                    buttonObj.SetActive(true);
                    spawnedButtons.Add(buttonObj);

                    TMP_Text buttonText = buttonObj.GetComponentInChildren<TMP_Text>();
                    string label = string.IsNullOrWhiteSpace(category.description) ? "Activity" : category.description.Trim();
                    string dueRaw = string.IsNullOrWhiteSpace(category.due_date_text)
                        ? ResolveClassDueDateByClassId(currentClassId)
                        : category.due_date_text;
                    string dueLine = BuildDueLine(dueRaw);
                    EnsureAssignmentButtonLayout(buttonObj, buttonText, label, assignmentType, dueLine, category.is_completed);

                    Image buttonImage = buttonObj.GetComponent<Image>();
                    if (buttonImage != null)
                    {
                        if (assignmentType == "Alchemy")
                            buttonImage.color = new Color(0.4f, 0.85f, 0.4f);
                        else if (assignmentType == "MultipleChoice")
                            buttonImage.color = new Color(1f, 0.75f, 0.3f);
                        else if (assignmentType == "Identification")
                            buttonImage.color = new Color(0.4f, 0.7f, 1f);
                    }

                    Button button = buttonObj.GetComponent<Button>();
                    if (button != null)
                    {
                        int capturedCategoryId = category.category_id;
                        string capturedType = assignmentType;
                        button.interactable = !category.is_completed;
                        if (!category.is_completed)
                            button.onClick.AddListener(() => OnCategorySelected(capturedCategoryId, capturedType));
                    }

                    if (category.is_completed)
                    {
                        Image doneBg = buttonObj.GetComponent<Image>();
                        if (doneBg != null)
                            doneBg.color = new Color(0.72f, 0.72f, 0.72f, 0.9f);
                    }
                }
            }
            else
            {
                UseOldButtonSystem(categoryList);
            }
        }
        else
        {
            if (noQuestionPanel != null) noQuestionPanel.SetActive(true);
        }
    }

    private string ResolveSubjectNameByClassId(int classId)
    {
        foreach (var item in classroomDataByRoom)
        {
            ClassroomData classroom = item.Value;
            if (classroom == null)
                continue;

            if (classroom.class_id == classId)
                return ResolveClassroomLabel(classroom);
        }

        return string.Empty;
    }

    private List<AssignmentTypeData> ParseAssignmentTypesFromModern(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return new List<AssignmentTypeData>();

        AssignmentsResponse parsed = null;
        try
        {
            parsed = JsonUtility.FromJson<AssignmentsResponse>(body);
        }
        catch
        {
        }

        List<AssignmentServerItem> source = null;
        if (parsed != null && parsed.assignments != null)
            source = parsed.assignments;
        else if (parsed != null && parsed.activities != null)
            source = parsed.activities;

        List<AssignmentTypeData> result = new List<AssignmentTypeData>();
        if (source == null)
            return result;

        for (int i = 0; i < source.Count; i++)
        {
            AssignmentServerItem item = source[i];
            if (item == null)
                continue;

            int assignmentId = item.assignment_id > 0 ? item.assignment_id : item.id;
            string title = string.IsNullOrWhiteSpace(item.title) ? "Assignment" : item.title.Trim();
            string assignmentType = !string.IsNullOrWhiteSpace(item.assignment_type) ? item.assignment_type : item.type;

            result.Add(new AssignmentTypeData
            {
                category_id = assignmentId,
                description = title,
                assignment_type = assignmentType,
                is_completed = ResolveSubmissionStatus(item),
                due_date_text = ResolveDueDateText(item)
            });
        }

        return result;
    }

    private List<AssignmentTypeData> ParseAssignmentTypesFromLegacyAssignments(string body)
    {
        List<AssignmentTypeData> result = new List<AssignmentTypeData>();
        if (string.IsNullOrWhiteSpace(body))
            return result;

        List<LegacyAssignmentItem> source = null;
        try
        {
            source = JsonUtilityWrapper.FromJsonList<LegacyAssignmentItem>(body);
        }
        catch
        {
        }

        if (source == null || source.Count == 0)
        {
            try
            {
                LegacyAssignmentsResponse parsed = JsonUtility.FromJson<LegacyAssignmentsResponse>(body);
                if (parsed != null && parsed.assignments != null)
                    source = parsed.assignments;
            }
            catch
            {
            }
        }

        if (source == null)
            return result;

        for (int i = 0; i < source.Count; i++)
        {
            LegacyAssignmentItem item = source[i];
            if (item == null)
                continue;

            int assignmentId = item.assignmentId > 0 ? item.assignmentId : item.assignment_id;
            if (assignmentId <= 0)
                continue;

            string title = item.assignmentName;
            if (string.IsNullOrWhiteSpace(title))
                title = item.assignment_name;
            if (string.IsNullOrWhiteSpace(title))
                title = item.title;
            if (string.IsNullOrWhiteSpace(title))
                title = "Assignment";

            string assignmentType = item.assignmentType;
            if (string.IsNullOrWhiteSpace(assignmentType))
                assignmentType = item.assignment_type;

            result.Add(new AssignmentTypeData
            {
                category_id = assignmentId,
                description = title.Trim(),
                assignment_type = assignmentType,
                is_completed = item.isSubmitted,
                due_date_text = ResolveDueDateText(item)
            });
        }

        return result;
    }

    void OnCategorySelected(int assignmentId, string assignmentType)
    {
        // Set session data
        CurrentClassSession.SelectedClassId = currentClassId;
        CurrentClassSession.SelectedCategoryId = assignmentId;

        // Also save to PlayerPrefs for backward compatibility
        PlayerPrefs.SetInt("CategoryId", assignmentId);
        PlayerPrefs.SetInt("ClassId", currentClassId);
        PlayerPrefs.SetString("SelectedActivityType", assignmentType);
        PlayerPrefs.Save();

        Debug.Log($"🎯 Selected assignment ID: {assignmentId}, Type: {assignmentType}");

        string type = string.IsNullOrWhiteSpace(assignmentType) ? "" : assignmentType.Trim();
        if (string.Equals(type, "Alchemy", StringComparison.OrdinalIgnoreCase))
            LoadSceneWithFallbacks("TrueFalseScene", "yn", "NewMap");
        else if (string.Equals(type, "Identification", StringComparison.OrdinalIgnoreCase))
            LoadSceneWithFallbacks("Identification", "identification", "NewMap");
        else if (type.IndexOf("enumeration", StringComparison.OrdinalIgnoreCase) >= 0)
            LoadSceneWithFallbacks("EnumerationScene", "enumeration", "NewMap");
        else if (type.IndexOf("problem", StringComparison.OrdinalIgnoreCase) >= 0)
            LoadSceneWithFallbacks("ProblemSolving", "problemSolving", "ps", "NewMap");
        else if (type.IndexOf("fill", StringComparison.OrdinalIgnoreCase) >= 0 || type.IndexOf("blank", StringComparison.OrdinalIgnoreCase) >= 0 || string.Equals(type, "FIB", StringComparison.OrdinalIgnoreCase))
            LoadSceneWithFallbacks("FillInTheBlank", "fib", "NewMap");
        else if (type.IndexOf("essay", StringComparison.OrdinalIgnoreCase) >= 0)
            LoadSceneWithFallbacks("Essay", "essay", "NewMap");
        else
            LoadSceneWithFallbacks("MultipleChoice", "NewMap");
    }

    void LoadSceneWithFallbacks(string preferredScene, params string[] fallbackScenes)
    {
        if (!string.IsNullOrWhiteSpace(preferredScene) && Application.CanStreamedLevelBeLoaded(preferredScene))
        {
            SceneManager.LoadScene(preferredScene);
            return;
        }

        if (fallbackScenes != null)
        {
            for (int i = 0; i < fallbackScenes.Length; i++)
            {
                string candidate = fallbackScenes[i];
                if (!string.IsNullOrWhiteSpace(candidate) && Application.CanStreamedLevelBeLoaded(candidate))
                {
                    SceneManager.LoadScene(candidate);
                    return;
                }
            }
        }

        Debug.LogError($"No loadable scene found. Preferred='{preferredScene}'. Add scenes via File > Build Profiles.");
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

    string GetAssignmentType(string description, string explicitType = null)
    {
        string typeLower = string.IsNullOrWhiteSpace(explicitType) ? "" : explicitType.Trim().ToLowerInvariant();
        if (typeLower.Contains("yes_no") || typeLower.Contains("yes/no") || typeLower.Contains("true") || typeLower.Contains("false"))
            return "Alchemy";
        if (typeLower.Contains("yesno"))
            return "Alchemy";
        if (typeLower.Contains("identification"))
            return "Identification";
        if (typeLower.Contains("essay"))
            return "Essay";
        if (typeLower.Contains("fill") || typeLower.Contains("fib") || typeLower.Contains("blank"))
            return "FillInTheBlank";
        if (typeLower.Contains("enumeration"))
            return "Enumeration";
        if (typeLower.Contains("problem"))
            return "ProblemSolving";
        if (typeLower.Contains("multiple_choice") || typeLower.Contains("multiplechoice"))
            return "MultipleChoice";

        if (!string.IsNullOrWhiteSpace(description) && (description.Contains("True") || description.Contains("False")))
            return "Alchemy";
        else if (!string.IsNullOrWhiteSpace(description) && description.Contains("Identification"))
            return "Identification";
        else if (!string.IsNullOrWhiteSpace(description) && description.Contains("Essay"))
            return "Essay";
        else if (!string.IsNullOrWhiteSpace(description) && description.Contains("Enumeration"))
            return "Enumeration";
        else if (!string.IsNullOrWhiteSpace(description) && description.Contains("Multiple Choice"))
            return "MultipleChoice";

        if (!string.IsNullOrWhiteSpace(explicitType))
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
            
            string mappedType = GetAssignmentType(category.description, category.assignment_type);
            if (mappedType == "Alchemy")
            {
                if (alchemyButton != null)
                {
                    alchemyButton.gameObject.SetActive(true);
                    alchemyButton.onClick.AddListener(() => OnCategorySelected(capturedCategoryId, "Alchemy"));
                }
            }
            else if (mappedType == "Identification")
            {
                if (identificationButton != null)
                {
                    identificationButton.gameObject.SetActive(true);
                    identificationButton.onClick.AddListener(() => OnCategorySelected(capturedCategoryId, "Identification"));
                }
            }
            else if (mappedType == "FillInTheBlank")
            {
                if (multipleChoiceButton != null)
                {
                    multipleChoiceButton.gameObject.SetActive(true);
                    multipleChoiceButton.onClick.AddListener(() => OnCategorySelected(capturedCategoryId, "FillInTheBlank"));
                }
            }
            else if (mappedType == "Essay")
            {
                if (multipleChoiceButton != null)
                {
                    multipleChoiceButton.gameObject.SetActive(true);
                    multipleChoiceButton.onClick.AddListener(() => OnCategorySelected(capturedCategoryId, "Essay"));
                }
            }
            else if (mappedType == "MultipleChoice")
            {
                if (multipleChoiceButton != null)
                {
                    multipleChoiceButton.gameObject.SetActive(true);
                    multipleChoiceButton.onClick.AddListener(() => OnCategorySelected(capturedCategoryId, "MultipleChoice"));
                }
            }
        }
    }

    private static string BuildDueLine(string dueText)
    {
        string resolved = string.IsNullOrWhiteSpace(dueText) ? "No deadline" : dueText.Trim();
        return "DUE: " + resolved;
    }

    private static void EnsureAssignmentButtonLayout(GameObject buttonObj, TMP_Text fallbackText, string title, string assignmentType, string dueLine, bool isCompleted)
    {
        if (buttonObj == null)
            return;

        if (fallbackText != null)
            fallbackText.gameObject.SetActive(false);

        Transform root = buttonObj.transform.Find("RuntimeLayout");
        if (root == null)
        {
            GameObject rootObj = new GameObject("RuntimeLayout", typeof(RectTransform));
            rootObj.transform.SetParent(buttonObj.transform, false);
            RectTransform rrt = rootObj.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero;
            rrt.anchorMax = Vector2.one;
            rrt.offsetMin = new Vector2(16f, 4f);
            rrt.offsetMax = new Vector2(-16f, -4f);
            root = rootObj.transform;
        }

        TMP_Text titleText = EnsureText(root, "TitleText", 32f, TextAlignmentOptions.MidlineLeft, new Color32(32, 33, 36, 255));
        RectTransform titleRt = titleText.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 0f);
        titleRt.anchorMax = new Vector2(0.68f, 1f);
        titleRt.offsetMin = Vector2.zero;
        titleRt.offsetMax = new Vector2(-8f, 0f);
        titleText.textWrappingMode = TextWrappingModes.NoWrap;
        titleText.overflowMode = TextOverflowModes.Ellipsis;
        titleText.text = title;

        Transform existingType = root.Find("TypeText");
        if (existingType != null)
            existingType.gameObject.SetActive(false);

        TMP_Text dueText = EnsureText(root, "DueText", 24f, TextAlignmentOptions.MidlineRight, new Color32(62, 64, 68, 255));
        RectTransform dueRt = dueText.GetComponent<RectTransform>();
        dueRt.anchorMin = new Vector2(0.68f, 0f);
        dueRt.anchorMax = new Vector2(1f, 1f);
        dueRt.offsetMin = new Vector2(8f, 0f);
        dueRt.offsetMax = Vector2.zero;
        dueText.textWrappingMode = TextWrappingModes.NoWrap;
        dueText.overflowMode = TextOverflowModes.Ellipsis;
        dueText.text = dueLine;

        TMP_Text doneText = EnsureText(root, "DoneText", 22f, TextAlignmentOptions.Center, new Color32(34, 34, 34, 255));
        RectTransform doneRt = doneText.GetComponent<RectTransform>();
        doneRt.anchorMin = new Vector2(0.58f, 0f);
        doneRt.anchorMax = new Vector2(0.68f, 1f);
        doneRt.offsetMin = Vector2.zero;
        doneRt.offsetMax = Vector2.zero;
        doneText.fontStyle = FontStyles.Bold;
        doneText.text = isCompleted ? "DONE" : string.Empty;
    }

    private static TMP_Text EnsureText(Transform parent, string name, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        Transform child = parent.Find(name);
        if (child == null)
        {
            GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(parent, false);
            child = textObj.transform;
        }

        TMP_Text text = child.GetComponent<TMP_Text>();
        if (text == null)
            text = child.gameObject.AddComponent<TextMeshProUGUI>();

        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        return text;
    }

    private static string MapTypeToBadge(string assignmentType)
    {
        string raw = string.IsNullOrWhiteSpace(assignmentType) ? string.Empty : assignmentType.Trim().ToLowerInvariant();
        if (raw.Contains("multiple")) return "TEST";
        if (raw.Contains("yes") || raw.Contains("no") || raw.Contains("true") || raw.Contains("false")) return "TEST";
        if (raw.Contains("quiz")) return "TEST";
        return "ACTIVITY";
    }

    private IEnumerator HydrateDueDatesFromModern(int classId, List<AssignmentTypeData> categories)
    {
        if (categories == null || categories.Count == 0)
            yield break;

        bool needsHydration = false;
        for (int i = 0; i < categories.Count; i++)
        {
            string due = categories[i] != null ? categories[i].due_date_text : string.Empty;
            if (string.IsNullOrWhiteSpace(due) || due.Trim().Equals("No deadline", StringComparison.OrdinalIgnoreCase))
            {
                needsHydration = true;
                break;
            }
        }

        if (!needsHydration)
            yield break;

        List<string> apiBases = BuildCandidateApiBases();
        for (int i = 0; i < apiBases.Count; i++)
        {
            string baseApi = apiBases[i];
            string getEndpoint = baseApi + "/student/assignments?student_id=" + studentId + "&class_id=" + classId;

            List<AssignmentTypeData> parsed = null;
            using (UnityWebRequest req = UnityWebRequest.Get(getEndpoint))
            {
                req.timeout = 12;
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    string body = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;
                    parsed = ParseAssignmentTypesFromModern(body);
                }
            }

            if (parsed == null || parsed.Count == 0)
            {
                string postEndpoint = baseApi + "/student/assignments";
                string payload = "{\"student_id\":" + studentId + ",\"class_id\":" + classId + "}";
                using (UnityWebRequest req = new UnityWebRequest(postEndpoint, "POST"))
                {
                    req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
                    req.downloadHandler = new DownloadHandlerBuffer();
                    req.SetRequestHeader("Content-Type", "application/json");
                    req.timeout = 12;

                    yield return req.SendWebRequest();
                    if (req.result == UnityWebRequest.Result.Success)
                    {
                        string body = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;
                        parsed = ParseAssignmentTypesFromModern(body);
                    }
                }
            }

            if (parsed != null && parsed.Count > 0)
            {
                int hydrated = MergeDueDates(categories, parsed);
                if (hydrated > 0)
                {
                    PlayerPrefs.SetString("ApiBaseUrl", baseApi);
                    PlayerPrefs.Save();
                    yield break;
                }
            }
        }
    }

    private static int MergeDueDates(List<AssignmentTypeData> target, List<AssignmentTypeData> source)
    {
        if (target == null || source == null)
            return 0;

        Dictionary<int, string> sourceDueById = new Dictionary<int, string>();
        for (int i = 0; i < source.Count; i++)
        {
            AssignmentTypeData src = source[i];
            if (src == null || src.category_id <= 0)
                continue;

            if (!string.IsNullOrWhiteSpace(src.due_date_text) &&
                !src.due_date_text.Trim().Equals("No deadline", StringComparison.OrdinalIgnoreCase))
            {
                sourceDueById[src.category_id] = src.due_date_text.Trim();
            }
        }

        int updated = 0;
        for (int i = 0; i < target.Count; i++)
        {
            AssignmentTypeData dst = target[i];
            if (dst == null || dst.category_id <= 0)
                continue;

            if (sourceDueById.TryGetValue(dst.category_id, out string dueText))
            {
                dst.due_date_text = dueText;
                updated++;
            }

            for (int j = 0; j < source.Count; j++)
            {
                AssignmentTypeData src = source[j];
                if (src == null || src.category_id != dst.category_id)
                    continue;

                if (src.is_completed)
                    dst.is_completed = true;
                break;
            }
        }

        return updated;
    }

    private static string ResolveDueDateText(AssignmentServerItem item)
    {
        if (item == null)
            return "No deadline";

        string preformatted = SafeString(item.due_date_display, SafeString(item.deadline_display, SafeString(item.deadlineDisplay, string.Empty)));
        if (!string.IsNullOrWhiteSpace(preformatted))
            return preformatted;

        string raw = SafeString(item.due_date, SafeString(item.deadline, SafeString(item.due_at, SafeString(item.dueDate, string.Empty))));
        return FormatDueDate(raw);
    }

    private static bool ResolveSubmissionStatus(AssignmentServerItem item)
    {
        if (item == null)
            return false;

        return item.is_submitted || item.isSubmitted || item.is_completed;
    }

    private static string ResolveDueDateText(LegacyAssignmentItem item)
    {
        if (item == null)
            return "No deadline";

        string preformatted = SafeString(item.due_date_display, SafeString(item.deadline_display, SafeString(item.deadlineDisplay, string.Empty)));
        if (!string.IsNullOrWhiteSpace(preformatted))
            return preformatted;

        string raw = SafeString(item.due_date, SafeString(item.deadline, SafeString(item.due_at, SafeString(item.dueDate, string.Empty))));
        return FormatDueDate(raw);
    }

    private static string ResolveSubjectDueDate(StudentSubjectData subject)
    {
        if (subject == null)
            return "No deadline";

        if (!string.IsNullOrWhiteSpace(subject.latest_activity_due_date_display))
            return subject.latest_activity_due_date_display.Trim();

        string raw = SafeString(subject.latest_activity_due_date,
                    SafeString(subject.activity_due_date,
                    SafeString(subject.latest_activity_deadline, string.Empty)));

        return FormatDueDate(raw);
    }

    private string ResolveClassDueDateByClassId(int classId)
    {
        foreach (var item in classroomDataByRoom)
        {
            ClassroomData classroom = item.Value;
            if (classroom == null)
                continue;

            if (classroom.class_id == classId)
                return string.IsNullOrWhiteSpace(classroom.dueDate) ? "No deadline" : classroom.dueDate;
        }

        return "No deadline";
    }

    private static string FormatDueDate(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "No deadline";

        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime parsed) ||
            DateTime.TryParse(raw, out parsed))
        {
            return parsed.ToString("MMM dd, yyyy hh:mm tt", CultureInfo.InvariantCulture);
        }

        return raw.Trim();
    }

    private static string SafeString(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
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
