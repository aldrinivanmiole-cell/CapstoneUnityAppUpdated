using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

[System.Serializable]
public class FeedbackItem
{
    public int feedback_id;
    public string student_feedback;
    public string date_submitted;
    public string teacher_reply;
    public string date_replied;
}

[System.Serializable]
public class FeedbackResponse
{
    public string status;
    public List<FeedbackItem> feedbacks;
}

public class ViewFeedbackReplies : MonoBehaviour
{
    [Header("UI References")]
    public Transform contentContainer;   // ScrollView Content
    public GameObject feedbackItemPrefab; // Prefab with feedback + reply display
    public TMP_Text titleText;

    [Header("Server")]
    public string getFeedbackUrl = "https://homeworkquest.site/get_feedback_reply.php";

    private int studentId;

    void Start()
    {
        if (SessionManager.Instance != null)
        {
            studentId = SessionManager.Instance.StudentId;
            titleText.text = $"{SessionManager.Instance.Username}'s Feedback History";
            StartCoroutine(LoadFeedbackReplies());
        }
        else
        {
            Debug.LogError("SessionManager not found or student not logged in!");
        }
    }

    IEnumerator LoadFeedbackReplies()
    {
        using (UnityWebRequest www = UnityWebRequest.Get(getFeedbackUrl + "?student_id=" + studentId))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error fetching feedback: " + www.error);
                yield break;
            }

            FeedbackResponse response = JsonUtility.FromJson<FeedbackResponse>(www.downloadHandler.text);

            if (response.status != "success" || response.feedbacks == null)
            {
                Debug.LogWarning("No feedback found for this student.");
                yield break;
            }

            foreach (Transform child in contentContainer)
                Destroy(child.gameObject);

            foreach (var f in response.feedbacks)
            {
                GameObject row = Instantiate(feedbackItemPrefab, contentContainer);
                TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>();

                // Expected order: Feedback, Date Submitted, Reply, Date Replied
                texts[0].text = f.student_feedback;
                texts[1].text = "Sent: " + f.date_submitted;

                if (!string.IsNullOrEmpty(f.teacher_reply))
                {
                    texts[2].text = "🟢 Reply: " + f.teacher_reply;
                    texts[3].text = "Replied: " + f.date_replied;
                }
                else
                {
                    texts[2].text = "⏳ No reply yet.";
                    texts[3].text = "";
                }
            }
        }
    }
}
