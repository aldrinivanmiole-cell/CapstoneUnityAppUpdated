using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

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
}

[System.Serializable]
public class ServerResponse
{
    public string status;
    public string message;
}

public static class JsonUtilityWrapper
{
    public static List<T> FromJsonList<T>(string json)
    {
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>("{\"items\":" + json + "}");
        return wrapper.items;
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public List<T> items;
    }
}

public static class CurrentClassSession
{
    public static int SelectedClassId;
    public static int SelectedCategoryId;
}

public class ClassroomManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject stagePanel;      // ✅ For selecting assignment types
    public GameObject addClassPanel;   // ✅ For adding classroom via code
    public GameObject noQuestionPanel; // ✅ For no assignments found

    [Header("Add Class Panel UI")]
    public TMP_InputField codeInput;
    public Button joinClassButton;
    public TMP_Text warningText;

    [Header("Assignment Category UI")]
    public Transform categoryButtonContainer; 
    public GameObject categoryButtonPrefab;

    [Header("Rooms (r1–r10)")]
    public List<RoomUI> rooms;

    private int selectedRoomId;
    private int selectedClassId;
    private int studentId;

    private string baseUrl = "https://homeworkquest.site/";

    private Dictionary<int, int> roomClassMap = new Dictionary<int, int>();

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

        // Panels setup
        stagePanel.SetActive(false);
        addClassPanel.SetActive(false);
        noQuestionPanel.SetActive(false);
        if (warningText != null) warningText.gameObject.SetActive(false);

        joinClassButton.onClick.AddListener(OnJoinClassClicked);

        foreach (var room in rooms)
            AddClickListener(room.roomImage, room.roomId);

        StartCoroutine(LoadRoomAssignments());
    }

    void AddClickListener(Image img, int roomId)
    {
        EventTrigger trigger = img.GetComponent<EventTrigger>();
        if (trigger == null) trigger = img.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry entry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerClick
        };
        entry.callback.AddListener((data) => { OnRoomClicked(roomId); });
        trigger.triggers.Add(entry);
    }

    void OnRoomClicked(int roomId)
    {
        selectedRoomId = roomId;
        var room = rooms.Find(r => r.roomId == roomId);
        if (room == null) return;

        // If the classroom slot is filled → show assignment picker
        if (!string.IsNullOrEmpty(room.roomText.text) && room.roomText.text != "Empty")
        {
            selectedClassId = roomClassMap.ContainsKey(roomId) ? roomClassMap[roomId] : 0;
            Debug.Log("Selected Room " + roomId + " → Class ID " + selectedClassId);

            if (selectedClassId > 0)
            {
                StartCoroutine(LoadAssignmentTypes(selectedClassId));
            }
        }
        else
        {
            // If room is empty → show AddClassPanel
            addClassPanel.SetActive(true);
            codeInput.text = "";
            if (warningText != null) warningText.gameObject.SetActive(false);
        }
    }

    IEnumerator LoadRoomAssignments()
    {
        using (UnityWebRequest www = UnityWebRequest.Get(baseUrl + "get_classrooms.php?student_id=" + studentId))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error loading classrooms: " + www.error);
            }
            else
            {
                string json = www.downloadHandler.text;
                Debug.Log("Classroom JSON: " + json);

                List<ClassroomData> classroomList = JsonUtilityWrapper.FromJsonList<ClassroomData>(json);
                roomClassMap.Clear();

                bool hasClassroom = false;

                foreach (var room in rooms)
                {
                    var assigned = classroomList.Find(c => c.room_no == room.roomId);
                    if (assigned != null)
                    {
                        room.roomText.text = assigned.description ?? "Unknown";
                        roomClassMap[room.roomId] = assigned.class_id;
                        hasClassroom = true;
                    }
                    else
                    {
                        room.roomText.text = "";
                    }
                }

                // ✅ If student has no classes, automatically open AddClassPanel
                if (!hasClassroom)
                {
                    addClassPanel.SetActive(false);
                    codeInput.text = "";
                    if (warningText != null) warningText.gameObject.SetActive(false);
                }
                else
                {
                    addClassPanel.SetActive(false);
                }
            }
        }
    }

    IEnumerator LoadAssignmentTypes(int classId)
    {
        string url = baseUrl + "get_assignment_types.php?class_id=" + classId;
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error loading assignment types: " + www.error);
            }
            else
            {
                string json = www.downloadHandler.text;
                Debug.Log("Assignment types JSON: " + json);

                List<AssignmentTypeData> types = JsonUtilityWrapper.FromJsonList<AssignmentTypeData>(json);

                foreach (Transform child in categoryButtonContainer)
                    Destroy(child.gameObject);

                if (types.Count > 0)
                {
                    stagePanel.SetActive(true);
                    addClassPanel.SetActive(false);
                    noQuestionPanel.SetActive(false);

                    foreach (var type in types)
                    {
                        GameObject btnObj = Instantiate(categoryButtonPrefab, categoryButtonContainer);
                        TMP_Text label = btnObj.GetComponentInChildren<TMP_Text>();
                        label.text = type.description;

                        Button btn = btnObj.GetComponent<Button>();
                        int categoryId = type.category_id;
                        string categoryName = type.description;

                        btn.onClick.AddListener(() =>
                        {
                            OnCategorySelected(categoryId, categoryName);
                        });
                    }
                }
                else
                {
                    stagePanel.SetActive(false);
                    noQuestionPanel.SetActive(true);
                    Debug.Log("No assignments available for this class.");
                }
            }
        }
    }

    void OnJoinClassClicked()
    {
        string code = codeInput.text.Trim();
        if (string.IsNullOrEmpty(code))
        {
            if (warningText != null)
            {
                warningText.text = "Please enter a class code.";
                warningText.gameObject.SetActive(true);
            }
            return;
        }

        StartCoroutine(SaveClassroom(studentId, code, selectedRoomId));
    }

    IEnumerator SaveClassroom(int studentId, string code, int roomId)
    {
        WWWForm form = new WWWForm();
        form.AddField("student_id", studentId);
        form.AddField("code", code);
        form.AddField("room_no", roomId);

        using (UnityWebRequest www = UnityWebRequest.Post(baseUrl + "save_classroom.php", form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error: " + www.error);
            }
            else
            {
                var response = JsonUtility.FromJson<ServerResponse>(www.downloadHandler.text);
                if (response.status == "success")
                {
                    Debug.Log(response.message);
                    addClassPanel.SetActive(false);
                    StartCoroutine(LoadRoomAssignments());
                }
                else
                {
                    if (warningText != null)
                    {
                        warningText.text = response.message;
                        warningText.gameObject.SetActive(true);
                    }
                }
            }
        }
    }

    void OnCategorySelected(int categoryId, string categoryName)
    {
        Debug.Log($"Selected Category: {categoryName} (ID: {categoryId}) for Class: {selectedClassId}");

        CurrentClassSession.SelectedClassId = selectedClassId;
        CurrentClassSession.SelectedCategoryId = categoryId;

        switch (categoryName)
        {
            case "Problem Solving": SceneManager.LoadScene("ps"); break;
            case "Multiple Choice": SceneManager.LoadScene("mc"); break;
            case "True/False": SceneManager.LoadScene("yn"); break;
            case "Fill in the Blank": SceneManager.LoadScene("fib"); break;
            case "Identification": SceneManager.LoadScene("identification"); break;
            case "Enumeration": SceneManager.LoadScene("enumeration"); break;
            case "Essay": SceneManager.LoadScene("essay"); break;
            default:
                Debug.LogWarning("No valid category scene matched.");
                break;
        }
    }
}
