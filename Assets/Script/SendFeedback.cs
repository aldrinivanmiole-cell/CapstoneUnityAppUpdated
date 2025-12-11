using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections;

public class SendFeedback : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField feedbackInput;
    public TMP_Text messageText;
    public Button sendButton;
    public Button backButton;
    public TMP_Text replyText; // Text to show teacher's reply

    [Header("Server Settings")]
    public string saveFeedbackURL = "https://homequest-c3k7.onrender.com/save_feedback";
    public string getFeedbackURL = "https://homequest-c3k7.onrender.com/get_feedback_reply";
    public string getClassInfoURL = "https://homequest-c3k7.onrender.com/get_class_info";

    private int studentId;
    private int teacherId = -1;

    void Start()
    {
        // You can get the logged-in student ID from your session manager
        if (SessionManager.Instance != null)
            studentId = SessionManager.Instance.StudentId;
        else
            studentId = 0;

        // Auto-find components if not assigned
        if (feedbackInput == null)
        {
            feedbackInput = GameObject.Find("InputField (TMP)")?.GetComponent<TMP_InputField>();
            if (feedbackInput == null)
                feedbackInput = FindFirstObjectByType<TMP_InputField>();
            
            if (feedbackInput != null)
                Debug.Log("✅ Found feedback input field: " + feedbackInput.name);
            else
                Debug.LogError("❌ Could not find feedback input field!");
        }

        if (messageText == null)
        {
            messageText = GetComponentInChildren<TMP_Text>();
            if (messageText != null)
                Debug.Log("✅ Found message text: " + messageText.name);
        }

        if (sendButton == null)
        {
            sendButton = GetComponent<Button>();
            if (sendButton != null)
                Debug.Log("✅ Found send button");
        }

        if (replyText == null)
        {
            GameObject replyObj = GameObject.Find("ReplyText");
            if (replyObj != null)
            {
                replyText = replyObj.GetComponent<TMP_Text>();
                Debug.Log("✅ Found ReplyText object");
            }
            else
            {
                Debug.LogWarning("⚠️ ReplyText not found, will try to create it");
                CreateReplyTextElement();
            }
        }

        if (sendButton != null)
            sendButton.onClick.AddListener(SendFeedbackMessage);
        
        if (backButton != null)
            backButton.onClick.AddListener(GoBack);
        else
            Debug.LogWarning("⚠️ Back button not assigned");

        // Get teacher ID from current class
        StartCoroutine(GetTeacherIdFromClass());

        // Load teacher replies when scene starts
        StartCoroutine(LoadTeacherReplies());
    }

    void GoBack()
    {
        SceneManager.LoadScene("map"); // Change this to your main scene
    }

    void SendFeedbackMessage()
    {
        if (feedbackInput == null)
        {
            ShowMessage("⚠️ Feedback input field not assigned!", Color.red);
            Debug.LogError("feedbackInput is not assigned in Inspector!");
            return;
        }

        string message = feedbackInput.text.Trim();
        Debug.Log("Raw input text: '" + feedbackInput.text + "'");
        Debug.Log("Trimmed message: '" + message + "'");
        Debug.Log("Message length: " + message.Length);

        if (string.IsNullOrEmpty(message))
        {
            ShowMessage("⚠️ Please enter your feedback first.", Color.red);
            return;
        }

        StartCoroutine(SendFeedbackToServer(studentId, message, teacherId));
    }

    IEnumerator GetTeacherIdFromClass()
    {
        int classId = CurrentClassSession.SelectedClassId;
        if (classId <= 0)
        {
            Debug.LogWarning("No class selected, feedback will go to any teacher");
            yield break;
        }

        string url = getClassInfoURL + "?class_id=" + classId;
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.timeout = 10;
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    ClassInfoResponse classInfo = JsonUtility.FromJson<ClassInfoResponse>(www.downloadHandler.text);
                    if (classInfo != null)
                    {
                        teacherId = classInfo.teacher_id;
                        Debug.Log($"✅ Got teacher ID: {teacherId} for class {classId}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Error parsing class info: " + e.Message);
                }
            }
            else
            {
                Debug.LogError("Error getting class info: " + www.error);
            }
        }
    }

    IEnumerator SendFeedbackToServer(int id, string message, int tId)
    {
        Debug.Log("=== SENDING FEEDBACK ===");
        Debug.Log("Student ID: " + id);
        Debug.Log("Message: " + message);
        Debug.Log("URL: " + saveFeedbackURL);

        WWWForm form = new WWWForm();
        form.AddField("student_id", id);
        form.AddField("message", message);
        if (tId > 0)
            form.AddField("teacher_id", tId);

        using (UnityWebRequest www = UnityWebRequest.Post(saveFeedbackURL, form))
        {
            www.timeout = 10; // 10 second timeout
            
            Debug.Log("Sending request...");
            yield return www.SendWebRequest();
            Debug.Log("Request completed!");

            Debug.Log("Response Code: " + www.responseCode);
            Debug.Log("Response Body: " + www.downloadHandler.text);

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Request successful!");
                
                try
                {
                    ServerResponse response = JsonUtility.FromJson<ServerResponse>(www.downloadHandler.text);

                    if (response != null && response.status == "success")
                    {
                        ShowMessage("Feedback sent to teacher successfully!", Color.green);
                        feedbackInput.text = "";
                        Debug.Log("✅ Feedback saved successfully!");
                    }
                    else
                    {
                        string errorMsg = response != null ? response.message : "Unknown error";
                        ShowMessage("Error: " + errorMsg, Color.red);
                        Debug.LogError("Server error: " + errorMsg);
                    }
                }
                catch (System.Exception)
                {
                    ShowMessage("Feedback sent to teacher successfully!", Color.green);
                    feedbackInput.text = "";
                    Debug.Log("✅ Feedback sent (response parsing skipped)");
                }
            }
            else
            {
                ShowMessage("Error sending feedback: " + www.error, Color.red);
                Debug.LogError("Network error: " + www.error);
                Debug.LogError("Response: " + www.downloadHandler.text);
            }
        }
    }

    void ShowMessage(string text, Color color)
    {
        messageText.text = text;
        messageText.color = color;
    }

    [System.Serializable]
    public class FeedbackData
    {
        public int student_id;
        public string message;
    }

    [System.Serializable]
    public class ServerResponse
    {
        public string status;
        public string message;
    }

    [System.Serializable]
    public class ClassInfoResponse
    {
        public int teacher_id;
        public string class_name;
    }

    [System.Serializable]
    public class FeedbackReply
    {
        public string reply_message;
        public string teacher_name;
        public string created_at;
    }

    [System.Serializable]
    public class FeedbackItem
    {
        public int id;
        public string message;
        public string created_at;
        public FeedbackReply[] replies;
    }

    IEnumerator LoadTeacherReplies()
    {
        if (studentId <= 0)
        {
            Debug.LogWarning("Invalid student ID, cannot load replies");
            yield break;
        }

        string url = getFeedbackURL + "?student_id=" + studentId;
        Debug.Log("Loading teacher replies from: " + url);

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string json = www.downloadHandler.text;
                Debug.Log("Replies response: " + json);

                if (!string.IsNullOrEmpty(json) && json.StartsWith("["))
                {
                    // Wrap in object for JsonHelper
                    string wrappedJson = "{\"items\":" + json + "}";
                    FeedbackWrapper wrapper = JsonUtility.FromJson<FeedbackWrapper>(wrappedJson);

                    if (wrapper != null && wrapper.items != null && wrapper.items.Length > 0)
                    {
                        DisplayReplies(wrapper.items);
                    }
                    else
                    {
                        if (replyText != null)
                            replyText.text = "No replies from teacher yet.";
                    }
                }
                else
                {
                    if (replyText != null)
                        replyText.text = "No feedback history found.";
                }
            }
            else
            {
                Debug.LogError("Error loading replies: " + www.error);
                if (replyText != null)
                    replyText.text = "Could not load replies.";
            }
        }
    }

    void DisplayReplies(FeedbackItem[] feedbacks)
    {
        if (replyText == null) return;

        string displayText = "<b><color=#2196F3>Teacher's Replies:</color></b>\n\n";
        
        foreach (var feedback in feedbacks)
        {
            if (feedback.replies != null && feedback.replies.Length > 0)
            {
                displayText += "<b>Your message:</b> " + feedback.message + "\n\n";
                
                foreach (var reply in feedback.replies)
                {
                    displayText += "<color=#4CAF50>Teacher " + reply.teacher_name + ":</color>\n";
                    displayText += reply.reply_message + "\n\n";
                }
                
                displayText += "---\n\n";
            }
        }

        if (displayText == "<b><color=#2196F3>Teacher's Replies:</color></b>\n\n")
        {
            replyText.text = "No replies from teacher yet.\nYour feedback has been sent!";
        }
        else
        {
            replyText.text = displayText;
        }
    }

    [System.Serializable]
    public class FeedbackWrapper
    {
        public FeedbackItem[] items;
    }

    void CreateReplyTextElement()
    {
        // Find the Canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("❌ No Canvas found in scene!");
            return;
        }

        // Create a new GameObject for reply text
        GameObject replyObj = new GameObject("ReplyText");
        replyObj.transform.SetParent(canvas.transform, false);

        // Add TMP_Text component
        replyText = replyObj.AddComponent<TextMeshProUGUI>();

        // Configure RectTransform for bottom half of screen
        RectTransform rectTransform = replyObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.05f, 0.05f);
        rectTransform.anchorMax = new Vector2(0.95f, 0.45f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        // Configure text properties
        replyText.fontSize = 24;
        replyText.color = Color.black;
        replyText.alignment = TextAlignmentOptions.TopLeft;
        replyText.textWrappingMode = TextWrappingModes.Normal;
        replyText.text = "Loading replies...";

        Debug.Log("✅ Created ReplyText element in bottom half of screen");
    }
}
