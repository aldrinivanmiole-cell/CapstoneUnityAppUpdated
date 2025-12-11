using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Linq;
using UnityEngine.SceneManagement;

[System.Serializable]
public class EnumerationAnswer
{
    public string answer_description;
    public int correct_answer; // 1 = correct, 0 = wrong
}

[System.Serializable]
public class EnumerationQuestion
{
    public int id;
    public int assignment_id;
    public string question_description;
    public List<EnumerationAnswer> answers;
}

public class EnumerationManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text questionText;
    public TMP_Text progressText;
    public TMP_InputField answerInput;
    public Button addAnswerButton;
    public Transform answerList;
    public TMP_Text answerItemPrefab; // assign a TMP_Text prefab here
    public Button submitButton;
    public GameObject finishPanel;
    public TMP_Text scoreText;

    private List<EnumerationQuestion> questions = new List<EnumerationQuestion>();
    private int currentIndex = 0;
    private int correctCount = 0;
    private int studentId;
    private int assignmentId;
    private List<string> studentAnswers = new List<string>();
    private int expectedAnswerCount = 0;

    void Start()
    {
        if (SessionManager.Instance == null)
        {
            Debug.LogError("SessionManager not found!");
            return;
        }

        studentId = SessionManager.Instance.StudentId;
        finishPanel.SetActive(false);

        addAnswerButton.onClick.AddListener(OnAddAnswer);
        submitButton.onClick.AddListener(OnSubmitAnswers);

        StartCoroutine(LoadEnumerationQuestions());
    }

    IEnumerator LoadEnumerationQuestions()
    {
        assignmentId = CurrentClassSession.SelectedCategoryId; // This is the assignment ID
        string url = $"https://homequest-c3k7.onrender.com/get_enumeration?student_id={studentId}&assignment_id={assignmentId}";
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error loading enumeration questions: " + www.error);
            }
            else
            {
                string json = www.downloadHandler.text.Trim();
                Debug.Log("Enumeration JSON: " + json);

                if (string.IsNullOrEmpty(json) || !json.StartsWith("["))
                {
                    Debug.LogError("Invalid or empty JSON received!");
                    yield break;
                }

                try
                {
                    questions = JsonUtilityWrapper.FromJsonList<EnumerationQuestion>(json);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("JSON parse failed: " + ex.Message);
                    yield break;
                }

                if (questions.Count > 0)
                {
                    assignmentId = questions[0].assignment_id;
                    ShowQuestion();
                }
                else
                    Debug.LogWarning("No Enumeration questions found.");
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

        // Clear previous answers
        studentAnswers.Clear();
        
        // Store expected answer count for auto-submit
        expectedAnswerCount = q.answers?.Count ?? 0;
        foreach (Transform child in answerList)
            Destroy(child.gameObject);

        answerInput.text = "";
        
        // Re-enable buttons for new question
        if (submitButton != null)
            submitButton.interactable = true;
        if (addAnswerButton != null)
            addAnswerButton.interactable = true;
    }

    public void OnAddAnswer()
    {
        if (answerInput == null)
        {
            Debug.LogError("❌ Missing answerInput reference!");
            return;
        }

        string ans = answerInput.text.Trim();
        if (string.IsNullOrEmpty(ans)) return;

        studentAnswers.Add(ans);
        answerInput.text = "";
        
        // Auto-submit if all answers provided
        if (expectedAnswerCount > 0 && studentAnswers.Count >= expectedAnswerCount)
        {
            OnSubmitAnswers();
        }
    }

    void OnSubmitAnswers()
    {
        // Check if we're at the end
        if (currentIndex >= questions.Count)
        {
            return;
        }
        
        // Disable submit button to prevent double-clicks
        if (submitButton != null)
            submitButton.interactable = false;
        
        var q = questions[currentIndex];
        int correctForThis = 0;
        string correctAnswersText = "";
        string playerAnswersText = string.Join(", ", studentAnswers);

        Debug.Log($"🔍 Enumeration - Checking answers for question: {q.question_description}");
        Debug.Log($"🔍 Enumeration - q.answers is null? {q.answers == null}");
        Debug.Log($"🔍 Enumeration - q.answers count: {q.answers?.Count ?? 0}");

        // Compare each correct answer in the database with student's list
        if (q.answers != null)
        {
            foreach (var correctAns in q.answers)
            {
                Debug.Log($"🔍 Answer: {correctAns.answer_description}, correct_answer: {correctAns.correct_answer}");
                
                if (correctAns.correct_answer == 1)
                {
                    correctAnswersText += correctAns.answer_description + ", ";

                    foreach (var studentAns in studentAnswers)
                    {
                        if (studentAns.Equals(correctAns.answer_description, System.StringComparison.OrdinalIgnoreCase))
                        {
                            correctForThis++;
                            break;
                        }
                    }
                }
            }
        }

        // Trim last comma
        if (correctAnswersText.EndsWith(", "))
            correctAnswersText = correctAnswersText.Substring(0, correctAnswersText.Length - 2);

        Debug.Log($"📝 Enumeration - Player Answers: {playerAnswersText}");
        Debug.Log($"✅ Enumeration - Correct Answers: {correctAnswersText}");
        
        int expectedCorrectCount = q.answers?.FindAll(a => a.correct_answer == 1).Count ?? 0;
        Debug.Log($"📊 Enumeration - Got {correctForThis} correct out of {expectedCorrectCount}");
        Debug.Log($"📊 Enumeration - Student submitted {studentAnswers.Count} answers total");

        // Determine correctness: must have all correct answers AND no extra wrong answers
        bool isFullyCorrect = (correctForThis == expectedCorrectCount) && (studentAnswers.Count == expectedCorrectCount);
        
        if (!isFullyCorrect)
        {
            Debug.Log($"❌ Got {correctForThis} correct out of {expectedCorrectCount}");
        }
        else
        {
            Debug.Log($"✅ CORRECT: All {expectedCorrectCount} answers matched!");
            correctCount++;
        }

        // ✅ Save the student's answers to the history database
        StartCoroutine(SaveAnswerToHistory(
            studentId,
            q.id,
            q.question_description,
            playerAnswersText,
            correctAnswersText,
            isFullyCorrect ? 1 : 0
        ));

        // Show feedback with score
        StartCoroutine(ShowFeedbackAndContinue(correctForThis, expectedCorrectCount));
    }

    IEnumerator ShowFeedbackAndContinue(int correctAnswers, int totalCorrect)
    {
        // Show finish panel with feedback
        finishPanel.SetActive(true);
        if (scoreText != null)
        {
            scoreText.text = $"{correctAnswers} out of {totalCorrect} correct answers!";
            scoreText.color = (correctAnswers == totalCorrect) ? Color.green : Color.red;
            scoreText.fontSize = 30;
            scoreText.gameObject.SetActive(true);
        }
        
        yield return new WaitForSeconds(2f);
        finishPanel.SetActive(false);

        // Update index
        currentIndex++;
        
        // Show next question
        ShowQuestion();
    }

    // ✅ Save each Enumeration answer to history
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
                Debug.Log("✅ Enumeration answer saved to history: " + www.downloadHandler.text);
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

        int assignmentId = (questions.Count > 0) ? questions[0].id : 0;

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
                Debug.Log("✅ Enumeration score saved successfully!");
        }

        // Wait 5 seconds then navigate to gameresult scene
        yield return new WaitForSeconds(5f);
        SceneManager.LoadScene("gameresult");
    }
}
