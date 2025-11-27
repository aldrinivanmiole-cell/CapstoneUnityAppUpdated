using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

[System.Serializable]
public class PSAnswer
{
    public string answer_description;
    public int correct_answer;
}

[System.Serializable]
public class Problem
{
    public int id;
    public string question_description;
    public string tutorial_link; // ✅ Add tutorial link
    public List<PSAnswer> answers;
}

public class ProblemSolvingManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text questionText;
    public TMP_Text progressText;
    public TMP_InputField answerInput;
    public Button submitButton;
    public Button tutorialButton; // ✅ Button for tutorial
    public GameObject finishPanel;
    public TMP_Text scoreText;

    private List<Problem> problems = new List<Problem>();
    private int currentIndex = 0;
    private int correctCount = 0;
    private int studentId;
    private string currentTutorialLink = ""; // ✅ Store current tutorial link

    void Start()
    {
        studentId = SessionManager.Instance.StudentId;
        finishPanel.SetActive(false);
        submitButton.onClick.AddListener(OnSubmitAnswer);
        tutorialButton.onClick.AddListener(OpenTutorialLink); // ✅ Add listener
        StartCoroutine(LoadProblems());
    }

    IEnumerator LoadProblems()
    {
        using (UnityWebRequest www = UnityWebRequest.Get("https://homeworkquest.site/get_problems.php?student_id=" + studentId))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(www.error);
                yield break;
            }

            string json = www.downloadHandler.text;
            problems = JsonUtilityWrapper.FromJsonList<Problem>(json);

            if (problems.Count > 0)
                ShowProblem();
        }
    }

    void ShowProblem()
    {
        if (currentIndex >= problems.Count)
        {
            StartCoroutine(SaveScore());
            return;
        }

        var p = problems[currentIndex];
        questionText.text = p.question_description;
        progressText.text = $"Problem {currentIndex + 1} of {problems.Count}";
        answerInput.text = "";

        // ✅ Update tutorial link
        currentTutorialLink = p.tutorial_link;
        tutorialButton.gameObject.SetActive(!string.IsNullOrEmpty(currentTutorialLink));
    }

    void OpenTutorialLink()
    {
        if (!string.IsNullOrEmpty(currentTutorialLink))
            Application.OpenURL(currentTutorialLink); // ✅ Open in browser
    }

    void OnSubmitAnswer()
    {
        string userAnswer = answerInput.text.Trim();
        if (string.IsNullOrEmpty(userAnswer)) return;

        var p = problems[currentIndex];
        bool isCorrect = false;
        string correctAnswer = "";

        foreach (var ans in p.answers)
        {
            if (ans.correct_answer == 1)
                correctAnswer = ans.answer_description;

            if (ans.correct_answer == 1 &&
                userAnswer.Equals(ans.answer_description, System.StringComparison.OrdinalIgnoreCase))
            {
                correctCount++;
                isCorrect = true;
            }
        }

        StartCoroutine(SaveAnswerToHistory(
            studentId,
            p.id,
            p.question_description,
            userAnswer,
            correctAnswer,
            isCorrect ? 1 : 0
        ));

        currentIndex++;
        ShowProblem();
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
        form.AddField("assignment_type", "Problem Solving");

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
        scoreText.text = $"You solved {correctCount} / {problems.Count} correctly!";

        foreach (var problem in problems)
        {
            string userAnswer = answerInput.text.Trim();

            WWWForm form = new WWWForm();
            form.AddField("student_id", studentId);
            form.AddField("assignment_id", problem.id);
            form.AddField("student_answer", userAnswer);

            using (UnityWebRequest www = UnityWebRequest.Post("https://homeworkquest.site/submit_score.php", form))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                    Debug.LogError("❌ Error saving score: " + www.error);
                else
                    Debug.Log("✅ Score response: " + www.downloadHandler.text);
            }
        }
    }
}
