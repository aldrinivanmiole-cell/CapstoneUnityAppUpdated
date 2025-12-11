using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

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
    public int assignment_id;
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
    public Button yesButton;  // True button (can be styled as potion)
    public Button noButton;   // False button (can be styled as potion)
    public Button tutorialButton; // tutorial button
    public GameObject finishPanel;
    public TMP_Text scoreText;
    
    [Header("Potion Theme (Optional)")]
    public Image truePotionImage;   // Potion image for True (optional)
    public Image falsePotionImage;  // Potion image for False (optional)
    public TMP_Text truePotionLabel; // Label for True potion (optional)
    public TMP_Text falsePotionLabel; // Label for False potion (optional)

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

        // Setup potion buttons
        if (yesButton != null)
            yesButton.onClick.AddListener(() => OnAnswerSelected("True"));
        if (noButton != null)
            noButton.onClick.AddListener(() => OnAnswerSelected("False"));
        
        // Update button labels if potion theme is used
        if (truePotionLabel != null)
            truePotionLabel.text = "TRUE";
        if (falsePotionLabel != null)
            falsePotionLabel.text = "FALSE";

        StartCoroutine(LoadYesNoQuestions());
    }

    IEnumerator LoadYesNoQuestions()
    {
        assignmentId = CurrentClassSession.SelectedCategoryId; // This is the assignment ID
        string url = $"https://homequest-c3k7.onrender.com/get_yesno?student_id={studentId}&assignment_id={assignmentId}";
        using (UnityWebRequest www = UnityWebRequest.Get(url))
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
                
                // Debug each question's answers
                foreach (var q in questions)
                {
                    Debug.Log($"Question: {q.question_description}");
                    foreach (var ans in q.answers)
                    {
                        Debug.Log($"  Answer: '{ans.answer_description}' - Correct: {ans.correct_answer}");
                    }
                }

                if (questions.Count > 0)
                {
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

        Debug.Log($"User selected: '{selected}'");
        
        foreach (var ans in q.answers)
        {
            Debug.Log($"Checking answer: '{ans.answer_description}' (correct={ans.correct_answer})");
            
            if (ans.correct_answer == 1)
            {
                correctAnswerText = ans.answer_description.Trim();
            }

            if (ans.correct_answer == 1 &&
                selected.Equals(ans.answer_description.Trim(), System.StringComparison.OrdinalIgnoreCase))
            {
                correctCount++;
                isCorrect = true;
            }
        }

        Debug.Log($"Correct answer: '{correctAnswerText}' | User got it: {isCorrect}");

        StartCoroutine(SaveAnswerToHistory(
            studentId,
            q.id,
            q.question_description,
            selected,
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

    void OpenTutorial(string url)
    {
        Application.OpenURL(url);
    }

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
        PlayerPrefs.SetInt("TotalQuestions", questions.Count);
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

        using (UnityWebRequest www = UnityWebRequest.Post("https://homequest-c3k7.onrender.com/submit_score2", form))
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

        // Wait 3 seconds then navigate to potion ending scene
        yield return new WaitForSeconds(3f);
        SceneManager.LoadScene("PotionEnding");
    }
}
