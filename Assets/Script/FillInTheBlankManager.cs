using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

[System.Serializable]
public class FIBAnswer
{
    public string answer_description;
    public int correct_answer; // 1 = correct, 0 = wrong
}

[System.Serializable]
public class FIBQuestion
{
    public int id;
    public int assignment_id;
    public string question_description;
    public List<FIBAnswer> answers;
}

public class FillInTheBlankManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text questionText;
    public TMP_Text progressText;
    public TMP_InputField answerInput;
    public Button submitButton;
    public GameObject finishPanel;
    public TMP_Text scoreText;

    private List<FIBQuestion> questions = new List<FIBQuestion>();
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

        StartCoroutine(LoadFIBQuestions());
    }

    IEnumerator LoadFIBQuestions()
    {
        assignmentId = CurrentClassSession.SelectedCategoryId; // This is the assignment ID
        string url = $"https://homequest-c3k7.onrender.com/get_fib?student_id={studentId}&assignment_id={assignmentId}";
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error loading FIB questions: " + www.error);
            }
            else
            {
                string json = www.downloadHandler.text.Trim();
                Debug.Log("FIB JSON: " + json);

                if (string.IsNullOrEmpty(json) || !json.StartsWith("["))
                {
                    Debug.LogError("Invalid or empty JSON received!");
                    yield break;
                }

                try
                {
                    questions = JsonUtilityWrapper.FromJsonList<FIBQuestion>(json);
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
                    Debug.LogWarning("No Fill in the Blank questions found.");
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
        List<string> correctAnswers = new List<string>();

        // Collect all correct answers
        foreach (var ans in q.answers)
        {
            if (ans.correct_answer == 1)
                correctAnswers.Add(ans.answer_description);
        }

        string correctAnswerText = string.Join(", ", correctAnswers);

        // Check if user answer matches
        // Support comma-separated answers for multiple blanks
        if (correctAnswers.Count > 1)
        {
            // Multiple blanks - split user answer by comma
            string[] userAnswerParts = userAnswer.Split(new[] { ',', '|' }, System.StringSplitOptions.RemoveEmptyEntries);
            
            // Trim each part
            for (int i = 0; i < userAnswerParts.Length; i++)
                userAnswerParts[i] = userAnswerParts[i].Trim();

            // Check if all parts match (in order)
            if (userAnswerParts.Length == correctAnswers.Count)
            {
                isCorrect = true;
                for (int i = 0; i < userAnswerParts.Length; i++)
                {
                    if (!userAnswerParts[i].Equals(correctAnswers[i], System.StringComparison.OrdinalIgnoreCase))
                    {
                        isCorrect = false;
                        break;
                    }
                }
            }
        }
        else if (correctAnswers.Count == 1)
        {
            // Single blank - direct comparison
            isCorrect = userAnswer.Equals(correctAnswers[0], System.StringComparison.OrdinalIgnoreCase);
        }

        if (isCorrect)
            correctCount++;

        // ✅ Save the answer to history database
        StartCoroutine(SaveAnswerToHistory(
            studentId,
            q.id,
            q.question_description,
            userAnswer,
            correctAnswerText,
            isCorrect ? 1 : 0
        ));

        // Show feedback
        StartCoroutine(ShowFeedbackAndContinue(isCorrect));
    }

    IEnumerator ShowFeedbackAndContinue(bool isCorrect)
    {
        Debug.Log(isCorrect ? "✓ Answer was CORRECT!" : "✗ Answer was INCORRECT!");

        currentIndex++;
        ShowQuestion();
        yield break;
    }

    // ✅ New: Save each answer to `history` table
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
                Debug.Log("✅ Saved answer to history: " + www.downloadHandler.text);
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
                Debug.Log("✅ FIB Score saved successfully!");
        }

        // Wait 5 seconds then navigate to gameresult scene
        yield return new WaitForSeconds(5f);
        SceneManager.LoadScene("gameresult");
    }
}
