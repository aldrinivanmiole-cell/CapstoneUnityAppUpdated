using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

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
    public int assignment_id;
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
        submitButton.onClick.AddListener(OnSubmitAnswer);

        StartCoroutine(LoadIdentificationQuestions());
    }

    IEnumerator LoadIdentificationQuestions()
    {
        assignmentId = CurrentClassSession.SelectedCategoryId; // This is the assignment ID
        string url = $"https://homequest-c3k7.onrender.com/get_identification?student_id={studentId}&assignment_id={assignmentId}";
        using (UnityWebRequest www = UnityWebRequest.Get(url))
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
                {
                    assignmentId = questions[0].assignment_id;
                    Debug.Log("Assignment ID: " + assignmentId);
                    ShowQuestion();
                }
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

        // Get correct answer and check if user answer matches
        foreach (var ans in q.answers)
        {
            if (ans.correct_answer == 1)
            {
                correctAnswer = ans.answer_description.Trim();
                
                // Case-insensitive comparison with trimmed whitespace
                if (userAnswer.Equals(correctAnswer, System.StringComparison.OrdinalIgnoreCase))
                {
                    isCorrect = true;
                }
                break;
            }
        }

        Debug.Log($"User Answer: '{userAnswer}' (Length: {userAnswer.Length})");
        Debug.Log($"Correct Answer: '{correctAnswer}' (Length: {correctAnswer.Length})");
        Debug.Log($"Is Correct: {isCorrect}");
        Debug.Log($"Correct Count BEFORE: {correctCount}");

        if (isCorrect)
        {
            correctCount++;
            Debug.Log($"✅ CORRECT! Count increased to: {correctCount}");
        }
        else
        {
            Debug.Log($"❌ WRONG! Count stays: {correctCount}");
        }

        // Update index
        currentIndex++;
        
        // ✅ Save to history immediately
        StartCoroutine(SaveAnswerToHistory(
            studentId,
            q.id,
            q.question_description,
            userAnswer,
            correctAnswer,
            isCorrect ? 1 : 0
        ));

        // Wait a moment then show next question
        StartCoroutine(ShowNextQuestionDelayed());
    }

    IEnumerator ShowNextQuestionDelayed()
    {
        yield return new WaitForSeconds(1.5f);
        ShowQuestion();
    }



    // ✅ NEW: Save every answer to database
    IEnumerator SaveAnswerToHistory(int studentId, int questionId, string question, string playerAnswer, string correctAnswer, int isCorrect)
    {
        WWWForm form = new WWWForm();
        form.AddField("student_id", studentId);
        form.AddField("assignment_id", assignmentId);
        form.AddField("question_id", questionId);
        form.AddField("question_text", question);
        form.AddField("student_answer", playerAnswer);
        form.AddField("correct_answer", correctAnswer);
        form.AddField("is_correct", isCorrect);

        using (UnityWebRequest www = UnityWebRequest.Post("https://homequest-c3k7.onrender.com/save_history", form))
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
        // Show finish panel with score
        finishPanel.SetActive(true);
        
        if (scoreText != null)
        {
            scoreText.text = $"You got {correctCount} out of {questions.Count} correct!";
            scoreText.color = Color.white;
            scoreText.fontSize = 36;
            scoreText.gameObject.SetActive(true);
        }
        
        Debug.Log($"Final Score: {correctCount}/{questions.Count}");

        // Calculate percentage score for trophy system
        int percentageScore = (questions.Count > 0) ? (correctCount * 100) / questions.Count : 0;
        PlayerPrefs.SetInt("PlayerScore", percentageScore);
        PlayerPrefs.Save();
        Debug.Log($"✅ Score saved to PlayerPrefs: {percentageScore}%");

        if (studentId <= 0 || assignmentId <= 0)
        {
            Debug.LogError("Cannot save score. Invalid student or assignment ID.");
            yield break;
        }

        WWWForm form = new WWWForm();
        form.AddField("student_id", studentId);
        form.AddField("assignment_id", assignmentId);
        form.AddField("score", correctCount);

        using (UnityWebRequest www = UnityWebRequest.Post("https://homequest-c3k7.onrender.com/submit_score", form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
                Debug.LogError("Error saving score: " + www.error);
            else
                Debug.Log("✅ Identification score saved successfully!");
        }

        // Wait 5 seconds then navigate to gameresult scene
        yield return new WaitForSeconds(5f);
        SceneManager.LoadScene("gameresult");
    }
}
