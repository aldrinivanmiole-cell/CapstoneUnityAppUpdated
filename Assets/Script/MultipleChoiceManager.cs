using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

[System.Serializable]
public class MCAnswer
{
    public string answer_description;
    public int correct_answer;
}

[System.Serializable]
public class MCQuestion
{
    public int id;
    public string question_description;
    public string tutorial_link; // Tutorial link from database
    public List<MCAnswer> choices;
}

public class MultipleChoiceManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text questionText;
    public TMP_Text progressText;
    public List<Button> choiceButtons;
    public GameObject finishPanel;
    public TMP_Text scoreText;

    [Header("Tutorial")]
    public Button tutorialButton;

    private List<MCQuestion> questions = new List<MCQuestion>();
    private int currentIndex = 0;
    private int correctCount = 0;
    private int studentId;

    void Start()
    {
        studentId = SessionManager.Instance.StudentId;
        finishPanel.SetActive(false);

        // Setup tutorial button
        if (tutorialButton != null)
            tutorialButton.onClick.AddListener(OpenTutorialLink);

        StartCoroutine(LoadQuestions());
    }

    IEnumerator LoadQuestions()
    {
        string url = $"https://homeworkquest.site/get_questions.php?student_id={studentId}";
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to load questions: " + www.error);
                yield break;
            }

            string json = www.downloadHandler.text;
            questions = JsonUtilityWrapper.FromJsonList<MCQuestion>(json);

            if (questions.Count > 0)
                ShowQuestion();
        }
    }

    void ShowQuestion()
    {
        if (currentIndex >= questions.Count)
        {
            StartCoroutine(SaveScore());
            return;
        }

        MCQuestion q = questions[currentIndex];
        questionText.text = q.question_description;
        progressText.text = $"Question {currentIndex + 1} of {questions.Count}";

        for (int i = 0; i < choiceButtons.Count; i++)
        {
            if (i < q.choices.Count)
            {
                MCAnswer choice = q.choices[i];
                choiceButtons[i].gameObject.SetActive(true);
                choiceButtons[i].GetComponentInChildren<TMP_Text>().text = choice.answer_description;
                choiceButtons[i].onClick.RemoveAllListeners();

                int capturedIndex = i; // Closure capture
                choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(q, q.choices[capturedIndex]));
            }
            else
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
        }

        // Show tutorial button if link exists
        if (tutorialButton != null)
            tutorialButton.gameObject.SetActive(!string.IsNullOrEmpty(q.tutorial_link));
    }

    void OnChoiceSelected(MCQuestion question, MCAnswer selectedChoice)
    {
        bool isCorrect = selectedChoice.correct_answer == 1;
        string playerAnswer = selectedChoice.answer_description;
        string correctAnswer = "";

        foreach (var c in question.choices)
        {
            if (c.correct_answer == 1)
            {
                correctAnswer = c.answer_description;
                break;
            }
        }

        if (isCorrect) correctCount++;

        StartCoroutine(SaveAnswerToHistory(studentId, question.id, question.question_description, playerAnswer, correctAnswer, isCorrect ? 1 : 0));

        currentIndex++;
        ShowQuestion();
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
        form.AddField("assignment_type", "Multiple Choice");

        using (UnityWebRequest www = UnityWebRequest.Post("https://homeworkquest.site/save_history.php", form))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
                Debug.LogError("Error saving history: " + www.error);
        }
    }

   IEnumerator SaveScore()
{
    finishPanel.SetActive(true);
    scoreText.text = $"You got {correctCount} / {questions.Count} correct!";

    int assignmentId = (questions.Count > 0) ? questions[0].id : 0;

    WWWForm form = new WWWForm();
    form.AddField("student_id", studentId);
    form.AddField("assignment_id", assignmentId);
    form.AddField("score", correctCount); // Send the total correct answers as score

    using (UnityWebRequest www = UnityWebRequest.Post("https://homeworkquest.site/submit_score.php", form))
    {
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
            Debug.LogError("❌ Error saving score: " + www.error);
        else
            Debug.Log("✅ Score saved successfully: " + www.downloadHandler.text);
    }
}

    void OpenTutorialLink()
    {
        if (currentIndex < questions.Count)
        {
            string link = questions[currentIndex].tutorial_link;
            if (!string.IsNullOrEmpty(link))
            {
                Application.OpenURL(link);
                Debug.Log("Opening tutorial link: " + link);
            }
        }
    }
}
