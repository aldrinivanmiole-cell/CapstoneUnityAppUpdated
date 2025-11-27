using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

[System.Serializable]
public class YesNoAnswer
{
    public string answer_description; // "Yes" or "No"
    public int correct_answer;        // 1 = correct, 0 = wrong
}

[System.Serializable]
public class YesNoQuestion
{
    public int id;
    public string question_description;
    public string tutorial_link;      // tutorial link
    public List<YesNoAnswer> answers;
}

public class YesNoServerResponse
{
    public string status;
    public string message;
}

public class YesNoManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text questionText;
    public TMP_Text progressText;
    public Button yesButton;
    public Button noButton;
    public Button tutorialButton; // tutorial button
    public GameObject finishPanel;
    public TMP_Text scoreText;

    private List<YesNoQuestion> questions = new List<YesNoQuestion>();
    private int currentIndex = 0;
    private int correctCount = 0;
    private int studentId;
    private int assignmentId; // ✅ store assignmentId separately

    void Start()
    {
        if (SessionManager.Instance == null)
        {
            Debug.LogError("SessionManager not found!");
            return;
        }

        // Get student ID
        studentId = SessionManager.Instance.StudentId;
        Debug.Log("Student ID: " + studentId);
        if (studentId <= 0)
        {
            Debug.LogError("Invalid student ID! Cannot start quiz.");
            return;
        }

        finishPanel.SetActive(false);

        yesButton.onClick.AddListener(() => OnAnswerSelected("True"));
        noButton.onClick.AddListener(() => OnAnswerSelected("False"));

        StartCoroutine(LoadYesNoQuestions());
    }

    IEnumerator LoadYesNoQuestions()
    {
        using (UnityWebRequest www = UnityWebRequest.Get("https://homeworkquest.site/get_yesno.php?student_id=" + studentId))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error loading Yes/No questions: " + www.error);
            }
            else
            {
                string json = www.downloadHandler.text;
                Debug.Log("Yes/No Questions JSON: " + json);

                questions = JsonUtilityWrapper.FromJsonList<YesNoQuestion>(json);

                if (questions.Count > 0)
                {
                    // ✅ Get assignmentId from first question or your DB
                    assignmentId = questions[0].id;
                    Debug.Log("Assignment ID: " + assignmentId);

                    ShowQuestion();
                }
                else
                {
                    Debug.LogWarning("No Yes/No questions found.");
                }
            }
        }
    }

    void ShowQuestion()
    {
        if (currentIndex >= questions.Count)
        {
            StartCoroutine(SaveScore());
            return;
        }

        var q = questions[currentIndex];
        questionText.text = q.question_description;
        progressText.text = $"Question {currentIndex + 1} of {questions.Count}";

        if (!string.IsNullOrEmpty(q.tutorial_link))
        {
            tutorialButton.gameObject.SetActive(true);
            tutorialButton.onClick.RemoveAllListeners();
            tutorialButton.onClick.AddListener(() => OpenTutorial(q.tutorial_link));
        }
        else
        {
            tutorialButton.gameObject.SetActive(false);
        }
    }

    void OnAnswerSelected(string selected)
    {
        var q = questions[currentIndex];
        string correctAnswerText = "";
        bool isCorrect = false;

        foreach (var ans in q.answers)
        {
            if (ans.correct_answer == 1)
                correctAnswerText = ans.answer_description;

            if (ans.correct_answer == 1 &&
                selected.Equals(ans.answer_description, System.StringComparison.OrdinalIgnoreCase))
            {
                correctCount++;
                isCorrect = true;
            }
        }

        StartCoroutine(SaveAnswerToHistory(
            studentId,
            q.id,
            q.question_description,
            selected,
            correctAnswerText,
            isCorrect ? 1 : 0
        ));

        currentIndex++;
        ShowQuestion();
    }

    void OpenTutorial(string url)
    {
        Application.OpenURL(url);
    }

    IEnumerator SaveAnswerToHistory(int studentId, int questionId, string question, string playerAnswer, string correctAnswer, int isCorrect)
    {
        WWWForm form = new WWWForm();
        form.AddField("student_id", studentId);
        form.AddField("question_id", questionId);
        form.AddField("question_description", question);
        form.AddField("player_answer", playerAnswer);
        form.AddField("correct_answer", correctAnswer);
        form.AddField("is_correct", isCorrect);
        form.AddField("assignment_type", "True/False");

        using (UnityWebRequest www = UnityWebRequest.Post("https://homeworkquest.site/save_history.php", form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error saving answer: " + www.error);
            }
            else
            {
                Debug.Log("Saved answer: " + www.downloadHandler.text);
            }
        }
    }

    IEnumerator SaveScore()
    {
        finishPanel.SetActive(true);
        scoreText.text = $"You answered {correctCount} / {questions.Count} correctly!";

        if (studentId <= 0 || assignmentId <= 0)
        {
            Debug.LogError("Cannot save score. Invalid student or assignment ID.");
            yield break;
        }

        WWWForm form = new WWWForm();
        form.AddField("student_id", studentId);
        form.AddField("assignment_id", assignmentId);
        form.AddField("score", correctCount);

        using (UnityWebRequest www = UnityWebRequest.Post("https://homeworkquest.site/submit_score2.php", form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error saving score: " + www.error);
            }
            else
            {
                var response = JsonUtility.FromJson<YesNoServerResponse>(www.downloadHandler.text);
                Debug.Log("✅ Score saved: " + response.message);
            }
        }
    }
}
