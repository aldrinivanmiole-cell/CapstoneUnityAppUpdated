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

    [Header("Server Settings")]
    public string saveFeedbackURL = "https://homeworkquest.site/save_feedback.php";

    private int studentId;

    void Start()
    {
        // You can get the logged-in student ID from your session manager
        if (SessionManager.Instance != null)
            studentId = SessionManager.Instance.StudentId;
        else
            studentId = 0;

        sendButton.onClick.AddListener(SendFeedbackMessage);
        backButton.onClick.AddListener(GoBack);
    }

    void GoBack()
    {
        SceneManager.LoadScene("map"); // Change this to your main scene
    }

    void SendFeedbackMessage()
    {
        string message = feedbackInput.text.Trim();

        if (string.IsNullOrEmpty(message))
        {
            ShowMessage("⚠️ Please enter your feedback first.", Color.red);
            return;
        }

        StartCoroutine(SendFeedbackToServer(studentId, message));
    }

    IEnumerator SendFeedbackToServer(int id, string message)
    {
        FeedbackData data = new FeedbackData { student_id = id, message = message };
        string json = JsonUtility.ToJson(data);

        using (UnityWebRequest www = new UnityWebRequest(saveFeedbackURL, "POST"))
        {
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(body);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                ServerResponse response = JsonUtility.FromJson<ServerResponse>(www.downloadHandler.text);

                if (response.status == "success")
                {
                    ShowMessage("✅ Feedback sent successfully!", Color.green);
                    feedbackInput.text = "";
                }
                else
                {
                    ShowMessage("❌ " + response.message, Color.red);
                }
            }
            else
            {
                ShowMessage("⚠️ Error sending feedback: " + www.error, Color.red);
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
}
