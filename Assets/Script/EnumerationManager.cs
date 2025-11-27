using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Linq;

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
    private List<string> studentAnswers = new List<string>();

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
        using (UnityWebRequest www = UnityWebRequest.Get("https://homeworkquest.site/get_enumeration.php?student_id=" + studentId))
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
                    ShowQuestion();
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
        foreach (Transform child in answerList)
            Destroy(child.gameObject);

        answerInput.text = "";
    }

    public void OnAddAnswer()
    {
        if (answerInput == null || answerList == null)
        {
            Debug.LogError("❌ Missing references! Assign answerInput and answerList in Inspector.");
            return;
        }

        string ans = answerInput.text.Trim();
        if (string.IsNullOrEmpty(ans)) return;

        GameObject newTextObj = new GameObject("AnswerItem");
        newTextObj.transform.SetParent(answerList, false);

        TextMeshProUGUI tmp = newTextObj.AddComponent<TextMeshProUGUI>();
        tmp.text = ans;
        tmp.font = answerInput.textComponent.font;
        tmp.fontSize = 20;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = Color.black;

        studentAnswers.Add(ans);
        answerInput.text = "";
    }

    void OnSubmitAnswers()
    {
        var q = questions[currentIndex];
        int correctForThis = 0;
        string correctAnswersText = "";
        string playerAnswersText = string.Join(", ", studentAnswers);

        // Compare each correct answer in the database with student's list
        foreach (var correctAns in q.answers)
        {
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

        // Trim last comma
        if (correctAnswersText.EndsWith(", "))
            correctAnswersText = correctAnswersText.Substring(0, correctAnswersText.Length - 2);

        // Determine correctness
        bool isFullyCorrect = (correctForThis == q.answers.FindAll(a => a.correct_answer == 1).Count);
        if (isFullyCorrect)
            correctCount++;

        // ✅ Save the student's answers to the history database
        StartCoroutine(SaveAnswerToHistory(
            studentId,
            q.id,
            q.question_description,
            playerAnswersText,
            correctAnswersText,
            isFullyCorrect ? 1 : 0
        ));

        currentIndex++;
        ShowQuestion();
    }

    // ✅ Save each Enumeration answer to history
    IEnumerator SaveAnswerToHistory(int studentId, int questionId, string question, string playerAnswer, string correctAnswer, int isCorrect)
    {
        WWWForm form = new WWWForm();
        form.AddField("student_id", studentId);
        form.AddField("question_id", questionId);
        form.AddField("question_description", question);
        form.AddField("player_answer", playerAnswer);
        form.AddField("correct_answer", correctAnswer);
        form.AddField("is_correct", isCorrect);
        form.AddField("assignment_type", "Enumeration");

        using (UnityWebRequest www = UnityWebRequest.Post("https://homeworkquest.site/save_history.php", form))
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
                Debug.Log("✅ Enumeration score saved successfully!");
        }
    }
}
