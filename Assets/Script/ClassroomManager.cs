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

public class ClassroomManager : MonoBehaviour
{
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
    }

    [System.Serializable]
    private class StudentSubjectData
    {
        public int class_id;
        public string subject;
        public string subject_name;
        public string activity;
        public string activity_title;
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
    }

    [System.Serializable]
    private class AssignmentServerItem
    {
        public int assignment_id;
        public int id;
        public string title;
        public string assignment_type;
        public string type;
    }

    [System.Serializable]
    private class AssignmentsResponse
    {
        public List<AssignmentServerItem> assignments;
        public List<AssignmentServerItem> activities;
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

    private bool enterCodeButtonHidden;

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
            if (!enterCodeButtonHidden && submitButton != null)
            {
                submitButton.gameObject.SetActive(false);
                enterCodeButtonHidden = true;
            }

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
        string normalizedCode = NormalizeClassCode(classCode);
        if (string.IsNullOrEmpty(normalizedCode))
        {
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

        WWWForm legacyForm = new WWWForm();
        legacyForm.AddField("student_id", studentId);
        legacyForm.AddField("room_no", roomNo);
        legacyForm.AddField("code", normalizedCode);

        using (UnityWebRequest request = UnityWebRequest.Post(baseUrl + "save_classroom", legacyForm))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                joinedSubject = ExtractJoinedSubject(request.downloadHandler != null ? request.downloadHandler.text : string.Empty);
                joined = true;
            }
            else
            {
                string responseText = request.downloadHandler != null ? request.downloadHandler.text : request.error;
                lastError = "save_classroom => " + responseText;
            }
        }

        if (!joined)
        {
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
                    joinedSubject = ExtractJoinedSubject(request.downloadHandler != null ? request.downloadHandler.text : string.Empty);
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
                    joinedSubject = ExtractJoinedSubject(request.downloadHandler != null ? request.downloadHandler.text : string.Empty);
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
                    joinedSubject = ExtractJoinedSubject(request.downloadHandler != null ? request.downloadHandler.text : string.Empty);
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
            yield break;
        }

        classroomDataByRoom.Clear();

        foreach (var room in rooms)
        {
            room.roomText.text = "";
            room.roomImage.color = new Color(0.8f, 0.8f, 0.8f);
        }

        if (classroomList.Count > 0)
        {
            Debug.Log($"📚 Found {classroomList.Count} classrooms");
            foreach (var classroom in classroomList)
            {
                int roomNo = classroom.room_no > 0 ? classroom.room_no : ResolveFallbackRoomNo(classroom.class_id);
                classroom.room_no = roomNo;

                Debug.Log($"📖 Classroom - room_no: {classroom.room_no}, class_id: {classroom.class_id}, description: {classroom.description}");
                classroomDataByRoom[classroom.room_no] = classroom;

                RoomUI roomUI = rooms.Find(r => r.roomId == classroom.room_no);
                Debug.Log($"🔍 Looking for room with ID {classroom.room_no}, found: {(roomUI != null ? "YES" : "NO")}");
                if (roomUI != null)
                {
                    roomUI.roomText.text = ResolveClassroomLabel(classroom);
                    roomUI.roomImage.color = Color.white;
                }
            }
        }
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

            int roomNo = (i % Mathf.Max(1, rooms.Count)) + 1;
            result.Add(new ClassroomData
            {
                room_no = roomNo,
                class_id = subject.class_id,
                description = subjectLabel,
                subjectName = subjectLabel,
                name = subjectLabel
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

    IEnumerator LoadAssignmentTypes(int classId)
    {
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

            string subjectName = ResolveSubjectNameByClassId(classId);
            if (string.IsNullOrWhiteSpace(subjectName))
                continue;

            string[] modernPaths = new string[] { "/student/assignments", "/api/student/assignments" };
            for (int p = 0; p < modernPaths.Length && categoryList == null; p++)
            {
                string endpoint = baseApi + modernPaths[p] + "?student_id=" + studentId + "&subject=" + UnityWebRequest.EscapeURL(subjectName);
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

        Debug.Log($"📋 Parsed {(categoryList != null ? categoryList.Count : 0)} assignment types");

        if (categoryList != null && categoryList.Count > 0)
        {
            if (stagePanel != null) stagePanel.SetActive(true);

            ClearSpawnedButtons();

            if (categoryButtonContainer != null && categoryButtonPrefab != null)
            {
                foreach (var category in categoryList)
                {
                    Debug.Log($"📝 Assignment: {category.description}, ID: {category.category_id}");

                    string assignmentType = GetAssignmentType(category.description, category.assignment_type);
                    if (string.IsNullOrEmpty(assignmentType)) continue;

                    GameObject buttonObj = Instantiate(categoryButtonPrefab, categoryButtonContainer);
                    buttonObj.SetActive(true);
                    spawnedButtons.Add(buttonObj);

                    TMP_Text buttonText = buttonObj.GetComponentInChildren<TMP_Text>();
                    if (buttonText != null)
                    {
                        buttonText.text = category.description.ToUpper();
                    }

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
                        button.onClick.AddListener(() => OnCategorySelected(capturedCategoryId, capturedType));
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
                is_completed = false
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
            LoadSceneWithFallbacks("Alchemy", "YN-Opening", "TrueFalseScene", "NewMap");
        else if (string.Equals(type, "Identification", StringComparison.OrdinalIgnoreCase))
            LoadSceneWithFallbacks("Identification", "identification", "MC-Opening", "NewMap");
        else if (type.IndexOf("problem", StringComparison.OrdinalIgnoreCase) >= 0)
            LoadSceneWithFallbacks("ProblemSolving", "problemSolving", "ps", "MC-Opening", "NewMap");
        else if (type.IndexOf("fill", StringComparison.OrdinalIgnoreCase) >= 0 || type.IndexOf("blank", StringComparison.OrdinalIgnoreCase) >= 0 || string.Equals(type, "FIB", StringComparison.OrdinalIgnoreCase))
            LoadSceneWithFallbacks("FillInTheBlank", "fib", "MC-Opening", "NewMap");
        else if (type.IndexOf("essay", StringComparison.OrdinalIgnoreCase) >= 0)
            LoadSceneWithFallbacks("Essay", "essay", "MC-Opening", "NewMap");
        else
            LoadSceneWithFallbacks("MultipleChoice", "MC-Opening", "NewMap");
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
        if (typeLower.Contains("identification"))
            return "Identification";
        if (typeLower.Contains("essay"))
            return "Essay";
        if (typeLower.Contains("fill") || typeLower.Contains("fib") || typeLower.Contains("blank"))
            return "FillInTheBlank";
        if (typeLower.Contains("problem"))
            return "ProblemSolving";
        if (typeLower.Contains("multiple_choice") || typeLower.Contains("multiplechoice") || typeLower.Contains("enumeration"))
            return "MultipleChoice";

        if (!string.IsNullOrWhiteSpace(description) && (description.Contains("True") || description.Contains("False")))
            return "Alchemy";
        else if (!string.IsNullOrWhiteSpace(description) && description.Contains("Identification"))
            return "Identification";
        else if (!string.IsNullOrWhiteSpace(description) && description.Contains("Essay"))
            return "Essay";
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
