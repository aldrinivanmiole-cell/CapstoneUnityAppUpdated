using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

[System.Serializable]
public class IdentificationAnswer
{
    public string answer_description;
    public int correct_answer; // 1 = correct, 0 = wrong
}

[System.Serializable]
public class IdentificationQuestion
{
    public int id;
    public string question_description;
    public List<IdentificationAnswer> answers;
}

public class IdentificationManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text questionText;
    public TMP_Text progressText;
    public TMP_InputField answerInput;
    public Button submitButton;
    public GameObject finishPanel;
    public TMP_Text scoreText;

    private List<IdentificationQuestion> questions = new List<IdentificationQuestion>();
    private int currentIndex = 0;
    private int correctCount = 0;
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
        submitButton.onClick.AddListener(OnSubmitAnswer);

        StartCoroutine(LoadIdentificationQuestions());
    }

    IEnumerator LoadIdentificationQuestions()
    {
        using (UnityWebRequest www = UnityWebRequest.Get("https://homeworkquest.site/get_identification.php?student_id=" + studentId))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error loading identification questions: " + www.error);
            }
            else
            {
                string json = www.downloadHandler.text.Trim();
                Debug.Log("Identification JSON: " + json);

                if (string.IsNullOrEmpty(json) || !json.StartsWith("["))
                {
                    Debug.LogError("Invalid or empty JSON received!");
                    yield break;
                }

                try
                {
                    questions = JsonUtilityWrapper.FromJsonList<IdentificationQuestion>(json);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("JSON parse failed: " + ex.Message);
                    yield break;
                }

                if (questions.Count > 0)
                    ShowQuestion();
                else
                    Debug.LogWarning("No Identification questions found.");
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
        answerInput.text = "";
    }

    void OnSubmitAnswer()
    {
        string userAnswer = answerInput.text.Trim();
        if (string.IsNullOrEmpty(userAnswer))
        {
            Debug.LogWarning("Please enter an answer.");
            return;
        }

        var q = questions[currentIndex];
        bool isCorrect = false;
        string correctAnswer = "";

        foreach (var ans in q.answers)
        {
            if (ans.correct_answer == 1)
                correctAnswer = ans.answer_description;

            if (ans.correct_answer == 1 &&
                userAnswer.Equals(ans.answer_description, System.StringComparison.OrdinalIgnoreCase))
            {
                isCorrect = true;
                break;
            }
        }

        if (isCorrect)
            correctCount++;

        // ✅ Save to history immediately
        StartCoroutine(SaveAnswerToHistory(
            studentId,
            q.id,
            q.question_description,
            userAnswer,
            correctAnswer,
            isCorrect ? 1 : 0
        ));

        currentIndex++;
        ShowQuestion();
    }

    // ✅ NEW: Save every answer to database
    IEnumerator SaveAnswerToHistory(int studentId, int questionId, string question, string playerAnswer, string correctAnswer, int isCorrect)
    {
        WWWForm form = new WWWForm();
        form.AddField("student_id", studentId);
        form.AddField("question_id", questionId);
        form.AddField("question_description", question);
        form.AddField("player_answer", playerAnswer);
        form.AddField("correct_answer", correctAnswer);
        form.AddField("is_correct", isCorrect);
        form.AddField("assignment_type", "Identification"); // ✅ identify question type

        using (UnityWebRequest www = UnityWebRequest.Post("https://homeworkquest.site/save_history.php", form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
                Debug.LogError("❌ Error saving history: " + www.error);
            else
                Debug.Log("✅ Saved to history: " + www.downloadHandler.text);
        }
    }

    IEnumerator SaveScore()
    {
        finishPanel.SetActive(true);
        scoreText.text = $"You answered {correctCount} / {questions.Count} correctly!";

        int assignmentId = (questions.Count > 0) ? questions[0].id : 0;

        WWWForm form = new WWWForm();
        form.AddField("student_id", studentId);
        form.AddField("assignment_id", assignmentId);
        form.AddField("score", correctCount);

        using (UnityWebRequest www = UnityWebRequest.Post("https://homeworkquest.site/submit_score.php", form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
                Debug.LogError("Error saving score: " + www.error);
            else
                Debug.Log("✅ Identification score saved successfully!");
        }
    }
}
