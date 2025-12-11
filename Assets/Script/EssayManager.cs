using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

[System.Serializable]
public class EssayQuestion
{
    public int id;
    public int assignment_id;
    public string question_description;
    public string tutorial_link; // ✅ Add tutorial link
}

public class EssayServerResponse
{
    public string status;
    public string message;
}

public class EssayManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text questionText;
    public TMP_Text progressText;
    public TMP_InputField essayInput;
    public Button submitButton;
    public Button tutorialButton; // ✅ New button for tutorial
    public GameObject finishPanel;
    public TMP_Text messageText;

    private List<EssayQuestion> questions = new List<EssayQuestion>();
    private int currentIndex = 0;
    private int studentId;
    private int assignmentId;

    void Start()
    {
        if (SessionManager.Instance == null)
        {
            Debug.LogError("SessionManager not found!");
            return;
        }

        studentId = SessionManager.Instance.StudentId;
        finishPanel.SetActive(false);
        submitButton.onClick.AddListener(OnSubmitEssay);
        tutorialButton.onClick.AddListener(OpenTutorialLink); // ✅ Add listener

        StartCoroutine(LoadEssayQuestions());
    }

    IEnumerator LoadEssayQuestions()
    {
        assignmentId = CurrentClassSession.SelectedCategoryId; // This is the assignment ID
        string url = $"https://homequest-c3k7.onrender.com/get_essay?student_id={studentId}&assignment_id={assignmentId}";
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error loading essay questions: " + www.error);
            }
            else
            {
                string json = www.downloadHandler.text.Trim();
                Debug.Log("Essay JSON: " + json);

                if (string.IsNullOrEmpty(json) || !json.StartsWith("["))
                {
                    Debug.LogError("Invalid or empty JSON received!");
                    yield break;
                }

                try
                {
                    questions = JsonUtilityWrapper.FromJsonList<EssayQuestion>(json);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("JSON parse failed: " + ex.Message);
                    yield break;
                }

                if (questions.Count > 0)
                    ShowQuestion();
                else
                    Debug.LogWarning("No Essay questions found.");
            }
        }
    }

    void ShowQuestion()
    {
        if (currentIndex >= questions.Count)
        {
            finishPanel.SetActive(true);
            messageText.text = "All essay answers submitted. Waiting for teacher review.";
            return;
        }

        var q = questions[currentIndex];
        questionText.text = q.question_description;
        progressText.text = $"Question {currentIndex + 1} of {questions.Count}";
        essayInput.text = "";

        // ✅ Enable tutorial button if link exists
        tutorialButton.gameObject.SetActive(!string.IsNullOrEmpty(q.tutorial_link));
    }

    void OpenTutorialLink()
    {
        var q = questions[currentIndex];
        if (!string.IsNullOrEmpty(q.tutorial_link))
        {
            Application.OpenURL(q.tutorial_link);
        }
        else
        {
            Debug.LogWarning("No tutorial link available for this question.");
        }
    }

    void OnSubmitEssay()
    {
        string answer = essayInput.text.Trim();
        if (string.IsNullOrEmpty(answer))
        {
            Debug.LogWarning("Please write your essay answer before submitting.");
            return;
        }

        var q = questions[currentIndex];
        StartCoroutine(SaveEssayAnswer(studentId, assignmentId, q.id, q.question_description, answer));
    }

    IEnumerator SaveEssayAnswer(int studentId, int assignmentId, int questionId, string questionText, string playerAnswer)
    {
        WWWForm form = new WWWForm();
        form.AddField("student_id", studentId);
        form.AddField("assignment_id", assignmentId);
        form.AddField("question_id", questionId);
        form.AddField("question_text", questionText);
        form.AddField("student_answer", playerAnswer);
        form.AddField("correct_answer", "Pending teacher review");
        form.AddField("is_correct", 0); // Essays are not auto-graded

        using (UnityWebRequest www = UnityWebRequest.Post("https://homequest-c3k7.onrender.com/save_history", form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error saving essay: " + www.error);
            }
            else
            {
                Debug.Log("✅ Essay saved to history successfully!");
            }
        }

        currentIndex++;
        
        if (currentIndex >= questions.Count)
        {
            // All essays submitted, mark as completed
            StartCoroutine(MarkAssignmentComplete());
        }
        else
        {
            ShowQuestion();
        }
    }
    
    IEnumerator MarkAssignmentComplete()
    {
        // Submit assignment completion to mark as completed
        WWWForm form = new WWWForm();
        form.AddField("student_id", studentId);
        form.AddField("assignment_id", assignmentId);
        form.AddField("score", 0); // Essays are graded by teacher, score is 0 until graded

        using (UnityWebRequest www = UnityWebRequest.Post("https://homequest-c3k7.onrender.com/submit_score", form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error marking assignment complete: " + www.error);
            }
            else
            {
                Debug.Log("✅ Essay assignment marked as complete!");
            }
        }
        
        // Show completion message and go back to map
        finishPanel.SetActive(true);
        messageText.text = "All essays submitted!\nWaiting for teacher review.";
        yield return new WaitForSeconds(3f);
        UnityEngine.SceneManagement.SceneManager.LoadScene("map");
    }
}
