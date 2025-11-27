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
        using (UnityWebRequest www = UnityWebRequest.Get("https://homeworkquest.site/get_essay.php?student_id=" + studentId))
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

        // ✅ Simple scoring: check if answer contains a keyword (e.g., correct_answer)
        int score = 0;
        if (!string.IsNullOrEmpty(answer))
        {
            // Example: if essay should contain "Unity", we can give 1 point
            if (answer.ToLower().Contains("unity"))
                score = 1; // 1 point for correct keyword
        }

        StartCoroutine(SaveEssayAnswer(studentId, q.id, q.question_description, answer, score));
    }

IEnumerator SaveEssayAnswer(int studentId, int questionId, string questionText, string playerAnswer, int score)
{
    WWWForm essayForm = new WWWForm();
    essayForm.AddField("student_id", studentId);
    essayForm.AddField("assignment_id", questionId);
    essayForm.AddField("answer_text", playerAnswer);
    essayForm.AddField("score", score); // ✅ send score to server

    using (UnityWebRequest www = UnityWebRequest.Post("https://homeworkquest.site/submit_essay.php", essayForm))
    {
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error submitting essay: " + www.error);
        }
        else
        {
            var response = JsonUtility.FromJson<EssayServerResponse>(www.downloadHandler.text);
            Debug.Log("Essay submitted: " + response.message + " | Score: " + score);
        }
    }

    currentIndex++;
    ShowQuestion();
}

    IEnumerator SaveEssayAnswer(int studentId, int questionId, string questionText, string playerAnswer)
    {
        WWWForm essayForm = new WWWForm();
        essayForm.AddField("student_id", studentId);
        essayForm.AddField("assignment_id", questionId);
        essayForm.AddField("answer_text", playerAnswer);

        using (UnityWebRequest www = UnityWebRequest.Post("https://homeworkquest.site/submit_essay.php", essayForm))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error submitting essay: " + www.error);
            }
            else
            {
                var response = JsonUtility.FromJson<EssayServerResponse>(www.downloadHandler.text);
                Debug.Log("Essay submitted: " + response.message);
            }
        }

        currentIndex++;
        ShowQuestion();
    }
}
